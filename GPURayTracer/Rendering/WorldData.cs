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
        public List<Triangle> triangles;
        public List<Triangle> triNormals;

        private bool materialsDirty = true;
        private bool spheresDirty = true;
        private bool trianglesDirty = true;
        private bool triNormalsDirty = true;
        
        private MemoryBuffer<MaterialData> device_materials;
        private MemoryBuffer<Sphere> device_spheres;
        private MemoryBuffer<Triangle> device_triangles;
        private MemoryBuffer<Triangle> device_triNormals;

        public WorldData(Accelerator device)
        {
            this.device = device;

            spheres = new List<Sphere>();
            materials = new List<MaterialData>();
            triNormals = new List<Triangle>();
            triangles = new List<Triangle>();

            int boxMat = addMaterial(MaterialData.makeDiffuse(new Vec3(0.20, 0.30, 0.36)));
            
            Vec3 tl = new Vec3( -0.5f, -0.5f, -0.5f );
            Vec3 tr = new Vec3(  0.5f, -0.5f, -0.5f );
            Vec3 bl = new Vec3( -0.5f,  0.5f, -0.5f );
            Vec3 br = new Vec3(  0.5f,  0.5f, -0.5f);

            addTriangle(new Triangle(tl, tr, bl, boxMat));
            addTriangle(new Triangle(tr, bl, br, boxMat));

            //addSphere(new Sphere(new Vec3(0, 0, -0.5f), 0.25f, addMaterial(MaterialData.makeDiffuse(new Vec3(0.9, 0.5, 0.5)))));
            //addSphere(new Sphere(new Vec3(0, 100.5, -1), 100, addMaterial(MaterialData.makeDiffuse(new Vec3(0.8, 0.8, 0.8)))));
            //addSphere(new Sphere(new Vec3(1, 0, -1),  0.5f, addMaterial(MaterialData.makeMirror(new Vec3(0.8, 0.6, 0.2), 0))));
            addSphere(new Sphere(new Vec3(-3, 0,-3), 0.5f, addMaterial(MaterialData.makeMirror(new Vec3(0.9, 0.9, 0.9), 0))));
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

        public ArrayView<Triangle> getDeviceTriangles()
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

        public ArrayView<Triangle> getDeviceTriNormals()
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
    }
}
