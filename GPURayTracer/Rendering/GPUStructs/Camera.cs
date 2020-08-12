using ILGPU.Algorithms;
using System;
using System.Collections.Generic;
using System.Text;
using System.Numerics;
using GPURayTracer.Utils;
using System.Runtime.InteropServices;
using ILGPU.Runtime;
using System.Runtime.CompilerServices;

namespace GPURayTracer.Rendering
{
    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct Camera
    {
        public SpecializedValue<int> height;
        public SpecializedValue<int> width;
        public SpecializedValue<int> maxBounces;

        public float verticalFov;

        public Vec3 origin;
        public Vec3 lookAt;
        public Vec3 up;
        public OrthoNormalBasis axis;

        public float aspectRatio;
        public float cameraPlaneDist;
        public float reciprocalHeight;
        public float reciprocalWidth;

        public Camera(Camera camera, Vec3 movement, Vec3 turn)
        {
            this.width = camera.width;
            this.height = camera.height;
            this.maxBounces = camera.maxBounces;

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

        public Camera(Camera camera, int width, int height, float verticalFov)
        {
            this.width = new SpecializedValue<int>(width);
            this.height = new SpecializedValue<int>(height);
            this.maxBounces = camera.maxBounces;

            this.verticalFov = verticalFov;

            this.origin = camera.origin;
            this.lookAt = camera.lookAt;
            this.up = camera.up;

            axis = OrthoNormalBasis.fromZY(Vec3.unitVector(lookAt - origin), up);

            aspectRatio = ((float)width / (float)height);
            cameraPlaneDist = 1.0f / XMath.Tan(verticalFov * XMath.PI / 360.0f);
            reciprocalHeight = 1.0f / height;
            reciprocalWidth = 1.0f / width;
        }

        public Camera(Vec3 origin, Vec3 lookAt, Vec3 up, int width, int height, int maxBounces, float verticalFov)
        {
            this.width = new SpecializedValue<int>(width);
            this.height = new SpecializedValue<int>(height);
            this.maxBounces = new SpecializedValue<int>(maxBounces);
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

        public static Camera UpdateMovement(Camera camera, InputManager input)
        {
            Vec3 movement = new Vec3();
            float speed = 0.1f;
            bool moved = false;

            if (input.IsKeyHeld(OpenTK.Input.Key.W))
            {
                movement += ((camera.lookAt - camera.origin) * speed);
                //movement.y = 0;
                moved = true;
            }

            if (input.IsKeyHeld(OpenTK.Input.Key.S))
            {
                movement -= (camera.lookAt - camera.origin) * speed;
                movement.y = 0;
                moved = true;
            }

            if (input.IsKeyHeld(OpenTK.Input.Key.D))
            {
                movement -= Vec3.cross(camera.up, (camera.lookAt - camera.origin)) * speed;
                movement.y = 0;
                moved = true;
            }

            if (input.IsKeyHeld(OpenTK.Input.Key.A))
            {
                movement += Vec3.cross(camera.up, (camera.lookAt - camera.origin)) * speed;
                movement.y = 0;
                moved = true;
            }

            if (moved)
            {
                return new Camera(camera, movement, new Vec3());
            }
            else
            {
                return camera;
            }

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
