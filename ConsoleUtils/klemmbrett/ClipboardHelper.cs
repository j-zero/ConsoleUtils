
using System.Drawing;
using System.Windows.Forms;

//using uwp = Windows.ApplicationModel.DataTransfer;

namespace klemmbrett
{
    class ClipboardHelper
    {
        public static void SetText(string Text)
        {
            Clipboard.SetText(Text, TextDataFormat.UnicodeText);
        }
        public static void SetText(string Text, TextDataFormat Format)
        {
            Clipboard.SetText(Text, Format);

        }
        public static void SetImage(Image Image)
        {
            Clipboard.SetImage(Image);
        }

        public static string GetStringOrNull()
        {
            return Clipboard.ContainsText() ? Clipboard.GetText(TextDataFormat.UnicodeText) : null;
        }

        public static void SetTextWindows10(string text, bool enableHistory, bool enableRoaming)
        {
            /*
            uwp.DataPackage dataPackage = new uwp.DataPackage { RequestedOperation = uwp.DataPackageOperation.Copy };
            dataPackage.SetText(text);
            uwp.Clipboard.SetContentWithOptions(dataPackage, new uwp.ClipboardContentOptions() { IsAllowedInHistory = enableHistory, IsRoamable = enableRoaming });
            uwp.Clipboard.Flush();
            */
        }

        public static bool ContainsImage()
        {
            try
            {
                return Clipboard.ContainsImage();
            }
            catch
            {
                return false;
            }
        }
        public static bool ContainsText(TextDataFormat format)
        {
            try
            {
                return Clipboard.ContainsText(format);
            }
            catch
            {
                return false;
            }
        }
        public static bool ContainsText()
        {
            try
            {
                return Clipboard.ContainsText();
            }
            catch
            {
                return false;
            }
        }
        public static bool ContainsFileDropList()
        {
            try
            {
                return Clipboard.ContainsFileDropList();
            }
            catch
            {
                return false;
            }
        }
    }
}
