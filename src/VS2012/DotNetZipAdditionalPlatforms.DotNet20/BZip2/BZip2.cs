namespace DotNetZipAdditionalPlatforms.BZip2
{
    using System;

    internal static class BZip2
    {
        public const int BlockSizeMultiple = 0x186a0;
        public const int G_SIZE = 50;
        public const int MaxAlphaSize = 0x102;
        public const int MaxBlockSize = 9;
        public const int MaxCodeLength = 0x17;
        public const int MaxSelectors = (2 + (0xdbba0 / G_SIZE));
        public const int MinBlockSize = 1;
        public const int N_ITERS = 4;
        public const int NGroups = 6;
        public const int NUM_OVERSHOOT_BYTES = 20;
        internal const int QSORT_STACK_SIZE = 0x3e8;
        public const char RUNA = '\0';
        public const char RUNB = '\x0001';

        internal static T[][] InitRectangularArray<T>(int d1, int d2)
        {
            T[][] localArray = new T[d1][];
            for (int i = 0; i < d1; i++)
            {
                localArray[i] = new T[d2];
            }
            return localArray;
        }
    }
}

