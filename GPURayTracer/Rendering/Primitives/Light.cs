using System;
using System.Collections.Generic;
using System.Text;

namespace GPURayTracer.Rendering.Primitives
{
    public readonly struct Light
    {
        public readonly Vec3 position;
        public readonly float intensity;

        public Light(Vec3 position, float intensity)
        {
            this.position = position;
            this.intensity = intensity;
        }
    }
}
