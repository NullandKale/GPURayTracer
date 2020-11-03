using ILGPU;
using ILGPU.Runtime;
using NullEngine.Rendering.Implementation;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace NullEngine.Rendering.DataStructures
{
    public class RenderDataManager
    {
        public List<float> rawTextureData;
        public List<dTexture> textures;

        public List<int> rawTriangleBuffers;
        public List<float> rawVertexBuffers;
        public List<float> rawUVBuffers;
        public List<dMesh> meshBuffers;

        public RenderData renderData;
        private GPU gpu;
        private bool isDirty;

        public RenderDataManager(GPU gpu)
        {
            this.gpu = gpu;
            setupDummyData();
        }

        public dRenderData getDeviceRenderData()
        {
            if(isDirty)
            {
                if(renderData != null)
                {
                    renderData.Dispose();
                }

                //maybe one day do this async from the render thread
                renderData = new RenderData(gpu.device, this);
            }

            return renderData.deviceRenderData;
        }

        public int addGbufferForID(AABB boundingBox, Vec3 origin, Vec3 rotation, List<int> triangles, List<float> verts, List<float> uvs)
        {
            int Voffset = rawVertexBuffers.Count;
            int Uoffset = rawUVBuffers.Count;
            int Toffset = rawTriangleBuffers.Count;
            int id = meshBuffers.Count;

            rawVertexBuffers.AddRange(verts);
            rawUVBuffers.AddRange(uvs);
            rawTriangleBuffers.AddRange(triangles);
            meshBuffers.Add(new dMesh(boundingBox, origin, rotation, Voffset, Uoffset, Toffset, triangles.Count));

            isDirty = true;
            return id;
        }

        public int addGTextureForID(int width, int height, List<float> pixels)
        {
            int offset = rawTextureData.Count;
            rawTextureData.AddRange(pixels);

            int id = textures.Count;
            textures.Add(new dTexture(width, height, offset));

            isDirty = true;
            return id;
        }

        private void setupDummyData()
        {
            rawTextureData = new List<float>(new float[3]);
            textures = new List<dTexture>(new dTexture[1]);

            rawTriangleBuffers = new List<int>(new int[3]);
            rawVertexBuffers = new List<float>(new float[3]);
            rawUVBuffers = new List<float>(new float[2]);
            meshBuffers = new List<dMesh>(new dMesh[1]);
            isDirty = true;
        }
    }

    public class RenderData
    {
        public MemoryBuffer<float> rawTextureData;
        public MemoryBuffer<dTexture> textures;

        public MemoryBuffer<int> rawTriangleBuffers;
        public MemoryBuffer<float> rawVertexBuffers;
        public MemoryBuffer<float> rawUVBuffers;
        public MemoryBuffer<dMesh> meshBuffers;

        public dRenderData deviceRenderData;

        public RenderData(Accelerator device, RenderDataManager dataManager)
        {
            rawTextureData = device.Allocate<float>(dataManager.rawTextureData.Count);
            rawTextureData.CopyFrom(dataManager.rawTextureData.ToArray(), 0, 0, dataManager.rawTextureData.Count);

            textures = device.Allocate<dTexture>(dataManager.textures.Count);
            textures.CopyFrom(dataManager.textures.ToArray(), 0, 0, dataManager.textures.Count);

            rawTriangleBuffers = device.Allocate<int>(dataManager.rawTriangleBuffers.Count);
            rawTriangleBuffers.CopyFrom(dataManager.rawTriangleBuffers.ToArray(), 0, 0, dataManager.rawTriangleBuffers.Count);

            rawUVBuffers = device.Allocate<float>(dataManager.rawUVBuffers.Count);
            rawUVBuffers.CopyFrom(dataManager.rawUVBuffers.ToArray(), 0, 0, dataManager.rawUVBuffers.Count);

            rawVertexBuffers = device.Allocate<float>(dataManager.rawVertexBuffers.Count);
            rawVertexBuffers.CopyFrom(dataManager.rawVertexBuffers.ToArray(), 0, 0, dataManager.rawVertexBuffers.Count);

            meshBuffers = device.Allocate<dMesh>(dataManager.meshBuffers.Count);
            meshBuffers.CopyFrom(dataManager.meshBuffers.ToArray(), 0, 0, dataManager.meshBuffers.Count);

            deviceRenderData = new dRenderData(this);
        }

        public void Dispose()
        {
            rawTextureData.Dispose();
            rawVertexBuffers.Dispose();
            textures.Dispose();
            meshBuffers.Dispose();
        }
    }

    public struct dRenderData
    {
        public ArrayView<float> rawTextureData;
        public ArrayView<dTexture> textures;

        public ArrayView<int> rawTriangleBuffers;
        public ArrayView<float> rawVertexBuffers;
        public ArrayView<float> rawUVBuffers;
        public ArrayView<dMesh> meshBuffers;

        public dRenderData(RenderData renderData)
        {
            rawTextureData = renderData.rawTextureData;
            textures = renderData.textures;
            rawTriangleBuffers = renderData.rawTriangleBuffers;
            rawVertexBuffers = renderData.rawVertexBuffers;
            rawUVBuffers = renderData.rawUVBuffers;
            meshBuffers = renderData.meshBuffers;
        }
    }

    public struct dTexture
    {
        public int width;
        public int height;
        public int offset;

        public dTexture(int width, int height, int offset)
        {
            this.width = width;
            this.height = height;
            this.offset = offset;
        }
    }

    public struct dMesh
    {
        public AABB boundingBox;
        public Vec3 origin;
        public Vec3 rotation;

        public int vertsOffset;
        public int uvOffset;
        public int triangleOffset;
        public int triangleLength;

        public dMesh(AABB boundingBox, Vec3 origin, Vec3 rotation, int vertsOffset, int uvOffset, int triangleOffset, int triangleLength)
        {
            this.boundingBox = boundingBox;
            this.origin = origin;
            this.rotation = rotation;
            this.vertsOffset = vertsOffset;
            this.uvOffset = uvOffset;
            this.triangleOffset = triangleOffset;
            this.triangleLength = triangleLength;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Triangle GetTriangle(int index, dRenderData renderData)
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
