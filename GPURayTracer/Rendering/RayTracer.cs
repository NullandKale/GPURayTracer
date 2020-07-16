﻿using GPURayTracer.Rendering.Primitives;
using GPURayTracer.Utils;
using ILGPU;
using ILGPU.Algorithms;
using ILGPU.Algorithms.Random;
using ILGPU.IR.Types;
using ILGPU.Runtime;
using ILGPU.Runtime.CPU;
using ILGPU.Runtime.Cuda;
using ILGPU.Runtime.OpenCL;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Text;
using System.Threading;
using System.Windows.Input;

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

        Action<Index1, ArrayView<float>, ArrayView<float>, ArrayView<MaterialData>, ArrayView<Sphere>, Camera, int> renderKernel;
        Action<Index1, ArrayView<float>, ArrayView<byte>, Camera> outputKernel;
        Action<Index1, ArrayView<float>, ArrayView<float>, float, int, Camera> filterKernel;

        public byte[] output;
        FrameManager frame;

        FrameData frameData;
        WorldData worldData;

        int targetFPS;
        int tick = 0;

        public UpdateStatsTimer rFPStimer;

        public RayTracer(FrameManager frameManager, int width, int height, int targetFPS, int MSAA, int maxBounce, bool forceCPU)
        {
            context = new Context();
            context.EnableAlgorithms();
            initBestDevice(forceCPU);

            frameData = new FrameData(device, width, height, MSAA, maxBounce);
            worldData = new WorldData(device);

            this.targetFPS = targetFPS;

            run = true;
            frame = frameManager;

            output = new byte[width * height * 3];

            rFPStimer = new UpdateStatsTimer();

            outputKernel = device.LoadAutoGroupedStreamKernel<Index1, ArrayView<float>, ArrayView<byte>, Camera>(Kernels.CreatBitmap);
            filterKernel = device.LoadAutoGroupedStreamKernel<Index1, ArrayView<float>, ArrayView<float>, float, int, Camera>(Kernels.RadianceFuzz);
            renderKernel = device.LoadAutoGroupedStreamKernel<Index1, ArrayView<float>, ArrayView<float>, ArrayView<MaterialData>, ArrayView<Sphere>, Camera, int>(RTKernels.RenderKernel);

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

                    frame.write(ref output);

                    rFPStimer.endUpdateForTargetUpdateTime((1000.0 / targetFPS), true);
                    ready = true;
                }
            }

        }

        public void generateFrame()
        {
            renderKernel(frameData.rawFrameBuffer.Extent / 3, frameData.rawFrameBuffer, frameData.rngData, worldData.getDeviceMaterials(), worldData.getDeviceSpheres(), frameData.camera, tick++);

            //Test filter
            //filterKernel(frameData.rawFrameBuffer.Extent / 3, frameData.rawFrameBuffer, frameData.filteredFrameBuffer, float.Epsilon, 0, frameData.camera);

            //outputKernel(frameData.filteredFrameBuffer.Extent / 3, frameData.filteredFrameBuffer, frameData.bitmapData, frameData.camera);

            outputKernel(frameData.rawFrameBuffer.Extent / 3, frameData.rawFrameBuffer, frameData.bitmapData, frameData.camera);

            device.Synchronize();

            frameData.bitmapData.CopyTo(output, 0, 0, frameData.bitmapData.Length);
        }
    }
}