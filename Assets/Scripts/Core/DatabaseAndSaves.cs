// Football Realm — Club database and save system.
// Database: clubs.json (799 clubs, 40 leagues, 20 countries) is the
// single source of truth for clubs. Squads are generated, not stored,
// so the JSON stays small and squads stay deterministic.
// Saves: versioned JSON, written atomically (temp file + replace),
// with a backup of the previous save. A corrupted save never
// destroys the previous good save — that is the whole point of the
// atomic write.
//
// JSON is handled by MiniJson (dependency-free), not System.Text.Json,
// because System.Text.Json is not officially available as a Unity
// package. Core therefore compiles in Unity with zero packages.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace FootballRealm.Core
{
    public sealed class LeagueData
    {
        public string Country = string.Empty;
        public string League = string.Empty;
        public int Tier;
        public List<string> Clubs = new List<string>();
    }

    public static class ClubDatabase
    {
        public static List<LeagueData> LoadFromJson(string json)
        {
            var arr = MiniJson.ParseArray(json);
            var result = new List<LeagueData>();
            foreach (var item in arr)
            {
                if (item is not Dictionary<string, object> obj) continue;
                var ld = new LeagueData
                {
                    Country = MiniJson.GetString(obj, "country"),
                    League = MiniJson.GetString(obj, "league"),
                    Tier = MiniJson.GetInt(obj, "tier"),
                };
                if (obj.TryGetValue("clubs", out var clubsObj) && clubsObj is List<object> clubs)
                {
                    foreach (var c in clubs)
                        if (c is string name) ld.Clubs.Add(name);
                }
                result.Add(ld);
            }
            return result;
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
            var sb = new StringBuilder();
            sb.Append("{\n");
            sb.Append($"  \"version\": {save.Version},\n");
            sb.Append($"  \"playerClubId\": \"{MiniJson.Escape(save.PlayerClubId)}\",\n");
            sb.Append($"  \"leagueName\": \"{MiniJson.Escape(save.LeagueName)}\",\n");
            sb.Append($"  \"country\": \"{MiniJson.Escape(save.Country)}\",\n");
            sb.Append($"  \"seasonYear\": {save.SeasonYear},\n");
            sb.Append($"  \"currentRound\": {save.CurrentRound},\n");
            sb.Append($"  \"managerName\": \"{MiniJson.Escape(save.ManagerName)}\",\n");
            sb.Append($"  \"funds\": {save.Funds},\n");
            sb.Append($"  \"legacyPoints\": {save.LegacyPoints},\n");
            sb.Append("  \"achievements\": [");
            for (int i = 0; i < save.Achievements.Count; i++)
            {
                if (i > 0) sb.Append(", ");
                sb.Append($"\"{MiniJson.Escape(save.Achievements[i])}\"");
            }
            sb.Append("],\n  \"clubTotals\": {");
            bool first = true;
            foreach (var kv in save.ClubTotals)
            {
                if (!first) sb.Append(", ");
                first = false;
                sb.Append($"\"{MiniJson.Escape(kv.Key)}\": [{string.Join(", ", kv.Value)}]");
            }
            sb.Append("}\n}");
            return sb.ToString();
        }

        public static SaveData Deserialize(string json)
        {
            var obj = MiniJson.ParseObject(json);
            var save = new SaveData
            {
                Version = MiniJson.GetInt(obj, "version"),
                PlayerClubId = MiniJson.GetString(obj, "playerClubId"),
                LeagueName = MiniJson.GetString(obj, "leagueName"),
                Country = MiniJson.GetString(obj, "country"),
                SeasonYear = MiniJson.GetInt(obj, "seasonYear", 2025),
                CurrentRound = MiniJson.GetInt(obj, "currentRound", 1),
                ManagerName = MiniJson.GetString(obj, "managerName"),
                Funds = MiniJson.GetLong(obj, "funds"),
                LegacyPoints = MiniJson.GetInt(obj, "legacyPoints"),
            };
            if (save.Version != 1)
                throw new InvalidDataException($"Unsupported save version {save.Version}.");
            if (obj.TryGetValue("achievements", out var ach) && ach is List<object> achList)
                foreach (var a in achList)
                    if (a is string s) save.Achievements.Add(s);
            if (obj.TryGetValue("clubTotals", out var ct) && ct is Dictionary<string, object> ctObj)
            {
                foreach (var kv in ctObj)
                {
                    if (kv.Value is List<object> nums)
                    {
                        var arr = new int[nums.Count];
                        for (int i = 0; i < nums.Count; i++)
                            arr[i] = nums[i] is long l ? (int)l : 0;
                        save.ClubTotals[kv.Key] = arr;
                    }
                }
            }
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
            // Unity's .NET profile has no File.Move overwrite overload
            // (plain .NET does), so delete-then-move instead.
            if (File.Exists(path)) File.Delete(path);
            File.Move(temp, path);
        }
    }
}
