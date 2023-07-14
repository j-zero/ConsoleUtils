using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;


public class NetworkShareHelper
{
    #region External Calls
    [DllImport("Netapi32.dll", SetLastError = true)]
    static extern int NetApiBufferFree(IntPtr Buffer);
    [DllImport("Netapi32.dll", CharSet = CharSet.Unicode)]
    private static extern int NetShareEnum(
         StringBuilder ServerName,
         int level,
         ref IntPtr bufPtr,
         uint prefmaxlen,
         ref int entriesread,
         ref int totalentries,
         ref int resume_handle
         );
    #endregion
    #region External Structures
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct ShareInfo
    {
        public string Name;
        public uint Type;
        public string Description;
        public ShareInfo(string sharename, uint sharetype, string remark)
        {
            this.Name = sharename;
            this.Type = sharetype;
            this.Description = remark;
        }
        public override string ToString()
        {
            return Name;
        }
    }
    #endregion
    const uint MAX_PREFERRED_LENGTH = 0xFFFFFFFF;
    const int NERR_Success = 0;
    private enum NetError : uint
    {
        NERR_Success = 0,
        NERR_BASE = 2100,
        NERR_UnknownDevDir = (NERR_BASE + 16),
        NERR_DuplicateShare = (NERR_BASE + 18),
        NERR_BufTooSmall = (NERR_BASE + 23),
    }
    private enum SHARE_TYPE : uint
    {
        STYPE_DISKTREE = 0,
        STYPE_PRINTQ = 1,
        STYPE_DEVICE = 2,
        STYPE_IPC = 3,
        STYPE_SPECIAL = 0x80000000,
    }
    public static ShareInfo[] EnumNetShares(string Server)
    {
        List<ShareInfo> ShareInfos = new List<ShareInfo>();
        int entriesread = 0;
        int totalentries = 0;
        int resume_handle = 0;
        int nStructSize = Marshal.SizeOf(typeof(ShareInfo));
        IntPtr bufPtr = IntPtr.Zero;
        StringBuilder server = new StringBuilder(Server);
        int ret = NetShareEnum(server, 1, ref bufPtr, MAX_PREFERRED_LENGTH, ref entriesread, ref totalentries, ref resume_handle);
        if (ret == NERR_Success)
        {
            IntPtr currentPtr = bufPtr;
            for (int i = 0; i < entriesread; i++)
            {
                ShareInfo shi1 = (ShareInfo)Marshal.PtrToStructure(currentPtr, typeof(ShareInfo));
                ShareInfos.Add(shi1);
                currentPtr += nStructSize;
            }
            NetApiBufferFree(bufPtr);
            return ShareInfos.ToArray();
        }
        else
        {
            //ShareInfos.Add(new ShareInfo("ERROR=" + ret.ToString(), 10, string.Empty));
            throw new Exception(ret.ToString());
            //return ShareInfos.ToArray();
        }
    }
}
