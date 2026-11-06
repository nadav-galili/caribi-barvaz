using System;
using System.Collections.Generic;

namespace BarvazTheDealer.Core
{
    /// <summary>A standard 52-card deck, freshly shuffled for every hand.</summary>
    public class Deck
    {
        private readonly List<Card> _cards = new List<Card>(52);
        private readonly Random _random;

        public Deck(Random random = null)
        {
            _random = random ?? new Random();
            Reset();
        }

        public int Count
        {
            get { return _cards.Count; }
        }

        /// <summary>Restores all 52 cards and shuffles them (Fisher-Yates).</summary>
        public void Reset()
        {
            _cards.Clear();
            foreach (Suit suit in Enum.GetValues(typeof(Suit)))
            foreach (Rank rank in Enum.GetValues(typeof(Rank)))
                _cards.Add(new Card(rank, suit));

            for (int i = _cards.Count - 1; i > 0; i--)
            {
                int j = _random.Next(i + 1);
                Card tmp = _cards[i];
                _cards[i] = _cards[j];
                _cards[j] = tmp;
            }
        }

        public Card Draw()
        {
            if (_cards.Count == 0)
                throw new InvalidOperationException("The deck is empty.");

            int last = _cards.Count - 1;
            Card card = _cards[last];
            _cards.RemoveAt(last);
            return card;
        }
    }
}
