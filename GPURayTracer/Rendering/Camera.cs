using System;
using System.Collections.Generic;
using System.Text;

namespace GPURayTracer.Rendering
{
    public struct Camera
    {
        public int height;
        public int width;

        public float viewport_height;
        public float viewport_width;
        public float focal_length;

        public Vec3 origin;
        public Vec3 horizontal;
        public Vec3 vertical;
        public Vec3 lower_left_corner;

        public Camera(int width, int height)
        {
            this.width = width;
            this.height = height;
            viewport_height = 2.0f;
            viewport_width = ((float)width / (float)height) * viewport_height;
            focal_length = 1.0f;

            origin = new Vec3(0, 0, 0);
            horizontal = new Vec3(viewport_width, 0, 0);
            vertical = new Vec3(0, viewport_height, 0);
            lower_left_corner = origin - horizontal / 2 - vertical / 2 - new Vec3(0, 0, focal_length);
        }

        public Ray GetRay(float u, float v)
        {
            return new Ray(origin, lower_left_corner + (u * horizontal) + (v * vertical) - origin);
        }
    }
}
