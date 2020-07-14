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

            //                radius, position,                    emission,        color,              material
            //        Sphere( 1e5,    Vec(1e5 + 1, 40.8, 81.6),    Vec(),           Vec(.75, .25, .25), DIFF),  // Left
            //        Sphere( 1e5,    Vec(-1e5 + 99, 40.8, 81.6),  Vec(),           Vec(.25, .25, .75), DIFF),// Rght
            //        Sphere( 1e5,    Vec(50, 40.8, 1e5),          Vec(),           Vec(.75, .75, .75), DIFF),        // Back
            //        Sphere( 1e5,    Vec(50, 40.8, -1e5 + 170),   Vec(),           Vec(),              DIFF),              // Frnt
            //        Sphere( 1e5,    Vec(50, 1e5, 81.6),          Vec(),           Vec(.75, .75, .75), DIFF),        // Botm
            //        Sphere( 1e5,    Vec(50, -1e5 + 81.6, 81.6),  Vec(),           Vec(.75, .75, .75), DIFF),// Top
            //        Sphere( 16.5,   Vec(27, 16.5, 47),           Vec(),           Vec(1, 1, 1) * .999,SPEC),       // Mirr
            //        Sphere( 16.5,   Vec(73, 16.5, 78),           Vec(),           Vec(1, 1, 1) * .999,REFR),       // Glas
            //        Sphere( 600,    Vec(50, 681.6 - .27, 81.6),  Vec(12, 12, 12), Vec(),              DIFF)     // Lite

            addSphere(new Sphere(new Vec3(1e5 + 1, 40.8, 81.6), 1e5f, addMaterial(MaterialData.makeDiffuse(new Vec3(.75, .25, .25)))));
            addSphere(new Sphere(new Vec3(-1e5 + 99, 40.8, 81.6), 1e5f, addMaterial(MaterialData.makeDiffuse(new Vec3(.25, .25, .75)))));
            addSphere(new Sphere(new Vec3(50, 40.8, 1e5), 1e5f, addMaterial(MaterialData.makeDiffuse(new Vec3(.75, .75, .75)))));

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
