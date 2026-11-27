using System;
using System.IO;
using System.Security.Principal;
using System.Security.AccessControl;
using System.Runtime.InteropServices;
using System.Text;
using System.ComponentModel;
using System.Globalization;
using System.Collections.Generic;
using System.Collections.Concurrent;
using NtfsDataStreams;
using System.Security.Cryptography.X509Certificates;

public class FilesystemEntryInfo
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

    private bool _isCertValid = false;


    public X509Certificate2 Certificate { get { return ReadCertificate();  } }
    public bool IsCertificateValid { get { ReadCertificate();  return _isCertValid; } }

    public bool IsDirectory { get; private set; }
    public bool IsFile { get; private set; }
    
    public bool IsShare { get;  set; }

    public string ShareDescription { get; set; }
    public string Server { get; set; }
    public uint ShareType { get; set; }
    public string ShareName { get; set; }

    public string Name { get; private set; }
    public string FullPath { get; private set; }
    public bool CanRead { get { return _CanRead(); } }
    public bool CanWrite { get { if (!_canWrite.HasValue) _canWrite = _HasWritePermission(this.FullPath); return _canWrite.Value; } }
    private bool? _canWrite;
    public bool Exists { get; private set; }
    public string Owner { get { return this._GetOwner(false); } }
    public bool CanReadSecurity { get {  return this.Owner != null ; } }
    public string ShortOwner { get { return this._GetOwner(true); } }
    public bool HasReadOnlyAttribute { get { return this.HasFlag(System.IO.FileAttributes.ReadOnly); } }
    public bool HasHiddenAttribute { get {
            if (this.IsShare && this.ShareName.EndsWith("$"))
                return true;
            return this.HasFlag(System.IO.FileAttributes.Hidden);
        } }
    public bool HasSystemAttribute { get { return this.HasFlag(System.IO.FileAttributes.System); } }
    public bool HasArchiveAttribute { get { return this.HasFlag(System.IO.FileAttributes.Archive); } }
    public bool IsEncrypted { get { return this.HasFlag(System.IO.FileAttributes.Encrypted); } }
    public bool IsLink { get { return this.HasFlag(System.IO.FileAttributes.ReparsePoint); } }
    public string ParentDirectory { get { return _parentDirectory; } }
    public string LinkTarget {  get
        {
            return !IsLink ? null : GetFinalPathName(this.FullPath).Replace(@"\\?\",""); 
        } }
    public string ColorString {  get { return _colorString ?? (_colorString = _GetColorString()); } }
    private string _colorString;
    public string BaseDirectory { 
        get {
            if (this.IsShare)
                return "\\\\" + this.Server + "\\";
            else if (this.IsDirectory || this.IsFile)
                return this._parentDirectory; // empty for root directories
            else
                return null;
        } 
    }

    public string Extension { get
        {
            if (this.IsFile)
                return _fileInfo.Extension;
            else
                return null;
        } 
    }

    public long Length
    {
        get
        {
            if (this.IsDirectory)
                return 0;
            else if (this.IsFile)
            {
                return this._fileInfo.Length;
            }
            return 0;
        }
    }

    public string HumanReadbleSize { get { return _humanReadbleSize; } }
    public string HumanReadbleSizeSuffix { get { return _humanReadbleSizeSuffix; } }
    public string HumanReadableLastWriteTime { get { return _formatDateTimeHumanReadable(_lastWriteTime); } }


    private X509Certificate2 ReadCertificate()
    {
            if (!File.Exists(this.FullPath))
            {
                //Console.WriteLine("File not found");
            }

            X509Certificate2 theCertificate;

            try
            {
                X509Certificate theSigner = X509Certificate.CreateFromSignedFile(this.FullPath);
                theCertificate = new X509Certificate2(theSigner);
            }
            catch
            {
            //Console.WriteLine("No digital signature found: " + ex.Message);
                return null;
            }

            bool chainIsValid = false;

            /*
             *
             * This section will check that the certificate is from a trusted authority IE
             * not self-signed.
             *
             */

            var theCertificateChain = new X509Chain();

            theCertificateChain.ChainPolicy.RevocationFlag = X509RevocationFlag.EndCertificateOnly;

            /*
             *
             * Using .Online here means that the validation WILL CALL OUT TO THE INTERNET
             * to check the revocation status of the certificate. Change to .Offline if you
             * don't want that to happen.
             */

            theCertificateChain.ChainPolicy.RevocationMode = X509RevocationMode.Offline;

            theCertificateChain.ChainPolicy.UrlRetrievalTimeout = new TimeSpan(0, 1, 0);

            theCertificateChain.ChainPolicy.VerificationFlags = X509VerificationFlags.NoFlag;

            chainIsValid = theCertificateChain.Build(theCertificate);

            this._isCertValid = chainIsValid;

            //Console.WriteLine("Publisher Information : " + theCertificate.SubjectName.Name);
            //Console.WriteLine("Valid From: " + theCertificate.GetEffectiveDateString());
            //Console.WriteLine("Valid To: " + theCertificate.GetExpirationDateString());
            //Console.WriteLine("Issued By: " + theCertificate.Issuer);
            /*
            if (chainIsValid)
            {
                
            }
            else
            {

                //Console.WriteLine("Chain Not Valid (certificate is self-signed)");
            }
            */
            return theCertificate;
        
    }

    public DateTime CreationTime { get; private set; }
    public DateTime LastAccessTime { get; private set; }
    public DateTime LastWriteTime { get; private set; }

    public FileTypes FileType
    {
        get
        {
            if (this.IsDirectory)
                return FileTypes.Directory;
            if (!_fileType.HasValue)
                _fileType = FileDefinitions.GetFileTypeByExtension(this.Extension);
            return _fileType.Value;
        }
    }
    private FileTypes? _fileType;

    public IEnumerable<NtfsDataStreams.FileDataStream> AlternateDataStreams
    {
        get
        {
            if (this.IsDirectory)
                return null;
            else if (this.IsFile)
                return  (new FileInfo(this.FullPath).GetDataStreams());
            else
                return null;
        }
    }

    public string MIMEType
    {
        get
        {
            return MIMEHelper.GetMIMEType(this.FullPath);
        }
    }
    public string Encoding
    {
        get
        {
            return MIMEHelper.GetEncoding(this.FullPath);
        }
    }
    public string FileTypeDescription
    {
        get
        {
            if (this.IsShare)
                return this.ShareDescription;
            return MIMEHelper.GetDescription(this.FullPath);
        }
    }
    public string ShouldBeExtension
    {
        get
        {
            return MIMEHelper.GetExtension(this.FullPath);
        }
    }

    public Exception LastException { get; set; }
    public bool Error { get; set; }

    private string _longOwner = null;

    //public bool HasWritePermissions { get { return HasWritePermission(this.FullPath); } }

    private FileInfo _fileInfo;
    private DirectoryInfo _directoryInfo;
    private string _humanReadbleSizeSuffix;
    private string _humanReadbleSize = string.Empty;
    private DateTime _lastWriteTime = DateTime.MinValue;
    private string _parentDirectory = string.Empty;
    //private string _humanReadableLastAccessTime = String.Empty;

    private static bool _cultureInitialized = false;

    private static void InitCulture()
    {
        if (_cultureInitialized)
            return;
        System.Globalization.CultureInfo customCulture = (System.Globalization.CultureInfo)System.Threading.Thread.CurrentThread.CurrentCulture.Clone();
        customCulture.NumberFormat.NumberDecimalSeparator = ".";
        System.Threading.Thread.CurrentThread.CurrentCulture = customCulture;
        _cultureInitialized = true;
    }

    /// <summary>
    /// Creates an entry from an already queried FileSystemInfo (e.g. from an enumeration),
    /// which avoids additional File.Exists/Directory.Exists calls.
    /// </summary>
    public FilesystemEntryInfo(FileSystemInfo info)
    {
        InitCulture();

        _attributes = info.Attributes;
        this.Exists = true;
        this.Name = info.Name;
        this.FullPath = info.FullName;
        _lastWriteTime = info.LastWriteTime;
        this.CreationTime = info.CreationTime;
        this.LastAccessTime = info.LastAccessTime;
        this.LastWriteTime = _lastWriteTime;

        if ((_attributes & System.IO.FileAttributes.Directory) != 0)
        {
            _directoryInfo = info as DirectoryInfo ?? new DirectoryInfo(info.FullName);
            this.IsDirectory = true;
            if (_directoryInfo.Parent != null)
                this._parentDirectory = _directoryInfo.Parent.FullName;
        }
        else
        {
            _fileInfo = info as FileInfo ?? new FileInfo(info.FullName);
            this.IsFile = true;
            CalculateHumanReadableSize(_fileInfo.Length);
            if (_fileInfo.DirectoryName != null)
                this._parentDirectory = _fileInfo.DirectoryName;
        }
    }

    private System.IO.FileAttributes _attributes;

    public FilesystemEntryInfo(string path)
    {
        InitCulture();

       

        if (File.Exists(path))
        {
            _fileInfo = new FileInfo(path);
            _attributes = _fileInfo.Attributes;
            this.Exists = true;
            this.IsFile = true;
            this.Name = _fileInfo.Name;
            _lastWriteTime = _fileInfo.LastWriteTime;
            this.CreationTime = _fileInfo.CreationTime;
            this.LastAccessTime = _fileInfo.LastAccessTime;
            this.LastWriteTime = _fileInfo.LastWriteTime;

            CalculateHumanReadableSize(_fileInfo.Length);
            this._parentDirectory = _fileInfo.Directory.FullName;
            //this.Owner = System.IO.File.GetAccessControl(path).GetOwner(typeof(System.Security.Principal.NTAccount)).ToString();
            //this.ReadOnly = _fileInfo.Attributes.HasFlag(System.IO.FileAttributes.ReadOnly);

        }
        else if (Directory.Exists(path))
        {
            _directoryInfo = new DirectoryInfo(path);
            _attributes = _directoryInfo.Attributes;
            this.Exists = true;
            this.IsDirectory = true;
            this.Name = _directoryInfo.Name;
            _lastWriteTime = _directoryInfo.LastWriteTime;
            this.CreationTime = _directoryInfo.CreationTime;
            this.LastAccessTime = _directoryInfo.LastAccessTime;
            this.LastWriteTime = _directoryInfo.LastWriteTime;
            if (_directoryInfo.Parent != null)
                this._parentDirectory = _directoryInfo.Parent.FullName;
            //this.Owner = System.IO.Directory.GetAccessControl(path).GetOwner(typeof(System.Security.Principal.NTAccount)).ToString();
            //this.ReadOnly = _directoryInfo.Attributes.HasFlag(System.IO.FileAttributes.ReadOnly);
            //this.Hidden = _directoryInfo.Attributes.HasFlag(System.IO.FileAttributes.Hidden);
            //this.System = _directoryInfo.Attributes.HasFlag(System.IO.FileAttributes.System);
        }
        else if (this.IsShare)
        {
            this.Name = this.ShareName;
            this.Exists = true;
        }


        if (Exists)
        {
            this.FullPath = Path.GetFullPath(path);
        }
    }

    public FilesystemEntryInfo(string server, string share, string shareDescription, uint shareType)
    {
        InitCulture();

        this.ShareName = share;
        this.IsShare = true;
        this.Name = this.ShareName;
        this.Server = server;
        this.ShareDescription = shareDescription;
        this.ShareType = shareType;
        this.Exists = true;
        this.FullPath = Path.GetFullPath("\\\\" + server + "\\" + share);
        this._parentDirectory = "\\\\" + server + "\\";
        

    }


    //private static readonly string[] _monthNames = { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Okt", "Nov", "Dec" };

    private string _formatDateTimeHumanReadable(DateTime timestamp)
    {
        if (timestamp.Ticks == 0)
            return "-";
        ;
       // string result = "";
        string field1 = "";
        string field2 = "";

        //field1 = timestamp.Day.ToString().PadLeft(2) + " " + _monthNames[timestamp.Month-1];
        field1 = timestamp.Day.ToString().PadLeft(2) + " " + CultureInfo.InvariantCulture.DateTimeFormat.GetAbbreviatedMonthName(timestamp.Month);

        if (timestamp.Year < DateTime.Now.Year)
        {
            field2 = timestamp.Year.ToString().PadLeft(5);
        }
        else
        {
            field2 = timestamp.ToShortTimeString();
        }

        return $"{field1} {field2}";

    }

    public bool HasFlag(Enum flag)
    {
        if (this.IsFile || (this.IsDirectory && !this.IsShare))
            return _attributes.HasFlag(flag);
        return false;
    }

    private string _shortOwner = null;

    // SID -> account name cache, most files in a directory share the same few owners
    private static readonly ConcurrentDictionary<string, string> _ownerNameCache = new ConcurrentDictionary<string, string>();

    private static string _ResolveOwnerName(SecurityIdentifier sid)
    {
        string key = sid.Value;
        string name;
        if (_ownerNameCache.TryGetValue(key, out name))
            return name;

        try
        {
            name = sid.Translate(typeof(NTAccount)).ToString();
        }
        catch (Exception) // not mappable (deleted account, offline domain, ...)
        {
            name = string.Empty;
        }
        _ownerNameCache.TryAdd(key, name);
        return name;
    }

    private FileSystemSecurity _security;
    private bool _securityLoaded;

    // owner + access rules in a single call (the ACL read is the most expensive part of a long listing)
    private FileSystemSecurity _GetSecurity()
    {
        if (!_securityLoaded)
        {
            _securityLoaded = true;
            const AccessControlSections sections = AccessControlSections.Owner | AccessControlSections.Access;
            if (this.IsFile)
                _security = new FileInfo(this.FullPath).GetAccessControl(sections);
            else if (this.IsDirectory || this.IsShare)
                _security = new DirectoryInfo(this.FullPath).GetAccessControl(sections);
        }
        return _security;
    }

    /// <summary>Reads owner and write permission, can be called from several threads for different entries.</summary>
    public void Prefetch()
    {
        _ReadOwner();
        var ignore = this.CanWrite;
    }

    private void _ReadOwner()
    {
        if (this._longOwner != null)
            return;

        string longOwner = string.Empty;

        try
        {
            // only read the owner section, reading the complete ACL is much slower
            var security = _GetSecurity();
            var sid = security?.GetOwner(typeof(SecurityIdentifier)) as SecurityIdentifier;
            if (sid != null)
                longOwner = _ResolveOwnerName(sid);
        }
        catch (Exception ex)
        {
            this.LastException = ex;
        }

        this._longOwner = longOwner;
        // owner may be without domain (e.g. "Everyone")
        int sep = longOwner.IndexOf('\\');
        this._shortOwner = sep >= 0 ? longOwner.Substring(sep + 1) : longOwner;
    }

    private string _GetOwner(bool ShortOwner)
    {
        _ReadOwner();
        return ShortOwner ? _shortOwner : _longOwner;
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
                string ignore = unAuthEx.Message;
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
                //FileSystemSecurity security;
                return true;
            }
            catch
            {
                return false;
            }
        }
        return false;
    }
    private static WindowsPrincipal _currentUser;
    private static readonly ConcurrentDictionary<string, bool> _writeCache = new ConcurrentDictionary<string, bool>();
    private static readonly ConcurrentDictionary<SecurityIdentifier, bool> _inRoleCache = new ConcurrentDictionary<SecurityIdentifier, bool>();
    private static readonly object _principalLock = new object();

    private static bool _IsCurrentUserInRole(SecurityIdentifier sid)
    {
        bool inRole;
        if (_inRoleCache.TryGetValue(sid, out inRole))
            return inRole;

        lock (_principalLock)
        {
            var currentuser = _currentUser ?? (_currentUser = new WindowsPrincipal(WindowsIdentity.GetCurrent()));
            try
            {
                inRole = currentuser.IsInRole(sid);
            }
            catch (Exception)
            {
                inRole = false;
            }
        }
        _inRoleCache.TryAdd(sid, inRole);
        return inRole;
    }

    private bool _HasWritePermission(string FilePath)
    {
        try
        {
            FileSystemSecurity security;
            if (this.IsFile)
            {
                try { security = _GetSecurity(); }
                catch (Exception) { return false; }
                if (security == null)
                    return false;
            }
            else if (File.Exists(FilePath))
            {
                security = new FileInfo(FilePath).GetAccessControl(AccessControlSections.Access);
            }
            else
            {
                security = new DirectoryInfo(Path.GetDirectoryName(FilePath)).GetAccessControl(AccessControlSections.Access);
            }

            // files inheriting the same ACL share the result
            string sddl = security.GetSecurityDescriptorSddlForm(AccessControlSections.Access);
            bool cached;
            if (_writeCache.TryGetValue(sddl, out cached))
                return cached;

            // SIDs instead of NTAccount: no (slow) account name lookup for every rule
            var rules = security.GetAccessRules(true, true, typeof(SecurityIdentifier));
            bool result = false;
            bool denied = false;
            foreach (FileSystemAccessRule rule in rules)
            {
                if (0 == (rule.FileSystemRights &
                    (FileSystemRights.WriteData | FileSystemRights.Write)))
                {
                    continue;
                }

                if (!_IsCurrentUserInRole((SecurityIdentifier)rule.IdentityReference))
                    continue;

                if (rule.AccessControlType == AccessControlType.Deny)
                {
                    denied = true;
                    break;
                }
                if (rule.AccessControlType == AccessControlType.Allow)
                    result = true;
            }

            result = result && !denied;
            _writeCache.TryAdd(sddl, result);
            return result;
        }
        catch
        {
            return false;
        }
    }

    public string GetRelativePath(string referencePath)
    {
        return _getRelativePath(this.FullPath, referencePath);
    }

    public string GetRelativeParent(string referencePath)
    {
        //return _getRelativePath(this.ParentDirectory, referencePath);
        return _PathDifference(this.ParentDirectory, referencePath, true);
    }

    public string GetRelativeParent(string referencePath, bool onlyDown)
    {
        return _PathDifference(this.ParentDirectory, referencePath, onlyDown);
    }

    private string _getRelativePath(string path, string referencePath)
    {
        string _path = path;
        if (!_path.EndsWith(Path.DirectorySeparatorChar.ToString()))
            _path = _path + Path.DirectorySeparatorChar;

        var fileUri = new Uri(_path);
        var referenceUri = new Uri(referencePath);
        return Uri.UnescapeDataString(referenceUri.MakeRelativeUri(fileUri).ToString()).Replace('/', Path.DirectorySeparatorChar);
    }

    private static string _PathDifference(string path2, string path1, bool onlyDown)
    {
        int c = 0;  //index up to which the paths are the same
        int d = -1; //index of trailing slash for the portion where the paths are the same

        while (c < path1.Length && c < path2.Length)
        {
            if (char.ToLowerInvariant(path1[c]) != char.ToLowerInvariant(path2[c]))
            {
                break;
            }

            if (path1[c] == '\\')
            {
                d = c;
            }

            c++;
        }

        if (c == 0)
        {
            return path2;
        }

        if (c == path1.Length && c == path2.Length)
        {
            return string.Empty;
        }


        System.Text.StringBuilder builder = new System.Text.StringBuilder();

        while (c < path1.Length)
        {
            if (path1[c] == '\\')
            {
                if (onlyDown)
                    return path2;
                builder.Append(@"..\");
                
            }
            c++;
        }

        if (builder.Length == 0 && path2.Length - 1 == d)
        {
            return @".\";
        }

        //return builder.ToString() + path2.Substring(d + 1);
        return builder.ToString() + path2;
    }

    private string _GetColorString()
    {
        string result = ColorTheme.File; // default color
        if (this.IsLink)
            return ColorTheme.Symlink;
        if (this.IsDirectory)
            return ColorTheme.Directory;
        if (this.IsShare && this.HasHiddenAttribute)
            return "#" + ColorTheme.DarkColor;
        if (this.IsShare)
            return ColorTheme.Directory;
        if (this.IsFile) {
            string color = ColorTheme.GetColorByExtension(this.Extension);
            return color != null ? color : result;
            ;
        }

        return result;
    }


    // based on https://stackoverflow.com/a/14488941

    private void CalculateHumanReadableSize(Int64 value, int factor = 1024, int decimalPlaces = 1)
    {
        (string size, string suffix) = UnitHelper.GetHumanReadableSize(value, factor, decimalPlaces);
        this._humanReadbleSize = size;
        this._humanReadbleSizeSuffix = suffix;
    }





    private static readonly string[] SizeSuffixes =
        { "", "k", "M", "G", "T", "P", "E", "Z", "Y" };



    private static readonly string[] SizeSuffixesIbi =
        { "B", "KiB", "MiB", "GiB", "TiB", "PiB", "EiB", "ZiB", "YiB" };

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

