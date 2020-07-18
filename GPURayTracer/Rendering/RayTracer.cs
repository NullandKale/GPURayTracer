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

        Action<Index1, ArrayView<float>, ArrayView<float>, ArrayView<int>, ArrayView<float>, ArrayView<MaterialData>, ArrayView<Sphere>, ArrayView<Triangle>, ArrayView<Triangle>, Camera, int> renderKernel;
        Action<Index1, ArrayView<float>, ArrayView<float>, ArrayView<int>, ArrayView<float>, ArrayView<MaterialData>, ArrayView<Sphere>, ArrayView<Triangle>, ArrayView<Triangle>, Camera, int> renderKernelSecondaryPass;
        Action<Index1, ArrayView<float>, ArrayView<float>, ArrayView<int>, ArrayView<float>, ArrayView<float>, ArrayView<int>, float, float, int> filterKernel;
        Action<Index1, ArrayView<float>, float, float> normalizeKernel;
        Action<Index1, ArrayView<float>, ArrayView<byte>, Camera> outputKernel;
        Action<Index1, ArrayView<float>, ArrayView<byte>, Camera> outputZbufferKernel;
        Action<Index1, int, float, ArrayView<float>, ArrayView<byte>, float> generateKernel;

        public byte[] output;
        FrameManager frame;

        public FrameData frameData;
        WorldData worldData;

        int targetFPS;
        int tick = 0;

        public UpdateStatsTimer rFPStimer;
        public InputManager inputManager;

        public RayTracer(FrameManager frameManager, int width, int height, int targetFPS, int extraRenderPasses, int maxBounce, bool forceCPU)
        {
            context = new Context();
            context.EnableAlgorithms();
            initBestDevice(forceCPU);

            frameData = new FrameData(device, width, height, extraRenderPasses, maxBounce);
            worldData = new WorldData(device);
            inputManager = new InputManager();

            this.targetFPS = targetFPS;

            run = true;
            frame = frameManager;

            output = new byte[width * height * 3];

            rFPStimer = new UpdateStatsTimer();

            generateKernel = device.LoadAutoGroupedStreamKernel<Index1, int, float, ArrayView<float>, ArrayView<byte>, float>(Noise.GenerateKernel);
            normalizeKernel = device.LoadAutoGroupedStreamKernel<Index1, ArrayView<float>, float, float>(Kernels.Normalize);
            outputKernel = device.LoadAutoGroupedStreamKernel<Index1, ArrayView<float>, ArrayView<byte>, Camera>(Kernels.CreatBitmap);
            outputZbufferKernel = device.LoadAutoGroupedStreamKernel<Index1, ArrayView<float>, ArrayView<byte>, Camera>(Kernels.CreateGrayScaleBitmap);
            filterKernel = device.LoadAutoGroupedStreamKernel
                <Index1, ArrayView<float>, ArrayView<float>, ArrayView<int>, ArrayView<float>, ArrayView<float>, ArrayView<int>, float, float, int>
                (Kernels.NULLTAA);
            renderKernel = device.LoadAutoGroupedStreamKernel
                <Index1, ArrayView<float>, ArrayView<float>, ArrayView<int>, ArrayView<float>, ArrayView<MaterialData>, ArrayView<Sphere>, ArrayView<Triangle>, ArrayView<Triangle>, Camera, int>
                (RTKernels.RenderKernel);
            renderKernelSecondaryPass = device.LoadAutoGroupedStreamKernel
                <Index1, ArrayView<float>, ArrayView<float>, ArrayView<int>, ArrayView<float>, ArrayView<MaterialData>, ArrayView<Sphere>, ArrayView<Triangle>, ArrayView<Triangle>, Camera, int>
                (RTKernels.RenderKernelSecondaryPass);

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
                Random random = new Random(0);
                random.NextBytes(bytes);
                rng.CopyFrom(bytes, 0, 0, 512);
                generateKernel(frameData.rngData.Extent, frameData.rngData.Extent / 100, 0.1f, frameData.rngData, rng, 1);
                (float min, float max) = Kernels.ReduceMax(device, frameData.rngData);
                normalizeKernel(frameData.rngData.Extent, frameData.rngData, min, max);
            }
        }

        public void generateFrame()
        {
            renderKernel(frameData.ColorFrameBuffer0.Extent / 3, 
                frameData.ColorFrameBuffer0, frameData.ZBuffer0, frameData.SphereIDBuffer0, 
                frameData.rngData, worldData.getDeviceMaterials(), worldData.getDeviceSpheres(), worldData.getDeviceTriangles(), worldData.getDeviceTriNormals(), frameData.camera,
                tick);

            for(int i = 0; i < frameData.camera.extraRenderPasses; i++)
            {
                renderKernelSecondaryPass(frameData.ColorFrameBuffer0.Extent / 3,
                    frameData.ColorFrameBuffer0, frameData.ZBuffer0, frameData.SphereIDBuffer0,
                    frameData.rngData, worldData.getDeviceMaterials(), worldData.getDeviceSpheres(), worldData.getDeviceTriangles(), worldData.getDeviceTriNormals(), frameData.camera,
                    tick + i + 1);
            }

            if (MainWindow.debugZbuffer)
            {
                (float min, float max) = Kernels.ReduceMax(device, frameData.ZBuffer0);
                normalizeKernel(frameData.ZBuffer0.Extent, frameData.ZBuffer0, min, max);
                outputZbufferKernel(frameData.ZBuffer0.Extent, frameData.ZBuffer0, frameData.bitmapData, frameData.camera);
            }
            else if (MainWindow.debugTAA)
            {
                filterKernel(frameData.ColorFrameBuffer0.Extent / 3,
                    frameData.ColorFrameBuffer0, frameData.ZBuffer0, frameData.SphereIDBuffer0,
                    frameData.ColorFrameBuffer1, frameData.ZBuffer1, frameData.SphereIDBuffer1,
                    0.5f, MainWindow.debugTAAScale, tick);
                outputKernel(frameData.ColorFrameBuffer1.Extent / 3, frameData.ColorFrameBuffer1, frameData.bitmapData, frameData.camera);
            }
            else
            {
                outputKernel(frameData.ColorFrameBuffer0.Extent / 3, frameData.ColorFrameBuffer0, frameData.bitmapData, frameData.camera);
            }

            device.Synchronize();

            frameData.bitmapData.CopyTo(output, 0, 0, frameData.bitmapData.Length);
            tick++;
        }


    }
}