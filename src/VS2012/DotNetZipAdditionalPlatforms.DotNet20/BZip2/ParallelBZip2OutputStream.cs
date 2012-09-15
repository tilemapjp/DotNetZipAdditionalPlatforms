namespace DotNetZipAdditionalPlatforms.BZip2
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Runtime.CompilerServices;
    using System.Threading;

    /// <summary>
    /// A write-only decorator stream that compresses data as it is
    /// written using the BZip2 algorithm. This stream compresses by
    /// block using multiple threads.
    /// </summary>
    /// <para>
    /// This class performs BZIP2 compression through writing.  For
    /// more information on the BZIP2 algorithm, see
    /// <see href="http://en.wikipedia.org/wiki/BZIP2" />.
    /// </para>
    /// 
    /// <para>
    /// This class is similar to <see cref="T:DotNetZipAdditionalPlatforms.BZip2.BZip2OutputStream" />,
    /// except that this implementation uses an approach that employs multiple
    /// worker threads to perform the compression.  On a multi-cpu or multi-core
    /// computer, the performance of this class can be significantly higher than
    /// the single-threaded BZip2OutputStream, particularly for larger streams.
    /// How large?  Anything over 10mb is a good candidate for parallel
    /// compression.
    /// </para>
    /// 
    /// <para>
    /// The tradeoff is that this class uses more memory and more CPU than the
    /// vanilla <c>BZip2OutputStream</c>. Also, for small files, the
    /// <c>ParallelBZip2OutputStream</c> can be much slower than the vanilla
    /// <c>BZip2OutputStream</c>, because of the overhead associated to using the
    /// thread pool.
    /// </para>
    /// 
    /// <seealso cref="T:DotNetZipAdditionalPlatforms.BZip2.BZip2OutputStream" />
    public class ParallelBZip2OutputStream : Stream
    {
        private int _maxWorkers;
        private int blockSize100k;
        private const int BufferPairsPerCore = 4;
        private BitWriter bw;
        private uint combinedCRC;
        private int currentlyFilling;
        private TraceBits desiredTrace;
        private object eLock;
        private bool emitting;
        private bool firstWriteDone;
        private bool handlingException;
        private int lastFilled;
        private int lastWritten;
        private int latestCompressed;
        private object latestLock;
        private bool leaveOpen;
        private AutoResetEvent newlyCompressedBlob;
        private Stream output;
        private object outputLock;
        private volatile Exception pendingException;
        private List<WorkItem> pool;
        private Queue<int> toFill;
        private long totalBytesWrittenIn;
        private long totalBytesWrittenOut;
        private Queue<int> toWrite;

        /// <summary>
        /// Constructs a new <c>ParallelBZip2OutputStream</c>, that sends its
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
        /// using (var compressor = new DotNetZipAdditionalPlatforms.BZip2.ParallelBZip2OutputStream(output))
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
        public ParallelBZip2OutputStream(Stream output) : this(output, DotNetZipAdditionalPlatforms.BZip2.BZip2.MaxBlockSize, false)
        {
        }

        /// <summary>
        /// Constructs a new <c>ParallelBZip2OutputStream</c>.
        /// </summary>
        /// <param name="output">the destination stream.</param>
        /// <param name="leaveOpen">
        /// whether to leave the captive stream open upon closing this stream.
        /// </param>
        public ParallelBZip2OutputStream(Stream output, bool leaveOpen) : this(output, DotNetZipAdditionalPlatforms.BZip2.BZip2.MaxBlockSize, leaveOpen)
        {
        }

        /// <summary>
        /// Constructs a new <c>ParallelBZip2OutputStream</c> with specified blocksize.
        /// </summary>
        /// <param name="output">the destination stream.</param>
        /// <param name="blockSize">
        /// The blockSize in units of 100000 bytes.
        /// The valid range is 1..9.
        /// </param>
        public ParallelBZip2OutputStream(Stream output, int blockSize) : this(output, blockSize, false)
        {
        }

        /// <summary>
        /// Constructs a new <c>ParallelBZip2OutputStream</c> with specified blocksize,
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
        public ParallelBZip2OutputStream(Stream output, int blockSize, bool leaveOpen)
        {
            this.latestLock = new object();
            this.eLock = new object();
            this.outputLock = new object();
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
            this.leaveOpen = leaveOpen;
            this.combinedCRC = 0;
            this.MaxWorkers = 0x10;
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
            if (this.pendingException != null)
            {
                this.handlingException = true;
                Exception pendingException = this.pendingException;
                this.pendingException = null;
                throw pendingException;
            }
            if (!this.handlingException && (this.output != null))
            {
                Stream output = this.output;
                try
                {
                    this.FlushOutput(true);
                }
                finally
                {
                    this.output = null;
                    this.bw = null;
                }
                if (!this.leaveOpen)
                {
                    output.Close();
                }
            }
        }

        private void CompressOne(object wi)
        {
            object obj2;
            WorkItem item = (WorkItem) wi;
            try
            {
                item.Compressor.CompressAndWrite();
                lock ((obj2 = this.latestLock))
                {
                    if (item.ordinal > this.latestCompressed)
                    {
                        this.latestCompressed = item.ordinal;
                    }
                }
                lock (this.toWrite)
                {
                    this.toWrite.Enqueue(item.index);
                }
                this.newlyCompressedBlob.Set();
            }
            catch (Exception exception)
            {
                lock ((obj2 = this.eLock))
                {
                    if (this.pendingException != null)
                    {
                        this.pendingException = exception;
                    }
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

        private void EmitPendingBuffers(bool doAll, bool mustWait)
        {
            if (!this.emitting)
            {
                this.emitting = true;
                if (doAll || mustWait)
                {
                    this.newlyCompressedBlob.WaitOne();
                }
                do
                {
                    int num = -1;
                    int millisecondsTimeout = doAll ? 200 : (mustWait ? -1 : 0);
                    int num3 = -1;
                    do
                    {
                        if (Monitor.TryEnter(this.toWrite, millisecondsTimeout))
                        {
                            num3 = -1;
                            try
                            {
                                if (this.toWrite.Count > 0)
                                {
                                    num3 = this.toWrite.Dequeue();
                                }
                            }
                            finally
                            {
                                Monitor.Exit(this.toWrite);
                            }
                            if (num3 >= 0)
                            {
                                WorkItem item = this.pool[num3];
                                if (item.ordinal != (this.lastWritten + 1))
                                {
                                    lock (this.toWrite)
                                    {
                                        this.toWrite.Enqueue(num3);
                                    }
                                    if (num == num3)
                                    {
                                        this.newlyCompressedBlob.WaitOne();
                                        num = -1;
                                    }
                                    else if (num == -1)
                                    {
                                        num = num3;
                                    }
                                }
                                else
                                {
                                    int num4;
                                    num = -1;
                                    BitWriter bw = item.bw;
                                    bw.Flush();
                                    MemoryStream ms = item.ms;
                                    ms.Seek(0L, SeekOrigin.Begin);
                                    int num5 = -1;
                                    long num6 = 0L;
                                    byte[] buffer = new byte[0x400];
                                    while ((num4 = ms.Read(buffer, 0, buffer.Length)) > 0)
                                    {
                                        num5 = num4;
                                        for (int i = 0; i < num4; i++)
                                        {
                                            this.bw.WriteByte(buffer[i]);
                                        }
                                        num6 += num4;
                                    }
                                    if (bw.NumRemainingBits > 0)
                                    {
                                        this.bw.WriteBits(bw.NumRemainingBits, bw.RemainingBits);
                                    }
                                    this.combinedCRC = (this.combinedCRC << 1) | (this.combinedCRC >> 0x1f);
                                    this.combinedCRC ^= item.Compressor.Crc32;
                                    this.totalBytesWrittenOut += num6;
                                    bw.Reset();
                                    this.lastWritten = item.ordinal;
                                    item.ordinal = -1;
                                    this.toFill.Enqueue(item.index);
                                    if (millisecondsTimeout == -1)
                                    {
                                        millisecondsTimeout = 0;
                                    }
                                }
                            }
                        }
                        else
                        {
                            num3 = -1;
                        }
                    }
                    while (num3 >= 0);
                }
                while (doAll && (this.lastWritten != this.latestCompressed));
                if (doAll)
                {
                }
                this.emitting = false;
            }
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

        /// <summary>
        /// Flush the stream.
        /// </summary>
        public override void Flush()
        {
            if (this.output != null)
            {
                this.FlushOutput(false);
                this.bw.Flush();
                this.output.Flush();
            }
        }

        private void FlushOutput(bool lastInput)
        {
            if (!this.emitting)
            {
                if (this.currentlyFilling >= 0)
                {
                    WorkItem wi = this.pool[this.currentlyFilling];
                    this.CompressOne(wi);
                    this.currentlyFilling = -1;
                }
                if (lastInput)
                {
                    this.EmitPendingBuffers(true, false);
                    this.EmitTrailer();
                }
                else
                {
                    this.EmitPendingBuffers(false, false);
                }
            }
        }

        private void InitializePoolOfWorkItems()
        {
            this.toWrite = new Queue<int>();
            this.toFill = new Queue<int>();
            this.pool = new List<WorkItem>();
            int num = BufferPairsPerCore * Environment.ProcessorCount;
            num = Math.Min(num, this.MaxWorkers);
            for (int i = 0; i < num; i++)
            {
                this.pool.Add(new WorkItem(i, this.blockSize100k));
                this.toFill.Enqueue(i);
            }
            this.newlyCompressedBlob = new AutoResetEvent(false);
            this.currentlyFilling = -1;
            this.lastFilled = -1;
            this.lastWritten = -1;
            this.latestCompressed = -1;
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
                lock (this.outputLock)
                {
                    int hashCode = Thread.CurrentThread.GetHashCode();
                    Console.ForegroundColor = (ConsoleColor) ((hashCode % 8) + 10);
                    Console.Write("{0:000} PBOS ", hashCode);
                    Console.WriteLine(format, varParams);
                    Console.ResetColor();
                }
            }
        }

        /// <summary>
        /// Write data to the stream.
        /// </summary>
        /// <remarks>
        /// 
        /// <para>
        /// Use the <c>ParallelBZip2OutputStream</c> to compress data while
        /// writing: create a <c>ParallelBZip2OutputStream</c> with a writable
        /// output stream.  Then call <c>Write()</c> on that
        /// <c>ParallelBZip2OutputStream</c>, providing uncompressed data as
        /// input.  The data sent to the output stream will be the compressed
        /// form of the input data.
        /// </para>
        /// 
        /// <para>
        /// A <c>ParallelBZip2OutputStream</c> can be used only for
        /// <c>Write()</c> not for <c>Read()</c>.
        /// </para>
        /// 
        /// </remarks>
        /// 
        /// <param name="buffer">The buffer holding data to write to the stream.</param>
        /// <param name="offset">the offset within that data array to find the first byte to write.</param>
        /// <param name="count">the number of bytes to write.</param>
        public override void Write(byte[] buffer, int offset, int count)
        {
            bool mustWait = false;
            if (this.output == null)
            {
                throw new IOException("the stream is not open");
            }
            if (this.pendingException != null)
            {
                this.handlingException = true;
                Exception pendingException = this.pendingException;
                this.pendingException = null;
                throw pendingException;
            }
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
            if (count == 0)
            {
                return;
            }
            if (!this.firstWriteDone)
            {
                this.InitializePoolOfWorkItems();
                this.firstWriteDone = true;
            }
            int num = 0;
            int num2 = count;
        Label_00FA:
            this.EmitPendingBuffers(false, mustWait);
            mustWait = false;
            int currentlyFilling = -1;
            if (this.currentlyFilling >= 0)
            {
                currentlyFilling = this.currentlyFilling;
            }
            else
            {
                if (this.toFill.Count == 0)
                {
                    mustWait = true;
                    goto Label_01E0;
                }
                currentlyFilling = this.toFill.Dequeue();
                this.lastFilled++;
            }
            WorkItem state = this.pool[currentlyFilling];
            state.ordinal = this.lastFilled;
            int num4 = state.Compressor.Fill(buffer, offset, num2);
            if (num4 != num2)
            {
                if (!ThreadPool.QueueUserWorkItem(new WaitCallback(this.CompressOne), state))
                {
                    throw new Exception("Cannot enqueue workitem");
                }
                this.currentlyFilling = -1;
                offset += num4;
            }
            else
            {
                this.currentlyFilling = currentlyFilling;
            }
            num2 -= num4;
            num += num4;
        Label_01E0:
            if (num2 > 0)
            {
                goto Label_00FA;
            }
            this.totalBytesWrittenIn += num;
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
        /// The total number of bytes written out by the stream.
        /// </summary>
        /// <remarks>
        /// This value is meaningful only after a call to Close().
        /// </remarks>
        public long BytesWrittenOut
        {
            get
            {
                return this.totalBytesWrittenOut;
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
        /// The return value depends on whether the captive stream supports writing.
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
        /// The maximum number of concurrent compression worker threads to use.
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// This property sets an upper limit on the number of concurrent worker
        /// threads to employ for compression. The implementation of this stream
        /// employs multiple threads from the .NET thread pool, via <see cref="M:System.Threading.ThreadPool.QueueUserWorkItem(System.Threading.WaitCallback)">
        /// ThreadPool.QueueUserWorkItem()</see>, to compress the incoming data by
        /// block.  As each block of data is compressed, this stream re-orders the
        /// compressed blocks and writes them to the output stream.
        /// </para>
        /// 
        /// <para>
        /// A higher number of workers enables a higher degree of
        /// parallelism, which tends to increase the speed of compression on
        /// multi-cpu computers.  On the other hand, a higher number of buffer
        /// pairs also implies a larger memory consumption, more active worker
        /// threads, and a higher cpu utilization for any compression. This
        /// property enables the application to limit its memory consumption and
        /// CPU utilization behavior depending on requirements.
        /// </para>
        /// 
        /// <para>
        /// By default, DotNetZip allocates 4 workers per CPU core, subject to the
        /// upper limit specified in this property. For example, suppose the
        /// application sets this property to 16.  Then, on a machine with 2
        /// cores, DotNetZip will use 8 workers; that number does not exceed the
        /// upper limit specified by this property, so the actual number of
        /// workers used will be 4 * 2 = 8.  On a machine with 4 cores, DotNetZip
        /// will use 16 workers; again, the limit does not apply. On a machine
        /// with 8 cores, DotNetZip will use 16 workers, because of the limit.
        /// </para>
        /// 
        /// <para>
        /// For each compression "worker thread" that occurs in parallel, there is
        /// up to 2mb of memory allocated, for buffering and processing. The
        /// actual number depends on the <see cref="P:DotNetZipAdditionalPlatforms.BZip2.ParallelBZip2OutputStream.BlockSize" /> property.
        /// </para>
        /// 
        /// <para>
        /// CPU utilization will also go up with additional workers, because a
        /// larger number of buffer pairs allows a larger number of background
        /// threads to compress in parallel. If you find that parallel
        /// compression is consuming too much memory or CPU, you can adjust this
        /// value downward.
        /// </para>
        /// 
        /// <para>
        /// The default value is 16. Different values may deliver better or
        /// worse results, depending on your priorities and the dynamic
        /// performance characteristics of your storage and compute resources.
        /// </para>
        /// 
        /// <para>
        /// The application can set this value at any time, but it is effective
        /// only before the first call to Write(), which is when the buffers are
        /// allocated.
        /// </para>
        /// </remarks>
        public int MaxWorkers
        {
            get
            {
                return this._maxWorkers;
            }
            set
            {
                if (value < 4)
                {
                    throw new ArgumentException("MaxWorkers", "Value must be 4 or greater.");
                }
                this._maxWorkers = value;
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
                return this.totalBytesWrittenIn;
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

