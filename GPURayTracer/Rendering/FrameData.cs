using GPURayTracer.Rendering.Primitives;
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

        public hFramebuffer framebuffer0;
        public hFramebuffer framebuffer1;

        public MemoryBuffer<float> finalFrameBuffer;
        public MemoryBuffer<byte> bitmapData;
        public FrameData(Accelerator device, int width, int height, int extraRenderPasses, int maxBounces)
        {
            this.device = device;
            changeSize(width, height, extraRenderPasses, maxBounces);
        }

        public void changeSize(int width, int height, int extraRenderPasses, int maxBounces)
        {
            camera = new Camera(new Vec3(0, 0, -4), new Vec3(0,0,0), Vec3.unitVector(new Vec3(0, 1, 0)), width, height, maxBounces, extraRenderPasses, 40f);
            
            finalFrameBuffer = device.Allocate<float>(width * height * 3);

            framebuffer0 = new hFramebuffer(device, width, height);
            framebuffer1 = new hFramebuffer(device, width, height);

            bitmapData = device.Allocate<byte>(width * height * 3);
        }

        public dFramebuffer getFrameBuffer(int bufferID)
        {
            switch (bufferID)
            {
                case 0:
                    return framebuffer0.GetRef();
                case 1:
                    return framebuffer1.GetRef();
            }

            return framebuffer0.GetRef();
        }

        public void Dispose()
        {
            finalFrameBuffer.Dispose();

            framebuffer0.Dispose();
            framebuffer1.Dispose();

            bitmapData.Dispose();
        }
    }
}
