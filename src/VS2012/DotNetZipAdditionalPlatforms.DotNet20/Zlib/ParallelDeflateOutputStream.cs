namespace DotNetZipAdditionalPlatforms.Zlib
{
    using DotNetZipAdditionalPlatforms.Crc;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Runtime.CompilerServices;
    using System.Threading;

    /// <summary>
    /// A class for compressing streams using the
    /// Deflate algorithm with multiple threads.
    /// </summary>
    /// 
    /// <remarks>
    /// <para>
    /// This class performs DEFLATE compression through writing.  For
    /// more information on the Deflate algorithm, see IETF RFC 1951,
    /// "DEFLATE Compressed Data Format Specification version 1.3."
    /// </para>
    /// 
    /// <para>
    /// This class is similar to <see cref="T:DotNetZipAdditionalPlatforms.Zlib.DeflateStream" />, except
    /// that this class is for compression only, and this implementation uses an
    /// approach that employs multiple worker threads to perform the DEFLATE.  On
    /// a multi-cpu or multi-core computer, the performance of this class can be
    /// significantly higher than the single-threaded DeflateStream, particularly
    /// for larger streams.  How large?  Anything over 10mb is a good candidate
    /// for parallel compression.
    /// </para>
    /// 
    /// <para>
    /// The tradeoff is that this class uses more memory and more CPU than the
    /// vanilla DeflateStream, and also is less efficient as a compressor. For
    /// large files the size of the compressed data stream can be less than 1%
    /// larger than the size of a compressed data stream from the vanialla
    /// DeflateStream.  For smaller files the difference can be larger.  The
    /// difference will also be larger if you set the BufferSize to be lower than
    /// the default value.  Your mileage may vary. Finally, for small files, the
    /// ParallelDeflateOutputStream can be much slower than the vanilla
    /// DeflateStream, because of the overhead associated to using the thread
    /// pool.
    /// </para>
    /// 
    /// </remarks>
    /// <seealso cref="T:DotNetZipAdditionalPlatforms.Zlib.DeflateStream" />
    public class ParallelDeflateOutputStream : Stream
    {
        private int _bufferSize;
        private CompressionLevel _compressLevel;
        private int _Crc32;
        private int _currentlyFilling;
        private TraceBits _DesiredTrace;
        private object _eLock;
        private bool _firstWriteDone;
        private bool _handlingException;
        private bool _isClosed;
        private int _lastFilled;
        private int _lastWritten;
        private int _latestCompressed;
        private object _latestLock;
        private bool _leaveOpen;
        private int _maxBufferPairs;
        private AutoResetEvent _newlyCompressedBlob;
        private object _outputLock;
        private Stream _outStream;
        private volatile Exception _pendingException;
        private List<WorkItem> _pool;
        private CRC32 _runningCrc;
        private Queue<int> _toFill;
        private long _totalBytesProcessed;
        private Queue<int> _toWrite;
        private const int BufferPairsPerCore = 4;
        private bool emitting;
        private const int IO_BUFFER_SIZE_DEFAULT = 0x10000;

        /// <summary>
        /// Create a ParallelDeflateOutputStream.
        /// </summary>
        /// <remarks>
        /// 
        /// <para>
        /// This stream compresses data written into it via the DEFLATE
        /// algorithm (see RFC 1951), and writes out the compressed byte stream.
        /// </para>
        /// 
        /// <para>
        /// The instance will use the default compression level, the default
        /// buffer sizes and the default number of threads and buffers per
        /// thread.
        /// </para>
        /// 
        /// <para>
        /// This class is similar to <see cref="T:DotNetZipAdditionalPlatforms.Zlib.DeflateStream" />,
        /// except that this implementation uses an approach that employs
        /// multiple worker threads to perform the DEFLATE.  On a multi-cpu or
        /// multi-core computer, the performance of this class can be
        /// significantly higher than the single-threaded DeflateStream,
        /// particularly for larger streams.  How large?  Anything over 10mb is
        /// a good candidate for parallel compression.
        /// </para>
        /// 
        /// </remarks>
        /// 
        /// <example>
        /// 
        /// This example shows how to use a ParallelDeflateOutputStream to compress
        /// data.  It reads a file, compresses it, and writes the compressed data to
        /// a second, output file.
        /// 
        /// <code>
        /// byte[] buffer = new byte[WORKING_BUFFER_SIZE];
        /// int n= -1;
        /// String outputFile = fileToCompress + ".compressed";
        /// using (System.IO.Stream input = System.IO.File.OpenRead(fileToCompress))
        /// {
        /// using (var raw = System.IO.File.Create(outputFile))
        /// {
        /// using (Stream compressor = new ParallelDeflateOutputStream(raw))
        /// {
        /// while ((n= input.Read(buffer, 0, buffer.Length)) != 0)
        /// {
        /// compressor.Write(buffer, 0, n);
        /// }
        /// }
        /// }
        /// }
        /// </code>
        /// <code lang="VB">
        /// Dim buffer As Byte() = New Byte(4096) {}
        /// Dim n As Integer = -1
        /// Dim outputFile As String = (fileToCompress &amp; ".compressed")
        /// Using input As Stream = File.OpenRead(fileToCompress)
        /// Using raw As FileStream = File.Create(outputFile)
        /// Using compressor As Stream = New ParallelDeflateOutputStream(raw)
        /// Do While (n &lt;&gt; 0)
        /// If (n &gt; 0) Then
        /// compressor.Write(buffer, 0, n)
        /// End If
        /// n = input.Read(buffer, 0, buffer.Length)
        /// Loop
        /// End Using
        /// End Using
        /// End Using
        /// </code>
        /// </example>
        /// <param name="stream">The stream to which compressed data will be written.</param>
        public ParallelDeflateOutputStream(Stream stream) : this(stream, CompressionLevel.Default, CompressionStrategy.Default, false)
        {
        }

        /// <summary>
        /// Create a ParallelDeflateOutputStream using the specified CompressionLevel.
        /// </summary>
        /// <remarks>
        /// See the <see cref="M:DotNetZipAdditionalPlatforms.Zlib.ParallelDeflateOutputStream.#ctor(System.IO.Stream)" />
        /// constructor for example code.
        /// </remarks>
        /// <param name="stream">The stream to which compressed data will be written.</param>
        /// <param name="level">A tuning knob to trade speed for effectiveness.</param>
        public ParallelDeflateOutputStream(Stream stream, CompressionLevel level) : this(stream, level, CompressionStrategy.Default, false)
        {
        }

        /// <summary>
        /// Create a ParallelDeflateOutputStream and specify whether to leave the captive stream open
        /// when the ParallelDeflateOutputStream is closed.
        /// </summary>
        /// <remarks>
        /// See the <see cref="M:DotNetZipAdditionalPlatforms.Zlib.ParallelDeflateOutputStream.#ctor(System.IO.Stream)" />
        /// constructor for example code.
        /// </remarks>
        /// <param name="stream">The stream to which compressed data will be written.</param>
        /// <param name="leaveOpen">
        /// true if the application would like the stream to remain open after inflation/deflation.
        /// </param>
        public ParallelDeflateOutputStream(Stream stream, bool leaveOpen) : this(stream, CompressionLevel.Default, CompressionStrategy.Default, leaveOpen)
        {
        }

        /// <summary>
        /// Create a ParallelDeflateOutputStream and specify whether to leave the captive stream open
        /// when the ParallelDeflateOutputStream is closed.
        /// </summary>
        /// <remarks>
        /// See the <see cref="M:DotNetZipAdditionalPlatforms.Zlib.ParallelDeflateOutputStream.#ctor(System.IO.Stream)" />
        /// constructor for example code.
        /// </remarks>
        /// <param name="stream">The stream to which compressed data will be written.</param>
        /// <param name="level">A tuning knob to trade speed for effectiveness.</param>
        /// <param name="leaveOpen">
        /// true if the application would like the stream to remain open after inflation/deflation.
        /// </param>
        public ParallelDeflateOutputStream(Stream stream, CompressionLevel level, bool leaveOpen) : this(stream, CompressionLevel.Default, CompressionStrategy.Default, leaveOpen)
        {
        }

        /// <summary>
        /// Create a ParallelDeflateOutputStream using the specified
        /// CompressionLevel and CompressionStrategy, and specifying whether to
        /// leave the captive stream open when the ParallelDeflateOutputStream is
        /// closed.
        /// </summary>
        /// <remarks>
        /// See the <see cref="M:DotNetZipAdditionalPlatforms.Zlib.ParallelDeflateOutputStream.#ctor(System.IO.Stream)" />
        /// constructor for example code.
        /// </remarks>
        /// <param name="stream">The stream to which compressed data will be written.</param>
        /// <param name="level">A tuning knob to trade speed for effectiveness.</param>
        /// <param name="strategy">
        /// By tweaking this parameter, you may be able to optimize the compression for
        /// data with particular characteristics.
        /// </param>
        /// <param name="leaveOpen">
        /// true if the application would like the stream to remain open after inflation/deflation.
        /// </param>
        public ParallelDeflateOutputStream(Stream stream, CompressionLevel level, CompressionStrategy strategy, bool leaveOpen)
        {
            this._bufferSize = IO_BUFFER_SIZE_DEFAULT;
            this._outputLock = new object();
            this._latestLock = new object();
            this._eLock = new object();
            this._DesiredTrace = TraceBits.WriteEnter | TraceBits.Compress | TraceBits.Session | TraceBits.WriteTake | TraceBits.EmitSkip | TraceBits.EmitDone | TraceBits.EmitBegin | TraceBits.EmitEnter | TraceBits.EmitLock;
            this._outStream = stream;
            this._compressLevel = level;
            this.Strategy = strategy;
            this._leaveOpen = leaveOpen;
            this.MaxBufferPairs = 0x10;
        }

        private void _DeflateOne(object wi)
        {
            object obj2;
            WorkItem workitem = (WorkItem) wi;
            try
            {
                int index = workitem.index;
                CRC32 crc = new CRC32();
                crc.SlurpBlock(workitem.buffer, 0, workitem.inputBytesAvailable);
                this.DeflateOneSegment(workitem);
                workitem.crc = crc.Crc32Result;
                lock ((obj2 = this._latestLock))
                {
                    if (workitem.ordinal > this._latestCompressed)
                    {
                        this._latestCompressed = workitem.ordinal;
                    }
                }
                lock (this._toWrite)
                {
                    this._toWrite.Enqueue(workitem.index);
                }
                this._newlyCompressedBlob.Set();
            }
            catch (Exception exception)
            {
                lock ((obj2 = this._eLock))
                {
                    if (this._pendingException != null)
                    {
                        this._pendingException = exception;
                    }
                }
            }
        }

        private void _Flush(bool lastInput)
        {
            if (this._isClosed)
            {
                throw new InvalidOperationException();
            }
            if (!this.emitting)
            {
                if (this._currentlyFilling >= 0)
                {
                    WorkItem wi = this._pool[this._currentlyFilling];
                    this._DeflateOne(wi);
                    this._currentlyFilling = -1;
                }
                if (lastInput)
                {
                    this.EmitPendingBuffers(true, false);
                    this._FlushFinish();
                }
                else
                {
                    this.EmitPendingBuffers(false, false);
                }
            }
        }

        private void _FlushFinish()
        {
            byte[] buffer = new byte[0x80];
            ZlibCodec codec = new ZlibCodec();
            int num = codec.InitializeDeflate(this._compressLevel, false);
            codec.InputBuffer = null;
            codec.NextIn = 0;
            codec.AvailableBytesIn = 0;
            codec.OutputBuffer = buffer;
            codec.NextOut = 0;
            codec.AvailableBytesOut = buffer.Length;
            num = codec.Deflate(FlushType.Finish);
            if ((num != 1) && (num != 0))
            {
                throw new Exception("deflating: " + codec.Message);
            }
            if ((buffer.Length - codec.AvailableBytesOut) > 0)
            {
                this._outStream.Write(buffer, 0, buffer.Length - codec.AvailableBytesOut);
            }
            codec.EndDeflate();
            this._Crc32 = this._runningCrc.Crc32Result;
        }

        private void _InitializePoolOfWorkItems()
        {
            this._toWrite = new Queue<int>();
            this._toFill = new Queue<int>();
            this._pool = new List<WorkItem>();
            int num = BufferPairsPerCore * Environment.ProcessorCount;
            num = Math.Min(num, this._maxBufferPairs);
            for (int i = 0; i < num; i++)
            {
                this._pool.Add(new WorkItem(this._bufferSize, this._compressLevel, this.Strategy, i));
                this._toFill.Enqueue(i);
            }
            this._newlyCompressedBlob = new AutoResetEvent(false);
            this._runningCrc = new CRC32();
            this._currentlyFilling = -1;
            this._lastFilled = -1;
            this._lastWritten = -1;
            this._latestCompressed = -1;
        }

        /// <summary>
        /// Close the stream.
        /// </summary>
        /// <remarks>
        /// You must call Close on the stream to guarantee that all of the data written in has
        /// been compressed, and the compressed data has been written out.
        /// </remarks>
        public override void Close()
        {
            if (this._pendingException != null)
            {
                this._handlingException = true;
                Exception exception = this._pendingException;
                this._pendingException = null;
                throw exception;
            }
            if (!this._handlingException && !this._isClosed)
            {
                this._Flush(true);
                if (!this._leaveOpen)
                {
                    this._outStream.Close();
                }
                this._isClosed = true;
            }
        }

        private bool DeflateOneSegment(WorkItem workitem)
        {
            ZlibCodec compressor = workitem.compressor;
            int num = 0;
            compressor.ResetDeflate();
            compressor.NextIn = 0;
            compressor.AvailableBytesIn = workitem.inputBytesAvailable;
            compressor.NextOut = 0;
            compressor.AvailableBytesOut = workitem.compressed.Length;
            do
            {
                compressor.Deflate(FlushType.None);
            }
            while ((compressor.AvailableBytesIn > 0) || (compressor.AvailableBytesOut == 0));
            num = compressor.Deflate(FlushType.Sync);
            workitem.compressedBytesAvailable = (int) compressor.TotalBytesOut;
            return true;
        }

        /// <summary>Dispose the object</summary>
        /// <remarks>
        /// <para>
        /// Because ParallelDeflateOutputStream is IDisposable, the
        /// application must call this method when finished using the instance.
        /// </para>
        /// <para>
        /// This method is generally called implicitly upon exit from
        /// a <c>using</c> scope in C# (<c>Using</c> in VB).
        /// </para>
        /// </remarks>
        public new void Dispose()
        {
            this.Close();
            this._pool = null;
            this.Dispose(true);
        }

        /// <summary>The Dispose method</summary>
        /// <param name="disposing">
        /// indicates whether the Dispose method was invoked by user code.
        /// </param>
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }

        private void EmitPendingBuffers(bool doAll, bool mustWait)
        {
            if (!this.emitting)
            {
                this.emitting = true;
                if (doAll || mustWait)
                {
                    this._newlyCompressedBlob.WaitOne();
                }
                do
                {
                    int num = -1;
                    int millisecondsTimeout = doAll ? 200 : (mustWait ? -1 : 0);
                    int num3 = -1;
                    do
                    {
                        if (Monitor.TryEnter(this._toWrite, millisecondsTimeout))
                        {
                            num3 = -1;
                            try
                            {
                                if (this._toWrite.Count > 0)
                                {
                                    num3 = this._toWrite.Dequeue();
                                }
                            }
                            finally
                            {
                                Monitor.Exit(this._toWrite);
                            }
                            if (num3 >= 0)
                            {
                                WorkItem item = this._pool[num3];
                                if (item.ordinal != (this._lastWritten + 1))
                                {
                                    lock (this._toWrite)
                                    {
                                        this._toWrite.Enqueue(num3);
                                    }
                                    if (num == num3)
                                    {
                                        this._newlyCompressedBlob.WaitOne();
                                        num = -1;
                                    }
                                    else if (num == -1)
                                    {
                                        num = num3;
                                    }
                                }
                                else
                                {
                                    num = -1;
                                    this._outStream.Write(item.compressed, 0, item.compressedBytesAvailable);
                                    this._runningCrc.Combine(item.crc, item.inputBytesAvailable);
                                    this._totalBytesProcessed += item.inputBytesAvailable;
                                    item.inputBytesAvailable = 0;
                                    this._lastWritten = item.ordinal;
                                    this._toFill.Enqueue(item.index);
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
                while (doAll && (this._lastWritten != this._latestCompressed));
                this.emitting = false;
            }
        }

        /// <summary>
        /// Flush the stream.
        /// </summary>
        public override void Flush()
        {
            if (this._pendingException != null)
            {
                this._handlingException = true;
                Exception exception = this._pendingException;
                this._pendingException = null;
                throw exception;
            }
            if (!this._handlingException)
            {
                this._Flush(false);
            }
        }

        /// <summary>
        /// This method always throws a NotSupportedException.
        /// </summary>
        /// <param name="buffer">
        /// The buffer into which data would be read, IF THIS METHOD
        /// ACTUALLY DID ANYTHING.
        /// </param>
        /// <param name="offset">
        /// The offset within that data array at which to insert the
        /// data that is read, IF THIS METHOD ACTUALLY DID
        /// ANYTHING.
        /// </param>
        /// <param name="count">
        /// The number of bytes to write, IF THIS METHOD ACTUALLY DID
        /// ANYTHING.
        /// </param>
        /// <returns>nothing.</returns>
        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Resets the stream for use with another stream.
        /// </summary>
        /// <remarks>
        /// Because the ParallelDeflateOutputStream is expensive to create, it
        /// has been designed so that it can be recycled and re-used.  You have
        /// to call Close() on the stream first, then you can call Reset() on
        /// it, to use it again on another stream.
        /// </remarks>
        /// 
        /// <param name="stream">
        /// The new output stream for this era.
        /// </param>
        /// 
        /// <example>
        /// <code>
        /// ParallelDeflateOutputStream deflater = null;
        /// foreach (var inputFile in listOfFiles)
        /// {
        /// string outputFile = inputFile + ".compressed";
        /// using (System.IO.Stream input = System.IO.File.OpenRead(inputFile))
        /// {
        /// using (var outStream = System.IO.File.Create(outputFile))
        /// {
        /// if (deflater == null)
        /// deflater = new ParallelDeflateOutputStream(outStream,
        /// CompressionLevel.Best,
        /// CompressionStrategy.Default,
        /// true);
        /// deflater.Reset(outStream);
        /// 
        /// while ((n= input.Read(buffer, 0, buffer.Length)) != 0)
        /// {
        /// deflater.Write(buffer, 0, n);
        /// }
        /// }
        /// }
        /// }
        /// </code>
        /// </example>
        public void Reset(Stream stream)
        {
            if (this._firstWriteDone)
            {
                this._toWrite.Clear();
                this._toFill.Clear();
                foreach (WorkItem item in this._pool)
                {
                    this._toFill.Enqueue(item.index);
                    item.ordinal = -1;
                }
                this._firstWriteDone = false;
                this._totalBytesProcessed = 0L;
                this._runningCrc = new CRC32();
                this._isClosed = false;
                this._currentlyFilling = -1;
                this._lastFilled = -1;
                this._lastWritten = -1;
                this._latestCompressed = -1;
                this._outStream = stream;
            }
        }

        /// <summary>
        /// This method always throws a NotSupportedException.
        /// </summary>
        /// <param name="offset">
        /// The offset to seek to....
        /// IF THIS METHOD ACTUALLY DID ANYTHING.
        /// </param>
        /// <param name="origin">
        /// The reference specifying how to apply the offset....  IF
        /// THIS METHOD ACTUALLY DID ANYTHING.
        /// </param>
        /// <returns>nothing. It always throws.</returns>
        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// This method always throws a NotSupportedException.
        /// </summary>
        /// <param name="value">
        /// The new value for the stream length....  IF
        /// THIS METHOD ACTUALLY DID ANYTHING.
        /// </param>
        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        [Conditional("Trace")]
        private void TraceOutput(TraceBits bits, string format, params object[] varParams)
        {
            if ((bits & this._DesiredTrace) != TraceBits.None)
            {
                lock (this._outputLock)
                {
                    int hashCode = Thread.CurrentThread.GetHashCode();
                    Console.ForegroundColor = (ConsoleColor) ((hashCode % 8) + 8);
                    Console.Write("{0:000} PDOS ", hashCode);
                    Console.WriteLine(format, varParams);
                    Console.ResetColor();
                }
            }
        }

        /// <summary>
        /// Write data to the stream.
        /// </summary>
        /// 
        /// <remarks>
        /// 
        /// <para>
        /// To use the ParallelDeflateOutputStream to compress data, create a
        /// ParallelDeflateOutputStream with CompressionMode.Compress, passing a
        /// writable output stream.  Then call Write() on that
        /// ParallelDeflateOutputStream, providing uncompressed data as input.  The
        /// data sent to the output stream will be the compressed form of the data
        /// written.
        /// </para>
        /// 
        /// <para>
        /// To decompress data, use the <see cref="T:DotNetZipAdditionalPlatforms.Zlib.DeflateStream" /> class.
        /// </para>
        /// 
        /// </remarks>
        /// <param name="buffer">The buffer holding data to write to the stream.</param>
        /// <param name="offset">the offset within that data array to find the first byte to write.</param>
        /// <param name="count">the number of bytes to write.</param>
        public override void Write(byte[] buffer, int offset, int count)
        {
            bool mustWait = false;
            if (this._isClosed)
            {
                throw new InvalidOperationException();
            }
            if (this._pendingException != null)
            {
                this._handlingException = true;
                Exception exception = this._pendingException;
                this._pendingException = null;
                throw exception;
            }
            if (count == 0)
            {
                return;
            }
            if (!this._firstWriteDone)
            {
                this._InitializePoolOfWorkItems();
                this._firstWriteDone = true;
            }
        Label_0073:
            this.EmitPendingBuffers(false, mustWait);
            mustWait = false;
            int num = -1;
            if (this._currentlyFilling >= 0)
            {
                num = this._currentlyFilling;
            }
            else
            {
                if (this._toFill.Count == 0)
                {
                    mustWait = true;
                    goto Label_01A2;
                }
                num = this._toFill.Dequeue();
                this._lastFilled++;
            }
            WorkItem state = this._pool[num];
            int num2 = ((state.buffer.Length - state.inputBytesAvailable) > count) ? count : (state.buffer.Length - state.inputBytesAvailable);
            state.ordinal = this._lastFilled;
            Buffer.BlockCopy(buffer, offset, state.buffer, state.inputBytesAvailable, num2);
            count -= num2;
            offset += num2;
            state.inputBytesAvailable += num2;
            if (state.inputBytesAvailable == state.buffer.Length)
            {
                if (!ThreadPool.QueueUserWorkItem(new WaitCallback(this._DeflateOne), state))
                {
                    throw new Exception("Cannot enqueue workitem");
                }
                this._currentlyFilling = -1;
            }
            else
            {
                this._currentlyFilling = num;
            }
            if (count > 0)
            {
            }
        Label_01A2:
            if (count > 0)
            {
                goto Label_0073;
            }
        }

        /// <summary>
        /// The size of the buffers used by the compressor threads.
        /// </summary>
        /// <remarks>
        /// 
        /// <para>
        /// The default buffer size is 128k. The application can set this value
        /// at any time, but it is effective only before the first Write().
        /// </para>
        /// 
        /// <para>
        /// Larger buffer sizes implies larger memory consumption but allows
        /// more efficient compression. Using smaller buffer sizes consumes less
        /// memory but may result in less effective compression.  For example,
        /// using the default buffer size of 128k, the compression delivered is
        /// within 1% of the compression delivered by the single-threaded <see cref="T:DotNetZipAdditionalPlatforms.Zlib.DeflateStream" />.  On the other hand, using a
        /// BufferSize of 8k can result in a compressed data stream that is 5%
        /// larger than that delivered by the single-threaded
        /// <c>DeflateStream</c>.  Excessively small buffer sizes can also cause
        /// the speed of the ParallelDeflateOutputStream to drop, because of
        /// larger thread scheduling overhead dealing with many many small
        /// buffers.
        /// </para>
        /// 
        /// <para>
        /// The total amount of storage space allocated for buffering will be
        /// (N*S*2), where N is the number of buffer pairs, and S is the size of
        /// each buffer (this property). There are 2 buffers used by the
        /// compressor, one for input and one for output.  By default, DotNetZip
        /// allocates 4 buffer pairs per CPU core, so if your machine has 4
        /// cores, then the number of buffer pairs used will be 16. If you
        /// accept the default value of this property, 128k, then the
        /// ParallelDeflateOutputStream will use 16 * 2 * 128kb of buffer memory
        /// in total, or 4mb, in blocks of 128kb.  If you set this property to
        /// 64kb, then the number will be 16 * 2 * 64kb of buffer memory, or
        /// 2mb.
        /// </para>
        /// 
        /// </remarks>
        public int BufferSize
        {
            get
            {
                return this._bufferSize;
            }
            set
            {
                if (value < 0x400)
                {
                    throw new ArgumentOutOfRangeException("BufferSize", "BufferSize must be greater than 1024 bytes");
                }
                this._bufferSize = value;
            }
        }

        /// <summary>
        /// The total number of uncompressed bytes processed by the ParallelDeflateOutputStream.
        /// </summary>
        /// <remarks>
        /// This value is meaningful only after a call to Close().
        /// </remarks>
        public long BytesProcessed
        {
            get
            {
                return this._totalBytesProcessed;
            }
        }

        /// <summary>
        /// Indicates whether the stream supports Read operations.
        /// </summary>
        /// <remarks>
        /// Always returns false.
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
        /// Indicates whether the stream supports Write operations.
        /// </summary>
        /// <remarks>
        /// Returns true if the provided stream is writable.
        /// </remarks>
        public override bool CanWrite
        {
            get
            {
                return this._outStream.CanWrite;
            }
        }

        /// <summary>
        /// The CRC32 for the data that was written out, prior to compression.
        /// </summary>
        /// <remarks>
        /// This value is meaningful only after a call to Close().
        /// </remarks>
        public int Crc32
        {
            get
            {
                return this._Crc32;
            }
        }

        /// <summary>
        /// Reading this property always throws a NotSupportedException.
        /// </summary>
        public override long Length
        {
            get
            {
                throw new NotSupportedException();
            }
        }

        /// <summary>
        /// The maximum number of buffer pairs to use.
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// This property sets an upper limit on the number of memory buffer
        /// pairs to create.  The implementation of this stream allocates
        /// multiple buffers to facilitate parallel compression.  As each buffer
        /// fills up, this stream uses <see cref="M:System.Threading.ThreadPool.QueueUserWorkItem(System.Threading.WaitCallback)">
        /// ThreadPool.QueueUserWorkItem()</see>
        /// to compress those buffers in a background threadpool thread. After a
        /// buffer is compressed, it is re-ordered and written to the output
        /// stream.
        /// </para>
        /// 
        /// <para>
        /// A higher number of buffer pairs enables a higher degree of
        /// parallelism, which tends to increase the speed of compression on
        /// multi-cpu computers.  On the other hand, a higher number of buffer
        /// pairs also implies a larger memory consumption, more active worker
        /// threads, and a higher cpu utilization for any compression. This
        /// property enables the application to limit its memory consumption and
        /// CPU utilization behavior depending on requirements.
        /// </para>
        /// 
        /// <para>
        /// For each compression "task" that occurs in parallel, there are 2
        /// buffers allocated: one for input and one for output.  This property
        /// sets a limit for the number of pairs.  The total amount of storage
        /// space allocated for buffering will then be (N*S*2), where N is the
        /// number of buffer pairs, S is the size of each buffer (<see cref="P:DotNetZipAdditionalPlatforms.Zlib.ParallelDeflateOutputStream.BufferSize" />).  By default, DotNetZip allocates 4 buffer
        /// pairs per CPU core, so if your machine has 4 cores, and you retain
        /// the default buffer size of 128k, then the
        /// ParallelDeflateOutputStream will use 4 * 4 * 2 * 128kb of buffer
        /// memory in total, or 4mb, in blocks of 128kb.  If you then set this
        /// property to 8, then the number will be 8 * 2 * 128kb of buffer
        /// memory, or 2mb.
        /// </para>
        /// 
        /// <para>
        /// CPU utilization will also go up with additional buffers, because a
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
        /// This property is not the number of buffer pairs to use; it is an
        /// upper limit. An illustration: Suppose you have an application that
        /// uses the default value of this property (which is 16), and it runs
        /// on a machine with 2 CPU cores. In that case, DotNetZip will allocate
        /// 4 buffer pairs per CPU core, for a total of 8 pairs.  The upper
        /// limit specified by this property has no effect.
        /// </para>
        /// 
        /// <para>
        /// The application can set this value at any time, but it is effective
        /// only before the first call to Write(), which is when the buffers are
        /// allocated.
        /// </para>
        /// </remarks>
        public int MaxBufferPairs
        {
            get
            {
                return this._maxBufferPairs;
            }
            set
            {
                if (value < 4)
                {
                    throw new ArgumentException("MaxBufferPairs", "Value must be 4 or greater.");
                }
                this._maxBufferPairs = value;
            }
        }

        /// <summary>
        /// Returns the current position of the output stream.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Because the output gets written by a background thread,
        /// the value may change asynchronously.  Setting this
        /// property always throws a NotSupportedException.
        /// </para>
        /// </remarks>
        public override long Position
        {
            get
            {
                return this._outStream.Position;
            }
            set
            {
                throw new NotSupportedException();
            }
        }

        /// <summary>
        /// The ZLIB strategy to be used during compression.
        /// </summary>
        public CompressionStrategy Strategy { get; set; }

        [Flags]
        private enum TraceBits : uint
        {
            All = 0xffffffff,
            Compress = 0x800,
            EmitAll = 0x3a,
            EmitBegin = 8,
            EmitDone = 0x10,
            EmitEnter = 4,
            EmitLock = 2,
            EmitSkip = 0x20,
            Flush = 0x40,
            Instance = 0x400,
            Lifecycle = 0x80,
            None = 0,
            NotUsed1 = 1,
            Session = 0x100,
            Synch = 0x200,
            Write = 0x1000,
            WriteEnter = 0x2000,
            WriteTake = 0x4000
        }
    }
}

