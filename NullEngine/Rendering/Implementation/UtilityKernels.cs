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
