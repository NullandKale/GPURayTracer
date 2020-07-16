﻿using ILGPU.Algorithms;
using System;
using System.Collections.Generic;
using System.Text;

namespace GPURayTracer.Rendering
{
    public readonly struct Camera
    {
        public readonly int height;
        public readonly int width;
        public readonly int maxBounces;
        public readonly int MSAA;

        public readonly Vec3 origin;
        public readonly OrthoNormalBasis axis;

        public readonly float aspectRatio;
        public readonly float cameraPlaneDist;
        public readonly float reciprocalHeight;
        public readonly float reciprocalWidth;
        public readonly float apertureRadius;
        public readonly float focalDistance;

        public Camera(Vec3 origin, Vec3 lookAt, Vec3 up, int width, int height, int maxBounces, int MSAA, float verticalFov, Vec3 focalPoint, float apertureRadius)
        {
            this.width = width;
            this.height = height;
            this.maxBounces = maxBounces;
            this.MSAA = MSAA;

            this.origin = origin;
            axis = OrthoNormalBasis.fromZY(Vec3.unitVector(lookAt - origin), up);

            aspectRatio = ((float)width / (float)height);
            cameraPlaneDist = 1.0f / XMath.Tan(verticalFov * XMath.PI / 360.0f);
            reciprocalHeight = 1.0f / height;
            reciprocalWidth = 1.0f / width;
            focalDistance = (focalPoint - origin).length();
            this.apertureRadius = apertureRadius;
        }

        private Ray rayFromUnit(float x, float y)
        {
            Vec3 xContrib = axis.x * -x * aspectRatio;
            Vec3 yContrib = axis.y * -y;
            Vec3 zContrib = axis.z * cameraPlaneDist;
            Vec3 direction = Vec3.unitVector(xContrib + yContrib + zContrib);

            return new Ray(origin, direction);
        }

        public Ray GetRay(float x, float y)
        {
            return rayFromUnit(2 * (x * reciprocalWidth) - 1, 2 * (y * reciprocalHeight) - 1);
        }
    }

    public readonly struct OrthoNormalBasis
    {
        public readonly Vec3 x;
        public readonly Vec3 y;
        public readonly Vec3 z;

        public OrthoNormalBasis(Vec3 x, Vec3 y, Vec3 z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public Vec3 transform(Vec3 pos)
        {
            return x * pos.x + y * pos.y + z * pos.z;
        }
        public static OrthoNormalBasis fromXY(Vec3 x, Vec3 y)
        {
            Vec3 zz = Vec3.unitVector(Vec3.cross(x, y));
            Vec3 yy = Vec3.unitVector(Vec3.cross(zz, x));
            return new OrthoNormalBasis(x, yy, zz);
        }

        public static OrthoNormalBasis fromYX(Vec3 y, Vec3 x)
        {
            Vec3 zz = Vec3.unitVector(Vec3.cross(x, y));
            Vec3 xx = Vec3.unitVector(Vec3.cross(y, zz));
            return new OrthoNormalBasis(xx, y, zz);
        }

        public static OrthoNormalBasis fromXZ(Vec3 x, Vec3 z)
        {
            Vec3 yy = Vec3.unitVector(Vec3.cross(z, x));
            Vec3 zz = Vec3.unitVector(Vec3.cross(x, yy));
            return new OrthoNormalBasis(x, yy, zz);
        }

        public static OrthoNormalBasis fromZX(Vec3 z, Vec3 x)
        {
            Vec3 yy = Vec3.unitVector(Vec3.cross(z, x));
            Vec3 xx = Vec3.unitVector(Vec3.cross(yy, z));
            return new OrthoNormalBasis(xx, yy, z);
        }

        public static OrthoNormalBasis fromYZ(Vec3 y, Vec3 z)
        {
            Vec3 xx = Vec3.unitVector(Vec3.cross(y, z));
            Vec3 zz = Vec3.unitVector(Vec3.cross(xx, y));
            return new OrthoNormalBasis(xx, y, zz);
        }

        public static OrthoNormalBasis fromZY(Vec3 z, Vec3 y)
        {
            Vec3 xx = Vec3.unitVector(Vec3.cross(y, z));
            Vec3 yy = Vec3.unitVector(Vec3.cross(z, xx));
            return new OrthoNormalBasis(xx, yy, z);
        }

        public static OrthoNormalBasis fromZ(Vec3 z) 
        {
            Vec3 xx;
            if (XMath.Abs(Vec3.dot(z, Vec3.xAxis)) > 0.99999f)
            {
                xx = Vec3.unitVector(Vec3.cross(Vec3.yAxis, z));
            }
            else
            {
                xx = Vec3.unitVector(Vec3.cross(Vec3.xAxis, z));
            }
            Vec3 yy = Vec3.unitVector(Vec3.cross(z, xx));
            return new OrthoNormalBasis(xx, yy, z);
        }
    }
}
