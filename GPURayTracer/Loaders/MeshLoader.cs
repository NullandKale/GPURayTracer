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
        public static MaterialData LoadMaterialsFromFile(string filename)
        {
            return new MaterialData();
        }

        public static GPUMesh LoadMeshFromFile(Accelerator accelerator, string filename, int material)
        {
            return new GPUMesh();
        }
    }
}
