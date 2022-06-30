using ILGPU;
using System.Diagnostics;

namespace NullEngine.Rendering.DataStructures.BVH
{
    public struct dBLAS
    {
        public int meshID;

        public int splitAxisOffset;

        public int leftIDOffset;
        public int leftIDCount;
        
        public int rightIDOffset;
        public int rightIDCount;

        public int boxesOffset;
        public int boxesCount;

        public dBLAS(int meshID, int splitAxisOffset, int leftIDOffset, int leftIDCount, int rightIDOffset, int rightIDCount, int boxesOffset, int boxesCount)
        {
            this.meshID = meshID;
            this.splitAxisOffset = splitAxisOffset;
            this.leftIDOffset = leftIDOffset;
            this.leftIDCount = leftIDCount;
            this.rightIDOffset = rightIDOffset;
            this.rightIDCount = rightIDCount;
            this.boxesOffset = boxesOffset;
            this.boxesCount = boxesCount;
        }

        private void hitBLASNode(dRenderData renderData, dTLAS TLAS, int nodeID, Ray r, float tMin, ref HitRecord rec, ArrayView<int> stack, ref int currentStackPointer)
        {
            nodeID = nodeID + boxesOffset;

            if (TLAS.BLASBoxes[nodeID].hit(r, tMin, rec.t))
            {
                int leftID = TLAS.BLASLeftIDs[nodeID];
                int rightID = TLAS.BLASRightIDs[nodeID];

                // node is NOT leaf
                if (leftID >= 0)
                {
                    Triangle left = TLAS.meshes[meshID].GetTriangle(leftID, renderData);
                    Triangle right = TLAS.meshes[meshID].GetTriangle(rightID, renderData);

                    left.GetTriangleHit(r, leftID, ref rec);
                    right.GetTriangleHit(r, rightID, ref rec);
                }
                else // node is leaf
                {
                    leftID = -leftID;
                    rightID = -rightID;

                    Debug.Assert(leftID + 1 == rightID);

                    bool hit_left = TLAS.BLASBoxes[leftID + boxesOffset].hit(r, tMin, rec.t);
                    bool hit_right = TLAS.BLASBoxes[rightID + boxesOffset].hit(r, tMin, rec.t);

                    //TODO put the most likely node LAST
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

        public bool hit(dRenderData renderData, dTLAS TLAS, Ray r, float tMin, ref HitRecord hit)
        {
            if(false)
            {
                return TLAS.meshes[meshID].GetMeshHit(r, ref hit, renderData) != float.MaxValue;
            }

            int currentNode;

            int stackSize = 1024;
            ArrayView<int> stack = LocalMemory.Allocate<int>(stackSize); // need this to be as small as possible
            int sp = 0;

            stack[sp++] = 0;

            while (sp > 0)
            {
                Debug.Assert(sp < stackSize);
                sp--;
                currentNode = stack[sp];

                hitBLASNode(renderData, TLAS, currentNode, r, tMin, ref hit, stack, ref sp);
            }

            return hit.t != float.MaxValue;
        }
    }
}
