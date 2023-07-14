using HeyRed.Mime;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


public class MIMEHelper
{
    /*
        *      magic = new Magic(MagicOpenFlags.MAGIC_NONE);
            Console.WriteLine("Info:      " + magic.Read(path));

            magic = new Magic(MagicOpenFlags.MAGIC_MIME_ENCODING);
            Console.WriteLine("Encoding:  " + magic.Read(path));

            magic = new Magic(MagicOpenFlags.MAGIC_MIME_TYPE);
            string mime = magic.Read(path);
    */

    public MIMEInfo GetMIMEInfo(string Path)
    {
        MIMEInfo mimeInfo = new MIMEInfo();
        Magic magic = new HeyRed.Mime.Magic(MagicOpenFlags.MAGIC_NONE);
        mimeInfo.Description = magic.Read(Path);

        magic = new Magic(MagicOpenFlags.MAGIC_MIME_ENCODING);
        mimeInfo.Encoding = magic.Read(Path);

        magic = new Magic(MagicOpenFlags.MAGIC_MIME_TYPE);
        string mime = magic.Read(Path);
        mimeInfo.MimeType = magic.Read(Path);

        string ext = HeyRed.Mime.MimeTypesMap.GetExtension(mime);
        mimeInfo.Extension = ext;

        return mimeInfo;

    }

    public static string GetDescription(string Path)
    {
        return GetMIMEFlag(Path, MagicOpenFlags.MAGIC_NONE);
    }

    public static string GetDescription(FileStream Stream)
    {
        return GetMIMEFlag(Stream, MagicOpenFlags.MAGIC_NONE);
        
    }

    public static string GetMIMEType(string Path)
    {
        return GetMIMEFlag(Path, MagicOpenFlags.MAGIC_MIME_TYPE);
    }
    public static string GetEncoding(string Path)
    {
        return GetMIMEFlag(Path, MagicOpenFlags.MAGIC_MIME_ENCODING);
    }

    public static string GetExtension(string MIMEType)
    {
        return HeyRed.Mime.MimeTypesMap.GetExtension(MIMEType);

    }

    public static string GetMIMEFlag(string Path, MagicOpenFlags Flag)
    {
        return (new HeyRed.Mime.Magic(Flag)).Read(Path);
    }

    public static string GetMIMEFlag(FileStream Stream, MagicOpenFlags Flag)
    {
        return (new HeyRed.Mime.Magic(Flag)).Read(Stream, 1024);
    }

}

public class MIMEInfo
{
    public string Description { get; set; }
    public string Encoding { get; set; }
    public string MimeType { get; set; }
    public string Extension { get; set; }

    public MIMEInfo()
    {

    }
    public MIMEInfo(string Description, string Encoding, string MimeType, string Extension)
    {
        this.Description = Description;
        this.Encoding = Encoding;
        this.Extension = Extension;
        this.MimeType = MimeType;
    }
}

