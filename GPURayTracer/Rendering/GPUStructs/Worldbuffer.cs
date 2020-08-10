using GPURayTracer.Rendering.Primitives;
using ILGPU;
using ILGPU.Runtime;
using System;
using System.Collections.Generic;
using System.Text;

namespace GPURayTracer.Rendering.GPUStructs
{
    public class hWorldBuffer
    {
        public Accelerator device;

        hVoxelChunk voxelChunk;

        public List<int> lightSphereIDs;
        public List<Sphere> spheres;
        public List<MaterialData> materials;
        public List<hGPUMesh> meshes;
        public List<dGPUMesh> dmeshes;

        public List<float> verticies;
        public List<int> triangles;
        public List<int> triangleMaterials;

        private bool materialsDirty = true;
        private bool spheresDirty = true;
        private bool meshesDirty = true;

        public MemoryBuffer<int> d_lightSphereIDs;
        public MemoryBuffer<Sphere> d_spheres;
        public MemoryBuffer<MaterialData> d_materials;
        public MemoryBuffer<dGPUMesh> d_dmeshes;
        public MemoryBuffer<float> d_verticies;
        public MemoryBuffer<int> d_triangles;
        public MemoryBuffer<int> d_triangleMaterials;

        public hWorldBuffer(Accelerator device)
        {
            this.device = device;
            lightSphereIDs = new List<int>();
            spheres = new List<Sphere>();
            materials = new List<MaterialData>();
            meshes = new List<hGPUMesh>();
            dmeshes = new List<dGPUMesh>();
            verticies = new List<float>();
            triangles = new List<int>();
            triangleMaterials = new List<int>();

            int[] tileMaterials =
            {
                -1,
                addMaterial(MaterialData.makeDiffuse(new Vec3(0, 0, 1))),
                addMaterial(MaterialData.makeDiffuse(new Vec3(1, 0, 0))),
                addMaterial(MaterialData.makeDiffuse(new Vec3(0, 1, 0))),
                addMaterial(MaterialData.makeGlass(new Vec3(1, 1, 1), 1.3f)),
                addMaterial(MaterialData.makeMirror(new Vec3(1, 1, 1), 0f)),
            };

            voxelChunk = new hVoxelChunk(device, new Vec3(-64, -64, -64), 128, 128, 128, 256, tileMaterials);
        }

        public int addMaterial(MaterialData toAdd)
        {
            if(materials.Contains(toAdd))
            {
                return materials.IndexOf(toAdd);
            }
            else
            {
                materials.Add(toAdd);
                return materials.Count - 1;
            }
        }

        public int addSphere(Sphere toAdd)
        {
            if (spheres.Contains(toAdd))
            {
                return spheres.IndexOf(toAdd);
            }
            else
            {
                spheres.Add(toAdd);
                return spheres.Count - 1;
            }
        }

        public int addGPUMesh(hGPUMesh toAdd)
        {
            if (meshes.Contains(toAdd))
            {
                return meshes.IndexOf(toAdd);
            }
            else
            {
                meshes.Add(toAdd);
                updateMeshData(toAdd);
                return spheres.Count - 1;
            }
        }

        public dWorldBuffer GetDWorldBuffer()
        {
            return new dWorldBuffer(getDeviceSpheres(), getDeviceLightSphereIDs(), getDeviceMaterials(), getDeviceMeshes(), getDeviceVerts(), getDeviceTriangles(), getDeviceTriangleMats(), voxelChunk.GetDeviceVoxelChunk());
        }

        private void updateMeshData(hGPUMesh toAdd)
        {
            int vertIndex = verticies.Count;
            verticies.AddRange(toAdd.verticies);

            int triangleIndex = triangles.Count;
            triangles.AddRange(toAdd.triangles);

            int matIndex = triangleMaterials.Count;
            triangleMaterials.AddRange(toAdd.triangleMaterials);

            dmeshes.Add(new dGPUMesh(toAdd.aabb, toAdd.position, vertIndex, triangleIndex, matIndex, toAdd.triangleCount));
        }

        private ArrayView<MaterialData> getDeviceMaterials()
        {
            if (materialsDirty && materials.Count > 0)
            {
                if (d_materials != null)
                {
                    d_materials.Dispose();
                    d_materials = null;
                }

                var temp = materials.ToArray();
                d_materials = device.Allocate<MaterialData>(temp.Length);
                d_materials.CopyFrom(temp, Index1.Zero, Index1.Zero, d_materials.Extent);
                materialsDirty = false;
            }

            return d_materials;
        }

