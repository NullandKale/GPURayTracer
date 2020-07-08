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
            camera = new Camera(width, height);
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
