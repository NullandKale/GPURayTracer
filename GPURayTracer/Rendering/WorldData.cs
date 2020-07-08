﻿using GPURayTracer.Rendering.Primitives;
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

            addMaterial(MaterialData.redRubber);
            addMaterial(MaterialData.mirror);

            addSphere(new Sphere(new Vec3(0, 0, -1), 0.5f, 0));
            addSphere(new Sphere(new Vec3(0, -100.5f, -1), 100, 0));

            addLight(new Light(new Vec3(0, 100, 0), 12));
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
