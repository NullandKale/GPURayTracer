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

        public Action<Index1D, dByteFrameBuffer, dFrameData> generateFrame;
        public Action<Index1D, Camera, dFrameData> generatePrimaryRays;
        public GPU(bool forceCPU)
        {
            context = Context.Create(builder => builder.Cuda().CPU().EnableAlgorithms());
            device = context.GetPreferredDevice(preferCPU: false)
                                      .CreateAccelerator(context);

            initRenderKernels();
        }

        private void initRenderKernels()
        {
            generateFrame = device.LoadAutoGroupedStreamKernel<Index1D, dByteFrameBuffer, dFrameData>(GPUKernels.GenerateFrame);
            generatePrimaryRays = device.LoadAutoGroupedStreamKernel<Index1D, Camera, dFrameData>(GPUKernels.GeneratePrimaryRays);
        }

        public void Dispose()
        {
            device.Dispose();
            context.Dispose();
        }

        public void Render(Camera camera, dByteFrameBuffer output, dRenderData renderData, dFrameData frameData)
        {
            generatePrimaryRays(output.width * output.height, camera, frameData);
            generateFrame(output.height * output.width, output, frameData);
            device.Synchronize();
        }
    }

    public static class GPUKernels
    {
        public static void GeneratePrimaryRays(Index1D pixel, Camera camera, dFrameData frameData)
        {
            float x = ((float)(pixel % camera.width)) / camera.width;
            float y = ((float)(pixel / camera.width)) / camera.height;

            frameData.rayBuffer[pixel] = camera.GetRay(x, y);
        }

        public static void GenerateFrame(Index1D pixel, dByteFrameBuffer output, dFrameData frameData)
        {
            Vec3 color = UtilityKernels.readFrameBuffer(frameData.outputBuffer, pixel * 3);
            output.writeFrameBuffer(pixel * 3, color.x, color.y, color.z);
        }

    }
}
