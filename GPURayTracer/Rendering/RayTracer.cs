using GPURayTracer.Rendering.GPUStructs;
using GPURayTracer.Rendering.Primitives;
using GPURayTracer.Utils;
using ILGPU;
using ILGPU.Runtime;
using ILGPU.Runtime.CPU;
using ILGPU.Runtime.Cuda;
using System;
using System.Threading;

namespace GPURayTracer.Rendering
{
    public class RayTracer
    {
        public bool run = true;
        public bool pause = false;
        public bool renderThreadLock = false;
        public UpdateStatsTimer renderThreadTimer;

        private Thread RenderThread;

        public Context context;
        public Accelerator device;

        Action<Index2, dFramebuffer, dWorldBuffer, Camera, int> renderKernel;
        Action<Index1, dFramebuffer, dFramebuffer, float, float, int> TAAKernel;
        Action<Index1, ArrayView<float>, ArrayView<float>, Camera, int> FilterKernel;
        Action<Index1, ArrayView<float>, float, float> normalizeMapKernel;
        Action<Index1, ArrayView<float>> normalizeLightingKernel;
        Action<Index1, ArrayView<float>, ArrayView<float>, ArrayView<int>> combineKernel;
        Action<Index1, ArrayView<float>, ArrayView<byte>, Camera> outputKernel;
        Action<Index1, ArrayView<float>, ArrayView<byte>, Camera> outputZbufferKernel;

        public byte[] output;
        FrameManager frame;

        public FrameData frameData;
        WorldData worldData;

        int targetFPS;
        int tick = 0;

        public InputManager inputManager;

        public RayTracer(FrameManager frameManager, int width, int height, int targetFPS, int maxBounce, bool forceCPU)
        {
            //ContextFlags improve performance and are ordered by speedup
            context = new Context(ContextFlags.AggressiveInlining | ContextFlags.FastMath | ContextFlags.Force32BitFloats);
            context.EnableAlgorithms();
            initBestDevice(forceCPU);

            setResolution(frameManager, width, height, maxBounce);
            worldData = new WorldData(device);
            inputManager = new InputManager();

            this.targetFPS = targetFPS;

            run = true;

            renderThreadTimer = new UpdateStatsTimer();

            normalizeMapKernel = device.LoadAutoGroupedStreamKernel<Index1, ArrayView<float>, float, float>(Kernels.Normalize);
            normalizeLightingKernel = device.LoadAutoGroupedStreamKernel<Index1, ArrayView<float>>(Kernels.NormalizeLighting);
            combineKernel = device.LoadAutoGroupedStreamKernel<Index1, ArrayView<float>, ArrayView<float>, ArrayView<int>>(Kernels.CombineLightingAndColor);
            outputKernel = device.LoadAutoGroupedStreamKernel<Index1, ArrayView<float>, ArrayView<byte>, Camera>(Kernels.CreatBitmap);
            outputZbufferKernel = device.LoadAutoGroupedStreamKernel<Index1, ArrayView<float>, ArrayView<byte>, Camera>(Kernels.CreateGrayScaleBitmap);
            TAAKernel = device.LoadAutoGroupedStreamKernel<Index1, dFramebuffer, dFramebuffer, float, float, int>(Kernels.NULLTAA);
            FilterKernel = device.LoadAutoGroupedStreamKernel<Index1, ArrayView<float>, ArrayView<float>, Camera, int>(Kernels.NULLLowPassFilter);
            renderKernel = device.LoadAutoGroupedStreamKernel<Index2, dFramebuffer, dWorldBuffer, Camera, int>(RTKernels.RenderKernel);

            startRenderThread();
        }

        public void setResolution(FrameManager frameManager, int width, int height, int maxBounce)
        {
            waitForRenderThreadLock(100000);

            frame = frameManager;
            if (frameData != null)
            {
                frameData.changeSize(width, height);
            }
            else
            {
                frameData = new FrameData(device, width, height, maxBounce);
            }

            output = new byte[width * height * 3];
        }

        private void initBestDevice(bool forceCPU)
        {
            if (!forceCPU)
            {
                var cudaAccelerators = CudaAccelerator.CudaAccelerators;

                if (cudaAccelerators.Length > 0)
                {
                    device = new CudaAccelerator(context);
                }
                else
                {
                    device = new CPUAccelerator(context);
                }
            }
            else
            {
                device = new CPUAccelerator(context);
            }
        }

