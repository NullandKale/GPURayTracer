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
        public List<Triangle> triangles;
        public List<Triangle> triNormals;

        private bool materialsDirty = true;
        private bool spheresDirty = true;
        private bool trianglesDirty = true;
        private bool triNormalsDirty = true;

        public MemoryBuffer<int> lightSphereIDs;
        public MemoryBuffer<Sphere> device_spheres;
        public MemoryBuffer<MaterialData> device_materials;
        public MemoryBuffer<Triangle> device_triangles;
        public MemoryBuffer<Triangle> device_triNormals;

        public WorldData(Accelerator device)
        {
            this.device = device;

            spheres = new List<Sphere>();
            materials = new List<MaterialData>();
            triNormals = new List<Triangle>();
            triangles = new List<Triangle>();

            int boxMat = addMaterial(MaterialData.makeDiffuse(new Vec3(0.20, 0.30, 0.36)));

            Vec3 tl = new Vec3(-0.5f, -0.5f, -0.5f);
            Vec3 tr = new Vec3(0.5f, -0.5f, -0.5f);
            Vec3 bl = new Vec3(-0.5f, 0.5f, -0.5f);
            Vec3 br = new Vec3(0.5f, 0.5f, -0.5f);

            addTriangle(new Triangle(tl, tr, bl, boxMat));
            addTriangle(new Triangle(tr, bl, br, boxMat));

            addSphere(new Sphere(new Vec3(0, -1000, -0.5f), 10f, addMaterial(MaterialData.makeLight(new Vec3(1, 1, 1)))));
            addSphere(new Sphere(new Vec3(0, -1, -0.5f), 0.25f, addMaterial(MaterialData.makeDiffuse(new Vec3(0.4, 0.2, 0.2)))));
            addSphere(new Sphere(new Vec3(0, 1000.5, -1), 1000, addMaterial(MaterialData.makeDiffuse(new Vec3(0.8, 0.8, 0.8)))));
            addSphere(new Sphere(new Vec3(1, 0, -1), 0.5f, addMaterial(MaterialData.makeDiffuse(new Vec3(0.4, 0.4, 0.9)))));
            addSphere(new Sphere(new Vec3(-1, 0, -1), 0.5f, addMaterial(MaterialData.makeGlass(new Vec3(0.9, 0.9, 0.9), 1.5f))));

            Random random = new Random();

            for (int i = 0; i < 100; i++)
            {
                float size = (float)((random.NextDouble() * 0.25) + 0.25);
                Vec3 pos = new Vec3((random.NextDouble() * 40) - 20, size / 2, (random.NextDouble() * 40) - 20);
                addSphere(new Sphere(pos, size, addMaterial(MaterialData.makeDiffuse(new Vec3(random.NextDouble(), random.NextDouble(), random.NextDouble())))));
            }

            //for (int i = 0; i < 10; i++)
            //{
            //    float size = (float)((random.NextDouble() * 0.25) + 0.25);
            //    Vec3 pos = new Vec3((random.NextDouble() * 40) - 20, size / 2, (random.NextDouble() * 40) - 20);
            //    if (random.NextDouble() >= 0.5)
            //    {
            //        addSphere(new Sphere(pos, size, addMaterial(MaterialData.makeMirror(new Vec3(random.NextDouble(), random.NextDouble(), random.NextDouble()), i % 2 == 0 ? 0.4f : 0))));
            //    }
            //    else
            //    {
            //        addSphere(new Sphere(pos, size, addMaterial(MaterialData.makeLight(new Vec3(random.NextDouble(), random.NextDouble(), random.NextDouble())))));
            //    }
            //}
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

        public WorldBuffer GetWorldBuffer()
        {
            return new WorldBuffer(getDeviceLightSphereIDs(), getDeviceSpheres(), getDeviceMaterials(), getDeviceTriangles(), getDeviceTriNormals());
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
