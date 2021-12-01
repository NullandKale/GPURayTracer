using ILGPU.Algorithms;
using ILGPU.Runtime;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace NullEngine.Rendering.DataStructures
{
    public struct Camera
    {
        public SpecializedValue<int> height { get; set; }
        public SpecializedValue<int> width { get; set; }
        public Vec3 noHitColor { get; set; }

        public float verticalFov { get; set; }

        public Vec3 origin { get; set; }
        public Vec3 lookAt { get; set; }
        public Vec3 up { get; set; }
        public OrthoNormalBasis axis { get; set; }

        public float aspectRatio { get; set; }
        public float cameraPlaneDist { get; set; }
        public float reciprocalHeight { get; set; }
        public float reciprocalWidth { get; set; }


        public Camera(Camera camera, Vec3 movement, Vec3 turn)
        {
            this.width = camera.width;
            this.height = camera.height;

            this.noHitColor = camera.noHitColor;

            Vector4 temp = camera.lookAt - camera.origin;

            if (turn.y != 0)
            {
                temp += Vector4.Transform(temp, Matrix4x4.CreateFromAxisAngle(Vec3.cross(Vec3.cross(camera.up, (camera.lookAt - camera.origin)), (camera.lookAt - camera.origin)), (float)turn.y));
            }
            if (turn.x != 0)
            {
                temp += Vector4.Transform(temp, Matrix4x4.CreateFromAxisAngle(Vec3.cross(camera.up, (camera.lookAt - camera.origin)), (float)turn.x));
            }

            lookAt = camera.origin + Vec3.unitVector(temp);

            this.origin = camera.origin + movement;
            this.lookAt += movement;
            this.up = camera.up;

            axis = OrthoNormalBasis.fromZY(Vec3.unitVector(lookAt - origin), up);

            aspectRatio = ((float)width / (float)height);
            cameraPlaneDist = 1.0f / XMath.Tan(camera.verticalFov * XMath.PI / 360.0f);
            this.verticalFov = camera.verticalFov;
            reciprocalHeight = 1.0f / height;
            reciprocalWidth = 1.0f / width;
        }

        public Camera(Camera camera, int width, int height)
        {
            this.width = new SpecializedValue<int>(width);
            this.height = new SpecializedValue<int>(height);
            this.noHitColor = camera.noHitColor;

            this.verticalFov = camera.verticalFov;

            this.origin = camera.origin;
            this.lookAt = camera.lookAt;
            this.up = camera.up;

            axis = OrthoNormalBasis.fromZY(Vec3.unitVector(lookAt - origin), up);

            aspectRatio = ((float)width / (float)height);
            cameraPlaneDist = 1.0f / XMath.Tan(verticalFov * XMath.PI / 360.0f);
            reciprocalHeight = 1.0f / height;
            reciprocalWidth = 1.0f / width;
        }

        public Camera(Vec3 origin, Vec3 lookAt, Vec3 up, int width, int height, float verticalFov, Vec3 noHitColor)
        {
            this.width = new SpecializedValue<int>(width);
            this.height = new SpecializedValue<int>(height);
            this.noHitColor = noHitColor;

            this.verticalFov = verticalFov;
            this.origin = origin;
            this.lookAt = lookAt;
            this.up = up;

            axis = OrthoNormalBasis.fromZY(Vec3.unitVector(lookAt - origin), up);

            aspectRatio = ((float)width / (float)height);
            cameraPlaneDist = 1.0f / XMath.Tan(verticalFov * XMath.PI / 360.0f);
            reciprocalHeight = 1.0f / height;
            reciprocalWidth = 1.0f / width;
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
            return rayFromUnit(2f * (x * reciprocalWidth) - 1f, 2f * (y * reciprocalHeight) - 1f);
        }
    }

    public struct OrthoNormalBasis
    {
        public Vec3 x { get; set; }
        public Vec3 y { get; set; }
        public Vec3 z { get; set; }

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
            if (XMath.Abs(Vec3.dot(z, new Vec3(1, 0, 0))) > 0.99999f)
            {
                xx = Vec3.unitVector(Vec3.cross(new Vec3(0, 1, 0), z));
            }
            else
            {
                xx = Vec3.unitVector(Vec3.cross(new Vec3(1, 0, 0), z));
            }
            Vec3 yy = Vec3.unitVector(Vec3.cross(z, xx));
            return new OrthoNormalBasis(xx, yy, z);
        }
    }
}