        private ArrayView<Sphere> getDeviceSpheres()
        {
            if (spheresDirty && spheres.Count > 0)
            {
                if (d_spheres != null)
                {
                    d_spheres.Dispose();
                    d_spheres = null;
                }

                var temp = spheres.ToArray();
                d_spheres = device.Allocate<Sphere>(temp.Length);
                d_spheres.CopyFrom(temp, Index1.Zero, Index1.Zero, d_spheres.Extent);

                var lights = buildSphereLights();
                d_lightSphereIDs = device.Allocate<int>(lights.Length);
                d_lightSphereIDs.CopyFrom(lights, Index1.Zero, Index1.Zero, d_lightSphereIDs.Extent);

                spheresDirty = false;
            }

            return d_spheres;
        }

        private ArrayView<int> getDeviceLightSphereIDs()
        {
            if (spheresDirty && spheres.Count > 0)
            {
                //forceUpdate
                getDeviceSpheres();
            }

            return d_lightSphereIDs;
        }

        private ArrayView<dGPUMesh> getDeviceMeshes()
        {
            if (meshesDirty && meshes.Count > 0)
            {
                if (d_dmeshes != null)
                {
                    d_dmeshes.Dispose();
                    d_dmeshes = null;
                }

                var temp = dmeshes.ToArray();
                d_dmeshes = device.Allocate<dGPUMesh>(temp.Length);
                d_dmeshes.CopyFrom(temp, Index1.Zero, Index1.Zero, d_dmeshes.Extent);

                var verts = verticies.ToArray();
                d_verticies = device.Allocate<float>(verts.Length);
                d_verticies.CopyFrom(verts, Index1.Zero, Index1.Zero, d_verticies.Extent);

                var triInd = triangles.ToArray();
                d_triangles = device.Allocate<int>(triInd.Length);
                d_triangles.CopyFrom(triInd, Index1.Zero, Index1.Zero, d_triangles.Extent);

                var triMat = triangleMaterials.ToArray();
                d_triangleMaterials = device.Allocate<int>(triMat.Length);
                d_triangleMaterials.CopyFrom(triMat, Index1.Zero, Index1.Zero, d_triangleMaterials.Extent);

                meshesDirty = false;
            }

            return d_dmeshes;
        }

        private ArrayView<float> getDeviceVerts()
        {
            if (meshesDirty && meshes.Count > 0)
            {
                //forceUpdate
                getDeviceMeshes();
            }

            return d_verticies;
        }

        private ArrayView<int> getDeviceTriangles()
        {
            if (meshesDirty && meshes.Count > 0)
            {
                //forceUpdate
                getDeviceMeshes();
            }

            return d_triangles;
        }

        private ArrayView<int> getDeviceTriangleMats()
        {
            if (meshesDirty && meshes.Count > 0)
            {
                //forceUpdate
                getDeviceMeshes();
            }

            return d_triangleMaterials;
        }

        private int[] buildSphereLights()
        {
            List<int> lights = new List<int>();

            for (int i = 0; i < spheres.Count; i++)
            {
                if (materials[spheres[i].materialIndex].type == 3)
                {
                    lights.Add(i);
                }
            }

            return lights.ToArray();
        }
    }

    public struct dWorldBuffer
    {
        public ArrayView<int> lightSphereIDs;
        public ArrayView<Sphere> spheres;
        public ArrayView<MaterialData> materials;

        //MeshData
        public ArrayView<dGPUMesh> meshes;
        public ArrayView<float> verticies;
        public ArrayView<int> triangles;
        public ArrayView<int> triangleMaterials;

        public dVoxelChunk VoxelChunk;

        public dWorldBuffer(ArrayView<Sphere> spheres, ArrayView<int> lightSphereIDs, ArrayView<MaterialData> materials, ArrayView<dGPUMesh> meshes, ArrayView<float> verticies, ArrayView<int> triangles, ArrayView<int> triangleMaterials, dVoxelChunk voxelChunk)
        {
            this.lightSphereIDs = lightSphereIDs;
            this.spheres = spheres;
            this.materials = materials;
            this.meshes = meshes;
            this.verticies = verticies;
            this.triangles = triangles;
            this.triangleMaterials = triangleMaterials;
            VoxelChunk = voxelChunk;
        }
    }
}
