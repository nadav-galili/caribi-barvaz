// Standalone checks for the pure game logic (no Unity needed).
// Run from the project root:
//   mcs -out:LogicTests/tests.exe LogicTests/LogicTests.cs Assets/BarvazTheDealer/Scripts/Core/*.cs && mono LogicTests/tests.exe
using System;
using System.Collections.Generic;
using System.Linq;
using BarvazTheDealer.Core;

public static class LogicTests
{
    private static int _failures;

    private static List<Card> H(string s)
    {
        return s.Split(' ').Select(Card.Parse).ToList();
    }

    private static void Check(bool ok, string what)
    {
        if (!ok)
        {
            _failures++;
            Console.WriteLine("FAIL: " + what);
        }
    }

    private static void Cat(string hand, HandCategory expected)
    {
        var v = HandEvaluator.Evaluate(H(hand));
        Check(v.Category == expected, hand + " -> " + v.Category + " (expected " + expected + ")");
    }

    private static void Beats(string a, string b)
    {
        var va = HandEvaluator.Evaluate(H(a));
        var vb = HandEvaluator.Evaluate(H(b));
        Check(va.CompareTo(vb) > 0 && vb.CompareTo(va) < 0, a + " should beat " + b);
    }

    private static void Ties(string a, string b)
    {
        var va = HandEvaluator.Evaluate(H(a));
        var vb = HandEvaluator.Evaluate(H(b));
        Check(va.CompareTo(vb) == 0, a + " should tie " + b);
    }

