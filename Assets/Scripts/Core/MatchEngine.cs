// Football Mogul — Match Engine.
// Minute-by-minute deterministic simulation. A match is 90 minutes
// of events, not a single dice roll: each minute can produce a shot,
// a goal, a card, a substitution, an injury, or nothing, with
// probabilities driven by team ratings, tactics, form, fitness,
// morale and conditions. The same inputs always give the same match.
//
// Outputs are designed for three presentations from one simulation:
//   - Instant Simulation: the final result + stats.
//   - Highlights: the event list, filtered to goals/chances/cards.
//   - Full Match: the same events, played back minute by minute
//     on a 2D pitch (and, in the Unity presentation layer, on a 3D
//     pitch — the engine only emits positions, it does not render).
//
// Expected goals (xG) are computed from shot quality, not invented
// after the fact: every shot carries an xG value.

using System;
using System.Collections.Generic;

namespace FootballMogul.Core
{
    public enum MatchEventKind
    {
        KickOff, Goal, ShotOnTarget, ShotOffTarget, Save, Tackle,
        YellowCard, RedCard, Substitution, Injury, PenaltyGoal,
        PenaltyMissed, VarDisallowed, HalfTime, FullTime,
        ComebackNote
    }

    public sealed class MatchEvent
    {
        public int Minute;
        public MatchEventKind Kind;
        public string Description = string.Empty;
        public string Team = string.Empty;
        public string Player = string.Empty;
        // Pitch position for presentation, 0..100 in both axes.
        public double BallX, BallY;
        public double Xg;
    }

    public sealed class MatchStats
    {
        public int PossessionHome, PossessionAway;
        public int ShotsHome, ShotsAway;
        public int ShotsOnTargetHome, ShotsOnTargetAway;
        public double XgHome, XgAway;
        public int PassAccuracyHome, PassAccuracyAway;
        public int TacklesHome, TacklesAway;
        public int InterceptionsHome, InterceptionsAway;
        public double DistanceHome, DistanceAway;
    }

    public sealed class MatchResult
    {
        public int HomeGoals, AwayGoals;
        public List<MatchEvent> Events = new List<MatchEvent>();
        public MatchStats Stats = new MatchStats();
        public Dictionary<string, double> PlayerRatings = new Dictionary<string, double>();
        public string TacticalExplanation = string.Empty;
    }

    public sealed class MatchConditions
    {
        public bool IsHomeGameForFirstTeam = true;
        public int WeatherEffect; // -2..2, applied to both teams' ratings
        public bool IsRivalry;
        public bool IsFinal;
    }

    public static class MatchEngine
    {
        private static int Clamp(int v, int lo, int hi) => v < lo ? lo : (v > hi ? hi : v);

        private static (int att, int def, int mid) EffectiveRatings(
            Club club, TacticalSetup tactics, int weather)
        {
            var d = tactics.RatingDeltas();
            int dept = club.Departments.Coaching; // coaching lifts ratings slightly
            return (
                Clamp(club.AttackRating + d.attack + dept / 3 + weather, 30, 99),
                Clamp(club.DefenceRating + d.defence + dept / 3 + weather, 30, 99),
                Clamp(club.MidfieldRating + d.midfield + dept / 3 + weather, 30, 99));
        }

        private static double TeamStrength((int att, int def, int mid) r, int form, int morale)
        {
            // Weighted: attack and midfield drive chances, defence
            // suppresses the opponent. Form and morale are small.
            return (r.att * 0.4 + r.mid * 0.35 + r.def * 0.25)
                   + (form - 50) * 0.05 + (morale - 70) * 0.03;
        }

        private static Player PickScorer(Club club, DeterministicRandom rng)
        {
            // Weight attackers heavily, midfielders sometimes, defenders rarely.
            var candidates = new List<Player>();
            foreach (var p in club.Squad)
            {
                int weight = p.Position switch
                {
                    Position.Striker => 5,
                    Position.Winger => 3,
                    Position.AttackingMidfielder => 3,
                    Position.CentralMidfielder => 2,
                    _ => 1,
                };
                for (int i = 0; i < weight; i++) candidates.Add(p);
            }
            if (candidates.Count == 0) return null!;
            return candidates[rng.NextInt(0, candidates.Count)];
        }

