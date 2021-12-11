using ILGPU.Algorithms;
using System;
using System.Collections.Generic;
using System.Text;

namespace NullEngine.Rendering.DataStructures
{
    public struct Triangle
    {
        public Vec3 Vert0;
        public Vec3 Vert1;
        public Vec3 Vert2;

        public Triangle(Vec3 vert0, Vec3 vert1, Vec3 vert2)
        {
            Vert0 = vert0;
            Vert1 = vert1;
            Vert2 = vert2;
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
            return Vec3.unitVector(Vec3.cross(Vert1 - Vert0, Vert2 - Vert0));
        }

        public Vec3 getCenter()
        {
            return (Vert0 + Vert1 + Vert2) / 3f;
        }

        public float GetTriangleHit2(Ray r, float epsilon, ref HitRecord hit)
        {
            Vec3 tuVec = uVector();
            Vec3 tvVec = vVector();
            Vec3 N = Vec3.cross(tvVec, tuVec);
            float area2 = N.length();

            float NdotRayDir = Vec3.dot(N, r.b);

            if(XMath.Abs(NdotRayDir) < epsilon)
            {
                return float.MaxValue;
            }

            float d = Vec3.dot(N, Vert0);
            float t = (Vec3.dot(N, r.a) + d);
            if(t < 0)
            {
                return float.MaxValue;
            }

            Vec3 P = r.a + (t * r.b); // parens might be wrong
            Vec3 C;

            Vec3 edge0 = Vert1 - Vert0;
            Vec3 vp0 = P - Vert0;
            C = Vec3.cross(edge0, vp0);
            if(Vec3.dot(N, C) < 0)
            {
                return float.MaxValue;
            }

            Vec3 edge1 = Vert2 - Vert1;
            Vec3 vp1 = P - Vert1;
            C = Vec3.cross(edge1, vp1);
            if(Vec3.dot(N, C) < 0)
            {
                return float.MaxValue;
            }

            Vec3 edge2 = Vert0 - Vert2;
            Vec3 vp2 = P - Vert2;
            C = Vec3.cross(edge2, vp2);
            if(Vec3.dot(N, C) < 0)
            {
                return float.MaxValue;
            }

            return t;
        }

        public float GetTriangleHit(Ray r, float epsilon, ref HitRecord hit)
        {
            Vec3 tuVec = uVector();
            Vec3 tvVec = vVector();
            Vec3 pVec = Vec3.cross(r.b, tvVec);
            float det = Vec3.dot(tuVec, pVec);

            if (XMath.Abs(det) < hit.t)
            {
                float invDet = 1.0f / det;
                Vec3 tVec = r.a - Vert0;
                float u = Vec3.dot(tVec, pVec) * invDet;
                Vec3 qVec = Vec3.cross(tVec, tuVec);
                float v = Vec3.dot(r.b, qVec) * invDet;

                if (u > 0 && u <= 1.0f && v > 0 && u + v <= 1.0f)
                {
                    float temp = Vec3.dot(tvVec, qVec) * invDet;
                    if (temp < hit.t)
                    {
                        hit.t = temp;
                        hit.p = r.pointAtParameter(temp);

                        if(det < 0)
                        {
                            hit.normal = -faceNormal();
                            hit.inside = true;
                        }
                        else
                        {
                            hit.normal = faceNormal();
                            hit.inside = false;
                        }

                        return temp;
                    }
                }
            }

            return float.MaxValue;
        }

        public static int CompareTo(Triangle a, Triangle b)
        {
            return Vec3.CompareTo(a.getCenter(), b.getCenter());
        }
    }
}
