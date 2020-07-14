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

            addMaterial(MaterialData.makeLight(new Vec3(80, 80, 80)));
            addSphere(new Sphere(new Vec3(0, -1, 1), 0.5f, 0));

            MaterialData sphereMat = new MaterialData(new Vec3(), new Vec3(0.2f, 0.2f, 0.2f), 1.3f, 0, 0.05f);

            MaterialData redSphereMat = MaterialData.makeDiffuse(new Vec3(0.8f, 0.2f, 0.2f));

            MaterialData bluephereMat = MaterialData.makeDiffuse(new Vec3(0.2f, 0.2f, 0.8f));


            addMaterial(sphereMat);
            addSphere(new Sphere(new Vec3(0, 0, 0), 0.25f, 1));

            addMaterial(redSphereMat);
            addSphere(new Sphere(new Vec3(1f, 0, 0), 0.25f, 2));
            addMaterial(bluephereMat);
            addSphere(new Sphere(new Vec3(-1f, 0, 0), 0.25f, 3));
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
            if(materialsDirty)
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
            if (spheresDirty)
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
