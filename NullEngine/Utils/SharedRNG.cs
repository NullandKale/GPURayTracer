using System;
using System.Collections.Generic;
using System.Text;

namespace NullEngine.Utils
{
    public static class SharedRNG
    {
        private static Random random = new Random();

        public static void reseed(int seed)
        {
            random = new Random(seed);
        }

        public static int randi(int min, int max)
        {
            return random.Next(min, max);
        }
    }
}
