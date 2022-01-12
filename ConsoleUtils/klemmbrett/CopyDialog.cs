using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace klemmbrett
{
    class CopyDialog
    {
        private enum FO_Func : uint
        {
            FO_COPY = 0x0002,
            FO_DELETE = 0x0003,
            FO_MOVE = 0x0001,
            FO_RENAME = 0x0004,
        }

        private struct SHFILEOPSTRUCT
        {
            public IntPtr hwnd;
            public FO_Func wFunc;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string pFrom;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string pTo;
            public ushort fFlags;
            public bool fAnyOperationsAborted;
            public IntPtr hNameMappings;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string lpszProgressTitle;

        }

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        static extern int SHFileOperation([In] ref SHFILEOPSTRUCT
           lpFileOp);

        private static SHFILEOPSTRUCT _ShFile;

        public static void CopyFiles(string sSource, string sTarget)
        {
            try
            {
                _ShFile.wFunc = FO_Func.FO_COPY;
                _ShFile.pFrom = sSource;
                _ShFile.pTo = sTarget;
                SHFileOperation(ref _ShFile);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}
