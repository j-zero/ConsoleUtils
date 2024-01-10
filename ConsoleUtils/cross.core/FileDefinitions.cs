public enum FileTypes
{
    Directory,
    Executable,
    Libraries,
    Config,
    Script,
    Sourcecode,
    Image,
    Video,
    Document,
    Music,
    Archive,
    Cryptography,
    Immediate,
    Temp,
    OtherKnown,
    Unknown
}

public class FileDefinitions
{

    private static readonly Dictionary<FileTypes, string[]> FileTypeExtensions = new Dictionary<FileTypes, string[]>()
    { 
        {FileTypes.Executable, new string[] {".exe",".com",".pif",".cmd",".bat",".ps1"}},
        {FileTypes.Libraries, new string[] {".dll",".sys"}},
        {FileTypes.Config, new string[] {".ini",".inf", ".cfg", ".conf"}},
        {FileTypes.Script, new string[] {".py",".php",".js",".perl", ".sh"} },
        {FileTypes.Sourcecode, new string[] {".c",".cpp",".cs" } },
        {FileTypes.Image, new string[] { ".arw", ".bmp", ".cbr", ".cbz", ".cr2", ".dvi", ".eps", ".gif", ".heif", ".ico", ".jpeg", ".jpg", ".nef", ".orf", ".pbm", ".pgm", ".png", ".pnm", ".ppm", ".ps", ".raw", ".stl", ".svg", ".tif", ".tiff", ".webp", ".xpm" } },
        {FileTypes.Video, new string[]    {".avi",".flv",".heic",".m2ts",".m2v",".mkv",".mov",".mp4",".mpeg",".mpg",".ogm",".ogv",".ts",".vob",".webm",".wmv"}},
        {FileTypes.Document, new string[] { ".djvu",".doc",".docx",".dvi",".eml",".eps",".fotd",".key",".odp",".odt",".pdf",".ppt",".pptx",".rtf",".xls",".xlsx" }},
        {FileTypes.Music, new string[] {".aac",".alac",".ape",".flac",".m4a",".mka",".mp3",".ogg",".opus",".wav",".wma"}},
        {FileTypes.Archive, new string[] { ".7z", ".a", ".ar", ".bz2", ".deb", ".dmg", ".gz", ".iso", ".lzma", ".par", ".rar", ".rpm", ".tar", ".tc", ".tgz", ".txz", ".xz", ".z", ".Z", ".zip", ".zst" } },
        {FileTypes.Cryptography, new string[] { ".asc", ".enc", ".gpg", ".p12", ".pfx", ".pgp", ".sig", ".signature", ".cer", ".pem", ".csr", ".crt" } },
        {FileTypes.Immediate, new string[] { "Makefile","Dockerfile"} },
        {FileTypes.Temp, new string[] { ".bak",".bk",".swn",".swo",".swp" } },
        {FileTypes.OtherKnown, new string[] { ".txt" } }
    };

    public static FileTypes GetFileTypeByExtension(string Extension)
    {
        if (Extension == null)
            return FileTypes.Unknown;
        FileTypes result = FileTypes.Unknown;
        foreach (KeyValuePair<FileTypes, string[]> kv in FileTypeExtensions)
            if (kv.Value.Contains(Extension.ToLower())) return kv.Key;
        return result;
    }

}

