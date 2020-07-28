using GPURayTracer.Rendering;
using GPURayTracer.Rendering.Primitives;
using ILGPU.IR.Values;
using ILGPU.Runtime;
using System;
using System.Collections.Generic;
using System.Text;

namespace GPURayTracer.Loaders
{
    public static class MeshLoader
    {
        public static List<MaterialData> LoadMaterialsFromFile(string filename)
        {
            List<MaterialData> materials = new List<MaterialData>();

            MaterialData material = new MaterialData();
            int illum = 2;
            Vec3 ambientColor = new Vec3();


            return materials;
        }

        public static GPUMesh LoadMeshFromFile(Accelerator accelerator, string filename, int material)
        {
            return new GPUMesh();
        }
    }
}
