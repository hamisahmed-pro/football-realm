// Football Realm — Unity presentation layer.
// GameManager is the only bootstrap object you need in a scene:
// create an empty scene, add an empty GameObject, attach GameManager,
// and it builds the whole UI at runtime (no prefabs required for v1).
// Core simulation lives in FootballRealm.Core and has no Unity
// dependency, so it can be unit-tested outside Unity.
//
// Platforms: one codebase, adapted interfaces — Android first,
// Windows/macOS/Linux desktop next, iOS later (the stack choice
// from the tech recommendation). No platform-specific code lives
// in Core; platform differences are input size and save path only.

using System.Collections.Generic;
using System.IO;
using FootballRealm.Core;
using UnityEngine;

namespace FootballRealm.Unity
{
    public enum GameScreen { MainMenu, League, Club, Tactics, Transfers, Match, Career, Manager, Settings }

    public sealed class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; } = null!;

        [Header("Data")]
        public TextAsset clubsJson; // assign in Inspector, or place clubs.json in StreamingAssets

        public List<League> Leagues { get; private set; } = new List<League>();
        public League CurrentLeague { get; private set; } = null!;
        public Season CurrentSeason { get; private set; } = null!;
        public Club PlayerClub { get; private set; } = null!;
        public TacticalSetup PlayerTactics { get; private set; } = new TacticalSetup();
        public ManagerCareer Manager { get; private set; } = new ManagerCareer();
        public GameScreen CurrentScreen { get; private set; } = GameScreen.MainMenu;

        public string SavePath => Path.Combine(Application.persistentDataPath, "football_realm_save.json");

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadDatabase();
        }

        private void LoadDatabase()
        {
            string json = null!;
            if (clubsJson != null) json = clubsJson.text;
            else
            {
                string path = Path.Combine(Application.streamingAssetsPath, "clubs.json");
                if (File.Exists(path)) json = File.ReadAllText(path);
            }
            if (string.IsNullOrEmpty(json))
            {
                Debug.LogError("Football Realm: clubs.json not found. Assign it in the Inspector or place it in StreamingAssets.");
                return;
            }
            var data = ClubDatabase.LoadFromJson(json);
            Leagues = ClubDatabase.BuildLeagues(data);
            Debug.Log($"Football Realm: loaded {Leagues.Count} leagues, {ClubDatabase.TotalClubs(data)} clubs.");
        }

        /// <summary>Start a new game in a league, managing a club.</summary>
        public void StartNewGame(string country, string leagueName, string clubName, string managerName)
        {
            CurrentLeague = Leagues.Find(l => l.Country == country && l.Name == leagueName)!;
            CurrentSeason = new Season(CurrentLeague);
            PlayerClub = CurrentLeague.Clubs.Find(c => c.Name == clubName)!;
            Manager = new ManagerCareer { Name = managerName };
            Manager.ClubsManaged.Add(PlayerClub.Name);
            CurrentScreen = GameScreen.League;
        }

        /// <summary>Play the player's next fixture, instantly simulated.</summary>
        public MatchResult PlayNextFixture()
        {
            var fixtures = CurrentSeason.FixturesForRound(CurrentSeason.CurrentRound);
            Fixture mine = null!;
            foreach (var f in fixtures)
            {
                if (f.Home == PlayerClub || f.Away == PlayerClub) { mine = f; break; }
            }
            if (mine == null) return null!;
            long seed = StableHash.Fnv1a(PlayerClub.Id) + CurrentSeason.CurrentRound * 1000003L;
            var homeTactics = mine.Home == PlayerClub ? PlayerTactics : new TacticalSetup();
            var awayTactics = mine.Away == PlayerClub ? PlayerTactics : new TacticalSetup();
            var result = MatchEngine.Simulate(mine.Home, mine.Away, homeTactics, awayTactics, seed);
            CurrentSeason.ApplyResult(mine, result);
            Manager.RecordResult(
                mine.Home == PlayerClub ? result.HomeGoals : result.AwayGoals,
                mine.Home == PlayerClub ? result.AwayGoals : result.HomeGoals);
            CurrentSeason.CurrentRound++;
            return result;
        }

        public void Save()
        {
            var save = new SaveData
            {
                PlayerClubId = PlayerClub?.Id ?? string.Empty,
                LeagueName = CurrentLeague?.Name ?? string.Empty,
                Country = CurrentLeague?.Country ?? string.Empty,
                CurrentRound = CurrentSeason?.CurrentRound ?? 1,
                ManagerName = Manager?.Name ?? string.Empty,
                Funds = PlayerClub?.Funds ?? 0,
            };
            if (CurrentLeague != null)
            {
                foreach (var c in CurrentLeague.Clubs)
                {
                    save.ClubTotals[c.Id] = new[] { c.Points, c.Wins, c.Draws, c.Losses, c.GoalsFor, c.GoalsAgainst };
                }
            }
            SaveSystem.WriteAtomic(SavePath, SaveSystem.Serialize(save));
            Debug.Log($"Football Realm: saved to {SavePath}");
        }

        public bool TryLoad()
        {
            if (!File.Exists(SavePath)) return false;
            try
            {
                var save = SaveSystem.Deserialize(File.ReadAllText(SavePath));
                Debug.Log($"Football Realm: save v{save.Version} loaded, round {save.CurrentRound}.");
                return true;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Football Realm: save failed to load ({ex.Message}). Previous backup is at {SavePath}.bak");
                return false;
            }
        }
    }
}