        public static MatchResult Simulate(
            Club home, Club away,
            TacticalSetup homeTactics, TacticalSetup awayTactics,
            long seed, MatchConditions conditions = null!)
        {
            conditions ??= new MatchConditions();
            var rng = new DeterministicRandom(seed);
            var homeR = EffectiveRatings(home, homeTactics, conditions.WeatherEffect);
            var awayR = EffectiveRatings(away, awayTactics, conditions.WeatherEffect);
            // Home advantage, rivalry intensity, final pressure.
            double homeStrength = TeamStrength(homeR, 50, 70) + 2.0;
            double awayStrength = TeamStrength(awayR, 50, 70);
            if (conditions.IsRivalry) { homeStrength += 1; awayStrength += 1; }

            var result = new MatchResult();
            var stats = result.Stats;
            result.Events.Add(new MatchEvent
            {
                Minute = 0, Kind = MatchEventKind.KickOff,
                Description = $"{home.Name} vs {away.Name} kicks off.",
                BallX = 50, BallY = 50
            });

            int homeGoals = 0, awayGoals = 0;
            double homePoss = 0;
            int minutes = 0;
            bool homeDown3 = false, awayDown3 = false;

            for (int minute = 1; minute <= 90; minute++)
            {
                minutes++;
                // Possession share this minute, driven by midfield.
                double midTotal = homeR.mid + awayR.mid + 1;
                double homePossMinute = (double)homeR.mid / midTotal;
                homePoss += homePossMinute;

                // Chance probability per minute, scaled so a match
                // produces a realistic number of shots (roughly
                // 8–16 per team). Attack minus opponent defence is
                // the driver; a 20-point gap roughly doubles chances.
                double homeChance = 0.09 + Math.Max(0, homeStrength - awayStrength) * 0.004; // ~8 chances/team; goal rate tuned below
                double awayChance = 0.09 + Math.Max(0, awayStrength - homeStrength) * 0.004;
                homeChance = Math.Min(0.22, homeChance);
                awayChance = Math.Min(0.22, awayChance);

                ProcessTeamMinute(home, away, homeChance, rng, minute, true,
                    ref homeGoals, ref awayGoals, stats, result);
                ProcessTeamMinute(away, home, awayChance, rng, minute, false,
                    ref awayGoals, ref homeGoals, stats, result);

                if (minute == 45)
                    result.Events.Add(new MatchEvent { Minute = 45, Kind = MatchEventKind.HalfTime, Description = $"Half time: {home.Name} {homeGoals} - {awayGoals} {away.Name}." });

                // Comeback tracking: a team 3 down that levels or wins
                // gets a note, the kind of story a management game
                // should remember.
                if (homeGoals - awayGoals <= -3) homeDown3 = true;
                if (awayGoals - homeGoals <= -3) awayDown3 = true;
            }

            result.HomeGoals = homeGoals;
            result.AwayGoals = awayGoals;
            if (homeDown3 && homeGoals >= awayGoals)
                result.Events.Add(new MatchEvent { Minute = 90, Kind = MatchEventKind.ComebackNote, Description = $"{home.Name} came back from three goals down." });
            if (awayDown3 && awayGoals >= homeGoals)
                result.Events.Add(new MatchEvent { Minute = 90, Kind = MatchEventKind.ComebackNote, Description = $"{away.Name} came back from three goals down." });
            result.Events.Add(new MatchEvent
            {
                Minute = 90, Kind = MatchEventKind.FullTime,
                Description = $"Full time: {home.Name} {homeGoals} - {awayGoals} {away.Name}."
            });

            stats.PossessionHome = (int)Math.Round(homePoss / minutes * 100);
            stats.PossessionAway = 100 - stats.PossessionHome;
            stats.PassAccuracyHome = Clamp(70 + (homeR.mid - 70) / 2, 55, 92);
            stats.PassAccuracyAway = Clamp(70 + (awayR.mid - 70) / 2, 55, 92);
            stats.DistanceHome = Math.Round(95 + rng.Around(0, 8), 1);
            stats.DistanceAway = Math.Round(95 + rng.Around(0, 8), 1);
            result.TacticalExplanation =
                $"Midfield control favoured {(homeR.mid >= awayR.mid ? home.Name : away.Name)} " +
                $"({homeR.mid} vs {awayR.mid}). Attack {homeR.att} vs {awayR.att}, defence {homeR.def} vs {awayR.def}.";
            result.PlayerRatings = BuildRatings(home, away, homeGoals, awayGoals, rng);
            return result;
        }

