using ILGPU;
using ILGPU.Runtime;
using System;
using System.Collections.Generic;
using System.Text;

namespace NullEngine.Rendering.DataStructures
{
    public class FrameData
    {
        public int width;
        public int height;

        public MemoryBuffer1D<Ray, Stride1D.Dense> rayBuffer;

        public MemoryBuffer1D<float, Stride1D.Dense> colorBuffer;
        public MemoryBuffer1D<float, Stride1D.Dense> lightBuffer;
        public MemoryBuffer1D<float, Stride1D.Dense> depthBuffer;

        public MemoryBuffer1D<float, Stride1D.Dense> outputBuffer;


        public dFrameData deviceFrameData;

        public FrameData(Accelerator device, int width, int height)
        {
            this.width = width;
            this.height = height;

            rayBuffer = device.Allocate1D<Ray>(width * height);

            colorBuffer = device.Allocate1D<float>(width * height * 3);
            lightBuffer = device.Allocate1D<float>(width * height * 3);
            depthBuffer = device.Allocate1D<float>(width * height * 3);

            outputBuffer = device.Allocate1D<float>(width * height * 3);

            deviceFrameData = new dFrameData(this);
        }

        public void Dispose()
        {
            colorBuffer.Dispose();
            lightBuffer.Dispose();
            depthBuffer.Dispose();
            outputBuffer.Dispose();
        }
    }

    public struct dFrameData
    {
        public int width;
        public int height;
        public ArrayView1D<Ray, Stride1D.Dense> rayBuffer;
        public ArrayView1D<float, Stride1D.Dense> colorBuffer;
        public ArrayView1D<float, Stride1D.Dense> lightBuffer;
        public ArrayView1D<float, Stride1D.Dense> depthBuffer;

        public ArrayView1D<float, Stride1D.Dense> outputBuffer;

        public dFrameData(FrameData frameData)
        {
            width = frameData.width;
            height = frameData.height;

            rayBuffer = frameData.rayBuffer;
            colorBuffer = frameData.colorBuffer;
            lightBuffer = frameData.lightBuffer;
            depthBuffer = frameData.depthBuffer;
            outputBuffer = frameData.outputBuffer;
        }
    }
}
