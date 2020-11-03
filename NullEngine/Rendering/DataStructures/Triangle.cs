using System;
using System.Collections.Generic;
using System.Text;

namespace NullEngine.Rendering.DataStructures
{
    public struct Triangle
    {
        public Vec3 Vert0;
        public Vec3 Vert1;
        public Vec3 Vert2;

        public Triangle(Vec3 vert0, Vec3 vert1, Vec3 vert2)
        {
            Vert0 = vert0;
            Vert1 = vert1;
            Vert2 = vert2;
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
