using ILGPU;
using ILGPU.Runtime;
using ILGPU.Runtime.Cuda;
using ILGPU.Runtime.CPU;
using System;
using System.Collections.Generic;
using System.Text;
using NullEngine.Rendering.DataStructures;
using NullEngine.Rendering.DataStructures.BVH;

namespace NullEngine.Rendering.Implementation
{
    public class GPU
    {
        public Context context;
        public Accelerator device;
        public Action<Index1D, Camera, dFrameData> generatePrimaryRays;
        public Action<Index1D, dFrameData, dTLAS, dRenderData> hitRays;
        public Action<Index1D, dByteFrameBuffer, dFrameData> generateFrame;
        public GPU(bool forceCPU)
        {
            context = Context.Create(builder => builder.Cuda().CPU().Optimize(OptimizationLevel.O2).EnableAlgorithms());
            device = context.GetPreferredDevice(preferCPU: forceCPU)
                                      .CreateAccelerator(context);

            initRenderKernels();
        }

        private void initRenderKernels()
        {
            generateFrame = device.LoadAutoGroupedStreamKernel<Index1D, dByteFrameBuffer, dFrameData>(GPUKernels.GenerateFrame);
            hitRays = device.LoadAutoGroupedStreamKernel<Index1D, dFrameData, dTLAS, dRenderData>(GPUKernels.HitRays);
            generatePrimaryRays = device.LoadAutoGroupedStreamKernel<Index1D, Camera, dFrameData>(GPUKernels.GeneratePrimaryRays);
        }

        public void Dispose()
        {
            device.Dispose();
            context.Dispose();
        }

        public void Render(Camera camera, Scene scene, dByteFrameBuffer output, dFrameData frameData)
        {
            generatePrimaryRays(output.width * output.height, camera, frameData);
            hitRays(output.width * output.height, frameData, scene.tlas.GetDTLAS(), scene.tlas.renderDataManager.getDeviceRenderData());
            generateFrame(output.height * output.width, output, frameData);
            device.Synchronize();
        }
    }

    public static class GPUKernels
    {
        public static void GeneratePrimaryRays(Index1D pixel, Camera camera, dFrameData frameData)
        {
            float x = (pixel % camera.width);
            float y = (pixel / camera.width);

            frameData.rayBuffer[pixel] = camera.GetRay(x, y);
        }

        public static void HitRays(Index1D pixel, dFrameData frameData, dTLAS tlas, dRenderData renderData)
        {
            HitRecord hit = new HitRecord();
            hit.t = float.MaxValue;

            if(true)
            {
                for(int i = 0; i < tlas.meshes.Length; i++)
                {
                    dMesh mesh = tlas.meshes[i];
                    for(int j = 0; j < mesh.triangleLength; j++)
                    {
                        mesh.GetTriangle(j, renderData).GetTriangleHit(frameData.rayBuffer[pixel], j, ref hit);
                    }
                }
            }
            else
            {
                tlas.hit(renderData, frameData.rayBuffer[pixel], 0.01f, ref hit);
            }

            if (hit.t < float.MaxValue)
            {
                Vec3 color = UtilityKernels.GetRandomColor(hit.drawableID);
                frameData.outputBuffer[(pixel * 3)]     = color.x;
                frameData.outputBuffer[(pixel * 3) + 1] = color.y;
                frameData.outputBuffer[(pixel * 3) + 2] = color.z;
            }
        }

        public static void GenerateFrame(Index1D pixel, dByteFrameBuffer output, dFrameData frameData)
        {
            Vec3 color = UtilityKernels.readFrameBuffer(frameData.outputBuffer, pixel * 3);
            //color = Vec3.aces_approx(color);
            output.writeFrameBuffer(pixel * 3, color.x, color.y, color.z);
        }

    }
}
