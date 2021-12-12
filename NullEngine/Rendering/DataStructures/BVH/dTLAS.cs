using ILGPU;
using ILGPU.Runtime;
using System.Diagnostics;

namespace NullEngine.Rendering.DataStructures.BVH
{
    public struct dTLAS
    {
        public ArrayView1D<dMesh, Stride1D.Dense> meshes;

        public ArrayView1D<int, Stride1D.Dense> splitAxis;

        public ArrayView1D<int, Stride1D.Dense> leftIDs;
        public ArrayView1D<int, Stride1D.Dense> rightIDs;
        public ArrayView1D<AABB, Stride1D.Dense> boxes;

        public ArrayView1D<dBLAS , Stride1D.Dense> BLASs;
        public ArrayView1D<AABB, Stride1D.Dense> BLASBoxes;
        public ArrayView1D<int, Stride1D.Dense> BLASLeftIDs;
        public ArrayView1D<int, Stride1D.Dense> BLASRightIDs;

        public dTLAS(hTLAS TLAS)
        {
            meshes = TLAS.dMeshes;
            splitAxis = TLAS.dSplitAxis;
            leftIDs = TLAS.dLeftIDs;
            rightIDs = TLAS.dRightIDs;
            boxes = TLAS.dBoxes;
            
            BLASs = TLAS.dBLAS;
            BLASBoxes = TLAS.dBLASBoxes;
            BLASLeftIDs =TLAS.dBLASLeftIDs;
            BLASRightIDs = TLAS.dBLASRightIDs;
        }

        private void hitTLASNode(dRenderData renderData, int nodeID, Ray r, float tMin, ref HitRecord rec, ArrayView<int> stack, ref int currentStackPointer)
        {
            if (boxes[nodeID].hit(r, tMin, rec.t))
            {
                int leftID = leftIDs[nodeID];
                int rightID = rightIDs[nodeID];

                // node is leaf
                if (leftID >= 0)
                {
                    BLASs[leftID].hit(renderData, this, r, tMin, ref rec);
                    BLASs[rightID].hit(renderData, this, r, tMin, ref rec);
                }
                else // node is NOT leaf
                {
                    leftID = -leftID;
                    rightID = -rightID;

                    bool hit_left = boxes[leftID].hit(r, tMin, rec.t);
                    bool hit_right = boxes[rightID].hit(r, tMin, rec.t);

                    //TODO put the closest node first
                    if (hit_left)
                    {
                        stack[currentStackPointer] = leftID;
                        currentStackPointer++;
                    }

                    if (hit_right)
                    {
                        stack[currentStackPointer] = rightID;
                        currentStackPointer++;
                    }
                }
            }
        }

        public bool hit(dRenderData renderData, Ray r, float tMin, ref HitRecord hit)
        {
            int currentNode = 0;

            int stackSize = 16;
            ArrayView<int> stack = LocalMemory.Allocate<int>(stackSize); // need this to be as small as possible
            int sp = 0;

            stack[sp++] = 0;

            while(sp > 0)
            {
                Debug.Assert(sp < stackSize);
                
                sp--;
                currentNode = stack[sp];

                hitTLASNode(renderData, currentNode, r, tMin, ref hit, stack, ref sp);

            }

            return hit.t != float.MaxValue;
        }
    }

}
