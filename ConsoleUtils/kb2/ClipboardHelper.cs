using System;
using System.Text;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.ComponentModel;

namespace klemmbrett
{


    internal class ClipboardHelper
    {

        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GlobalUnlock(IntPtr hMem);
        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern UIntPtr GlobalSize(IntPtr hMem);

        [DllImport("kernel32.dll")]
        internal static extern IntPtr GlobalLock(IntPtr hMem);

        [DllImport("user32.dll")]
        static extern uint EnumClipboardFormats(uint format);

        [DllImport("user32.dll")]
        static extern bool OpenClipboard(IntPtr hWndNewOwner);
        [DllImport("user32.dll", SetLastError = true)]
        internal static extern IntPtr GetClipboardData(uint uFormat);
        [DllImport("user32.dll")]
        static extern bool CloseClipboard();

        [DllImport("user32.dll")]
        static extern int GetClipboardFormatName(uint format, [Out] StringBuilder lpszFormatName, int cchMaxCount);

        // https://docs.microsoft.com/de-de/windows/win32/dataxchg/standard-clipboard-formats

        public enum ClipboardFormats
        {
            CF_NULL = 0,
            CF_BITMAP = 2,
            CF_DIB = 8,
            CF_DIBV5 = 17,
            CF_DIF = 5,
            CF_DSPBITMAP = 0x0082,
            CF_DSPENHMETAFILE = 0x008E,
            CF_DSPMETAFILEPICT = 0x0083,
            CF_DSPTEXT = 0x0081,
            CF_ENHMETAFILE = 14,
            CF_GDIOBJFIRST = 0x0300,
            CF_GDIOBJLAST = 0x03FF,
            CF_HDROP = 15,
            CF_LOCALE = 16,
            CF_METAFILEPICT = 3,
            CF_OEMTEXT = 7,
            CF_OWNERDISPLAY = 0x0080,
            CF_PALETTE = 9,
            CF_PENDATA = 10,
            CF_PRIVATEFIRST = 0x0200,
            CF_PRIVATELAST = 0x02FF,
            CF_RIFF = 11,
            CF_SYLK = 4,
            CF_TEXT = 1,
            CF_TIFF = 6,
            CF_UNICODETEXT = 13,
            CF_WAVE = 12
        }

        public Dictionary<ClipboardFormats, string> DefaultClipboardDescriptions = new Dictionary<ClipboardFormats, string>
        {
            {ClipboardFormats.CF_NULL ,"null"},
            {ClipboardFormats.CF_BITMAP, "A handle to a bitmap (HBITMAP)." },
            {ClipboardFormats.CF_DIB, "A memory object containing a BITMAPINFO structure followed by the bitmap bits." },
            {ClipboardFormats.CF_DIBV5, "A memory object containing a BITMAPV5HEADER structure followed by the bitmap color space information and the bitmap bits." },
            {ClipboardFormats.CF_DIF, "Software Arts' Data Interchange Format." },
            {ClipboardFormats.CF_DSPBITMAP, "Bitmap display format associated with a private format. The hMem parameter must be a handle to data that can be displayed in bitmap format in lieu of the privately formatted data." },
            {ClipboardFormats.CF_DSPENHMETAFILE, "Enhanced metafile display format associated with a private format. The hMem parameter must be a handle to data that can be displayed in enhanced metafile format in lieu of the privately formatted data." },
            {ClipboardFormats.CF_DSPMETAFILEPICT, "Metafile-picture display format associated with a private format. The hMem parameter must be a handle to data that can be displayed in metafile-picture format in lieu of the privately formatted data." },
            {ClipboardFormats.CF_DSPTEXT, "Text display format associated with a private format. The hMem parameter must be a handle to data that can be displayed in text format in lieu of the privately formatted data." },
            {ClipboardFormats.CF_ENHMETAFILE, "A handle to an enhanced metafile (HENHMETAFILE)." },
            {ClipboardFormats.CF_GDIOBJFIRST, "Start of a range of integer values for application-defined GDI object clipboard formats. The end of the range is CF_GDIOBJLAST. Handles associated with clipboard formats in this range are not automatically deleted using the GlobalFree function when the clipboard is emptied.Also, when using values in this range, the hMem parameter is not a handle to a GDI object, but is a handle allocated by the GlobalAlloc function with the GMEM_MOVEABLE flag." },
            {ClipboardFormats.CF_GDIOBJLAST, "See CF_GDIOBJFIRST" },
            {ClipboardFormats.CF_HDROP, "A handle to type HDROP that identifies a list of files. An application can retrieve information about the files by passing the handle to the DragQueryFile function." },
            {ClipboardFormats.CF_LOCALE, "The data is a handle (HGLOBAL) to the locale identifier (LCID) associated with text in the clipboard. When you close the clipboard, if it contains CF_TEXT data but no CF_LOCALE data, the system automatically sets the CF_LOCALE format to the current input language. You can use the CF_LOCALE format to associate a different locale with the clipboard text. An application that pastes text from the clipboard can retrieve this format to determine which character set was used to generate the text. Note that the clipboard does not support plain text in multiple character sets. To achieve this, use a formatted text data type such as RTF instead. The system uses the code page associated with CF_LOCALE to implicitly convert from CF_TEXT to CF_UNICODETEXT. Therefore, the correct code page table is used for the conversion." },
            {ClipboardFormats.CF_METAFILEPICT, "Handle to a metafile picture format as defined by the METAFILEPICT structure. When passing a CF_METAFILEPICT handle by means of DDE, the application responsible for deleting hMem should also free the metafile referred to by the CF_METAFILEPICT handle." },
            {ClipboardFormats.CF_OEMTEXT, "Text format containing characters in the OEM character set. Each line ends with a carriage return/linefeed (CR-LF) combination. A null character signals the end of the data." },
            {ClipboardFormats.CF_OWNERDISPLAY, "Owner-display format. The clipboard owner must display and update the clipboard viewer window, and receive the WM_ASKCBFORMATNAME, WM_HSCROLLCLIPBOARD, WM_PAINTCLIPBOARD, WM_SIZECLIPBOARD, and WM_VSCROLLCLIPBOARD messages. The hMem parameter must be NULL." },
            {ClipboardFormats.CF_PALETTE, "Handle to a color palette. Whenever an application places data in the clipboard that depends on or assumes a color palette, it should place the palette on the clipboard as well. If the clipboard contains data in the CF_PALETTE (logical color palette) format, the application should use the SelectPalette and RealizePalette functions to realize (compare) any other data in the clipboard against that logical palette.When displaying clipboard data, the clipboard always uses as its current palette any object on the clipboard that is in the CF_PALETTE format." },
            {ClipboardFormats.CF_PENDATA, "Data for the pen extensions to the Microsoft Windows for Pen Computing." },
            {ClipboardFormats.CF_PRIVATEFIRST, "Start of a range of integer values for private clipboard formats. The range ends with CF_PRIVATELAST. Handles associated with private clipboard formats are not freed automatically; the clipboard owner must free such handles, typically in response to the WM_DESTROYCLIPBOARD message." },
            {ClipboardFormats.CF_PRIVATELAST, "See CF_PRIVATEFIRST." },
            {ClipboardFormats.CF_RIFF, "Represents audio data more complex than can be represented in a CF_WAVE standard wave format." },
            {ClipboardFormats.CF_SYLK, "Microsoft Symbolic Link (SYLK) format." },
            {ClipboardFormats.CF_TEXT, "Text format. Each line ends with a carriage return/linefeed (CR-LF) combination. A null character signals the end of the data. Use this format for ANSI text." },
            {ClipboardFormats.CF_TIFF, "Tagged-image file format." },
            {ClipboardFormats.CF_UNICODETEXT, "Unicode text format. Each line ends with a carriage return/linefeed (CR-LF) combination. A null character signals the end of the data." },
            {ClipboardFormats.CF_WAVE, "Represents audio data in one of the standard wave formats, such as 11 kHz or 22 kHz PCM." },
        };

