using System.Collections.Generic;

public class ColorTheme
{
    public static string Directory = "#569CD6";
    //private string _colorExecutable = "";
    public static string File = "#DDDDDD";
    public static string Symlink = "#00CED1";
    public static string UnknownFileType = "#aaaaaa";

    public static string Default1 { get { return "9CDCFE"; } }
    public static string Default2 { get { return "569CD6"; } }
    public static string DarkColor { get { return "268C96"; } }
    public static string HighLight2 { get { return "D69D85"; } }
    public static string OffsetColor {  get { return "eeeeee";} }
    public static string OffsetColorHighlight { get { return "D2CC8D"; } }

    private static readonly Dictionary<FileTypes, string> FileTypeColors = new Dictionary<FileTypes, string>()
    {
        {FileTypes.Image, "#DDA0DD" },
        {FileTypes.Video, "#EE82EE" },
        {FileTypes.Executable, "#32CD32" },
        {FileTypes.Script, "#32CD32" },
        {FileTypes.Sourcecode, "#00bfff" },
        {FileTypes.Document, "#8EDCFE" },
        {FileTypes.Music, "#ffc0cb" },
        {FileTypes.Archive, "#dc143c" },
        {FileTypes.Cryptography,"#ff69b4" },
        {FileTypes.Immediate, "#8fbc8f" },
        {FileTypes.Temp, "#778899" },
        {FileTypes.OtherKnown, File },
        {FileTypes.Unknown, UnknownFileType }



    };

    public static string GetColorByFileType(FileTypes Type)
    {
        if (FileTypeColors.ContainsKey(Type))
            return FileTypeColors[Type];
        return null;
    }
    public static string GetColorByExtension(string Extension)
    {
        return GetColorByFileType(FileDefinitions.GetFileTypeByExtension(Extension));
    }

    public static string GetColor(byte b, bool isOdd)
    {
        return GetColor((int)b, isOdd);
    }
    public static string GetColor(int b, bool isOdd)
    {
        string color = isOdd ? Default1 : Default2;    // default blue;

        if (b == 0x00)
            color = isOdd ? "D7DDEB" : "B0BAD7";
        else if (b == 0x0d || b == 0x0a)    // CR LF
            color = isOdd ? "4EC9B0" : "2EA990";
        else if (b < 32)
            color = isOdd ? HighLight2 : "A67D65";
        else if (b > 127 && b <= 255)                   // US-ASCII
            color = isOdd ? OffsetColorHighlight : "B2AC6D";
        else if (b > 255)
            color = isOdd ? "ffc299" : "ffa366";

        return color;
    }
}

