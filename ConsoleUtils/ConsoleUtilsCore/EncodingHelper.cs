using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


public class EncodingHelper
{
    /// <summary>
    /// Determines a text file's encoding by analyzing its byte order mark (BOM).
    /// Defaults to ASCII when detection of the text file's endianness fails.
    /// </summary>
    /// <param name="filename">The text file to analyze.</param>
    /// <returns>The detected encoding.</returns>
    // https://stackoverflow.com/a/19283954
    public static Encoding GetEncodingFromFile(string filename)
    {
        // Read the BOM
        var bom = new byte[4];
        using (var file = new FileStream(filename, FileMode.Open, FileAccess.Read))
        {
            file.Read(bom, 0, 4);
        }

        // Analyze the BOM
        if (bom[0] == 0x2b && bom[1] == 0x2f && bom[2] == 0x76) return Encoding.UTF7;
        if (bom[0] == 0xef && bom[1] == 0xbb && bom[2] == 0xbf) return Encoding.UTF8;
        if (bom[0] == 0xff && bom[1] == 0xfe && bom[2] == 0 && bom[3] == 0) return Encoding.UTF32; //UTF-32LE
        if (bom[0] == 0xff && bom[1] == 0xfe) return Encoding.Unicode; //UTF-16LE
        if (bom[0] == 0xfe && bom[1] == 0xff) return Encoding.BigEndianUnicode; //UTF-16BE
        if (bom[0] == 0 && bom[1] == 0 && bom[2] == 0xfe && bom[3] == 0xff) return new UTF32Encoding(true, true);  //UTF-32BE

        // We actually have no idea what the encoding is if we reach this point, so
        // you may wish to return null instead of defaulting to ASCII
        return Encoding.ASCII;
    }

    public static Encoding GetEncodingFromName(string Name)
    {
        Encoding encoding;
        string enc = Name.Replace("-", "").ToLower().Trim();
        // "utf8" (default), "ascii", "utf7", "utf16", "utf16be","utf32", "utf32be"

        switch (enc)
        {
            case "utf8":
                return Encoding.UTF8;
            case "ascii":
                return Encoding.ASCII;
            case "utf7":
                return Encoding.UTF7;
            case "utf16":
            case "utf16le":
            case "unicode":
                return Encoding.Unicode;
            case "utf16be":
                return Encoding.BigEndianUnicode;
            case "utf32":
            case "utf32le":
                return Encoding.UTF32;
            case "utf32be":
                return new UTF32Encoding(true, true);
            default:
                return Encoding.UTF8;
        }
        
    }

}

