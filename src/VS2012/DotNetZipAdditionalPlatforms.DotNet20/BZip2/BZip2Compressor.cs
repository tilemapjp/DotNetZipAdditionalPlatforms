namespace DotNetZipAdditionalPlatforms.BZip2
{
    using DotNetZipAdditionalPlatforms.Crc;
    using System;
    using System.Globalization;
    using System.Runtime.CompilerServices;

    internal class BZip2Compressor
    {
        private bool blockRandomisedField;
        private int blockSize100kField;
        private BitWriter bitWriterField;
        private const int CLEARMASK = ~SETMASK;
        private readonly CRC32 crcField;
        private CompressionState compressionStateField;
        private int currentByteField;
        private const int DEPTH_THRESH = 10;
        private bool firstAttemptField;
        private const byte GREATER_ICOST = 15;
        /// Knuth's increments seem to work better than Incerpi-Sedgewick here.
        /// Possibly because the number of elems to sort is usually small, typically
        /// &lt;= 20.
        private static readonly int[] incrementsField = new int[] { 1, 4, 13, 40, 0x79, 0x16c, 0x445, 0xcd0, 0x2671, 0x7354, 0x159fd, 0x40df8, 0xc29e9, 0x247dbc };
        private int lastField;
        private const byte LESSER_ICOST = 0;
        private int numberInUseField;
        private int numberMTFField;
        private int originalPointerField;
        private int outBlockFillThresholdField;
        private int runLengthField;
        private int runsField;
        private const int SETMASK = 0x200000;
        private const int SMALL_THRESH = 20;
        private const int WORK_FACTOR = 30;
        private int workDoneField;
        private int workLimitField;

        /// <summary>
        /// BZip2Compressor writes its compressed data out via a BitWriter. This
        /// is necessary because BZip2 does byte shredding.
        /// </summary>
        public BZip2Compressor(BitWriter writer) : this(writer, BZip2.MaxBlockSize)
        {
        }

        public BZip2Compressor(BitWriter writer, int blockSize)
        {
            this.currentByteField = -1;
            this.runLengthField = 0;
            this.crcField = new CRC32(true);
            this.blockSize100kField = blockSize;
            this.bitWriterField = writer;
            this.outBlockFillThresholdField = (blockSize * BZip2.BlockSizeMultiple) - 20;
            this.compressionStateField = new CompressionState(blockSize);
            this.Reset();
        }

        /// <summary>
        /// Append one run to the output block.
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// This compressor does run-length-encoding before BWT and etc. This
        /// method simply appends a run to the output block. The append always
        /// succeeds. The return value indicates whether the block is full:
        /// false (not full) implies that at least one additional run could be
        /// processed.
        /// </para>
        /// </remarks>
        /// <returns>true if the block is now full; otherwise false.</returns>
        private bool AddRunToOutputBlock(bool final)
        {
            this.runsField++;
            int last = this.lastField;
            if (!((last < this.outBlockFillThresholdField) || final))
            {
                throw new Exception(string.Format(CultureInfo.InvariantCulture, "block overrun(final={2}): {0} >= threshold ({1})", last, this.outBlockFillThresholdField, final));
            }
            byte currentByte = (byte) this.currentByteField;
            byte[] block = this.compressionStateField.block;
            this.compressionStateField.inUse[currentByte] = true;
            int runLength = this.runLengthField;
            this.crcField.UpdateCRC(currentByte, runLength);
            switch (runLength)
            {
                case 1:
                    block[last + 2] = currentByte;
                    this.lastField = last + 1;
                    break;

                case 2:
                    block[last + 2] = currentByte;
                    block[last + 3] = currentByte;
                    this.lastField = last + 2;
                    break;

                case 3:
                    block[last + 2] = currentByte;
                    block[last + 3] = currentByte;
                    block[last + 4] = currentByte;
                    this.lastField = last + 3;
                    break;

                default:
                    runLength -= 4;
                    this.compressionStateField.inUse[runLength] = true;
                    block[last + 2] = currentByte;
                    block[last + 3] = currentByte;
                    block[last + 4] = currentByte;
                    block[last + 5] = currentByte;
                    block[last + 6] = (byte) runLength;
                    this.lastField = last + 5;
                    break;
            }
            return (this.lastField >= this.outBlockFillThresholdField);
        }

        private void blockSort()
        {
            this.workLimitField = WORK_FACTOR * this.lastField;
            this.workDoneField = 0;
            this.blockRandomisedField = false;
            this.firstAttemptField = true;
            this.mainSort();
            if (this.firstAttemptField && (this.workDoneField > this.workLimitField))
            {
                this.randomiseBlock();
                this.workLimitField = this.workDoneField = 0;
                this.firstAttemptField = false;
                this.mainSort();
            }
            int[] fmap = this.compressionStateField.fmap;
            this.originalPointerField = -1;
            int index = 0;
            int last = this.lastField;
            while (index <= last)
            {
                if (fmap[index] == 0)
                {
                    this.originalPointerField = index;
                    break;
                }
                index++;
            }
        }

        /// <summary>
        /// Compress the data that has been placed (Run-length-encoded) into the
        /// block. The compressed data goes into the CompressedBytes array.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Side effects: 1.  fills the CompressedBytes array.  2. sets the
        /// AvailableBytesOut property.
        /// </para>
        /// </remarks>
        public void CompressAndWrite()
        {
            if (this.runLengthField > 0)
            {
                this.AddRunToOutputBlock(true);
            }
            this.currentByteField = -1;
            if (this.lastField != -1)
            {
                this.blockSort();
                this.bitWriterField.WriteByte(0x31);
                this.bitWriterField.WriteByte(0x41);
                this.bitWriterField.WriteByte(0x59);
                this.bitWriterField.WriteByte(0x26);
                this.bitWriterField.WriteByte(0x53);
                this.bitWriterField.WriteByte(0x59);
                this.Crc32 = (uint) this.crcField.Crc32Result;
                this.bitWriterField.WriteInt(this.Crc32);
                this.bitWriterField.WriteBits(1, this.blockRandomisedField ? (uint)1 : (uint)0);
                this.moveToFrontCodeAndSend();
                this.Reset();
            }
        }

        /// <summary>
        /// Accept new bytes into the compressor data buffer
        /// </summary>
        /// <remarks>
        /// <para>
        /// This method does the first-level (cheap) run-length encoding, and
        /// stores the encoded data into the rle block.
        /// </para>
        /// </remarks>
        public int Fill(byte[] buffer, int offset, int count)
        {
            int num3;
            if (this.lastField >= this.outBlockFillThresholdField)
            {
                return 0;
            }
            int num = 0;
            int num2 = offset + count;
            do
            {
                num3 = this.write0(buffer[offset++]);
                if (num3 > 0)
                {
                    num++;
                }
            }
            while ((offset < num2) && (num3 == 1));
            return num;
        }

        private void generateMTFValues()
        {
            int num3;
            int last = this.lastField;
            CompressionState cstate = this.compressionStateField;
            bool[] inUse = cstate.inUse;
            byte[] block = cstate.block;
            int[] fmap = cstate.fmap;
            char[] sfmap = cstate.sfmap;
            int[] mtfFreq = cstate.mtfFreq;
            byte[] unseqToSeq = cstate.unseqToSeq;
            byte[] buffer3 = cstate.generateMTFValues_yy;
            int num2 = 0;
            for (num3 = 0; num3 < 0x100; num3++)
            {
                if (inUse[num3])
                {
                    unseqToSeq[num3] = (byte) num2;
                    num2++;
                }
            }
            this.numberInUseField = num2;
            int index = num2 + 1;
            for (num3 = index; num3 >= 0; num3--)
            {
                mtfFreq[num3] = 0;
            }
            num3 = num2;
            while (--num3 >= 0)
            {
                buffer3[num3] = (byte) num3;
            }
            int num5 = 0;
            int num6 = 0;
            for (num3 = 0; num3 <= last; num3++)
            {
                byte num7 = unseqToSeq[block[fmap[num3]] & 0xff];
                byte num8 = buffer3[0];
                int num9 = 0;
                while (num7 != num8)
                {
                    num9++;
                    byte num10 = num8;
                    num8 = buffer3[num9];
                    buffer3[num9] = num10;
                }
                buffer3[0] = num8;
                if (num9 == 0)
                {
                    num6++;
                    continue;
                }
                if (num6 <= 0)
                {
                    goto Label_01F8;
                }
                num6--;
                goto Label_01EC;
            Label_0168:
                if ((num6 & 1) == 0)
                {
                    sfmap[num5] = BZip2.RUNA;
                    num5++;
                    mtfFreq[BZip2.RUNA]++;
                }
                else
                {
                    sfmap[num5] = BZip2.RUNB;
                    num5++;
                    mtfFreq[BZip2.RUNB]++;
                }
                if (num6 >= 2)
                {
                    num6 = (num6 - 2) >> 1;
                }
                else
                {
                    goto Label_01F4;
                }
            Label_01EC:
                goto Label_0168;
            Label_01F4:
                num6 = 0;
            Label_01F8:
                sfmap[num5] = (char) (num9 + 1);
                num5++;
                mtfFreq[num9 + 1]++;
            }
            if (num6 > 0)
            {
                num6--;
                while (true)
                {
                    if ((num6 & 1) == 0)
                    {
                        sfmap[num5] = BZip2.RUNA;
                        num5++;
                        mtfFreq[BZip2.RUNA]++;
                    }
                    else
                    {
                        sfmap[num5] = BZip2.RUNB;
                        num5++;
                        mtfFreq[BZip2.RUNB]++;
                    }
                    if (num6 >= 2)
                    {
                        num6 = (num6 - 2) >> 1;
                    }
                    else
                    {
                        break;
                    }
                }
            }
            sfmap[num5] = (char) index;
            mtfFreq[index]++;
            this.numberMTFField = num5 + 1;
        }

        private static void hbAssignCodes(int[] code, byte[] length, int minLen, int maxLen, int alphaSize)
        {
            int num = 0;
            for (int i = minLen; i <= maxLen; i++)
            {
                for (int j = 0; j < alphaSize; j++)
                {
                    if ((length[j] & 0xff) == i)
                    {
                        code[j] = num;
                        num++;
                    }
                }
                num = num << 1;
            }
        }

        private static void hbMakeCodeLengths(byte[] len, int[] freq, CompressionState state1, int alphaSize, int maxLen)
        {
            int[] heap = state1.heap;
            int[] weight = state1.weight;
            int[] parent = state1.parent;
            int index = alphaSize;
            while (--index >= 0)
            {
#pragma warning disable 472
                weight[index + 1] = ((freq[index] == null) ? 1 : freq[index]) << 8;
#pragma warning restore 472
            }
            bool flag = true;
            while (flag)
            {
                int num4;
                int num5;
                int num12;
                flag = false;
                int num2 = alphaSize;
                int num3 = 0;
                heap[0] = 0;
                weight[0] = 0;
                parent[0] = -2;
                index = 1;
                while (index <= alphaSize)
                {
                    parent[index] = -1;
                    num3++;
                    heap[num3] = index;
                    num4 = num3;
                    num5 = heap[num4];
                    while (weight[num5] < weight[heap[num4 >> 1]])
                    {
                        heap[num4] = heap[num4 >> 1];
                        num4 = num4 >> 1;
                    }
                    heap[num4] = num5;
                    index++;
                }
                while (num3 > 1)
                {
                    int num6 = heap[1];
                    heap[1] = heap[num3];
                    num3--;
                    int num7 = 0;
                    num4 = 1;
                    num5 = heap[1];
                    goto Label_0149;
                Label_00E1:
                    num7 = num4 << 1;
                    if (num7 > num3)
                    {
                        goto Label_014E;
                    }
                    if ((num7 < num3) && (weight[heap[num7 + 1]] < weight[heap[num7]]))
                    {
                        num7++;
                    }
                    if (weight[num5] < weight[heap[num7]])
                    {
                        goto Label_014E;
                    }
                    heap[num4] = heap[num7];
                    num4 = num7;
                Label_0149:
                    goto Label_00E1;
                Label_014E:
                    heap[num4] = num5;
                    int num8 = heap[1];
                    heap[1] = heap[num3];
                    num3--;
                    num7 = 0;
                    num4 = 1;
                    num5 = heap[1];
                    goto Label_01DB;
                Label_0173:
                    num7 = num4 << 1;
                    if (num7 > num3)
                    {
                        goto Label_01E0;
                    }
                    if ((num7 < num3) && (weight[heap[num7 + 1]] < weight[heap[num7]]))
                    {
                        num7++;
                    }
                    if (weight[num5] < weight[heap[num7]])
                    {
                        goto Label_01E0;
                    }
                    heap[num4] = heap[num7];
                    num4 = num7;
                Label_01DB:
                    goto Label_0173;
                Label_01E0:
                    heap[num4] = num5;
                    num2++;
                    parent[num6] = parent[num8] = num2;
                    int num9 = weight[num6];
                    int num10 = weight[num8];
                    weight[num2] = ((num9 & -256) + (num10 & -256)) | (1 + (((num9 & 0xff) > (num10 & 0xff)) ? (num9 & 0xff) : (num10 & 0xff)));
                    parent[num2] = -1;
                    num3++;
                    heap[num3] = num2;
                    num5 = 0;
                    num4 = num3;
                    num5 = heap[num4];
                    int num11 = weight[num5];
                    while (num11 < weight[heap[num4 >> 1]])
                    {
                        heap[num4] = heap[num4 >> 1];
                        num4 = num4 >> 1;
                    }
                    heap[num4] = num5;
                }
                index = 1;
                while (index <= alphaSize)
                {
                    int num14;
                    num12 = 0;
                    int num13 = index;
                    while ((num14 = parent[num13]) >= 0)
                    {
                        num13 = num14;
                        num12++;
                    }
                    len[index - 1] = (byte) num12;
                    if (num12 > maxLen)
                    {
                        flag = true;
                    }
                    index++;
                }
                if (flag)
                {
                    for (index = 1; index < alphaSize; index++)
                    {
                        num12 = weight[index] >> 8;
                        num12 = 1 + (num12 >> 1);
                        weight[index] = num12 << 8;
                    }
                }
            }
        }

        /// Method "mainQSort3", file "blocksort.c", BZip2 1.0.2
        private void mainQSort3(CompressionState dataShadow, int loSt, int hiSt, int dSt)
        {
            int[] numArray = dataShadow.stack_ll;
            int[] numArray2 = dataShadow.stack_hh;
            int[] numArray3 = dataShadow.stack_dd;
            int[] fmap = dataShadow.fmap;
            byte[] block = dataShadow.block;
            numArray[0] = loSt;
            numArray2[0] = hiSt;
            numArray3[0] = dSt;
            int index = 1;
            while (--index >= 0)
            {
                int num11;
                int num12;
                int lo = numArray[index];
                int hi = numArray2[index];
                int d = numArray3[index];
                if (((hi - lo) < SMALL_THRESH) || (d > DEPTH_THRESH))
                {
                    if (this.mainSimpleSort(dataShadow, lo, hi, d))
                    {
                        break;
                    }
                    continue;
                }
                int num5 = d + 1;
                int num6 = med3(block[fmap[lo] + num5], block[fmap[hi] + num5], block[fmap[(lo + hi) >> 1] + num5]) & 0xff;
                int num7 = lo;
                int num8 = hi;
                int num9 = lo;
                int num10 = hi;
                goto Label_01F6;
            Label_00E0:
                num11 = (block[fmap[num7] + num5] & 0xff) - num6;
                if (num11 == 0)
                {
                    num12 = fmap[num7];
                    fmap[num7++] = fmap[num9];
                    fmap[num9++] = num12;
                }
                else if (num11 < 0)
                {
                    num7++;
                }
                else
                {
                    goto Label_01B5;
                }
            Label_0142:
                if (num7 <= num8)
                {
                    goto Label_00E0;
                }
            Label_01B5:
                while (num7 <= num8)
                {
                    num11 = (block[fmap[num8] + num5] & 0xff) - num6;
                    if (num11 == 0)
                    {
                        num12 = fmap[num8];
                        fmap[num8--] = fmap[num10];
                        fmap[num10--] = num12;
                    }
                    else
                    {
                        if (num11 > 0)
                        {
                            num8--;
                            continue;
                        }
                        break;
                    }
                }
                if (num7 <= num8)
                {
                    num12 = fmap[num7];
                    fmap[num7++] = fmap[num8];
                    fmap[num8--] = num12;
                }
                else
                {
                    goto Label_01FE;
                }
            Label_01F6:
                goto Label_0142;
            Label_01FE:
                if (num10 < num9)
                {
                    numArray[index] = lo;
                    numArray2[index] = hi;
                    numArray3[index] = num5;
                    index++;
                }
                else
                {
                    num11 = ((num9 - lo) < (num7 - num9)) ? (num9 - lo) : (num7 - num9);
                    vswap(fmap, lo, num7 - num11, num11);
                    int n = ((hi - num10) < (num10 - num8)) ? (hi - num10) : (num10 - num8);
                    vswap(fmap, num7, (hi - n) + 1, n);
                    num11 = ((lo + num7) - num9) - 1;
                    n = (hi - (num10 - num8)) + 1;
                    numArray[index] = lo;
                    numArray2[index] = num11;
                    numArray3[index] = d;
                    index++;
                    numArray[index] = num11 + 1;
                    numArray2[index] = n - 1;
                    numArray3[index] = num5;
                    index++;
                    numArray[index] = n;
                    numArray2[index] = hi;
                    numArray3[index] = d;
                    index++;
                }
            }
        }

        /// This is the most hammered method of this class.
        /// 
        /// <p>
        /// This is the version using unrolled loops.
        /// </p>
        private bool mainSimpleSort(CompressionState dataShadow, int lo, int hi, int d)
        {
            int num = (hi - lo) + 1;
            if (num < 2)
            {
                return (this.firstAttemptField && (this.workDoneField > this.workLimitField));
            }
            int index = 0;
            while (incrementsField[index] < num)
            {
                index++;
            }
            int[] fmap = dataShadow.fmap;
            char[] quadrant = dataShadow.quadrant;
            byte[] block = dataShadow.block;
            int last = this.lastField;
            int num4 = last + 1;
            bool firstAttempt = this.firstAttemptField;
            int workLimit = this.workLimitField;
            int workDone = this.workDoneField;
            while (--index >= 0)
            {
                int num7 = incrementsField[index];
                int num8 = (lo + num7) - 1;
                int num9 = lo + num7;
                while (num9 <= hi)
                {
                    int num10 = 3;
                    while ((num9 <= hi) && (--num10 >= 0))
                    {
                        int num11 = fmap[num9];
                        int num12 = num11 + d;
                        int num13 = num9;
                        bool flag2 = false;
                        int num14 = 0;
                        goto Label_056A;
                    Label_00D1:
                        if (flag2)
                        {
                            fmap[num13] = num14;
                            if ((num13 -= num7) <= num8)
                            {
                                goto Label_0572;
                            }
                        }
                        else
                        {
                            flag2 = true;
                        }
                        num14 = fmap[num13 - num7];
                        int num15 = num14 + d;
                        int num16 = num12;
                        if (block[num15 + 1] == block[num16 + 1])
                        {
                            if (block[num15 + 2] == block[num16 + 2])
                            {
                                if (block[num15 + 3] == block[num16 + 3])
                                {
                                    if (block[num15 + 4] == block[num16 + 4])
                                    {
                                        if (block[num15 + 5] == block[num16 + 5])
                                        {
                                            if (block[num15 += 6] == block[num16 += 6])
                                            {
                                                int num17 = last;
                                                while (num17 > 0)
                                                {
                                                    num17 -= 4;
                                                    if (block[num15 + 1] == block[num16 + 1])
                                                    {
                                                        if (quadrant[num15] == quadrant[num16])
                                                        {
                                                            if (block[num15 + 2] == block[num16 + 2])
                                                            {
                                                                if (quadrant[num15 + 1] == quadrant[num16 + 1])
                                                                {
                                                                    if (block[num15 + 3] == block[num16 + 3])
                                                                    {
                                                                        if (quadrant[num15 + 2] == quadrant[num16 + 2])
                                                                        {
                                                                            if (block[num15 + 4] == block[num16 + 4])
                                                                            {
                                                                                if (quadrant[num15 + 3] == quadrant[num16 + 3])
                                                                                {
                                                                                    if ((num15 += 4) >= num4)
                                                                                    {
                                                                                        num15 -= num4;
                                                                                    }
                                                                                    if ((num16 += 4) >= num4)
                                                                                    {
                                                                                        num16 -= num4;
                                                                                    }
                                                                                    workDone++;
                                                                                    continue;
                                                                                }
                                                                                if (quadrant[num15 + 3] > quadrant[num16 + 3])
                                                                                {
                                                                                    goto Label_056A;
                                                                                }
                                                                            }
                                                                            else if ((block[num15 + 4] & 0xff) > (block[num16 + 4] & 0xff))
                                                                            {
                                                                                goto Label_056A;
                                                                            }
                                                                        }
                                                                        else if (quadrant[num15 + 2] > quadrant[num16 + 2])
                                                                        {
                                                                            goto Label_056A;
                                                                        }
                                                                    }
                                                                    else if ((block[num15 + 3] & 0xff) > (block[num16 + 3] & 0xff))
                                                                    {
                                                                        goto Label_056A;
                                                                    }
                                                                }
                                                                else if (quadrant[num15 + 1] > quadrant[num16 + 1])
                                                                {
                                                                    goto Label_056A;
                                                                }
                                                            }
                                                            else if ((block[num15 + 2] & 0xff) > (block[num16 + 2] & 0xff))
                                                            {
                                                                goto Label_056A;
                                                            }
                                                        }
                                                        else if (quadrant[num15] > quadrant[num16])
                                                        {
                                                            goto Label_056A;
                                                        }
                                                    }
                                                    else if ((block[num15 + 1] & 0xff) > (block[num16 + 1] & 0xff))
                                                    {
                                                        goto Label_056A;
                                                    }
                                                    break;
                                                }
                                            }
                                            else if ((block[num15] & 0xff) > (block[num16] & 0xff))
                                            {
                                                goto Label_056A;
                                            }
                                        }
                                        else if ((block[num15 + 5] & 0xff) > (block[num16 + 5] & 0xff))
                                        {
                                            goto Label_056A;
                                        }
                                    }
                                    else if ((block[num15 + 4] & 0xff) > (block[num16 + 4] & 0xff))
                                    {
                                        goto Label_056A;
                                    }
                                }
                                else if ((block[num15 + 3] & 0xff) > (block[num16 + 3] & 0xff))
                                {
                                    goto Label_056A;
                                }
                            }
                            else if ((block[num15 + 2] & 0xff) > (block[num16 + 2] & 0xff))
                            {
                                goto Label_056A;
                            }
                            goto Label_0572;
                        }
                        if ((block[num15 + 1] & 0xff) <= (block[num16 + 1] & 0xff))
                        {
                            goto Label_0572;
                        }
                    Label_056A:
                        goto Label_00D1;
                    Label_0572:
                        fmap[num13] = num11;
                        num9++;
                    }
                    if ((firstAttempt && (num9 <= hi)) && (workDone > workLimit))
                    {
                        break;
                    }
                }
            }
            this.workDoneField = workDone;
            return (firstAttempt && (workDone > workLimit));
        }

        private void mainSort()
        {
            int num5;
            int num10;
            int num23;
            CompressionState cstate = this.compressionStateField;
            int[] numArray = cstate.mainSort_runningOrder;
            int[] numArray2 = cstate.mainSort_copy;
            bool[] flagArray = cstate.mainSort_bigDone;
            int[] ftab = cstate.ftab;
            byte[] block = cstate.block;
            int[] fmap = cstate.fmap;
            char[] quadrant = cstate.quadrant;
            int last = this.lastField;
            int workLimit = this.workLimitField;
            bool firstAttempt = this.firstAttemptField;
            int index = 0x10001;
            while (--index >= 0)
            {
                ftab[index] = 0;
            }
            for (index = 0; index < BZip2.NUM_OVERSHOOT_BYTES; index++)
            {
                block[(last + index) + 2] = block[(index % (last + 1)) + 1];
            }
            index = (last + BZip2.NUM_OVERSHOOT_BYTES) + 1;
            while (--index >= 0)
            {
                quadrant[index] = '\0';
            }
            block[0] = block[last + 1];
            int num4 = block[0] & 0xff;
            for (index = 0; index <= last; index++)
            {
                num5 = block[index + 1] & 0xff;
                ftab[(num4 << 8) + num5]++;
                num4 = num5;
            }
            for (index = 1; index <= 0x10000; index++)
            {
                ftab[index] += ftab[index - 1];
            }
            num4 = block[1] & 0xff;
            for (index = 0; index < last; index++)
            {
                num5 = block[index + 2] & 0xff;
                ftab[(num4 << 8) + num5] = num23 = ftab[(num4 << 8) + num5] - 1;
                fmap[num23] = index;
                num4 = num5;
            }
            ftab[((block[last + 1] & 0xff) << 8) + (block[1] & 0xff)] = num23 = ftab[((block[last + 1] & 0xff) << 8) + (block[1] & 0xff)] - 1;
            fmap[num23] = last;
            index = 0x100;
            while (--index >= 0)
            {
                flagArray[index] = false;
                numArray[index] = index;
            }
            int num6 = 0x16c;
            while (num6 != 1)
            {
                num6 /= 3;
                index = num6;
                while (index <= 0xff)
                {
                    int num7 = numArray[index];
                    int num8 = ftab[(num7 + 1) << 8] - ftab[num7 << 8];
                    int num9 = num6 - 1;
                    num10 = index;
                    for (int i = numArray[num10 - num6]; (ftab[(i + 1) << 8] - ftab[i << 8]) > num8; i = numArray[num10 - num6])
                    {
                        numArray[num10] = i;
                        num10 -= num6;
                        if (num10 <= num9)
                        {
                            break;
                        }
                    }
                    numArray[num10] = num7;
                    index++;
                }
            }
            for (index = 0; index <= 0xff; index++)
            {
                int num12 = numArray[index];
                num10 = 0;
                while (num10 <= 0xff)
                {
                    int num13 = (num12 << 8) + num10;
                    int num14 = ftab[num13];
                    if ((num14 & SETMASK) != SETMASK)
                    {
                        int loSt = num14 & CLEARMASK;
                        int hiSt = (ftab[num13 + 1] & CLEARMASK) - 1;
                        if (hiSt > loSt)
                        {
                            this.mainQSort3(cstate, loSt, hiSt, 2);
                            if (firstAttempt && (this.workDoneField > workLimit))
                            {
                                break;
                            }
                        }
                        ftab[num13] = num14 | SETMASK;
                    }
                    num10++;
                }
                num10 = 0;
                while (num10 <= 0xff)
                {
                    numArray2[num10] = ftab[(num10 << 8) + num12] & CLEARMASK;
                    num10++;
                }
                num10 = ftab[num12 << 8] & CLEARMASK;
                int num17 = ftab[(num12 + 1) << 8] & CLEARMASK;
                while (num10 < num17)
                {
                    int num18 = fmap[num10];
                    num4 = block[num18] & 0xff;
                    if (!flagArray[num4])
                    {
                        fmap[numArray2[num4]] = (num18 == 0) ? last : (num18 - 1);
                        numArray2[num4]++;
                    }
                    num10++;
                }
                num10 = 0x100;
                while (--num10 >= 0)
                {
                    ftab[(num10 << 8) + num12] |= SETMASK;
                }
                flagArray[num12] = true;
                if (index < 0xff)
                {
                    int num19 = ftab[num12 << 8] & CLEARMASK;
                    int num20 = (ftab[(num12 + 1) << 8] & CLEARMASK) - num19;
                    int num21 = 0;
                    while ((num20 >> num21) > 0xfffe)
                    {
                        num21++;
                    }
                    for (num10 = 0; num10 < num20; num10++)
                    {
                        int num22 = fmap[num19 + num10];
                        char ch = (char) (num10 >> num21);
                        quadrant[num22] = ch;
                        if (num22 < BZip2.NUM_OVERSHOOT_BYTES)
                        {
                            quadrant[(num22 + last) + 1] = ch;
                        }
                    }
                }
            }
        }

        private static byte med3(byte a, byte b, byte c)
        {
            return ((a < b) ? ((b < c) ? b : ((a < c) ? c : a)) : ((b > c) ? b : ((a > c) ? c : a)));
        }

        private void moveToFrontCodeAndSend()
        {
            this.bitWriterField.WriteBits(0x18, (uint) this.originalPointerField);
            this.generateMTFValues();
            this.sendMTFValues();
        }

        private void randomiseBlock()
        {
            bool[] inUse = this.compressionStateField.inUse;
            byte[] block = this.compressionStateField.block;
            int last = this.lastField;
            int index = 0x100;
            while (--index >= 0)
            {
                inUse[index] = false;
            }
            int num3 = 0;
            int i = 0;
            index = 0;
            for (int j = 1; index <= last; j++)
            {
                if (num3 == 0)
                {
                    num3 = (ushort) Rand.Rnums(i);
                    if (++i == 0x200)
                    {
                        i = 0;
                    }
                }
                num3--;
                block[j] = (byte) (block[j] ^ ((num3 == 1) ? ((byte) 1) : ((byte) 0)));
                inUse[block[j] & 0xff] = true;
                index = j;
            }
            this.blockRandomisedField = true;
        }

        private void Reset()
        {
            this.crcField.Reset();
            this.currentByteField = -1;
            this.runLengthField = 0;
            this.lastField = -1;
            int index = 0x100;
            while (--index >= 0)
            {
                this.compressionStateField.inUse[index] = false;
            }
        }

        private void sendMTFValues()
        {
            byte[][] bufferArray = this.compressionStateField.sendMTFValues_len;
            int alphaSize = this.numberInUseField + 2;
            int nGroups = BZip2.NGroups;
            while (--nGroups >= 0)
            {
                byte[] buffer = bufferArray[nGroups];
                int index = alphaSize;
                while (--index >= 0)
                {
                    buffer[index] = GREATER_ICOST;
                }
            }
            int num4 = (this.numberMTFField < 200) ? 2 : ((this.numberMTFField < 600) ? 3 : ((this.numberMTFField < 0x4b0) ? 4 : ((this.numberMTFField < 0x960) ? 5 : 6)));
            this.sendMTFValues0(num4, alphaSize);
            int nSelectors = this.sendMTFValues1(num4, alphaSize);
            this.sendMTFValues2(num4, nSelectors);
            this.sendMTFValues3(num4, alphaSize);
            this.sendMTFValues4();
            this.sendMTFValues5(num4, nSelectors);
            this.sendMTFValues6(num4, alphaSize);
            this.sendMTFValues7(nSelectors);
        }

        private void sendMTFValues0(int nGroups, int alphaSize)
        {
            byte[][] bufferArray = this.compressionStateField.sendMTFValues_len;
            int[] mtfFreq = this.compressionStateField.mtfFreq;
            int nMTF = this.numberMTFField;
            int num2 = 0;
            for (int i = nGroups; i > 0; i--)
            {
                int num4 = nMTF / i;
                int num5 = num2 - 1;
                int num6 = 0;
                int num7 = alphaSize - 1;
                while ((num6 < num4) && (num5 < num7))
                {
                    num6 += mtfFreq[++num5];
                }
                if ((((num5 > num2) && (i != nGroups)) && (i != 1)) && (((nGroups - i) & 1) != 0))
                {
                    num6 -= mtfFreq[num5--];
                }
                byte[] buffer = bufferArray[i - 1];
                int index = alphaSize;
                while (--index >= 0)
                {
                    if ((index >= num2) && (index <= num5))
                    {
                        buffer[index] = LESSER_ICOST;
                    }
                    else
                    {
                        buffer[index] = GREATER_ICOST;
                    }
                }
                num2 = num5 + 1;
                nMTF -= num6;
            }
        }

        private int sendMTFValues1(int nGroups, int alphaSize)
        {
            CompressionState cstate = this.compressionStateField;
            int[][] numArray = cstate.sendMTFValues_rfreq;
            int[] numArray2 = cstate.sendMTFValues_fave;
            short[] numArray3 = cstate.sendMTFValues_cost;
            char[] sfmap = cstate.sfmap;
            byte[] selector = cstate.selector;
            byte[][] bufferArray = cstate.sendMTFValues_len;
            byte[] buffer2 = bufferArray[0];
            byte[] buffer3 = bufferArray[1];
            byte[] buffer4 = bufferArray[2];
            byte[] buffer5 = bufferArray[3];
            byte[] buffer6 = bufferArray[4];
            byte[] buffer7 = bufferArray[5];
            int nMTF = this.numberMTFField;
            int index = 0;
            for (int i = 0; i < BZip2.N_ITERS; i++)
            {
                int num5;
                int num7;
                int num4 = nGroups;
                while (--num4 >= 0)
                {
                    numArray2[num4] = 0;
                    int[] numArray4 = numArray[num4];
                    num5 = alphaSize;
                    while (--num5 >= 0)
                    {
                        numArray4[num5] = 0;
                    }
                }
                index = 0;
                for (int j = 0; j < this.numberMTFField; j = num7 + 1)
                {
                    int num8;
                    num7 = Math.Min((int) ((j + BZip2.G_SIZE) - 1), (int) (nMTF - 1));
                    if (nGroups == BZip2.NGroups)
                    {
                        int[] numArray5 = new int[6];
                        num5 = j;
                        while (num5 <= num7)
                        {
                            num8 = sfmap[num5];
                            numArray5[0] += buffer2[num8] & 0xff;
                            numArray5[1] += buffer3[num8] & 0xff;
                            numArray5[2] += buffer4[num8] & 0xff;
                            numArray5[3] += buffer5[num8] & 0xff;
                            numArray5[4] += buffer6[num8] & 0xff;
                            numArray5[5] += buffer7[num8] & 0xff;
                            num5++;
                        }
                        numArray3[0] = (short) numArray5[0];
                        numArray3[1] = (short) numArray5[1];
                        numArray3[2] = (short) numArray5[2];
                        numArray3[3] = (short) numArray5[3];
                        numArray3[4] = (short) numArray5[4];
                        numArray3[5] = (short) numArray5[5];
                    }
                    else
                    {
                        num4 = nGroups;
                        while (--num4 >= 0)
                        {
                            numArray3[num4] = 0;
                        }
                        num5 = j;
                        while (num5 <= num7)
                        {
                            num8 = sfmap[num5];
                            num4 = nGroups;
                            while (--num4 >= 0)
                            {
                                numArray3[num4] = (short) (numArray3[num4] + ((short) (bufferArray[num4][num8] & 0xff)));
                            }
                            num5++;
                        }
                    }
                    int num9 = -1;
                    num4 = nGroups;
                    int num10 = 0x3b9ac9ff;
                    while (--num4 >= 0)
                    {
                        int num11 = numArray3[num4];
                        if (num11 < num10)
                        {
                            num10 = num11;
                            num9 = num4;
                        }
                    }
                    numArray2[num9]++;
                    selector[index] = (byte) num9;
                    index++;
                    int[] numArray6 = numArray[num9];
                    for (num5 = j; num5 <= num7; num5++)
                    {
                        numArray6[sfmap[num5]]++;
                    }
                }
                for (num4 = 0; num4 < nGroups; num4++)
                {
                    hbMakeCodeLengths(bufferArray[num4], numArray[num4], this.compressionStateField, alphaSize, 20);
                }
            }
            return index;
        }

        private void sendMTFValues2(int nGroups, int nSelectors)
        {
            CompressionState cstate = this.compressionStateField;
            byte[] buffer = cstate.sendMTFValues2_pos;
            int index = nGroups;
            while (--index >= 0)
            {
                buffer[index] = (byte) index;
            }
            for (index = 0; index < nSelectors; index++)
            {
                byte num2 = cstate.selector[index];
                byte num3 = buffer[0];
                int num4 = 0;
                while (num2 != num3)
                {
                    num4++;
                    byte num5 = num3;
                    num3 = buffer[num4];
                    buffer[num4] = num5;
                }
                buffer[0] = num3;
                cstate.selectorMtf[index] = (byte) num4;
            }
        }

        private void sendMTFValues3(int nGroups, int alphaSize)
        {
            int[][] numArray = this.compressionStateField.sendMTFValues_code;
            byte[][] bufferArray = this.compressionStateField.sendMTFValues_len;
            for (int i = 0; i < nGroups; i++)
            {
                int minLen = 0x20;
                int maxLen = 0;
                byte[] buffer = bufferArray[i];
                int index = alphaSize;
                while (--index >= 0)
                {
                    int num5 = buffer[index] & 0xff;
                    if (num5 > maxLen)
                    {
                        maxLen = num5;
                    }
                    if (num5 < minLen)
                    {
                        minLen = num5;
                    }
                }
                hbAssignCodes(numArray[i], bufferArray[i], minLen, maxLen, alphaSize);
            }
        }

        private void sendMTFValues4()
        {
            int num2;
            int num3;
            bool[] inUse = this.compressionStateField.inUse;
            bool[] flagArray2 = this.compressionStateField.sentMTFValues4_inUse16;
            int index = 0x10;
            while (--index >= 0)
            {
                flagArray2[index] = false;
                num2 = index * 0x10;
                num3 = 0x10;
                while (--num3 >= 0)
                {
                    if (inUse[num2 + num3])
                    {
                        flagArray2[index] = true;
                    }
                }
            }
            uint num4 = 0;
            for (index = 0; index < 0x10; index++)
            {
                if (flagArray2[index])
                {
                    num4 |= ((uint) 1) << ((0x10 - index) - 1);
                }
            }
            this.bitWriterField.WriteBits(0x10, num4);
            for (index = 0; index < 0x10; index++)
            {
                if (flagArray2[index])
                {
                    num2 = index * 0x10;
                    num4 = 0;
                    for (num3 = 0; num3 < 0x10; num3++)
                    {
                        if (inUse[num2 + num3])
                        {
                            num4 |= ((uint) 1) << ((0x10 - num3) - 1);
                        }
                    }
                    this.bitWriterField.WriteBits(0x10, num4);
                }
            }
        }

        private void sendMTFValues5(int nGroups, int nSelectors)
        {
            this.bitWriterField.WriteBits(3, (uint) nGroups);
            this.bitWriterField.WriteBits(15, (uint) nSelectors);
            byte[] selectorMtf = this.compressionStateField.selectorMtf;
            for (int i = 0; i < nSelectors; i++)
            {
                int num2 = 0;
                int num3 = selectorMtf[i] & 0xff;
                while (num2 < num3)
                {
                    this.bitWriterField.WriteBits(1, 1);
                    num2++;
                }
                this.bitWriterField.WriteBits(1, 0);
            }
        }

        private void sendMTFValues6(int nGroups, int alphaSize)
        {
            byte[][] bufferArray = this.compressionStateField.sendMTFValues_len;
            for (int i = 0; i < nGroups; i++)
            {
                byte[] buffer = bufferArray[i];
                uint num2 = (uint) (buffer[0] & 0xff);
                this.bitWriterField.WriteBits(5, num2);
                for (int j = 0; j < alphaSize; j++)
                {
                    int num4 = buffer[j] & 0xff;
                    while (num2 < num4)
                    {
                        this.bitWriterField.WriteBits(2, 2);
                        num2++;
                    }
                    while (num2 > num4)
                    {
                        this.bitWriterField.WriteBits(2, 3);
                        num2--;
                    }
                    this.bitWriterField.WriteBits(1, 0);
                }
            }
        }

        private void sendMTFValues7(int nSelectors)
        {
            byte[][] bufferArray = this.compressionStateField.sendMTFValues_len;
            int[][] numArray = this.compressionStateField.sendMTFValues_code;
            byte[] selector = this.compressionStateField.selector;
            char[] sfmap = this.compressionStateField.sfmap;
            int nMTF = this.numberMTFField;
            int index = 0;
            int num3 = 0;
            while (num3 < nMTF)
            {
                int num4 = Math.Min((int) ((num3 + BZip2.G_SIZE) - 1), (int) (nMTF - 1));
                int num5 = selector[index] & 0xff;
                int[] numArray2 = numArray[num5];
                byte[] buffer2 = bufferArray[num5];
                while (num3 <= num4)
                {
                    int num6 = sfmap[num3];
                    int nbits = buffer2[num6] & 0xff;
                    this.bitWriterField.WriteBits(nbits, (uint) numArray2[num6]);
                    num3++;
                }
                num3 = num4 + 1;
                index++;
            }
        }

        private static void vswap(int[] fmap, int p1, int p2, int n)
        {
            n += p1;
            while (p1 < n)
            {
                int num = fmap[p1];
                fmap[p1++] = fmap[p2];
                fmap[p2++] = num;
            }
        }

        /// <summary>
        /// Process one input byte into the block.
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// To "process" the byte means to do the run-length encoding.
        /// There are 3 possible return values:
        /// 
        /// 0 - the byte was not written, in other words, not
        /// encoded into the block. This happens when the
        /// byte b would require the start of a new run, and
        /// the block has no more room for new runs.
        /// 
        /// 1 - the byte was written, and the block is not full.
        /// 
        /// 2 - the byte was written, and the block is full.
        /// 
        /// </para>
        /// </remarks>
        /// <returns>0 if the byte was not written, non-zero if written.</returns>
        private int write0(byte b)
        {
            if (this.currentByteField == -1)
            {
                this.currentByteField = b;
                this.runLengthField++;
                return 1;
            }
            if (this.currentByteField == b)
            {
                if (++this.runLengthField > 0xfe)
                {
                    bool flag = this.AddRunToOutputBlock(false);
                    this.currentByteField = -1;
                    this.runLengthField = 0;
                    return (flag ? 2 : 1);
                }
                return 1;
            }
            if (this.AddRunToOutputBlock(false))
            {
                this.currentByteField = -1;
                this.runLengthField = 0;
                return 0;
            }
            this.runLengthField = 1;
            this.currentByteField = b;
            return 1;
        }

        public int AvailableBytesOut { get; set; }

        public int BlockSize
        {
            get
            {
                return this.blockSize100kField;
            }
        }

        public uint Crc32 { get; set; }

        /// <summary>
        /// The number of uncompressed bytes being held in the buffer.
        /// </summary>
        /// <remarks>
        /// <para>
        /// I am thinking this may be useful in a Stream that uses this
        /// compressor class. In the Close() method on the stream it could
        /// check this value to see if anything has been written at all.  You
        /// may think the stream could easily track the number of bytes it
        /// wrote, which would eliminate the need for this. But, there is the
        /// case where the stream writes a complete block, and it is full, and
        /// then writes no more. In that case the stream may want to check.
        /// </para>
        /// </remarks>
        public int UncompressedBytes
        {
            get
            {
                return (this.lastField + 1);
            }
        }

        private class CompressionState
        {
            public byte[] block;
            public int[] fmap;
            public readonly int[] ftab = new int[0x10001];
            public readonly byte[] generateMTFValues_yy = new byte[0x100];
            public int[] heap = new int[BZip2.MaxAlphaSize + 2];
            public readonly bool[] inUse = new bool[0x100];
            public readonly bool[] mainSort_bigDone = new bool[0x100];
            public readonly int[] mainSort_copy = new int[0x100];
            public readonly int[] mainSort_runningOrder = new int[0x100];
            public readonly int[] mtfFreq = new int[BZip2.MaxAlphaSize];
            public int[] parent = new int[BZip2.MaxAlphaSize * 2];
            /// Array instance identical to sfmap, both are used only
            /// temporarily and independently, so we do not need to allocate
            /// additional memory.
            public char[] quadrant;
            public readonly byte[] selector = new byte[BZip2.MaxSelectors];
            public readonly byte[] selectorMtf = new byte[BZip2.MaxSelectors];
            public int[][] sendMTFValues_code;
            public readonly short[] sendMTFValues_cost = new short[BZip2.NGroups];
            public readonly int[] sendMTFValues_fave = new int[BZip2.NGroups];
            public byte[][] sendMTFValues_len;
            public int[][] sendMTFValues_rfreq;
            public readonly byte[] sendMTFValues2_pos = new byte[BZip2.NGroups];
            public readonly bool[] sentMTFValues4_inUse16 = new bool[0x10];
            public char[] sfmap;
            public readonly int[] stack_dd = new int[BZip2.QSORT_STACK_SIZE];
            public readonly int[] stack_hh = new int[BZip2.QSORT_STACK_SIZE];
            public readonly int[] stack_ll = new int[BZip2.QSORT_STACK_SIZE];
            public readonly byte[] unseqToSeq = new byte[0x100];
            public int[] weight = new int[BZip2.MaxAlphaSize * 2];

            public CompressionState(int blockSize100k)
            {
                int num = blockSize100k * BZip2.BlockSizeMultiple;
                this.block = new byte[(num + 1) + BZip2.NUM_OVERSHOOT_BYTES];
                this.fmap = new int[num];
                this.sfmap = new char[2 * num];
                this.quadrant = this.sfmap;
                this.sendMTFValues_len = BZip2.InitRectangularArray<byte>(BZip2.NGroups, BZip2.MaxAlphaSize);
                this.sendMTFValues_rfreq = BZip2.InitRectangularArray<int>(BZip2.NGroups, BZip2.MaxAlphaSize);
                this.sendMTFValues_code = BZip2.InitRectangularArray<int>(BZip2.NGroups, BZip2.MaxAlphaSize);
            }
        }
    }
}