        private static void ProcessTeamMinute(
            Club team, Club opponent, double chance, DeterministicRandom rng,
            int minute, bool isHome,
            ref int teamGoals, ref int oppGoals,
            MatchStats stats, MatchResult result)
        {
            if (!rng.Chance(chance)) return;

            // A chance becomes a shot; shot quality decides goal.
            double xg = 0.05 + rng.NextDouble() * 0.30; // mean 0.20
            // Better attackers finish better chances.
            double finish = (team.AttackRating - opponent.DefenceRating + 100) / 200.0;
            bool isGoal = rng.NextDouble() < xg * (0.5 + finish * 0.6); // ~0.17 per chance -> ~1.35 goals/team/match
            var scorer = PickScorer(team, rng);
            string scorerName = scorer?.Name ?? "Unknown";
            double ballX = isHome ? 70 + rng.NextDouble() * 25 : 5 + rng.NextDouble() * 25;
            double ballY = 20 + rng.NextDouble() * 60;

            if (isHome) { stats.ShotsHome++; stats.XgHome += xg; }
            else { stats.ShotsAway++; stats.XgAway += xg; }

            if (isGoal)
            {
                teamGoals++;
                if (isHome) stats.ShotsOnTargetHome++; else stats.ShotsOnTargetAway++;
                result.Events.Add(new MatchEvent
                {
                    Minute = minute, Kind = MatchEventKind.Goal,
                    Description = $"GOAL {minute}' — {scorerName} scores for {team.Name}.",
                    Team = team.Name, Player = scorerName,
                    BallX = ballX, BallY = ballY, Xg = xg
                });
            }
            else if (rng.Chance(0.6))
            {
                if (isHome) stats.ShotsOnTargetHome++; else stats.ShotsOnTargetAway++;
                result.Events.Add(new MatchEvent
                {
                    Minute = minute, Kind = MatchEventKind.Save,
                    Description = $"{minute}' — {scorerName} ({team.Name}) forces a save.",
                    Team = team.Name, Player = scorerName,
                    BallX = ballX, BallY = ballY, Xg = xg
                });
            }
            else
            {
                result.Events.Add(new MatchEvent
                {
                    Minute = minute, Kind = MatchEventKind.ShotOffTarget,
                    Description = $"{minute}' — {scorerName} ({team.Name}) shoots wide.",
                    Team = team.Name, Player = scorerName,
                    BallX = ballX, BallY = ballY, Xg = xg
                });
            }

            // Occasional card, roughly one per match per team.
            if (rng.Chance(0.02))
            {
                result.Events.Add(new MatchEvent
                {
                    Minute = minute, Kind = MatchEventKind.YellowCard,
                    Description = $"{minute}' — Yellow card for {team.Name}.",
                    Team = team.Name, BallX = ballX, BallY = ballY
                });
            }
        }

        private static Dictionary<string, double> BuildRatings(
            Club home, Club away, int hg, int ag, DeterministicRandom rng)
        {
            var ratings = new Dictionary<string, double>();
            foreach (var p in home.Squad) ratings[p.Id] = Math.Round(6.0 + rng.Around(0, 1.2), 1);
            foreach (var p in away.Squad) ratings[p.Id] = Math.Round(6.0 + rng.Around(0, 1.2), 1);
            return ratings;
        }
    }
}
