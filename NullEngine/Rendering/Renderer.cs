using NullEngine.Utils;
using NullEngine.Rendering.DataStructures;
using System.Threading;
using ILGPU.Runtime;
using NullEngine.Rendering.Implementation;
using System.Windows;
using System;
using ILGPU.Algorithms;

namespace NullEngine.Rendering
{
    public class Renderer
    {
        public int width;
        public int height;
        
        private bool run = true;
        private int targetFramerate;
        private double frameTime;

        private ByteFrameBuffer deviceFrameBuffer;
        private byte[] frameBuffer = new byte[0];

        private GPU gpu;
        private Camera camera;
        private Scene scene;
        private FrameData frameData;
        private UI.RenderFrame renderFrame;
        private Thread renderThread;
        private FrameTimer frameTimer;

        public Renderer(UI.RenderFrame renderFrame, int targetFramerate, bool forceCPU)
        {
            this.renderFrame = renderFrame;
            this.targetFramerate = targetFramerate;
            gpu = new GPU(forceCPU);
            //this.scene = new Scene(gpu, "../../../Assets/CubeTest/Scene.json");
            //this.scene = new Scene(gpu, "../../../Assets/Sponza/Scene.json");
            this.scene = new Scene(gpu, "../../../Assets/Suzannes/Scene.json");
            camera = new Camera(new Vec3(0, -3, 3), new Vec3(0, 0, 0), new Vec3(0, -1, 0), width, height, 40, new Vec3(0, 0, 0));
            frameTimer = new FrameTimer();

            renderFrame.onResolutionChanged = OnResChanged;

            renderThread = new Thread(RenderThread);
            renderThread.IsBackground = true;
        }

        public void Start()
        {
            renderThread.Start();
        }

        public void Stop()
        {
            run = false;
            renderThread.Join();
        }

        private void OnResChanged(int newWidth, int newHeight)
        {
            width = newWidth;
            height = newHeight;

            camera = new Camera(camera, width, height);
        }

        //eveything below this happens in the render thread
        private void RenderThread()
        {
            while (run)
            {
                frameTimer.startUpdate();

                if(ReadyFrameBuffer())
                {
                    RenderToFrameBuffer();
                    Application.Current.Dispatcher.InvokeAsync(Draw);
                }

                frameTime = frameTimer.endUpdateForTargetUpdateTime(1000.0 / targetFramerate, true);
                renderFrame.frameTime = frameTime;
            }

            if (deviceFrameBuffer != null)
            {
                deviceFrameBuffer.Dispose();
                frameData.Dispose();
            }
            gpu.Dispose();
        }

        private bool ReadyFrameBuffer()
        {
            if((width != 0 && height != 0))
            {
                if(deviceFrameBuffer == null || deviceFrameBuffer.frameBuffer.width != width || deviceFrameBuffer.frameBuffer.height != height)
                {
                    if (deviceFrameBuffer != null)
                    {
                        deviceFrameBuffer.Dispose();
                        frameData.Dispose();
                    }

                    frameBuffer = new byte[width * height * 3];
                    deviceFrameBuffer = new ByteFrameBuffer(gpu, height, width);
                    frameData = new FrameData(gpu.device, width, height);
                }

                return true;
            }
            else
            {
                return false;
            }
        }

        private void RenderToFrameBuffer()
        {
            if (deviceFrameBuffer != null && !deviceFrameBuffer.isDisposed)
            {
                gpu.Render(camera, scene, deviceFrameBuffer.frameBuffer, frameData.deviceFrameData);
                deviceFrameBuffer.memoryBuffer.CopyToCPU(frameBuffer);
            }
        }

        private void Draw()
        {
            renderFrame.update(ref frameBuffer);
            renderFrame.frameRate = frameTimer.lastFrameTimeMS;
        }
    }
}
