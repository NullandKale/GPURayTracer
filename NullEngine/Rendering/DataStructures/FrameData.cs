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

        public MemoryBuffer<float> colorBuffer;
        public MemoryBuffer<float> lightBuffer;
        public MemoryBuffer<float> depthBuffer;

        public MemoryBuffer<float> outputBuffer;


        public dFrameData deviceFrameData;

        public FrameData(Accelerator device, int width, int height)
        {
            this.width = width;
            this.height = height;

            colorBuffer = device.Allocate<float>(width * height * 3);
            lightBuffer = device.Allocate<float>(width * height * 3);
            depthBuffer = device.Allocate<float>(width * height * 3);
            outputBuffer = device.Allocate<float>(width * height * 3);

            deviceFrameData = new dFrameData(width, height, colorBuffer, lightBuffer, depthBuffer, outputBuffer);
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
        public ArrayView<float> colorBuffer;
        public ArrayView<float> lightBuffer;
        public ArrayView<float> depthBuffer;
        public ArrayView<float> outputBuffer;

        public dFrameData(int width, int height, ArrayView<float> colorBuffer, ArrayView<float> lightBuffer, ArrayView<float> depthBuffer, ArrayView<float> outputBuffer)
        {
            this.width = width;
            this.height = height;
            this.colorBuffer = colorBuffer;
            this.lightBuffer = lightBuffer;
            this.depthBuffer = depthBuffer;
            this.outputBuffer = outputBuffer;
        }
    }
}
