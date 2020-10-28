using NullEngine.Rendering.DataStructures;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace NullEngine.Rendering.Implementation
{
    public class CPU
    {
        public static void ClearFramebuffer(byte[] framebuffer, Vec3 color)
        {
            //for(int i = 0; i < framebuffer.Length;)
            //{
            //    framebuffer[i++] = (byte)(color.x * 255.0);
            //    framebuffer[i++] = (byte)(color.y * 255.0);
            //    framebuffer[i++] = (byte)(color.z * 255.0);
            //}
            Parallel.For(0, framebuffer.Length / 3,
             index =>
             {
                 int r = index * 3;
                 framebuffer[r++] = (byte)(color.x * 255.0);
                 framebuffer[r++] = (byte)(color.y * 255.0);
                 framebuffer[r] = (byte)(color.z * 255.0);
             });
        }
    }
}
