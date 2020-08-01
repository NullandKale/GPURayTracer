using GPURayTracer.Rendering.GPUStructs;
using GPURayTracer.Rendering.Primitives;
using GPURayTracer.Utils;
using ILGPU;
using ILGPU.Algorithms;
using ILGPU.Algorithms.Random;
using ILGPU.IR.Types;
using ILGPU.Runtime;
using ILGPU.Runtime.CPU;
using ILGPU.Runtime.Cuda;
using ILGPU.Runtime.OpenCL;
using Simplex;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;

namespace GPURayTracer.Rendering
{
    public class RayTracer
    {
        public bool run = true;
        public bool pause = false;
        public bool ready = false;

        private Thread RenderThread;

        public Context context;
        public Accelerator device;

        Action<Index2, dFramebuffer, ArrayView<float>, WorldBuffer, Camera, int> renderKernel;
        Action<Index1, dFramebuffer, dFramebuffer, float, float, int> TAAKernel;
        Action<Index1, ArrayView<float>, ArrayView<float>, Camera, int> FilterKernel;
        Action<Index1, ArrayView<float>, float, float> normalizeMapKernel;
        Action<Index1, ArrayView<float>> normalizeLightingKernel;
        Action<Index1, ArrayView<float>, ArrayView<float>, ArrayView<int>> combineKernel;
        Action<Index1, ArrayView<float>, ArrayView<byte>, Camera> outputKernel;
        Action<Index1, ArrayView<float>, ArrayView<byte>, Camera> outputZbufferKernel;
        Action<Index1, int, float, ArrayView<float>, ArrayView<byte>, float> generateKernel;

        public byte[] output;
        FrameManager frame;

        public FrameData frameData;
        WorldData worldData;

        int targetFPS;
        int tick = 0;
        Random random;

        public UpdateStatsTimer rFPStimer;
        public InputManager inputManager;

        public RayTracer(FrameManager frameManager, int width, int height, int targetFPS, int extraRenderPasses, int maxBounce, bool forceCPU)
        {
            context = new Context();
            context.EnableAlgorithms();
            initBestDevice(forceCPU);

            random = new Random();

            frameData = new FrameData(device, width, height, extraRenderPasses, maxBounce);
            worldData = new WorldData(device);
            inputManager = new InputManager();

            this.targetFPS = targetFPS;

            run = true;
            frame = frameManager;

            output = new byte[width * height * 3];

            rFPStimer = new UpdateStatsTimer();

            generateKernel = device.LoadAutoGroupedStreamKernel<Index1, int, float, ArrayView<float>, ArrayView<byte>, float>(Noise.GenerateKernel);
            normalizeMapKernel = device.LoadAutoGroupedStreamKernel<Index1, ArrayView<float>, float, float>(Kernels.Normalize);
            normalizeLightingKernel = device.LoadAutoGroupedStreamKernel<Index1, ArrayView<float>>(Kernels.NormalizeLighting);
            combineKernel = device.LoadAutoGroupedStreamKernel<Index1, ArrayView<float>, ArrayView<float>, ArrayView<int>>(Kernels.CombineLightingAndColor);
            outputKernel = device.LoadAutoGroupedStreamKernel<Index1, ArrayView<float>, ArrayView<byte>, Camera>(Kernels.CreatBitmap);
            outputZbufferKernel = device.LoadAutoGroupedStreamKernel<Index1, ArrayView<float>, ArrayView<byte>, Camera>(Kernels.CreateGrayScaleBitmap);
            TAAKernel = device.LoadAutoGroupedStreamKernel<Index1, dFramebuffer, dFramebuffer, float, float, int>(Kernels.NULLTAA);
            FilterKernel = device.LoadAutoGroupedStreamKernel<Index1, ArrayView<float>, ArrayView<float>, Camera, int>(Kernels.NULLLowPassFilter);
            renderKernel = device.LoadAutoGroupedStreamKernel<Index2, dFramebuffer, ArrayView<float>, WorldBuffer, Camera, int>(RTKernels.RenderKernel);

            startRenderThread();
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
        public void waitForReady()
        {
            for (int i = 0; i < 100; i++)
            {
                if (ready)
                {
                    return;
                }

                Thread.Sleep(1);
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
            if(MainWindow.debugRandomGeneration)
            {
                generateRandomness();
            }

            while (run)
            {
                if (pause)
                {
                    ready = true;
                }
                else
                {
                    ready = false;
                    rFPStimer.startUpdate();

                    generateFrame();
                    inputManager.Update();
                    frameData.camera = Camera.UpdateMovement(frameData.camera, inputManager);

                    frame.write(ref output);

                    rFPStimer.endUpdateForTargetUpdateTime((1000.0 / targetFPS), true);
                    ready = true;
                }
            }

        }

        public void generateRandomness()
        {
            using (var rng = device.Allocate<byte>(512))
            {
                byte[] bytes = new byte[512];
                random.NextBytes(bytes);
                rng.CopyFrom(bytes, 0, 0, 512);
                generateKernel(frameData.rngData.Extent, frameData.rngData.Extent / 100, 0.1f, frameData.rngData, rng, 1);
                (float min, float max) = Kernels.ReduceMax(device, frameData.rngData);
                normalizeMapKernel(frameData.rngData.Extent, frameData.rngData, min, max);
            }
        }

        public void generateFrame()
        {
            renderKernel(new Index2(frameData.camera.width, frameData.camera.height), 
                frameData.framebuffer0.D, 
                frameData.rngData, worldData.GetWorldBuffer(), frameData.camera,
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

                FilterKernel(frameData.framebuffer1.ColorFrameBuffer.Extent / 3,
                    frameData.framebuffer1.ColorFrameBuffer,
                    frameData.finalFrameBuffer,
                    frameData.camera, 2);

                outputKernel(frameData.finalFrameBuffer.Extent / 3, frameData.finalFrameBuffer, frameData.bitmapData, frameData.camera);
            }
            else if (MainWindow.debugLighting)
            {
                FilterKernel(frameData.framebuffer0.LightingFrameBuffer.Extent / 3,
                    frameData.framebuffer0.LightingFrameBuffer,
                    frameData.framebuffer1.LightingFrameBuffer,
                    frameData.camera, 0);

                outputKernel(frameData.framebuffer1.LightingFrameBuffer.Extent / 3, frameData.framebuffer0.LightingFrameBuffer, frameData.bitmapData, frameData.camera);
            }
            else
            {
                normalizeLightingKernel(frameData.framebuffer0.LightingFrameBuffer.Extent / 3,
                    frameData.framebuffer0.LightingFrameBuffer);

                combineKernel(frameData.framebuffer0.ColorFrameBuffer.Extent / 3,
                    frameData.framebuffer0.ColorFrameBuffer,
                    frameData.framebuffer0.LightingFrameBuffer,
                    frameData.framebuffer0.DrawableIDBuffer);

                outputKernel(frameData.framebuffer0.ColorFrameBuffer.Extent / 3, frameData.framebuffer0.ColorFrameBuffer, frameData.bitmapData, frameData.camera);
            }

            device.Synchronize();

            frameData.bitmapData.CopyTo(output, 0, 0, frameData.bitmapData.Length);
            tick++;
        }


    }
}