        public static string GetClipboardFormatName(uint ClipboardFormat)
        {
            string result = GetDefaultClipboardFormatName(ClipboardFormat);
            if (result == null)
            {
                StringBuilder sb = new StringBuilder(512);
                GetClipboardFormatName(ClipboardFormat, sb, sb.Capacity);
                result = sb.ToString();
            }
            if (result.Contains("\0"))
                result = result.Substring(0, result.IndexOf("\0"));
            return result;
        }

        private static string GetDefaultClipboardFormatName(uint ClipboardFormat)
        {
            string format = Enum.GetName(typeof(ClipboardFormats), ClipboardFormat);
            return format;
        }

        internal static byte[] GetClipboardDataBytes(string format)
        {
            uint LastRetrievedFormat = 0;
            while (0 != (LastRetrievedFormat = EnumClipboardFormats(LastRetrievedFormat)))
            {
                if(GetClipboardFormatName(LastRetrievedFormat) == format)
                {
                    return GetClipboardDataBytes(LastRetrievedFormat);
                }
            }

            return null;
        }

        internal static byte[] GetClipboardDataBytes(uint format)
        {
            OpenClipboard(IntPtr.Zero);
            var dataPointer = GetClipboardDataPointer(format);
            if (dataPointer == IntPtr.Zero)
                return null;

            var length = GetPointerDataLength(dataPointer);
            if (length == UIntPtr.Zero)
            {
                return null;
            }

            var lockedMemory = GetLockedMemoryBlockPointer(dataPointer);
            try
            {
                if (lockedMemory == IntPtr.Zero)
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }

                var buffer = new byte[(int)length];
                Marshal.Copy(lockedMemory, buffer, 0, (int)length);

                return buffer;
            }
            finally
            {
                GlobalUnlock(dataPointer);
                CloseClipboard();
            }
        }

        static IntPtr GetClipboardDataPointer(uint format)
        {
            return GetClipboardData(format);
        }

        static UIntPtr GetPointerDataLength(IntPtr dataPointer)
        {
            return GlobalSize(dataPointer);
        }

        static IntPtr GetLockedMemoryBlockPointer(IntPtr dataPointer)
        {
            return GlobalLock(dataPointer);
        }

        public static uint[] GetClipboardFormats()
        {
            List<uint> result = new List<uint>();
            //OpenClipboard(Handle);
            OpenClipboard(IntPtr.Zero);


            uint LastRetrievedFormat = 0;
            while (0 != (LastRetrievedFormat = EnumClipboardFormats(LastRetrievedFormat)))
            {
                //string Description = "0x" + LastRetrievedFormat.ToString("X4") + ": " + GetClipboardFormatName(LastRetrievedFormat);
                result.Add(LastRetrievedFormat);

            }

            CloseClipboard();
            return result.ToArray();
        }

        public static void ListClipboardFormatsString()
        {
            //OpenClipboard(Handle);
            OpenClipboard(IntPtr.Zero);


            uint LastRetrievedFormat = 0;
            while (0 != (LastRetrievedFormat = EnumClipboardFormats(LastRetrievedFormat)))
            {
                string Description = "0x" + LastRetrievedFormat.ToString("X4") + ": " + GetClipboardFormatName(LastRetrievedFormat);
                Console.WriteLine(Description);
            }

            CloseClipboard();
        }

    }
}
