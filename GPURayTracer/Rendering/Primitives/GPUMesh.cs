using GPURayTracer.Rendering.GPUStructs;
using ILGPU;
using ILGPU.Runtime;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace GPURayTracer.Rendering.Primitives
{
    public struct dGPUMesh
    {
        public AABB aabb;
        public Vec3 position;
        public int verticiesStartIndex;
        public int trianglesStartIndex;
        public int triangleMaterialsStartIndex;
        public int triangleCount;

        public dGPUMesh(AABB aabb, Vec3 position, int verticiesStartIndex, int trianglesStartIndex, int triangleMaterialsStartIndex, int triangleCount)
        {
            this.aabb = aabb;
            this.position = position;
            this.verticiesStartIndex = verticiesStartIndex;
            this.trianglesStartIndex = trianglesStartIndex;
            this.triangleMaterialsStartIndex = triangleMaterialsStartIndex;
            this.triangleCount = triangleCount;
        }


        public Triangle GetTriangle(int index, dWorldBuffer world)
        {
            int triangleIndex = index * 3;
            int vertexStartIndex0 = world.triangles[triangleIndex] * 3;
            int vertexStartIndex1 = world.triangles[triangleIndex + 1] * 3;
            int vertexStartIndex2 = world.triangles[triangleIndex + 2] * 3;

            Vec3 Vert0 = new Vec3(world.verticies[vertexStartIndex0], world.verticies[vertexStartIndex0 + 1], world.verticies[vertexStartIndex0 + 2]) + position;
            Vec3 Vert1 = new Vec3(world.verticies[vertexStartIndex1], world.verticies[vertexStartIndex1 + 1], world.verticies[vertexStartIndex1 + 2]) + position;
            Vec3 Vert2 = new Vec3(world.verticies[vertexStartIndex2], world.verticies[vertexStartIndex2 + 1], world.verticies[vertexStartIndex2 + 2]) + position;

            return new Triangle(Vert0, Vert1, Vert2, world.triangleMaterials[index]);
        }
    }
    public struct hGPUMesh
    {
        public AABB aabb;
        public Vec3 position;
        public List<float> verticies;
        public List<int> triangles;
        public List<int> triangleMaterials;
        public int triangleCount;

        public hGPUMesh(Vec3 position, List<float> verticies, List<int> triangles, List<int> triangleMaterials)
        {
            aabb = AABB.CreateFromVerticies(verticies, position);
            this.position = position;
            this.verticies = verticies;
            this.triangles = triangles;
            this.triangleMaterials = triangleMaterials;
            triangleCount = triangleMaterials.Count;
        }
    }
}
