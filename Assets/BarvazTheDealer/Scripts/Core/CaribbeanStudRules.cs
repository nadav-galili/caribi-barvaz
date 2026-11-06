using System.Collections.Generic;

namespace BarvazTheDealer.Core
{
    public enum RoundOutcome
    {
        PlayerFolded,
        DealerDidNotQualify,
        PlayerWins,
        DealerWins,
        Push
    }

    /// <summary>The settled result of one hand. All amounts are in whole dollars.</summary>
    public struct RoundResult
    {
        public RoundOutcome Outcome;
        public HandValue PlayerHand;
        public HandValue DealerHand;
        public bool DealerQualified;
        public int Ante;
        public int Raise;

        /// <summary>Money handed back on the ante spot, stake included (0 = lost).</summary>
        public int AnteReturned;

        /// <summary>Money handed back on the raise spot, stake included (0 = lost).</summary>
        public int RaiseReturned;

        /// <summary>The paytable multiplier applied to the raise when the player won (e.g. 3 for 3:1).</summary>
        public int RaiseMultiplier;

        public int TotalWagered
        {
            get { return Ante + Raise; }
        }

        public int TotalReturned
        {
            get { return AnteReturned + RaiseReturned; }
        }

        /// <summary>Profit or loss for the hand.</summary>
        public int Net
        {
            get { return TotalReturned - TotalWagered; }
        }
    }

    /// <summary>
    /// Standard Caribbean Stud Poker rules (no progressive side bet).
    ///
    /// 1. Player posts an ante. Player and dealer get 5 cards; one dealer card is face up.
    /// 2. Player folds (loses the ante) or raises exactly 2x the ante.
    /// 3. Dealer needs Ace-King or better to qualify.
    ///    - Dealer does not qualify: ante pays 1:1, raise is returned (push).
    ///    - Dealer qualifies, player wins: ante pays 1:1, raise pays by the paytable.
    ///    - Dealer qualifies, dealer wins: player loses ante and raise.
    ///    - Exact tie: ante and raise both push.
    /// </summary>
    public static class CaribbeanStudRules
    {
        public const int AnteAmount = 5;
        public const int RaiseAmount = 10; // 2x the ante, as the standard rules require

        /// <summary>House rule: before deciding, the player may swap up to two cards for a fee.</summary>
        public const int MaxSwaps = 2;

        /// <summary>Fee for the n-th swap of the hand (0-based): $5, then $10. Fees are not returned.</summary>
        public static int SwapCost(int swapsAlreadyUsed)
        {
            return swapsAlreadyUsed == 0 ? 5 : 10;
        }

        /// <summary>Raise payout odds ("X to 1") when the player beats a qualifying dealer.</summary>
        public static int RaiseMultiplier(HandCategory category)
        {
            switch (category)
            {
                case HandCategory.RoyalFlush: return 100;
                case HandCategory.StraightFlush: return 50;
                case HandCategory.FourOfAKind: return 20;
                case HandCategory.FullHouse: return 7;
                case HandCategory.Flush: return 5;
                case HandCategory.Straight: return 4;
                case HandCategory.ThreeOfAKind: return 3;
                case HandCategory.TwoPair: return 2;
                default: return 1; // One pair or high card
            }
        }

        /// <summary>Rows for the on-screen paytable, best hand first.</summary>
        public static readonly KeyValuePair<HandCategory, string>[] PaytableRows =
        {
            new KeyValuePair<HandCategory, string>(HandCategory.RoyalFlush, "Royal Flush"),
            new KeyValuePair<HandCategory, string>(HandCategory.StraightFlush, "Straight Flush"),
            new KeyValuePair<HandCategory, string>(HandCategory.FourOfAKind, "Four of a Kind"),
            new KeyValuePair<HandCategory, string>(HandCategory.FullHouse, "Full House"),
            new KeyValuePair<HandCategory, string>(HandCategory.Flush, "Flush"),
            new KeyValuePair<HandCategory, string>(HandCategory.Straight, "Straight"),
            new KeyValuePair<HandCategory, string>(HandCategory.ThreeOfAKind, "Three of a Kind"),
            new KeyValuePair<HandCategory, string>(HandCategory.TwoPair, "Two Pair"),
            new KeyValuePair<HandCategory, string>(HandCategory.OnePair, "Pair or less")
        };

        public static RoundResult ResolveFold(IList<Card> player, IList<Card> dealer, int ante)
        {
            var dealerHand = HandEvaluator.Evaluate(dealer);
            return new RoundResult
            {
                Outcome = RoundOutcome.PlayerFolded,
                PlayerHand = HandEvaluator.Evaluate(player),
                DealerHand = dealerHand,
                DealerQualified = HandEvaluator.DealerQualifies(dealerHand),
                Ante = ante,
                Raise = 0,
                AnteReturned = 0,
                RaiseReturned = 0
            };
        }

        public static RoundResult ResolveRaise(IList<Card> player, IList<Card> dealer, int ante, int raise)
        {
            var playerHand = HandEvaluator.Evaluate(player);
            var dealerHand = HandEvaluator.Evaluate(dealer);
            bool qualifies = HandEvaluator.DealerQualifies(dealerHand);

            var result = new RoundResult
            {
                PlayerHand = playerHand,
                DealerHand = dealerHand,
                DealerQualified = qualifies,
                Ante = ante,
                Raise = raise
            };

            if (!qualifies)
            {
                result.Outcome = RoundOutcome.DealerDidNotQualify;
                result.AnteReturned = ante * 2; // 1:1
                result.RaiseReturned = raise;   // push
                return result;
            }

            int cmp = playerHand.CompareTo(dealerHand);
            if (cmp > 0)
            {
                result.Outcome = RoundOutcome.PlayerWins;
                result.RaiseMultiplier = RaiseMultiplier(playerHand.Category);
                result.AnteReturned = ante * 2;
                result.RaiseReturned = raise * (1 + result.RaiseMultiplier);
            }
            else if (cmp < 0)
            {
                result.Outcome = RoundOutcome.DealerWins;
            }
            else
            {
                result.Outcome = RoundOutcome.Push;
                result.AnteReturned = ante;
                result.RaiseReturned = raise;
            }

            return result;
        }
    }
}
