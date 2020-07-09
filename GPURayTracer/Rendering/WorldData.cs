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

            addMaterial(MaterialData.makeLight(new Vec3(0.5f, 1, 1)));
            addSphere(new Sphere(new Vec3(6, 6, -6.2f), 3, 0));

            MaterialData sphereMat = MaterialData.makeDiffuse(new Vec3(0.2f, 0.2f, 0.2f));
            sphereMat.ref_idx = 1.3f;
            sphereMat.reflectionConeAngleRadians = 0.05f;

            addMaterial(sphereMat);
            addSphere(new Sphere(new Vec3(0.5f, 0, 0), 0.25f, 1));
            addSphere(new Sphere(new Vec3(-0.5f, 0, 0), 0.25f, 1));

            //addMaterial(MaterialData.makeDiffuse(new Vec3(0.2f, 0.2f, 0.5f)));
            //addSphere(new Sphere(new Vec3(), 10, 2));
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

                device_materials = device.Allocate(materials.ToArray());
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

                device_spheres = device.Allocate(spheres.ToArray());
                spheresDirty = false;
            }

            return device_spheres;
        }
    }
}
