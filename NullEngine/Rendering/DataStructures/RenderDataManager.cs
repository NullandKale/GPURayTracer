using ILGPU;
using ILGPU.Runtime;
using NullEngine.Rendering.Implementation;
using System;
using System.Collections.Generic;
using System.Text;

namespace NullEngine.Rendering.DataStructures
{
    public class RenderDataManager
    {
        private bool isDirty;
        private GPU gpu;
        public List<float> rawTextureData;
        public List<gTexture> textures;

        public List<float> rawVertexBuffers;
        public List<float> rawUVBuffers;
        public List<gVertexBuffer> vertexBuffers;

        public RenderData renderData;

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

                renderData = new RenderData(gpu.device, rawTextureData, textures, rawVertexBuffers, vertexBuffers);
            }

            return renderData.deviceRenderData;
        }

        public int addGbufferForID(Vec3 origin, Vec3 rotation, List<float> verts, List<float> uvs)
        {
            int Voffset = rawVertexBuffers.Count;
            int Uoffset = rawUVBuffers.Count;
            rawVertexBuffers.AddRange(verts);
            rawUVBuffers.AddRange(uvs);

            int id = vertexBuffers.Count;
            vertexBuffers.Add(new gVertexBuffer(origin, rotation, Voffset, verts.Count, Uoffset, uvs.Count));

            isDirty = true;
            return id;
        }

        public int addGTextureForID(int width, int height, List<float> pixels)
        {
            int offset = rawTextureData.Count;
            rawTextureData.AddRange(pixels);

            int id = textures.Count;
            textures.Add(new gTexture(width, height, offset));

            isDirty = true;
            return id;
        }

        private void setupDummyData()
        {
            rawTextureData = new List<float>(new float[3]);
            textures = new List<gTexture>(new gTexture[1]);

            rawVertexBuffers = new List<float>(new float[3]);
            rawUVBuffers = new List<float>(new float[2]);
            vertexBuffers = new List<gVertexBuffer>(new gVertexBuffer[1]);
            isDirty = true;
        }
    }

    public class RenderData
    {
        public MemoryBuffer<float> rawTextureData;
        public MemoryBuffer<gTexture> textures;

        public MemoryBuffer<float> rawVertexBuffers;
        public MemoryBuffer<gVertexBuffer> vertexBuffers;

        public dRenderData deviceRenderData;

        public RenderData(Accelerator device, List<float> RawTextureData, List<gTexture> Textures, List<float> RawVertexData, List<gVertexBuffer> VertexData)
        {
            rawTextureData = device.Allocate<float>(RawTextureData.Count);
            rawTextureData.CopyFrom(RawTextureData.ToArray(), 0, 0, RawTextureData.Count);

            textures = device.Allocate<gTexture>(Textures.Count);
            textures.CopyFrom(Textures.ToArray(), 0, 0, Textures.Count);

            rawVertexBuffers = device.Allocate<float>(RawVertexData.Count);
            rawVertexBuffers.CopyFrom(RawVertexData.ToArray(), 0, 0, RawVertexData.Count);

            vertexBuffers = device.Allocate<gVertexBuffer>(VertexData.Count);
            vertexBuffers.CopyFrom(VertexData.ToArray(), 0, 0, VertexData.Count);

            deviceRenderData = new dRenderData(rawTextureData, textures, rawVertexBuffers, vertexBuffers);
        }

        public void Dispose()
        {
            rawTextureData.Dispose();
            rawVertexBuffers.Dispose();
            textures.Dispose();
            vertexBuffers.Dispose();
        }
    }

    public struct dRenderData
    {
        public ArrayView<float> rawTextureData;
        public ArrayView<gTexture> textures;

        public ArrayView<float> rawVertexData;
        public ArrayView<gVertexBuffer> vertexData;

        public dRenderData(ArrayView<float> rawTextureData, ArrayView<gTexture> textures, ArrayView<float> rawVertexData, ArrayView<gVertexBuffer> vertexData)
        {
            this.rawTextureData = rawTextureData;
            this.textures = textures;
            this.rawVertexData = rawVertexData;
            this.vertexData = vertexData;
        }
    }

    public struct gTexture
    {
        public int width;
        public int height;
        public int offset;

        public gTexture(int width, int height, int offset)
        {
            this.width = width;
            this.height = height;
            this.offset = offset;
        }
    }

    public struct gVertexBuffer
    {
        public Vec3 origin;
        public Vec3 rotation;
        public int vertsOffset;
        public int vertLength;
        public int uvOffset;
        public int uvLength;

        public gVertexBuffer(Vec3 origin, Vec3 rotation, int vertsOffset, int vertLength, int uvOffset, int uvLength)
        {
            this.origin = origin;
            this.rotation = rotation;
            this.vertsOffset = vertsOffset;
            this.vertLength = vertLength;
            this.uvOffset = uvOffset;
            this.uvLength = uvLength;
        }
    }
}
