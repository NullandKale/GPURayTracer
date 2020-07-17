﻿using GPURayTracer.Rendering.Primitives;
using ILGPU;
using ILGPU.Algorithms;
using ILGPU.Algorithms.ScanReduceOperations;
using ILGPU.Runtime;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace GPURayTracer.Rendering
{
    public static class Kernels
    {
        public static void CreatBitmap(Index1 index, ArrayView<float> data, ArrayView<byte> bitmapData, Camera camera)
        {
            //FLIP Y
            //int x = (camera.width - 1) - ((index) % camera.width);
            int y = (camera.height - 1) - ((index) / camera.width);

            //NORMAL X
            int x = ((index) % camera.width);
            //int y = ((index) / camera.width);

            int newIndex = ((y * camera.width) + x);

            bitmapData[(newIndex * 3)] = (byte)(255.99f * data[(index * 3)]);
            bitmapData[(newIndex * 3) + 1] = (byte)(255.99f * data[(index * 3) + 1]);
            bitmapData[(newIndex * 3) + 2] = (byte)(255.99f * data[(index * 3) + 2]);
        }

        public static void CreateGrayScaleBitmap(Index1 index, ArrayView<float> data, ArrayView<byte> bitmapData, Camera camera)
        {
            //FLIP Y
            //int x = (camera.width - 1) - ((index) % camera.width);
            int y = (camera.height - 1) - ((index) / camera.width);

            //NORMAL X
            int x = ((index) % camera.width);
            //int y = ((index) / camera.width);

            int newIndex = ((y * camera.width) + x);

            bitmapData[(newIndex * 3)] = (byte)(255.99f * data[(index)]);
            bitmapData[(newIndex * 3) + 1] = (byte)(255.99f * data[(index)]);
            bitmapData[(newIndex * 3) + 2] = (byte)(255.99f * data[(index)]);
        }

        public static float map(float x, float in_min, float in_max, float out_min, float out_max)
        {
            return (x - in_min) * (out_max - out_min) / (in_max - in_min) + out_min;
        }

        public static void Normalize(Index1 index, ArrayView<float> data, float min, float max)
        {
            data[index] = map(data[index], min, max, 0, 1);
        }

        public static (float min, float max) ReduceMax(Accelerator device, ArrayView<float> map)
        {
            using (var target = device.Allocate<float>(1))
            {
                // This overload requires an explicit output buffer but
                // uses an implicit temporary cache from the associated accelerator.
                // Call a different overload to use a user-defined memory cache.
                device.Reduce<float, MinFloat>(
                    device.DefaultStream,
                    map,
                    target.View);

                device.Synchronize();

                var min = target.GetAsArray();

                device.Reduce<float, MaxFloat>(
                device.DefaultStream,
                map,
                target.View);

                device.Synchronize();

                var max = target.GetAsArray();
                return (min[0], max[0]);
            }
        }

        public static void NULLTAA(Index1 index, 
            ArrayView<float> srcColor, ArrayView<float> srcZBuffer, ArrayView<int> srcSphereID, 
            ArrayView<float> dstColor, ArrayView<float> dstZBuffer, ArrayView<int> dstSphereID, 
            float depthFuzz, float exponent, int tick)
        {
            Vec3 newColor = new Vec3(srcColor[index * 3], srcColor[(index * 3) + 1], srcColor[(index * 3) + 2]);

            float newDepth = srcZBuffer[index];
            int newID = srcSphereID[index];

            float lastDepth = dstZBuffer[index];
            int lastID = dstSphereID[index];

            if(newID == -2)
            {
                return;
            }
            
            if (XMath.Abs(newDepth - lastDepth) > depthFuzz || tick == 0 || newID != lastID || newID == -1)
            {
                dstColor[(index * 3)    ] = newColor.x;
                dstColor[(index * 3) + 1] = newColor.y;
                dstColor[(index * 3) + 2] = newColor.z;

                dstSphereID[index] = newID;
                dstZBuffer[index] = newDepth;
            }
            else if(newID != -2)
            {
                if(tick < 1 / exponent)
                {
                    dstColor[(index * 3)] = ((1.0f / tick) * newColor.x)     + ((1 - (1.0f / tick)) * dstColor[(index * 3)]);
                    dstColor[(index * 3) + 1] = ((1.0f / tick) * newColor.y) + ((1 - (1.0f / tick)) * dstColor[(index * 3) + 1]);
                    dstColor[(index * 3) + 2] = ((1.0f / tick) * newColor.z) + ((1 - (1.0f / tick)) * dstColor[(index * 3) + 2]);

                    dstSphereID[index] = newID;
                    dstZBuffer[index] = newDepth;
                }
                else
                {
                    dstColor[(index * 3)] = (exponent * newColor.x) + ((1 - exponent) * dstColor[(index * 3)]);
                    dstColor[(index * 3) + 1] = (exponent * newColor.y) + ((1 - exponent) * dstColor[(index * 3) + 1]);
                    dstColor[(index * 3) + 2] = (exponent * newColor.z) + ((1 - exponent) * dstColor[(index * 3) + 2]);

                    dstSphereID[index] = newID;
                    dstZBuffer[index] = newDepth;
                }
            }
        }
    }
}
