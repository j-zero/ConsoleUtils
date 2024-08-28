using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using Pastel;
using System.Drawing.Imaging;


namespace img
{
    internal class Program
    {

        static void Main(string[] args)
        {
            

            Image image = Image.FromFile(args[0]);

            Image image_trans = new Bitmap(image, new Size((int)(image.Width * 2), image.Height)); // console font ratio ~ 2/1

            var new_image = ScaleImage(image_trans, Console.WindowWidth - 4, Console.WindowHeight - 8);

            PrintImageConsole(new_image);

            //Console.WriteLine($"console: {Console.WindowWidth}x{Console.WindowHeight}, image: {image.Width}x{image.Height}, new image: {new_image.Width}x{new_image.Height}");


            ;
        }

        static void PrintImageConsole(Image image)
        {
            string pixel = " ";
            StringBuilder sb = new StringBuilder();
            //Console.WriteLine($"console: {Console.WindowWidth}x{Console.WindowHeight}, image: {image.Width}x{image.Height}, new image: {new_image.Width}x{new_image.Height}");
            for (int y = 0; y < image.Height; y++)
            {
                for (int x = 0; x < image.Width; x++)
                {
                    Color c = ((Bitmap)image).GetPixel(x, y);
                    if (c.A == 0)
                        sb.Append(pixel);
                    else
                        sb.Append(pixel.PastelBg(c));
                }
                sb.Append(Environment.NewLine);
            }

            Console.Write(sb.ToString());
        }

        public static Image ScaleImage(Image image, int maxWidth, int maxHeight)
        {
            var ratioX = (double)maxWidth / image.Width;
            var ratioY = (double)maxHeight / image.Height ;

            var ratio = Math.Min(ratioX, ratioY);

            var newWidth = (int)(image.Width * ratio);
            var newHeight = (int)(image.Height * ratio);

            var newImage = new Bitmap(newWidth, newHeight);

            using (var graphics = Graphics.FromImage(newImage))
                graphics.DrawImage(image, 0, 0, newWidth, newHeight);

            return newImage;
        }

        public double GetBrightness(Color color)
        {
            return (0.2126 * color.R + 0.7152 * color.G + 0.0722 * color.B);
        }

        public static Bitmap MakeGrayscale3(Bitmap original)
        {
            //create a blank bitmap the same size as original
            Bitmap newBitmap = new Bitmap(original.Width, original.Height);

            //get a graphics object from the new image
            using (Graphics g = Graphics.FromImage(newBitmap))
            {

                //create the grayscale ColorMatrix
                ColorMatrix colorMatrix = new ColorMatrix(
                   new float[][]
                   {
             new float[] {.3f, .3f, .3f, 0, 0},
             new float[] {.59f, .59f, .59f, 0, 0},
             new float[] {.11f, .11f, .11f, 0, 0},
             new float[] {0, 0, 0, 1, 0},
             new float[] {0, 0, 0, 0, 1}
                   });

                //create some image attributes
                using (ImageAttributes attributes = new ImageAttributes())
                {

                    //set the color matrix attribute
                    attributes.SetColorMatrix(colorMatrix);

                    //draw the original image on the new image
                    //using the grayscale color matrix
                    g.DrawImage(original, new Rectangle(0, 0, original.Width, original.Height),
                                0, 0, original.Width, original.Height, GraphicsUnit.Pixel, attributes);
                }
            }
            return newBitmap;
        }
    }
}
