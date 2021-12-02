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
    public class hBLAS
    {
        GPU gpu;

        public dMesh mesh;
        public RenderDataManager renderData;

        hBLAS_node root;

        List<AABB> hBoxes;
        List<int> hRightIDs;
        List<int> hLeftIDs;

        dBLAS DBLAS;

        MemoryBuffer1D<AABB, Stride1D.Dense> dBoxes;
        MemoryBuffer1D<int, Stride1D.Dense> dLeftIDs;
        MemoryBuffer1D<int, Stride1D.Dense> dRightIDs;

        public hBLAS(GPU gpu, dMesh mesh, RenderDataManager renderData)
        {
            this.gpu = gpu;
            this.mesh = mesh;
            this.renderData = renderData;

            hRightIDs = new List<int>();
            hLeftIDs = new List<int>();
            hBoxes = new List<AABB>();

            buildHBLAS();
            buildDBLAS();
        }

        public dBLAS GetDBLAS()
        {
            return DBLAS;
        }

        private void buildHBLAS()
        {
            List<Triangle> triangles = mesh.GetTriangles(renderData);
            // there may be a better way to build this
            List<(Triangle t, Vec3 center, int id)> TandID = new List<(Triangle, Vec3, int)>();
            for (int i = 0; i < triangles.Count; i++)
            {
                TandID.Add((triangles[i], triangles[i].getCenter(), i));
            }

            TandID.Sort((a, b) => Vec3.CompareTo(a.center, b.center));
            root = new hBLAS_node(TandID.ToArray(), TandID.Count);
        }

        private void buildDBLAS()
        {
            hBoxes.Add(root.box);
            RecursiveAddNodeToDBLAS(root);

            dBoxes = gpu.device.Allocate1D(hBoxes.ToArray());
            dLeftIDs = gpu.device.Allocate1D(hLeftIDs.ToArray());
            dRightIDs = gpu.device.Allocate1D(hRightIDs.ToArray());

            DBLAS = new dBLAS(mesh.meshID, dLeftIDs, dRightIDs, dBoxes); 
        }

        private void RecursiveAddNodeToDBLAS(hBLAS_node node)
        {
            //node is leaf
            if (node.leftID != -1)
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

                RecursiveAddNodeToDBLAS(node.left);
                RecursiveAddNodeToDBLAS(node.right);
            }

        }

    }

    public struct dBLAS
    {
        public int meshID;
        public ArrayView1D<int, Stride1D.Dense> leftIDs;
        public ArrayView1D<int, Stride1D.Dense> rightIDs;
        public ArrayView1D<AABB, Stride1D.Dense> boxes;

        public dBLAS(int meshID, ArrayView1D<int, Stride1D.Dense> leftIDs, ArrayView1D<int, Stride1D.Dense> rightIDs, ArrayView1D<AABB, Stride1D.Dense> boxes)
        {
            this.meshID = meshID;
            this.leftIDs = leftIDs;
            this.rightIDs = rightIDs;
            this.boxes = boxes;
        }
    }

    class hBLAS_node
    {
        public hBLAS_node left;
        public hBLAS_node right;

        public int leftID = -1;
        public int rightID = -1;

        public AABB box;

        public hBLAS_node(Span<(Triangle t, Vec3 center, int id)> triangles, int n)
        {
            int axis = SharedRNG.randi(0, 3);

            if (axis == 0)
            {
                triangles.Sort(xCompare);
            }
            else if (axis == 1)
            {
                triangles.Sort(yCompare);
            }
            else
            {
                triangles.Sort(zCompare);
            }

            if (n == 1)
            {
                leftID = triangles[0].id;
                rightID = triangles[0].id;
                box = AABB.CreateFromTriangle(triangles[0].t);
            }
            else if (n == 2)
            {
                leftID = triangles[0].id;
                rightID = triangles[1].id;
                box = AABB.surrounding_box(AABB.CreateFromTriangle(triangles[0].t), AABB.CreateFromTriangle(triangles[1].t));
            }
            else
            {
                left = new hBLAS_node(triangles.Slice(0, n / 2), n / 2);
                right = new hBLAS_node(triangles.Slice(n / 2, n - n / 2), n - n / 2);
                box = AABB.surrounding_box(left.box, right.box);
            }
        }

        private Comparer<(Triangle t, Vec3 center, int id)> xCompare = Comparer<(Triangle t, Vec3 center, int id)>.Create(boxXCompare);
        private Comparer<(Triangle t, Vec3 center, int id)> yCompare = Comparer<(Triangle t, Vec3 center, int id)>.Create(boxYCompare);
        private Comparer<(Triangle t, Vec3 center, int id)> zCompare = Comparer<(Triangle t, Vec3 center, int id)>.Create(boxZCompare);

        private static int boxXCompare((Triangle t, Vec3 center, int id) a, (Triangle t, Vec3 center, int id) b)
        {
            if (a.center.x - b.center.x < 0.0)
            {
                return -1;
            }
            else
            {
                return 1;
            }
        }

        private static int boxYCompare((Triangle t, Vec3 center, int id) a, (Triangle t, Vec3 center, int id) b)
        {
            if (a.center.x - b.center.x < 0.0)
            {
                return -1;
            }
            else
            {
                return 1;
            }
        }

        private static int boxZCompare((Triangle t, Vec3 center, int id) a, (Triangle t, Vec3 center, int id) b)
        {
            if (a.center.x - b.center.x < 0.0)
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
