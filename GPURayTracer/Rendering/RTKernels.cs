using GPURayTracer.Rendering.GPUStructs;
using GPURayTracer.Rendering.Primitives;
using ILGPU;
using ILGPU.Algorithms;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Media.Media3D;

namespace GPURayTracer.Rendering
{
    public static class RTKernels
    {
        public static void RenderKernel(Index2 id,
            dFramebuffer framebuffer,
            ArrayView<float> rngData, 
            WorldBuffer world,
            Camera camera, int rngOffset)
        {
            int x = id.X;
            int y = id.Y;
            int index = ((y * camera.width) + x);

            int rngIndex = index + rngOffset + (int)(getNext(rngData, index) * index);
            Ray ray = camera.GetRay(x, y);

            ColorRecord col = ColorRay(index, rngIndex, ray, world.device_materials, world.device_spheres, world.lightSphereIDs, world.device_triangles, world.device_triNormals, framebuffer.ZBuffer, framebuffer.DrawableIDBuffer, rngData, camera);

            int rIndex = index * 3;
            int gIndex = rIndex + 1;
            int bIndex = rIndex + 2;

            framebuffer.ColorFrameBuffer[rIndex] = col.attenuation.x;
            framebuffer.ColorFrameBuffer[gIndex] = col.attenuation.y;
            framebuffer.ColorFrameBuffer[bIndex] = col.attenuation.z;

            framebuffer.LightingFrameBuffer[rIndex] = col.lighting.x;
            framebuffer.LightingFrameBuffer[gIndex] = col.lighting.y;
            framebuffer.LightingFrameBuffer[bIndex] = col.lighting.z;
        }

        private static ColorRecord ColorRay(int index, int rngStartIndex, 
            Ray ray, ArrayView<MaterialData> materials, 
            ArrayView<Sphere> spheres, ArrayView<int> lightSphereIDs,
            ArrayView<Triangle> triangles, ArrayView<Triangle> triNorms, 
            ArrayView<float> Zbuffer, ArrayView<int> sphereIDBuffer, ArrayView<float> rngData, Camera camera)
        {
            Vec3 attenuation = new Vec3(1f, 1f, 1f);
            Vec3 lighting = new Vec3();
            Ray working = ray;
            bool attenuationHasValue = false;
            HitRecord rec;
            ScatterRecord sRec;

            for (int i = 0; i < camera.maxBounces; i++)
            {
                rec = GetWorldHit(working, spheres, triangles, triNorms);

                if (rec.materialID == -1)
                {
                    if(i == 0 || attenuationHasValue)
                    {
                        sphereIDBuffer[index] = -2;
                    }

                    Vec3 unit_direction = Vec3.unitVector(working.b);
                    float t = 0.5f * (unit_direction.y + 1.0f);
                    attenuation *= (1.0f - t) * new Vec3(1.0f, 1.0f, 1.0f) + t * new Vec3(0.5f, 0.7f, 1.0f);
                    return new ColorRecord(attenuation, lighting);
                }
                else
                {
                    if (i == 0)
                    {
                        Zbuffer[index] = rec.t;
                        sphereIDBuffer[index] = rec.drawableID;
                    }

                    //reflection / refraction / diffuse
                    sRec = Scatter(working, rec, rngStartIndex + i, rngData, materials);
                    if(sRec.materialID != -1)
                    {
                        attenuationHasValue = sRec.mirrorSkyLightingFix;
                        attenuation *= sRec.attenuation;
                        working = sRec.scatterRay;
                    }
                    else
                    {
                        sphereIDBuffer[index] = -1;
                        return new ColorRecord(attenuation, lighting);
                    }
                }

                for (int j = 0; j < lightSphereIDs.Length; j++)
                {
                    Sphere s = spheres[lightSphereIDs[j]];
                    Vec3 lightDir = Vec3.unitVector(s.center - rec.p);
                    float lightDist = (s.center - rec.p).length() - s.radius;
                    Vec3 shadowOrig = rec.p;
                    HitRecord shadowRec = GetWorldHit(new Ray(rec.p, lightDir), spheres, triangles, triNorms);

                    if(shadowRec.materialID != -1 && (shadowRec.p - shadowOrig).length() >= lightDist - 0.05f) // the second part of this IF could probably be much more efficent
                    {
                        MaterialData material = materials[shadowRec.materialID];
                        if(material.type != 1)
                        {
                            lighting += material.emmissiveColor * XMath.Max(0.0f, Vec3.dot(lightDir, rec.normal));
                            lighting *= XMath.Pow(XMath.Max(0.0f, Vec3.dot(-Vec3.reflect(rec.normal, -lightDir), ray.b)), material.reflectivity) * material.emmissiveColor;
                        }
                    }
                }
            }

            return new ColorRecord(attenuation, lighting);
        }

