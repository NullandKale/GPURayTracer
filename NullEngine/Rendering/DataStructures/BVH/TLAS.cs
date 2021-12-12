using ILGPU;
using ILGPU.Runtime;
using NullEngine.Rendering.Implementation;
using NullEngine.Utils;
using ObjLoader.Loader.Data.Elements;
using ObjLoader.Loader.Loaders;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace NullEngine.Rendering.DataStructures.BVH
{
    public class hTLAS
    {
        public RenderDataManager renderDataManager;

        bool isDirty;
        dTLAS DTLAS;

        hTLAS_node root;
        List<hBLAS> hBLASs;

        GPU gpu;

        List<dMesh> hMeshes;
        List<int> hSplitAxis;

        List<AABB> hBoxes;
        List<int> hRightIDs;
        List<int> hLeftIDs;

        List<dBLAS> hdBLASs;
        List<AABB> hBLASBoxes;
        List<int> hBLASRightIDs;
        List<int> hBLASLeftIDs;
        
        internal MemoryBuffer1D<dMesh, Stride1D.Dense> dMeshes;
        internal MemoryBuffer1D<int, Stride1D.Dense> dSplitAxis;

        internal MemoryBuffer1D<AABB, Stride1D.Dense> dBoxes;
        internal MemoryBuffer1D<int, Stride1D.Dense> dLeftIDs;
        internal MemoryBuffer1D<int, Stride1D.Dense> dRightIDs;

        internal MemoryBuffer1D<dBLAS, Stride1D.Dense> dBLAS;
        internal MemoryBuffer1D<AABB, Stride1D.Dense> dBLASBoxes;
        internal MemoryBuffer1D<int, Stride1D.Dense> dBLASLeftIDs;
        internal MemoryBuffer1D<int, Stride1D.Dense> dBLASRightIDs;



        public hTLAS(GPU gpu)
        {
            this.gpu = gpu;
            this.renderDataManager = new RenderDataManager(gpu);

            hMeshes = new List<dMesh>();
            hSplitAxis = new List<int>();

            hBoxes = new List<AABB>();
            hRightIDs = new List<int>();
            hLeftIDs = new List<int>();

            hBLASs = new List<hBLAS>();
            hdBLASs = new List<dBLAS>();

            hBLASBoxes = new List<AABB>();
            hBLASLeftIDs = new List<int>();
            hBLASRightIDs = new List<int>();

            isDirty = true;
        }

        public void LoadMeshFromFile(Vec3 pos, Vec3 rot, string filename)
        {
            string[] lines = File.ReadAllLines(filename + (filename.EndsWith(".obj") ? "" : ".obj"));

            List<float> verticies = new List<float>();
            List<int> triangles = new List<int>();
            List<int> mats = new List<int>();

            int mat = 0;

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                string[] split = line.Split(" ");

                if (line.Length > 0 && line[0] != '#' && split.Length >= 2)
                {
                    switch (split[0])
                    {
                        case "v":
                            {
                                if (double.TryParse(split[1], out double v0) && double.TryParse(split[2], out double v1) && double.TryParse(split[3], out double v2))
                                {
                                    verticies.Add((float)v0);
                                    verticies.Add((float)-v1);
                                    verticies.Add((float)v2);
                                }
                                break;
                            }
                        case "f":
                            {
                                List<int> indexes = new List<int>();
                                for (int j = 1; j < split.Length; j++)
                                {
                                    string[] indicies = split[j].Split("/");

                                    if (indicies.Length >= 1)
                                    {
                                        if (int.TryParse(indicies[0], out int i0))
                                        {
                                            indexes.Add(i0 < 0 ? i0 + verticies.Count : i0 - 1);
                                        }
                                    }
                                }

                                for (int j = 1; j < indexes.Count - 1; ++j)
                                {
                                    triangles.Add(indexes[0]);
                                    triangles.Add(indexes[j]);
                                    triangles.Add(indexes[j + 1]);
                                    mats.Add(mat);
                                }

                                break;
                            }
                        case "usemtl":
                            {
                                // material handling happens here!
                                break;
                            }
                    }

                }
            }

            AABB aabb = AABB.CreateFromVerticies(verticies, pos);
            dMesh mesh = renderDataManager.addGbufferForID(hMeshes.Count, aabb, pos, rot, triangles, verticies, new List<float>());

            BuildAndAddBLAS(mesh);

            hMeshes.Add(mesh);

            isDirty = true;
        }

        public void AddObj(LoadResult loadedObj, Vec3 position, Vec3 rotation)
        {
            for (int i = 0; i < loadedObj.Groups.Count; i++)
            {
                ObjLoader.Loader.Data.Elements.Group group = loadedObj.Groups[i];

                List<float> verts = new List<float>();
                List<int> triangles = new List<int>();
                List<float> uvs = new List<float>();

                for (int j = 0; j < group.Faces.Count; j++)
                {
                    Face f = group.Faces[j];

                    for (int k = 0; k < 3; k++)
                    {
                        if (f[k].VertexIndex < loadedObj.Vertices.Count
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
                dMesh mesh = renderDataManager.addGbufferForID(hMeshes.Count, aabb, position, rotation, triangles, verts, uvs);

                BuildAndAddBLAS(mesh);

                hMeshes.Add(mesh);

                isDirty = true;
            }
        }

        public dBLAS addGBlas(int meshID, List<int> hSplitAxis, List<AABB> hBoxes, List<int> hLeftIDs, List<int> hRightIDs)
        {
            int splitAxisOffset = this.hSplitAxis.Count;
            int boxOffset = this.hBLASBoxes.Count;
            int leftIDOffset = this.hBLASLeftIDs.Count;
            int rightIDOffset = this.hBLASRightIDs.Count;

            this.hSplitAxis.AddRange(hSplitAxis);
            hBLASBoxes.AddRange(hBoxes);
            hBLASLeftIDs.AddRange(hLeftIDs);
            hBLASRightIDs.AddRange(hRightIDs);

            Debug.Assert(boxOffset == leftIDOffset && leftIDOffset == rightIDOffset && boxOffset == splitAxisOffset);

            return new dBLAS(meshID, splitAxisOffset, leftIDOffset, hLeftIDs.Count, rightIDOffset, hRightIDs.Count, boxOffset, hBoxes.Count);
        }

        public void rebuildTLAS()
        {
            buildHTLAS();
            buildDTLAS();
        }

        private void buildHTLAS()
        {
            List<dMesh> working = new List<dMesh>(hMeshes);
            working.Sort((a, b) => Vec3.CompareTo(a.origin, b.origin));
            root = new hTLAS_node(working, working.Count);
        }

        private void buildDTLAS()
        {
            hBoxes.Add(root.box);
            RecursiveAddNodeToDTLAS(root);
        }

        public dTLAS GetDTLAS()
        {
            if(isDirty)
            {
                rebuildTLAS();

                dMeshes = gpu.device.Allocate1D(hMeshes.ToArray());
                
                dSplitAxis = gpu.device.Allocate1D(hSplitAxis.ToArray()); 
                dBoxes = gpu.device.Allocate1D(hBoxes.ToArray());
                dLeftIDs = gpu.device.Allocate1D(hLeftIDs.ToArray());
                dRightIDs = gpu.device.Allocate1D(hRightIDs.ToArray());

                dBLAS = gpu.device.Allocate1D(hdBLASs.ToArray());
                dBLASBoxes = gpu.device.Allocate1D(hBLASBoxes.ToArray());
                dBLASLeftIDs = gpu.device.Allocate1D(hBLASLeftIDs.ToArray());
                dBLASRightIDs = gpu.device.Allocate1D(hBLASRightIDs.ToArray());

                DTLAS = new dTLAS(this);
                isDirty = false;
            }

            return DTLAS;
        }

        private void RecursiveAddNodeToDTLAS(hTLAS_node node)
        {
            //node is leaf
            if(node.leftID != -1)
            {
                hLeftIDs.Add(node.leftID);
                hRightIDs.Add(node.rightID);
            }
            else // node is not leaf
            {
                int left = hBoxes.Count;
                int right = left + 1;

                hBoxes.Add(node.left.box);
                hBoxes.Add(node.right.box);

                hLeftIDs.Add(-left);
                hRightIDs.Add(-right);

                RecursiveAddNodeToDTLAS(node.left);
                RecursiveAddNodeToDTLAS(node.right);
            }

        }

        private void BuildAndAddBLAS(dMesh mesh)
        {
            hBLAS BLAS = new hBLAS(gpu, mesh, renderDataManager);

            hBLASs.Add(BLAS);
            hdBLASs.Add(addGBlas(BLAS.mesh.meshID, BLAS.hSplitAxis, BLAS.hBoxes, BLAS.hLeftIDs, BLAS.hRightIDs));
        }
    }

    class hTLAS_node
    {
        public hTLAS_node left;
        public hTLAS_node right;

        public int leftID = -1;
        public int rightID = -1;

        public int splitAxis = -1;

        public AABB box;

        public hTLAS_node(List<dMesh> meshes, int n)
        {
            splitAxis = SharedRNG.randi(0, 3);

            if (splitAxis == 0)
            {
                meshes.Sort(0, n, xCompare);
            }
            else if (splitAxis == 1)
            {
                meshes.Sort(0, n, yCompare);
            }
            else
            {
                meshes.Sort(0, n, zCompare);
            }

            if (n == 1)
            {
                leftID = meshes[0].meshID;
                rightID = meshes[0].meshID;
                box = meshes[0].boundingBox;
            }
            else if (n == 2)
            {
                leftID = meshes[0].meshID;
                rightID = meshes[1].meshID;
                box = AABB.surrounding_box(meshes[0].boundingBox, meshes[1].boundingBox);
            }
            else
            {
                left = new hTLAS_node(meshes.GetRange(0, n / 2), n / 2);
                right = new hTLAS_node(meshes.GetRange(n / 2, n - n / 2), n - n / 2);
                box = AABB.surrounding_box(left.box, right.box);
            }
        }

        private Comparer<dMesh> xCompare = Comparer<dMesh>.Create(boxXCompare);
        private Comparer<dMesh> yCompare = Comparer<dMesh>.Create(boxYCompare);
        private Comparer<dMesh> zCompare = Comparer<dMesh>.Create(boxZCompare);

        private static int boxXCompare(dMesh a, dMesh b)
        {
            if (a.boundingBox.min.x - b.boundingBox.min.x < 0.0)
            {
                return -1;
            }
            else
            {
                return 1;
            }
        }

        private static int boxYCompare(dMesh a, dMesh b)
        {
            if (a.boundingBox.min.y - b.boundingBox.min.y < 0.0)
            {
                return -1;
            }
            else
            {
                return 1;
            }
        }

        private static int boxZCompare(dMesh a, dMesh b)
        {
            if (a.boundingBox.min.z - b.boundingBox.min.z < 0.0)
            {
                return -1;
            }
            else
            {
                return 1;
            }
        }
    }

}
