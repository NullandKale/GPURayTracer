using ILGPU;
using ILGPU.Runtime;
using System;
using System.Collections.Generic;
using System.Text;

namespace GPURayTracer.Rendering.Primitives
{
    public struct dFramebuffer
    {
        public ArrayView<float> ColorFrameBuffer;
        public ArrayView<float> LightingFrameBuffer;
        public ArrayView<float> ZBuffer;
        public ArrayView<int>   DrawableIDBuffer;

        public dFramebuffer(hFramebuffer _Framebuffer)
        {
            ColorFrameBuffer = _Framebuffer.ColorFrameBuffer;
            LightingFrameBuffer = _Framebuffer.LightingFrameBuffer;
            ZBuffer = _Framebuffer.ZBuffer;
            DrawableIDBuffer = _Framebuffer.DrawableIDBuffer;
        }
    }

    public class hFramebuffer
    {
        public MemoryBuffer<float> ColorFrameBuffer;
        public MemoryBuffer<float> LightingFrameBuffer;
        public MemoryBuffer<float> ZBuffer;
        public MemoryBuffer<int> DrawableIDBuffer;
        public dFramebuffer D;

        public hFramebuffer(Accelerator device, int width, int height)
        {
            ColorFrameBuffer = device.Allocate<float>(width * height * 3);
            LightingFrameBuffer = device.Allocate<float>(width * height * 3);
            ZBuffer = device.Allocate<float>(width * height);
            DrawableIDBuffer = device.Allocate<int>(width * height);
            D = new dFramebuffer(this);
        }

        public dFramebuffer GetRef()
        {
            return D;
        }

        public void Dispose()
        {
            ColorFrameBuffer.Dispose();
            LightingFrameBuffer.Dispose();
            ZBuffer.Dispose();
            DrawableIDBuffer.Dispose();
        }
    }
}
