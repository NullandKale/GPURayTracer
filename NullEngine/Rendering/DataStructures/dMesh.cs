using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace NullEngine.Rendering.DataStructures
{
    public struct dMesh
    {
        public int meshID;

        public AABB boundingBox;
        public Vec3 origin;
        public Vec3 rotation;

        public int vertsOffset;
        public int uvOffset;
        public int triangleOffset;
        public int triangleLength;

        public dMesh(int meshID, AABB boundingBox, Vec3 origin, Vec3 rotation, int vertsOffset, int uvOffset, int triangleOffset, int triangleLength)
        {
            this.meshID = meshID;
            this.boundingBox = boundingBox;
            this.origin = origin;
            this.rotation = rotation;
            this.vertsOffset = vertsOffset;
            this.uvOffset = uvOffset;
            this.triangleOffset = triangleOffset;
            this.triangleLength = triangleLength;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public List<Triangle> GetTriangles(RenderDataManager renderData)
        {
            List<Triangle> triangles = new List<Triangle>(triangleLength);
            
            for(int i = 0; i < triangleLength; i++)
            {
                triangles.Add(GetTriangle(i, renderData));
            }

            return triangles;
        }

        public float GetMeshHit(Ray r, ref HitRecord hit, dRenderData renderData)
        {
            for(int i = 0; i < triangleLength; i++)
            {
                GetTriangle(i, renderData).GetTriangleHit(r, i, ref hit);
            }

            return hit.t;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Triangle GetTriangle(int index, dRenderData renderData)
        {
            int triangleIndex = index * 3;
            int vertexStartIndex0 = (renderData.rawTriangleBuffers[triangleIndex] * 3);
            int vertexStartIndex1 = (renderData.rawTriangleBuffers[triangleIndex + 1] * 3);
            int vertexStartIndex2 = (renderData.rawTriangleBuffers[triangleIndex + 2] * 3);

            Vec3 Vert0 = new Vec3(renderData.rawVertexBuffers[vertexStartIndex0], renderData.rawVertexBuffers[vertexStartIndex0 + 1], renderData.rawVertexBuffers[vertexStartIndex0 + 2]) + origin;
            Vec3 Vert1 = new Vec3(renderData.rawVertexBuffers[vertexStartIndex1], renderData.rawVertexBuffers[vertexStartIndex1 + 1], renderData.rawVertexBuffers[vertexStartIndex1 + 2]) + origin;
            Vec3 Vert2 = new Vec3(renderData.rawVertexBuffers[vertexStartIndex2], renderData.rawVertexBuffers[vertexStartIndex2 + 1], renderData.rawVertexBuffers[vertexStartIndex2 + 2]) + origin;

            return new Triangle(Vert0, Vert1, Vert2);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Triangle GetTriangle(int index, RenderDataManager renderData)
        {
            int triangleIndex = index * 3;
            int vertexStartIndex0 = renderData.rawTriangleBuffers[triangleIndex] * 3;
            int vertexStartIndex1 = renderData.rawTriangleBuffers[triangleIndex + 1] * 3;
            int vertexStartIndex2 = renderData.rawTriangleBuffers[triangleIndex + 2] * 3;

            Vec3 Vert0 = new Vec3(renderData.rawVertexBuffers[vertexStartIndex0], renderData.rawVertexBuffers[vertexStartIndex0 + 1], renderData.rawVertexBuffers[vertexStartIndex0 + 2]) + origin;
            Vec3 Vert1 = new Vec3(renderData.rawVertexBuffers[vertexStartIndex1], renderData.rawVertexBuffers[vertexStartIndex1 + 1], renderData.rawVertexBuffers[vertexStartIndex1 + 2]) + origin;
            Vec3 Vert2 = new Vec3(renderData.rawVertexBuffers[vertexStartIndex2], renderData.rawVertexBuffers[vertexStartIndex2 + 1], renderData.rawVertexBuffers[vertexStartIndex2 + 2]) + origin;

            return new Triangle(Vert0, Vert1, Vert2);
        }
    }
}