        private static Vec3 RandomUnitVector(int rngStartIndex, ArrayView<float> rngData)
        {
            float a = 2f * XMath.PI * getNext(rngData, rngStartIndex);
            float z = (getNext(rngData, rngStartIndex + 1) * 2f) - 1f;
            float r = XMath.Sqrt(1f - z * z);
            return new Vec3(r * XMath.Cos(a), r * XMath.Sin(a), z);
        }

        private static HitRecord GetWorldHit(Ray r, ArrayView<Sphere> spheres, ArrayView<Triangle> triangles, ArrayView<Triangle> normals)
        {
            HitRecord rec = GetSphereHit(r, spheres);
            HitRecord triRec = GetTriangleHit(r, triangles, normals, rec.t);

            if (rec.materialID == -1 || triRec.materialID != -1)
            {
                return triRec;
            }
            else
            {
                return rec;
            }
        }

        private static HitRecord GetSphereHit(Ray r, ArrayView<Sphere> spheres)
        {
            float closestT = float.MaxValue;
            int sphereIndex = -1;

            Sphere s;
            Vec3 oc;
            float a;
            float b;
            float c;
            float discr;

            for (int i = 0; i < spheres.Length; i++)
            {
                s = spheres[i];
                oc = r.a - s.center;

                a = Vec3.dot(r.b, r.b);
                b = Vec3.dot(oc, r.b);
                c = Vec3.dot(oc, oc) - s.radiusSquared;
                discr = (b * b) - (a * c);

                if (discr > 0.01f)
                {
                    float sqrtdisc = XMath.Sqrt(discr);
                    float temp = (-b - sqrtdisc) / a;
                    if (temp < closestT && temp > 0.01f)
                    {
                        closestT = temp;
                        sphereIndex = i;
                        continue;
                    }
                    temp = (-b + sqrtdisc) / a;
                    if (temp < closestT && temp > 0.01f)
                    {
                        closestT = temp;
                        sphereIndex = i;
                    }
                }
            }

            if (sphereIndex != -1)
            {
                oc = r.pointAtParameter(closestT);
                s = spheres[sphereIndex];
                return new HitRecord(closestT, oc, (oc - s.center) / s.radius, r.b, s.materialIndex, sphereIndex);
            }
            else
            {
                return new HitRecord(float.MaxValue, new Vec3(), new Vec3(), false, -1, -1);
            }
        }

