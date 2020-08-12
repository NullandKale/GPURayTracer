using GPURayTracer.Rendering.Primitives;
using ILGPU;
using ILGPU.Algorithms;
using ILGPU.Runtime;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace GPURayTracer.Rendering.GPUStructs
{
    public struct dVoxelChunk
    {
        public AABB aabb;
        public Vec3 position;
        public int width;
        public int depth;
        public int height;
        public int maxViewDist;

        public ArrayView3D<int> tiles;
        public ArrayView<int> tileMaterialIDs;

        public dVoxelChunk(AABB aabb, Vec3 position, int width, int depth, int height, int maxViewDist, ArrayView3D<int> tiles, ArrayView<int> tileMaterialIDs)
        {
            this.aabb = aabb;
            this.position = position;
            this.width = width;
            this.depth = depth;
            this.height = height;
            this.maxViewDist = maxViewDist;
            this.tiles = tiles;
            this.tileMaterialIDs = tileMaterialIDs;
        }

        public int tileAt(Vec3 pos)
        {
            Vec3 offsetPos = pos - position;
            if((offsetPos.x >= 0 && offsetPos.x < width) && (offsetPos.y >= 0 && offsetPos.y < height) && (offsetPos.z >= 0 && offsetPos.z < depth))
            {
                return tiles[(int)offsetPos.x, (int)offsetPos.y, (int)offsetPos.z];
            }

            return -1;
        }


        public HitRecord hit(Ray ray, float tmin, float tmax)
        {
            if(aabb.hit(ray, tmin, tmax))
            {
                float t = tmin;

                Vec3 pos = ray.a;

                Vec3i iPos = new Vec3i(XMath.Floor(pos.x), XMath.Floor(pos.y), XMath.Floor(pos.z));

                Vec3 step = new Vec3(ray.b.x > 0 ? 1f : -1f, ray.b.y > 0 ? 1f : -1f, ray.b.z > 0 ? 1f : -1f);

                Vec3 tDelta = new Vec3(
                    XMath.Abs(1f / ray.b.x), 
                    XMath.Abs(1f / ray.b.y),
                    XMath.Abs(1f / ray.b.z));

                Vec3 dist = new Vec3(
                    step.x > 0 ? (iPos.x + 1 - pos.x) : (pos.x - iPos.x),
                    step.y > 0 ? (iPos.y + 1 - pos.y) : (pos.y - iPos.y),
                    step.z > 0 ? (iPos.z + 1 - pos.z) : (pos.z - iPos.z));
                
                Vec3 tMax = new Vec3(
                     float.IsInfinity(tDelta.x) ? float.MaxValue : tDelta.x * dist.x,
                     float.IsInfinity(tDelta.y) ? float.MaxValue : tDelta.y * dist.y,
                     float.IsInfinity(tDelta.z) ? float.MaxValue : tDelta.z * dist.z);

                int stepIndex = -1;
                int i = -1;

                while (i < maxViewDist)
                {
                    int tile = tileAt(pos);
                    i++;

                    if (tile > 0)
                    {
                        Vec3 hitPos = ray.pointAtParameter(t);
                        Vec3 hitNorm = new Vec3();

                        if(stepIndex == 0)
                        {
                            hitNorm = new Vec3(-step.x, 0, 0);
                        }
                        else if (stepIndex == 1)
                        {
                            hitNorm = new Vec3(0, -step.y, 0);
                        }
                        else if (stepIndex == 2)
                        {
                            hitNorm = new Vec3(0, 0, -step.z);
                        }

                        return new HitRecord(t, hitPos, hitNorm, false, tileMaterialIDs[tile], tile);
                    }

                    if (tMax.x < tMax.y)
                    {
                        if (tMax.x < tMax.z)
                        {
                            pos.x += step.x;
                            t = tMax.x;
                            tMax.x += tDelta.x;
                            stepIndex = 0;
                        }
                        else
                        {
                            pos.z += step.z;
                            t = tMax.z;
                            tMax.z += tDelta.z;
                            stepIndex = 2;
                        }
                    }
                    else
                    {
                        if (tMax.y < tMax.z)
                        {
                            pos.y += step.y;
                            t = tMax.y;
                            tMax.y += tDelta.y;
                            stepIndex = 1;
                        }
                        else
                        {
                            pos.z += step.z;
                            t = tMax.z;
                            tMax.z += tDelta.z;
                            stepIndex = 2;
                        }
                    }
                }

                return new HitRecord(float.MaxValue, new Vec3(), new Vec3(), false, -1, -1);
            }
            else
            {
                return new HitRecord(float.MaxValue, new Vec3(), new Vec3(), false, -1, -1);
            }
        }
    }

    public class hVoxelChunk
    {
        public Accelerator device;
        public Vec3 position;
        public int width;
        public int depth;
        public int height;
        public int maxViewDist;

        public int[,,] tiles;
        public int[] materialIDs;

        public MemoryBuffer3D<int> d_tiles;
        public MemoryBuffer<int> d_materialIDs;

        public hVoxelChunk(Accelerator device, Vec3 position, int width, int height, int depth, int maxViewDist, int[] materialIDs)
        {
            this.device = device;
            this.position = position - new Vec3(width / 2f, 0, depth / 2f);
            this.width = width;
            this.depth = depth;
            this.height = height;
            this.maxViewDist = maxViewDist;
            this.materialIDs = materialIDs;
            
            tiles = new int[width, height, depth];

            Random rng = new Random(5);

            for(int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    for (int z = 0; z < depth; z++)
                    {
                        tiles[x, y, z] = rng.NextDouble() < 0.25 ? rng.Next(1, materialIDs.Length) : 0;
                    }
                }
            }

            d_tiles = device.Allocate<int>(new Index3(width, height, depth));
            d_tiles.CopyFrom(tiles, new Index3(), new Index3(), d_tiles.Extent);

            d_materialIDs = device.Allocate<int>(materialIDs.Length);
            d_materialIDs.CopyFrom(materialIDs, 0, 0, materialIDs.Length);
        }

        public hVoxelChunk(Accelerator device, Vec3 position, int width, int depth, int height, int maxViewDist, int[] materialIDs, int[,,] tiles)
        {
            this.device = device;
            this.position = position;
            this.width = width;
            this.depth = depth;
            this.height = height;
            this.tiles = tiles;
            this.maxViewDist = maxViewDist;
            this.materialIDs = materialIDs;

            d_tiles = device.Allocate<int>(new Index3(width, height, depth));
            d_tiles.CopyFrom(tiles, new Index3(), new Index3(), d_tiles.Extent);
        }

        public void setTile(Vec3i pos, int tile)
        {
            tiles[pos.x, pos.y, pos.z] = tile;
        }

        public dVoxelChunk GetDeviceVoxelChunk()
        {
            return new dVoxelChunk(new AABB(position, new Vec3(position.x + width, position.y + height, position.z + depth)), position, width, depth, height, maxViewDist, d_tiles, d_materialIDs);
        }
    }
}
