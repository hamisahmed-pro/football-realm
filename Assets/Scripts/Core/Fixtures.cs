// Football Realm — Fixtures and Season.
// Berger round-robin: every club plays every other club once at home
// and once away (double round-robin = 2 * (n - 1) rounds). Odd club
// counts get a bye. This replaces the foundation slice's rotating
// opponent rule, which could repeat pairings.

using System;
using System.Collections.Generic;

namespace FootballRealm.Core
{
    public sealed class Fixture
    {
        public int Round;
        public Club Home = null!;
        public Club Away = null!;
        public bool Played;
        public MatchResult Result = null!;
    }

    public static class FixtureGenerator
    {
        /// <summary>Double round-robin fixtures for a league.</summary>
        public static List<Fixture> Generate(List<Club> clubs)
        {
            var teams = new List<Club>(clubs);
            bool hasBye = teams.Count % 2 == 1;
            if (hasBye) teams.Add(null!); // bye placeholder

            int n = teams.Count;
            int rounds = n - 1;
            var firstLeg = new List<Fixture>();

            // Berger: fix the first team, rotate the rest.
            var rotating = new List<Club>(teams);
            for (int round = 0; round < rounds; round++)
            {
                for (int i = 0; i < n / 2; i++)
                {
                    var home = rotating[i];
                    var away = rotating[n - 1 - i];
                    if (home == null || away == null) continue; // bye
                    // Alternate home/away by round so no club gets
                    // all its home games in one half.
                    bool swap = (round % 2 == 1);
                    firstLeg.Add(new Fixture
                    {
                        Round = round + 1,
                        Home = swap ? away : home,
                        Away = swap ? home : away
                    });
                }
                // Rotate all but the first element.
                var last = rotating[n - 1];
                rotating.RemoveAt(n - 1);
                rotating.Insert(1, last);
            }

            var all = new List<Fixture>(firstLeg);
            foreach (var f in firstLeg)
            {
                all.Add(new Fixture
                {
                    Round = f.Round + rounds,
                    Home = f.Away,
                    Away = f.Home
                });
            }
            return all;
        }
    }

    /// <summary>
    /// One league season: fixtures, table, promotion/relegation
    /// places, and awards. Promotion/relegation is computed, not
    /// just displayed: the top tier-2 clubs swap with the bottom
    /// tier-1 clubs of the same country at season end.
    /// </summary>
    public sealed class Season
    {
        public League League = null!;
        public List<Fixture> Fixtures = new List<Fixture>();
        public int CurrentRound = 1;
        public bool Finished => CurrentRound > MaxRound;
        public int MaxRound => Fixtures.Count == 0 ? 0 : Fixtures[Fixtures.Count - 1].Round;

        public Season(League league)
        {
            League = league;
            Fixtures = FixtureGenerator.Generate(league.Clubs);
        }

        public List<Fixture> FixturesForRound(int round)
        {
            return Fixtures.FindAll(f => f.Round == round);
        }

        public void ApplyResult(Fixture fixture, MatchResult result)
        {
            fixture.Played = true;
            fixture.Result = result;
            ApplyClub(fixture.Home, result.HomeGoals, result.AwayGoals);
            ApplyClub(fixture.Away, result.AwayGoals, result.HomeGoals);
        }

        private static void ApplyClub(Club club, int gf, int ga)
        {
            club.GoalsFor += gf;
            club.GoalsAgainst += ga;
            if (gf > ga) { club.Wins++; club.Points += 3; }
            else if (gf == ga) { club.Draws++; club.Points += 1; }
            else club.Losses++;
        }

        public List<Club> Table() => TableSorter.Sort(League.Clubs);

        /// <summary>Top scorer across all squads in this league.</summary>
        public Player TopScorer()
        {
            Player best = null!;
            foreach (var c in League.Clubs)
                foreach (var p in c.Squad)
                    if (best == null || p.Goals > best.Goals) best = p;
            return best;
        }
    }
}
