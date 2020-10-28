using ILGPU.Algorithms;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace NullEngine.Rendering.DataStructures
{
    public struct Vec3
    {
        public float x;
        public float y;
        public float z;


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
            return XMath.Sqrt(x * x + y * y + z * z);
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

        public static Vec3 setX(Vec3 v, float x)
        {
            return new Vec3(x, v.y, v.z);
        }

        public static Vec3 setY(Vec3 v, float y)
        {
            return new Vec3(v.x, y, v.z);
        }

        public static Vec3 setZ(Vec3 v, float z)
        {
            return new Vec3(v.x, v.y, z);
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


        public static Vec3 operator /(float v, Vec3 v1)
        {
            return new Vec3(v / v1.x, v / v1.y, v / v1.z);
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
            return v1 * (1.0f / v);
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
            return v / XMath.Sqrt(v.x * v.x + v.y * v.y + v.z * v.z);
        }


        public static Vec3 reflect(Vec3 normal, Vec3 incomming)
        {
            return unitVector(incomming - normal * 2f * dot(incomming, normal));
        }


        public static Vec3 refract(Vec3 v, Vec3 n, float niOverNt)
        {
            Vec3 uv = unitVector(v);
            float dt = dot(uv, n);
            float discriminant = 1.0f - niOverNt * niOverNt * (1f - dt * dt);

            if (discriminant > 0)
            {
                return niOverNt * (uv - (n * dt)) - n * XMath.Sqrt(discriminant);
            }

            return v;
        }


        public static float NormalReflectance(Vec3 normal, Vec3 incomming, float iorFrom, float iorTo)
        {
            float iorRatio = iorFrom / iorTo;
            float cosThetaI = -dot(normal, incomming);
            float sinThetaTSquared = iorRatio * iorRatio * (1 - cosThetaI * cosThetaI);
            if (sinThetaTSquared > 1)
            {
                return 1f;
            }

            float cosThetaT = XMath.Sqrt(1 - sinThetaTSquared);
            float rPerpendicular = (iorFrom * cosThetaI - iorTo * cosThetaT) / (iorFrom * cosThetaI + iorTo * cosThetaT);
            float rParallel = (iorFrom * cosThetaI - iorTo * cosThetaT) / (iorFrom * cosThetaI + iorTo * cosThetaT);
            return (rPerpendicular * rPerpendicular + rParallel * rParallel) / 2f;
        }


        public static Vec3 aces_approx(Vec3 v)
        {
            v *= 0.6f;
            float a = 2.51f;
            float b = 0.03f;
            float c = 2.43f;
            float d = 0.59f;
            float e = 0.14f;
            Vec3 working = (v * (a * v + b)) / (v * (c * v + d) + e);
            return new Vec3(XMath.Clamp(working.x, 0, 1), XMath.Clamp(working.y, 0, 1), XMath.Clamp(working.z, 0, 1));
        }


        public static Vec3 reinhard(Vec3 v)
        {
            return v / (1.0f + v);
        }


        public static bool Equals(Vec3 a, Vec3 b)
        {
            return a.x == b.x &&
                   a.y == b.y &&
                   a.z == b.z;
        }

        public static implicit operator Vector3(Vec3 d)
        {
            return new Vector3((float)d.x, (float)d.y, (float)d.z);
        }

        public static implicit operator Vec3(Vector3 d)
        {
            return new Vec3(d.X, d.Y, d.Z);
        }

        public static implicit operator Vector4(Vec3 d)
        {
            return new Vector4((float)d.x, (float)d.y, (float)d.z, 0);
        }

        public static implicit operator Vec3(Vector4 d)
        {
            return new Vec3(d.X, d.Y, d.Z);
        }
    }

    public struct Vec3i
    {
        public int x;
        public int y;
        public int z;

        public Vec3i(int x, int y, int z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public Vec3i(float x, float y, float z)
        {
            this.x = (int)x;
            this.y = (int)y;
            this.z = (int)z;
        }

        public override string ToString()
        {
            return "{" + string.Format("{0:0.00}", x) + ", " + string.Format("{0:0.00}", y) + ", " + string.Format("{0:0.00}", z) + "}";
        }


        public static Vec3i operator -(Vec3i vec)
        {
            return new Vec3i(-vec.x, -vec.y, -vec.z);
        }


        public float length()
        {
            return XMath.Sqrt(x * x + y * y + z * z);
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

        public static Vec3i setX(Vec3i v, int x)
        {
            return new Vec3i(x, v.y, v.z);
        }

        public static Vec3i setY(Vec3i v, int y)
        {
            return new Vec3i(v.x, y, v.z);
        }

        public static Vec3i setZ(Vec3i v, int z)
        {
            return new Vec3i(v.x, v.y, z);
        }


        public static float dist(Vec3i v1, Vec3i v2)
        {
            float dx = v1.x - v2.x;
            float dy = v1.y - v2.y;
            float dz = v1.z - v2.z;
            return XMath.Sqrt(dx * dx + dy * dy + dz * dz);
        }


        public static Vec3i operator +(Vec3i v1, Vec3i v2)
        {
            return new Vec3i(v1.x + v2.x, v1.y + v2.y, v1.z + v2.z);
        }


        public static Vec3i operator -(Vec3i v1, Vec3i v2)
        {
            return new Vec3i(v1.x - v2.x, v1.y - v2.y, v1.z - v2.z);
        }


        public static Vec3i operator *(Vec3i v1, Vec3i v2)
        {
            return new Vec3i(v1.x * v2.x, v1.y * v2.y, v1.z * v2.z);
        }


        public static Vec3i operator /(Vec3i v1, Vec3i v2)
        {
            return new Vec3i(v1.x / v2.x, v1.y / v2.y, v1.z / v2.z);
        }


        public static Vec3i operator /(int v, Vec3i v1)
        {
            return new Vec3i(v / v1.x, v / v1.y, v / v1.z);
        }


        public static Vec3i operator *(Vec3i v1, int v)
        {
            return new Vec3i(v1.x * v, v1.y * v, v1.z * v);
        }


        public static Vec3i operator *(int v, Vec3i v1)
        {
            return new Vec3i(v1.x * v, v1.y * v, v1.z * v);
        }


        public static Vec3i operator +(Vec3i v1, int v)
        {
            return new Vec3i(v1.x + v, v1.y + v, v1.z + v);
        }


        public static Vec3i operator +(int v, Vec3i v1)
        {
            return new Vec3i(v1.x + v, v1.y + v, v1.z + v);
        }


        public static Vec3i operator /(Vec3i v1, int v)
        {
            return v1 * (1 / v);
        }


        public static float dot(Vec3i v1, Vec3i v2)
        {
            return v1.x * v2.x + v1.y * v2.y + v1.z * v2.z;
        }


        public static Vec3i cross(Vec3i v1, Vec3i v2)
        {
            return new Vec3i(v1.y * v2.z - v1.z * v2.y,
                          -(v1.x * v2.z - v1.z * v2.x),
                            v1.x * v2.y - v1.y * v2.x);
        }


        public static bool Equals(Vec3i a, Vec3i b)
        {
            return a.x == b.x &&
                   a.y == b.y &&
                   a.z == b.z;
        }
    }
}
