// Football Mogul — Club database and save system.
// Database: clubs.json (799 clubs, 40 leagues, 20 countries) is the
// single source of truth for clubs. Squads are generated, not stored,
// so the JSON stays small and squads stay deterministic.
// Saves: versioned JSON, written atomically (temp file + replace),
// with a backup of the previous save. A corrupted save never
// destroys the previous good save — that is the whole point of the
// atomic write.
//
// This file uses System.Text.Json, which is available in Unity 2021+
// and in plain .NET. In Unity, the JSON text asset is loaded from
// Resources/StreamingAssets; in tests, it is loaded from disk.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FootballMogul.Core
{
    public sealed class LeagueData
    {
        [JsonPropertyName("country")] public string Country = string.Empty;
        [JsonPropertyName("league")] public string League = string.Empty;
        [JsonPropertyName("tier")] public int Tier;
        [JsonPropertyName("clubs")] public List<string> Clubs = new List<string>();
    }

    public static class ClubDatabase
    {
        public static List<LeagueData> LoadFromJson(string json)
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true, IncludeFields = true };
            return JsonSerializer.Deserialize<List<LeagueData>>(json, options)
                   ?? new List<LeagueData>();
        }

        /// <summary>Build playable leagues: every club gets an Id,
        /// a reputation, funds, a stadium, and a generated squad.</summary>
        public static List<League> BuildLeagues(List<LeagueData> data)
        {
            var leagues = new List<League>();
            foreach (var ld in data)
            {
                var league = new League { Country = ld.Country, Name = ld.League, Tier = ld.Tier };
                foreach (var name in ld.Clubs)
                {
                    var club = new Club
                    {
                        Id = $"{ld.Country}-{ld.League}-{name}".Replace(" ", "_"),
                        Name = name,
                        Country = ld.Country,
                        League = ld.League,
                        Tier = ld.Tier,
                        Reputation = SquadGenerator.LeagueBaseRating(ld.Country, ld.Tier),
                        Funds = ld.Tier == 1 ? 5_000_000 : 1_000_000,
                        StadiumCapacity = ld.Tier == 1 ? 30000 : 12000,
                    };
                    club.Squad = SquadGenerator.GenerateForClub(club);
                    league.Clubs.Add(club);
                }
                leagues.Add(league);
            }
            return leagues;
        }

        public static int TotalClubs(List<LeagueData> data)
        {
            int total = 0;
            foreach (var ld in data) total += ld.Clubs.Count;
            return total;
        }
    }

    /// <summary>Versioned save: the version field is what allows a
    /// future update to migrate an old save instead of discarding it.</summary>
    public sealed class SaveData
    {
        public int Version = 1;
        public string PlayerClubId = string.Empty;
        public string LeagueName = string.Empty;
        public string Country = string.Empty;
        public int SeasonYear = 2025;
        public int CurrentRound = 1;
        public string ManagerName = string.Empty;
        public long Funds;
        public int LegacyPoints;
        public List<string> Achievements = new List<string>();
        // Club season totals, keyed by club Id, so a save does not
        // need to store 20,000 players — squads regenerate identically.
        public Dictionary<string, int[]> ClubTotals = new Dictionary<string, int[]>();
    }

    public static class SaveSystem
    {
        public static string Serialize(SaveData save)
        {
            var options = new JsonSerializerOptions { WriteIndented = true, IncludeFields = true };
            return JsonSerializer.Serialize(save, options);
        }

        public static SaveData Deserialize(string json)
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true, IncludeFields = true };
            var save = JsonSerializer.Deserialize<SaveData>(json, options);
            if (save == null) throw new InvalidDataException("Save file is empty or invalid.");
            if (save.Version != 1)
                throw new InvalidDataException($"Unsupported save version {save.Version}.");
            return save;
        }

        /// <summary>Atomic write: write temp, replace, keep a backup
        /// of the previous save. Never write directly over the only copy.</summary>
        public static void WriteAtomic(string path, string content)
        {
            string temp = path + ".tmp";
            string backup = path + ".bak";
            File.WriteAllText(temp, content);
            if (File.Exists(path))
            {
                File.Copy(path, backup, overwrite: true);
            }
            File.Move(temp, path, overwrite: true);
        }
    }
}
