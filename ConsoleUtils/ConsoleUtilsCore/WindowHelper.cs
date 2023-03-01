using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

public class WindowHelper
{
    private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
    private static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2);
    private const UInt32 SWP_NOSIZE = 0x0001;
    private const UInt32 SWP_NOMOVE = 0x0002;
    private const UInt32 TOPMOST_FLAGS = SWP_NOMOVE | SWP_NOSIZE;

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool AttachConsole(uint dwProcessId);
    [DllImport("kernel32.dll")]
    static extern IntPtr GetConsoleWindow();
    [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
    static extern bool FreeConsole();

    [DllImport("user32.dll", ExactSpelling = true, CharSet = CharSet.Auto)]
    static extern IntPtr GetParent(IntPtr hWnd);

    public IntPtr GetMainWindow(IntPtr handle)
    {
        IntPtr windowParent = IntPtr.Zero;
        while (handle != IntPtr.Zero)
        {
            windowParent = handle;
            handle = GetParent(handle);
        }
        return windowParent;
    }

    public static void SetWindowTopMost(IntPtr Handle, bool TopMost)
    {
        SetWindowPos(Handle, TopMost ? HWND_TOPMOST : HWND_NOTOPMOST, 0, 0, 0, 0, TOPMOST_FLAGS);
    }
    public static void SetCurrentWindowTopMost(bool TopMost)
    {
        IntPtr handle = GetParent(GetConsoleWindow());
        SetWindowTopMost(handle, TopMost);
    }
}

