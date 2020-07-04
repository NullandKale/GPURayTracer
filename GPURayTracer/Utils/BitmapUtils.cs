using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Media.Imaging;

namespace GPURayTracer
{
    public static class BitmapUtils
    {
        public static BitmapImage DataToImageSource(byte[] data, int width, int height)
        {
            Bitmap bitmap = BitmapFromData(data, width, height);

            using (MemoryStream memory = new MemoryStream())
            {
                memory.Read(data, 0, data.Length);
                bitmap.Save(memory, ImageFormat.Bmp);
                memory.Position = 0;
                BitmapImage bitmapimage = new BitmapImage();
                bitmapimage.BeginInit();
                bitmapimage.StreamSource = memory;
                bitmapimage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapimage.EndInit();

                return bitmapimage;
            }
        }

        public static BitmapImage BitmapToImageSource(Bitmap bitmap)
        {
            using (MemoryStream memory = new MemoryStream())
            {
                bitmap.Save(memory, ImageFormat.Bmp);
                memory.Position = 0;
                BitmapImage bitmapimage = new BitmapImage();
                bitmapimage.BeginInit();
                bitmapimage.StreamSource = memory;
                bitmapimage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapimage.EndInit();

                return bitmapimage;
            }
        }

        public static Bitmap BitmapFromData(byte[] data, int width, int height)
        {
            Bitmap bitmap = new Bitmap(width, height, PixelFormat.Format24bppRgb);

            BitmapData bmData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadWrite, bitmap.PixelFormat);
            IntPtr pNative = bmData.Scan0;
            Marshal.Copy(data, 0, pNative, width * height * 3);
            bitmap.UnlockBits(bmData);

            //for (int i = 0; i < width * height * 3; i += 3)
            //{
            //    Color c = FromUnboundedRGB(data[i], data[i + 1], data[i + 2]);

            //    int x = ((i / 3) % width);
            //    int y = ((i / 3) / width);

            //    bmp.SetPixel(x, y, c);
            //}

            bitmap.RotateFlip(RotateFlipType.RotateNoneFlipY);

            return bitmap;
        }

        public static Color FromUnboundedRGB(int R, int G, int B)
        {
            if (R < 0)
            {
                R = 0;
            }
            else if (R > 255)
            {
                R = 255;
            }

            if (G < 0)
            {
                G = 0;
            }
            else if (G > 255)
            {
                G = 255;
            }

            if (B < 0)
            {
                B = 0;
            }
            else if (B > 255)
            {
                B = 255;
            }

            return Color.FromArgb(R, G, B);
        }
    }
}
