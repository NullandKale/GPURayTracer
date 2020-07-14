using System;
using System.Collections.Generic;
using System.Text;

namespace GPURayTracer.Rendering.Primitives
{
    public readonly struct HitRecord
    {
        public static readonly HitRecord badHit = new HitRecord(float.MaxValue, new Vec3(), new Vec3(), false, -1);

        public readonly float t;
        public readonly bool inside;
        public readonly Vec3 p;
        public readonly Vec3 normal;
        public readonly int materialID;

        public HitRecord(float t, Vec3 p, Vec3 normal, bool inside, int materialID)
        {
            this.t = t;
            this.inside = inside;
            this.p = p;
            this.normal = normal;
            this.materialID = materialID;
        }

        public HitRecord(float t, Vec3 p, Vec3 normal, Vec3 rayDirection, int materialID)
        {
            this.t = t;
            inside = Vec3.dot(normal, rayDirection) > 0;
            this.p = p;
            this.normal = normal;
            this.materialID = materialID;
        }
    }
}
