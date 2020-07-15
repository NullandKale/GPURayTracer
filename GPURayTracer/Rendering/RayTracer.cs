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
            renderKernel(frameData.frameBuffer.Extent / 3, frameData.frameBuffer, frameData.rngData, worldData.getDeviceMaterials(), worldData.getDeviceSpheres(), frameData.camera);
            outputKernel(frameData.frameBuffer.Extent / 3, frameData.frameBuffer, frameData.bitmapData, frameData.camera);

            device.Synchronize();

            frameData.bitmapData.CopyTo(output, 0, 0, frameData.bitmapData.Length);
        }

        private static void RenderKernel(Index1 index, ArrayView<float> diffuseFrameData, ArrayView<float> rngData, ArrayView<MaterialData> materials, ArrayView<Sphere> spheres, Camera camera)
        {
            int x = ((index) % camera.width);
            int y = ((index) / camera.width);

            Vec3 col = ColorRay(index, camera.GetRay(x + 0.5f, y + 0.5f), materials, spheres, rngData, camera);

            diffuseFrameData[(index * 3)] = col.x;
            diffuseFrameData[(index * 3) + 1] = col.y;
            diffuseFrameData[(index * 3) + 2] = col.z;
        }

        private static Vec3 ColorRay(int rngStartIndex, Ray ray, ArrayView<MaterialData> materials, ArrayView<Sphere> spheres, ArrayView<float> rngData, Camera camera)
        {
            Vec3 result = new Vec3();

            int bounceCount = 0;

            BounceRecord[] bounces = new BounceRecord[11];

            for (int i = 0; i < camera.maxBounces; i++)
            {
                bounces[bounceCount] = hitBounce(rngStartIndex, ray, materials, spheres, rngData);

                if (bounces[bounceCount].MaterialID == -1)
                {
                    break;
                }
                else
                {
                    bounceCount++;

                    if (camera.diffuse)
                    {
                        return materials[bounces[bounceCount].MaterialID].diffuseColor;
                    }
                }
            }

            if (bounceCount == 0)
            {
                return new Vec3(0.2f, 0.2f, 0.5f);
            }
            else
            {
                for (int i = bounceCount; i >= 0; i--)
                {
                    BounceRecord record = bounces[i];
                    MaterialData material = materials[record.MaterialID];

                    if (record.wasReflection)
                    {
                        result = material.emmissiveColor + result;
                    }
                    else
                    {
                        result = material.emmissiveColor + material.diffuseColor * result;
                    }
                }

                return result / bounceCount;
            }
        }

        private static BounceRecord hitBounce(int rngStartIndex, Ray ray, ArrayView<MaterialData> materials, ArrayView<Sphere> spheres, ArrayView<float> rngData)
        {
            HitRecord hit = GetHit(ray, spheres);

            if (hit.materialID == -1)
            {
                return new BounceRecord(new Ray(), -1, false);
            }
            else
            {
                MaterialData material = materials[hit.materialID];
                return Bounce(getNext(rngData, rngStartIndex), hit, ray, material);
            }
        }

        private static HitRecord GetHit(Ray r, ArrayView<Sphere> spheres)
        {
            float closestT = float.MaxValue;
            int sphereIndex = -1;

            for (int i = 0; i < spheres.Length; i++)
            {
                Sphere s = spheres[i];
                Vec3 oc = r.a - s.center;

                float a = Vec3.dot(r.b, r.b);
                float b = Vec3.dot(oc, r.b);
                float c = Vec3.dot(oc, oc) - s.radiusSquared;
                float discr = (b * b) - (a * c);

                if (discr > 0)
                {
                    float sqrtdisc = XMath.Sqrt(discr);
                    float temp = (-b - sqrtdisc) / a;
                    if (temp < closestT && temp > float.Epsilon)
                    {
                        closestT = temp;
                        sphereIndex = i;
                        continue;
                    }
                    temp = (-b + sqrtdisc) / a;
                    if (temp < closestT && temp >float.Epsilon)
                    {
                        closestT = temp;
                        sphereIndex = i;
                    }
                }
            }

            if(sphereIndex != -1)
            {
                Vec3 p = r.pointAtParameter(closestT);
                Sphere s = spheres[sphereIndex];
                return new HitRecord(closestT, p, (p - s.center) / s.radius, r.b, s.materialIndex);
            }
            else
            {
                return HitRecord.badHit;
            }
        }

        private static HitRecord GetTriangleHit(Ray r, ArrayView<Triangle> triangles, ArrayView<Triangle> normals)
        {
            float currentNearestDist = float.MaxValue;
            int NcurrentIndex = -1;
            float Ndet = 0;
            float Nu = 0;
            float Nv = 0;

            for (int i = 0; i < triangles.Length; i++)
            {
                Triangle t = triangles[i];
                Vec3 tuVec = t.uVector();
                Vec3 tvVec = t.vVector();
                Vec3 pVec = Vec3.cross(r.b, tvVec);
                float det = Vec3.dot(tuVec, pVec);

                if (XMath.Abs(det) > 0.00001f)
                {
                    float invDet = 1.0f / det;
                    Vec3 tVec = r.a - t.Vert0;
                    float u = Vec3.dot(tVec, pVec) * invDet;
                    Vec3 qVec = Vec3.cross(tVec, tuVec);
                    float v = Vec3.dot(r.b, qVec);

                    if (u < 0.0 || u > 1.0 || v < 0 || u + v > 1)
                    {
                        continue;
                    }
                    else
                    {
                        float temp = Vec3.dot(tvVec, qVec) * invDet;
                        if (temp > float.Epsilon && temp < currentNearestDist)
                        {
                            currentNearestDist = temp;
                            NcurrentIndex = i;
                            Ndet = det;
                            Nu = u;
                            Nv = v;
                        }
                    }
                }
            }

            if (NcurrentIndex == -1)
            {
                return HitRecord.badHit;
            }
            else
            {
                Triangle tn = normals[NcurrentIndex];
                Vec3 uNorm = tn.uVector();
                Vec3 vNorm = tn.vVector();
                Vec3 normal = Vec3.unitVector((Nu * uNorm) + (Nv * vNorm) + tn.Vert0);
                bool backfacing = Ndet < float.Epsilon;
                return new HitRecord(currentNearestDist, r.pointAtParameter(currentNearestDist), backfacing ? -normal : normal, backfacing, tn.MaterialID);
            }
        }

        private static BounceRecord Bounce(float rngNext, HitRecord hit, Ray ray, MaterialData material)
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

            if (rngNext < reflectivity)
            {
                return new BounceRecord(new Ray(hit.p, coneSample(Vec3.reflect(hit.normal, ray.b), material.reflectionConeAngleRadians, 0, 0)), hit.materialID, true);
            }
            else
            {
                return new BounceRecord(new Ray(hit.p, hemisphereSample(basis, 0, 0)), hit.materialID, false);
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

        private static float getNext(ArrayView<float> data, int index)
        {
            return data[(index % data.Length)];
        }

        private static void CreatBitmap(Index1 index, ArrayView<float> data, ArrayView<byte> bitmapData, Camera camera)
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
    }

    internal readonly struct BounceRecord
    {
        public readonly Ray ray;
        public readonly int MaterialID;
        public readonly bool wasReflection;

        public BounceRecord(Ray ray, int materialID, bool wasReflection)
        {
            this.ray = ray;
            MaterialID = materialID;
            this.wasReflection = wasReflection;
        }
    }

    internal readonly struct BounceHitRecord
    {
        public readonly int materialID;
        public readonly bool wasReflection;
        public BounceHitRecord(int materialID, bool wasReflection)
        {
            this.materialID = materialID;
            this.wasReflection = wasReflection;
        }
    }
}