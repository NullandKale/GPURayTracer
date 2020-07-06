using GPURayTracer.Utils;
using ILGPU;
using ILGPU.Algorithms;
using ILGPU.IR.Types;
using ILGPU.Runtime;
using ILGPU.Runtime.CPU;
using ILGPU.Runtime.Cuda;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
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
        int tickMultiplyer;

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

        public void startConwayThread(FrameManager frameManager, int width, int height, int percentFilled, int targetFPS, int tickMultiplyer)
        {
            this.width = width;
            this.height = height;
            this.targetFPS = targetFPS;
            this.tickMultiplyer = tickMultiplyer;
            run = true;
            frame = frameManager;
            output = generateRandom(width * height * 3, percentFilled);
            //output = generateFromImage(width, height, "troy.jpg");
            rFPStimer = new UpdateStatsTimer();

            t = new Thread(runConway);
            t.IsBackground = true;
            t.Start();
        }

        public void startNoiseThread(FrameManager frameManager, int width, int height, int targetFPS)
        {
            this.width = width;
            this.height = height;
            this.targetFPS = targetFPS;
            run = true;
            frame = frameManager;
            output = generateRandom(width * height * 3, 20);
            //output = generateFromImage(width, height, "troy.jpg");
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

        public void JoinRenderThread()
        {
            run = false;
            t.Join();
        }

        public void dispose()
        {
            JoinRenderThread();
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

                    for(int i = 0; i < 100; i+=2)
                    {
                        generateConwayImage(width, height, buffer0, buffer1, false);
                        generateConwayImage(width, height, buffer1, buffer0, false);
                    }

                    generateConwayImage(width, height, buffer0, buffer1, true);
                    
                    frame.write(ref output);
                    rFPStimer.endUpdateForTargetUpdateTime((1000.0 / targetFPS), true);
                    ready = true;
                }
            }

            buffer0.Dispose();
            buffer1.Dispose();
        }

        private byte[] generateRandom(int count, int percent)
        {
            Random rng = new Random();
            byte[] data = new byte[count];

            for (int i = 0; i < count; i += 3)
            {
                data[i] = (byte)(rng.Next(0, 101) > percent ? 255 : 0);
            }

            return data;
        }

        private byte[] generateFromImage(int width, int height, string image)
        {
            Random rng = new Random();
            byte[] data = new byte[width * height * 3];
            Bitmap b = (Bitmap)Bitmap.FromFile(image);

            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    int index = (j * width) + i;

                    if(i > 0 && i < b.Width && j > 0 && j < b.Height)
                    {
                        Color c = b.GetPixel(i, j);
                        data[(index * 3)    ] = c.R;
                        data[(index * 3) + 1] = c.G;
                        data[(index * 3) + 2] = c.B;
                    }
                    else
                    {
                        data[(index * 3)    ] = (byte)(rng.Next(0, 101) > 20 ? 255 : 0);
                        data[(index * 3) + 1] = (byte)(rng.Next(0, 101) > 20 ? 255 : 0);
                        data[(index * 3) + 2] = (byte)(rng.Next(0, 101) > 20 ? 255 : 0);
                    }
                }
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

        public void generateConwayImage(int width, int height, MemoryBuffer<byte> buffer0, MemoryBuffer<byte> buffer1, bool setoutput)
        {
            conwayKernel(buffer0.Extent / 3, buffer0.View, buffer1.View, width, height);

            if(setoutput)
            {
                cuda.Synchronize();
                buffer1.CopyTo(output, 0, 0, buffer1.Length);
            }
        }

        public void generateConwayImageCPU(int width, int height, MemoryBuffer<byte> buffer0, MemoryBuffer<byte> buffer1)
        {
            conwayKernelCPU(buffer0.Extent / 3, buffer0.View, buffer1.View, width, height);

            cpu.Synchronize();

            buffer1.CopyTo(output, 0, 0, buffer1.Length);
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

                if(isFilled)
                {
                    if (neighborCount <= 1)
                    {
                        newState = false;
                    }

                    if (neighborCount > 3)
                    {
                        newState = false;
                    }
                }
                else if(neighborCount == 3)
                {
                    newState = true;
                }

                if (newState)
                {
                    data1[(index * 3)] = 255;
                    if (isFilled)
                    {
                        if (data0[index * 3 + 1] == 255)
                        {
                            data1[index * 3 + 1] = 255;
                        }
                        else
                        {
                            data1[(index * 3) + 1] = (byte)(data0[index * 3 + 1] + 1);
                        }
                    }

                    if (data0[index * 3 + 2] == 255)
                    {
                        data1[index * 3 + 2] = 255;
                    }
                    else
                    {
                        data1[(index * 3) + 2] = (byte)(data0[index * 3 + 2] + 1);
                    }
                }
                else
                {
                    data1[(index * 3)] = 0;
                    if (data0[index * 3 + 1] > 10)
                    {
                        data1[(index * 3) + 1] = (byte)(data0[index * 3 + 1] - 1);
                    }
                    if (data0[index * 3 + 2] > 180)
                    {
                        data1[(index * 3) + 2] = (byte)(data0[index * 3 + 2] - 1);
                    }
                }
            }
        }

    }
}
