using GPURayTracer.Rendering.Primitives;
using GPURayTracer.Utils;
using ILGPU;
using ILGPU.Algorithms;
using ILGPU.IR.Types;
using ILGPU.Runtime;
using ILGPU.Runtime.CPU;
using ILGPU.Runtime.Cuda;
using ILGPU.Runtime.OpenCL;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Text;
using System.Threading;
using System.Windows.Input;

namespace GPURayTracer.Rendering
{
    public class RayTracer
    {
        public bool run = true;
        public bool pause = false;
        public bool ready = false;

        private Thread t;
        public Context context;
        public Accelerator device;

        Action<Index1, ArrayView<float>, ArrayView<MaterialData>, ArrayView<Sphere>, ArrayView<Light>, Camera> renderKernel;
        Action<Index1, ArrayView<float>, ArrayView<byte>, Camera> outputKernel;

        public byte[] output;
        FrameManager frame;

        FrameData frameData;
        WorldData worldData;

        int targetFPS;

        public UpdateStatsTimer rFPStimer;

        public RayTracer()
        {
            context = new Context();
            context.EnableAlgorithms();
            initBestDevice(false);

            renderKernel = device.LoadAutoGroupedStreamKernel<Index1, ArrayView<float>, ArrayView<MaterialData>, ArrayView<Sphere>, ArrayView<Light>, Camera>(RenderKernel);
            outputKernel = device.LoadAutoGroupedStreamKernel<Index1, ArrayView<float>, ArrayView<byte>, Camera>(CreatBitmap);
        }

        private void initBestDevice(bool forceCPU)
        {
            if(!forceCPU)
            {
                var cudaAccelerators = CudaAccelerator.CudaAccelerators;

                if (cudaAccelerators.Length > 0)
                {
                    device = new CudaAccelerator(context);
                }
                else
                {
                    device = new CPUAccelerator(context);
                }
            }
            else
            {
                device = new CPUAccelerator(context);
            }
        }

        public void startRenderThread(FrameManager frameManager, int width, int height, int targetFPS)
        {
            frameData = new FrameData(device, width, height);
            worldData = new WorldData(device);

            this.targetFPS = targetFPS;

            run = true;
            frame = frameManager;

            output = new byte[width * height * 3];

            rFPStimer = new UpdateStatsTimer();

            t = new Thread(renderThreadMain);
            t.IsBackground = true;
            t.Start();
        }
        public void waitForReady()
        {
            for(int i = 0; i < 100; i++)
            {
                if(ready)
                {
                    return;
                }

                Thread.Sleep(1);
            }
        }

        public void JoinRenderThread()
        {
            run = false;
            t.Join();
        }

        public void dispose()
        {
            JoinRenderThread();
            device.Dispose();
            context.Dispose();
        }
        private void renderThreadMain()
        {
            while (run)
            {
                if (pause)
                {
                    ready = true;
                }
                else
                {
                    ready = false;
                    rFPStimer.startUpdate();

                    generateFrame();

                    frame.write(ref output);

                    rFPStimer.endUpdateForTargetUpdateTime((1000.0 / targetFPS), true);
                    ready = true;
                }
            }

        }

        public void generateFrame()
        {
            renderKernel(frameData.frameBuffer.Extent / 3, frameData.frameBuffer, worldData.getDeviceMaterials(), worldData.getDeviceSpheres(), worldData.getDeviceLights(), frameData.camera);
            outputKernel(frameData.frameBuffer.Extent / 3, frameData.frameBuffer, frameData.bitmapData, frameData.camera);

            device.Synchronize();

            frameData.bitmapData.CopyTo(output, 0, 0, frameData.bitmapData.Length);
        }

        public static void CreatBitmap(Index1 index, ArrayView<float> data, ArrayView<byte> bitmapData, Camera camera)
        {
            //FLIP Y
            //int x = (camera.width - 1) - ((index) % camera.width);
            int y = (camera.height - 1) - ((index) / camera.width);

            //NORMAL X
            int x = ((index) % camera.width);
            //int y = ((index) / camera.width);
            
            int newIndex = ((y * camera.width) + x);

            bitmapData[(newIndex * 3)] = (byte)(255.99f * data[(index * 3)]);
            bitmapData[(newIndex * 3) + 1] = (byte)(255.99f * data[(index * 3) + 1]);
            bitmapData[(newIndex * 3) + 2] = (byte)(255.99f * data[(index * 3) + 2]);
        }

