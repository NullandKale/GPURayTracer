using GPURayTracer.Utils;
using ILGPU;
using ILGPU.Algorithms;
using ILGPU.Runtime;
using ILGPU.Runtime.CPU;
using ILGPU.Runtime.Cuda;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;

namespace GPURayTracer.Rendering
{
    public class RayTracer
    {
        public bool run = true;
        public bool pause = false;
        public bool ready = false;

        private Thread t;
        public Context context;
        public CudaAccelerator cuda;
        public CPUAccelerator cpu;

        Action<Index1, ArrayView<int>, int, int> renderKernel;
        Action<Index1, ArrayView<byte>, ArrayView<byte>, int, int> conwayKernel;
        Action<Index1, ArrayView<byte>, ArrayView<byte>, int, int> conwayKernelCPU;

        public byte[] output;
        FrameManager frame;
        int width;
        int height;
        int targetFPS;

        public UpdateStatsTimer rFPStimer;

        public RayTracer()
        {
            context = new Context();
            cuda = new CudaAccelerator(context);
            cpu = new CPUAccelerator(context);
            renderKernel = cuda.LoadAutoGroupedStreamKernel<Index1, ArrayView<int>, int, int>(RenderKernel);
            conwayKernel = cuda.LoadAutoGroupedStreamKernel<Index1, ArrayView<byte>, ArrayView<byte>, int, int>(ConwayKernel);
            conwayKernelCPU = cpu.LoadAutoGroupedStreamKernel<Index1, ArrayView<byte>, ArrayView<byte>, int, int>(ConwayKernel);
        }

        public void startThread(FrameManager frameManager, int width, int height, int targetFPS)
        {
            this.width = width;
            this.height = height;
            this.targetFPS = targetFPS;
            frame = frameManager;
            output = generateRandom(width * height * 3);
            rFPStimer = new UpdateStatsTimer();

            t = new Thread(runConway);
            t.IsBackground = true;
            t.Start();
        }

        public void waitForReady()
        {
            for(int i = 0; i < 100; i++)
            {
                if(ready)
                {
                    return;
                }

                Thread.Sleep(1);
            }
        }

        public void dispose()
        {
            run = false;
            t.Join();
            cpu.Dispose();
            cuda.Dispose();
            context.Dispose();
        }
        private void runConway()
        {
            MemoryBuffer<byte> buffer0 = cuda.Allocate<byte>(width * height * 3);
            MemoryBuffer<byte> buffer1 = cuda.Allocate<byte>(width * height * 3);

            while (run)
            {
                if(pause)
                {
                    ready = true;
                }
                else
                {
                    ready = false;
                    rFPStimer.startUpdate();
                    buffer0.CopyFrom(output, 0, 0, output.Length);
                    generateConwayImage(ref output, width, height, buffer0, buffer1);
                    frame.write(ref output);
                    rFPStimer.endUpdateForTargetUpdateTime((1000.0 / targetFPS), true);
                    ready = true;
                }
            }

            buffer0.Dispose();
            buffer1.Dispose();
        }

        private byte[] generateRandom(int count)
        {
            Random rng = new Random();
            byte[] data = new byte[count];

            for (int i = 0; i < count; i += 3)
            {
                data[i] = (byte)(rng.Next(0, 101) > 30 ? 255 : 0);
            }

            return data;
        }

        public int[] generateImage(int width, int height)
        {
            using (MemoryBuffer<int> buffer = cuda.Allocate<int>(width * height * 3))
            {
                renderKernel(buffer.Extent / 3, buffer.View, width, height);

                cuda.Synchronize();

                return buffer.GetAsArray();
            }
        }

        public void generateConwayImage(ref byte[] data, int width, int height, MemoryBuffer<byte> buffer0, MemoryBuffer<byte> buffer1)
        {
            conwayKernel(buffer0.Extent / 3, buffer0.View, buffer1.View, width, height);

            cuda.Synchronize();

            buffer1.CopyTo(data, 0, 0, buffer1.Length);
        }

        public void generateConwayImageCPU(ref byte[] data, int width, int height, MemoryBuffer<byte> buffer0, MemoryBuffer<byte> buffer1)
        {
            conwayKernelCPU(buffer0.Extent / 3, buffer0.View, buffer1.View, width, height);

            cpu.Synchronize();

            buffer1.CopyTo(data, 0, 0, buffer1.Length);
        }

        private static void RenderKernel(Index1 index, ArrayView<int> data, int width, int height)
        {
            // color cheatsheet
            //int r = data[(index * 3)];
            //int g = data[(index * 3) + 1];
            //int b = data[(index * 3) + 2];

            int x = ((index) % width);
            int y = ((index) / width);

            float r = (float)x / (float)(width - 1);
            float g = (float)y / (float)(height - 1);

            int ir = (int)(255.99 * r);
            int ig = (int)(255.99 * g);
            int ib = (int)(255.99 * 0.25);

            data[(index * 3)] = ir;
            data[(index * 3) + 1] = ig;
            data[(index * 3) + 2] = ib;
        }

        private static void ConwayKernel(Index1 index, ArrayView<byte> data0, ArrayView<byte> data1, int width, int height)
        {
            // color cheatsheet
            //int r = data[(index * 3)];
            //int g = data[(index * 3) + 1];
            //int b = data[(index * 3) + 2];

            int x = ((index) % width);
            int y = ((index) / width);

            if (x > 1 && y > 1 && x < width - 1 && y < height - 1)
            {
                bool isFilled = data0[(index * 3)] == 255;
                bool newState = isFilled;

                int n0 = data0[((( y - 1) * width) + (x - 1)) * 3];
                int n1 = data0[((  y      * width) + (x - 1)) * 3];
                int n2 = data0[((( y + 1) * width) + (x - 1)) * 3];
                int n3 = data0[((( y - 1) * width) +  x)      * 3];
                int n4 = data0[((( y + 1) * width) +  x)      * 3];
                int n5 = data0[((( y - 1) * width) + (x + 1)) * 3];
                int n6 = data0[((  y      * width) + (x + 1)) * 3];
                int n7 = data0[((( y + 1) * width) + (x + 1)) * 3];
                int neighborCount = (n0 + n1 + n2 + n3 + n4 + n5 + n6 + n7) / 255;

                if (isFilled && neighborCount <= 1)
                {
                    newState = false;
                }

                if (isFilled && neighborCount > 3)
                {
                    newState = false;
                }

                if (!isFilled && neighborCount == 3)
                {
                    newState = true;
                }

                if (newState)
                {
                    data1[(index * 3)] = 255;
                    data1[(index * 3) + 1] = (byte)(data0[index * 3 + 1] + 1);
                    data1[(index * 3) + 2] = (byte)(data0[index * 3 + 2] + 1);
                }
                else
                {
                    data1[(index * 3)] = 0;
                    data1[(index * 3) + 1] = 0;
                    if(data0[index * 3 + 2] > 50)
                    {
                        data1[(index * 3) + 2] = (byte)(data0[index * 3 + 2] - 1);
                    }
                }
            }
        }

    }
}
