namespace DotNetZipAdditionalPlatforms.BZip2
{
    using System;
    using System.IO;

    internal class BitWriter
    {
        private uint accumulator;
        private int nAccumulatedBits;
        private Stream output;
        private int totalBytesWrittenOut;

        public BitWriter(Stream s)
        {
            this.output = s;
        }

        /// <summary>
        /// Writes all available bytes, and emits padding for the final byte as
        /// necessary. This must be the last method invoked on an instance of
        /// BitWriter.
        /// </summary>
        public void FinishAndPad()
        {
            this.Flush();
            if (this.NumRemainingBits > 0)
            {
                byte num = (byte) ((this.accumulator >> 0x18) & 0xff);
                this.output.WriteByte(num);
                this.totalBytesWrittenOut++;
            }
        }

        /// <summary>
        /// Write all available byte-aligned bytes.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This method writes no new output, but flushes any accumulated
        /// bits. At completion, the accumulator may contain up to 7
        /// bits.
        /// </para>
        /// <para>
        /// This is necessary when re-assembling output from N independent
        /// compressors, one for each of N blocks. The output of any
        /// particular compressor will in general have some fragment of a byte
        /// remaining. This fragment needs to be accumulated into the
        /// parent BZip2OutputStream.
        /// </para>
        /// </remarks>
        public void Flush()
        {
            this.WriteBits(0, 0);
        }

        /// <summary>
        /// Reset the BitWriter.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This is useful when the BitWriter writes into a MemoryStream, and
        /// is used by a BZip2Compressor, which itself is re-used for multiple
        /// distinct data blocks.
        /// </para>
        /// </remarks>
        public void Reset()
        {
            this.accumulator = 0;
            this.nAccumulatedBits = 0;
            this.totalBytesWrittenOut = 0;
            this.output.Seek(0L, SeekOrigin.Begin);
            this.output.SetLength(0L);
        }

        /// <summary>
        /// Write some number of bits from the given value, into the output.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The nbits value should be a max of 25, for safety. For performance
        /// reasons, this method does not check!
        /// </para>
        /// </remarks>
        public void WriteBits(int nbits, uint value)
        {
            int nAccumulatedBits = this.nAccumulatedBits;
            uint accumulator = this.accumulator;
            while (nAccumulatedBits >= 8)
            {
                this.output.WriteByte((byte) ((accumulator >> 0x18) & 0xff));
                this.totalBytesWrittenOut++;
                accumulator = accumulator << 8;
                nAccumulatedBits -= 8;
            }
            this.accumulator = accumulator | (value << ((0x20 - nAccumulatedBits) - nbits));
            this.nAccumulatedBits = nAccumulatedBits + nbits;
        }

        /// <summary>
        /// Write a full 8-bit byte into the output.
        /// </summary>
        public void WriteByte(byte b)
        {
            this.WriteBits(8, b);
        }

        /// <summary>
        /// Write four 8-bit bytes into the output.
        /// </summary>
        public void WriteInt(uint u)
        {
            this.WriteBits(8, (u >> 0x18) & 0xff);
            this.WriteBits(8, (u >> 0x10) & 0xff);
            this.WriteBits(8, (u >> 8) & 0xff);
            this.WriteBits(8, u & 0xff);
        }

        public int NumRemainingBits
        {
            get
            {
                return this.nAccumulatedBits;
            }
        }

        /// <summary>
        /// Delivers the remaining bits, left-aligned, in a byte.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This is valid only if NumRemainingBits is less than 8;
        /// in other words it is valid only after a call to Flush().
        /// </para>
        /// </remarks>
        public byte RemainingBits
        {
            get
            {
                return (byte) ((this.accumulator >> (0x20 - this.nAccumulatedBits)) & 0xff);
            }
        }

        public int TotalBytesWrittenOut
        {
            get
            {
                return this.totalBytesWrittenOut;
            }
        }
    }
}

