using ILGPU;
using ILGPU.Runtime;
using System;
using System.Collections.Generic;
using System.Text;

namespace GPURayTracer.Rendering.Primitives
{
    public struct GPUMesh
    {
        public AABB aabb;
        public Vec3 position;
        public int triangleStartIndex;
        public int triangleCount;

        public GPUMesh(Vec3 position, int triangleStartIndex, int triangleCount, AABB aabb)
        {
            this.position = position;
            this.triangleStartIndex = triangleStartIndex;
            this.triangleCount = triangleCount;
            this.aabb = aabb;
        }
    }
}
