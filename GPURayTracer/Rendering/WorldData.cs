using GPURayTracer.Loaders;
using GPURayTracer.Rendering.GPUStructs;
using GPURayTracer.Rendering.Primitives;
using ILGPU;
using ILGPU.IR.Types;
using ILGPU.Runtime;
using System;
using System.Collections.Generic;
using System.Text;

namespace GPURayTracer.Rendering
{
    public class WorldData
    {
        public Accelerator device;
        public hWorldBuffer worldBuffer;

        public WorldData(Accelerator device)
        {
            this.device = device;

            worldBuffer = new hWorldBuffer(device);

            worldBuffer.addGPUMesh(MeshLoader.LoadMeshFromFile(new Vec3(0, -1.5, 2), this, "Assets/defaultcube/defaultcube"));
            //worldBuffer.addGPUMesh(MeshLoader.LoadMeshFromFile(new Vec3(), this, "Assets/Tree/LowPolyTree"));
            //worldBuffer.addGPUMesh(MeshLoader.LoadMeshFromFile(new Vec3(0, 0.5, 0), this, "Assets/cat/cat"));
            //worldBuffer.addGPUMesh(MeshLoader.LoadMeshFromFile(new Vec3(), this, "Assets/cornellbox/cornellbox"));

            worldBuffer.addSphere(new Sphere(new Vec3(0, -1000, 0), 1, worldBuffer.addMaterial(MaterialData.makeLight(new Vec3(1, 1, 1)))));
            //worldBuffer.addSphere(new Sphere(new Vec3(0, 1000.5, -1), 1000, worldBuffer.addMaterial(MaterialData.makeDiffuse(new Vec3(0.99f, 0.99f, 0.99f)))));
            worldBuffer.addSphere(new Sphere(new Vec3(1.5, -1, -1), 0.5f, worldBuffer.addMaterial(MaterialData.makeGlass(new Vec3(0.99f, 0.99f, 0.99f), 1.3f))));
            worldBuffer.addSphere(new Sphere(new Vec3(-1.5, -1, -1), 0.5f, worldBuffer.addMaterial(MaterialData.makeMirror(new Vec3(0.99f, 0.99f, 0.99f), 0f))));

            Random random = new Random(5);

            for (int i = 0; i < 25; i++)
            {
                float size = (float)((random.NextDouble() * 0.25f) + 0.25f);
                Vec3 pos = new Vec3((random.NextDouble() * 10f) - 5f, (size / 2f) - 1f, (random.NextDouble() * 10f) - 5f);
                worldBuffer.addSphere(new Sphere(pos, size, worldBuffer.addMaterial(MaterialData.makeDiffuse(new Vec3(
                    random.NextDouble() > 0.5 ? 0 : 1, random.NextDouble() > 0.5 ? 0 : 1, random.NextDouble() > 0.5 ? 0 : 1)))));
            }
        }

        public dWorldBuffer getDeviceWorldBuffer()
        {
            return worldBuffer.GetDWorldBuffer();
        }
    }
}
