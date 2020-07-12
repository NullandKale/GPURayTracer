using GPURayTracer.Rendering.Primitives;
using GPURayTracer.Utils;
using ILGPU;
using ILGPU.Algorithms;
using ILGPU.Algorithms.Random;
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

        private Thread RenderThread;
        public Context context;
        public Accelerator device;

        Action<Index1, ArrayView<float>, ArrayView<float>, ArrayView<MaterialData>, ArrayView<Sphere>, Camera> renderKernel;
        Action<Index1, ArrayView<float>, ArrayView<byte>, Camera> outputKernel;

        public byte[] output;
        FrameManager frame;

        FrameData frameData;
        WorldData worldData;

        int targetFPS;

        public UpdateStatsTimer rFPStimer;

        public RayTracer(FrameManager frameManager, int width, int height, int targetFPS, bool diffuse, bool forceCPU)
        {
            context = new Context();
            context.EnableAlgorithms();
            initBestDevice(forceCPU);

            frameData = new FrameData(device, width, height, diffuse);
            worldData = new WorldData(device);

            this.targetFPS = targetFPS;

            run = true;
            frame = frameManager;

            output = new byte[width * height * 3];

            rFPStimer = new UpdateStatsTimer();

            outputKernel = device.LoadAutoGroupedStreamKernel<Index1, ArrayView<float>, ArrayView<byte>, Camera>(CreatBitmap);
            renderKernel = device.LoadAutoGroupedStreamKernel<Index1, ArrayView<float>, ArrayView<float>, ArrayView<MaterialData>, ArrayView<Sphere>, Camera>(RenderKernel);

            startRenderThread();
        }

        private void initBestDevice(bool forceCPU)
        {
            if (!forceCPU)
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

        public void startRenderThread()
        {
            RenderThread = new Thread(renderThreadMain);
            RenderThread.IsBackground = true;
            RenderThread.Start();
        }
        public void waitForReady()
        {
            for (int i = 0; i < 100; i++)
            {
                if (ready)
                {
                    return;
                }

                Thread.Sleep(1);
            }
        }

        public void JoinRenderThread()
        {
            run = false;
            RenderThread.Join();
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
            renderKernel(frameData.frameBufferDiffuse.Extent / 3, frameData.frameBufferDiffuse, frameData.rngData, worldData.getDeviceMaterials(), worldData.getDeviceSpheres(), frameData.camera);
            outputKernel(frameData.frameBufferDiffuse.Extent / 3, frameData.frameBufferDiffuse, frameData.bitmapData, frameData.camera);

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

        private static void RenderKernel(Index1 index, ArrayView<float> diffuseFrameData, ArrayView<float> rngData, ArrayView<MaterialData> materials, ArrayView<Sphere> spheres, Camera camera)
        {
            // color cheatsheet
            //int r = data[(index * 3)];
            //int g = data[(index * 3) + 1];
            //int b = data[(index * 3) + 2];

            if((index * 3) < diffuseFrameData.Length)
            {
                int x = ((index) % camera.width);
                int y = ((index) / camera.width);

                BounceHitRecord[] records = new BounceHitRecord[25];

                Vec3 col = ColorRay(index, camera.GetRay(x + 0.5f, y + 0.5f), materials, spheres, rngData, records, camera);

                for (int i = 0; i < camera.superSample; i++)
                {
                    for (int j = 0; j < camera.superSample; j++)
                    {
                        col += ColorRay(index, camera.GetRay(x + ((float)i / camera.superSample), y + ((float)j / camera.superSample)), materials, spheres, rngData, records, camera);
                    }
                }

                col /= ((camera.superSample * camera.superSample) + 1);

                diffuseFrameData[(index * 3)] = col.x;
                diffuseFrameData[(index * 3) + 1] = col.y;
                diffuseFrameData[(index * 3) + 2] = col.z;
            }
        }

        private static HitRecord GetHit(Ray ray, ArrayView<Sphere> spheres)
        {
            HitRecord closestHit = HitRecord.badHit;

            for (int i = 0; i < spheres.Length; i++)
            {
                HitRecord rec = Sphere.hit(spheres[i], ray, 0.001f, float.MaxValue, closestHit);
                if (rec.t != -1)
                {
                    if(rec.t < closestHit.t)
                    {
                        closestHit = rec;
                    }
                }
            }

            return closestHit;
        }

        private static BounceRecord Bounce(HitRecord hit, Ray ray, MaterialData material)
        {
            float iorFrom, iorTo, reflectivity;

            if (hit.inside)
            {
                iorFrom = material.ref_idx;
                iorTo = 1;
            }
            else
            {
                iorFrom = 1;
                iorTo = material.ref_idx;
            }

            if (material.reflectivity < 0)
            {
                reflectivity = Vec3.NormalReflectance(hit.normal, ray.b, iorFrom, iorTo);
            }
            else
            {
                reflectivity = material.reflectivity;
            }

            OrthoNormalBasis basis = OrthoNormalBasis.fromZ(hit.normal);
            Ray reflectRay = new Ray(hit.p, coneSample(Vec3.reflect(hit.normal, ray.b), material.reflectionConeAngleRadians, 0, 0));
            Ray diffuseRay = new Ray(hit.p, hemisphereSample(basis, 0, 0));

            return new BounceRecord(reflectRay, diffuseRay, reflectivity);
        }

        private static Vec3 ColorRay(int rngStartIndex, Ray ray, ArrayView<MaterialData> materials, ArrayView<Sphere> spheres, ArrayView<float> rngData, BounceHitRecord[] records, Camera camera)
        {
            HitRecord hit = GetHit(ray, spheres);

            if (hit.materialID == -1)
            {
                return new Vec3(0.2f, 0.2f, 0.5f);
            }

            MaterialData material = materials[hit.materialID];

            float iorFrom, iorTo, reflectivity;

            if (hit.inside)
            {
                iorFrom = material.ref_idx;
                iorTo = 1;
            }
            else
            {
                iorFrom = 1;
                iorTo = material.ref_idx;
            }

            if (material.reflectivity < 0)
            {
                reflectivity = Vec3.NormalReflectance(hit.normal, ray.b, iorFrom, iorTo);
            }
            else
            {
                reflectivity = material.reflectivity;
            }

            if (camera.diffuse)
            {
                return material.diffuseColor;
            }

            BounceHitRecord firstHitColor;

            if (getNext(rngData, rngStartIndex) < reflectivity)
            {
                firstHitColor = new BounceHitRecord(hit.materialID, true);
            }
            else
            {
                firstHitColor = new BounceHitRecord(hit.materialID, false);
            }

            Ray currentRay = ray;
            HitRecord currentHit = hit;

            int totalBounces = 0;

            for (int i = 0; i < camera.maxBounces; i++)
            {
                BounceRecord currentBounce = Bounce(currentHit, currentRay, materials[currentHit.materialID]);

                if (getNext(rngData, rngStartIndex + i) < currentBounce.reflectivity)
                {
                    currentRay = currentBounce.reflectRay;
                    records[totalBounces].wasReflection = true;
                }
                else
                {
                    currentRay = currentBounce.diffuseRay;
                }

                currentHit = GetHit(currentRay, spheres);

                if (currentHit.materialID == -1)
                {
                    break;
                }
                else
                {
                    records[totalBounces].materialID = currentHit.materialID;
                    totalBounces++;
                    if(totalBounces <= camera.maxBounces)
                    {
                        records[totalBounces].materialID = -1;
                    }
                }
            }

            Vec3 bounceColor = new Vec3();

            for (int i = totalBounces - 1; i >= 0; i--)
            {
                if(records[i].materialID != -1)
                {
                    material = materials[records[i].materialID];
                    if (records[i].wasReflection)
                    {
                        bounceColor += material.emmissiveColor;
                    }
                    else
                    {
                        bounceColor = material.emmissiveColor + (material.diffuseColor * bounceColor);
                    }
                }
            }

            material = materials[firstHitColor.materialID];

            if(firstHitColor.wasReflection)
            {
                return (material.emmissiveColor + bounceColor) / (totalBounces + 1);
            }
            else
            {
                return (material.emmissiveColor + (material.diffuseColor * bounceColor)) / (totalBounces + 1);
            }
        }

        private static Vec3 coneSample(Vec3 direction, float coneTheta, float u, float v)
        {
            if (coneTheta < float.Epsilon)
            {
                return direction;
            }

            coneTheta = coneTheta * (1.0f - (2.0f * XMath.Acos(u) / XMath.PI));
            float radius = XMath.Sin(coneTheta);
            float zScale = XMath.Cos(coneTheta);
            float randomTheta = v * 2f * XMath.PI;
            OrthoNormalBasis basis = OrthoNormalBasis.fromZ(direction);
            return Vec3.unitVector(basis.transform(new Vec3(XMath.Cos(randomTheta) * radius, XMath.Sin(randomTheta) * radius, zScale)));
        }

        private static Vec3 hemisphereSample(OrthoNormalBasis basis, float u, float v)
        {
            float theta = 2f * XMath.PI * u;
            float radiusSquared = v;
            float radius = XMath.Sqrt(radiusSquared);
            return Vec3.unitVector(basis.transform(new Vec3(XMath.Cos(theta) * radius, XMath.Sin(theta) * radius, XMath.Sqrt(1 - radiusSquared))));
        }

        public static float getNext(ArrayView<float> data, int index)
        {
            return data[(index % data.Length)];
        }
    }

    internal struct BounceRecord
    {
        public Ray reflectRay;
        public Ray diffuseRay;
        public float reflectivity;

        public BounceRecord(Ray reflectRay, Ray diffuseRay, float reflectivity)
        {
            this.reflectRay = reflectRay;
            this.diffuseRay = diffuseRay;
            this.reflectivity = reflectivity;
        }
    }

    internal struct BounceHitRecord
    {
        public int materialID;
        public bool wasReflection;
        public BounceHitRecord(int materialID, bool wasReflection)
        {
            this.materialID = materialID;
            this.wasReflection = wasReflection;
        }
    }
}