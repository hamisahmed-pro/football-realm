// Football Realm — Core verification (plain .NET, no Unity).
// Run: dotnet run --project Tests/CoreTests
// Exit code 0 = all checks passed. Any failure prints FAIL and exits 1.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FootballRealm.Core;

internal static class Program
{
    private static int _failures;

    private static void Check(bool condition, string name)
    {
        Console.WriteLine((condition ? "PASS" : "FAIL") + " " + name);
        if (!condition) _failures++;
    }

    private static int Main()
    {
        string jsonPath = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory, "..", "..", "..", "..",
            "Assets", "Data", "clubs.json"));
        // When run from the project folder, fall back to the known path.
        if (!File.Exists(jsonPath))
            jsonPath = "/home/hatch/workspace/football-realm/Assets/Data/clubs.json";
        Check(File.Exists(jsonPath), "clubs.json exists: " + jsonPath);

        var data = ClubDatabase.LoadFromJson(File.ReadAllText(jsonPath));
        Check(data.Count == 40, $"40 leagues (got {data.Count})");
        Check(ClubDatabase.TotalClubs(data) == 799, $"799 clubs (got {ClubDatabase.TotalClubs(data)})");

        var leagues = ClubDatabase.BuildLeagues(data);
        var eng = leagues.Find(l => l.Country == "England" && l.Tier == 1);
        Check(eng != null && eng.Clubs.Count == 20, "England Premier League has 20 clubs");
        Check(leagues.All(l => l.Clubs.All(c => c.Squad.Count == 25)),
            "Every club has a 25-player squad");
        int totalPlayers = leagues.Sum(l => l.Clubs.Sum(c => c.Squad.Count));
        Check(totalPlayers == 799 * 25, $"19,975 generated players (got {totalPlayers})");

        // Match determinism: same seed, same result.
        var home = eng!.Clubs[0];
        var away = eng.Clubs[1];
        var t = new TacticalSetup();
        var r1 = MatchEngine.Simulate(home, away, t, t, 12345);
        var r2 = MatchEngine.Simulate(home, away, t, t, 12345);
        Check(r1.HomeGoals == r2.HomeGoals && r1.AwayGoals == r2.AwayGoals
              && r1.Events.Count == r2.Events.Count,
            $"Match deterministic: {home.Name} {r1.HomeGoals}-{r1.AwayGoals} {away.Name}, {r1.Events.Count} events");
        Check(r1.Events.Count >= 5, "Match produces events (kickoff, half-time, full-time + shots)");
        Check(r1.Stats.PossessionHome + r1.Stats.PossessionAway == 100,
            $"Possession sums to 100 ({r1.Stats.PossessionHome}/{r1.Stats.PossessionAway})");

        // Fixtures: 20 clubs -> 38 rounds, 380 fixtures, each pair twice.
        var season = new Season(eng);
        Check(season.MaxRound == 38, $"20-club season has 38 rounds (got {season.MaxRound})");
        Check(season.Fixtures.Count == 380, $"20-club season has 380 fixtures (got {season.Fixtures.Count})");
        var pairCounts = new Dictionary<string, int>();
        foreach (var f in season.Fixtures)
        {
            string key = string.Join("|", new[] { f.Home.Id, f.Away.Id }.OrderBy(x => x));
            pairCounts[key] = pairCounts.TryGetValue(key, out var v) ? v + 1 : 1;
        }
        Check(pairCounts.Values.All(v => v == 2), "Every pair plays exactly twice");

        // Season table: apply a result, points move.
        var fixture = season.FixturesForRound(1)[0];
        var result = MatchEngine.Simulate(fixture.Home, fixture.Away, t, t, 999);
        int before = fixture.Home.Points + fixture.Away.Points;
        season.ApplyResult(fixture, result);
        int after = fixture.Home.Points + fixture.Away.Points;
        Check(after - before == (result.HomeGoals == result.AwayGoals ? 2 : 3),
            "Points awarded correctly (3 for a win, 1 each for a draw)");

        // Transfers: fee below demand fails, at/above passes, registration moves player + funds.
        var seller = eng.Clubs[2];
        var buyer = eng.Clubs[3];
        var player = seller.Squad[20]; // a striker slot
        long buyerBefore = buyer.Funds, sellerBefore = seller.Funds;
        var deal = new TransferDeal { Player = player, From = seller, To = buyer, AgentRelationship = 0 };
        long demand = TransferMarket.FeeDemand(deal);
        Check(!TransferMarket.TryFee(deal, demand - 1), "Fee below demand is rejected");
        Check(TransferMarket.TryFee(deal, demand), "Fee at demand is accepted");
        Check(TransferMarket.TryContract(deal, player.Wage + 5000, 3), "Contract at/above demand is accepted");
        Check(TransferMarket.Register(deal), "Registration succeeds");
        Check(!seller.Squad.Contains(player) && buyer.Squad.Contains(player), "Player moved clubs");
        Check(buyer.Funds == buyerBefore - demand && seller.Funds == sellerBefore + demand,
            "Funds moved buyer -> seller by the fee");

        // Saves: round-trip + atomic write + backup.
        var save = new SaveData { PlayerClubId = home.Id, LeagueName = eng.Name, Country = "England", CurrentRound = 5, ManagerName = "Test" };
        string tmp = Path.Combine(Path.GetTempPath(), "fm_test_save.json");
        // Use a workspace temp dir, not /tmp tmpfs, for the atomic write test.
        tmp = "/home/hatch/workspace/football-realm/Tests/fm_test_save.json";
        SaveSystem.WriteAtomic(tmp, SaveSystem.Serialize(save));
        var loaded = SaveSystem.Deserialize(File.ReadAllText(tmp));
        Check(loaded.PlayerClubId == home.Id && loaded.CurrentRound == 5, "Save round-trips");
        SaveSystem.WriteAtomic(tmp, SaveSystem.Serialize(save));
        Check(File.Exists(tmp + ".bak"), "Atomic write keeps a .bak backup");
        File.Delete(tmp); File.Delete(tmp + ".bak");

        // Story: a choice moves morale in the stated direction.
        int moraleBefore = seller.Squad[0].Morale;
        StoryEngine.ApplyChoice(seller, StoryEngine.CaptainsUltimatum(seller).Choices[0]);
        Check(seller.Squad[0].Morale > moraleBefore, "Story choice raises morale as documented");

        Console.WriteLine(_failures == 0 ? "ALL CHECKS PASSED" : $"{_failures} CHECK(S) FAILED");
        return _failures == 0 ? 0 : 1;
    }
}
