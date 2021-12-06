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

        internal List<AABB> hBoxes;
        internal List<int> hRightIDs;
        internal List<int> hLeftIDs;

        public hBLAS(GPU gpu, dMesh mesh, RenderDataManager renderData)
        {
            this.gpu = gpu;
            this.mesh = mesh;
            this.renderData = renderData;

            hRightIDs = new List<int>((mesh.triangleLength * 2) + 1);
            hLeftIDs = new List<int>((mesh.triangleLength * 2) + 1);
            hBoxes = new List<AABB>((mesh.triangleLength * 2) + 1);

            buildHBLAS();
            buildDBLAS();
        }

        private void buildHBLAS()
        {
            List<TriangleRecord> TandID = new List<TriangleRecord>(mesh.triangleLength);
            for (int i = 0; i < mesh.triangleLength; i++)
            {
                Triangle t = mesh.GetTriangle(i, renderData);
                TandID.Add(new TriangleRecord{ t=t, center=t.getCenter(), id=i});
            }

            TandID.Sort((a, b) => Vec3.CompareTo(a.center, b.center));
            root = new hBLAS_node(TandID.ToArray(), TandID.Count);
        }

        private void buildDBLAS()
        {
            hBoxes.Add(root.box);
            RecursiveAddNodeToDBLAS(root);
            root = null;
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

        internal static readonly Comparer<TriangleRecord> xCompare = Comparer<TriangleRecord>.Create(boxXCompare);
        internal static readonly Comparer<TriangleRecord> yCompare = Comparer<TriangleRecord>.Create(boxYCompare);
        internal static readonly Comparer<TriangleRecord> zCompare = Comparer<TriangleRecord>.Create(boxZCompare);

        private static int boxXCompare(TriangleRecord a, TriangleRecord b)
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

        private static int boxYCompare(TriangleRecord a, TriangleRecord b)
        {
            if (a.center.y - b.center.y < 0.0)
            {
                return -1;
            }
            else
            {
                return 1;
            }
        }

        private static int boxZCompare(TriangleRecord a, TriangleRecord b)
        {
            if (a.center.z - b.center.z < 0.0)
            {
                return -1;
            }
            else
            {
                return 1;
            }
        }
    }

    internal class hBLAS_node
    {
        public hBLAS_node left;
        public hBLAS_node right;

        public int leftID = -1;
        public int rightID = -1;

        public AABB box;

        public hBLAS_node(Span<TriangleRecord> triangles, int n)
        {
            int axis = SharedRNG.randi(0, 3);

            if (axis == 0)
            {
                triangles.Sort(hBLAS.xCompare);
            }
            else if (axis == 1)
            {
                triangles.Sort(hBLAS.yCompare);
            }
            else
            {
                triangles.Sort(hBLAS.zCompare);
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
    }

    internal struct TriangleRecord
    {
        public Triangle t;
        public Vec3 center;
        public int id;
    }
}
