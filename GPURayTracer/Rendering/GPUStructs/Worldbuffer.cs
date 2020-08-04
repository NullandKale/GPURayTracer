using GPURayTracer.Rendering.Primitives;
using ILGPU;
using ILGPU.Runtime;
using System;
using System.Collections.Generic;
using System.Text;

namespace GPURayTracer.Rendering.GPUStructs
{
    public struct WorldBuffer
    {
        public ArrayView<int> lightSphereIDs;
        public ArrayView<Sphere> spheres;
        public ArrayView<MaterialData> materials;
        public ArrayView<Triangle> triangles;
        public ArrayView<GPUMesh> meshes;

        public WorldBuffer(ArrayView<int> lightSphereIDs, ArrayView<Sphere> spheres, ArrayView<MaterialData> materials, ArrayView<Triangle> triangles, ArrayView<GPUMesh> meshes)
        {
            this.lightSphereIDs = lightSphereIDs;
            this.spheres = spheres;
            this.materials = materials;
            this.triangles = triangles;
            this.meshes = meshes;
        }
    }
}
