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
        public List<Light> lights;

        private bool materialsDirty = true;
        private MemoryBuffer<MaterialData> device_materials;
        private bool spheresDirty = true;
        private MemoryBuffer<Sphere> device_spheres;
        private bool lightsDirty = true;
        private MemoryBuffer<Light> device_Lights;

        public WorldData(Accelerator device)
        {
            this.device = device;

            spheres = new List<Sphere>();
            materials = new List<MaterialData>();
            lights = new List<Light>();

            addMaterial(MaterialData.makeDiffuse(new Vec3(0.75f, 0.75f, 0.75f)));
            addMaterial(MaterialData.makeDiffuse(new Vec3(0.75f, 0, 0)));
            addMaterial(MaterialData.makeDiffuse(new Vec3(0, 0.75f, 0)));

            //addSphere(new Sphere(new Vec3(1e5f + 1.0f, 40.8f, 81.6f), 1e5f, 1));//left
            //addSphere(new Sphere(new Vec3(-1e5f + 99.0f, 40.8f, 81.6f), 1e5f, 2));//right
            //addSphere(new Sphere(new Vec3(50f, 40.8f, 1e5f), 1e5f, 0));//back
            //addSphere(new Sphere(new Vec3(50f, 40.8f, -1e5f + 170.0f), 1e5f, 0));//front
            //addSphere(new Sphere(new Vec3(50f, 1e5f, 81.6f), 1e5f, 0));//bottom
            //addSphere(new Sphere(new Vec3(50f, -1e5f + 81.6f, 81.6f), 1e5f, 0));//top

            addSphere(new Sphere(new Vec3(), 0.25f, 0));
            addSphere(new Sphere(new Vec3(1, 0, 0), 0.25f, 1));
            addSphere(new Sphere(new Vec3(-1, 0, 0), 0.25f, 2));

            addLight(new Light(new Vec3(0, 100, 0), 1));
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

        public int addLight(Light toAdd)
        {
            if (lights.Contains(toAdd))
            {
                return lights.IndexOf(toAdd);
            }
            else
            {
                lightsDirty = true;
                lights.Add(toAdd);
                return lights.Count - 1;
            }
        }

        public ArrayView<Light> getDeviceLights()
        {
            if (lightsDirty)
            {
                if (device_Lights != null)
                {
                    device_Lights.Dispose();
                    device_Lights = null;
                }

                device_Lights = device.Allocate(lights.ToArray());
                lightsDirty = false;
            }

            return device_Lights;
        }
    }
}