        private static HitRecord GetTriangleHit(Ray r, ArrayView<Triangle> triangles, ArrayView<Triangle> normals, float nearerThan)
        {
            float currentNearestDist = nearerThan;
            int NcurrentIndex = -1;
            float Ndet = 0;
            float Nu = 0;
            float Nv = 0;
            Triangle t;
            Vec3 tuVec;
            Vec3 tvVec;
            Vec3 pVec;
            float det;

            for (int i = 0; i < triangles.Length; i++)
            {
                t = triangles[i];
                tuVec = t.uVector();
                tvVec = t.vVector();
                pVec = Vec3.cross(r.b, tvVec);
                det = Vec3.dot(tuVec, pVec);

                if (XMath.Abs(det) > 0.000001f)
                {
                    float invDet = 1.0f / det;
                    Vec3 tVec = r.a - t.Vert0;
                    float u = Vec3.dot(tVec, pVec) * invDet;
                    Vec3 qVec = Vec3.cross(tVec, tuVec);
                    float v = Vec3.dot(r.b, qVec) * invDet;

                    if (u < 0.0001f || u > 1.0f || v < 0.0001f || u + v > 1.0f)
                    {
                        continue;
                    }
                    else
                    {
                        float temp = Vec3.dot(tvVec, qVec) * invDet;
                        if (temp > 0.000001f && temp < currentNearestDist)
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
                return new HitRecord(float.MaxValue, new Vec3(), new Vec3(), false, -1, -1);
            }
            else
            {
                Triangle tn = normals[NcurrentIndex];
                Vec3 uNorm = tn.uVector();
                Vec3 vNorm = tn.vVector();
                Vec3 normal = Vec3.unitVector((Nu * uNorm) + (Nv * vNorm) + tn.Vert0);
                bool backfacing = Ndet < 0.00001f;
                return new HitRecord(currentNearestDist, r.pointAtParameter(currentNearestDist), backfacing ? -normal : normal, backfacing, tn.MaterialID, NcurrentIndex);
            }
        }

        private static ScatterRecord Scatter(Ray r, HitRecord rec, int rngStartIndex, ArrayView<float> rngData, ArrayView<MaterialData> materials)
        {
            MaterialData material = materials[rec.materialID];
            Ray ray;
            Vec3 outward_normal;
            Vec3 refracted;
            Vec3 reflected;
            float ni_over_nt;
            float reflect_prob;
            float cosine;

            if (material.type == 0) //Diffuse
            {
                refracted = rec.p + rec.normal + RandomUnitVector(rngStartIndex, rngData);
                return new ScatterRecord(rec.materialID, new Ray(rec.p, refracted - rec.p), material.diffuseColor, false);
            }
            else if (material.type == 1) // dielectric
            {
                if(Vec3.dot(r.b, rec.normal) > 0f)
                {
                    outward_normal = -rec.normal;
                    ni_over_nt = material.ref_idx;
                    cosine = Vec3.dot(r.b, rec.normal) / r.b.length();
                    cosine = XMath.Sqrt(1.0f - material.ref_idx * material.ref_idx * (1f - cosine * cosine));
                }
                else
                {
                    outward_normal = rec.normal;
                    ni_over_nt = 1.0f / material.ref_idx;
                    cosine = -Vec3.dot(r.b, rec.normal) / r.b.length();
                }

                //moved the refract code here because I need the if (discriminant > 0) check
                Vec3 uv = Vec3.unitVector(r.b);
                float dt = Vec3.dot(uv, outward_normal);
                float discriminant = 1.0f - ni_over_nt * ni_over_nt * (1f - dt * dt);

                reflected = Vec3.reflect(rec.normal, r.b);

                if (discriminant > 0f)
                {
                    refracted = ni_over_nt * (uv - (outward_normal * dt)) - outward_normal * XMath.Sqrt(discriminant);
                    reflect_prob = schlick(cosine, material.ref_idx);
                }
                else
                {
                    reflect_prob = 1f;
                    refracted = reflected;
                }

                if(getNext(rngData, rngStartIndex) < reflect_prob)
                {
                    ray = new Ray(rec.p, reflected);
                }
                else
                {
                    ray = new Ray(rec.p, refracted);
                }

                return new ScatterRecord(rec.materialID, ray, material.diffuseColor, true);
            }
            else if (material.type == 2) //Metal
            {
                reflected = Vec3.reflect(rec.normal, Vec3.unitVector(r.b));
                if (material.ref_idx > 0)
                {
                    ray = new Ray(rec.p, reflected + (material.ref_idx * RandomUnitVector(rngStartIndex, rngData)));
                }
                else
                {
                    ray = new Ray(rec.p, reflected);
                }

                if ((Vec3.dot(ray.b, rec.normal) > 0))
                {
                    return new ScatterRecord(rec.materialID, ray, material.diffuseColor, true);
                }
            }
            else if (material.type == 3) //Lights
            {
                refracted = rec.p + rec.normal + RandomUnitVector(rngStartIndex, rngData);
                return new ScatterRecord(rec.materialID, new Ray(rec.p, refracted - rec.p), material.emmissiveColor, false);
            }

            return new ScatterRecord(-1, r, new Vec3(), true);
        }

        private static float getNext(ArrayView<float> data, int index)
        {
            return data[(index % data.Length)];
        }

        private static float schlick(float cosine, float ref_idx)
        {
            float r0 = (1.0f - ref_idx) / (1.0f + ref_idx);
            r0 = r0 * r0;
            return r0 + (1.0f - r0) * XMath.Pow((1.0f - cosine), 5.0f);
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    internal struct ScatterRecord
    {
        public int materialID;
        public Ray scatterRay;
        public Vec3 attenuation;
        public bool mirrorSkyLightingFix;

        public ScatterRecord(int materialID, Ray scatterRay, Vec3 attenuation, bool mirrorSkyLightingFix)
        {
            this.materialID = materialID;
            this.scatterRay = scatterRay;
            this.attenuation = attenuation;
            this.mirrorSkyLightingFix = mirrorSkyLightingFix;
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    internal struct ColorRecord
    {
        public Vec3 attenuation;
        public Vec3 lighting;

        public ColorRecord(Vec3 attenuation, Vec3 lighting)
        {
            this.attenuation = attenuation;
            this.lighting = lighting;
        }
    }
}
