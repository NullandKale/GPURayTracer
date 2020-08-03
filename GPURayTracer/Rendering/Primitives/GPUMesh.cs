using ILGPU;
using ILGPU.Runtime;
using System;
using System.Collections.Generic;
using System.Text;

namespace GPURayTracer.Rendering.Primitives
{
    public struct GPUMesh
    {
        public Vec3 position;
        public int material;
        public int triangleStartIndex;
        public int triangleCount;

        public GPUMesh(Vec3 position, int material, int triangleStartIndex, int triangleCount)
        {
            this.position = position;
            this.material = material;
            this.triangleStartIndex = triangleStartIndex;
            this.triangleCount = triangleCount;
        }
    }
}