        public void CameraUpdate(Vec3 movement, Vec3 turn)
        {
            frameData.camera = new Camera(frameData.camera, movement, turn);
        }

        public void startRenderThread()
        {
            RenderThread = new Thread(renderThreadMain);
            RenderThread.IsBackground = true;
            RenderThread.Start();
        }
        public void waitForRenderThreadLock(int msToWait)
        {
            if(RenderThread != null)
            {
                for (int i = 0; i < msToWait; i++)
                {
                    if (renderThreadLock)
                    {
                        return;
                    }

                    Thread.Sleep(1);
                }
            }
        }

        public void JoinRenderThread()
        {
            run = false;
            RenderThread.Join();
        }

        public void dispose()
        {
            JoinRenderThread();
            device.Dispose();
            context.Dispose();
        }

        private void renderThreadMain()
        {
            while (run)
            {
                if (pause)
                {
                    renderThreadLock = true;
                }
                else
                {
                    renderThreadLock = false;
                    renderThreadTimer.startUpdate();

                    generateFrame();
                    inputManager.Update();
                    frameData.camera = Camera.UpdateMovement(frameData.camera, inputManager);

                    frame.write(ref output);

                    renderThreadLock = true;
                    renderThreadTimer.endUpdateForTargetUpdateTime((1000.0 / targetFPS), true);
                }
            }

        }

        public void generateFrame()
        {
            renderKernel(new Index2(frameData.camera.width, frameData.camera.height), 
                frameData.framebuffer0.D,
                worldData.getDeviceWorldBuffer(), frameData.camera,
                tick);

            if (MainWindow.debugZbuffer)
            {
                (float min, float max) = Kernels.ReduceMax(device, frameData.framebuffer0.ZBuffer);
                normalizeMapKernel(frameData.framebuffer0.ZBuffer.Extent, frameData.framebuffer0.ZBuffer, min, max);
                outputZbufferKernel(frameData.framebuffer0.ZBuffer.Extent, frameData.framebuffer0.ZBuffer, frameData.bitmapData, frameData.camera);
            }
            else if (MainWindow.debugTAA)
            {
                normalizeLightingKernel(frameData.framebuffer0.LightingFrameBuffer.Extent / 3, 
                    frameData.framebuffer0.LightingFrameBuffer);

                combineKernel(frameData.framebuffer0.ColorFrameBuffer.Extent / 3,
                    frameData.framebuffer0.ColorFrameBuffer,
                    frameData.framebuffer0.LightingFrameBuffer,
                    frameData.framebuffer0.DrawableIDBuffer);

                TAAKernel(frameData.framebuffer0.ColorFrameBuffer.Extent / 3,
                    frameData.framebuffer0.D, frameData.framebuffer1.D,
                    MainWindow.debugTAADistScale, MainWindow.debugTAAScale, tick);

                outputKernel(frameData.framebuffer1.ColorFrameBuffer.Extent / 3, frameData.framebuffer1.ColorFrameBuffer, frameData.bitmapData, frameData.camera);
            }
            else if (MainWindow.debugLighting)
            {
                normalizeLightingKernel(frameData.framebuffer0.LightingFrameBuffer.Extent / 3,
                    frameData.framebuffer0.LightingFrameBuffer);

                outputKernel(frameData.framebuffer1.LightingFrameBuffer.Extent / 3, frameData.framebuffer0.LightingFrameBuffer, frameData.bitmapData, frameData.camera);
            }
            else
            {
                normalizeLightingKernel(frameData.framebuffer0.LightingFrameBuffer.Extent / 3,
                    frameData.framebuffer0.LightingFrameBuffer);

                //combineKernel(frameData.framebuffer0.ColorFrameBuffer.Extent / 3,
                //    frameData.framebuffer0.ColorFrameBuffer,
                //    frameData.framebuffer0.LightingFrameBuffer,
                //    frameData.framebuffer0.DrawableIDBuffer);

                outputKernel(frameData.framebuffer0.ColorFrameBuffer.Extent / 3, frameData.framebuffer0.ColorFrameBuffer, frameData.bitmapData, frameData.camera);
            }

            device.Synchronize();

            if(frameData.bitmapData.Length == output.Length)
            {
                frameData.bitmapData.CopyTo(output, 0, 0, frameData.bitmapData.Length);
            }

            tick++;
        }


    }
}