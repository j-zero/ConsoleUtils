using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace klemmbrett
{
    /// <summary>Typed access to the clipboard (text, HTML, RTF, CSV, image, files). Must be called from an STA thread.</summary>
    internal static class Clip
    {
        public const string PngFormat = "PNG";
        private const string DropEffectFormat = "Preferred DropEffect";
        private const int DropEffectCopy = 1, DropEffectMove = 2;

        // another program may hold the clipboard for a short time
        private static T Retry<T>(Func<T> action)
        {
            for (int attempt = 0; ; attempt++)
            {
                try
                {
                    return action();
                }
                catch (ExternalException) when (attempt < 9)
                {
                    Thread.Sleep(50);
                }
            }
        }

        private static void Retry(Action action) => Retry<object>(() => { action(); return null; });

        private static void Set(DataObject data) => Retry(() => Clipboard.SetDataObject(data, true, 10, 50));

        // ---- queries -------------------------------------------------------------------------

        public static bool HasText => Retry(() => Clipboard.ContainsText(TextDataFormat.UnicodeText) || Clipboard.ContainsText(TextDataFormat.Text));
        public static bool HasHtml => Retry(() => Clipboard.ContainsText(TextDataFormat.Html));
        public static bool HasRtf => Retry(() => Clipboard.ContainsText(TextDataFormat.Rtf));
        public static bool HasCsv => Retry(() => Clipboard.ContainsText(TextDataFormat.CommaSeparatedValue));
        public static bool HasImage => Retry(() => Clipboard.ContainsImage());
        public static bool HasFiles => Retry(() => Clipboard.ContainsFileDropList());
        public static bool IsEmpty => NativeClipboard.List(false).Count == 0;

        // ---- text ----------------------------------------------------------------------------

        public static string GetText() => Retry(() => Clipboard.GetText(TextDataFormat.UnicodeText));
        public static string GetRtf() => Retry(() => Clipboard.GetText(TextDataFormat.Rtf));
        public static string GetCsv() => Retry(() => Clipboard.GetText(TextDataFormat.CommaSeparatedValue));

        public static void SetText(string text)
        {
            var data = new DataObject();
            data.SetText(text, TextDataFormat.UnicodeText);
            Set(data);
        }

        public static void SetRtf(string rtf)
        {
            var data = new DataObject();
            data.SetText(rtf, TextDataFormat.Rtf);
            Set(data);
        }

        public static void SetCsv(string csv)
        {
            var data = new DataObject();
            data.SetText(csv, TextDataFormat.CommaSeparatedValue);
            data.SetText(csv, TextDataFormat.UnicodeText);
            Set(data);
        }

        // ---- html ----------------------------------------------------------------------------

        /// <summary>The copied HTML without the CF_HTML header. Read as raw bytes because the header offsets are UTF-8 positions.</summary>
        public static string GetHtmlFragment() => HtmlFormat.ExtractFragment(GetHtmlRaw());

        public static string GetHtmlRaw()
        {
            uint? id = NativeClipboard.FindFormat("HTML Format");
            var bytes = id.HasValue ? NativeClipboard.GetBytes(id.Value) : null;
            return bytes == null ? string.Empty : Encoding.UTF8.GetString(bytes).TrimEnd('\0');
        }

        public static void SetHtml(string html)
        {
            var data = new DataObject();
            data.SetText(HtmlFormat.Build(html), TextDataFormat.Html);
            data.SetText(HtmlFormat.ToPlainText(html), TextDataFormat.UnicodeText); // programs without HTML support get the text
            Set(data);
        }

        // ---- image ---------------------------------------------------------------------------

        /// <summary>The image of the clipboard, PNG (with transparency) if the source provides one.</summary>
        public static Image GetImage()
        {
            uint? png = NativeClipboard.FindFormat(PngFormat);
            var bytes = png.HasValue ? NativeClipboard.GetBytes(png.Value) : null;
            if (bytes != null && bytes.Length > 0)
            {
                try
                {
                    using var ms = new MemoryStream(bytes);
                    using var img = Image.FromStream(ms);
                    return new Bitmap(img); // detached from the stream
                }
                catch (ArgumentException)
                {
                    // damaged PNG, use the bitmap below
                }
            }
            return Retry(() => Clipboard.GetImage());
        }

        public static void SetImage(Image image)
        {
            var data = new DataObject();
            using var png = new MemoryStream();
            image.Save(png, ImageFormat.Png);
            data.SetData(PngFormat, false, new MemoryStream(png.ToArray()));
            data.SetImage(image);
            Set(data);
        }

        // ---- files ---------------------------------------------------------------------------

        public static List<string> GetFiles(out bool cut)
        {
            cut = false;
            if (!HasFiles)
                return new List<string>();

            var list = Retry(() => Clipboard.GetFileDropList()).Cast<string>().ToList();

            uint? id = NativeClipboard.FindFormat(DropEffectFormat);
            var bytes = id.HasValue ? NativeClipboard.GetBytes(id.Value) : null;
            if (bytes != null && bytes.Length >= 4)
                cut = (BitConverter.ToInt32(bytes, 0) & DropEffectMove) != 0 && (BitConverter.ToInt32(bytes, 0) & DropEffectCopy) == 0;
            return list;
        }

        /// <summary>Puts files and directories on the clipboard like Explorer does (copy or cut).</summary>
        public static void SetFiles(IEnumerable<string> paths, bool cut)
        {
            var list = new System.Collections.Specialized.StringCollection();
            list.AddRange(paths.ToArray());

            var data = new DataObject();
            data.SetFileDropList(list);
            data.SetData(DropEffectFormat, false, new MemoryStream(BitConverter.GetBytes(cut ? DropEffectMove : DropEffectCopy)));
            Set(data);
        }

        // ---- misc ----------------------------------------------------------------------------

        public static void Clear() => Retry(() => Clipboard.Clear());
    }
}
