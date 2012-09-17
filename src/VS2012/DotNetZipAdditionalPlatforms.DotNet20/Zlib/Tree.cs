namespace DotNetZipAdditionalPlatforms.Zlib
{
    using System;

    internal sealed class Tree
    {
        private static readonly sbyte[] _dist_code = new sbyte[] { 
            0, 1, 2, 3, 4, 4, 5, 5, 6, 6, 6, 6, 7, 7, 7, 7, 
            8, 8, 8, 8, 8, 8, 8, 8, 9, 9, 9, 9, 9, 9, 9, 9, 
            10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 
            11, 11, 11, 11, 11, 11, 11, 11, 11, 11, 11, 11, 11, 11, 11, 11, 
            12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 
            12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 
            13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 
            13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 
            14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 
            14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 
            14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 
            14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 
            15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 
            15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 
            15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 
            15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 
            0, 0, 0x10, 0x11, 0x12, 0x12, 0x13, 0x13, 20, 20, 20, 20, 0x15, 0x15, 0x15, 0x15, 
            0x16, 0x16, 0x16, 0x16, 0x16, 0x16, 0x16, 0x16, 0x17, 0x17, 0x17, 0x17, 0x17, 0x17, 0x17, 0x17, 
            0x18, 0x18, 0x18, 0x18, 0x18, 0x18, 0x18, 0x18, 0x18, 0x18, 0x18, 0x18, 0x18, 0x18, 0x18, 0x18, 
            0x19, 0x19, 0x19, 0x19, 0x19, 0x19, 0x19, 0x19, 0x19, 0x19, 0x19, 0x19, 0x19, 0x19, 0x19, 0x19, 
            0x1a, 0x1a, 0x1a, 0x1a, 0x1a, 0x1a, 0x1a, 0x1a, 0x1a, 0x1a, 0x1a, 0x1a, 0x1a, 0x1a, 0x1a, 0x1a, 
            0x1a, 0x1a, 0x1a, 0x1a, 0x1a, 0x1a, 0x1a, 0x1a, 0x1a, 0x1a, 0x1a, 0x1a, 0x1a, 0x1a, 0x1a, 0x1a, 
            0x1b, 0x1b, 0x1b, 0x1b, 0x1b, 0x1b, 0x1b, 0x1b, 0x1b, 0x1b, 0x1b, 0x1b, 0x1b, 0x1b, 0x1b, 0x1b, 
            0x1b, 0x1b, 0x1b, 0x1b, 0x1b, 0x1b, 0x1b, 0x1b, 0x1b, 0x1b, 0x1b, 0x1b, 0x1b, 0x1b, 0x1b, 0x1b, 
            0x1c, 0x1c, 0x1c, 0x1c, 0x1c, 0x1c, 0x1c, 0x1c, 0x1c, 0x1c, 0x1c, 0x1c, 0x1c, 0x1c, 0x1c, 0x1c, 
            0x1c, 0x1c, 0x1c, 0x1c, 0x1c, 0x1c, 0x1c, 0x1c, 0x1c, 0x1c, 0x1c, 0x1c, 0x1c, 0x1c, 0x1c, 0x1c, 
            0x1c, 0x1c, 0x1c, 0x1c, 0x1c, 0x1c, 0x1c, 0x1c, 0x1c, 0x1c, 0x1c, 0x1c, 0x1c, 0x1c, 0x1c, 0x1c, 
            0x1c, 0x1c, 0x1c, 0x1c, 0x1c, 0x1c, 0x1c, 0x1c, 0x1c, 0x1c, 0x1c, 0x1c, 0x1c, 0x1c, 0x1c, 0x1c, 
            0x1d, 0x1d, 0x1d, 0x1d, 0x1d, 0x1d, 0x1d, 0x1d, 0x1d, 0x1d, 0x1d, 0x1d, 0x1d, 0x1d, 0x1d, 0x1d, 
            0x1d, 0x1d, 0x1d, 0x1d, 0x1d, 0x1d, 0x1d, 0x1d, 0x1d, 0x1d, 0x1d, 0x1d, 0x1d, 0x1d, 0x1d, 0x1d, 
            0x1d, 0x1d, 0x1d, 0x1d, 0x1d, 0x1d, 0x1d, 0x1d, 0x1d, 0x1d, 0x1d, 0x1d, 0x1d, 0x1d, 0x1d, 0x1d, 
            0x1d, 0x1d, 0x1d, 0x1d, 0x1d, 0x1d, 0x1d, 0x1d, 0x1d, 0x1d, 0x1d, 0x1d, 0x1d, 0x1d, 0x1d, 0x1d
         };
        internal static readonly sbyte[] bl_order = new sbyte[] { 
            0x10, 0x11, 0x12, 0, 8, 7, 9, 6, 10, 5, 11, 4, 12, 3, 13, 2, 
            14, 1, 15
         };
        internal const int Buf_size = 0x10;
        internal static readonly int[] DistanceBase = new int[] { 
            0, 1, 2, 3, 4, 6, 8, 12, 0x10, 0x18, 0x20, 0x30, 0x40, 0x60, 0x80, 0xc0, 
            0x100, 0x180, 0x200, 0x300, 0x400, 0x600, 0x800, 0xc00, 0x1000, 0x1800, 0x2000, 0x3000, 0x4000, 0x6000
         };
        internal short[] dyn_tree;
        internal static readonly int[] extra_blbits = new int[] { 
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 
            2, 3, 7
         };
        internal static readonly int[] ExtraDistanceBits = new int[] { 
            0, 0, 0, 0, 1, 1, 2, 2, 3, 3, 4, 4, 5, 5, 6, 6, 
            7, 7, 8, 8, 9, 9, 10, 10, 11, 11, 12, 12, 13, 13
         };
        internal static readonly int[] ExtraLengthBits = new int[] { 
            0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 2, 2, 2, 2, 
            3, 3, 3, 3, 4, 4, 4, 4, 5, 5, 5, 5, 0
         };
        private const int HEAP_SIZE = ((2 * InternalConstants.L_CODES) + 1);
        internal static readonly int[] LengthBase = new int[] { 
            0, 1, 2, 3, 4, 5, 6, 7, 8, 10, 12, 14, 0x10, 20, 0x18, 0x1c, 
            0x20, 40, 0x30, 0x38, 0x40, 80, 0x60, 0x70, 0x80, 160, 0xc0, 0xe0, 0
         };
        internal static readonly sbyte[] LengthCode = new sbyte[] { 
            0, 1, 2, 3, 4, 5, 6, 7, 8, 8, 9, 9, 10, 10, 11, 11, 
            12, 12, 12, 12, 13, 13, 13, 13, 14, 14, 14, 14, 15, 15, 15, 15, 
            0x10, 0x10, 0x10, 0x10, 0x10, 0x10, 0x10, 0x10, 0x11, 0x11, 0x11, 0x11, 0x11, 0x11, 0x11, 0x11, 
            0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x13, 0x13, 0x13, 0x13, 0x13, 0x13, 0x13, 0x13, 
            20, 20, 20, 20, 20, 20, 20, 20, 20, 20, 20, 20, 20, 20, 20, 20, 
            0x15, 0x15, 0x15, 0x15, 0x15, 0x15, 0x15, 0x15, 0x15, 0x15, 0x15, 0x15, 0x15, 0x15, 0x15, 0x15, 
            0x16, 0x16, 0x16, 0x16, 0x16, 0x16, 0x16, 0x16, 0x16, 0x16, 0x16, 0x16, 0x16, 0x16, 0x16, 0x16, 
            0x17, 0x17, 0x17, 0x17, 0x17, 0x17, 0x17, 0x17, 0x17, 0x17, 0x17, 0x17, 0x17, 0x17, 0x17, 0x17, 
            0x18, 0x18, 0x18, 0x18, 0x18, 0x18, 0x18, 0x18, 0x18, 0x18, 0x18, 0x18, 0x18, 0x18, 0x18, 0x18, 
            0x18, 0x18, 0x18, 0x18, 0x18, 0x18, 0x18, 0x18, 0x18, 0x18, 0x18, 0x18, 0x18, 0x18, 0x18, 0x18, 
            0x19, 0x19, 0x19, 0x19, 0x19, 0x19, 0x19, 0x19, 0x19, 0x19, 0x19, 0x19, 0x19, 0x19, 0x19, 0x19, 
            0x19, 0x19, 0x19, 0x19, 0x19, 0x19, 0x19, 0x19, 0x19, 0x19, 0x19, 0x19, 0x19, 0x19, 0x19, 0x19, 
            0x1a, 0x1a, 0x1a, 0x1a, 0x1a, 0x1a, 0x1a, 0x1a, 0x1a, 0x1a, 0x1a, 0x1a, 0x1a, 0x1a, 0x1a, 0x1a, 
            0x1a, 0x1a, 0x1a, 0x1a, 0x1a, 0x1a, 0x1a, 0x1a, 0x1a, 0x1a, 0x1a, 0x1a, 0x1a, 0x1a, 0x1a, 0x1a, 
            0x1b, 0x1b, 0x1b, 0x1b, 0x1b, 0x1b, 0x1b, 0x1b, 0x1b, 0x1b, 0x1b, 0x1b, 0x1b, 0x1b, 0x1b, 0x1b, 
            0x1b, 0x1b, 0x1b, 0x1b, 0x1b, 0x1b, 0x1b, 0x1b, 0x1b, 0x1b, 0x1b, 0x1b, 0x1b, 0x1b, 0x1b, 0x1c
         };
        internal int max_code;
        internal StaticTree staticTree;

        internal static int bi_reverse(int code, int len)
        {
            int num = 0;
            do
            {
                num |= code & 1;
                code = code >> 1;
                num = num << 1;
            }
            while (--len > 0);
            return (num >> 1);
        }

        internal void build_tree(DeflateManager s)
        {
            int num2;
            int num5;
            short[] tree = this.dyn_tree;
            short[] treeCodes = this.staticTree.treeCodes;
            int elems = this.staticTree.elems;
            int num4 = -1;
            s.heapLenField = 0;
            s.heapMaxField = HEAP_SIZE;
            for (num2 = 0; num2 < elems; num2++)
            {
                if (tree[num2 * 2] != 0)
                {
                    s.heapField[++s.heapLenField] = num4 = num2;
                    s.depthField[num2] = 0;
                }
                else
                {
                    tree[(num2 * 2) + 1] = 0;
                }
            }
            while (s.heapLenField < 2)
            {
                num5 = s.heapField[++s.heapLenField] = (num4 < 2) ? ++num4 : 0;
                tree[num5 * 2] = 1;
                s.depthField[num5] = 0;
                s.optLenField--;
                if (treeCodes != null)
                {
                    s.staticLenField -= treeCodes[(num5 * 2) + 1];
                }
            }
            this.max_code = num4;
            num2 = s.heapLenField / 2;
            while (num2 >= 1)
            {
                s.pqdownheap(tree, num2);
                num2--;
            }
            num5 = elems;
            do
            {
                num2 = s.heapField[1];
                s.heapField[1] = s.heapField[s.heapLenField--];
                s.pqdownheap(tree, 1);
                int index = s.heapField[1];
                s.heapField[--s.heapMaxField] = num2;
                s.heapField[--s.heapMaxField] = index;
                tree[num5 * 2] = (short) (tree[num2 * 2] + tree[index * 2]);
                s.depthField[num5] = (sbyte) (Math.Max((byte) s.depthField[num2], (byte) s.depthField[index]) + 1);
                tree[(num2 * 2) + 1] = tree[(index * 2) + 1] = (short) num5;
                s.heapField[1] = num5++;
                s.pqdownheap(tree, 1);
            }
            while (s.heapLenField >= 2);
            s.heapField[--s.heapMaxField] = s.heapField[1];
            this.gen_bitlen(s);
            gen_codes(tree, num4, s.blcountField);
        }

        /// <summary>
        /// Map from a distance to a distance code.
        /// </summary>
        /// <remarks> 
        /// No side effects. _dist_code[256] and _dist_code[257] are never used.
        /// </remarks>
        internal static int DistanceCode(int dist)
        {
            return ((dist < 0x100) ? _dist_code[dist] : _dist_code[0x100 + SharedUtils.URShift(dist, 7)]);
        }

        internal void gen_bitlen(DeflateManager s)
        {
            int num4;
            short[] numArray = this.dyn_tree;
            short[] treeCodes = this.staticTree.treeCodes;
            int[] extraBits = this.staticTree.extraBits;
            int extraBase = this.staticTree.extraBase;
            int maxLength = this.staticTree.maxLength;
            int num9 = 0;
            int index = 0;
            while (index <= InternalConstants.MAX_BITS)
            {
                s.blcountField[index] = 0;
                index++;
            }
            numArray[(s.heapField[s.heapMaxField] * 2) + 1] = 0;
            int num3 = s.heapMaxField + 1;
            while (num3 < HEAP_SIZE)
            {
                num4 = s.heapField[num3];
                index = numArray[(numArray[(num4 * 2) + 1] * 2) + 1] + 1;
                if (index > maxLength)
                {
                    index = maxLength;
                    num9++;
                }
                numArray[(num4 * 2) + 1] = (short) index;
                if (num4 <= this.max_code)
                {
                    s.blcountField[index] = (short) (s.blcountField[index] + 1);
                    int num7 = 0;
                    if (num4 >= extraBase)
                    {
                        num7 = extraBits[num4 - extraBase];
                    }
                    short num8 = numArray[num4 * 2];
                    s.optLenField += num8 * (index + num7);
                    if (treeCodes != null)
                    {
                        s.staticLenField += num8 * (treeCodes[(num4 * 2) + 1] + num7);
                    }
                }
                num3++;
            }
            if (num9 != 0)
            {
                do
                {
                    index = maxLength - 1;
                    while (s.blcountField[index] == 0)
                    {
                        index--;
                    }
                    s.blcountField[index] = (short) (s.blcountField[index] - 1);
                    s.blcountField[index + 1] = (short) (s.blcountField[index + 1] + 2);
                    s.blcountField[maxLength] = (short) (s.blcountField[maxLength] - 1);
                    num9 -= 2;
                }
                while (num9 > 0);
                for (index = maxLength; index != 0; index--)
                {
                    num4 = s.blcountField[index];
                    while (num4 != 0)
                    {
                        int num5 = s.heapField[--num3];
                        if (num5 <= this.max_code)
                        {
                            if (numArray[(num5 * 2) + 1] != index)
                            {
                                s.optLenField += (index - numArray[(num5 * 2) + 1]) * numArray[num5 * 2];
                                numArray[(num5 * 2) + 1] = (short) index;
                            }
                            num4--;
                        }
                    }
                }
            }
        }

        internal static void gen_codes(short[] tree, int max_code, short[] bl_count)
        {
            short[] numArray = new short[InternalConstants.MAX_BITS + 1];
            short num = 0;
            for (int i = 1; i <= InternalConstants.MAX_BITS; i++)
            {
                numArray[i] = num = (short) ((num + bl_count[i - 1]) << 1);
            }
            for (int j = 0; j <= max_code; j++)
            {
                int index = tree[(j * 2) + 1];
                if (index != 0)
                {
                    short num5;
                    numArray[index] = (short) ((num5 = numArray[index]) + 1);
                    tree[j * 2] = (short) bi_reverse(num5, index);
                }
            }
        }
    }
}

