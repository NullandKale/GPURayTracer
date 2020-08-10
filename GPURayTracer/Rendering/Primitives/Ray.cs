﻿using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace GPURayTracer.Rendering
{
    public readonly struct Ray
    {
        public readonly Vec3 a;
        public readonly Vec3 b;

        public Ray(Vec3 a, Vec3 b)
        {
            this.a = a;
            this.b = Vec3.unitVector(b);
        }


        public Vec3 pointAtParameter(float t)
        {
            return a + (t * b);
        }
    }
}
