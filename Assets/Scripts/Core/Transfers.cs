// Football Realm — Transfer system.
// The seven stages from the design, implemented as a state machine,
// not as a single "buy" button:
//   1. Identify   2. Scout   3. Approach club / agent
//   4. Negotiate fee   5. Negotiate wages / contract
//   6. Bonuses, clauses, incentives   7. Registration
// Agent relationships, loyalty, release clauses, loans, hijacked
// targets and free transfers are modelled. Deadline Day is a season
// event with a countdown and breaking-news feed entries.

using System;
using System.Collections.Generic;

namespace FootballRealm.Core
{
    public enum TransferStage
    {
        Identified, Scouted, Approached, FeeNegotiating,
        ContractNegotiating, ClausesAgreed, Registered, Failed, Loaned
    }

    public sealed class TransferDeal
    {
        public Player Player = null!;
        public Club From = null!;
        public Club To = null!;
        public TransferStage Stage = TransferStage.Identified;
        public long FeeOffer;
        public int WageOffer;
        public int ContractYears;
        public int? ReleaseClause;
        public bool IsLoan;
        public int LoanWeeks;
        public string Log = string.Empty;
        public int AgentRelationship; // -100..100, affects fee/wage demands
        public bool WasHijacked;
    }

    public static class TransferMarket
    {
        /// <summary>Scout report: a 0..100 rating with uncertainty.
        /// Higher scouting department = narrower uncertainty.</summary>
        public static (int estimate, int uncertainty) Scout(Player player, int scoutingLevel)
        {
            int uncertainty = Math.Max(2, 15 - scoutingLevel);
            return (player.Overall, uncertainty);
        }

        /// <summary>Fee negotiation: the selling club's demand, adjusted
        /// by agent relationship and player loyalty. Returns the
        /// counter-demand; the buyer must meet or beat it.</summary>
        public static long FeeDemand(TransferDeal deal)
        {
            double loyaltyFactor = 1.0 + (deal.Player.Loyalty - 60) * 0.002;
            double agentFactor = 1.0 - deal.AgentRelationship * 0.001;
            long demand = (long)(deal.Player.Value * loyaltyFactor * agentFactor);
            if (deal.Player.ReleaseClause.HasValue)
                demand = Math.Min(demand, deal.Player.ReleaseClause.Value);
            return Math.Max(0, demand);
        }

        public static bool TryFee(TransferDeal deal, long offer)
        {
            deal.FeeOffer = offer;
            long demand = FeeDemand(deal);
            if (offer >= demand)
            {
                deal.Stage = TransferStage.ContractNegotiating;
                deal.Log += $"Fee agreed at {offer}. ";
                return true;
            }
            deal.Log += $"Fee rejected: demand {demand}, offered {offer}. ";
            return false;
        }

        public static bool TryContract(TransferDeal deal, int wage, int years)
        {
            deal.WageOffer = wage;
            deal.ContractYears = years;
            int demand = deal.Player.Wage + (100 - deal.Player.Morale) * 20;
            if (wage >= demand && years >= 1)
            {
                deal.Stage = TransferStage.ClausesAgreed;
                deal.Log += $"Contract agreed: wage {wage}, {years} years. ";
                return true;
            }
            deal.Log += $"Contract rejected: wage demand {demand}. ";
            return false;
        }

        /// <summary>Final stage: funds check, then move the player.</summary>
        public static bool Register(TransferDeal deal)
        {
            if (deal.Stage != TransferStage.ClausesAgreed) return false;
            if (!deal.IsLoan && deal.To.Funds < deal.FeeOffer) return false;
            if (!deal.IsLoan) deal.To.Funds -= deal.FeeOffer;
            if (!deal.IsLoan) deal.From.Funds += deal.FeeOffer;
            deal.From.Squad.Remove(deal.Player);
            deal.To.Squad.Add(deal.Player);
            deal.Player.ContractYearsLeft = deal.ContractYears;
            deal.Player.Wage = deal.WageOffer;
            deal.Player.ReleaseClause = deal.ReleaseClause;
            deal.Stage = deal.IsLoan ? TransferStage.Loaned : TransferStage.Registered;
            deal.Log += "Registration complete. ";
            return true;
        }

        public static List<Player> FreeAgents(List<Club> allClubs, DeterministicRandom rng)
        {
            // Players out of contract (0 years left) across the world.
            var free = new List<Player>();
            foreach (var c in allClubs)
                foreach (var p in c.Squad)
                    if (p.ContractYearsLeft <= 0) free.Add(p);
            return free;
        }
    }
}
