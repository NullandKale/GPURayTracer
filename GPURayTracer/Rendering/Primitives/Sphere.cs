using ILGPU.Algorithms;
using System;
using System.Collections.Generic;
using System.Text;

namespace GPURayTracer.Rendering.Primitives
{
    public readonly struct Sphere
    {
        public readonly Vec3 center;
        public readonly float radius;
        public readonly float radiusSquared;
        public readonly int materialIndex;

        public Sphere(Vec3 center, float radius, int materialIndex)
        {
            this.center = center;
            this.radius = radius;
            radiusSquared = radius * radius;
            this.materialIndex = materialIndex;
        }
    }
}
