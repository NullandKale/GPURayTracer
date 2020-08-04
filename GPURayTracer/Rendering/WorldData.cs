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

        public List<Sphere> spheres;
        public List<MaterialData> materials;
        public List<GPUMesh> meshes;

        private List<Triangle> triangles;
        private List<Triangle> triNormals;

        private bool materialsDirty = true;
        private bool spheresDirty = true;
        private bool trianglesDirty = true;
        private bool triNormalsDirty = true;
        private bool meshesDirty = true;

        private MemoryBuffer<int> lightSphereIDs;
        private MemoryBuffer<Sphere> device_spheres;
        private MemoryBuffer<MaterialData> device_materials;
        private MemoryBuffer<Triangle> device_triangles;
        private MemoryBuffer<Triangle> device_triNormals;
        private MemoryBuffer<GPUMesh> device_meshes;

        public WorldData(Accelerator device)
        {
            this.device = device;

            spheres = new List<Sphere>();
            materials = new List<MaterialData>();
            meshes = new List<GPUMesh>();
            triangles = new List<Triangle>();
            triNormals = new List<Triangle>();

            //addGPUMesh(MeshLoader.LoadMeshFromFile(device, this, "Assets/defaultcube/defaultcube"));
            addGPUMesh(MeshLoader.LoadMeshFromFile(device, this, "Assets/cat/cat"));
            //addGPUMesh(MeshLoader.LoadMeshFromFile(device, this, "Assets/cornellbox/cornellbox"));

            addSphere(new Sphere(new Vec3(-0.24, -1.98, 0.16), 0.25f, addMaterial(MaterialData.makeLight(new Vec3(1, 1, 1)))));
        }

        public int addMaterial(MaterialData toAdd)
        {
            if(materials.Contains(toAdd))
            {
                return materials.IndexOf(toAdd);
            }
            else
            {
                materialsDirty = true;
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
                spheresDirty = true;
                spheres.Add(toAdd);
                return spheres.Count - 1;
            }
        }

        public int addTriangle(Triangle toAdd)
        {
            Triangle normal = new Triangle(toAdd.faceNormal(), toAdd.faceNormal(), toAdd.faceNormal(), 0);
            if (triangles.Contains(toAdd))
            {
                triNormals[triangles.IndexOf(toAdd)] = normal;
                triNormalsDirty = true;
                return triangles.IndexOf(toAdd);
            }
            else
            {
                trianglesDirty = true;
                triNormalsDirty = true;

                triangles.Add(toAdd);
                triNormals.Add(normal);

                return triangles.Count - 1;
            }
        }

        public int addGPUMesh(GPUMesh toAdd)
        {
            if(meshes.Contains(toAdd))
            {
                return meshes.IndexOf(toAdd);
            }
            else
            {
                meshesDirty = true;
                meshes.Add(toAdd);
                return meshes.Count - 1;
            }
        }

        public WorldBuffer GetWorldBuffer()
        {
            return new WorldBuffer(getDeviceLightSphereIDs(), getDeviceSpheres(), getDeviceMaterials(), getDeviceTriangles(), getDeviceMeshes());
        }

        private ArrayView<MaterialData> getDeviceMaterials()
        {
            if (materialsDirty && materials.Count > 0)
            {
                if (device_materials != null)
                {
                    device_materials.Dispose();
                    device_materials = null;
                }

                var temp = materials.ToArray();
                device_materials = device.Allocate<MaterialData>(temp.Length);
                device_materials.CopyFrom(temp, Index1.Zero, Index1.Zero, device_materials.Extent);
                materialsDirty = false;
            }

            return device_materials;
        }


        private ArrayView<Sphere> getDeviceSpheres()
        {
            if (spheresDirty && spheres.Count > 0)
            {
                if (device_spheres != null)
                {
                    device_spheres.Dispose();
                    device_spheres = null;
                }

                var temp = spheres.ToArray();
                device_spheres = device.Allocate<Sphere>(temp.Length);
                device_spheres.CopyFrom(temp, Index1.Zero, Index1.Zero, device_spheres.Extent);

                var lights = buildSphereLights();
                lightSphereIDs = device.Allocate<int>(lights.Length);
                lightSphereIDs.CopyFrom(lights, Index1.Zero, Index1.Zero, lightSphereIDs.Extent);

                spheresDirty = false;
            }

            return device_spheres;
        }

        private ArrayView<int> getDeviceLightSphereIDs()
        {
            if (spheresDirty && spheres.Count > 0)
            {
                if (device_spheres != null)
                {
                    device_spheres.Dispose();
                    device_spheres = null;
                }

                var temp = spheres.ToArray();
                device_spheres = device.Allocate<Sphere>(temp.Length);
                device_spheres.CopyFrom(temp, Index1.Zero, Index1.Zero, device_spheres.Extent);

                var lights = buildSphereLights();
                lightSphereIDs = device.Allocate<int>(lights.Length);
                lightSphereIDs.CopyFrom(lights, Index1.Zero, Index1.Zero, lightSphereIDs.Extent);

                spheresDirty = false;
            }

            return lightSphereIDs;
        }

        private ArrayView<Triangle> getDeviceTriangles()
        {
            if (trianglesDirty && triangles.Count > 0)
            {
                if (device_triangles != null)
                {
                    device_materials.Dispose();
                    device_materials = null;
                }

                var temp = triangles.ToArray();
                device_triangles = device.Allocate<Triangle>(temp.Length);
                device_triangles.CopyFrom(temp, Index1.Zero, Index1.Zero, device_triangles.Extent);
                trianglesDirty = false;
            }

            return device_triangles;
        }

        private ArrayView<Triangle> getDeviceTriNormals()
        {
            if (triNormalsDirty && triangles.Count > 0)
            {
                if (device_triNormals != null)
                {
                    device_triNormals.Dispose();
                    device_triNormals = null;
                }

                var temp = triNormals.ToArray();
                device_triNormals = device.Allocate<Triangle>(temp.Length);
                device_triNormals.CopyFrom(temp, Index1.Zero, Index1.Zero, device_triNormals.Extent);
                triNormalsDirty = false;
            }

            return device_triNormals;
        }

        private ArrayView<GPUMesh> getDeviceMeshes()
        {
            if(meshesDirty && meshes.Count > 0)
            {
                if(device_meshes != null)
                {
                    device_meshes.Dispose();
                    device_meshes = null;
                }

                var temp = meshes.ToArray();
                device_meshes = device.Allocate<GPUMesh>(temp.Length);
                device_meshes.CopyFrom(temp, Index1.Zero, Index1.Zero, device_meshes.Extent);
                meshesDirty = false;
            }

            return device_meshes;
        }

        private int[] buildSphereLights()
        {
            List<int> lights = new List<int>();

            for(int i = 0; i < spheres.Count; i++)
            {
                if(materials[spheres[i].materialIndex].type == 3)
                {
                    lights.Add(i);
                }
            }

            return lights.ToArray();
        }
    }
}
