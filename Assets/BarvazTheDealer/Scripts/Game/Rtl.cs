using System.Collections.Generic;
using System.Text;

namespace BarvazTheDealer.Game
{
    /// <summary>
    /// Minimal right-to-left support for Unity's legacy Text, which only draws left-to-right.
    /// Hebrew needs no letter shaping, so it is enough to reorder each line into visual order
    /// while keeping runs of numbers / Latin text (like "$10", "1:1", "A-K") in reading order.
    /// Lines are wrapped here (by character count) because wrapping must happen before reordering.
    /// </summary>
    public static class Rtl
    {
        private enum Dir { R, L, Weak, Neutral }

        /// <summary>Converts logical Hebrew text to visual order, wrapping at maxChars per line (0 = no wrap).</summary>
        public static string Visual(string logical, int maxChars = 0)
        {
            if (string.IsNullOrEmpty(logical)) return logical;
            var sb = new StringBuilder();
            string[] paragraphs = logical.Split('\n');
            for (int p = 0; p < paragraphs.Length; p++)
            {
                List<string> lines = Wrap(paragraphs[p], maxChars);
                for (int i = 0; i < lines.Count; i++)
                {
                    if (sb.Length > 0) sb.Append('\n');
                    sb.Append(Line(lines[i]));
                }
            }
            return sb.ToString();
        }

        private static List<string> Wrap(string text, int maxChars)
        {
            var lines = new List<string>();
            if (maxChars <= 0 || text.Length <= maxChars)
            {
                lines.Add(text);
                return lines;
            }
            var current = new StringBuilder();
            foreach (string word in text.Split(' '))
            {
                if (current.Length > 0 && current.Length + 1 + word.Length > maxChars)
                {
                    lines.Add(current.ToString());
                    current.Length = 0;
                }
                if (current.Length > 0) current.Append(' ');
                current.Append(word);
            }
            if (current.Length > 0) lines.Add(current.ToString());
            return lines;
        }

        private static bool IsHebrew(char c)
        {
            return (c >= '֐' && c <= '׿') || (c >= 'יִ' && c <= 'ﭏ');
        }

        private static Dir Classify(char c)
        {
            if (IsHebrew(c)) return Dir.R;
            if (char.IsLetterOrDigit(c)) return Dir.L;
            if (c == '♠' || c == '♥' || c == '♦' || c == '♣') return Dir.L; // suits travel with their rank
            if (c == '$' || c == '+' || c == '-' || c == '%' || c == '.' || c == ':' || c == '/' || c == ',' || c == '×') return Dir.Weak;
            return Dir.Neutral;
        }

        /// <summary>Reorders one line (paragraph direction RTL) into left-to-right visual order.</summary>
        public static string Line(string s)
        {
            int n = s.Length;
            var dirs = new Dir[n];
            for (int i = 0; i < n; i++) dirs[i] = Classify(s[i]);

            // Weak characters ($ + - : . , /) join a neighbouring Latin/number run: "$10", "-$5", "1:1".
            for (int i = 0; i < n; i++)
            {
                if (dirs[i] != Dir.Weak) continue;
                Dir next = Dir.Neutral;
                for (int j = i + 1; j < n; j++)
                {
                    if (dirs[j] == Dir.Weak) continue;
                    next = dirs[j];
                    break;
                }
                Dir prev = i > 0 ? dirs[i - 1] : Dir.Neutral;
                // A hyphen glued to a Hebrew prefix ("ו-4") stays with the Hebrew word.
                bool gluedToHebrew = prev == Dir.R && s[i] != '$';
                dirs[i] = (next == Dir.L && !gluedToHebrew) ? Dir.L : Dir.Neutral;
            }

            // Neutrals between two Latin runs stay Latin ("A K"), everything else follows the RTL paragraph.
            for (int i = 0; i < n; i++)
            {
                if (dirs[i] != Dir.Neutral) continue;
                int a = i - 1;
                while (a >= 0 && dirs[a] == Dir.Neutral) a--;
                int b = i + 1;
                while (b < n && dirs[b] == Dir.Neutral) b++;
                bool ltr = a >= 0 && b < n && dirs[a] == Dir.L && dirs[b] == Dir.L;
                dirs[i] = ltr ? Dir.L : Dir.R;
            }

            // Split into runs, reverse run order, reverse characters inside RTL runs (mirroring brackets).
            var runs = new List<string>();
            var runIsLtr = new List<bool>();
            int start = 0;
            for (int i = 1; i <= n; i++)
            {
                if (i == n || (dirs[i] == Dir.L) != (dirs[start] == Dir.L))
                {
                    runs.Add(s.Substring(start, i - start));
                    runIsLtr.Add(dirs[start] == Dir.L);
                    start = i;
                }
            }

            var sb = new StringBuilder(n);
            for (int r = runs.Count - 1; r >= 0; r--)
            {
                if (runIsLtr[r])
                {
                    sb.Append(runs[r]);
                }
                else
                {
                    string run = runs[r];
                    for (int k = run.Length - 1; k >= 0; k--) sb.Append(Mirror(run[k]));
                }
            }
            return sb.ToString();
        }

        private static char Mirror(char c)
        {
            switch (c)
            {
                case '(': return ')';
                case ')': return '(';
                case '[': return ']';
                case ']': return '[';
                case '<': return '>';
                case '>': return '<';
                default: return c;
            }
        }
    }
}
