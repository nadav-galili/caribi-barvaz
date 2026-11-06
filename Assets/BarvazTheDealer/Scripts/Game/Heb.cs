using BarvazTheDealer.Core;

namespace BarvazTheDealer.Game
{
    /// <summary>All player-facing Hebrew text in one place (logical order; Rtl.Visual() handles display).</summary>
    public static class Heb
    {
        public const string Dealer = "ברווז";
        public const string YourDealer = "הדילר שלך";
        public const string Bankroll = "קופה";
        public const string LastHand = "יד אחרונה: ";
        public const string You = "אתה: ";
        public const string BarvazPrefix = "ברווז: ";
        public const string NotQualified = " (לא משתתף)";

        public const string Deal = "חלק קלפים";
        public const string NextHand = "יד הבאה";
        public const string Fold = "התקפל";
        public const string Raise = "העלה";
        public const string Swap = "החלף קלף";
        public const string PickCard = "בחר קלף להחלפה";
        public const string NoMoreSwaps = "נגמרו ההחלפות";
        public const string Rebuy = "קנייה מחדש";
        public const string SlapHim = "סטור לו!";
        public const string Keys = "רווח: חלוקה · F: קיפול · R: העלאה · S: סטירה · M: מוזיקה";

        // Barvaz's lines
        public const string Welcome = "ברוך הבא לשולחן שלי. שים $5 אנטה ואני מחלק.";
        public const string AnteIn = "האנטה בפנים. קלפים בדרך...";
        public const string Raised = "העלאה! בוא נראה מה יש לי.";
        public const string Folded = "מתקפל? הנה מה שהיה לי.";
        public const string Broke = "נגמרו לך הצ'יפים, חביבי. קנייה מחדש?";
        public const string PickToSwap = "לחץ על קלף שתרצה להחליף.";
        public const string SwapDone = "הנה קלף חדש. בהצלחה.";

        public static string Decide(Card up)
        {
            return "הקלף הגלוי שלי: " + CardName(up) + ". מתקפל, מעלה $10, או מחליף קלף?";
        }

        public static readonly string[] FoldLines =
        {
            "התקפלת? האנטה שלי, תודה רבה.",
            "חכם. או פחדן. האנטה שלי.",
        };

        public static readonly string[] NoQualifyLines =
        {
            "אין לי אס-מלך. האנטה משלמת 1:1, ההעלאה חוזרת אליך.",
            "יד זבל... אני לא משתתף. קח את האנטה.",
        };

        public static readonly string[] PlayerWinLines =
        {
            "אוי לא... ניצחת. רק לא בפנים, בבקשה!",
            "אתה מרמה? בטח מרמה. נו, תסטור.",
            "טוב, טוב, ניצחת. תהיה עדין!",
        };

        public static readonly string[] DealerWinLines =
        {
            "חחח! הבית תמיד מנצח.",
            "תודה על הצ'יפים, חביבי!",
            "עוד יד? אני רעב לעוד צ'יפים.",
        };

        public static readonly string[] OuchLines =
        {
            "אאוץ'! זה היה מיותר.",
            "אמא'לה!",
            "בסדר, הבנתי, ניצחת!",
            "את זה אני זוכר לך.",
            "עוד אחת ואני קורא לאבטחה!",
            "וואו. ראיתי כוכבים.",
        };

        public const string Push = "תיקו מושלם. כל אחד לוקח את שלו.";
        public const string RecoverLine = "אוקיי... עוד יד? הפעם אני מנצח.";

        // Banner titles
        public const string BannerFold = "קיפלת";
        public const string BannerNoQualify = "ברווז לא משתתף!";
        public const string BannerWin = "ניצחת!";
        public const string BannerBigWin = "ניצחון ענק!";
        public const string BannerLose = "ברווז ניצח";
        public const string BannerPush = "תיקו";

        public static string SlapsEarned(int n)
        {
            return n == 1 ? "הרווחת סטירה אחת! לחץ על הפרצוף שלו" : "הרווחת " + n + " סטירות! לחץ על הפרצוף שלו";
        }

        public static string SwapButton(int cost)
        {
            return Swap + " $" + cost;
        }

        public static string Signed(int amount)
        {
            if (amount > 0) return "+$" + amount;
            if (amount < 0) return "-$" + (-amount);
            return "$0";
        }

        // ------------------------------------------------------------- cards & hands

        private static string SuitName(Suit s)
        {
            switch (s)
            {
                case Suit.Spades: return "עלה";
                case Suit.Hearts: return "לב";
                case Suit.Diamonds: return "יהלום";
                default: return "תלתן";
            }
        }

        private static string RankSingular(int r)
        {
            switch (r)
            {
                case 14: return "אס";
                case 13: return "מלך";
                case 12: return "מלכה";
                case 11: return "נסיך";
                default: return r.ToString();
            }
        }

        private static string RankPlural(int r)
        {
            switch (r)
            {
                case 14: return "אסים";
                case 13: return "מלכים";
                case 12: return "מלכות";
                case 11: return "נסיכים";
                default: return r.ToString();
            }
        }

        public static string CardName(Card c)
        {
            return RankSingular((int)c.Rank) + " " + SuitName(c.Suit);
        }

        public static string HandName(HandValue v)
        {
            int[] t = v.Tiebreak;
            switch (v.Category)
            {
                case HandCategory.RoyalFlush: return "רויאל פלאש!";
                case HandCategory.StraightFlush: return "סטרייט פלאש עד " + RankSingular(t[0]);
                case HandCategory.FourOfAKind: return "רביעיית " + RankPlural(t[0]);
                case HandCategory.FullHouse: return "פול האוס: " + RankPlural(t[0]) + " על " + RankPlural(t[1]);
                case HandCategory.Flush: return "פלאש, " + RankSingular(t[0]) + " גבוה";
                case HandCategory.Straight: return "סטרייט עד " + RankSingular(t[0]);
                case HandCategory.ThreeOfAKind: return "שלישיית " + RankPlural(t[0]);
                case HandCategory.TwoPair: return "שני זוגות: " + RankPlural(t[0]) + " ו" + RankPlural(t[1]);
                case HandCategory.OnePair: return "זוג " + RankPlural(t[0]);
                default: return RankSingular(t[0]) + "-" + RankSingular(t[1]) + " גבוה";
            }
        }

        public static string CategoryName(HandCategory c)
        {
            switch (c)
            {
                case HandCategory.RoyalFlush: return "רויאל פלאש";
                case HandCategory.StraightFlush: return "סטרייט פלאש";
                case HandCategory.FourOfAKind: return "רביעייה";
                case HandCategory.FullHouse: return "פול האוס";
                case HandCategory.Flush: return "פלאש";
                case HandCategory.Straight: return "סטרייט";
                case HandCategory.ThreeOfAKind: return "שלישייה";
                case HandCategory.TwoPair: return "שני זוגות";
                case HandCategory.OnePair: return "זוג";
                default: return "קלף גבוה";
            }
        }
    }
}
