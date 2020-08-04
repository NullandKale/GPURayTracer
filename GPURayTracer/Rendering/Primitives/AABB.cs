using ILGPU.Algorithms;
using System;
using System.Collections.Generic;
using System.Text;

namespace GPURayTracer.Rendering.Primitives
{
    public struct AABB
    {
        public Vec3 min;
        public Vec3 max;

        public AABB(Vec3 min, Vec3 max)
        {
            this.min = min;
            this.max = max;
        }

        public static AABB CreateFromTriangle(Vec3 Vert0, Vec3 Vert1, Vec3 Vert2)
        {
            float minX = Vert0.x;
            float maxX = Vert0.x;

            float minY = Vert0.y;
            float maxY = Vert0.y;

            float minZ = Vert0.z;
            float maxZ = Vert0.z;

            if (Vert1.x < minX)
            {
                minX = Vert1.x;
            }

            if (Vert2.x < minX)
            {
                minX = Vert2.x;
            }

            if (Vert1.y < minY)
            {
                minY = Vert1.y;
            }

            if (Vert2.y < minY)
            {
                minY = Vert2.y;
            }

            if (Vert1.z < minZ)
            {
                minZ = Vert1.z;
            }

            if (Vert2.z < minZ)
            {
                minZ = Vert2.z;
            }

            if (Vert1.x > maxX)
            {
                maxX = Vert1.x;
            }

            if (Vert2.x > maxX)
            {
                maxX = Vert2.x;
            }

            if (Vert1.y > maxY)
            {
                maxY = Vert1.y;
            }

            if (Vert2.y > maxY)
            {
                maxY = Vert2.y;
            }

            if (Vert1.z > maxZ)
            {
                maxZ = Vert1.z;
            }

            if (Vert2.z > maxZ)
            {
                maxZ = Vert2.z;
            }

            return new AABB(new Vec3(minX, minY, minZ), new Vec3(maxX, maxY, maxZ));
        }

        public static AABB surrounding_box(AABB box0, AABB box1)
        {
            Vec3 small = new Vec3(XMath.Min(box0.min.x, box1.min.x), XMath.Min(box0.min.y, box1.min.y), XMath.Min(box0.min.z, box1.min.z));
            Vec3 big = new Vec3(XMath.Max(box0.max.x, box1.max.x), XMath.Max(box0.max.y, box1.max.y), XMath.Max(box0.max.z, box1.max.z));
            return new AABB(small, big);
        }

        public bool hit(Ray ray, float tMin, float tMax)
        {
            float minV = (min.x - ray.a.x) / ray.b.x;
            float maxV = (max.x - ray.a.x) / ray.b.x;
            float t1 = XMath.Max(minV, maxV);
            float t0 = XMath.Min(minV, maxV);
            tMin = XMath.Max(t0, tMin);
            tMax = XMath.Min(t1, tMax);
            if (tMax <= tMin)
            {
                return false;
            }

            minV = (min.y - ray.a.y) / ray.b.y;
            maxV = (max.y - ray.a.y) / ray.b.y;
            t1 = XMath.Max(minV, maxV);
            t0 = XMath.Min(minV, maxV);
            tMin = XMath.Max(t0, tMin);
            tMax = XMath.Min(t1, tMax);
            if (tMax <= tMin)
            {
                return false;
            }

            minV = (min.z - ray.a.z) / ray.b.z;
            maxV = (max.z - ray.a.z) / ray.b.z;
            t1 = XMath.Max(minV, maxV);
            t0 = XMath.Min(minV, maxV);
            tMin = XMath.Max(t0, tMin);
            tMax = XMath.Min(t1, tMax);
            if (tMax <= tMin)
            {
                return false;
            }

            return true;
        }
    }
}
