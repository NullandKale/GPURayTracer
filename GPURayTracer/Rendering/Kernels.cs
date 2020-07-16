using GPURayTracer.Rendering.Primitives;
using ILGPU;
using ILGPU.Algorithms;
using System;
using System.Collections.Generic;
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

        public static void RadianceFuzz(Index1 index, ArrayView<float> src, ArrayView<float> dst, float minRadiance, int searchWidth, Camera camera)
        {
            Vec3 color = new Vec3(src[index * 3], src[(index * 3) + 1], src[(index * 3) + 2]);

            if(color.x > minRadiance && color.y > minRadiance && color.z > minRadiance)
            {
                dst[(index * 3)]    =  src[(index * 3)];
                dst[(index * 3) + 1] = src[(index * 3) + 1];
                dst[(index * 3) + 2] = src[(index * 3) + 2];
            }
            else
            {
                int x = ((index) % camera.width);
                int y = ((index) / camera.width);

                int samples = 0;

                for(int i = -searchWidth; i <= searchWidth; i++)
                {
                    int xPos = x + i;
                    if (xPos >= 0 && xPos < camera.width)
                    {
                        for (int j = -searchWidth; j <= searchWidth; j++)
                        {
                            int yPos = y + j;
                            if (yPos >= 0 && yPos < camera.height)
                            {
                                int newIndex = (yPos * camera.width) + xPos;
                                Vec3 c = new Vec3(src[newIndex * 3], src[(newIndex * 3) + 1], src[(newIndex * 3) + 2]);
                                color += c;
                                samples++;
                            }
                        }
                    }
                }

                if(samples == 0)
                {
                    dst[(index * 3)] = src[(index * 3)];
                    dst[(index * 3) + 1] = src[(index * 3) + 1];
                    dst[(index * 3) + 2] = src[(index * 3) + 2];
                }
                else
                {
                    color /= samples;
                    dst[(index * 3)]     = color.x;
                    dst[(index * 3) + 1] = color.y;
                    dst[(index * 3) + 2] = color.z;
                }
            }
        }
    }
}
