using ILGPU;
using System;
using System.Collections.Generic;
using System.Text;

namespace GPURayTracer.Rendering.Primitives
{
    public readonly struct GPUMesh
    {
        public readonly Vec3 position;
        public readonly int materialID;
        public readonly int TrianglesOffset;
        public readonly ArrayView<Triangle> triangles;
        public readonly ArrayView<Triangle> triNorms;

        public GPUMesh(Vec3 position, int materialID, int trianglesOffset, ArrayView<Triangle> triangles, ArrayView<Triangle> triNorms)
        {
            this.position = position;
            this.materialID = materialID;
            TrianglesOffset = trianglesOffset;
            this.triangles = triangles;
            this.triNorms = triNorms;
        }
    }
}
