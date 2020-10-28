using NullEngine.Utils;
using NullEngine.Rendering.DataStructures;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using ILGPU;
using ILGPU.Runtime;
using NullEngine.Rendering.Implementation;
using System.Windows;

namespace NullEngine.Rendering
{
    public class Renderer
    {
        public int width;
        public int height;
        
        private bool run = true;
        private bool framebufferReady = true;
        private bool dRes;
        private int targetFramerate;
        private double frameTime;

        private ByteFrameBuffer deviceFrameBuffer;
        private byte[] frameBuffer = new byte[0];

        private GPU gpu;
        private RenderDataManager renderDataManager;
        private FrameData frameData;
        private UI.RenderFrame renderFrame;
        private Thread renderThread;
        private FrameTimer frameTimer;

        private Action<Index1, dByteFrameBuffer, byte, byte, byte> clearFramebuffer;

        public Renderer(UI.RenderFrame renderFrame, int targetFramerate, bool forceCPU)
        {
            this.renderFrame = renderFrame;
            this.targetFramerate = targetFramerate;

            gpu = new GPU(forceCPU);
            renderDataManager = new RenderDataManager(gpu);
            clearFramebuffer = gpu.device.LoadAutoGroupedStreamKernel<Index1, dByteFrameBuffer, byte, byte, byte>(UtilityKernels.ClearByteFramebuffer);
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
            framebufferReady = true;
            renderThread.Join();
            deviceFrameBuffer.Dispose();
            gpu.Dispose();
        }

        private void OnResChanged(int width, int height)
        {
            while(!framebufferReady && run)
            {
                Console.WriteLine("FUCK");
            }

            this.width = width;
            this.height = height;

            if(deviceFrameBuffer != null)
            {
                deviceFrameBuffer.Dispose();
            }

            frameBuffer = new byte[width * height * 3];
            deviceFrameBuffer = new ByteFrameBuffer(gpu, height, width);
            frameData = new FrameData(gpu.device, width, height);
        }

        private void Draw()
        {
            renderFrame.update(ref frameBuffer);
            renderFrame.frameRate = frameTimer.lastFrameTimeMS;
        }

        private void RenderToFrameBuffer()
        {
            if(deviceFrameBuffer != null && !deviceFrameBuffer.isDisposed)
            {
                deviceFrameBuffer.inUse = true;
                gpu.Render(deviceFrameBuffer, renderDataManager, frameData);
                gpu.device.Synchronize();
                deviceFrameBuffer.memoryBuffer.CopyTo(frameBuffer, 0, 0, frameBuffer.Length);
                deviceFrameBuffer.inUse = false;
            }
        }

        private void RenderThread()
        {
            while (run)
            {
                frameTimer.startUpdate();

                RenderToFrameBuffer();
                Application.Current.Dispatcher.InvokeAsync(Draw);

                frameTime = frameTimer.endUpdateForTargetUpdateTime(1000.0 / targetFramerate, true);
                if(dRes)
                {
                    renderFrame.frameTime = frameTime;
                    Application.Current.Dispatcher.Invoke(renderFrame.UpdateScale);
                }
            }
        }


    }
}
