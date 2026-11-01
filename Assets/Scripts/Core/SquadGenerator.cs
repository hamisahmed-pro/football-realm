// Football Realm — Squad generation.
// Real clubs, generated squads. This is stated plainly because it
// matters: licensed real player data is not included, and no real
// player is claimed to be in the game. Each club gets a deterministic
// 25-player squad (2 GK, 8 DEF, 8 MID, 7 ATT) whose ratings are driven
// by the club's tier and league strength, so a tier-1 giant is
// stronger than a tier-2 club, but individual players still vary.
//
// Names are generated from a large first/last name pool, seeded by
// club name + player index, so squads are identical on every run.

using System;
using System.Collections.Generic;

namespace FootballRealm.Core
{
    public static class SquadGenerator
    {
        private static readonly string[] FirstNames =
        {
            "James","Mohamed","Liam","Noah","Lucas","Mateo","Leo","Hugo","Alex","Omar",
            "Yusuf","Kofi","Chinedu","Emeka","Tunde","Ibrahim","Ali","Hassan","Karim","Yassine",
            "Pedro","Gabriel","Lucas","Rafael","Thiago","Bruno","Felipe","Diego","Carlos","Miguel",
            "Harry","Jack","George","Thomas","Oliver","William","Henry","Arthur","Ethan","Daniel",
            "Kylian","Antoine","Jules","Theo","Lucas","Adrien","Kingsley","Ousmane","Eduardo","Paul",
            "Kevin","Joshua","Erling","Martin","Jamal","Florian","Kai","Leroy","Serge","Joshua"
        };

        private static readonly string[] LastNames =
        {
            "Smith","Johnson","Williams","Garcia","Martinez","Silva","Santos","Oliveira","Costa","Almeida",
            "Ahmed","Hassan","Ali","Khan","Ibrahim","Mohammed","Yilmaz","Demir","Kaya","Arslan",
            "Okafor","Adeyemi","Obi","Nwosu","Eze","Okoro","Diallo","Traore","Keita","Barry",
            "Muller","Schmidt","Wagner","Becker","Hoffmann","Schulz","Koch","Richter","Klein","Wolf",
            "Rossi","Russo","Ferrari","Greco","Marino","Moretti","Conti","De Luca","Rizzo","Lombardi",
            "Dubois","Martin","Bernard","Thomas","Petit","Robert","Richard","Durand","Leroy","Moreau"
        };

        /// <summary>League strength: tier 1 = 78 base, tier 2 = 64 base,
        /// with a small country bonus for the strongest leagues.
        /// This is a game rating, not a real-world ranking claim.</summary>
        public static int LeagueBaseRating(string country, int tier)
        {
            int baseRating = tier == 1 ? 78 : 64;
            // The five historically strongest leagues in this set get
            // a small bonus; every other league uses the base.
            switch (country)
            {
                case "England":
                case "Spain":
                case "Germany":
                case "Italy":
                case "France":
                    baseRating += tier == 1 ? 6 : 3;
                    break;
            }
            return baseRating;
        }

        public static List<Player> GenerateForClub(Club club)
        {
            var rng = new DeterministicRandom(StableHash.Fnv1a(club.Name));
            int baseRating = LeagueBaseRating(club.Country, club.Tier);
            var squad = new List<Player>();

            var slots = new List<(Position pos, PlayStyle style)>
            {
                (Position.Goalkeeper, PlayStyle.ShotStopper),
                (Position.Goalkeeper, PlayStyle.SweeperKeeper),
                (Position.CentreBack, PlayStyle.BallPlayingDefender),
                (Position.CentreBack, PlayStyle.NoNonsenseDefender),
                (Position.CentreBack, PlayStyle.BallPlayingDefender),
                (Position.CentreBack, PlayStyle.NoNonsenseDefender),
                (Position.FullBack, PlayStyle.BallPlayingDefender),
                (Position.FullBack, PlayStyle.BallPlayingDefender),
                (Position.FullBack, PlayStyle.NoNonsenseDefender),
                (Position.FullBack, PlayStyle.NoNonsenseDefender),
                (Position.DefensiveMidfielder, PlayStyle.Destroyer),
                (Position.DefensiveMidfielder, PlayStyle.DeepLyingPlaymaker),
                (Position.CentralMidfielder, PlayStyle.BoxToBox),
                (Position.CentralMidfielder, PlayStyle.DeepLyingPlaymaker),
                (Position.CentralMidfielder, PlayStyle.BoxToBox),
                (Position.AttackingMidfielder, PlayStyle.AdvancedPlaymaker),
                (Position.AttackingMidfielder, PlayStyle.ShadowStriker),
                (Position.Winger, PlayStyle.InsideForward),
                (Position.Winger, PlayStyle.TraditionalWinger),
                (Position.Winger, PlayStyle.InsideForward),
                (Position.Striker, PlayStyle.Poacher),
                (Position.Striker, PlayStyle.TargetMan),
                (Position.Striker, PlayStyle.CompleteForward),
                (Position.Striker, PlayStyle.Poacher),
                (Position.CentralMidfielder, PlayStyle.BoxToBox),
            };

            for (int i = 0; i < slots.Count; i++)
            {
                var (pos, style) = slots[i];
                int overall = (int)Math.Round(rng.Around(baseRating, 7));
                overall = Math.Max(45, Math.Min(92, overall));
                var attrs = new Attributes
                {
                    Technical = Clamp(overall + rng.NextInt(-6, 7)),
                    Physical = Clamp(overall + rng.NextInt(-6, 7)),
                    Mental = Clamp(overall + rng.NextInt(-6, 7)),
                    Tactical = Clamp(overall + rng.NextInt(-6, 7)),
                    Personality = rng.NextInt(40, 90),
                    Reputation = rng.NextInt(40, 90),
                };
                int age = pos == Position.Goalkeeper
                    ? rng.NextInt(20, 38)
                    : rng.NextInt(18, 36);
                int value = overall * overall * 120; // value scales with quality
                squad.Add(new Player
                {
                    Id = $"{club.Id}-{i}",
                    Name = $"{FirstNames[rng.NextInt(0, FirstNames.Length)]} {LastNames[rng.NextInt(0, LastNames.Length)]}",
                    Nationality = club.Country,
                    Age = age,
                    Position = pos,
                    Style = style,
                    Attributes = attrs,
                    Wage = overall * 180,
                    Value = value,
                    ContractYearsLeft = rng.NextInt(1, 5),
                    ReleaseClause = rng.Chance(0.3) ? value * 2 : (int?)null,
                });
            }
            return squad;
        }

        private static int Clamp(int v) => v < 1 ? 1 : (v > 99 ? 99 : v);
    }
}
