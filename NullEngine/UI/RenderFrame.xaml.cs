using ILGPU;
using ILGPU.Runtime;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace NullEngine.UI
{
    /// <summary>
    /// Interaction logic for RenderFrame.xaml
    /// </summary>
    public partial class RenderFrame : UserControl
    {
        public double scale = 1;

        public int width;
        public int height;
        public WriteableBitmap wBitmap;
        public Int32Rect rect;

        public Action<int, int> onResolutionChanged;
        public double frameTime;
        public double frameRate;

        public RenderFrame()
        {
            InitializeComponent();

            SizeChanged += RenderFrame_SizeChanged;
        }

        private void RenderFrame_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            width = (int)e.NewSize.Width;
            height = (int)e.NewSize.Height;
            UpdateResolution();
        }

        private void UpdateResolution()
        {
            if (scale > 0)
            {
                height = (int)(height * scale);
                width = (int)(width * scale);
            }
            else
            {
                height = (int)(height / -scale);
                width = (int)(width / -scale);
            }

            width += ((width * 3) % 4);

            wBitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Rgb24, null);
            Frame.Source = wBitmap;
            rect = new Int32Rect(0, 0, width, height);
            onResolutionChanged(width, height);
        }

        public void UpdateScale()
        {
            if(frameTime < 0)
            {
                scale -= 0.1;
            }
            else if(frameTime > 5)
            {
                if(scale < 1)
                {
                    scale += 0.1;
                }
            }

            if(scale >= 0 && scale < 1)
            {
                scale = -1;
            }

            if(scale < -100)
            {
                scale = -100;
            }

            if(scale > 1)
            {
                scale = 1;
            }

            UpdateResolution();
        }

        public void update(ref byte[] data)
        {
            if(data.Length == wBitmap.PixelWidth * wBitmap.PixelHeight * 3)
            {
                wBitmap.Lock();
                IntPtr pBackBuffer = wBitmap.BackBuffer;
                Marshal.Copy(data, 0, pBackBuffer, data.Length);
                wBitmap.AddDirtyRect(rect);
                wBitmap.Unlock();

                Info.Content = (int)frameRate + " MS";
            }
        }
    }
}
