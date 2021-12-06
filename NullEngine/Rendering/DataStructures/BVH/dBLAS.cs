using ILGPU;
using System.Diagnostics;

namespace NullEngine.Rendering.DataStructures.BVH
{
    public struct dBLAS
    {
        public int meshID;

        public int leftIDOffset;
        public int leftIDCount;
        
        public int rightIDOffset;
        public int rightIDCount;

        public int boxesOffset;
        public int boxesCount;

        public dBLAS(int meshID, int leftIDOffset, int leftIDCount, int rightIDOffset, int rightIDCount, int boxesOffset, int boxesCount)
        {
            this.meshID = meshID;
            this.leftIDOffset = leftIDOffset;
            this.leftIDCount = leftIDCount;
            this.rightIDOffset = rightIDOffset;
            this.rightIDCount = rightIDCount;
            this.boxesOffset = boxesOffset;
            this.boxesCount = boxesCount;
        }

        private void hitBLASNode(dRenderData renderData, dTLAS TLAS, int nodeID, Ray r, float tMin, ref HitRecord rec, ArrayView<int> stack, ref int currentStackPointer)
        {
            if (TLAS.BLASBoxes[nodeID].hit(r, tMin, rec.t))
            {
                int leftID = TLAS.BLASLeftIDs[nodeID];
                int rightID = TLAS.BLASRightIDs[nodeID];

                // node is NOT leaf
                if (leftID >= 0)
                {
                    Triangle left = TLAS.meshes[meshID].GetTriangle(leftID, renderData);
                    Triangle right = TLAS.meshes[meshID].GetTriangle(rightID, renderData);

                    float Hit = left.GetTriangleHit(r, 0.001f, ref rec);
                    float rightHit = right.GetTriangleHit(r, 0.001f, ref rec);

                    int hitTri = leftID;

                    if (rightHit < Hit)
                    {
                        hitTri = rightID;
                        Hit = rightHit;
                    }

                    if (Hit < float.MaxValue)
                    {
                        rec.drawableID = meshID;
                        //currentStackPointer = 0;
                    }
                }
                else // node is leaf
                {
                    leftID = -leftID + boxesOffset;
                    rightID = -rightID + boxesOffset;

                    bool hit_left = TLAS.BLASBoxes[leftID].hit(r, tMin, rec.t);
                    bool hit_right = TLAS.BLASBoxes[rightID].hit(r, tMin, rec.t);

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
            int currentNode = boxesOffset;

            int stackSize = 64;
            ArrayView<int> stack = LocalMemory.Allocate<int>(stackSize); // need this to be as small as possible
            int sp = 0;

            stack[sp++] = boxesOffset;

            while (sp > 0)
            {
                Debug.Assert(sp < stackSize);
                sp--;
                currentNode = stack[sp];

                hitBLASNode(renderData, TLAS, currentNode, r, tMin, ref hit, stack, ref sp);
            }

            return hit.t != 0; //TODO SET THIS TO A BAD HIT
        }
    }
}
