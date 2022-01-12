using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Text;


public class ASCIIConverter
{

    /// <summary>
    /// These constants are used to convert an image to a "true" ASCII drawing.
    /// Each constant is selected by it's relative grayscale value to represent
    /// a single pixel in a bitmap.
    /// </summary>
    
    private const string BLACK = "@";
    private const string CHARCOAL = "#";
    private const string DARKGRAY = "8";
    private const string MEDIUMGRAY = "&";
    private const string MEDIUM = "o";
    private const string GRAY = ":";
    private const string SLATEGRAY = "*";
    private const string LIGHTGRAY = ".";
    private const string WHITE = " ";


    /// <summary>
    /// Macro to return one of the ASCII constants based on the relative
    /// "darkness" of the red value.
    /// </summary>
    /// <param name="redValue">The red value of the pixel</param>
    /// <returns>System.String</returns>
    private static string getGrayShade(int redValue)
    {
        string asciival = "&nbsp;";

        if (redValue >= 230)
        {
            asciival = WHITE;
        }
        else if (redValue >= 200)
        {
            asciival = LIGHTGRAY;
        }
        else if (redValue >= 180)
        {
            asciival = SLATEGRAY;
        }
        else if (redValue >= 160)
        {
            asciival = GRAY;
        }
        else if (redValue >= 130)
        {
            asciival = MEDIUM;
        }
        else if (redValue >= 100)
        {
            asciival = MEDIUMGRAY;
        }
        else if (redValue >= 70)
        {
            asciival = DARKGRAY;
        }
        else if (redValue >= 50)
        {
            asciival = CHARCOAL;
        }
        else
        {
            asciival = BLACK;
        }

        return asciival;
    }


    public static string GrayscaleImageToASCII(System.Drawing.Image img)
    {
        StringBuilder html = new StringBuilder();
        Bitmap bmp = null;

        try
        {
            // Create a bitmap from the image
            bmp = new Bitmap(img);

            // Loop through each pixel in the bitmap
            for (int y = 0; y < bmp.Height; y++)
            {
                for (int x = 0; x < bmp.Width; x++)
                {
                    // Get the color of the current pixel
                    Color col = bmp.GetPixel(x, y);

                    // To convert to grayscale, the easiest method is to add
                    // the R+G+B colors and divide by three to get the gray
                    // scaled color.
                    col = Color.FromArgb((col.R + col.G + col.B) / 3,
                        (col.R + col.G + col.B) / 3,
                        (col.R + col.G + col.B) / 3);

                    // Get the R(ed) value from the grayscale color,
                    // parse to an int. Will be between 0-255.
                    int rValue = int.Parse(col.R.ToString());

                    // Append the "color" using various darknesses of ASCII
                    // character.
                    html.Append(getGrayShade(rValue));

                    // If we're at the width, insert a line break
                    if (x == bmp.Width - 1)
                        html.Append("\n");
                }
            }


            return html.ToString();
        }
        catch (Exception exc)
        {
            return exc.ToString();
        }
        finally
        {
            bmp.Dispose();
        }
    }

    public static Image ResizeImageKeepAspect(Image OriginalImage, int maxWidth, int maxHeight, bool enlarge = false, InterpolationMode interpolationMode = InterpolationMode.Bicubic)
    {
        maxWidth = enlarge ? maxWidth : Math.Min(maxWidth, OriginalImage.Width);
        maxHeight = enlarge ? maxHeight : Math.Min(maxHeight, OriginalImage.Height);

        decimal rnd = Math.Min(maxWidth / (decimal)OriginalImage.Width, maxHeight / (decimal)OriginalImage.Height);
        Size s = new Size((int)Math.Round(OriginalImage.Width * rnd), (int)Math.Round(OriginalImage.Height * rnd));

        return ResizeImagePixelPerfect(OriginalImage, s.Width, s.Height);
    }

    public static Bitmap ResizeImagePixelPerfect(Image image, int width, int height)
    {
        var destRect = new Rectangle(0, 0, width, height);
        var destImage = new Bitmap(width, height);

        destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);

        using (var graphics = Graphics.FromImage(destImage))
        {

            graphics.PixelOffsetMode = PixelOffsetMode.Half;
            graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
            graphics.SmoothingMode = SmoothingMode.None;

            using (var wrapMode = new ImageAttributes())
            {
                wrapMode.SetWrapMode(WrapMode.TileFlipXY);
                graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);
            }
        }

        return destImage;
    }
}
