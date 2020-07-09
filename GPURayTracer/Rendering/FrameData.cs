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
        public MemoryBuffer<float> frameBuffer;
        public MemoryBuffer<byte> bitmapData;

        public FrameData(Accelerator device, int width, int height)
        {
            this.device = device;
            changeSize(width, height);
        }

        public void changeSize(int width, int height)
        {
            camera = new Camera(new Vec3(0, 0, -5), new Vec3(0, 0, 0), Vec3.unitVector(new Vec3(0, 1, 0)), width, height, 40f, new Vec3(), 0);
            frameBuffer = device.Allocate<float>(camera.width * camera.height * 3);
            bitmapData = device.Allocate<byte>(camera.width * camera.height * 3);
        }

        public void Dispose()
        {
            frameBuffer.Dispose();
            bitmapData.Dispose();
        }
    }
}
