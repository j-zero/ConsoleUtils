using System;
using System.IO;
using System.Security.Principal;
using System.Security.AccessControl;
using System.Runtime.InteropServices;
using System.Text;
using System.ComponentModel;

namespace list
{
    internal class EntryInfo
    {
        /*
        enum FileCategories
        {
            Directory,
            Executable,
            File,
            Symlink,
            Unknown
        }
        */


        public bool IsDirectory { get; private set; }
        public bool IsFile { get; private set; }
        public string Name { get; private set; }
        public string FullPath { get; private set; }
        public bool CanRead { get { return _CanRead(); } }
        public bool CanWrite { get { return _HasWritePermission(this.FullPath); } }
        public bool Exists { get; private set; }
        public string Owner { get { return this._GetOwner(); } }
        public bool HasReadOnlyAttribute { get { return this.HasFlag(System.IO.FileAttributes.ReadOnly); } }
        public bool HasHiddenAttribute { get { return this.HasFlag(System.IO.FileAttributes.Hidden); } }
        public bool HasSystemAttribute { get { return this.HasFlag(System.IO.FileAttributes.System); } }
        public bool HasArchiveAttribute { get { return this.HasFlag(System.IO.FileAttributes.Archive); } }
        public bool IsEncrypted { get { return this.HasFlag(System.IO.FileAttributes.Encrypted); } }
        public bool IsLink { get { return this.HasFlag(System.IO.FileAttributes.ReparsePoint); } }
        public string LinkTarget {  get
            {
                return !IsLink ? null : GetFinalPathName(this.FullPath).Replace(@"\\?\",""); 
            } }
        public string ColorString {  get { return _GetColorString(); } }

        public string Extension { get
            {
                if (this.IsFile)
                    return _fileInfo.Extension;
                else
                    return null;
            } }
        //public bool HasWritePermissions { get { return HasWritePermission(this.FullPath); } }

        private FileInfo _fileInfo;
        private DirectoryInfo _directoryInfo;
        public EntryInfo(string path)
        {
            if (File.Exists(path))
            {
                _fileInfo = new FileInfo(path);
                this.Exists = true;
                this.IsFile = true;
                this.Name = _fileInfo.Name;
                
                //this.Owner = System.IO.File.GetAccessControl(path).GetOwner(typeof(System.Security.Principal.NTAccount)).ToString();
                //this.ReadOnly = _fileInfo.Attributes.HasFlag(System.IO.FileAttributes.ReadOnly);

            }
            else if (Directory.Exists(path))
            {
                _directoryInfo = new DirectoryInfo(path);
                this.Exists = true;
                this.IsDirectory = true;
                this.Name = _directoryInfo.Name;
                //this.Owner = System.IO.Directory.GetAccessControl(path).GetOwner(typeof(System.Security.Principal.NTAccount)).ToString();
                //this.ReadOnly = _directoryInfo.Attributes.HasFlag(System.IO.FileAttributes.ReadOnly);
                //this.Hidden = _directoryInfo.Attributes.HasFlag(System.IO.FileAttributes.Hidden);
                //this.System = _directoryInfo.Attributes.HasFlag(System.IO.FileAttributes.System);
            }


            if (Exists)
            {
                this.FullPath = Path.GetFullPath(path);
            }
        }

        public bool HasFlag(Enum flag)
        {
            if(this.IsFile)
                return _fileInfo.Attributes.HasFlag(flag);
            else if(this.IsDirectory)
                return _directoryInfo.Attributes.HasFlag(flag);
            return false;
        }

        private string _GetOwner()
        {
            if (this.IsFile)
                return System.IO.File.GetAccessControl(this.FullPath).GetOwner(typeof(System.Security.Principal.NTAccount)).ToString();
            else if (this.IsDirectory)
                return System.IO.Directory.GetAccessControl(this.FullPath).GetOwner(typeof(System.Security.Principal.NTAccount)).ToString();
            return null;
        }
        private bool _CanRead()
        {
            if (this.IsFile)
            {
                try
                {
                    using (var fs = File.Open(this.FullPath, FileMode.Open))
                    {
                        return fs.CanRead;
                    }
                }
                catch(UnauthorizedAccessException unAuthEx)
                {
                    return false;
                }
                catch
                {
                    return false;
                }
            }
            else if (this.IsDirectory)
            {
                try
                {
                    Directory.GetDirectories(this.FullPath);
                    return true;
                }
                catch
                {
                    return false;
                }
            }
            return false;
        }
        private bool _HasWritePermission(string FilePath)
        {
            try
            {
                FileSystemSecurity security;
                if (File.Exists(FilePath))
                {
                    security = File.GetAccessControl(FilePath);
                }
                else
                {
                    security = Directory.GetAccessControl(Path.GetDirectoryName(FilePath));
                }
                var rules = security.GetAccessRules(true, true, typeof(NTAccount));

                var currentuser = new WindowsPrincipal(WindowsIdentity.GetCurrent());
                bool result = false;
                foreach (FileSystemAccessRule rule in rules)
                {
                    if (0 == (rule.FileSystemRights &
                        (FileSystemRights.WriteData | FileSystemRights.Write)))
                    {
                        continue;
                    }

                    if (rule.IdentityReference.Value.StartsWith("S-1-"))
                    {
                        var sid = new SecurityIdentifier(rule.IdentityReference.Value);
                        if (!currentuser.IsInRole(sid))
                        {
                            continue;
                        }
                    }
                    else
                    {
                        if (!currentuser.IsInRole(rule.IdentityReference.Value))
                        {
                            continue;
                        }
                    }

                    if (rule.AccessControlType == AccessControlType.Deny)
                        return false;
                    if (rule.AccessControlType == AccessControlType.Allow)
                        result = true;
                }
                return result;
            }
            catch
            {
                return false;
            }
        }
        private string _GetColorString()
        {
            string result = ColorTheme.File; // default color
            if (this.IsLink)
                return ColorTheme.Symlink;
            if (this.IsDirectory)
                return ColorTheme.Directory;
            if (this.IsFile) {
                string color = ColorTheme.GetColorByExtension(this.Extension);
                return color != null ? color : result;
                ;
            }

            return result;
        }



        private static readonly IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);

