using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using ILGPU;
using ILGPU.Runtime;
using NullEngine.Rendering.Implementation;

namespace NullEngine.Rendering.DataStructures
{
    public class FloatFrameBuffer
    {
        public dFloatFrameBuffer frameBuffer;
        public MemoryBuffer1D<float, Stride1D.Dense> memoryBuffer;

        public FloatFrameBuffer(GPU gpu, int height, int width)
        {
            memoryBuffer = gpu.device.Allocate1D<float>(height * width * 3);
            frameBuffer = new dFloatFrameBuffer(height, width, memoryBuffer);
        }

        public void Dispose()
        {
            memoryBuffer.Dispose();
        }
    }

    public struct dFloatFrameBuffer
    {
        public int height;
        public int width;
        public ArrayView1D<float, Stride1D.Dense> frame;

        public dFloatFrameBuffer(int height, int width, MemoryBuffer1D<float, Stride1D.Dense> frame)
        {
            this.height = height;
            this.width = width;
            this.frame = frame.View;
        }

        public Vec3 readFrameBuffer(int x, int y)
        {
            int newIndex = ((y * width) + x) * 3;
            return readFrameBuffer(newIndex);
        }

        public Vec3 readFrameBuffer(int index)
        {
            return new Vec3(frame[index], frame[index + 1], frame[index + 2]);
        }

        public void writeFrameBuffer(int x, int y, float r, float g, float b)
        {
            int newIndex = ((y * width) + x) * 3;
            writeFrameBuffer(newIndex, r, b, g);
        }

        public void writeFrameBuffer(int index, float r, float g, float b)
        {
            frame[index] = r;
            frame[index + 1] = g;
            frame[index + 2] = b;
        }
    }

    public class ByteFrameBuffer
    {
        public dByteFrameBuffer frameBuffer;
        public MemoryBuffer1D<byte, Stride1D.Dense> memoryBuffer;
        public bool isDisposed = false;
        public bool inUse = false;

        public ByteFrameBuffer(GPU gpu, int height, int width)
        {
            memoryBuffer = gpu.device.Allocate1D<byte>(height * width * 3);
            frameBuffer = new dByteFrameBuffer(height, width, memoryBuffer);
        }

        public void Dispose()
        {
            while(inUse)
            {
                Thread.Sleep(1);
            }
            isDisposed = true;
            memoryBuffer.Dispose();
        }
    }

    public struct dByteFrameBuffer
    {
        public int height;
        public int width;
        public ArrayView1D<byte, Stride1D.Dense> frame;

        public dByteFrameBuffer(int height, int width, MemoryBuffer1D<byte, Stride1D.Dense> frame)
        {
            this.height = height;
            this.width = width;
            this.frame = frame.View;
        }

        public Vec3i readFrameBuffer(int x, int y)
        {
            int newIndex = ((y * width) + x) * 3;
            return readFrameBuffer(newIndex);
        }

        public Vec3i readFrameBuffer(int index)
        {
            return new Vec3i(frame[index], frame[index + 1], frame[index + 2]);
        }

        public void writeFrameBuffer(int x, int y, float r, float g, float b)
        {
            int newIndex = ((y * width) + x) * 3;
            writeFrameBuffer(newIndex, r, b, g);
        }


        public void writeFrameBuffer(int x, int y, byte r, byte g, byte b)
        {
            int newIndex = ((y * width) + x) * 3;
            writeFrameBuffer(newIndex, r, b, g);
        }

        public void writeFrameBuffer(int index, byte r, byte g, byte b)
        {
            frame[index] = r;
            frame[index + 1] = g;
            frame[index + 2] = b;
        }

        public void writeFrameBuffer(int index, float r, float g, float b)
        {
            frame[index] = (byte)(r * 255f);
            frame[index + 1] = (byte)(g * 255f);
            frame[index + 2] = (byte)(b * 255f);
        }
    }
}
