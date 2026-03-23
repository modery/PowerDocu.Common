using System;
using System.Collections.Concurrent;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Threading;

namespace PowerDocu.Common
{
    public static class ImageHelper
    {
        private static readonly ConcurrentDictionary<string, string> _base64Cache = new();
        private static readonly ConcurrentDictionary<string, object> _conversionLocks = new();
        public static void ConvertImageTo32(string imagepath, string destinationpath)
        {
            if (string.IsNullOrWhiteSpace(imagepath) || string.IsNullOrWhiteSpace(destinationpath))
            {
                NotificationHelper.SendNotification("Image conversion skipped due to empty source or destination path.");
                return;
            }

            if (!File.Exists(imagepath))
            {
                NotificationHelper.SendNotification("Image conversion skipped because source image was not found: " + imagepath);
                return;
            }

            string destinationDirectory = Path.GetDirectoryName(destinationpath);
            if (!string.IsNullOrEmpty(destinationDirectory) && !Directory.Exists(destinationDirectory))
            {
                Directory.CreateDirectory(destinationDirectory);
            }

            string lockKey = Path.GetFullPath(destinationpath).ToLowerInvariant();
            object conversionLock = _conversionLocks.GetOrAdd(lockKey, _ => new object());

            lock (conversionLock)
            {
                if (File.Exists(destinationpath))
                {
                    return;
                }

                try
                {
                    using Bitmap source = new Bitmap(imagepath);

                    int targetWidth = 32;
                    int targetHeight = source.Width == 0
                        ? 32
                        : Math.Max(1, (int)Math.Round(targetWidth * ((double)source.Height / source.Width)));

                    using Bitmap resized = new Bitmap(targetWidth, targetHeight);
                    using (Graphics graphics = Graphics.FromImage(resized))
                    {
                        graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                        graphics.SmoothingMode = SmoothingMode.HighQuality;
                        graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                        graphics.CompositingQuality = CompositingQuality.HighQuality;
                        graphics.DrawImage(source, 0, 0, targetWidth, targetHeight);
                    }

                    const int maxAttempts = 3;
                    for (int attempt = 1; attempt <= maxAttempts; attempt++)
                    {
                        try
                        {
                            resized.Save(destinationpath, ImageFormat.Png);
                            return;
                        }
                        catch when (attempt < maxAttempts)
                        {
                            Thread.Sleep(60);
                        }
                    }
                }
                catch (Exception e)
                {
                    try
                    {
                        // Fallback: preserve graph rendering by using original icon if resizing fails.
                        File.Copy(imagepath, destinationpath, true);
                        NotificationHelper.SendNotification(
                            "Image conversion fallback used for " + imagepath + ": " + e.Message
                        );
                    }
                    catch (Exception fallbackException)
                    {
                        NotificationHelper.SendNotification(
                            "Image conversion failed for " + imagepath + ", " + destinationpath + "\n\n" + e.Message + "\n\n" + fallbackException.Message
                        );
                    }
                }
            }
        }

        public static string GetBase64(string filepath)
        {
            return _base64Cache.GetOrAdd(filepath, path =>
            {
                if (File.Exists(path))
                {
                    byte[] imageArray = File.ReadAllBytes(path);
                    return Convert.ToBase64String(imageArray);
                }
                return "";
            });
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
                MemoryStream ms = new MemoryStream(imageBytes);
                return new Bitmap(ms);
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