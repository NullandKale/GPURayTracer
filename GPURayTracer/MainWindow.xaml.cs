using GPURayTracer.Rendering;
using GPURayTracer.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        public int width;
        public int height;
        public double scale = -4;
        public int targetFPS = 60;

        public FrameManager frame;
        public bool readyForUpdate = false;

        public UpdateStatsTimer displayTimer;

        public MainWindow()
        {
            InitializeComponent();
            Closed += MainWindow_Closed;
            SizeChanged += Window_SizeChanged;
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (scale > 0)
            {
                height = (int)(grid.ActualHeight * scale);
                width = (int)(grid.ActualWidth * scale);
            }
            else
            {
                height = (int)(grid.ActualHeight / -scale);
                width = (int)(grid.ActualWidth / -scale);
            }

            Trace.WriteLine("X: " + width + " " + (width * 3) + " " + ((width * 3) % 4));
            width += ((width * 3) % 4);
            Trace.WriteLine("fixed X: " + width + " " + (width * 3) + " " + ((width * 3) % 4));
            restartRenderer();
        }

        public void restartRenderer()
        {
            frame = new FrameManager(onImage, width, height);
            Frame.Source = frame.wBitmap;
            displayTimer = new UpdateStatsTimer();

            if (rtRenderer != null)
            {
                rtRenderer.JoinRenderThread();
            }
            else
            {
                rtRenderer = new RayTracer();
            }

            rtRenderer.startRenderThread(frame, width, height, targetFPS);
        }

        private void MainWindow_Closed(object sender, EventArgs e)
        {
            if (rtRenderer != null)
            {
                rtRenderer.dispose();
            }
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
                displayTimer.endUpdate();
                displayTimer.startUpdate();
                FPS.Content = (int)(rtRenderer.rFPStimer.averageUpdateRate) + " tps " +
                    (int)displayTimer.averageUpdateRate + " dps\nResolution: " + width + " x " + height +
                    "\nArrow up / down. Res Scale: " + scale;
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
                int size = (int)(width * 0.025);

                for (int i = -size; i <= size; i++)
                {
                    int x = (int)((p.X / frameWidth) * width) + i;
                    if (x > 0 && x < width)
                    {
                        for (int j = -size; j <= size; j++)
                        {
                            int y = (int)((p.Y / frameHeight) * height) + j;
                            if (y > 0 && y < height)
                            {
                                rtRenderer.output[(((y - 1) * width) + (x - 1)) * 3] = 255;
                                rtRenderer.output[((((y - 1) * width) + (x - 1)) * 3) + 1] = 0;
                                rtRenderer.output[((((y - 1) * width) + (x - 1)) * 3) + 2] = 0;
                            }
                        }
                    }
                }
            }

            rtRenderer.pause = false;
        }

        private void Window_MouseMove(object sender, MouseEventArgs e)
        {
            if(rtRenderer != null)
            {
                if (e.LeftButton == MouseButtonState.Pressed)
                {
                    rtRenderer.pause = true;
                    Point p = e.GetPosition(Frame);
                    double frameWidth = Frame.ActualWidth;
                    double frameHeight = Frame.ActualHeight;

                    if (p.X > 0 && p.Y > 0 && p.X < frameWidth && p.Y < frameHeight)
                    {
                        int size = (int)(width * 0.025);

                        for (int i = -size; i <= size; i++)
                        {
                            int x = (int)((p.X / frameWidth) * width) + i;
                            if (x > 0 && x < width)
                            {
                                for (int j = -size; j <= size; j++)
                                {
                                    int y = (int)((p.Y / frameHeight) * height) + j;
                                    if (y > 0 && y < height)
                                    {
                                        rtRenderer.output[(((y - 1) * width) + (x - 1)) * 3] = 255;
                                        rtRenderer.output[((((y - 1) * width) + (x - 1)) * 3) + 1] = 0;
                                        rtRenderer.output[((((y - 1) * width) + (x - 1)) * 3) + 2] = 0;
                                    }
                                }
                            }
                        }
                    }

                    rtRenderer.pause = false;
                }
            }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if(rtRenderer != null)
            {
                if (e.Key == Key.O)
                {
                    if (WindowStyle == WindowStyle.SingleBorderWindow)
                    {
                        WindowState = WindowState.Normal;
                        WindowStyle = WindowStyle.None;
                        WindowState = WindowState.Maximized;
                    }
                    else
                    {
                        WindowStyle = WindowStyle.SingleBorderWindow;
                        WindowState = WindowState.Normal;
                    }

                    Window_SizeChanged(sender, null);
                }

                if(e.Key == Key.Up)
                {
                    scale++;
                    if (scale == 0)
                    {
                        scale = 1;
                    }
                    Window_SizeChanged(sender, null);
                }

                if (e.Key == Key.Down)
                {
                    scale--;
                    if (scale == 0)
                    {
                        scale = -1;
                    }
                    Window_SizeChanged(sender, null);
                }
            }
        }
    }
}
