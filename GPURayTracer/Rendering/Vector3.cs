using ILGPU.Algorithms;
using System;
using System.Collections.Generic;
using System.Text;

namespace GPURayTracer.Rendering
{
    public readonly struct Vec3
    {
        public static readonly Vec3 xAxis = new Vec3(1, 0, 0);
        public static readonly Vec3 yAxis = new Vec3(0, 1, 0);
        public static readonly Vec3 zAxis = new Vec3(0, 0, 1);

        public readonly float x;
        public readonly float y;
        public readonly float z;

        public Vec3(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public Vec3(double x, double y, double z)
        {
            this.x = (float)x;
            this.y = (float)y;
            this.z = (float)z;
        }

        public override string ToString()
        {
            return "{" + string.Format("{0:0.00}", x) + ", " + string.Format("{0:0.00}", y) + ", " + string.Format("{0:0.00}", z) + "}";
        }


        public static Vec3 operator -(Vec3 vec)
        {
            return new Vec3(-vec.x, -vec.y, -vec.z);
        }

        public float length()
        {
            return XMath.Sqrt(lengthSquared());
        }

        public float lengthSquared()
        {
            return x * x + y * y + z * z;
        }

        public float getAt(int a)
        {
            switch (a)
            {
                case 0:
                    return x;
                case 1:
                    return y;
                case 2:
                    return z;
                default:
                    return 0;
            }
        }

        public static float dist(Vec3 v1, Vec3 v2)
        {
            float dx = v1.x - v2.x;
            float dy = v1.y - v2.y;
            float dz = v1.z - v2.z;
            return XMath.Sqrt(dx * dx + dy * dy + dz * dz);
        }

        public static Vec3 operator +(Vec3 v1, Vec3 v2)
        {
            return new Vec3(v1.x + v2.x, v1.y + v2.y, v1.z + v2.z);
        }

        public static Vec3 operator -(Vec3 v1, Vec3 v2)
        {
            return new Vec3(v1.x - v2.x, v1.y - v2.y, v1.z - v2.z);
        }


        public static Vec3 operator *(Vec3 v1, Vec3 v2)
        {
            return new Vec3(v1.x * v2.x, v1.y * v2.y, v1.z * v2.z);
        }


        public static Vec3 operator /(Vec3 v1, Vec3 v2)
        {
            return new Vec3(v1.x / v2.x, v1.y / v2.y, v1.z / v2.z);
        }


        public static Vec3 operator *(Vec3 v1, float v)
        {
            return new Vec3(v1.x * v, v1.y * v, v1.z * v);
        }


        public static Vec3 operator *(float v, Vec3 v1)
        {
            return new Vec3(v1.x * v, v1.y * v, v1.z * v);
        }

        public static Vec3 operator +(Vec3 v1, float v)
        {
            return new Vec3(v1.x + v, v1.y + v, v1.z + v);
        }


        public static Vec3 operator +(float v, Vec3 v1)
        {
            return new Vec3(v1.x + v, v1.y + v, v1.z + v);
        }


        public static Vec3 operator /(Vec3 v1, float v)
        {
            return new Vec3(v1.x / v, v1.y / v, v1.z / v);
        }


        public static float dot(Vec3 v1, Vec3 v2)
        {
            return v1.x * v2.x + v1.y * v2.y + v1.z * v2.z;
        }

        public static Vec3 cross(Vec3 v1, Vec3 v2)
        {
            return new Vec3(v1.y * v2.z - v1.z * v2.y,
                          -(v1.x * v2.z - v1.z * v2.x),
                            v1.x * v2.y - v1.y * v2.x);
        }


        public static Vec3 unitVector(Vec3 v)
        {
            return v / v.length();
        }


        public static Vec3 reflect(Vec3 normal, Vec3 incomming)
        {
            return unitVector(incomming - normal * 2 * dot(incomming, normal));
        }

        public static Vec3 refract(Vec3 v, Vec3 n, float niOverNt)
        {
            Vec3 uv = unitVector(v);
            float dt = dot(uv, n);
            float discriminant = 1.0f - niOverNt * niOverNt * (1 - dt * dt);

            if (discriminant > 0)
            {
                return niOverNt * (uv - (n * dt)) - n * XMath.Sqrt(discriminant);
            }

            return new Vec3();
        }

        public static float NormalReflectance(Vec3 normal, Vec3 incomming, float iorFrom, float iorTo)
        {
            float iorRatio = iorFrom / iorTo;
            float cosThetaI = -dot(normal, incomming);
            float sinThetaTSquared = iorRatio * iorRatio * (1 - cosThetaI * cosThetaI);
            if(sinThetaTSquared > 1)
            {
                return 1f;
            }

            float cosThetaT = XMath.Sqrt(1 - sinThetaTSquared);
            float rPerpendicular = (iorFrom * cosThetaI - iorTo * cosThetaT) / (iorFrom * cosThetaI + iorTo * cosThetaT);
            float rParallel = (iorFrom * cosThetaI - iorTo * cosThetaT) / (iorFrom * cosThetaI + iorTo * cosThetaT);
            return (rPerpendicular * rPerpendicular + rParallel * rParallel) / 2;
        }
    }
}
