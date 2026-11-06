using System;

namespace BarvazTheDealer.Core
{
    public enum Suit
    {
        Clubs,
        Diamonds,
        Hearts,
        Spades
    }

    public enum Rank
    {
        Two = 2,
        Three = 3,
        Four = 4,
        Five = 5,
        Six = 6,
        Seven = 7,
        Eight = 8,
        Nine = 9,
        Ten = 10,
        Jack = 11,
        Queen = 12,
        King = 13,
        Ace = 14
    }

    /// <summary>An immutable playing card. Pure C#, no Unity dependency.</summary>
    public struct Card : IEquatable<Card>
    {
        public readonly Rank Rank;
        public readonly Suit Suit;

        public Card(Rank rank, Suit suit)
        {
            Rank = rank;
            Suit = suit;
        }

        public bool IsRed
        {
            get { return Suit == Suit.Hearts || Suit == Suit.Diamonds; }
        }

        public string RankLabel
        {
            get
            {
                switch (Rank)
                {
                    case Rank.Ace: return "A";
                    case Rank.King: return "K";
                    case Rank.Queen: return "Q";
                    case Rank.Jack: return "J";
                    default: return ((int)Rank).ToString();
                }
            }
        }

        public string SuitSymbol
        {
            get
            {
                switch (Suit)
                {
                    case Suit.Spades: return "♠";
                    case Suit.Hearts: return "♥";
                    case Suit.Diamonds: return "♦";
                    default: return "♣";
                }
            }
        }

        /// <summary>Parses short notation like "As", "Td", "10h", "7c".</summary>
        public static Card Parse(string text)
        {
            if (string.IsNullOrEmpty(text) || text.Length < 2)
                throw new FormatException("Bad card: " + text);

            string r = text.Substring(0, text.Length - 1).ToUpperInvariant();
            char s = char.ToLowerInvariant(text[text.Length - 1]);

            Rank rank;
            switch (r)
            {
                case "A": rank = Rank.Ace; break;
                case "K": rank = Rank.King; break;
                case "Q": rank = Rank.Queen; break;
                case "J": rank = Rank.Jack; break;
                case "T":
                case "10": rank = Rank.Ten; break;
                default:
                    int n;
                    if (!int.TryParse(r, out n) || n < 2 || n > 9)
                        throw new FormatException("Bad rank: " + text);
                    rank = (Rank)n;
                    break;
            }

            Suit suit;
            switch (s)
            {
                case 's': suit = Suit.Spades; break;
                case 'h': suit = Suit.Hearts; break;
                case 'd': suit = Suit.Diamonds; break;
                case 'c': suit = Suit.Clubs; break;
                default: throw new FormatException("Bad suit: " + text);
            }

            return new Card(rank, suit);
        }

        public bool Equals(Card other)
        {
            return Rank == other.Rank && Suit == other.Suit;
        }

        public override bool Equals(object obj)
        {
            return obj is Card && Equals((Card)obj);
        }

        public override int GetHashCode()
        {
            return (int)Rank * 4 + (int)Suit;
        }

        public override string ToString()
        {
            return RankLabel + SuitSymbol;
        }
    }
}
