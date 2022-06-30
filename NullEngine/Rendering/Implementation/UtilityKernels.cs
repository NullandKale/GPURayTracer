using ILGPU;
using ILGPU.Runtime;
using NullEngine.Rendering.DataStructures;
using System;
using System.Collections.Generic;
using System.Text;

namespace NullEngine.Rendering.Implementation
{
    public static class UtilityKernels
    {
        public static void ClearByteFramebuffer(Index1D index, dByteFrameBuffer frameBuffer, byte r, byte g, byte b)
        {
            //FLIP Y
            //int x = (frameBuffer.width - 1) - ((index) % frameBuffer.width);
            int y = (frameBuffer.height - 1) - ((index) / frameBuffer.width);

            //NORMAL X
            int x = ((index) % frameBuffer.width);
            //int y = ((index) / frameBuffer.width);

            int newIndex = ((y * frameBuffer.width) + x);
            frameBuffer.writeFrameBuffer(newIndex * 3, r, g, b);
        }

        public static Vec3 GetRandomColor(int id)
        {
            switch (id % 14)
            {
                case 0:
                    {
                        return new Vec3(0, 0, 1);
                    }
                case 1:
                    {
                        return new Vec3(0, 1, 0);
                    }
                case 2:
                    {
                        return new Vec3(1, 0, 0);
                    }
                case 3:
                    {
                        return new Vec3(1, 0, 1);
                    }
                case 4:
                    {
                        return new Vec3(1, 1, 0);
                    }
                case 5:
                    {
                        return new Vec3(1, 1, 1);
                    }
                case 6:
                    {
                        return new Vec3(0, 1, 1);
                    }
                case 7:
                    {
                        return new Vec3(0.5f, 0.5f, 0.5f);
                    }
                case 8:
                    {
                        return new Vec3(0, 0, 0.5f);
                    }
                case 9:
                    {
                        return new Vec3(0, 0.5f, 0);
                    }
                case 10:
                    {
                        return new Vec3(0, 0.5f, 0.5f);
                    }
                case 11:
                    {
                        return new Vec3(0.5f, 0, 0);
                    }
                case 12:
                    {
                        return new Vec3(0.5f, 0, 0.5f);
                    }
                case 13:
                    {
                        return new Vec3(0.5f, 0.5f, 0);
                    }
            }

            return new Vec3();
        }

        public static Vec3 readFrameBuffer(ArrayView1D<float, Stride1D.Dense> frame, int width, int x, int y)
        {
            int newIndex = ((y * width) + x) * 3;
            return readFrameBuffer(frame, newIndex);
        }

        public static Vec3 readFrameBuffer(ArrayView1D<float, Stride1D.Dense> frame, int index)
        {
            return new Vec3(frame[index], frame[index + 1], frame[index + 2]);
        }
    }
}
