// Football Mogul — Tactics.
// Formations, philosophies, team instructions and individual
// instructions are data, not UI strings: they change the match
// engine's ratings, pressing, width and risk in a documented way.

using System;

namespace FootballMogul.Core
{
    public enum Formation
    {
        FourThreeThree, FourTwoThreeOne, FourFourTwo,
        ThreeFiveTwo, ThreeFourThree, FourOneFourOne,
        FiveThreeTwo, Custom
    }

    public enum Philosophy
    {
        Gegenpressing, Possession, CounterAttack, HighPress,
        LowBlock, DirectFootball, WideAttack, TikiTaka
    }

    public enum Mentality { Defensive, Balanced, Attacking }

    [Serializable]
    public sealed class TeamInstructions
    {
        public int PressingIntensity = 50; // 0..100
        public int DefensiveLine = 50;     // 0 = deep, 100 = very high
        public int AttackingWidth = 50;    // 0 = narrow, 100 = very wide
        public Mentality Mentality = Mentality.Balanced;
        public bool ProtectLead;
        public bool ChaseGame;
    }

    [Serializable]
    public sealed class TacticalSetup
    {
        public Formation Formation = Formation.FourThreeThree;
        public Philosophy Philosophy = Philosophy.Possession;
        public TeamInstructions Instructions = new TeamInstructions();

        /// <summary>Rating deltas applied to a club's base ratings.
        /// Small by design: tactics matter, they do not erase a
        /// 15-point quality gap.</summary>
        public (int attack, int defence, int midfield) RatingDeltas()
        {
            int att = 0, def = 0, mid = 0;
            switch (Formation)
            {
                case Formation.FourThreeThree: att += 2; break;
                case Formation.FourFourTwo: def += 1; mid += 1; break;
                case Formation.ThreeFiveTwo: mid += 2; break;
                case Formation.FiveThreeTwo: def += 2; break;
                case Formation.FourTwoThreeOne: mid += 1; att += 1; break;
                case Formation.ThreeFourThree: att += 2; def -= 1; break;
                case Formation.FourOneFourOne: def += 1; break;
            }
            switch (Philosophy)
            {
                case Philosophy.Possession:
                case Philosophy.TikiTaka: mid += 2; break;
                case Philosophy.CounterAttack: att += 2; break;
                case Philosophy.Gegenpressing:
                case Philosophy.HighPress: att += 1; def += 1; break;
                case Philosophy.LowBlock: def += 2; att -= 1; break;
                case Philosophy.DirectFootball: att += 1; break;
                case Philosophy.WideAttack: att += 1; mid += 1; break;
            }
            if (Instructions.Mentality == Mentality.Attacking) { att += 1; def -= 1; }
            if (Instructions.Mentality == Mentality.Defensive) { def += 1; att -= 1; }
            return (att, def, mid);
        }

        public static string FormationName(Formation f) => f switch
        {
            Formation.FourThreeThree => "4-3-3",
            Formation.FourTwoThreeOne => "4-2-3-1",
            Formation.FourFourTwo => "4-4-2",
            Formation.ThreeFiveTwo => "3-5-2",
            Formation.ThreeFourThree => "3-4-3",
            Formation.FourOneFourOne => "4-1-4-1",
            Formation.FiveThreeTwo => "5-3-2",
            _ => "Custom",
        };
    }
}
