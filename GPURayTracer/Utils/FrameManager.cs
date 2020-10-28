using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace GPURayTracer.Utils
{
    public class FrameManager
    {
        public Action onImageUpdate;
        public int width;
        public int height;
        public WriteableBitmap wBitmap;
        public Int32Rect rect;
        public byte[] data;

        public FrameManager(Action onImageUpdate, int width, int height)
        {
            this.onImageUpdate = onImageUpdate;
            this.width = width;
            this.height = height;
            data = new byte[width * height * 3];

            wBitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Rgb24, null);
            rect = new Int32Rect(0, 0, width, height);
        }

        public void write(ref byte[] data)
        {
            if(this.data.Length == data.Length)
            {
                data.CopyTo(this.data, 0);
                onImageUpdate();
            }
        }

        public void update()
        {
            wBitmap.Lock();
            IntPtr pBackBuffer = wBitmap.BackBuffer;
            Marshal.Copy(data, 0, pBackBuffer, data.Length);
            wBitmap.AddDirtyRect(rect);
            wBitmap.Unlock();
        }
    }
}
