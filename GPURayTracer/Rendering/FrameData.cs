using ILGPU;
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

        public MemoryBuffer<float> ColorFrameBuffer0;
        public MemoryBuffer<float> LightingFrameBuffer0;
        public MemoryBuffer<float> ZBuffer0;
        public MemoryBuffer<int> SphereIDBuffer0;

        public MemoryBuffer<float> ColorFrameBuffer1;
        public MemoryBuffer<float> LightingFrameBuffer1;
        public MemoryBuffer<float> ZBuffer1;
        public MemoryBuffer<int> SphereIDBuffer1;

        public MemoryBuffer<float> finalFrameBuffer;
        public MemoryBuffer<byte> bitmapData;
        public MemoryBuffer<float> rngData;
        public FrameData(Accelerator device, int width, int height, int extraRenderPasses, int maxBounces)
        {
            this.device = device;
            changeSize(width, height, extraRenderPasses, maxBounces);
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

        public void changeSize(int width, int height, int extraRenderPasses, int maxBounces)
        {
            camera = new Camera(new Vec3(0, 0, -4), new Vec3(0,0,0), Vec3.unitVector(new Vec3(0, 1, 0)), width, height, maxBounces, extraRenderPasses, 40f);
            
            finalFrameBuffer = device.Allocate<float>(width * height * 3);

            ColorFrameBuffer0 = device.Allocate<float>(width * height * 3);
            LightingFrameBuffer0 = device.Allocate<float>(width * height * 3);
            ZBuffer0 = device.Allocate<float>(width * height);
            SphereIDBuffer0 = device.Allocate<int>(width * height);

            ColorFrameBuffer1 = device.Allocate<float>(width * height * 3);
            LightingFrameBuffer1 = device.Allocate<float>(width * height * 3);
            ZBuffer1 = device.Allocate<float>(width * height);
            SphereIDBuffer1 = device.Allocate<int>(width * height);

            bitmapData = device.Allocate<byte>(width * height * 3);
        }

        public void Dispose()
        {
            finalFrameBuffer.Dispose();
            
            ColorFrameBuffer0.Dispose();
            LightingFrameBuffer0.Dispose();
            ZBuffer0.Dispose();
            SphereIDBuffer0.Dispose();
            
            ColorFrameBuffer1.Dispose();
            LightingFrameBuffer1.Dispose();
            ZBuffer1.Dispose();
            SphereIDBuffer1.Dispose();
            
            bitmapData.Dispose();
        }
    }
}
