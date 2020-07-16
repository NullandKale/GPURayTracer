﻿using ILGPU;
using ILGPU.Runtime;
using System;
using System.Collections.Generic;
using System.Text;

namespace GPURayTracer.Rendering
{
    public class FrameData
    {
        private Accelerator device;

        public Camera camera;
        public MemoryBuffer<float> rawFrameBuffer;
        public MemoryBuffer<float> filteredFrameBuffer;
        public MemoryBuffer<byte> bitmapData;
        public MemoryBuffer<float> rngData;
        public FrameData(Accelerator device, int width, int height, int MSAA, int maxBounces)
        {
            this.device = device;
            changeSize(width, height, MSAA, maxBounces);
            initRandomness();
        }

        public void initRandomness()
        {
            int rngSize = 1024 * 1024;

            float[] rngData;
            rngData = new float[rngSize];

            Random rng = new Random();

            for (int i = 0; i < rngSize; i++)
            {
                rngData[i] = (float)rng.NextDouble();
            }

            this.rngData = device.Allocate<float>(rngData.Length);
            this.rngData.CopyFrom(rngData, Index1.Zero, Index1.Zero, this.rngData.Extent);
        }

        public void changeSize(int width, int height, int MSAA, int maxBounces)
        {
            camera = new Camera(new Vec3(0, 0, -4), new Vec3(0,0,0), Vec3.unitVector(new Vec3(0, 1, 0)), width, height, maxBounces, MSAA, 40f, new Vec3(), 0);
            rawFrameBuffer = device.Allocate<float>(width * height * 3);
            filteredFrameBuffer = device.Allocate<float>(width * height * 3);
            bitmapData = device.Allocate<byte>(width * height * 3);
        }

        public void Dispose()
        {
            rawFrameBuffer.Dispose();
            bitmapData.Dispose();
        }
    }
}
