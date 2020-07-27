using GPURayTracer.Rendering;
using ILGPU;
using ILGPU.Algorithms;
using ILGPU.Algorithms.ScanReduceOperations;
using ILGPU.Runtime;
using System;
using System.Collections.Generic;
using System.Text;

namespace GPURayTracer.Utils
{
    public static class UtilKernels
    {
        public static void ChannelMinMaxKernel(
            int channelIdx,
            int numChannels,
            ArrayView<float> input,
            ArrayView<float> output)
        {
            MinFloat minReduction = default;
            MaxFloat maxReduction = default;

            // Each kernel will apply the reductions locally.
            var stride = GridExtensions.GridStrideLoopStride;
            var minReduced = minReduction.Identity;
            var maxReduced = maxReduction.Identity;

            for (var idx = Grid.GlobalIndex.X; idx < input.Length; idx += stride)
            {
                if (idx % numChannels == channelIdx)
                {
                    minReduced = minReduction.Apply(minReduced, input[idx]);
                    maxReduced = maxReduction.Apply(maxReduced, input[idx]);
                }
            }

            // Perform a block-wide reduction.
            minReduced = GroupExtensions.Reduce<float, MinFloat>(minReduced);
            maxReduced = GroupExtensions.Reduce<float, MaxFloat>(maxReduced);

            // The first thread of the group is responsible for writing the
            // reduced results.
            if (Group.IsFirstThread)
            {
                minReduction.AtomicApply(ref output[0], minReduced);
                maxReduction.AtomicApply(ref output[1], maxReduced);
            }
        }

        private static (Index1, Index1) ComputeReductionDimension(
            Accelerator accelerator,
            Index1 dataLength)
        {
            // Calculate the group dimensions, ensuring it's a multiple of the warp size.
            var warpSize = accelerator.WarpSize;
            var groupDim = Math.Max(warpSize, (accelerator.MaxNumThreadsPerGroup / warpSize) * warpSize);

            // Calculate the number of grids required to process the input data, as
            // a multiple of the group size.
            var gridDim = Math.Min((dataLength + groupDim - 1) / groupDim, groupDim);
            return (gridDim * 2, groupDim / 2);
        }

        public static (Vec3 min, Vec3 max) MinMaxFloatImage(Accelerator accelerator, ArrayView<float> input)
        {
            float[] mins = new float[3];
            float[] maxs = new float[3];

            var kernel =
            accelerator.LoadStreamKernel<
                int,
                int,
                ArrayView<float>,
                ArrayView<float>>(
                    ChannelMinMaxKernel);

            var output = accelerator.Allocate<float>(2);
            var outputValues = new float[] { default(MinFloat).Identity, default(MaxFloat).Identity };
            output.CopyFrom(outputValues, 0, 0, outputValues.Length);

            // Calculate the group dimensions for running the kernel.
            var kernelConfig = ComputeReductionDimension(accelerator, input.Length);

            for (int i = 0; i < 3; i++)
            {
                kernel(
                    kernelConfig,
                    i,
                    3,
                    input,
                    output.View);

                accelerator.Synchronize();

                var minMax = output.GetAsArray();

                mins[i] = minMax[0];
                maxs[i] = minMax[1];
            }

            Vec3 min = new Vec3(mins[0], mins[1], mins[2]);
            Vec3 max = new Vec3(maxs[0], maxs[1], maxs[2]);

            output.Dispose();

            return (min, max);
        }
    }
}
