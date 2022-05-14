using Microsoft.Win32;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;

namespace SdarotTV_Downloader
{
    public class Utils
    {
        private static readonly Regex removeInvalidChars = new Regex($"[{Regex.Escape(new string(Path.GetInvalidFileNameChars()))}]",
         RegexOptions.Singleline | RegexOptions.Compiled | RegexOptions.CultureInvariant);

        public static string SanitizePath(string path)
        {
            return removeInvalidChars.Replace(path, "_");
        }

        public static string GetChromePath()
        {
            return Registry.GetValue(Consts.CHROME_REGISTRY_KEY, "", null).ToString();
        }

        private static string GetExecutableVersion(string exePath)
        {
            return FileVersionInfo.GetVersionInfo(exePath).FileVersion;
        }

        public static string GetExecutableBaseVersion(string exePath)
        {
            string fullVersion = GetExecutableVersion(exePath);
            return fullVersion.Split('.')[0];
        }
        
        public static string GetProgressString<T1, T2>(T1 val, T2 max)
        {
            return GetProgressString(val.ToString(), max.ToString());
        }
        
        public static string GetProgressString<T1, T2>(T1 val, T2 max, string ext)
        {
            return GetProgressString(val.ToString() + ext, max.ToString() + ext);
        }

        public static string GetProgressString(string val, string max)
        {
            return val + " / " + max;
        }

        public static string TruncateString(string value, int maxChars)
        {
            return value.Length <= maxChars ? value : "..." + value.Substring(value.Length - maxChars);
        }

        public static Image GetImage(string fromUrl)
        {
            using (WebClient webClient = new WebClient())
            {
                using (Stream stream = webClient.OpenRead(fromUrl))
                {
                    return Image.FromStream(stream);
                }
            }
        }

        public static Image ResizeImage(Image image, int width, int height)
        {
            var destRect = new Rectangle(0, 0, width, height);
            var destImage = new Bitmap(width, height);

            destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);

            using (var graphics = Graphics.FromImage(destImage))
            {
                graphics.CompositingMode = CompositingMode.SourceCopy;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                using (var wrapMode = new ImageAttributes())
                {
                    wrapMode.SetWrapMode(WrapMode.TileFlipXY);
                    graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);
                }
            }

            return destImage;
        }
    }
}