    public static int Main()
    {
        // Categories
        Cat("As Ks Qs Js Ts", HandCategory.RoyalFlush);
        Cat("9h 8h 7h 6h 5h", HandCategory.StraightFlush);
        Cat("5d 4d 3d 2d Ad", HandCategory.StraightFlush);
        Cat("7c 7d 7h 7s 2c", HandCategory.FourOfAKind);
        Cat("Kc Kd Kh 3s 3c", HandCategory.FullHouse);
        Cat("Ah 9h 7h 4h 2h", HandCategory.Flush);
        Cat("Ac Kd Qh Js Tc", HandCategory.Straight);
        Cat("5c 4d 3h 2s Ac", HandCategory.Straight);
        Cat("Qc Kd Ah 2s 3c", HandCategory.HighCard); // no wrap-around straight
        Cat("8c 8d 8h Ks 2c", HandCategory.ThreeOfAKind);
        Cat("Jc Jd 4h 4s 9c", HandCategory.TwoPair);
        Cat("Tc Td 4h 7s 9c", HandCategory.OnePair);
        Cat("Ac Kd 9h 7s 2c", HandCategory.HighCard);

        // Comparisons
        Beats("6c 5d 4h 3s 2c", "5c 4d 3h 2s Ac"); // wheel is the lowest straight
        Beats("Ac Ad 9h 7s 2c", "Kc Kd Ah Qs Jc");
        Beats("Ac Ad 9h 7s 3c", "Ah As 9c 7d 2h"); // kicker
        Beats("Jc Jd 4h 4s Ac", "Jh Js 4c 4d Kc"); // two pair kicker
        Beats("Qc Qd 3h 3s 2c", "Jc Jd Th Ts Ac"); // higher top pair wins
        Beats("3c 3d 3h 2s 2c", "Ac Ad Kh Ks Qc");
        Beats("Ah 9h 4h 3h 2h", "Ac Kc Qc Jc 9d"); // flush beats high card
        Ties("Ac Kd 9h 7s 2c", "Ad Kh 9s 7c 2d");

        // Dealer qualification
        Check(HandEvaluator.DealerQualifies(HandEvaluator.Evaluate(H("Ac Kd 4h 3s 2c"))), "A-K high qualifies");
        Check(!HandEvaluator.DealerQualifies(HandEvaluator.Evaluate(H("Ac Qd Jh 9s 8c"))), "A-Q high does not qualify");
        Check(!HandEvaluator.DealerQualifies(HandEvaluator.Evaluate(H("Kc Qd Jh 9s 8c"))), "K high does not qualify");
        Check(HandEvaluator.DealerQualifies(HandEvaluator.Evaluate(H("2c 2d 5h 7s 9c"))), "pair of twos qualifies");

        // Payouts (ante 5, raise 10)
        var r = CaribbeanStudRules.ResolveRaise(H("Ac Ad 9h 7s 2c"), H("Kc Qd 9s 7c 2d"), 5, 10);
        Check(r.Outcome == RoundOutcome.DealerDidNotQualify && r.Net == 5, "no-qualify: ante wins, raise pushes (+5), got " + r.Net);

        r = CaribbeanStudRules.ResolveRaise(H("8c 8d 8h Ks 2c"), H("Ac Kd 9s 7c 2d"), 5, 10);
        Check(r.Outcome == RoundOutcome.PlayerWins && r.Net == 5 + 30, "trips beat AK: ante +5, raise 3:1 +30, got " + r.Net);

        r = CaribbeanStudRules.ResolveRaise(H("As Ks Qs Js Ts"), H("2c 2d 9s 7c 3d"), 5, 10);
        Check(r.Net == 5 + 1000, "royal pays 100:1 on raise, got " + r.Net);

        r = CaribbeanStudRules.ResolveRaise(H("Ac Kh 9h 7s 2c"), H("2c 2d 9s 7c 3d"), 5, 10);
        Check(r.Outcome == RoundOutcome.DealerWins && r.Net == -15, "dealer wins: lose 15, got " + r.Net);

        r = CaribbeanStudRules.ResolveRaise(H("Ac Kd 9h 7s 2c"), H("Ad Kh 9s 7c 2d"), 5, 10);
        Check(r.Outcome == RoundOutcome.Push && r.Net == 0, "tie pushes, got " + r.Net);

        r = CaribbeanStudRules.ResolveFold(H("Ac Kd 9h 7s 2c"), H("Ad Kh 9s 7c 2d"), 5);
        Check(r.Outcome == RoundOutcome.PlayerFolded && r.Net == -5, "fold loses ante, got " + r.Net);

        // Exhaustive count of all 2,598,960 five-card hands against the known totals.
        var deck = new List<Card>();
        foreach (Suit s in Enum.GetValues(typeof(Suit)))
        foreach (Rank rk in Enum.GetValues(typeof(Rank)))
            deck.Add(new Card(rk, s));
        var counts = new long[10];
        var hand = new Card[5];
        for (int a = 0; a < 52; a++)
        for (int b = a + 1; b < 52; b++)
        for (int c = b + 1; c < 52; c++)
        for (int d = c + 1; d < 52; d++)
        for (int e = d + 1; e < 52; e++)
        {
            hand[0] = deck[a]; hand[1] = deck[b]; hand[2] = deck[c]; hand[3] = deck[d]; hand[4] = deck[e];
            counts[(int)HandEvaluator.Evaluate(hand).Category]++;
        }
        long[] expected = { 1302540, 1098240, 123552, 54912, 10200, 5108, 3744, 624, 36, 4 };
        for (int i = 0; i < 10; i++)
            Check(counts[i] == expected[i], (HandCategory)i + ": " + counts[i] + " vs " + expected[i]);

        // Monte Carlo sanity check of the house edge with a simple "raise with A-K or better" strategy.
        // Published edge for near-optimal play is about 5.2% of the ante; a simple strategy lands a bit worse.
        var rng = new Random(1234);
        var dk = new Deck(rng);
        long net = 0;
        const int Hands = 1000000;
        for (int i = 0; i < Hands; i++)
        {
            dk.Reset();
            var p = new List<Card>(); var dl = new List<Card>();
            for (int k = 0; k < 5; k++) { p.Add(dk.Draw()); dl.Add(dk.Draw()); }
            var pv = HandEvaluator.Evaluate(p);
            bool raise = HandEvaluator.DealerQualifies(pv);
            var res = raise ? CaribbeanStudRules.ResolveRaise(p, dl, 5, 10) : CaribbeanStudRules.ResolveFold(p, dl, 5);
            net += res.Net;
        }
        double edge = -(double)net / Hands / 5.0;
        Console.WriteLine("House edge per ante (raise with AK+): " + (edge * 100).ToString("F2") + "%");
        Check(edge > 0.04 && edge < 0.09, "house edge in plausible range");

        Console.WriteLine(_failures == 0 ? "ALL TESTS PASSED" : _failures + " FAILURE(S)");
        return _failures == 0 ? 0 : 1;
    }
}
