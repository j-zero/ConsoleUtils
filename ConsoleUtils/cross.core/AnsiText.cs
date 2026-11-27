using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Pastel
{
    /// <summary>
    /// Measuring, padding and truncating of strings that contain ANSI escape sequences (colors from Pastel).
    /// The escape sequences take no space on the screen, so <c>"x".Pastel("#ff0000").Length</c> is wrong for layout work –
    /// use <see cref="VisibleLength"/> instead.
    /// </summary>
    public static class AnsiText
    {
        // CSI sequences (colors, cursor, ...) ESC [ ... final, OSC sequences (titles, hyperlinks) ESC ] ... BEL or ESC \, and two-character sequences
        private static readonly Regex AnsiSequence = new(
            @"\u001b(?:\[[0-?]*[ -/]*[@-~]|\][^\u0007\u001b]*(?:\u0007|\u001b\\)|[@-Z\\-_])",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        /// <summary>True if the string contains escape sequences.</summary>
        public static bool HasAnsi(this string input) => input != null && input.Contains('\u001b') && AnsiSequence.IsMatch(input);

        /// <summary>The string without any escape sequences.</summary>
        public static string Strip(this string input)
        {
            if (string.IsNullOrEmpty(input) || !input.Contains('\u001b'))
                return input;
            return AnsiSequence.Replace(input, "");
        }

        /// <summary>
        /// Number of console cells the string needs: escape sequences count 0, combining and zero width characters count 0,
        /// surrogate pairs count once, East Asian wide characters and most emoji count 2.
        /// </summary>
        public static int VisibleLength(this string input)
        {
            if (string.IsNullOrEmpty(input))
                return 0;

            int width = 0;
            foreach (var rune in input.Strip().EnumerateRunes())
                width += RuneWidth(rune);
            return width;
        }

        /// <summary>Like <see cref="string.PadRight(int)"/>, but <paramref name="totalWidth"/> is the visible width.</summary>
        public static string PadRightVisible(this string input, int totalWidth, char paddingChar = ' ')
        {
            input ??= string.Empty;
            int missing = totalWidth - input.VisibleLength();
            return missing > 0 ? input + new string(paddingChar, missing) : input;
        }

        /// <summary>Like <see cref="string.PadLeft(int)"/>, but <paramref name="totalWidth"/> is the visible width.</summary>
        public static string PadLeftVisible(this string input, int totalWidth, char paddingChar = ' ')
        {
            input ??= string.Empty;
            int missing = totalWidth - input.VisibleLength();
            return missing > 0 ? new string(paddingChar, missing) + input : input;
        }

        /// <summary>Centers the string in a field of the given visible width.</summary>
        public static string CenterVisible(this string input, int totalWidth, char paddingChar = ' ')
        {
            input ??= string.Empty;
            int missing = totalWidth - input.VisibleLength();
            if (missing <= 0)
                return input;
            int left = missing / 2;
            return new string(paddingChar, left) + input + new string(paddingChar, missing - left);
        }

        /// <summary>
        /// Shortens the string to at most <paramref name="maxWidth"/> visible cells. Colors that were started stay intact,
        /// the colors are reset after the ellipsis. Strings that fit are returned unchanged.
        /// </summary>
        public static string TruncateVisible(this string input, int maxWidth, string ellipsis = "…")
        {
            if (string.IsNullOrEmpty(input) || input.VisibleLength() <= maxWidth)
                return input;
            if (maxWidth <= 0)
                return string.Empty;

            ellipsis ??= string.Empty;
            int ellipsisWidth = ellipsis.VisibleLength();
            if (ellipsisWidth > maxWidth)
            {
                ellipsis = string.Empty;
                ellipsisWidth = 0;
            }
            int budget = maxWidth - ellipsisWidth;

            var result = new StringBuilder();
            bool sawAnsi = false;
            int used = 0;
            int pos = 0;
            while (pos < input.Length)
            {
                var m = AnsiSequence.Match(input, pos);
                if (m.Success && m.Index == pos)
                {
                    result.Append(m.Value);
                    sawAnsi = true;
                    pos += m.Length;
                    continue;
                }

                Rune.DecodeFromUtf16(input.AsSpan(pos), out Rune rune, out int consumed);
                int w = RuneWidth(rune);
                if (used + w > budget)
                    break;
                result.Append(input, pos, consumed);
                used += w;
                pos += consumed;
            }

            result.Append(ellipsis);
            if (sawAnsi)
                result.Append("\u001b[0m"); // do not leak the color into the following text
            return result.ToString();
        }

        // ---- character width -----------------------------------------------------------------

        private static int RuneWidth(Rune rune)
        {
            int cp = rune.Value;
            if (cp == 0 || cp < 0x20 || (cp >= 0x7F && cp < 0xA0))
                return 0; // control characters

            switch (Rune.GetUnicodeCategory(rune))
            {
                case UnicodeCategory.NonSpacingMark:
                case UnicodeCategory.EnclosingMark:
                case UnicodeCategory.Format: // zero width space/joiner, variation selectors
                    return 0;
            }

            return IsWide(cp) ? 2 : 1;
        }

        // East Asian Wide / Fullwidth and the emoji blocks that terminals draw two cells wide
        private static bool IsWide(int cp) =>
            (cp >= 0x1100 && cp <= 0x115F) ||
            (cp >= 0x2E80 && cp <= 0x303E) ||
            (cp >= 0x3041 && cp <= 0xA4CF) ||
            (cp >= 0xAC00 && cp <= 0xD7A3) ||
            (cp >= 0xF900 && cp <= 0xFAFF) ||
            (cp >= 0xFE30 && cp <= 0xFE6F) ||
            (cp >= 0xFF00 && cp <= 0xFF60) ||
            (cp >= 0xFFE0 && cp <= 0xFFE6) ||
            (cp >= 0x1F300 && cp <= 0x1F64F) ||
            (cp >= 0x1F680 && cp <= 0x1F6FF) ||
            (cp >= 0x1F900 && cp <= 0x1F9FF) ||
            (cp >= 0x20000 && cp <= 0x3FFFD);
    }
}
