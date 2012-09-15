namespace DotNetZipAdditionalPlatforms.BZip2
{
    using System;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Threading;

    /// <summary>
    /// A write-only decorator stream that compresses data as it is
    /// written using the BZip2 algorithm.
    /// </summary>
    public class BZip2OutputStream : Stream
    {
        private int blockSize100k;
        private BitWriter bw;
        private uint combinedCRC;
        private BZip2Compressor compressor;
        private TraceBits desiredTrace;
        private bool leaveOpen;
        private Stream output;
        private int totalBytesWrittenIn;

        /// <summary>
        /// Constructs a new <c>BZip2OutputStream</c>, that sends its
        /// compressed output to the given output stream.
        /// </summary>
        /// 
        /// <param name="output">
        /// The destination stream, to which compressed output will be sent.
        /// </param>
        /// 
        /// <example>
        /// 
        /// This example reads a file, then compresses it with bzip2 file,
        /// and writes the compressed data into a newly created file.
        /// 
        /// <code>
        /// var fname = "logfile.log";
        /// using (var fs = File.OpenRead(fname))
        /// {
        /// var outFname = fname + ".bz2";
        /// using (var output = File.Create(outFname))
        /// {
        /// using (var compressor = new DotNetZipAdditionalPlatforms.BZip2.BZip2OutputStream(output))
        /// {
        /// byte[] buffer = new byte[2048];
        /// int n;
        /// while ((n = fs.Read(buffer, 0, buffer.Length)) &gt; 0)
        /// {
        /// compressor.Write(buffer, 0, n);
        /// }
        /// }
        /// }
        /// }
        /// </code>
        /// </example>
        public BZip2OutputStream(Stream output) : this(output, DotNetZipAdditionalPlatforms.BZip2.BZip2.MaxBlockSize, false)
        {
        }

        /// <summary>
        /// Constructs a new <c>BZip2OutputStream</c>.
        /// </summary>
        /// <param name="output">the destination stream.</param>
        /// <param name="leaveOpen">
        /// whether to leave the captive stream open upon closing this stream.
        /// </param>
        public BZip2OutputStream(Stream output, bool leaveOpen) : this(output, DotNetZipAdditionalPlatforms.BZip2.BZip2.MaxBlockSize, leaveOpen)
        {
        }

        /// <summary>
        /// Constructs a new <c>BZip2OutputStream</c> with specified blocksize.
        /// </summary>
        /// <param name="output">the destination stream.</param>
        /// <param name="blockSize">
        /// The blockSize in units of 100000 bytes.
        /// The valid range is 1..9.
        /// </param>
        public BZip2OutputStream(Stream output, int blockSize) : this(output, blockSize, false)
        {
        }

        /// <summary>
        /// Constructs a new <c>BZip2OutputStream</c> with specified blocksize,
        /// and explicitly specifies whether to leave the wrapped stream open.
        /// </summary>
        /// 
        /// <param name="output">the destination stream.</param>
        /// <param name="blockSize">
        /// The blockSize in units of 100000 bytes.
        /// The valid range is 1..9.
        /// </param>
        /// <param name="leaveOpen">
        /// whether to leave the captive stream open upon closing this stream.
        /// </param>
        public BZip2OutputStream(Stream output, int blockSize, bool leaveOpen)
        {
            this.desiredTrace = TraceBits.None | TraceBits.Crc | TraceBits.Write;
            if ((blockSize < DotNetZipAdditionalPlatforms.BZip2.BZip2.MinBlockSize) || (blockSize > DotNetZipAdditionalPlatforms.BZip2.BZip2.MaxBlockSize))
            {
                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, "blockSize={0} is out of range; must be between {1} and {2}", blockSize, DotNetZipAdditionalPlatforms.BZip2.BZip2.MinBlockSize, DotNetZipAdditionalPlatforms.BZip2.BZip2.MaxBlockSize), "blockSize");
            }
            this.output = output;
            if (!this.output.CanWrite)
            {
                throw new ArgumentException("The stream is not writable.", "output");
            }
            this.bw = new BitWriter(this.output);
            this.blockSize100k = blockSize;
            this.compressor = new BZip2Compressor(this.bw, blockSize);
            this.leaveOpen = leaveOpen;
            this.combinedCRC = 0;
            this.EmitHeader();
        }

        /// <summary>
        /// Close the stream.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This may or may not close the underlying stream.  Check the
        /// constructors that accept a bool value.
        /// </para>
        /// </remarks>
        public override void Close()
        {
            if (this.output != null)
            {
                Stream output = this.output;
                this.Finish();
                if (!this.leaveOpen)
                {
                    output.Close();
                }
            }
        }

        private void EmitHeader()
        {
            byte[] buffer2 = new byte[] { 0x42, 90, 0x68, 0 };
            buffer2[3] = (byte) (0x30 + this.blockSize100k);
            byte[] buffer = buffer2;
            this.output.Write(buffer, 0, buffer.Length);
        }

        private void EmitTrailer()
        {
            this.bw.WriteByte(0x17);
            this.bw.WriteByte(0x72);
            this.bw.WriteByte(0x45);
            this.bw.WriteByte(0x38);
            this.bw.WriteByte(80);
            this.bw.WriteByte(0x90);
            this.bw.WriteInt(this.combinedCRC);
            this.bw.FinishAndPad();
        }

        private void Finish()
        {
            try
            {
                int totalBytesWrittenOut = this.bw.TotalBytesWrittenOut;
                this.compressor.CompressAndWrite();
                this.combinedCRC = (this.combinedCRC << 1) | (this.combinedCRC >> 0x1f);
                this.combinedCRC ^= this.compressor.Crc32;
                this.EmitTrailer();
            }
            finally
            {
                this.output = null;
                this.compressor = null;
                this.bw = null;
            }
        }

        /// <summary>
        /// Flush the stream.
        /// </summary>
        public override void Flush()
        {
            if (this.output != null)
            {
                this.bw.Flush();
                this.output.Flush();
            }
        }

        /// <summary>
        /// Calling this method always throws a <see cref="T:System.NotImplementedException" />.
        /// </summary>
        /// <param name="buffer">this parameter is never used</param>
        /// <param name="offset">this parameter is never used</param>
        /// <param name="count">this parameter is never used</param>
        /// <returns>never returns anything; always throws</returns>
        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
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

        [Conditional("Trace")]
        private void TraceOutput(TraceBits bits, string format, params object[] varParams)
        {
            if ((bits & this.desiredTrace) != TraceBits.None)
            {
                int hashCode = Thread.CurrentThread.GetHashCode();
                Console.ForegroundColor = (ConsoleColor) ((hashCode % 8) + 10);
                Console.Write("{0:000} PBOS ", hashCode);
                Console.WriteLine(format, varParams);
                Console.ResetColor();
            }
        }

        /// <summary>
        /// Write data to the stream.
        /// </summary>
        /// <remarks>
        /// 
        /// <para>
        /// Use the <c>BZip2OutputStream</c> to compress data while writing:
        /// create a <c>BZip2OutputStream</c> with a writable output stream.
        /// Then call <c>Write()</c> on that <c>BZip2OutputStream</c>, providing
        /// uncompressed data as input.  The data sent to the output stream will
        /// be the compressed form of the input data.
        /// </para>
        /// 
        /// <para>
        /// A <c>BZip2OutputStream</c> can be used only for <c>Write()</c> not for <c>Read()</c>.
        /// </para>
        /// 
        /// </remarks>
        /// 
        /// <param name="buffer">The buffer holding data to write to the stream.</param>
        /// <param name="offset">the offset within that data array to find the first byte to write.</param>
        /// <param name="count">the number of bytes to write.</param>
        public override void Write(byte[] buffer, int offset, int count)
        {
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
            if (this.output == null)
            {
                throw new IOException("the stream is not open");
            }
            if (count != 0)
            {
                int num = 0;
                int num2 = count;
                do
                {
                    int num3 = this.compressor.Fill(buffer, offset, num2);
                    if (num3 != num2)
                    {
                        int totalBytesWrittenOut = this.bw.TotalBytesWrittenOut;
                        this.compressor.CompressAndWrite();
                        this.combinedCRC = (this.combinedCRC << 1) | (this.combinedCRC >> 0x1f);
                        this.combinedCRC ^= this.compressor.Crc32;
                        offset += num3;
                    }
                    num2 -= num3;
                    num += num3;
                }
                while (num2 > 0);
                this.totalBytesWrittenIn += num;
            }
        }

        /// <summary>
        /// The blocksize parameter specified at construction time.
        /// </summary>
        public int BlockSize
        {
            get
            {
                return this.blockSize100k;
            }
        }

        /// <summary>
        /// Indicates whether the stream can be read.
        /// </summary>
        /// <remarks>
        /// The return value is always false.
        /// </remarks>
        public override bool CanRead
        {
            get
            {
                return false;
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
        /// The return value should always be true, unless and until the
        /// object is disposed and closed.
        /// </remarks>
        public override bool CanWrite
        {
            get
            {
                if (this.output == null)
                {
                    throw new ObjectDisposedException("BZip2Stream");
                }
                return this.output.CanWrite;
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
        /// total number of uncompressed bytes written through.
        /// </remarks>
        public override long Position
        {
            get
            {
                return (long) this.totalBytesWrittenIn;
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        [Flags]
        private enum TraceBits : uint
        {
            All = 0xffffffff,
            Crc = 1,
            None = 0,
            Write = 2
        }
    }
}

