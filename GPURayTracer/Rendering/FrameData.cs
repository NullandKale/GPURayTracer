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
        public MemoryBuffer<float> frameBufferDiffuse;
        public MemoryBuffer<float> frameBufferEmmissive;
        public MemoryBuffer<byte> bitmapData;

        public FrameData(Accelerator device, int width, int height, bool diffuse)
        {
            this.device = device;
            changeSize(width, height, diffuse);
        }

        public void changeSize(int width, int height, bool diffuse)
        {
            camera = new Camera(new Vec3(0, 0, -3.2f), new Vec3(0, 0, 0), Vec3.unitVector(new Vec3(0, 1, 0)), width, height, 1, 2, 40f, diffuse, new Vec3(), 0);
            frameBufferDiffuse = device.Allocate<float>(width * height * 3);
            frameBufferEmmissive = device.Allocate<float>(width * height * 3);
            bitmapData = device.Allocate<byte>(width * height * 3);
        }

        public void Dispose()
        {
            frameBufferDiffuse.Dispose();
            frameBufferEmmissive.Dispose();
            bitmapData.Dispose();
        }
    }
}
