using GPURayTracer.Rendering.Primitives;
using ILGPU;
using ILGPU.Algorithms;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Media.Media3D;

namespace GPURayTracer.Rendering
{
    public static class RTKernels
    {
        public static void RenderKernel(Index1 index, 
            ArrayView<float> diffuseFrameData, ArrayView<float> Zbuffer, ArrayView<int> sphereIDBuffer, 
            ArrayView<float> rngData, ArrayView<MaterialData> materials, ArrayView<Sphere> spheres, ArrayView<Triangle> triangles, ArrayView<Triangle> triNorms, 
            Camera camera, int rngOffset)
        {
            int x = ((index) % camera.width);
            int y = ((index) / camera.width);

            int rngIndex = rngOffset + index + (int)(index * getNext(rngData, index) * 2.0f);
            Ray ray = camera.GetRay(x + getNext(rngData, rngIndex), y + getNext(rngData, rngIndex + 1));
            Vec3 col = ColorRay(index, rngIndex + 2, ray, materials, spheres, triangles, triNorms, Zbuffer, sphereIDBuffer, rngData, camera);

            diffuseFrameData[(index * 3)] = col.x;
            diffuseFrameData[(index * 3) + 1] = col.y;
            diffuseFrameData[(index * 3) + 2] = col.z;
        }

        public static void RenderKernelSecondaryPass(Index1 index,
            ArrayView<float> diffuseFrameData, ArrayView<float> Zbuffer, ArrayView<int> sphereIDBuffer,
            ArrayView<float> rngData, ArrayView<MaterialData> materials, ArrayView<Sphere> spheres, ArrayView<Triangle> triangles, ArrayView<Triangle> triNorms,
            Camera camera, int rngOffset)
        {
            if (sphereIDBuffer[index] == -2)
            {
                int x = ((index) % camera.width);
                int y = ((index) / camera.width);

                int rngIndex = rngOffset + index + (int)(index * getNext(rngData, index) * 2.0f);
                Ray ray = camera.GetRay(x + getNext(rngData, rngIndex), y + getNext(rngData, rngIndex + 1));
                Vec3 col = ColorRay(index, rngIndex + 2, ray, materials, spheres, triangles, triNorms, Zbuffer, sphereIDBuffer, rngData, camera);

                diffuseFrameData[(index * 3)] = col.x;
                diffuseFrameData[(index * 3) + 1] = col.y;
                diffuseFrameData[(index * 3) + 2] = col.z;
            }
        }

        private static Vec3 ColorRay(int index, int rngStartIndex, Ray ray, ArrayView<MaterialData> materials, ArrayView<Sphere> spheres, ArrayView<Triangle> triangles, ArrayView<Triangle> triNorms, ArrayView<float> Zbuffer, ArrayView<int> sphereIDBuffer, ArrayView<float> rngData, Camera camera)
        {
            Vec3 attenuation = new Vec3(1, 1, 1);
            Ray working = ray;

            for(int i = 0; i < camera.maxBounces; i++)
            {
                HitRecord rec = GetSphereHit(working, spheres);

                if (rec.materialID == -1)
                {
                    if (i == 0)
                    {
                        sphereIDBuffer[index] = -1;
                    }

                    Vec3 unit_direction = Vec3.unitVector(working.b);
                    float t = 0.5f * (unit_direction.y + 1.0f);
                    Vec3 c = (1.0f - t) * new Vec3(1.0, 1.0, 1.0) + t * new Vec3(0.5, 0.7, 1.0);
                    return attenuation * c;
                }
                else
                {
                    if (i == 0)
                    {
                        Zbuffer[index] = rec.t;
                        sphereIDBuffer[index] = rec.drawableID;
                    }

                    ScatterRecord sRec = Scatter(working, rec, rngStartIndex + i, rngData, materials);
                    if(sRec.didScatter)
                    {
                        attenuation *= sRec.attenuation;
                        working = sRec.scatterRay;
                    }
                    else
                    {
                        sphereIDBuffer[index] = -2;
                        return new Vec3();
                    }
                }
            }

            sphereIDBuffer[index] = -2;
            return new Vec3();
        }

        private static Vec3 RandomUnitVector(int rngStartIndex, ArrayView<float> rngData)
        {
            float a = 2f * XMath.PI * getNext(rngData, rngStartIndex);
            float z = (getNext(rngData, rngStartIndex) * 2f) - 1;
            float r = XMath.Sqrt(1 - z * z);
            return new Vec3(r * XMath.Cos(a), r * XMath.Sin(a), z);
        }

        private static HitRecord GetSphereHit(Ray r, ArrayView<Sphere> spheres)
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
                    if (temp < closestT && temp > float.Epsilon)
                    {
                        closestT = temp;
                        sphereIndex = i;
                    }
                }
            }

            if (sphereIndex != -1)
            {
                Vec3 p = r.pointAtParameter(closestT);
                Sphere s = spheres[sphereIndex];
                return new HitRecord(closestT, p, (p - s.center) / s.radius, r.b, s.materialIndex, sphereIndex);
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
                return new HitRecord(currentNearestDist, r.pointAtParameter(currentNearestDist), backfacing ? -normal : normal, backfacing, tn.MaterialID, NcurrentIndex);
            }
        }

        private static ScatterRecord Scatter(Ray r, HitRecord rec, int rngStartIndex, ArrayView<float> rngData, ArrayView<MaterialData> materials)
        {
            MaterialData material = materials[rec.materialID];

            if (material.type == 0) //Diffuse
            {
                Vec3 target = rec.p + rec.normal + RandomUnitVector(rngStartIndex, rngData);
                return new ScatterRecord(true, new Ray(rec.p, target - rec.p), material.diffuseColor);
            }
            else if (material.type == 2) //Metal
            {
                Vec3 reflected = Vec3.reflect(rec.normal, Vec3.unitVector(r.b));
                Ray scattered = new Ray(rec.p, reflected + (material.ref_idx * RandomUnitVector(rngStartIndex, rngData)));
                if((Vec3.dot(scattered.b, rec.normal) > 0))
                {
                    return new ScatterRecord(true, scattered, material.diffuseColor);
                }
            }

            return new ScatterRecord(false, r, new Vec3());
        }

        private static float getNext(ArrayView<float> data, int index)
        {
            return data[(index % data.Length)];
        }
    }

    internal struct ScatterRecord
    {
        public readonly bool didScatter;
        public readonly Ray scatterRay;
        public readonly Vec3 attenuation;

        public ScatterRecord(bool didScatter, Ray scatterRay, Vec3 attenuation)
        {
            this.didScatter = didScatter;
            this.scatterRay = scatterRay;
            this.attenuation = attenuation;
        }
    }
}
