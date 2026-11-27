using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace klemmbrett
{
    /// <summary>
    /// The "HTML Format" (CF_HTML) of the clipboard: a header with byte offsets followed by the HTML document.
    /// https://learn.microsoft.com/windows/win32/dataxchg/html-clipboard-format
    /// </summary>
    internal static class HtmlFormat
    {
        private const string HeaderTemplate = "Version:0.9\r\nStartHTML:{0:D10}\r\nEndHTML:{1:D10}\r\nStartFragment:{2:D10}\r\nEndFragment:{3:D10}\r\n";
        private const string Pre = "<html>\r\n<body>\r\n<!--StartFragment-->";
        private const string Post = "<!--EndFragment-->\r\n</body>\r\n</html>";

        /// <summary>Wraps HTML (a fragment or a complete document) into the clipboard format.</summary>
        public static string Build(string html)
        {
            var body = Regex.Match(html, @"<body[^>]*>(?<b>.*)</body>", RegexOptions.Singleline | RegexOptions.IgnoreCase);
            string fragment = body.Success ? body.Groups["b"].Value : html;

            int headerLength = string.Format(HeaderTemplate, 0, 0, 0, 0).Length; // all numbers have a fixed width
            int startHtml = headerLength;
            int startFragment = startHtml + Encoding.UTF8.GetByteCount(Pre);
            int endFragment = startFragment + Encoding.UTF8.GetByteCount(fragment);
            int endHtml = endFragment + Encoding.UTF8.GetByteCount(Post);

            return string.Format(HeaderTemplate, startHtml, endHtml, startFragment, endFragment) + Pre + fragment + Post;
        }

        /// <summary>The copied part (without header and document frame) of a CF_HTML string.</summary>
        public static string ExtractFragment(string cfHtml)
        {
            if (string.IsNullOrEmpty(cfHtml))
                return string.Empty;

            // offsets are byte positions in UTF-8
            var bytes = Encoding.UTF8.GetBytes(cfHtml);
            int start = ReadOffset(cfHtml, "StartFragment");
            int end = ReadOffset(cfHtml, "EndFragment");
            if (start >= 0 && end > start && end <= bytes.Length)
                return Encoding.UTF8.GetString(bytes, start, end - start);

            int a = cfHtml.IndexOf("<!--StartFragment-->", StringComparison.OrdinalIgnoreCase);
            int b = cfHtml.IndexOf("<!--EndFragment-->", StringComparison.OrdinalIgnoreCase);
            if (a >= 0 && b > a)
                return cfHtml.Substring(a + "<!--StartFragment-->".Length, b - a - "<!--StartFragment-->".Length);

            return cfHtml;
        }

        private static int ReadOffset(string cfHtml, string key)
        {
            var m = Regex.Match(cfHtml, @"^" + key + @":(\d+)", RegexOptions.Multiline);
            return m.Success && int.TryParse(m.Groups[1].Value, out int v) ? v : -1;
        }

        /// <summary>Very small HTML to text conversion for the plain text variant that goes with an HTML clipboard.</summary>
        public static string ToPlainText(string html)
        {
            string s = Regex.Replace(html, @"<(script|style)\b.*?</\1\s*>", "", RegexOptions.Singleline | RegexOptions.IgnoreCase);
            s = Regex.Replace(s, @"<br\s*/?>|</(p|div|li|tr|h[1-6]|table|ul|ol)\s*>", "\n", RegexOptions.IgnoreCase);
            s = Regex.Replace(s, @"</t[dh]\s*>", "\t", RegexOptions.IgnoreCase);
            s = Regex.Replace(s, @"<[^>]+>", "");
            s = WebUtility.HtmlDecode(s);
            s = Regex.Replace(s, @"[ \t]+\n", "\n");
            s = Regex.Replace(s, @"\n{3,}", "\n\n");
            return s.Trim().Replace("\n", "\r\n");
        }
    }
}