        private static void RenderKernel(Index1 index, ArrayView<float> data, ArrayView<MaterialData> materials, ArrayView<Sphere> spheres, ArrayView<Light> lights, Camera camera)
        {
            // color cheatsheet
            //int r = data[(index * 3)];
            //int g = data[(index * 3) + 1];
            //int b = data[(index * 3) + 2];

            int x = ((index) % camera.width);
            int y = ((index) / camera.width);

            float u = (float)x / (float)(camera.width - 1);
            float v = ((float)y / (float)(camera.height - 1));

            Vec3 col = ColorRay(camera.GetRay(u,v), materials, spheres, lights, 0, 1);

            data[(index * 3)] = col.x;
            data[(index * 3) + 1] = col.y;
            data[(index * 3) + 2] = col.z;
        }

        private static HitRecord GetHit(Ray ray, ArrayView<Sphere> spheres, ArrayView<Light> lights)
        {

            HitRecord closestHit = HitRecord.badHit;

            for (int i = 0; i < spheres.Length; i++)
            {
                HitRecord rec = Sphere.hit(spheres[i], ray, 0.001f, float.MaxValue, closestHit);
                if (rec.t != -1)
                {
                    closestHit = rec;
                }
            }

            if (closestHit.t == -1)
            {
                return HitRecord.badHit;
            }
            else
            {
                return closestHit;
            }
        }

        private static Vec3 ColorRay(Ray ray, ArrayView<MaterialData> materials, ArrayView<Sphere> spheres, ArrayView<Light> lights, int depth, int maxDepth)
        {
            if(depth > maxDepth)
            {
                return new Vec3(1, 1, 1);
            }

            HitRecord closestHit = GetHit(ray, spheres, lights);

            if (closestHit.t == -1)
            {
                return new Vec3(0, 0, 0);
            }

            MaterialData material = materials[closestHit.materialID];

            Vec3 reflectDir;
            Vec3 reflectColor;
            Vec3 refractDir;
            Vec3 refractColor;

            reflectDir = Vec3.unitVector(Vec3.reflect(ray.b, closestHit.normal));
            reflectColor = ColorRay(new Ray(closestHit.p, reflectDir), materials, spheres, lights, depth + 1, maxDepth);

            refractDir = Vec3.refract(ray.b, closestHit.normal, materials[closestHit.materialID].ref_idx);

            if (reflectDir.x != 0 && reflectDir.y != 0 && reflectDir.z != 0)
            {
                refractDir = Vec3.unitVector(refractDir);
            }

            refractColor = ColorRay(new Ray(closestHit.p, refractDir), materials, spheres, lights, depth + 1, maxDepth);

            float diffuseLightIntensity = 0.5f;
            float specularLightIntensity = 0;

            for (int i = 0; i < lights.Length; i++)
            {
                Vec3 lightDir = Vec3.unitVector(lights[i].position - closestHit.p);
                double lightDist = (lights[i].position - closestHit.p).length();
                Vec3 shadowOrig = closestHit.p;
                HitRecord shadowRec = GetHit(new Ray(shadowOrig, lightDir), spheres, lights);

                if (shadowRec.t != -1 && (shadowRec.p - shadowOrig).length() < lightDist)
                {
                    continue;
                }

                diffuseLightIntensity += lights[i].intensity * XMath.Max(0.0f, Vec3.dot(lightDir, closestHit.normal));
                specularLightIntensity += XMath.Pow(XMath.Max(0.0f, Vec3.dot(-Vec3.reflect(-lightDir, closestHit.normal), ray.b)), material.specularExponent) * lights[i].intensity;
            }


            return material.diffuseColor * diffuseLightIntensity * material.a0 + new Vec3(1, 1, 1) * specularLightIntensity * material.a1 + reflectColor * material.a2 + refractColor * material.a3;
        }
    }
}
