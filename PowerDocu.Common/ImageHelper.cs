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

        public static Bitmap ConvertBase64ToBitmap(string base64String)
        {
            // Remove data URL prefix if present (e.g., "data:image/png;base64,")
            if (base64String.Contains(","))
            {
                base64String = base64String.Substring(base64String.IndexOf(",") + 1);
            }

            try
            {
                byte[] imageBytes = Convert.FromBase64String(base64String);
                using (MemoryStream ms = new MemoryStream(imageBytes))
                {
                    return new Bitmap(ms);
                }
            }
            catch (Exception ex)
            {
                // Handle invalid base64 or image format
                NotificationHelper.SendNotification($"Error converting base64 to bitmap: {ex.Message}");
                return null;
            }
        }

    }
}