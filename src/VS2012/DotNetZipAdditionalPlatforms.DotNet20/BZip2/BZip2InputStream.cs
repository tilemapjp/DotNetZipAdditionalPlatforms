namespace DotNetZipAdditionalPlatforms.BZip2
{
    using DotNetZipAdditionalPlatforms.Crc;
    using System;
    using System.Globalization;
    using System.IO;

    /// <summary>
    /// A read-only decorator stream that performs BZip2 decompression on Read.
    /// </summary>
    public class BZip2InputStream : Stream
    {
        private bool _disposed;
        private bool _leaveOpen;
        private bool blockRandomised;
        private int blockSize100k;
        private int bsBuff;
        private int bsLive;
        private uint computedBlockCRC;
        private uint computedCombinedCRC;
        private readonly CRC32 crc;
        private int currentChar;
        private CState currentState;
        private DecompressionState data;
        private Stream input;
        private int last;
        private int nInUse;
        private int origPtr;
        private uint storedBlockCRC;
        private uint storedCombinedCRC;
        private int su_ch2;
        private int su_chPrev;
        private int su_count;
        private int su_i2;
        private int su_j2;
        private int su_rNToGo;
        private int su_rTPos;
        private int su_tPos;
        private char su_z;
        private long totalBytesRead;

        /// <summary>
        /// Create a BZip2InputStream, wrapping it around the given input Stream.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The input stream will be closed when the BZip2InputStream is closed.
        /// </para>
        /// </remarks>
        /// <param name="input">The stream from which to read compressed data</param>
        public BZip2InputStream(Stream input) : this(input, false)
        {
        }

        /// <summary>
        /// Create a BZip2InputStream with the given stream, and
        /// specifying whether to leave the wrapped stream open when
        /// the BZip2InputStream is closed.
        /// </summary>
        /// <param name="input">The stream from which to read compressed data</param>
        /// <param name="leaveOpen">
        /// Whether to leave the input stream open, when the BZip2InputStream closes.
        /// </param>
        /// 
        /// <example>
        /// 
        /// This example reads a bzip2-compressed file, decompresses it,
        /// and writes the decompressed data into a newly created file.
        /// 
        /// <code>
        /// var fname = "logfile.log.bz2";
        /// using (var fs = File.OpenRead(fname))
        /// {
        /// using (var decompressor = new DotNetZipAdditionalPlatforms.BZip2.BZip2InputStream(fs))
        /// {
        /// var outFname = fname + ".decompressed";
        /// using (var output = File.Create(outFname))
        /// {
        /// byte[] buffer = new byte[2048];
        /// int n;
        /// while ((n = decompressor.Read(buffer, 0, buffer.Length)) &gt; 0)
        /// {
        /// output.Write(buffer, 0, n);
        /// }
        /// }
        /// }
        /// }
        /// </code>
        /// </example>
        public BZip2InputStream(Stream input, bool leaveOpen)
        {
            this.crc = new CRC32(true);
            this.currentChar = -1;
            this.currentState = CState.START_BLOCK;
            this.input = input;
            this._leaveOpen = leaveOpen;
            this.init();
        }

        private bool bsGetBit()
        {
            return (this.GetBits(1) != 0);
        }

        private uint bsGetInt()
        {
            return (uint) ((((((this.GetBits(8) << 8) | this.GetBits(8)) << 8) | this.GetBits(8)) << 8) | this.GetBits(8));
        }

        private char bsGetUByte()
        {
            return (char) this.GetBits(8);
        }

        private void CheckMagicChar(char expected, int position)
        {
            int num = this.input.ReadByte();
            if (num != expected)
            {
                throw new IOException(string.Format(CultureInfo.InvariantCulture, "Not a valid BZip2 stream. byte {0}, expected '{1}', got '{2}'", position, (int) expected, num));
            }
        }

        /// <summary>
        /// Close the stream.
        /// </summary>
        public override void Close()
        {
            Stream input = this.input;
            if (input != null)
            {
                try
                {
                    if (!this._leaveOpen)
                    {
                        input.Close();
                    }
                }
                finally
                {
                    this.data = null;
                    this.input = null;
                }
            }
        }

        private void complete()
        {
            this.storedCombinedCRC = this.bsGetInt();
            this.currentState = CState.EOF;
            this.data = null;
            if (this.storedCombinedCRC != this.computedCombinedCRC)
            {
                throw new IOException(string.Format(CultureInfo.InvariantCulture, "BZip2 CRC error (expected {0:X8}, computed {1:X8})", this.storedCombinedCRC, this.computedCombinedCRC));
            }
        }

        /// Called by recvDecodingTables() exclusively.
        private void createHuffmanDecodingTables(int alphaSize, int nGroups)
        {
            DecompressionState data = this.data;
            char[][] chArray = data.temp_charArray2d;
            for (int i = 0; i < nGroups; i++)
            {
                int minLen = 0x20;
                int maxLen = 0;
                char[] chArray2 = chArray[i];
                int index = alphaSize;
                while (--index >= 0)
                {
                    char ch = chArray2[index];
                    if (ch > maxLen)
                    {
                        maxLen = ch;
                    }
                    if (ch < minLen)
                    {
                        minLen = ch;
                    }
                }
                hbCreateDecodeTables(data.gLimit[i], data.gBase[i], data.gPerm[i], chArray[i], minLen, maxLen, alphaSize);
                data.gMinlen[i] = minLen;
            }
        }

        /// <summary>
        /// Dispose the stream.
        /// </summary>
        /// <param name="disposing">
        /// indicates whether the Dispose method was invoked by user code.
        /// </param>
        protected override void Dispose(bool disposing)
        {
            try
            {
                if (!this._disposed)
                {
                    if (disposing && (this.input != null))
                    {
                        this.input.Close();
                    }
                    this._disposed = true;
                }
            }
            finally
            {
                base.Dispose(disposing);
            }
        }

        private void EndBlock()
        {
            this.computedBlockCRC = (uint) this.crc.Crc32Result;
            if (this.storedBlockCRC != this.computedBlockCRC)
            {
                throw new IOException(string.Format(CultureInfo.InvariantCulture, "BZip2 CRC error (expected {0:X8}, computed {1:X8})", this.storedBlockCRC, this.computedBlockCRC));
            }
            this.computedCombinedCRC = (this.computedCombinedCRC << 1) | (this.computedCombinedCRC >> 0x1f);
            this.computedCombinedCRC ^= this.computedBlockCRC;
        }

        /// <summary>
        /// Flush the stream.
        /// </summary>
        public override void Flush()
        {
            if (this._disposed)
            {
                throw new ObjectDisposedException("BZip2Stream");
            }
            this.input.Flush();
        }

        private void getAndMoveToFrontDecode()
        {
            DecompressionState data = this.data;
            this.origPtr = this.GetBits(0x18);
            if (this.origPtr < 0)
            {
                throw new IOException("BZ_DATA_ERROR");
            }
            if (this.origPtr > (10 + (DotNetZipAdditionalPlatforms.BZip2.BZip2.BlockSizeMultiple * this.blockSize100k)))
            {
                throw new IOException("BZ_DATA_ERROR");
            }
            this.recvDecodingTables();
            byte[] src = data.getAndMoveToFrontDecode_yy;
            int num = this.blockSize100k * DotNetZipAdditionalPlatforms.BZip2.BZip2.BlockSizeMultiple;
            int index = 0x100;
            while (--index >= 0)
            {
                src[index] = (byte) index;
                data.unzftab[index] = 0;
            }
            int num3 = 0;
            int num4 = DotNetZipAdditionalPlatforms.BZip2.BZip2.G_SIZE - 1;
            int num5 = this.nInUse + 1;
            int num6 = this.getAndMoveToFrontDecode0(0);
            int bsBuff = this.bsBuff;
            int bsLive = this.bsLive;
            int num9 = -1;
            int num10 = data.selector[num3] & 0xff;
            int[] numArray = data.gBase[num10];
            int[] numArray2 = data.gLimit[num10];
            int[] numArray3 = data.gPerm[num10];
            int num11 = data.gMinlen[num10];
            while (num6 != num5)
            {
                int num15;
                byte num17;
                if ((num6 != DotNetZipAdditionalPlatforms.BZip2.BZip2.RUNA) && (num6 != DotNetZipAdditionalPlatforms.BZip2.BZip2.RUNB))
                {
                    goto Label_0355;
                }
                int num12 = -1;
                int num13 = 1;
                goto Label_02DB;
            Label_0144:
                if (num6 == DotNetZipAdditionalPlatforms.BZip2.BZip2.RUNA)
                {
                    num12 += num13;
                }
                else if (num6 == DotNetZipAdditionalPlatforms.BZip2.BZip2.RUNB)
                {
                    num12 += num13 << 1;
                }
                else
                {
                    goto Label_02E3;
                }
                if (num4 == 0)
                {
                    num4 = DotNetZipAdditionalPlatforms.BZip2.BZip2.G_SIZE - 1;
                    num10 = data.selector[++num3] & 0xff;
                    numArray = data.gBase[num10];
                    numArray2 = data.gLimit[num10];
                    numArray3 = data.gPerm[num10];
                    num11 = data.gMinlen[num10];
                }
                else
                {
                    num4--;
                }
                int num14 = num11;
                while (bsLive < num14)
                {
                    num15 = this.input.ReadByte();
                    if (num15 < 0)
                    {
                        throw new IOException("unexpected end of stream");
                    }
                    bsBuff = (bsBuff << 8) | num15;
                    bsLive += 8;
                }
                int num16 = (bsBuff >> (bsLive - num14)) & ((((int) 1) << num14) - 1);
                bsLive -= num14;
                while (num16 > numArray2[num14])
                {
                    num14++;
                    while (bsLive < 1)
                    {
                        num15 = this.input.ReadByte();
                        if (num15 < 0)
                        {
                            throw new IOException("unexpected end of stream");
                        }
                        bsBuff = (bsBuff << 8) | num15;
                        bsLive += 8;
                    }
                    bsLive--;
                    num16 = (num16 << 1) | ((bsBuff >> bsLive) & 1);
                }
                num6 = numArray3[num16 - numArray[num14]];
                num13 = num13 << 1;
            Label_02DB:
                goto Label_0144;
            Label_02E3:
                num17 = data.seqToUnseq[src[0]];
                data.unzftab[num17 & 0xff] += num12 + 1;
                while (num12-- >= 0)
                {
                    data.ll8[++num9] = num17;
                }
                if (num9 >= num)
                {
                    throw new IOException("block overrun");
                }
                continue;
            Label_0355:
                if (++num9 >= num)
                {
                    throw new IOException("block overrun");
                }
                byte num18 = src[num6 - 1];
                data.unzftab[data.seqToUnseq[num18] & 0xff]++;
                data.ll8[num9] = data.seqToUnseq[num18];
                if (num6 <= 0x10)
                {
                    int num19 = num6 - 1;
                    while (num19 > 0)
                    {
                        src[num19] = src[--num19];
                    }
                }
                else
                {
                    Buffer.BlockCopy(src, 0, src, 1, num6 - 1);
                }
                src[0] = num18;
                if (num4 == 0)
                {
                    num4 = DotNetZipAdditionalPlatforms.BZip2.BZip2.G_SIZE - 1;
                    num10 = data.selector[++num3] & 0xff;
                    numArray = data.gBase[num10];
                    numArray2 = data.gLimit[num10];
                    numArray3 = data.gPerm[num10];
                    num11 = data.gMinlen[num10];
                }
                else
                {
                    num4--;
                }
                num14 = num11;
                while (bsLive < num14)
                {
                    num15 = this.input.ReadByte();
                    if (num15 < 0)
                    {
                        throw new IOException("unexpected end of stream");
                    }
                    bsBuff = (bsBuff << 8) | num15;
                    bsLive += 8;
                }
                num16 = (bsBuff >> (bsLive - num14)) & ((((int) 1) << num14) - 1);
                bsLive -= num14;
                while (num16 > numArray2[num14])
                {
                    num14++;
                    while (bsLive < 1)
                    {
                        num15 = this.input.ReadByte();
                        if (num15 < 0)
                        {
                            throw new IOException("unexpected end of stream");
                        }
                        bsBuff = (bsBuff << 8) | num15;
                        bsLive += 8;
                    }
                    bsLive--;
                    num16 = (num16 << 1) | ((bsBuff >> bsLive) & 1);
                }
                num6 = numArray3[num16 - numArray[num14]];
            }
            this.last = num9;
            this.bsLive = bsLive;
            this.bsBuff = bsBuff;
        }

        private int getAndMoveToFrontDecode0(int groupNo)
        {
            DecompressionState data = this.data;
            int index = data.selector[groupNo] & 0xff;
            int[] numArray = data.gLimit[index];
            int n = data.gMinlen[index];
            int bits = this.GetBits(n);
            int bsLive = this.bsLive;
            int bsBuff = this.bsBuff;
            while (bits > numArray[n])
            {
                n++;
                while (bsLive < 1)
                {
                    int num6 = this.input.ReadByte();
                    if (num6 < 0)
                    {
                        throw new IOException("unexpected end of stream");
                    }
                    bsBuff = (bsBuff << 8) | num6;
                    bsLive += 8;
                }
                bsLive--;
                bits = (bits << 1) | ((bsBuff >> bsLive) & 1);
            }
            this.bsLive = bsLive;
            this.bsBuff = bsBuff;
            return data.gPerm[index][bits - data.gBase[index][n]];
        }

        /// <summary>
        /// Read n bits from input, right justifying the result.
        /// </summary>
        /// <remarks>
        /// <para>
        /// For example, if you read 1 bit, the result is either 0
        /// or 1.
        /// </para>
        /// </remarks>
        /// <param name="n">
        /// The number of bits to read, always between 1 and 32.
        /// </param>
        private int GetBits(int n)
        {
            int bsLive = this.bsLive;
            int bsBuff = this.bsBuff;
            if (bsLive < n)
            {
                do
                {
                    int num3 = this.input.ReadByte();
                    if (num3 < 0)
                    {
                        throw new IOException("unexpected end of stream");
                    }
                    bsBuff = (bsBuff << 8) | num3;
                    bsLive += 8;
                }
                while (bsLive < n);
                this.bsBuff = bsBuff;
            }
            this.bsLive = bsLive - n;
            return ((bsBuff >> (bsLive - n)) & ((((int) 1) << n) - 1));
        }

        /// Called by createHuffmanDecodingTables() exclusively.
        private static void hbCreateDecodeTables(int[] limit, int[] bbase, int[] perm, char[] length, int minLen, int maxLen, int alphaSize)
        {
            int index = minLen;
            int num2 = 0;
            while (index <= maxLen)
            {
                for (int i = 0; i < alphaSize; i++)
                {
                    if (length[i] == index)
                    {
                        perm[num2++] = i;
                    }
                }
                index++;
            }
            index = DotNetZipAdditionalPlatforms.BZip2.BZip2.MaxCodeLength;
            while (--index > 0)
            {
                bbase[index] = 0;
                limit[index] = 0;
            }
            for (index = 0; index < alphaSize; index++)
            {
                bbase[length[index] + '\x0001']++;
            }
            index = 1;
            int num4 = bbase[0];
            while (index < DotNetZipAdditionalPlatforms.BZip2.BZip2.MaxCodeLength)
            {
                bbase[index] = num4 + bbase[index];
                index++;
            }
            index = minLen;
            int num5 = 0;
            num4 = bbase[index];
            while (index <= maxLen)
            {
                int num6 = bbase[index + 1];
                num5 += num6 - num4;
                num4 = num6;
                limit[index] = num5 - 1;
                num5 = num5 << 1;
                index++;
            }
            for (index = minLen + 1; index <= maxLen; index++)
            {
                bbase[index] = ((limit[index - 1] + 1) << 1) - bbase[index];
            }
        }

        private void init()
        {
            if (null == this.input)
            {
                throw new IOException("No input Stream");
            }
            if (!this.input.CanRead)
            {
                throw new IOException("Unreadable input Stream");
            }
            this.CheckMagicChar('B', 0);
            this.CheckMagicChar('Z', 1);
            this.CheckMagicChar('h', 2);
            int num = this.input.ReadByte();
            if ((num < 0x31) || (num > 0x39))
            {
                throw new IOException("Stream is not BZip2 formatted: illegal blocksize " + ((char) num));
            }
            this.blockSize100k = num - 0x30;
            this.InitBlock();
            this.SetupBlock();
        }

        private void InitBlock()
        {
            char ch = this.bsGetUByte();
            char ch2 = this.bsGetUByte();
            char ch3 = this.bsGetUByte();
            char ch4 = this.bsGetUByte();
            char ch5 = this.bsGetUByte();
            char ch6 = this.bsGetUByte();
            if (((((ch == '\x0017') && (ch2 == 'r')) && ((ch3 == 'E') && (ch4 == '8'))) && (ch5 == 'P')) && (ch6 == '\x0090'))
            {
                this.complete();
            }
            else
            {
                if (((((ch != '1') || (ch2 != 'A')) || ((ch3 != 'Y') || (ch4 != '&'))) || (ch5 != 'S')) || (ch6 != 'Y'))
                {
                    this.currentState = CState.EOF;
                    throw new IOException(string.Format(CultureInfo.InvariantCulture, "bad block header at offset 0x{0:X}", this.input.Position));
                }
                this.storedBlockCRC = this.bsGetInt();
                this.blockRandomised = this.GetBits(1) == 1;
                if (this.data == null)
                {
                    this.data = new DecompressionState(this.blockSize100k);
                }
                this.getAndMoveToFrontDecode();
                this.crc.Reset();
                this.currentState = CState.START_BLOCK;
            }
        }

        private void MakeMaps()
        {
            bool[] inUse = this.data.inUse;
            byte[] seqToUnseq = this.data.seqToUnseq;
            int num = 0;
            for (int i = 0; i < 0x100; i++)
            {
                if (inUse[i])
                {
                    seqToUnseq[num++] = (byte) i;
                }
            }
            this.nInUse = num;
        }

        /// <summary>
        /// Read data from the stream.
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// To decompress a BZip2 data stream, create a <c>BZip2InputStream</c>,
        /// providing a stream that reads compressed data.  Then call Read() on
        /// that <c>BZip2InputStream</c>, and the data read will be decompressed
        /// as you read.
        /// </para>
        /// 
        /// <para>
        /// A <c>BZip2InputStream</c> can be used only for <c>Read()</c>, not for <c>Write()</c>.
        /// </para>
        /// </remarks>
        /// 
        /// <param name="buffer">The buffer into which the read data should be placed.</param>
        /// <param name="offset">the offset within that data array to put the first byte read.</param>
        /// <param name="count">the number of bytes to read.</param>
        /// <returns>the number of bytes actually read</returns>
        public override int Read(byte[] buffer, int offset, int count)
        {
            int num3;
            if (offset < 0)
            {
                throw new IndexOutOfRangeException(string.Format(CultureInfo.InvariantCulture, "offset ({0}) must be > 0", offset));
            }
            if (count < 0)
            {
                throw new IndexOutOfRangeException(string.Format(CultureInfo.InvariantCulture, "count ({0}) must be > 0", count));
            }
            if ((offset + count) > buffer.Length)
            {
                throw new IndexOutOfRangeException(string.Format(CultureInfo.InvariantCulture, "offset({0}) count({1}) bLength({2})", offset, count, buffer.Length));
            }
            if (this.input == null)
            {
                throw new IOException("the stream is not open");
            }
            int num = offset + count;
            int num2 = offset;
            while ((num2 < num) && ((num3 = this.ReadByte()) >= 0))
            {
                buffer[num2++] = (byte) num3;
            }
            return ((num2 == offset) ? -1 : (num2 - offset));
        }

        /// <summary>
        /// Read a single byte from the stream.
        /// </summary>
        /// <returns>the byte read from the stream, or -1 if EOF</returns>
        public override int ReadByte()
        {
            int currentChar = this.currentChar;
            this.totalBytesRead += 1L;
            switch (this.currentState)
            {
                case CState.EOF:
                    return -1;

                case CState.START_BLOCK:
                    throw new IOException("bad state");

                case CState.RAND_PART_A:
                    throw new IOException("bad state");

                case CState.RAND_PART_B:
                    this.SetupRandPartB();
                    return currentChar;

                case CState.RAND_PART_C:
                    this.SetupRandPartC();
                    return currentChar;

                case CState.NO_RAND_PART_A:
                    throw new IOException("bad state");

                case CState.NO_RAND_PART_B:
                    this.SetupNoRandPartB();
                    return currentChar;

                case CState.NO_RAND_PART_C:
                    this.SetupNoRandPartC();
                    return currentChar;
            }
            throw new IOException("bad state");
        }

        private void recvDecodingTables()
        {
            int num2;
            int num4;
            DecompressionState data = this.data;
            bool[] inUse = data.inUse;
            byte[] buffer = data.recvDecodingTables_pos;
            int num = 0;
            for (num2 = 0; num2 < 0x10; num2++)
            {
                if (this.bsGetBit())
                {
                    num |= ((int) 1) << num2;
                }
            }
            num2 = 0x100;
            while (--num2 >= 0)
            {
                inUse[num2] = false;
            }
            for (num2 = 0; num2 < 0x10; num2++)
            {
                if ((num & (((int) 1) << num2)) != 0)
                {
                    int num3 = num2 << 4;
                    num4 = 0;
                    while (num4 < 0x10)
                    {
                        if (this.bsGetBit())
                        {
                            inUse[num3 + num4] = true;
                        }
                        num4++;
                    }
                }
            }
            this.MakeMaps();
            int alphaSize = this.nInUse + 2;
            int bits = this.GetBits(3);
            int num7 = this.GetBits(15);
            for (num2 = 0; num2 < num7; num2++)
            {
                num4 = 0;
                while (this.bsGetBit())
                {
                    num4++;
                }
                data.selectorMtf[num2] = (byte) num4;
            }
            int index = bits;
            while (--index >= 0)
            {
                buffer[index] = (byte) index;
            }
            num2 = 0;
            while (num2 < num7)
            {
                index = data.selectorMtf[num2];
                byte num9 = buffer[index];
                while (index > 0)
                {
                    buffer[index] = buffer[index - 1];
                    index--;
                }
                buffer[0] = num9;
                data.selector[num2] = num9;
                num2++;
            }
            char[][] chArray = data.temp_charArray2d;
            for (int i = 0; i < bits; i++)
            {
                int num11 = this.GetBits(5);
                char[] chArray2 = chArray[i];
                for (num2 = 0; num2 < alphaSize; num2++)
                {
                    while (this.bsGetBit())
                    {
                        num11 += this.bsGetBit() ? -1 : 1;
                    }
                    chArray2[num2] = (char) num11;
                }
            }
            this.createHuffmanDecodingTables(alphaSize, bits);
        }

        /// <summary>
        /// Calling this method always throws a <see cref="T:System.NotImplementedException" />.
        /// </summary>
        /// <param name="offset">this is irrelevant, since it will always throw!</param>
        /// <param name="origin">this is irrelevant, since it will always throw!</param>
        /// <returns>irrelevant!</returns>
        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Calling this method always throws a <see cref="T:System.NotImplementedException" />.
        /// </summary>
        /// <param name="value">this is irrelevant, since it will always throw!</param>
        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        private void SetupBlock()
        {
            if (this.data != null)
            {
                int num;
                DecompressionState data = this.data;
                int[] numArray = data.initTT(this.last + 1);
                for (num = 0; num <= 0xff; num++)
                {
                    if ((data.unzftab[num] < 0) || (data.unzftab[num] > this.last))
                    {
                        throw new Exception("BZ_DATA_ERROR");
                    }
                }
                data.cftab[0] = 0;
                for (num = 1; num <= 0x100; num++)
                {
                    data.cftab[num] = data.unzftab[num - 1];
                }
                for (num = 1; num <= 0x100; num++)
                {
                    data.cftab[num] += data.cftab[num - 1];
                }
                for (num = 0; num <= 0x100; num++)
                {
                    if ((data.cftab[num] < 0) || (data.cftab[num] > (this.last + 1)))
                    {
                        throw new Exception(string.Format(CultureInfo.InvariantCulture, "BZ_DATA_ERROR: cftab[{0}]={1} last={2}", num, data.cftab[num], this.last));
                    }
                }
                for (num = 1; num <= 0x100; num++)
                {
                    if (data.cftab[num - 1] > data.cftab[num])
                    {
                        throw new Exception("BZ_DATA_ERROR");
                    }
                }
                num = 0;
                int last = this.last;
                while (num <= last)
                {
                    numArray[data.cftab[data.ll8[num] & 0xff]++] = num;
                    num++;
                }
                if ((this.origPtr < 0) || (this.origPtr >= numArray.Length))
                {
                    throw new IOException("stream corrupted");
                }
                this.su_tPos = numArray[this.origPtr];
                this.su_count = 0;
                this.su_i2 = 0;
                this.su_ch2 = 0x100;
                if (this.blockRandomised)
                {
                    this.su_rNToGo = 0;
                    this.su_rTPos = 0;
                    this.SetupRandPartA();
                }
                else
                {
                    this.SetupNoRandPartA();
                }
            }
        }

        private void SetupNoRandPartA()
        {
            if (this.su_i2 <= this.last)
            {
                this.su_chPrev = this.su_ch2;
                int num = this.data.ll8[this.su_tPos] & 0xff;
                this.su_ch2 = num;
                this.su_tPos = this.data.tt[this.su_tPos];
                this.su_i2++;
                this.currentChar = num;
                this.currentState = CState.NO_RAND_PART_B;
                this.crc.UpdateCRC((byte) num);
            }
            else
            {
                this.currentState = CState.NO_RAND_PART_A;
                this.EndBlock();
                this.InitBlock();
                this.SetupBlock();
            }
        }

        private void SetupNoRandPartB()
        {
            if (this.su_ch2 != this.su_chPrev)
            {
                this.su_count = 1;
                this.SetupNoRandPartA();
            }
            else if (++this.su_count >= 4)
            {
                this.su_z = (char) (this.data.ll8[this.su_tPos] & 0xff);
                this.su_tPos = this.data.tt[this.su_tPos];
                this.su_j2 = 0;
                this.SetupNoRandPartC();
            }
            else
            {
                this.SetupNoRandPartA();
            }
        }

        private void SetupNoRandPartC()
        {
            if (this.su_j2 < this.su_z)
            {
                int num = this.su_ch2;
                this.currentChar = num;
                this.crc.UpdateCRC((byte) num);
                this.su_j2++;
                this.currentState = CState.NO_RAND_PART_C;
            }
            else
            {
                this.su_i2++;
                this.su_count = 0;
                this.SetupNoRandPartA();
            }
        }

        private void SetupRandPartA()
        {
            if (this.su_i2 <= this.last)
            {
                this.su_chPrev = this.su_ch2;
                int num = this.data.ll8[this.su_tPos] & 0xff;
                this.su_tPos = this.data.tt[this.su_tPos];
                if (this.su_rNToGo == 0)
                {
                    this.su_rNToGo = Rand.Rnums(this.su_rTPos) - 1;
                    if (++this.su_rTPos == 0x200)
                    {
                        this.su_rTPos = 0;
                    }
                }
                else
                {
                    this.su_rNToGo--;
                }
                this.su_ch2 = num ^= (this.su_rNToGo == 1) ? 1 : 0;
                this.su_i2++;
                this.currentChar = num;
                this.currentState = CState.RAND_PART_B;
                this.crc.UpdateCRC((byte) num);
            }
            else
            {
                this.EndBlock();
                this.InitBlock();
                this.SetupBlock();
            }
        }

        private void SetupRandPartB()
        {
            if (this.su_ch2 != this.su_chPrev)
            {
                this.currentState = CState.RAND_PART_A;
                this.su_count = 1;
                this.SetupRandPartA();
            }
            else if (++this.su_count >= 4)
            {
                this.su_z = (char) (this.data.ll8[this.su_tPos] & 0xff);
                this.su_tPos = this.data.tt[this.su_tPos];
                if (this.su_rNToGo == 0)
                {
                    this.su_rNToGo = Rand.Rnums(this.su_rTPos) - 1;
                    if (++this.su_rTPos == 0x200)
                    {
                        this.su_rTPos = 0;
                    }
                }
                else
                {
                    this.su_rNToGo--;
                }
                this.su_j2 = 0;
                this.currentState = CState.RAND_PART_C;
                if (this.su_rNToGo == 1)
                {
                    this.su_z = (char) (this.su_z ^ '\x0001');
                }
                this.SetupRandPartC();
            }
            else
            {
                this.currentState = CState.RAND_PART_A;
                this.SetupRandPartA();
            }
        }

        private void SetupRandPartC()
        {
            if (this.su_j2 < this.su_z)
            {
                this.currentChar = this.su_ch2;
                this.crc.UpdateCRC((byte) this.su_ch2);
                this.su_j2++;
            }
            else
            {
                this.currentState = CState.RAND_PART_A;
                this.su_i2++;
                this.su_count = 0;
                this.SetupRandPartA();
            }
        }

        /// <summary>
        /// Calling this method always throws a <see cref="T:System.NotImplementedException" />.
        /// </summary>
        /// <param name="buffer">this parameter is never used</param>
        /// <param name="offset">this parameter is never used</param>
        /// <param name="count">this parameter is never used</param>
        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Indicates whether the stream can be read.
        /// </summary>
        /// <remarks>
        /// The return value depends on whether the captive stream supports reading.
        /// </remarks>
        public override bool CanRead
        {
            get
            {
                if (this._disposed)
                {
                    throw new ObjectDisposedException("BZip2Stream");
                }
                return this.input.CanRead;
            }
        }

        /// <summary>
        /// Indicates whether the stream supports Seek operations.
        /// </summary>
        /// <remarks>
        /// Always returns false.
        /// </remarks>
        public override bool CanSeek
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Indicates whether the stream can be written.
        /// </summary>
        /// <remarks>
        /// The return value depends on whether the captive stream supports writing.
        /// </remarks>
        public override bool CanWrite
        {
            get
            {
                if (this._disposed)
                {
                    throw new ObjectDisposedException("BZip2Stream");
                }
                return this.input.CanWrite;
            }
        }

        /// <summary>
        /// Reading this property always throws a <see cref="T:System.NotImplementedException" />.
        /// </summary>
        public override long Length
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// The position of the stream pointer.
        /// </summary>
        /// 
        /// <remarks>
        /// Setting this property always throws a <see cref="T:System.NotImplementedException" />. Reading will return the
        /// total number of uncompressed bytes read in.
        /// </remarks>
        public override long Position
        {
            get
            {
                return this.totalBytesRead;
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Compressor State
        /// </summary>
        private enum CState
        {
            EOF,
            START_BLOCK,
            RAND_PART_A,
            RAND_PART_B,
            RAND_PART_C,
            NO_RAND_PART_A,
            NO_RAND_PART_B,
            NO_RAND_PART_C
        }

        private sealed class DecompressionState
        {
            public readonly int[] cftab = new int[0x101];
            public readonly int[][] gBase = DotNetZipAdditionalPlatforms.BZip2.BZip2.InitRectangularArray<int>(DotNetZipAdditionalPlatforms.BZip2.BZip2.NGroups, DotNetZipAdditionalPlatforms.BZip2.BZip2.MaxAlphaSize);
            public readonly byte[] getAndMoveToFrontDecode_yy = new byte[0x100];
            public readonly int[][] gLimit = DotNetZipAdditionalPlatforms.BZip2.BZip2.InitRectangularArray<int>(DotNetZipAdditionalPlatforms.BZip2.BZip2.NGroups, DotNetZipAdditionalPlatforms.BZip2.BZip2.MaxAlphaSize);
            public readonly int[] gMinlen = new int[DotNetZipAdditionalPlatforms.BZip2.BZip2.NGroups];
            public readonly int[][] gPerm = DotNetZipAdditionalPlatforms.BZip2.BZip2.InitRectangularArray<int>(DotNetZipAdditionalPlatforms.BZip2.BZip2.NGroups, DotNetZipAdditionalPlatforms.BZip2.BZip2.MaxAlphaSize);
            public readonly bool[] inUse = new bool[0x100];
            public byte[] ll8;
            public readonly byte[] recvDecodingTables_pos = new byte[DotNetZipAdditionalPlatforms.BZip2.BZip2.NGroups];
            public readonly byte[] selector = new byte[DotNetZipAdditionalPlatforms.BZip2.BZip2.MaxSelectors];
            public readonly byte[] selectorMtf = new byte[DotNetZipAdditionalPlatforms.BZip2.BZip2.MaxSelectors];
            public readonly byte[] seqToUnseq = new byte[0x100];
            public readonly char[][] temp_charArray2d = DotNetZipAdditionalPlatforms.BZip2.BZip2.InitRectangularArray<char>(DotNetZipAdditionalPlatforms.BZip2.BZip2.NGroups, DotNetZipAdditionalPlatforms.BZip2.BZip2.MaxAlphaSize);
            public int[] tt;
            /// Freq table collected to save a pass over the data during
            /// decompression.
            public readonly int[] unzftab = new int[0x100];

            public DecompressionState(int blockSize100k)
            {
                this.ll8 = new byte[blockSize100k * DotNetZipAdditionalPlatforms.BZip2.BZip2.BlockSizeMultiple];
            }

            /// Initializes the tt array.
            /// 
            /// This method is called when the required length of the array is known.
            /// I don't initialize it at construction time to avoid unneccessary
            /// memory allocation when compressing small files.
            public int[] initTT(int length)
            {
                int[] tt = this.tt;
                if ((tt == null) || (tt.Length < length))
                {
                    this.tt = tt = new int[length];
                }
                return tt;
            }
        }
    }
}

