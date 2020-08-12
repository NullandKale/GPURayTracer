using GPURayTracer.Utils;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace GPURayTracer
{
    /// <summary>
    /// Interaction logic for WorldGeneration.xaml
    /// </summary>
    public partial class WorldGeneration : Window
    {
        public FrameManager frame;

        bool readyForUpdate = false;
        int width;
        int height;

        public WorldGeneration()
        {
            InitializeComponent();

        }
        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            height = (int)(grid.ActualHeight);
            width = (int)(grid.ActualWidth);

            width += ((width * 3) % 4);
            frame = new FrameManager(onImage, width, height);
        }


        private void onImage()
        {
            if (!readyForUpdate)
            {
                readyForUpdate = true;

                if (Application.Current != null && Application.Current.Dispatcher != null)
                {
                    Application.Current.Dispatcher.BeginInvoke(updateFrame);
                }
            }
        }

        public void updateFrame()
        {
            if (readyForUpdate)
            {
                frame.update();
            }
            readyForUpdate = false;

        }
    }
}
