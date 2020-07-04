using GPURayTracer.Rendering;
using GPURayTracer.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace GPURayTracer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public RayTracer rtRenderer;

        public int width = 1920;
        public int height = 1080;
        public int targetFPS = 255;

        public FrameManager frame;
        public bool readyForUpdate = false;

        public UpdateStatsTimer displayTimer;

        public MainWindow()
        {
            InitializeComponent();

            frame = new FrameManager(onImage, width, height);
            Frame.Source = frame.wBitmap;
            displayTimer = new UpdateStatsTimer();

            rtRenderer = new RayTracer();
            rtRenderer.startThread(frame, width, height, targetFPS);

            Closed += MainWindow_Closed;
        }

        private void MainWindow_Closed(object sender, EventArgs e)
        {
            rtRenderer.dispose();
        }

        private void onImage()
        {
            if(!readyForUpdate)
            {
                readyForUpdate = true;

                if(Application.Current != null && Application.Current.Dispatcher != null)
                {
                    Application.Current.Dispatcher.BeginInvoke(updateFrame);
                }
            }
        }

        public void updateFrame()
        {
            if(readyForUpdate)
            {
                displayTimer.endUpdate();
                displayTimer.startUpdate();
                FPS.Content = (int)rtRenderer.rFPStimer.averageUpdateRate + " fps " + (int)displayTimer.averageUpdateRate + " dps";
                frame.update();
                readyForUpdate = false;
            }
        }

        private void Frame_MouseDown(object sender, MouseButtonEventArgs e)
        {
            rtRenderer.pause = true;
            Thread.Sleep(1);

            Point p = e.GetPosition(Frame);
            double frameWidth = Frame.ActualWidth;
            double frameHeight = Frame.ActualHeight;

            if (p.X > 0 && p.Y > 0 && p.X < frameWidth && p.Y < frameWidth)
            {
                int size = 50;

                for(int i = -size; i <= size; i++)
                {
                    for (int j = -size; j <= size; j++)
                    {
                        int x = (int)((p.X / frameWidth) * width) + i;
                        int y = (int)((p.Y / frameHeight) * height) + j;
                        rtRenderer.output[(((y - 1) * width) + (x - 1)) * 3] = 255;
                    }
                }
            }

            rtRenderer.pause = false;
        }

        private void Window_MouseMove(object sender, MouseEventArgs e)
        {
            if(e.LeftButton == MouseButtonState.Pressed)
            {
                rtRenderer.pause = true;
                Thread.Sleep(1);
                Point p = e.GetPosition(Frame);
                double frameWidth = Frame.ActualWidth;
                double frameHeight = Frame.ActualHeight;

                if (p.X > 0 && p.Y > 0 && p.X < frameWidth && p.Y < frameHeight)
                {
                    int size = 50;

                    for (int i = -size; i <= size; i++)
                    {
                        for (int j = -size; j <= size; j++)
                        {
                            int x = (int)((p.X / frameWidth) * width) + i;
                            int y = (int)((p.Y / frameHeight) * height) + j;
                            if (x > 0 && y > 0 && x < width && y < height)
                            {
                                rtRenderer.output[(((y - 1) * width) + (x - 1)) * 3] = 255;
                                rtRenderer.output[((((y - 1) * width) + (x - 1)) * 3) + 1] = 0;
                                rtRenderer.output[((((y - 1) * width) + (x - 1)) * 3) + 2] = 0;
                            }
                        }
                    }
                }

                rtRenderer.pause = false;
            }
        }
    }
}