        private const uint FILE_READ_EA = 0x0008;
        private const uint FILE_FLAG_BACKUP_SEMANTICS = 0x2000000;

        [DllImport("Kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern uint GetFinalPathNameByHandle(IntPtr hFile, [MarshalAs(UnmanagedType.LPTStr)] StringBuilder lpszFilePath, uint cchFilePath, uint dwFlags);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr CreateFile(
                [MarshalAs(UnmanagedType.LPTStr)] string filename,
                [MarshalAs(UnmanagedType.U4)] uint access,
                [MarshalAs(UnmanagedType.U4)] FileShare share,
                IntPtr securityAttributes, // optional SECURITY_ATTRIBUTES struct or IntPtr.Zero
                [MarshalAs(UnmanagedType.U4)] FileMode creationDisposition,
                [MarshalAs(UnmanagedType.U4)] uint flagsAndAttributes,
                IntPtr templateFile);

        public static string GetFinalPathName(string path)
        {
            var h = CreateFile(path,
                FILE_READ_EA,
                FileShare.ReadWrite | FileShare.Delete,
                IntPtr.Zero,
                FileMode.Open,
                FILE_FLAG_BACKUP_SEMANTICS,
                IntPtr.Zero);
            if (h == INVALID_HANDLE_VALUE)
                throw new Win32Exception();

            try
            {
                var sb = new StringBuilder(1024);
                var res = GetFinalPathNameByHandle(h, sb, 1024, 0);
                if (res == 0)
                    throw new Win32Exception();

                return sb.ToString();
            }
            finally
            {
                CloseHandle(h);
            }
        }
    }
}
