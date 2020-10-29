using ILGPU;
using ILGPU.Runtime;
using ILGPU.Runtime.Cuda;
using ILGPU.Runtime.CPU;
using System;
using System.Collections.Generic;
using System.Text;
using NullEngine.Rendering.DataStructures;

namespace NullEngine.Rendering.Implementation
{
    public class GPU
    {
        public Context context;
        public Accelerator device;

        public Action<Index1, dByteFrameBuffer, dFrameData, dRenderData> generateFrame;
        public GPU(bool forceCPU)
        {
            context = new Context(ContextFlags.AggressiveInlining | ContextFlags.FastMath);
            context.EnableAlgorithms();

            if(forceCPU || CudaAccelerator.CudaAccelerators.Length < 1)
            {
                device = new CPUAccelerator(context);
            }
            else
            {
                device = new CudaAccelerator(context);
            }

            initRenderKernels();
        }

        private void initRenderKernels()
        {
            generateFrame = device.LoadAutoGroupedStreamKernel<Index1, dByteFrameBuffer, dFrameData, dRenderData>(GPUKernels.GenerateFrame);
        }

        public void Dispose()
        {
            device.Dispose();
            context.Dispose();
        }

        public void Render(ByteFrameBuffer output, RenderDataManager renderDataManager, FrameData frameData)
        {
            generateFrame(output.memoryBuffer.Length / 3, output.frameBuffer, frameData.deviceFrameData, renderDataManager.getDeviceRenderData());
            device.Synchronize();
        }
    }

    public static class GPUKernels
    {
        public static void GenerateFrame(Index1 pixel, dByteFrameBuffer output, dFrameData frameData, dRenderData renderData)
        {
            Vec3 color = UtilityKernels.readFrameBuffer(frameData.outputBuffer, pixel * 3);
            output.writeFrameBuffer(pixel * 3, color.x, color.y, color.z);
        }

    }
}
