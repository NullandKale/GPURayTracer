using ILGPU.Algorithms;
using System;
using System.Collections.Generic;
using System.Text;

namespace GPURayTracer.Rendering.Primitives
{
    public readonly struct Sphere
    {
        public readonly Vec3 center;
        public readonly float radius;
        public readonly float radiusSquared;
        public readonly int materialIndex;

        public Sphere(Vec3 center, float radius, int materialIndex)
        {
            this.center = center;
            this.radius = radius;
            radiusSquared = radius * radius;
            this.materialIndex = materialIndex;
        }

        public static HitRecord hit(Sphere s, Ray r, float tMin, float tMax, HitRecord rec)
        {
            Vec3 oc = r.a - s.center;

            float a = Vec3.dot(r.b, r.b);
            float b = Vec3.dot(oc, r.b);
            float c = Vec3.dot(oc, oc) - s.radiusSquared;
            float discr = (b * b) - (a * c);

            if (discr > 0)
            {
                float sqrtdisc = XMath.Sqrt(discr);
                float temp = (-b - sqrtdisc) / a;
                if (temp < tMax && temp > tMin)
                {
                    return new HitRecord(temp, r.pointAtParameter(temp), (rec.p - s.center) / s.radius, r.b, s.materialIndex);
                }
                temp = (-b + sqrtdisc) / a;
                if (temp < tMax && temp > tMin)
                {
                    return new HitRecord(temp, r.pointAtParameter(temp), (rec.p - s.center) / s.radius, r.b, s.materialIndex);
                }
            }
            return HitRecord.badHit;
        }
    }
}
