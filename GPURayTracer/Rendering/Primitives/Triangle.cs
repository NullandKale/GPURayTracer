using ILGPU.Algorithms;
using System;
using System.Collections.Generic;
using System.Text;

namespace GPURayTracer.Rendering.Primitives
{
    public readonly struct Triangle
    {
        public readonly Vec3 Vert0;
        public readonly Vec3 Vert1;
        public readonly Vec3 Vert2;
        public readonly int MaterialID;

        public Triangle(Vec3 vert0, Vec3 vert1, Vec3 vert2, int MaterialID)
        {
            Vert0 = vert0;
            Vert1 = vert1;
            Vert2 = vert2;
            this.MaterialID = MaterialID;
        }

        public Vec3 uVector()
        {
            return Vert1 - Vert0;
        }

        public Vec3 vVector()
        {
            return Vert2 - Vert0;
        }

        public Vec3 faceNormal()
        {
            return Vec3.unitVector(Vec3.cross(uVector(), vVector()));
        }

        public static HitRecord hit(Triangle t, Ray r, float tMin, float tMax, HitRecord rec)
        {
            Vec3 tuVec = t.uVector();
            Vec3 tvVec = t.vVector();
            Vec3 pVec = Vec3.cross(r.b, tvVec);
            float det = Vec3.dot(tuVec, pVec);

            if (XMath.Abs(det) < 0.00001f)
            {
                return HitRecord.badHit;
            }
            else
            {
                float invDet = 1.0f / det;
                Vec3 tVec = r.a - t.Vert0;
                float u = Vec3.dot(tVec, pVec) * invDet;
                Vec3 qVec = Vec3.cross(tVec, tuVec);
                float v = Vec3.dot(r.b, qVec);

                if (u < 0.0 || u > 1.0 || v < 0 || u + v > 1)
                {
                    return HitRecord.badHit;
                }

                float temp = Vec3.dot(tvVec, qVec) * invDet;
                if (temp > tMin && temp < tMax)
                {
                    return new HitRecord(temp, )
                }
            }

            return HitRecord.badHit;
        }
    }
}
