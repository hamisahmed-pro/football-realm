// Football Realm — Core models.
// All ratings are integers 1..99. No floats are used for ratings,
// money, or points, so simulation is exact and reproducible.
// Money is in whole currency units (no minor-unit conversion in v1;
// a currency layer is a documented follow-up, not a hidden rule).

using System;
using System.Collections.Generic;

namespace FootballRealm.Core
{
    public enum Position
    {
        Goalkeeper,
        CentreBack, FullBack,
        DefensiveMidfielder, CentralMidfielder, AttackingMidfielder,
        Winger, Striker
    }

    public enum PlayStyle
    {
        ShotStopper, SweeperKeeper, Distributor,
        BallPlayingDefender, NoNonsenseDefender,
        DeepLyingPlaymaker, BoxToBox, Destroyer,
        AdvancedPlaymaker, ShadowStriker,
        InsideForward, TraditionalWinger,
        Poacher, TargetMan, CompleteForward
    }

    /// <summary>One attribute family. The six families are the career
    /// progression system: training, matches, coaching, mentorship,
    /// experience and lifestyle all feed these, never a single number.</summary>
    [Serializable]
    public sealed class Attributes
    {
        public int Technical;   // passing, shooting, dribbling, tackling
        public int Physical;    // speed, stamina, strength, agility
        public int Mental;      // vision, composure, positioning, decisions
        public int Tactical;    // awareness, pressing, movement
        public int Personality;// leadership, discipline, confidence
        public int Reputation; // popularity, loyalty, professionalism

        public int Overall => (Technical + Physical + Mental + Tactical) / 4;

        public Attributes Clone() => (Attributes)MemberwiseClone();

        public static Attributes Clamp(Attributes a)
        {
            return new Attributes
            {
                Technical = Clamp1(a.Technical),
                Physical = Clamp1(a.Physical),
                Mental = Clamp1(a.Mental),
                Tactical = Clamp1(a.Tactical),
                Personality = Clamp1(a.Personality),
                Reputation = Clamp1(a.Reputation),
            };
        }

        private static int Clamp1(int v) => v < 1 ? 1 : (v > 99 ? 99 : v);
    }

    [Serializable]
    public sealed class Player
    {
        public string Id = string.Empty;
        public string Name = string.Empty;
        public string Nationality = string.Empty;
        public int Age;
        public Position Position;
        public PlayStyle Style;
        public Attributes Attributes = new Attributes();
        public int Morale = 70;      // 0..100
        public int Fitness = 90;     // 0..100
        public int Form = 50;        // 0..100, rolling recent performance
        public int Loyalty = 60;     // 0..100
        public int Wage;             // per week
        public int Value;            // transfer value
        public int ContractYearsLeft;
        public int? ReleaseClause;   // null = none
        public bool IsInjured;
        public int InjuryWeeksLeft;
        public int Goals, Assists, Appearances, CleanSheets;

        public int Overall => Attributes.Overall;
    }

    [Serializable]
    public sealed class Club
    {
        public string Id = string.Empty;
        public string Name = string.Empty;
        public string Country = string.Empty;
        public string League = string.Empty;
        public int Tier; // 1 or 2
        public int Reputation; // 1..99, derived from tier + league strength
        public long Funds;
        public int StadiumCapacity;
        public List<Player> Squad = new List<Player>();
        public Departments Departments = new Departments();
        public int Points, Wins, Draws, Losses, GoalsFor, GoalsAgainst;
        public int GoalDifference => GoalsFor - GoalsAgainst;

        public int AttackRating => SquadRating(p => p.Position == Position.Striker || p.Position == Position.Winger);
        public int DefenceRating => SquadRating(p => p.Position == Position.Goalkeeper || p.Position == Position.CentreBack || p.Position == Position.FullBack);
        public int MidfieldRating => SquadRating(p => p.Position == Position.DefensiveMidfielder || p.Position == Position.CentralMidfielder || p.Position == Position.AttackingMidfielder);

        private int SquadRating(Func<Player, bool> filter)
        {
            int sum = 0, count = 0;
            foreach (var p in Squad)
            {
                if (filter(p)) { sum += p.Overall; count++; }
            }
            return count == 0 ? 50 : sum / count;
        }
    }

    [Serializable]
    public sealed class Departments
    {
        public int FirstTeam = 1;
        public int YouthAcademy = 1;
        public int Scouting = 1;
        public int Medical = 1;
        public int Coaching = 1;
        public int Finance = 1;
        public int Marketing = 1;
        public int Stadium = 1;
        public int Recruitment = 1;

        public const int MaxLevel = 10;

        public static long UpgradeCost(int currentLevel) => 25000L * currentLevel * currentLevel;
    }

    [Serializable]
    public sealed class League
    {
        public string Country = string.Empty;
        public string Name = string.Empty;
        public int Tier;
        public List<Club> Clubs = new List<Club>();
    }

    /// <summary>A league table row, computed from Club season totals.</summary>
    public sealed class TableRow
    {
        public Club Club = null!;
        public int Played => Club.Wins + Club.Draws + Club.Losses;
        public int Points => Club.Points;
    }

    public static class TableSorter
    {
        public static List<Club> Sort(List<Club> clubs)
        {
            var list = new List<Club>(clubs);
            list.Sort((a, b) =>
            {
                int c = b.Points.CompareTo(a.Points);
                if (c != 0) return c;
                c = b.GoalDifference.CompareTo(a.GoalDifference);
                if (c != 0) return c;
                c = b.GoalsFor.CompareTo(a.GoalsFor);
                if (c != 0) return c;
                return string.Compare(a.Name, b.Name, StringComparison.Ordinal);
            });
            return list;
        }
    }
}
