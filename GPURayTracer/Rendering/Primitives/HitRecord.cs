using System;
using System.Collections.Generic;
using System.Text;

namespace GPURayTracer.Rendering.Primitives
{
    public struct HitRecord
    {
        public static readonly HitRecord badHit = new HitRecord(-1, new Vec3(), new Vec3(), -1);

        public float t;
        public Vec3 p;
        public Vec3 normal;
        public int materialID;

        public HitRecord(float t, Vec3 p, Vec3 normal, int materialID)
        {
            this.t = t;
            this.p = p;
            this.normal = normal;
            this.materialID = materialID;
        }
    }
}
