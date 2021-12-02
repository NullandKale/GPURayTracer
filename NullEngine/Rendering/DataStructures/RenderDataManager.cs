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
        public List<dMesh> meshBuffers;
        public List<hBLAS> hBLASs;
        public List<dBLAS> dBLASs;

        public RenderData renderData;
        private GPU gpu;
        private bool isDirty;

        public hTLAS TopLevelAccelerationStructure;

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

                TopLevelAccelerationStructure = new hTLAS(gpu, meshBuffers, renderData.meshBuffers);

                Stopwatch timer = Stopwatch.StartNew();

                for(int i = 1; i < meshBuffers.Count; i++)
                {
                    hBLAS BLAS = new hBLAS(gpu, meshBuffers[i], this);
                    hBLASs.Add(BLAS);
                    dBLASs.Add(BLAS.GetDBLAS());
                }

                timer.Stop();
                Trace.WriteLine("Total BLAS time: " + timer.ElapsedMilliseconds);

                isDirty = false;
            }

            return renderData.deviceRenderData;
        }

        public void AddObj(LoadResult loadedObj, Vec3 position, Vec3 rotation)
        {       
            for(int i = 0; i < loadedObj.Groups.Count; i++)
            {
                ObjLoader.Loader.Data.Elements.Group group = loadedObj.Groups[i];
                List<float> verts = new List<float>();
                List<int> triangles = new List<int>();
                List<float> uvs = new List<float>();

                for(int j = 0; j < group.Faces.Count; j++)
                {
                    Face f = group.Faces[j];
                   
                    for(int k = 0; k < 3; k++)
                    {
                        if(f[k].VertexIndex < loadedObj.Vertices.Count
                        && f[k].TextureIndex < loadedObj.Textures.Count)
                        {
                            triangles.Add(f[k].VertexIndex);

                            verts.Add(loadedObj.Vertices[f[k].VertexIndex].X);
                            verts.Add(loadedObj.Vertices[f[k].VertexIndex].Y);
                            verts.Add(loadedObj.Vertices[f[k].VertexIndex].Z);

                            uvs.Add(loadedObj.Textures[f[k].TextureIndex].X);
                            uvs.Add(loadedObj.Textures[f[k].TextureIndex].Y);
                        }
                        else
                        {
                            Trace.WriteLine("Failed to load triangle " + j + " " + k + " from group " + i);
                        }
                    }
                }

                AABB aabb = AABB.CreateFromVerticies(verts, position);
                addGbufferForID(aabb, position, rotation, triangles, verts, uvs);
            }
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
            
            dMesh mesh = new dMesh(id, boundingBox, origin, rotation, Voffset, Uoffset, Toffset, triangles.Count / 3);
            meshBuffers.Add(mesh);

            //hBLAS BLAS = new hBLAS(gpu, mesh, this);
            //hBLASs.Add(BLAS);
            //dBLASs.Add(BLAS.GetDBLAS());

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

            hBLASs = new List<hBLAS>();
            dBLASs = new List<dBLAS>(new dBLAS[1]);

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
        public MemoryBuffer1D<dMesh, Stride1D.Dense> meshBuffers;

        public dRenderData deviceRenderData;

        public RenderData(Accelerator device, RenderDataManager dataManager)
        {
            rawTextureData = device.Allocate1D<float>(dataManager.rawTextureData.Count);
            rawTextureData.CopyFromCPU(dataManager.rawTextureData.ToArray());

            textures = device.Allocate1D<dTexture>(dataManager.textures.Count);
            textures.CopyFromCPU(dataManager.textures.ToArray());

            rawTriangleBuffers = device.Allocate1D<int>(dataManager.rawTriangleBuffers.Count);
            rawTriangleBuffers.CopyFromCPU(dataManager.rawTriangleBuffers.ToArray());

            rawUVBuffers = device.Allocate1D<float>(dataManager.rawUVBuffers.Count);
            rawUVBuffers.CopyFromCPU(dataManager.rawUVBuffers.ToArray());

            rawVertexBuffers = device.Allocate1D<float>(dataManager.rawVertexBuffers.Count);
            rawVertexBuffers.CopyFromCPU(dataManager.rawVertexBuffers.ToArray());

            meshBuffers = device.Allocate1D<dMesh>(dataManager.meshBuffers.Count);
            meshBuffers.CopyFromCPU(dataManager.meshBuffers.ToArray());

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
        public ArrayView1D<float, Stride1D.Dense> rawTextureData;
        public ArrayView1D<dTexture, Stride1D.Dense> textures;

        public ArrayView1D<int, Stride1D.Dense> rawTriangleBuffers;
        public ArrayView1D<float, Stride1D.Dense> rawVertexBuffers;
        public ArrayView1D<float, Stride1D.Dense> rawUVBuffers;
        public ArrayView1D<dMesh, Stride1D.Dense> meshBuffers;

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
}
