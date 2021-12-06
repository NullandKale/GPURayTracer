using ILGPU;
using ILGPU.Runtime;
using NullEngine.Rendering.DataStructures.BVH;
using NullEngine.Rendering.Implementation;
using ObjLoader.Loader.Data.Elements;
using ObjLoader.Loader.Loaders;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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

                isDirty = false;
            }

            return renderData.deviceRenderData;
        }

        public dMesh addGbufferForID(int id, AABB boundingBox, Vec3 origin, Vec3 rotation, List<int> triangles, List<float> verts, List<float> uvs)
        {
            int Voffset = rawVertexBuffers.Count;
            int Uoffset = rawUVBuffers.Count;
            int Toffset = rawTriangleBuffers.Count;

            rawVertexBuffers.AddRange(verts);
            rawUVBuffers.AddRange(uvs);
            rawTriangleBuffers.AddRange(triangles);
            
            dMesh mesh = new dMesh(id, boundingBox, origin, rotation, Voffset, Uoffset, Toffset, triangles.Count / 3);

            isDirty = true;
            return mesh;
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
            rawTextureData = new List<float>();
            textures = new List<dTexture>();

            rawTriangleBuffers = new List<int>();
            rawVertexBuffers = new List<float>();
            rawUVBuffers = new List<float>();

            isDirty = true;
        }
    }

    public class RenderData
    {
        public MemoryBuffer1D<float, Stride1D.Dense> rawTextureData;
        public MemoryBuffer1D<dTexture, Stride1D.Dense> textures;

        public MemoryBuffer1D<int, Stride1D.Dense> rawTriangleBuffers;
        public MemoryBuffer1D<float, Stride1D.Dense> rawVertexBuffers;
        public MemoryBuffer1D<float, Stride1D.Dense> rawUVBuffers;

        public dRenderData deviceRenderData;

        public RenderData(Accelerator device, RenderDataManager dataManager)
        {
            if (dataManager.rawTextureData.Count > 0)
            {
                rawTextureData = device.Allocate1D(dataManager.rawTextureData.ToArray());
                textures = device.Allocate1D(dataManager.textures.ToArray());
            }
            else
            {
                rawTextureData = device.Allocate1D<float>(1);
                textures = device.Allocate1D<dTexture>(1);
            }

            if(dataManager.rawTriangleBuffers.Count > 0)
            {
                rawTriangleBuffers = device.Allocate1D(dataManager.rawTriangleBuffers.ToArray());
                rawVertexBuffers = device.Allocate1D(dataManager.rawVertexBuffers.ToArray());
            }
            else
            {
                rawTriangleBuffers = device.Allocate1D<int>(1);
                rawVertexBuffers = device.Allocate1D<float>(1);
            }

            if(dataManager.rawUVBuffers.Count > 0)
            {
                rawUVBuffers = device.Allocate1D(dataManager.rawUVBuffers.ToArray());
            }
            else
            {
                rawUVBuffers = device.Allocate1D<float>(1);
            }

            deviceRenderData = new dRenderData(this);
        }

        public void Dispose()
        {
            rawTextureData.Dispose();
            rawVertexBuffers.Dispose();
            textures.Dispose();
        }
    }

    public struct dRenderData
    {
        public ArrayView1D<float, Stride1D.Dense> rawTextureData;
        public ArrayView1D<dTexture, Stride1D.Dense> textures;

        public ArrayView1D<int, Stride1D.Dense> rawTriangleBuffers;
        public ArrayView1D<float, Stride1D.Dense> rawVertexBuffers;
        public ArrayView1D<float, Stride1D.Dense> rawUVBuffers;

        public dRenderData(RenderData renderData)
        {
            rawTextureData = renderData.rawTextureData;
            textures = renderData.textures;
            rawTriangleBuffers = renderData.rawTriangleBuffers;
            rawVertexBuffers = renderData.rawVertexBuffers;
            rawUVBuffers = renderData.rawUVBuffers;
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
}
