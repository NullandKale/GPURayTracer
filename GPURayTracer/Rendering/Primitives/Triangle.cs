using ILGPU.Algorithms;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace GPURayTracer.Rendering.Primitives
{
    public struct Triangle
    {
        public Vec3 Vert0;
        public Vec3 Vert1;
        public Vec3 Vert2;
        public int MaterialID;

        public Triangle(Vec3 vert0, Vec3 vert1, Vec3 vert2, int MaterialID)
        {
            Vert0 = vert0;
            Vert1 = vert1;
            Vert2 = vert2;
            this.MaterialID = MaterialID;
        }


        public Vec3 uVector()
        {
            return Vert1 - Vert0;
        }


        public Vec3 vVector()
        {
            return Vert2 - Vert0;
        }


        public Vec3 faceNormal()
        {
            return Vec3.unitVector(Vec3.cross(Vert1 - Vert0, Vert2 - Vert0));
        }
    }
}
