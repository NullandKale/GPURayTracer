using GPURayTracer.Rendering.GPUStructs;
using GPURayTracer.Rendering.Primitives;
using ILGPU;
using ILGPU.Algorithms;
using ILGPU.Algorithms.Random;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Media.Media3D;

namespace GPURayTracer.Rendering
{
    public static class RTKernels
    {
        public static void RenderKernel(Index2 id,
            dFramebuffer framebuffer,
            dWorldBuffer world,
            Camera camera, int rngOffset)
        {
            int x = id.X;
            int y = id.Y;

            int index = ((y * camera.width) + x);

            //there is probably a better way to do this, but it seems to work. seed = the tick * a large prime xor (index + 1) * even larger prime
            XorShift64Star rng = new XorShift64Star((((ulong)(rngOffset + 1) * 3727177) ^ ((ulong)(index + 1) * 113013596393)));
            //XorShift64Star rng = new XorShift64Star();

            Ray ray = camera.GetRay(x + rng.NextFloat(), y + rng.NextFloat());

            ColorRay(index, ray, framebuffer, world, rng, camera);
        }


        private static void ColorRay(int index,
            Ray ray,
            dFramebuffer framebuffer,
            dWorldBuffer world,
            XorShift64Star rng, Camera camera)
        {
            Vec3 attenuation = new Vec3(1f, 1f, 1f);
            Vec3 lighting = new Vec3();

            Ray working = ray;
            bool attenuationHasValue = false;

            for (int i = 0; i < camera.maxBounces; i++)
            {
                HitRecord rec = GetWorldHit(working, world);

                if (rec.materialID == -1)
                {
                    if (i == 0 || attenuationHasValue)
                    {
                        framebuffer.DrawableIDBuffer[index] = -2;
                    }

                    float t = 0.5f * (working.b.y + 1.0f);
                    attenuation *= (1.0f - t) * new Vec3(1.0f, 1.0f, 1.0f) + t * new Vec3(0.5f, 0.7f, 1.0f);
                    break;
                }
                else
                {
                    if (i == 0)
                    {
                        framebuffer.ZBuffer[index] = rec.t;
                        framebuffer.DrawableIDBuffer[index] = rec.drawableID;
                    }

                    ScatterRecord sRec = Scatter(working, rec, rng, world.materials);
                    if (sRec.materialID != -1)
                    {
                        attenuationHasValue = sRec.mirrorSkyLightingFix;
                        attenuation *= sRec.attenuation;
                        working = sRec.scatterRay;
                    }
                    else
                    {
                        framebuffer.DrawableIDBuffer[index] = -1;
                        break;
                    }
                }

                for (int j = 0; j < world.lightSphereIDs.Length; j++)
                {
                    Sphere s = world.spheres[world.lightSphereIDs[j]];
                    Vec3 lightDir = s.center - rec.p;
                    float lightDist = (s.center - rec.p).length() - s.radius;
                    Vec3 shadowOrig = rec.p;
                    HitRecord shadowRec = GetWorldHit(new Ray(rec.p, lightDir), world);

                    if (shadowRec.materialID != -1 && (shadowRec.p - shadowOrig).length() >= lightDist - 0.1f) // the second part of this IF could probably be much more efficent
                    {
                        MaterialData material = world.materials[shadowRec.materialID];
                        if (material.type != 1)
                        {
                            lightDir = Vec3.unitVector(lightDir);
                            lighting += material.color * XMath.Max(0.0f, Vec3.dot(lightDir, rec.normal));
                            lighting *= XMath.Pow(XMath.Max(0.0f, Vec3.dot(-Vec3.reflect(rec.normal, -lightDir), ray.b)), material.reflectivity) * material.color;
                        }
                    }
                }
            }

            int rIndex = index * 3;
            int gIndex = rIndex + 1;
            int bIndex = rIndex + 2;

            framebuffer.ColorFrameBuffer[rIndex] = attenuation.x;
            framebuffer.ColorFrameBuffer[gIndex] = attenuation.y;
            framebuffer.ColorFrameBuffer[bIndex] = attenuation.z;

            framebuffer.LightingFrameBuffer[rIndex] = lighting.x;
            framebuffer.LightingFrameBuffer[gIndex] = lighting.y;
            framebuffer.LightingFrameBuffer[bIndex] = lighting.z;
        }


