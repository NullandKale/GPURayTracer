using GPURayTracer.Rendering.Primitives;
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

            bitmapData[(newIndex * 3)]     = (byte)(255.99f * data[(index * 3)]);
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

            bitmapData[(newIndex * 3)] =     (byte)(255.99f * data[(index)]);
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

        public static void NormalizeLighting(Index1 index, ArrayView<float> data)
        {
            int rIndex = index * 3;
            int gIndex = rIndex + 1;
            int bIndex = rIndex + 2;

            if(data[rIndex] != -1)
            {
                Vec3 color = Vec3.aces_approx(new Vec3(data[rIndex], data[gIndex], data[bIndex]));
                data[rIndex] = color.x;
                data[gIndex] = color.y;
                data[bIndex] = color.z;
            }
        }

        public static void InvertedNormalize(Index1 index, ArrayView<float> data, float min, float max)
        {
            data[index] = map(data[index], min, max, 1, 0);
        }

        public static void CombineLightingAndColor(Index1 index, ArrayView<float> color, ArrayView<float> lights, ArrayView<int> sphereIDs)
        {
            float minLight = 0.000005f;

            int rIndex = index * 3;
            int gIndex = rIndex + 1;
            int bIndex = rIndex + 2;

            Vec3 col   = new Vec3(color[rIndex], color[gIndex], color[bIndex]);
            Vec3 light = new Vec3(lights[rIndex], lights[gIndex], lights[bIndex]);

            if (sphereIDs[index] == -3)
            {
                color[rIndex] = 1;
                color[gIndex] = 0;
                color[bIndex] = 1;
            }
            else if (sphereIDs[index] == -2)
            {
                color[rIndex] = col.x;
                color[gIndex] = col.y;
                color[bIndex] = col.z;
            }
            else if (sphereIDs[index] == -1)
            {
                color[rIndex] = col.x * minLight;
                color[gIndex] = col.y * minLight;
                color[bIndex] = col.z * minLight;
            }
            else if(light.x == -1)
            {
                color[rIndex] = col.x * minLight;
                color[gIndex] = col.y * minLight;
                color[bIndex] = col.z * minLight;
            }
            else
            {
                if(light.x < minLight)
                {
                    color[rIndex] = col.x * minLight;
                }
                else
                {
                    color[rIndex] = (col.x * (light.x));
                }

                if (light.y < minLight)
                {
                    color[gIndex] = col.y * minLight;
                }
                else
                {
                    color[gIndex] = (col.y * (light.y));
                }

                if (light.z < minLight)
                {
                    color[bIndex] = col.z * minLight;
                }
                else
                {
                    color[bIndex] = (col.z * (light.z));
                }
            }
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
            dFramebuffer srcFramebuffer, 
            dFramebuffer dstFramebuffer, 
            float depthFuzz, float exponent, int tick)
        {

            float newDepth = srcFramebuffer.ZBuffer[index];
            int newID = srcFramebuffer.DrawableIDBuffer[index];

            float lastDepth = dstFramebuffer.ZBuffer[index];

            int rIndex = index * 3;
            int gIndex = rIndex + 1;
            int bIndex = gIndex + 1;

            if (/*XMath.Abs(newDepth - lastDepth) > depthFuzz || */tick == 0)
            {
                dstFramebuffer.ColorFrameBuffer[rIndex] = srcFramebuffer.ColorFrameBuffer[rIndex];
                dstFramebuffer.ColorFrameBuffer[gIndex] = srcFramebuffer.ColorFrameBuffer[gIndex];
                dstFramebuffer.ColorFrameBuffer[bIndex] = srcFramebuffer.ColorFrameBuffer[bIndex];

                dstFramebuffer.DrawableIDBuffer[index] = newID;
                dstFramebuffer.ZBuffer[index] = newDepth;
            }
            else
            {
                if(tick < 1 / exponent)
                {
                    dstFramebuffer.ColorFrameBuffer[rIndex] = ((1.0f / tick) * srcFramebuffer.ColorFrameBuffer[rIndex]) + ((1 - (1.0f / tick)) * dstFramebuffer.ColorFrameBuffer[rIndex]);
                    dstFramebuffer.ColorFrameBuffer[gIndex] = ((1.0f / tick) * srcFramebuffer.ColorFrameBuffer[gIndex]) + ((1 - (1.0f / tick)) * dstFramebuffer.ColorFrameBuffer[gIndex]);
                    dstFramebuffer.ColorFrameBuffer[bIndex] = ((1.0f / tick) * srcFramebuffer.ColorFrameBuffer[bIndex]) + ((1 - (1.0f / tick)) * dstFramebuffer.ColorFrameBuffer[bIndex]);

                    dstFramebuffer.DrawableIDBuffer[index] = newID;
                    dstFramebuffer.ZBuffer[index] = newDepth;
                }
                else
                {
                    dstFramebuffer.ColorFrameBuffer[rIndex] = (exponent * srcFramebuffer.ColorFrameBuffer[rIndex]) + ((1 - exponent) * dstFramebuffer.ColorFrameBuffer[rIndex]);
                    dstFramebuffer.ColorFrameBuffer[gIndex] = (exponent * srcFramebuffer.ColorFrameBuffer[gIndex]) + ((1 - exponent) * dstFramebuffer.ColorFrameBuffer[gIndex]);
                    dstFramebuffer.ColorFrameBuffer[bIndex] = (exponent * srcFramebuffer.ColorFrameBuffer[bIndex]) + ((1 - exponent) * dstFramebuffer.ColorFrameBuffer[bIndex]);

                    dstFramebuffer.DrawableIDBuffer[index] = newID;
                    dstFramebuffer.ZBuffer[index] = newDepth;
                }
            }
        }

        public static void NULLLowPassFilter(Index1 index,
            ArrayView<float> srcColor,
            ArrayView<float> dstColor,
            Camera camera, int filterWidth)
        {
            int x = ((index) % camera.width);
            int y = ((index) / camera.width);

            int rIndex = index * 3;
            int gIndex = rIndex + 1;
            int bIndex = rIndex + 2;

            float filterWidthHalf = filterWidth / 2f;

            if (x >= filterWidth && x <= camera.width - filterWidth && y >= filterWidth && y <= camera.width - filterWidth)
            {
                float distMax = XMath.Sqrt((filterWidthHalf * filterWidthHalf) + (filterWidthHalf * filterWidthHalf)) * 1.1f;

                Vec3 fuzzedColor = new Vec3(srcColor[rIndex], srcColor[gIndex], srcColor[bIndex]);
                float sampleCounter = 1;

                for (int i = 0; i < filterWidth; i++)
                {
                    for (int j = 0; j < filterWidth; j++)
                    {
                        int imageX = (x + (i - (int)filterWidthHalf));
                        int imageY = (y + (j - (int)filterWidthHalf));

                        int newIndex = ((imageY * camera.width) + imageX);
                        if (newIndex < srcColor.Length / 3)
                        {
                            int r = newIndex * 3;
                            int g = r + 1;
                            int b = r + 2;

                            float distMult = 1.0f / (XMath.Sqrt(((i - x) * (i - x)) + ((j - y) * (j - y))) / distMax);

                            fuzzedColor += new Vec3(srcColor[r] * distMult, srcColor[g] * distMult, srcColor[b] * distMult);
                            sampleCounter++;
                        }
                    }
                }

                dstColor[rIndex] = fuzzedColor.x / sampleCounter;
                dstColor[gIndex] = fuzzedColor.y / sampleCounter;
                dstColor[bIndex] = fuzzedColor.z / sampleCounter;
            }
            else
            {
                Vec3 fuzzedColor = new Vec3(srcColor[rIndex], srcColor[gIndex], srcColor[bIndex]);

                dstColor[rIndex] = fuzzedColor.x;
                dstColor[gIndex] = fuzzedColor.y;
                dstColor[bIndex] = fuzzedColor.z;
            }
        }
    }
}
