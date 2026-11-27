using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;

namespace klemmbrett
{
    /// <summary>One format that is currently on the clipboard.</summary>
    internal record ClipFormat(uint Id, string Name, long Size);

    /// <summary>
    /// Raw access to the clipboard through the Win32 API. Needed for everything that Windows Forms hides:
    /// the list of all formats, their sizes and the unmodified bytes of a format.
    /// </summary>
    internal static class NativeClipboard
    {
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool OpenClipboard(IntPtr hWndNewOwner);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CloseClipboard();

        [DllImport("user32.dll")]
        private static extern uint EnumClipboardFormats(uint format);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr GetClipboardData(uint uFormat);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetClipboardFormatName(uint format, [Out] StringBuilder lpszFormatName, int cchMaxCount);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GlobalLock(IntPtr hMem);

        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GlobalUnlock(IntPtr hMem);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern UIntPtr GlobalSize(IntPtr hMem);

        // https://learn.microsoft.com/windows/win32/dataxchg/standard-clipboard-formats
        private static readonly Dictionary<uint, string> StandardNames = new()
        {
            [1] = "CF_TEXT", [2] = "CF_BITMAP", [3] = "CF_METAFILEPICT", [4] = "CF_SYLK", [5] = "CF_DIF",
            [6] = "CF_TIFF", [7] = "CF_OEMTEXT", [8] = "CF_DIB", [9] = "CF_PALETTE", [10] = "CF_PENDATA",
            [11] = "CF_RIFF", [12] = "CF_WAVE", [13] = "CF_UNICODETEXT", [14] = "CF_ENHMETAFILE",
            [15] = "CF_HDROP", [16] = "CF_LOCALE", [17] = "CF_DIBV5",
            [0x80] = "CF_OWNERDISPLAY", [0x81] = "CF_DSPTEXT", [0x82] = "CF_DSPBITMAP",
            [0x83] = "CF_DSPMETAFILEPICT", [0x8E] = "CF_DSPENHMETAFILE",
        };

        // Formats whose handle is a GDI object and not a block of global memory – there is no byte size for them.
        private static bool IsMemoryFormat(uint id) =>
            id is not (2 or 9 or 14 or 0x82 or 0x8E) && !(id >= 0x0300 && id <= 0x03FF);

        public static string GetFormatName(uint id)
        {
            if (StandardNames.TryGetValue(id, out var name))
                return name;
            var sb = new StringBuilder(256);
            return GetClipboardFormatName(id, sb, sb.Capacity) > 0 ? sb.ToString() : $"0x{id:X4}";
        }

        /// <summary>Resolves "13", "0x0D", "CF_UNICODETEXT" or "HTML Format" to a format id that is on the clipboard.</summary>
        public static uint? FindFormat(string text)
        {
            text = text.Trim();
            var formats = List(false);

            uint id;
            bool numeric = text.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
                ? uint.TryParse(text[2..], System.Globalization.NumberStyles.HexNumber, null, out id)
                : uint.TryParse(text, out id);
            if (numeric)
                return formats.Any(f => f.Id == id) ? id : null;

            var hit = formats.FirstOrDefault(f => f.Name.Equals(text, StringComparison.OrdinalIgnoreCase))
                   ?? formats.FirstOrDefault(f => f.Name.Equals("CF_" + text, StringComparison.OrdinalIgnoreCase))
                   ?? formats.FirstOrDefault(f => f.Name.StartsWith(text, StringComparison.OrdinalIgnoreCase));
            return hit?.Id;
        }

        private static void Open()
        {
            // another program may hold the clipboard for a moment
            for (int i = 0; i < 10; i++)
            {
                if (OpenClipboard(IntPtr.Zero))
                    return;
                Thread.Sleep(25);
            }
            throw new Win32Exception(Marshal.GetLastWin32Error(), "The clipboard is locked by another program.");
        }

        public static List<ClipFormat> List(bool withSize)
        {
            var result = new List<ClipFormat>();
            Open();
            try
            {
                uint id = 0;
                while ((id = EnumClipboardFormats(id)) != 0)
                    result.Add(new ClipFormat(id, GetFormatName(id), withSize ? SizeOf(id) : -1));
            }
            finally
            {
                CloseClipboard();
            }
            return result;
        }

        private static long SizeOf(uint id)
        {
            if (!IsMemoryFormat(id))
                return -1;
            var handle = GetClipboardData(id);
            return handle == IntPtr.Zero ? -1 : (long)GlobalSize(handle);
        }

        /// <summary>The bytes of a format, or null if the format is missing or has no memory block (bitmap handles).</summary>
        public static byte[] GetBytes(uint id)
        {
            if (!IsMemoryFormat(id))
                return null;

            Open();
            try
            {
                var handle = GetClipboardData(id);
                if (handle == IntPtr.Zero)
                    return null;

                long size = (long)GlobalSize(handle);
                if (size <= 0)
                    return Array.Empty<byte>();

                var locked = GlobalLock(handle);
                if (locked == IntPtr.Zero)
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                try
                {
                    var buffer = new byte[size];
                    Marshal.Copy(locked, buffer, 0, buffer.Length);
                    return buffer;
                }
                finally
                {
                    GlobalUnlock(handle);
                }
            }
            finally
            {
                CloseClipboard();
            }
        }
    }
}
