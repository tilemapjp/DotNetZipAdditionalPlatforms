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
    /// This class is similar to <see cref="T:BZip2OutputStream" />,
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
    /// <seealso cref="T:BZip2OutputStream" />
    public class ParallelBZip2OutputStream : Stream
    {
        private int maxWorkersField;
        private int blockSize100kField;
        private const int BufferPairsPerCore = 4;
        private BitWriter bitWriterField;
        private uint combinedCrcField;
        private int currentlyFillingField;
        private TraceBits desiredTraceField;
        private object lockObjectField;
        private bool emittingField;
        private bool firstWriteDoneField;
        private bool handlingExceptionField;
        private int lastFilledField;
        private int lastWrittenField;
        private int latestCompressedField;
        private object latestLockField;
        private bool leaveOpenField;
        private AutoResetEvent newlyCompressedBlobField;
        private Stream outputStreamField;
        private object outputLockField;
        private volatile Exception pendingExceptionField;
        private List<WorkItem> poolField;
        private Queue<int> queueToFillField;
        private long totalBytesWrittenInField;
        private long totalBytesWrittenOutField;
        private Queue<int> queueToWriteField;

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
        /// using (var compressor = new ParallelBZip2OutputStream(output))
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
        public ParallelBZip2OutputStream(Stream output) : this(output, BZip2.MaxBlockSize, false)
        {
        }

        /// <summary>
        /// Constructs a new <c>ParallelBZip2OutputStream</c>.
        /// </summary>
        /// <param name="output">the destination stream.</param>
        /// <param name="leaveOpen">
        /// whether to leave the captive stream open upon closing this stream.
        /// </param>
        public ParallelBZip2OutputStream(Stream output, bool leaveOpen) : this(output, BZip2.MaxBlockSize, leaveOpen)
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
            this.latestLockField = new object();
            this.lockObjectField = new object();
            this.outputLockField = new object();
            this.desiredTraceField = TraceBits.None | TraceBits.Crc | TraceBits.Write;
            if ((blockSize < BZip2.MinBlockSize) || (blockSize > BZip2.MaxBlockSize))
            {
                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, "blockSize={0} is out of range; must be between {1} and {2}", blockSize, BZip2.MinBlockSize, BZip2.MaxBlockSize), "blockSize");
            }
            this.outputStreamField = output;
            if (!this.outputStreamField.CanWrite)
            {
                throw new ArgumentException("The stream is not writable.", "output");
            }
            this.bitWriterField = new BitWriter(this.outputStreamField);
            this.blockSize100kField = blockSize;
            this.leaveOpenField = leaveOpen;
            this.combinedCrcField = 0;
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
            if (this.pendingExceptionField != null)
            {
                this.handlingExceptionField = true;
                Exception pendingException = this.pendingExceptionField;
                this.pendingExceptionField = null;
                throw pendingException;
            }
            if (!this.handlingExceptionField && (this.outputStreamField != null))
            {
                Stream output = this.outputStreamField;
                try
                {
                    this.FlushOutput(true);
                }
                finally
                {
                    this.outputStreamField = null;
                    this.bitWriterField = null;
                }
                if (!this.leaveOpenField)
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
                lock ((obj2 = this.latestLockField))
                {
                    if (item.ordinalField > this.latestCompressedField)
                    {
                        this.latestCompressedField = item.ordinalField;
                    }
                }
                lock (this.queueToWriteField)
                {
                    this.queueToWriteField.Enqueue(item.indexField);
                }
                this.newlyCompressedBlobField.Set();
            }
            catch (Exception exception)
            {
                lock ((obj2 = this.lockObjectField))
                {
                    if (this.pendingExceptionField != null)
                    {
                        this.pendingExceptionField = exception;
                    }
                }
            }
        }

        private void EmitHeader()
        {
            byte[] buffer2 = new byte[] { 0x42, 90, 0x68, 0 };
            buffer2[3] = (byte) (0x30 + this.blockSize100kField);
            byte[] buffer = buffer2;
            this.outputStreamField.Write(buffer, 0, buffer.Length);
        }

        private void EmitPendingBuffers(bool doAll, bool mustWait)
        {
            if (!this.emittingField)
            {
                this.emittingField = true;
                if (doAll || mustWait)
                {
                    this.newlyCompressedBlobField.WaitOne();
                }
                do
                {
                    int num = -1;
                    int millisecondsTimeout = doAll ? 200 : (mustWait ? -1 : 0);
                    int num3 = -1;
                    do
                    {
                        if (Monitor.TryEnter(this.queueToWriteField, millisecondsTimeout))
                        {
                            num3 = -1;
                            try
                            {
                                if (this.queueToWriteField.Count > 0)
                                {
                                    num3 = this.queueToWriteField.Dequeue();
                                }
                            }
                            finally
                            {
                                Monitor.Exit(this.queueToWriteField);
                            }
                            if (num3 >= 0)
                            {
                                WorkItem item = this.poolField[num3];
                                if (item.ordinalField != (this.lastWrittenField + 1))
                                {
                                    lock (this.queueToWriteField)
                                    {
                                        this.queueToWriteField.Enqueue(num3);
                                    }
                                    if (num == num3)
                                    {
                                        this.newlyCompressedBlobField.WaitOne();
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
                                    BitWriter bw = item.bitWriterField;
                                    bw.Flush();
                                    MemoryStream ms = item.memoryStreamField;
                                    ms.Seek(0L, SeekOrigin.Begin);
                                    int num5 = -1;
                                    long num6 = 0L;
                                    byte[] buffer = new byte[0x400];
                                    while ((num4 = ms.Read(buffer, 0, buffer.Length)) > 0)
                                    {
                                        num5 = num4;
                                        for (int i = 0; i < num4; i++)
                                        {
                                            this.bitWriterField.WriteByte(buffer[i]);
                                        }
                                        num6 += num4;
                                    }
                                    if (bw.NumRemainingBits > 0)
                                    {
                                        this.bitWriterField.WriteBits(bw.NumRemainingBits, bw.RemainingBits);
                                    }
                                    this.combinedCrcField = (this.combinedCrcField << 1) | (this.combinedCrcField >> 0x1f);
                                    this.combinedCrcField ^= item.Compressor.Crc32;
                                    this.totalBytesWrittenOutField += num6;
                                    bw.Reset();
                                    this.lastWrittenField = item.ordinalField;
                                    item.ordinalField = -1;
                                    this.queueToFillField.Enqueue(item.indexField);
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
                while (doAll && (this.lastWrittenField != this.latestCompressedField));
                if (doAll)
                {
                }
                this.emittingField = false;
            }
        }

        private void EmitTrailer()
        {
            this.bitWriterField.WriteByte(0x17);
            this.bitWriterField.WriteByte(0x72);
            this.bitWriterField.WriteByte(0x45);
            this.bitWriterField.WriteByte(0x38);
            this.bitWriterField.WriteByte(80);
            this.bitWriterField.WriteByte(0x90);
            this.bitWriterField.WriteInt(this.combinedCrcField);
            this.bitWriterField.FinishAndPad();
        }

        /// <summary>
        /// Flush the stream.
        /// </summary>
        public override void Flush()
        {
            if (this.outputStreamField != null)
            {
                this.FlushOutput(false);
                this.bitWriterField.Flush();
                this.outputStreamField.Flush();
            }
        }

        private void FlushOutput(bool lastInput)
        {
            if (!this.emittingField)
            {
                if (this.currentlyFillingField >= 0)
                {
                    WorkItem wi = this.poolField[this.currentlyFillingField];
                    this.CompressOne(wi);
                    this.currentlyFillingField = -1;
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
            this.queueToWriteField = new Queue<int>();
            this.queueToFillField = new Queue<int>();
            this.poolField = new List<WorkItem>();
            int num = BufferPairsPerCore * Environment.ProcessorCount;
            num = Math.Min(num, this.MaxWorkers);
            for (int i = 0; i < num; i++)
            {
                this.poolField.Add(new WorkItem(i, this.blockSize100kField));
                this.queueToFillField.Enqueue(i);
            }
            this.newlyCompressedBlobField = new AutoResetEvent(false);
            this.currentlyFillingField = -1;
            this.lastFilledField = -1;
            this.lastWrittenField = -1;
            this.latestCompressedField = -1;
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
            if ((bits & this.desiredTraceField) != TraceBits.None)
            {
                lock (this.outputLockField)
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
            if (this.outputStreamField == null)
            {
                throw new IOException("the stream is not open");
            }
            if (this.pendingExceptionField != null)
            {
                this.handlingExceptionField = true;
                Exception pendingException = this.pendingExceptionField;
                this.pendingExceptionField = null;
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
            if (!this.firstWriteDoneField)
            {
                this.InitializePoolOfWorkItems();
                this.firstWriteDoneField = true;
            }
            int num = 0;
            int num2 = count;
        Label_00FA:
            this.EmitPendingBuffers(false, mustWait);
            mustWait = false;
            int currentlyFilling = -1;
            if (this.currentlyFillingField >= 0)
            {
                currentlyFilling = this.currentlyFillingField;
            }
            else
            {
                if (this.queueToFillField.Count == 0)
                {
                    mustWait = true;
                    goto Label_01E0;
                }
                currentlyFilling = this.queueToFillField.Dequeue();
                this.lastFilledField++;
            }
            WorkItem state = this.poolField[currentlyFilling];
            state.ordinalField = this.lastFilledField;
            int num4 = state.Compressor.Fill(buffer, offset, num2);
            if (num4 != num2)
            {
                if (!ThreadPool.QueueUserWorkItem(new WaitCallback(this.CompressOne), state))
                {
                    throw new Exception("Cannot enqueue workitem");
                }
                this.currentlyFillingField = -1;
                offset += num4;
            }
            else
            {
                this.currentlyFillingField = currentlyFilling;
            }
            num2 -= num4;
            num += num4;
        Label_01E0:
            if (num2 > 0)
            {
                goto Label_00FA;
            }
            this.totalBytesWrittenInField += num;
        }

        /// <summary>
        /// The blocksize parameter specified at construction time.
        /// </summary>
        public int BlockSize
        {
            get
            {
                return this.blockSize100kField;
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
                return this.totalBytesWrittenOutField;
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
                if (this.outputStreamField == null)
                {
                    throw new ObjectDisposedException("BZip2Stream");
                }
                return this.outputStreamField.CanWrite;
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
        /// actual number depends on the <see cref="P:ParallelBZip2OutputStream.BlockSize" /> property.
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
                return this.maxWorkersField;
            }
            set
            {
                if (value < 4)
                {
                    throw new ArgumentException("MaxWorkers", "Value must be 4 or greater.");
                }
                this.maxWorkersField = value;
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
                return this.totalBytesWrittenInField;
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

