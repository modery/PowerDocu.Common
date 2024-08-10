using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace PowerDocu.Common
{
    public static class ImageHelper
    {
        public static void ConvertImageTo32(string imagepath, string destinationpath)
        {
            try
            {
                Bitmap bmp = new Bitmap(imagepath);
                Bitmap resized = new Bitmap(bmp, new Size(32, (int)(32 * (bmp.Height / bmp.Width))));
                resized.Save(destinationpath, ImageFormat.Png);
                resized.Dispose();
                bmp.Dispose();
            }
            catch (Exception e)
            {
                throw new Exception("Image conversion failed for " + imagepath + ", " + destinationpath + "\n\n" + e.Message);
            }
        }

        public static string GetBase64(string filepath)
        {
            if (File.Exists(filepath))
            {
                byte[] imageArray = System.IO.File.ReadAllBytes(filepath);
                return Convert.ToBase64String(imageArray);
            }
            return "";
        }
    }
}