using GPURayTracer.Rendering.Primitives;
using ILGPU;
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

        private bool materialsDirty = true;
        private MemoryBuffer<MaterialData> device_materials;
        private bool spheresDirty = true;
        private MemoryBuffer<Sphere> device_spheres;

        public WorldData(Accelerator device)
        {
            this.device = device;

            spheres = new List<Sphere>();
            materials = new List<MaterialData>();

            addMaterial(MaterialData.makeLight(new Vec3(10, 10, 10) * 5));
            addMaterial(MaterialData.makeDiffuse(new Vec3(1, 0.01, 0.01)));
            addMaterial(MaterialData.makeDiffuse(new Vec3(0.01, 1, 0.01)));
            addMaterial(MaterialData.makeDiffuse(new Vec3(0.01, 0.01, 1)));
            addMaterial(MaterialData.makeDiffuse(new Vec3(0.75, 0.01, 0.75)));

            addSphere(new Sphere(new Vec3(0,  0, 0), 0.5f, 0));
            addSphere(new Sphere(new Vec3(2,  0, 0), 0.5f, 1));
            addSphere(new Sphere(new Vec3(-2, 0, 0), 0.5f, 2));
            addSphere(new Sphere(new Vec3(0, -2, 0), 0.5f, 3));
            addSphere(new Sphere(new Vec3(0,  2, 0), 0.5f, 4));
            addSphere(new Sphere(new Vec3(0,  2, 0), 0.5f, 4));
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

        public ArrayView<MaterialData> getDeviceMaterials()
        {
            if(materialsDirty && materials.Count > 0)
            {
                if(device_materials != null)
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

        public ArrayView<Sphere> getDeviceSpheres()
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
                spheresDirty = false;
            }

            return device_spheres;
        }
    }
}
