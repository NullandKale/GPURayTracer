using ILGPU;
using ILGPU.Runtime;
using NullEngine.Rendering.Implementation;
using NullEngine.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace NullEngine.Rendering.DataStructures.BVH
{
    public class hTLAS
    {
        GPU gpu;

        List<dMesh> hMeshes;
        MemoryBuffer1D<dMesh, Stride1D.Dense> dMeshes;

        hTLAS_node root;

        List<AABB> hBoxes;
        List<int> hRightIDs;
        List<int> hLeftIDs;

        dTLAS dTLAS;

        MemoryBuffer1D<AABB, Stride1D.Dense> dBoxes;
        MemoryBuffer1D<int, Stride1D.Dense> dLeftIDs;
        MemoryBuffer1D<int, Stride1D.Dense> dRightIDs;

        public hTLAS(GPU gpu, List<dMesh> hMeshes, MemoryBuffer1D<dMesh, Stride1D.Dense> dMeshes)
        {
            this.gpu = gpu;
            
            this.hMeshes = hMeshes;
            hRightIDs = new List<int>();
            hLeftIDs = new List<int>();
            hBoxes = new List<AABB>();

            this.dMeshes = dMeshes;
            Stopwatch timer = Stopwatch.StartNew();
            buildHTLAS();
            buildDTLAS();
            timer.Stop();

            Trace.WriteLine("TLAS build time " + timer.ElapsedMilliseconds);
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

            dBoxes = gpu.device.Allocate1D(hBoxes.ToArray());
            dLeftIDs = gpu.device.Allocate1D(hLeftIDs.ToArray());
            dRightIDs = gpu.device.Allocate1D(hRightIDs.ToArray());

            dTLAS = new dTLAS(dMeshes, dLeftIDs, dRightIDs, dBoxes);
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
    }

    public struct dTLAS
    {
        public ArrayView1D<dMesh, Stride1D.Dense> meshes;
        public ArrayView1D<int, Stride1D.Dense> leftIDs;
        public ArrayView1D<int, Stride1D.Dense> rightIDs;
        public ArrayView1D<AABB, Stride1D.Dense> boxes;

        public dTLAS(ArrayView1D<dMesh, Stride1D.Dense> meshes, ArrayView1D<int, Stride1D.Dense> leftIDs, ArrayView1D<int, Stride1D.Dense> rightIDs, ArrayView1D<AABB, Stride1D.Dense> boxes)
        {
            this.meshes = meshes;
            this.leftIDs = leftIDs;
            this.rightIDs = rightIDs;
            this.boxes = boxes;
        }
    }

    class hTLAS_node
    {
        public hTLAS_node left;
        public hTLAS_node right;

        public int leftID = -1;
        public int rightID = -1;

        public AABB box;

        public hTLAS_node(List<dMesh> meshes, int n)
        {
            int axis = SharedRNG.randi(0, 3);

            if (axis == 0)
            {
                meshes.Sort(0, n, xCompare);
            }
            else if (axis == 1)
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