        private static Vec3 RandomUnitVector(XorShift64Star rng)
        {
            float a = 2f * XMath.PI * rng.NextFloat();
            float z = (rng.NextFloat() * 2f) - 1f;
            float r = XMath.Sqrt(1f - z * z);
            return new Vec3(r * XMath.Cos(a), r * XMath.Sin(a), z);
        }


        private static HitRecord GetWorldHit(Ray r, dWorldBuffer world)
        {
            HitRecord rec = GetSphereHit(r, world.spheres);
            HitRecord vRec = world.VoxelChunk.hit(r, 0, rec.t);
            HitRecord triRec = GetMeshHit(r, world, vRec.t);

            if (rec.t < vRec.t && rec.t < triRec.t)
            {
                return rec;
            }
            else if (vRec.t < rec.t && vRec.t < triRec.t)
            {
                return vRec;
            }
            else
            {
                return triRec;
            }
        }


        private static HitRecord GetSphereHit(Ray r, ArrayView<Sphere> spheres)
        {
            float closestT = 10000;
            int sphereIndex = -1;

            Sphere s;
            Vec3 oc;

            for (int i = 0; i < spheres.Length; i++)
            {
                s = spheres[i];
                oc = r.a - s.center;

                float b = Vec3.dot(oc, r.b);
                float c = Vec3.dot(oc, oc) - s.radiusSquared;
                float discr = (b * b) - (c);

                if (discr > 0.01f)
                {
                    float sqrtdisc = XMath.Sqrt(discr);
                    float temp = (-b - sqrtdisc);
                    if (temp < closestT && temp > 0.01f)
                    {
                        closestT = temp;
                        sphereIndex = i;
                    }
                    else
                    {
                        temp = (-b + sqrtdisc);
                        if (temp < closestT && temp > 0.01f)
                        {
                            closestT = temp;
                            sphereIndex = i;
                        }
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


        private static HitRecord GetMeshHit(Ray r, dWorldBuffer world, float nearerThan)
        {
            float dist = nearerThan;
            HitRecord rec = new HitRecord(float.MaxValue, new Vec3(), new Vec3(), false, -1, -1);

            for (int i = 0; i < world.meshes.Length; i++)
            {
                if (world.meshes[i].aabb.hit(r, 0, dist))
                {
                    HitRecord meshHit = GetTriangleHit(r, world, world.meshes[i], dist);
                    if(meshHit.t < dist)
                    {
                        dist = meshHit.t;
                        rec = meshHit;
                    }
                }
            }

            return rec;
        }


        private static HitRecord GetTriangleHit(Ray r, dWorldBuffer world, dGPUMesh mesh, float nearerThan)
        {
            Triangle t = new Triangle();
            float currentNearestDist = nearerThan;
            int NcurrentIndex = -1;
            int material = 0;
            float Ndet = 0;

            for (int i = 0; i < mesh.triangleCount; i++)
            {
                t = mesh.GetTriangle(i, world);
                Vec3 tuVec = t.uVector();
                Vec3 tvVec = t.vVector();
                Vec3 pVec = Vec3.cross(r.b, tvVec);
                float det = Vec3.dot(tuVec, pVec);

                if (XMath.Abs(det) > 0.0001f)
                {
                    float invDet = 1.0f / det;
                    Vec3 tVec = r.a - t.Vert0;
                    float u = Vec3.dot(tVec, pVec) * invDet;
                    Vec3 qVec = Vec3.cross(tVec, tuVec);
                    float v = Vec3.dot(r.b, qVec) * invDet;

                    if (u > 0 && u <= 1.0f && v > 0 && u + v <= 1.0f)
                    {
                        float temp = Vec3.dot(tvVec, qVec) * invDet;
                        if (temp > 0.001f && temp < currentNearestDist)
                        {
                            currentNearestDist = temp;
                            NcurrentIndex = i;
                            Ndet = det;
                            material = t.MaterialID;
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
                if (Ndet < 0)
                {
                    return new HitRecord(currentNearestDist, r.pointAtParameter(currentNearestDist), -t.faceNormal(), true, material, NcurrentIndex);
                }
                else
                {
                    return new HitRecord(currentNearestDist, r.pointAtParameter(currentNearestDist), t.faceNormal(), false, material, NcurrentIndex);
                }
            }
        }


        private static ScatterRecord Scatter(Ray r, HitRecord rec, XorShift64Star rng, ArrayView<MaterialData> materials)
        {
            MaterialData material = materials[rec.materialID];
            Ray ray;
            Vec3 outward_normal;
            Vec3 refracted;
            Vec3 reflected;
            float ni_over_nt;
            float cosine;

            if (material.type == 0) //Diffuse
            {
                refracted = rec.p + rec.normal + RandomUnitVector(rng);
                return new ScatterRecord(rec.materialID, new Ray(rec.p, refracted - rec.p), material.color, false);
            }
            else if (material.type == 1) // dielectric
            {
                if (Vec3.dot(r.b, rec.normal) > 0.01f)
                {
                    outward_normal = -rec.normal;
                    ni_over_nt = material.ref_idx;
                    cosine = Vec3.dot(r.b, rec.normal);
                    cosine = XMath.Sqrt(1.0f - material.ref_idx * material.ref_idx * (1f - cosine * cosine));
                }
                else
                {
                    outward_normal = rec.normal;
                    ni_over_nt = 1.0f / material.ref_idx;
                    cosine = -Vec3.dot(r.b, rec.normal);
                }

                //moved the refract code here because I need the if (discriminant > 0) check
                float dt = Vec3.dot(r.b, outward_normal);
                float discriminant = 1.0f - ni_over_nt * ni_over_nt * (1f - dt * dt);

                if (discriminant > 0.001f)
                {

                    if (rng.NextFloat() < schlick(cosine, material.ref_idx))
                    {
                        return new ScatterRecord(rec.materialID, new Ray(rec.p, Vec3.reflect(rec.normal, r.b)), material.color, true);
                    }
                    else
                    {
                        return new ScatterRecord(rec.materialID, new Ray(rec.p, ni_over_nt * (r.b - (outward_normal * dt)) - outward_normal * XMath.Sqrt(discriminant)), material.color, true);
                    }
                }
                else
                {
                    return new ScatterRecord(rec.materialID, new Ray(rec.p, Vec3.reflect(rec.normal, r.b)), material.color, true);
                }

            }
            else if (material.type == 2) //Metal
            {
                reflected = Vec3.reflect(rec.normal, r.b);
                if (material.reflectionConeAngleRadians > 0.001f)
                {
                    ray = new Ray(rec.p, reflected + (material.reflectionConeAngleRadians * RandomUnitVector(rng)));
                }
                else
                {
                    ray = new Ray(rec.p, reflected);
                }

                if ((Vec3.dot(ray.b, rec.normal) > 0.001f))
                {
                    return new ScatterRecord(rec.materialID, ray, material.color, true);
                }
            }
            else if (material.type == 3) //Lights
            {
                refracted = rec.p + rec.normal + RandomUnitVector(rng);
                return new ScatterRecord(rec.materialID, new Ray(rec.p, refracted - rec.p), material.color, false);
            }

            return new ScatterRecord(-1, r, new Vec3(), true);
        }

        private static float schlick(float cosine, float ref_idx)
        {
            float r0 = (1.0f - ref_idx) / (1.0f + ref_idx);
            r0 = r0 * r0;
            return r0 + (1.0f - r0) * XMath.Pow((1.0f - cosine), 5.0f);
        }
    }

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
}