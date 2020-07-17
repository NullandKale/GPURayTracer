using GPURayTracer.Rendering;
using GPURayTracer.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
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

        public static bool debugZbuffer = false;
        public static bool debugTAA = true;

        public int width;
        public int height;

        public double scale = -1;
        public int MSAA = 0;
        public int maxBounces = 10;
        public int targetFPS = 60;
        public bool forceCPU = false;
        public Point? lastMousePos;
        public bool mouseDebounce = true;

        public FrameManager frame;
        public bool readyForUpdate = false;

        public UpdateStatsTimer displayTimer;

        public MainWindow()
        {
            InitializeComponent();
            Closed += MainWindow_Closed;
            SizeChanged += Window_SizeChanged;
            Frame.Cursor = Cursors.None;
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

            Point relativePoint = TransformToAncestor(this)
                           .Transform(new Point(0, 0));
            Point pt = new Point(relativePoint.X + grid.ActualWidth / 2,
                                 relativePoint.Y + grid.ActualHeight / 2);
            Point windowCenterPoint = pt;
            Point centerPointRelativeToSCreen = this.PointToScreen(windowCenterPoint);
            SetCursorPos((int)centerPointRelativeToSCreen.X, (int)centerPointRelativeToSCreen.Y);
            lastMousePos = null;
            mouseDebounce = true;

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
                rtRenderer.dispose();
                rtRenderer = null;
            }

            try
            {
                rtRenderer = new RayTracer(frame, width, height, targetFPS, MSAA, maxBounces, forceCPU);
            }
            catch (Exception e)
            {
                FPS.Content = e.ToString();
                Trace.WriteLine(e.ToString());
            }
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
            if(rtRenderer != null)
            {
                if (readyForUpdate)
                {
                    displayTimer.endUpdate();
                    displayTimer.startUpdate();
                    FPS.Content = rtRenderer.device.AcceleratorType.ToString() + " "
                        + (rtRenderer.rFPStimer.averageUpdateRate <= displayTimer.averageUpdateRate 
                        ? ((int)(rtRenderer.rFPStimer.averageUpdateRate) + " FPS") 
                        : ((int)displayTimer.averageUpdateRate + " FPS WPF LIMITED"));
                    debug.Content = "[ " + width + ", " + height + " ]" + " SF: " + scale + " Sample Per Pixel: " + MSAA;
                    renderScale.Content = "Scale Factor: " + scale;
                    samples.Content = "Sample Per Pixel: " + MSAA;
                    frame.update();
                }
                readyForUpdate = false;
            }

        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
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

            if (e.Key == Key.C)
            {
                forceCPU = !forceCPU;
                Window_SizeChanged(sender, null);
            }


            if (e.Key == Key.T)
            {
                debugTAA = !debugTAA;
            }

            if (e.Key == Key.Y)
            {
                debugZbuffer = !debugZbuffer;
            }

            if (e.Key == Key.U)
            {
                debugTAA = !debugTAA;
            }

            if (e.Key == Key.Escape)
            {
                Close();
            }
        }

        private void SFMinus_Click(object sender, RoutedEventArgs e)
        {
            scale--;
            if (scale == 0)
            {
                scale = -1;
            }
            Window_SizeChanged(sender, null);
        }

        private void SFPlus_Click(object sender, RoutedEventArgs e)
        {
            scale++;
            if (scale == 0)
            {
                scale = 1;
            }
            Window_SizeChanged(sender, null);
        }

        private void SFDef_Click(object sender, RoutedEventArgs e)
        {
            scale = -2;
            Window_SizeChanged(sender, null);
        }

        private void SampleMinus_Click(object sender, RoutedEventArgs e)
        {
            MSAA--;
            Window_SizeChanged(sender, null);
        }

        private void SamplePlus_Click(object sender, RoutedEventArgs e)
        {
            MSAA++;
            Window_SizeChanged(sender, null);
        }

        private void SampleDef_Click(object sender, RoutedEventArgs e)
        {
            MSAA = 2;
            Window_SizeChanged(sender, null);
        }

        [DllImport("User32.dll")]
        private static extern bool SetCursorPos(int X, int Y);
        private void Window_MouseMove(object sender, MouseEventArgs e)
        {
            if(lastMousePos == null)
            {
                lastMousePos = e.GetPosition(grid);
            }
            else
            {
                Point mouseMovement = e.GetPosition(grid);
                mouseMovement = new Point(mouseMovement.X - lastMousePos.Value.X, mouseMovement.Y - lastMousePos.Value.Y);
                lastMousePos = e.GetPosition(grid);

                if(!mouseDebounce && lastMousePos.Value.X != -mouseMovement.X && lastMousePos.Value.Y != -mouseMovement.Y)
                {
                    Vec3 strafe = new Vec3(-mouseMovement.Y, mouseMovement.X, 0) * 0.008f;

                    if (Math.Abs(strafe.x) > 0 || Math.Abs(strafe.y) > 0)
                    {
                        rtRenderer.CameraUpdate(new Vec3(), strafe);

                        Point relativePoint = TransformToAncestor(this)
                           .Transform(new Point(0, 0));
                        Point pt = new Point(relativePoint.X + grid.ActualWidth / 2,
                                             relativePoint.Y + grid.ActualHeight / 2);
                        Point windowCenterPoint = pt;
                        Point centerPointRelativeToSCreen = grid.PointToScreen(windowCenterPoint);
                        SetCursorPos((int)centerPointRelativeToSCreen.X, (int)centerPointRelativeToSCreen.Y);
                        mouseDebounce = true;
                    }
                }
                else
                {
                    mouseDebounce = false;
                }
            }
        }
    }
}
