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
        public ArrayView<Sphere> device_spheres;
        public ArrayView<MaterialData> device_materials;
        public ArrayView<Triangle> device_triangles;
        public ArrayView<Triangle> device_triNormals;

        public WorldBuffer(ArrayView<int> lightSphereIDs, ArrayView<Sphere> device_spheres, ArrayView<MaterialData> device_materials, ArrayView<Triangle> device_triangles, ArrayView<Triangle> device_triNormals)
        {
            this.lightSphereIDs = lightSphereIDs;
            this.device_spheres = device_spheres;
            this.device_materials = device_materials;
            this.device_triangles = device_triangles;
            this.device_triNormals = device_triNormals;
        }
    }
}
