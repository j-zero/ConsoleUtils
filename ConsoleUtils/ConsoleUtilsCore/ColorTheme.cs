public class ColorTheme
{
    public static string DarkColor { get { return "268C96"; } }
    public static string HighLight2 { get { return "E17B7C"; } }
    public static string OffsetColor {  get { return "eeeeee";} }
    public static string OffsetColorHighlight { get { return "ffff80"; } }

    public static string GetColor(byte b, bool isOdd)
    {
        return GetColor((int)b, isOdd);
    }
    public static string GetColor(int b, bool isOdd)
    {
        string color = isOdd ? "9CDCFE" : "569CD6";    // default blue;

        if (b == 0x00)
            color = isOdd ? "D7DDEB" : "B0BAD7";
        else if (b == 0x0d || b == 0x0a)    // CR LF
            color = isOdd ? "80ff80" : "66ff66";
        else if (b < 32)
            color = isOdd ? "EBA7A8" : "E17B7C";
        else if (b > 127 && b <= 255)                   // US-ASCII
            color = isOdd ? "ffffcc" : "ffff80";
        else if (b > 255)
            color = isOdd ? "ffc299" : "ffa366";

        return color;
    }
}

