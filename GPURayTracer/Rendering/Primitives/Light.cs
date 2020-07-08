using System;
using System.Collections.Generic;
using System.Text;

namespace GPURayTracer.Rendering.Primitives
{
    public struct Light
    {
        public Vec3 position;
        public float intensity;

        public Light(Vec3 position, float intensity)
        {
            this.position = position;
            this.intensity = intensity;
        }
    }
}
