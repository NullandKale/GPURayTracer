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
        public static bool debugRandomGeneration = true;
        public static float debugTAAScale = 0.95f;

        public int width;
        public int height;

        public double scale = -1;
        public int extraRenderPasses = 1;
        public int maxBounces = 10;
        public int targetFPS = 70;
        public bool forceCPU = false;
        public Point? lastMousePos;
        public bool mouseDebounce = true;
        public bool mouseEnabled = true;

        public FrameManager frame;
        public bool readyForUpdate = false;

        public UpdateStatsTimer displayTimer;

        public MainWindow()
        {
            InitializeComponent();
            Closed += MainWindow_Closed;
            SizeChanged += Window_SizeChanged;
            Frame.Cursor = Cursors.None;
            taaLabel.Content = debugTAA && !debugZbuffer ? string.Format("TAA ON # {0:0.##}", debugTAAScale) : "TAA OFF";
            taaSlider.Value = debugTAAScale;

            instructions.Content =
                "Keys\n" +
                "O: Toggle fullscreen\n" +
                "E: Toggle mouse capture\n" +
                "C: Toggle CPU / GPU Render\n" +
                "T: Toggle TAA\n" +
                "Y: Toggle Z Buffer display\n" +
                "U: Toggle Random Generation Style\n" +
                "Esc: Exit\n";
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

            if (mouseEnabled)
            {
                Point relativePoint = TransformToAncestor(this).Transform(new Point(0, 0));
                Point pt = new Point(relativePoint.X + grid.ActualWidth / 2, relativePoint.Y + grid.ActualHeight / 2);
                Point windowCenterPoint = pt;
                Point centerPointRelativeToSCreen = this.PointToScreen(windowCenterPoint);
                SetCursorPos((int)centerPointRelativeToSCreen.X, (int)centerPointRelativeToSCreen.Y);
                lastMousePos = null;
                mouseDebounce = true;
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
                rtRenderer.dispose();
                rtRenderer = null;
            }

            try
            {
                rtRenderer = new RayTracer(frame, width, height, targetFPS, extraRenderPasses, maxBounces, forceCPU);
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
                        : ((int)displayTimer.averageUpdateRate + " FPS WPF LIMITED")) + getWindowState();
                    camera.Content = string.Format("Pos: {0:0.##}, {1:0.##} {2:0.##}", rtRenderer.frameData.camera.origin.x, rtRenderer.frameData.camera.origin.y, rtRenderer.frameData.camera.origin.z);
                    debug.Content = "[ " + width + ", " + height + " ]" + " SF: " + scale;
                    renderScale.Content = "Scale Factor: " + scale;
                    samples.Content = "Sample Per Pixel: " + extraRenderPasses;
                    frame.update();
                }
                readyForUpdate = false;
            }

        }

        private string getWindowState()
        {
            string toReturn = " ";
            if(debugRandomGeneration)
            {
                toReturn += "HFP Noise ";
            }
            else
            {
                toReturn += "White Noise ";
            }

            if (debugZbuffer)
            {
                toReturn += "Z Buffer ";
            }
            else if (debugTAA)
            {
                toReturn += "TAA ";
            }

            return toReturn;
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

            if (e.Key == Key.E)
            {
                mouseEnabled = !mouseEnabled;
                Frame.Cursor = mouseEnabled ? Cursors.None : Cursors.Arrow;

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
                debugRandomGeneration = !debugRandomGeneration;
                Window_SizeChanged(sender, null);
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
            extraRenderPasses--;
        }

        private void SamplePlus_Click(object sender, RoutedEventArgs e)
        {
            extraRenderPasses++;
        }

        private void SampleDef_Click(object sender, RoutedEventArgs e)
        {
            extraRenderPasses = 0;
        }

        [DllImport("User32.dll")]
        private static extern bool SetCursorPos(int X, int Y);
        private void Window_MouseMove(object sender, MouseEventArgs e)
        {
            if(mouseEnabled)
            {
                if (lastMousePos == null)
                {
                    lastMousePos = e.GetPosition(grid);
                }
                else
                {
                    Point mouseMovement = e.GetPosition(grid);
                    mouseMovement = new Point(mouseMovement.X - lastMousePos.Value.X, mouseMovement.Y - lastMousePos.Value.Y);
                    lastMousePos = e.GetPosition(grid);

                    if (!mouseDebounce && lastMousePos.Value.X != -mouseMovement.X && lastMousePos.Value.Y != -mouseMovement.Y)
                    {
                        Vec3 strafe = new Vec3(-mouseMovement.Y, mouseMovement.X, 0) * 0.008f;

                        if (Math.Abs(strafe.x) > 0 || Math.Abs(strafe.y) > 0)
                        {
                            rtRenderer.CameraUpdate(new Vec3(), strafe);

                            Point relativePoint = TransformToAncestor(this).Transform(new Point(0, 0));
                            Point pt = new Point(relativePoint.X + grid.ActualWidth / 2, relativePoint.Y + grid.ActualHeight / 2);
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

        private void TaaToggleClick(object sender, RoutedEventArgs e)
        {
            debugTAA = !debugTAA;
            taaLabel.Content = debugTAA && !debugZbuffer ? string.Format("TAA ON # {0:0.##}", debugTAAScale) : "TAA OFF";
        }

        private void taaSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            debugTAAScale = (float)e.NewValue;
            taaLabel.Content = debugTAA && !debugZbuffer ? string.Format("TAA ON # {0:0.##}", debugTAAScale) : "TAA OFF";
        }
    }
}
