// Football Realm — Story engine, Career mode, Manager mode.
// The story engine is event-driven: events fire from simulation
// state (form, morale, board confidence, rivalry), present choices,
// and apply consequences to morale, chemistry, funds and reputation.
// Nothing in a story event invents a result — consequences are
// explicit numbers in the choice definition.

using System;
using System.Collections.Generic;

namespace FootballRealm.Core
{
    public sealed class StoryChoice
    {
        public string Text = string.Empty;
        public int MoraleDelta;
        public int ChemistryDelta;
        public int FundsDelta;
        public int ReputationDelta;
        public string OutcomeText = string.Empty;
    }

    public sealed class StoryEvent
    {
        public string Id = string.Empty;
        public string Title = string.Empty;
        public string Body = string.Empty;
        public List<StoryChoice> Choices = new List<StoryChoice>();
    }

    public static class StoryEngine
    {
        /// <summary>Captain's ultimatum: fires when morale is low
        /// after a poor run. The four choices are the design's.</summary>
        public static StoryEvent CaptainsUltimatum(Club club)
        {
            return new StoryEvent
            {
                Id = "captains_ultimatum",
                Title = "The Captain's Ultimatum",
                Body = $"The captain of {club.Name} has raised concerns about recent performances and wants a response.",
                Choices = new List<StoryChoice>
                {
                    new StoryChoice { Text = "Address the squad", MoraleDelta = 8, ChemistryDelta = 5, OutcomeText = "The squad responds to the honest talk." },
                    new StoryChoice { Text = "Speak privately with the captain", MoraleDelta = 5, ChemistryDelta = 8, OutcomeText = "The captain feels heard; trust grows." },
                    new StoryChoice { Text = "Demand better performances", MoraleDelta = -5, ChemistryDelta = -3, ReputationDelta = 2, OutcomeText = "The demand hardens attitudes, but standards are clear." },
                    new StoryChoice { Text = "Ignore it and focus on training", MoraleDelta = -8, ChemistryDelta = -5, OutcomeText = "Silence breeds resentment in the dressing room." },
                }
            };
        }

        public static StoryEvent BoardWarning(Club club, int boardConfidence)
        {
            return new StoryEvent
            {
                Id = "board_warning",
                Title = "Board Warning",
                Body = $"The board of {club.Name} is concerned. Confidence is {boardConfidence}%.",
                Choices = new List<StoryChoice>
                {
                    new StoryChoice { Text = "Present a recovery plan", ReputationDelta = 5, OutcomeText = "The board gives you time — for now." },
                    new StoryChoice { Text = "Blame injuries", MoraleDelta = -3, OutcomeText = "The board is unconvinced." },
                    new StoryChoice { Text = "Promise trophies", ReputationDelta = -5, OutcomeText = "A promise the board will hold you to." },
                }
            };
        }

        public static void ApplyChoice(Club club, StoryChoice choice)
        {
            foreach (var p in club.Squad)
            {
                p.Morale = Math.Max(0, Math.Min(100, p.Morale + choice.MoraleDelta));
            }
            club.Funds += choice.FundsDelta;
            club.Reputation = Math.Max(1, Math.Min(99, club.Reputation + choice.ReputationDelta));
        }
    }

    /// <summary>Career mode: one created player, five chapters.</summary>
    public enum CareerChapter
    {
        DreamBegins, BreakingThrough, RiseToStardom, WorldStage, Legacy
    }

    public sealed class CareerPlayer
    {
        public Player Player = null!;
        public CareerChapter Chapter = CareerChapter.DreamBegins;
        public int Experience;
        public int LegacyPoints;
        public int Trophies;
        public List<string> Awards = new List<string>();
        public int EndorsementIncome;
        public int PropertiesOwned;
        public int BusinessInvestments;

        public CareerChapter ChapterForExperience(int xp) => xp switch
        {
            < 50 => CareerChapter.DreamBegins,
            < 200 => CareerChapter.BreakingThrough,
            < 600 => CareerChapter.RiseToStardom,
            < 1500 => CareerChapter.WorldStage,
            _ => CareerChapter.Legacy,
        };

        /// <summary>Training: raises one attribute family, costs
        /// fitness, gains experience. Lifestyle choices modify it
        /// in the Unity layer (fitness, professionalism, family time).</summary>
        public void Train(string family)
        {
            var a = Player.Attributes;
            switch (family)
            {
                case "Technical": a.Technical = Math.Min(99, a.Technical + 1); break;
                case "Physical": a.Physical = Math.Min(99, a.Physical + 1); break;
                case "Mental": a.Mental = Math.Min(99, a.Mental + 1); break;
                case "Tactical": a.Tactical = Math.Min(99, a.Tactical + 1); break;
            }
            Player.Fitness = Math.Max(20, Player.Fitness - 5);
            Experience += 3;
            Chapter = ChapterForExperience(Experience);
        }
    }

    /// <summary>Manager mode: the manager's own record and legacy.</summary>
    public sealed class ManagerCareer
    {
        public string Name = string.Empty;
        public int Matches, Wins, Draws, Losses;
        public int Trophies;
        public int Promotions, RelegationEscapes;
        public int PlayersDeveloped;
        public long TransferProfit;
        public List<string> ClubsManaged = new List<string>();
        public int BoardConfidence = 70; // 0..100

        public double WinPercentage => Matches == 0 ? 0 : (double)Wins / Matches * 100;

        public void RecordResult(int gf, int ga)
        {
            Matches++;
            if (gf > ga) Wins++;
            else if (gf == ga) Draws++;
            else Losses++;
            // Board confidence moves slowly: wins lift it, losses cut it.
            BoardConfidence = Math.Max(0, Math.Min(100,
                BoardConfidence + (gf > ga ? 2 : (gf == ga ? 0 : -3))));
        }

        public bool IsInHallOfFame => Trophies >= 5 || (Matches >= 500 && WinPercentage >= 55);
    }

    public static class Awards
    {
        public static List<string> SeasonAwards(Season season)
        {
            var awards = new List<string>();
            var table = season.Table();
            if (table.Count > 0) awards.Add($"Champions: {table[0].Name}");
            var scorer = season.TopScorer();
            if (scorer != null) awards.Add($"Golden Boot: {scorer.Name} ({scorer.Goals} goals)");
            return awards;
        }
    }
}
