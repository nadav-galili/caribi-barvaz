using System;
using System.Collections.Generic;
using System.Linq;

namespace BarvazTheDealer.Core
{
    public enum HandCategory
    {
        HighCard = 0,
        OnePair = 1,
        TwoPair = 2,
        ThreeOfAKind = 3,
        Straight = 4,
        Flush = 5,
        FullHouse = 6,
        FourOfAKind = 7,
        StraightFlush = 8,
        RoyalFlush = 9
    }

    /// <summary>
    /// The value of a 5-card poker hand. Compare two values with CompareTo:
    /// category first, then the tiebreak ranks in order.
    /// </summary>
    public sealed class HandValue : IComparable<HandValue>
    {
        public readonly HandCategory Category;

        /// <summary>Ranks (2..14) that break ties within a category, most important first.</summary>
        public readonly int[] Tiebreak;

        public HandValue(HandCategory category, int[] tiebreak)
        {
            Category = category;
            Tiebreak = tiebreak;
        }

        public int CompareTo(HandValue other)
        {
            if (other == null) return 1;
            int c = Category.CompareTo(other.Category);
            if (c != 0) return c;
            for (int i = 0; i < Math.Min(Tiebreak.Length, other.Tiebreak.Length); i++)
            {
                c = Tiebreak[i].CompareTo(other.Tiebreak[i]);
                if (c != 0) return c;
            }
            return 0;
        }

        /// <summary>A short, readable name, e.g. "Pair of Kings" or "Ace-King high".</summary>
        public string Name
        {
            get
            {
                switch (Category)
                {
                    case HandCategory.RoyalFlush: return "Royal Flush";
                    case HandCategory.StraightFlush: return RankName(Tiebreak[0]) + "-high Straight Flush";
                    case HandCategory.FourOfAKind: return "Four " + Plural(Tiebreak[0]);
                    case HandCategory.FullHouse: return "Full House, " + Plural(Tiebreak[0]) + " over " + Plural(Tiebreak[1]);
                    case HandCategory.Flush: return RankName(Tiebreak[0]) + "-high Flush";
                    case HandCategory.Straight: return RankName(Tiebreak[0]) + "-high Straight";
                    case HandCategory.ThreeOfAKind: return "Three " + Plural(Tiebreak[0]);
                    case HandCategory.TwoPair: return "Two Pair, " + Plural(Tiebreak[0]) + " and " + Plural(Tiebreak[1]);
                    case HandCategory.OnePair: return "Pair of " + Plural(Tiebreak[0]);
                    default: return RankName(Tiebreak[0]) + "-" + RankName(Tiebreak[1]) + " high";
                }
            }
        }

        public override string ToString()
        {
            return Name;
        }

        public static string RankName(int rank)
        {
            switch (rank)
            {
                case 14: return "Ace";
                case 13: return "King";
                case 12: return "Queen";
                case 11: return "Jack";
                case 10: return "Ten";
                case 9: return "Nine";
                case 8: return "Eight";
                case 7: return "Seven";
                case 6: return "Six";
                case 5: return "Five";
                case 4: return "Four";
                case 3: return "Three";
                default: return "Two";
            }
        }

        private static string Plural(int rank)
        {
            return rank == 6 ? "Sixes" : RankName(rank) + "s";
        }
    }

    public static class HandEvaluator
    {
        /// <summary>Evaluates exactly five cards.</summary>
        public static HandValue Evaluate(IList<Card> cards)
        {
            if (cards == null || cards.Count != 5)
                throw new ArgumentException("A hand must have exactly 5 cards.");

            bool flush = cards.All(c => c.Suit == cards[0].Suit);

            // Group by rank: bigger groups first, then higher rank first.
            var groups = cards
                .GroupBy(c => (int)c.Rank)
                .Select(g => new { Rank = g.Key, Count = g.Count() })
                .OrderByDescending(g => g.Count)
                .ThenByDescending(g => g.Rank)
                .ToList();

            int[] groupRanks = groups.Select(g => g.Rank).ToArray();

            int straightHigh = 0;
            if (groups.Count == 5)
            {
                int high = groupRanks[0];
                int low = groupRanks[4];
                if (high - low == 4)
                    straightHigh = high;
                else if (high == 14 && groupRanks[1] == 5) // A-2-3-4-5, the "wheel": Ace plays low
                    straightHigh = 5;
            }

            if (straightHigh > 0 && flush)
            {
                var cat = straightHigh == 14 ? HandCategory.RoyalFlush : HandCategory.StraightFlush;
                return new HandValue(cat, new[] { straightHigh });
            }

            if (groups[0].Count == 4)
                return new HandValue(HandCategory.FourOfAKind, groupRanks);

            if (groups[0].Count == 3 && groups[1].Count == 2)
                return new HandValue(HandCategory.FullHouse, groupRanks);

            if (flush)
                return new HandValue(HandCategory.Flush, groupRanks);

            if (straightHigh > 0)
                return new HandValue(HandCategory.Straight, new[] { straightHigh });

            if (groups[0].Count == 3)
                return new HandValue(HandCategory.ThreeOfAKind, groupRanks);

            if (groups[0].Count == 2 && groups[1].Count == 2)
                return new HandValue(HandCategory.TwoPair, groupRanks);

            if (groups[0].Count == 2)
                return new HandValue(HandCategory.OnePair, groupRanks);

            return new HandValue(HandCategory.HighCard, groupRanks);
        }

        /// <summary>
        /// Caribbean Stud: the dealer qualifies with Ace-King high or better
        /// (a hand containing both an Ace and a King, or any pair or better).
        /// </summary>
        public static bool DealerQualifies(HandValue dealer)
        {
            if (dealer.Category >= HandCategory.OnePair) return true;
            return dealer.Tiebreak[0] == 14 && dealer.Tiebreak[1] == 13;
        }
    }
}
