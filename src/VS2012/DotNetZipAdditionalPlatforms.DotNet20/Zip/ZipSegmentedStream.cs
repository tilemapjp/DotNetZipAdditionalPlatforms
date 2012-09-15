namespace DotNetZipAdditionalPlatforms.Zip
{
    using System;
    using System.Globalization;
    using System.IO;
    using System.Runtime.CompilerServices;

    internal class ZipSegmentedStream : Stream
    {
        private string _baseDir;
        private string _baseName;
        private uint _currentDiskNumber;
        private string _currentName;
        private string _currentTempName;
        private bool _exceptionPending = false;
        private Stream _innerStream;
        private uint _maxDiskNumber;
        private int _maxSegmentSize;
        private RwMode rwMode;

        private ZipSegmentedStream()
        {
        }

        private string _NameForSegment(uint diskNumber)
        {
            if (diskNumber >= 0x63)
            {
                this._exceptionPending = true;
                throw new OverflowException("The number of zip segments would exceed 99.");
            }
            return string.Format(CultureInfo.InvariantCulture, "{0}.z{1:D2}", Path.Combine(Path.GetDirectoryName(this._baseName), Path.GetFileNameWithoutExtension(this._baseName)), diskNumber + 1);
        }

        private void _SetReadStream()
        {
            if (this._innerStream != null)
            {
                this._innerStream.Dispose();
            }
            if ((this.CurrentSegment + 1) == this._maxDiskNumber)
            {
                this._currentName = this._baseName;
            }
            this._innerStream = File.OpenRead(this.CurrentName);
        }

        private void _SetWriteStream(uint increment)
        {
            if (this._innerStream != null)
            {
                this._innerStream.Dispose();
                if (File.Exists(this.CurrentName))
                {
                    File.Delete(this.CurrentName);
                }
                File.Move(this._currentTempName, this.CurrentName);
            }
            if (increment > 0)
            {
                this.CurrentSegment += increment;
            }
            SharedUtilities.CreateAndOpenUniqueTempFile(this._baseDir, out this._innerStream, out this._currentTempName);
            if (this.CurrentSegment == 0)
            {
                this._innerStream.Write(BitConverter.GetBytes(0x8074b50), 0, 4);
            }
        }

        public uint ComputeSegment(int length)
        {
            if ((this._innerStream.Position + length) > this._maxSegmentSize)
            {
                return (this.CurrentSegment + 1);
            }
            return this.CurrentSegment;
        }

        protected override void Dispose(bool disposing)
        {
            try
            {
                if (this._innerStream != null)
                {
                    this._innerStream.Dispose();
                    if ((this.rwMode == RwMode.Write) && this._exceptionPending)
                    {
                    }
                }
            }
            finally
            {
                base.Dispose(disposing);
            }
        }

        public override void Flush()
        {
            this._innerStream.Flush();
        }

        public static ZipSegmentedStream ForReading(string name, uint initialDiskNumber, uint maxDiskNumber)
        {
            ZipSegmentedStream stream2 = new ZipSegmentedStream();
            stream2.rwMode = RwMode.ReadOnly;
            stream2.CurrentSegment = initialDiskNumber;
            stream2._maxDiskNumber = maxDiskNumber;
            stream2._baseName = name;
            ZipSegmentedStream stream = stream2;
            stream._SetReadStream();
            return stream;
        }

        /// <summary>
        /// Sort-of like a factory method, ForUpdate is used only when
        /// the application needs to update the zip entry metadata for
        /// a segmented zip file, when the starting segment is earlier
        /// than the ending segment, for a particular entry.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The update is always contiguous, never rolls over.  As a
        /// result, this method doesn't need to return a ZSS; it can
        /// simply return a FileStream.  That's why it's "sort of"
        /// like a Factory method.
        /// </para>
        /// <para>
        /// Caller must Close/Dispose the stream object returned by
        /// this method.
        /// </para>
        /// </remarks>
        public static Stream ForUpdate(string name, uint diskNumber)
        {
            if (diskNumber >= 0x63)
            {
                throw new ArgumentOutOfRangeException("diskNumber");
            }
            return File.Open(string.Format(CultureInfo.InvariantCulture, "{0}.z{1:D2}", Path.Combine(Path.GetDirectoryName(name), Path.GetFileNameWithoutExtension(name)), diskNumber + 1), FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        }

        public static ZipSegmentedStream ForWriting(string name, int maxSegmentSize)
        {
            ZipSegmentedStream stream2 = new ZipSegmentedStream();
            stream2.rwMode = RwMode.Write;
            stream2.CurrentSegment = 0;
            stream2._baseName = name;
            stream2._maxSegmentSize = maxSegmentSize;
            stream2._baseDir = Path.GetDirectoryName(name);
            ZipSegmentedStream stream = stream2;
            if (stream._baseDir == "")
            {
                stream._baseDir = ".";
            }
            stream._SetWriteStream(0);
            return stream;
        }

        /// <summary>
        /// Read from the stream
        /// </summary>
        /// <param name="buffer">the buffer to read</param>
        /// <param name="offset">the offset at which to start</param>
        /// <param name="count">the number of bytes to read</param>
        /// <returns>the number of bytes actually read</returns>
        public override int Read(byte[] buffer, int offset, int count)
        {
            if (this.rwMode != RwMode.ReadOnly)
            {
                this._exceptionPending = true;
                throw new InvalidOperationException("Stream Error: Cannot Read.");
            }
            int num = this._innerStream.Read(buffer, offset, count);
            int num2 = num;
            while (num2 != count)
            {
                if (this._innerStream.Position != this._innerStream.Length)
                {
                    this._exceptionPending = true;
                    throw new ZipException(string.Format(CultureInfo.InvariantCulture, "Read error in file {0}", this.CurrentName));
                }
                if ((this.CurrentSegment + 1) == this._maxDiskNumber)
                {
                    return num;
                }
                this.CurrentSegment++;
                this._SetReadStream();
                offset += num2;
                count -= num2;
                num2 = this._innerStream.Read(buffer, offset, count);
                num += num2;
            }
            return num;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return this._innerStream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            if (this.rwMode != RwMode.Write)
            {
                this._exceptionPending = true;
                throw new InvalidOperationException();
            }
            this._innerStream.SetLength(value);
        }

        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture, "{0}[{1}][{2}], pos=0x{3:X})", new object[] { "ZipSegmentedStream", this.CurrentName, this.rwMode.ToString(), this.Position });
        }

        public long TruncateBackward(uint diskNumber, long offset)
        {
            if (diskNumber >= 0x63)
            {
                throw new ArgumentOutOfRangeException("diskNumber");
            }
            if (this.rwMode != RwMode.Write)
            {
                this._exceptionPending = true;
                throw new ZipException("bad state.");
            }
            if (diskNumber != this.CurrentSegment)
            {
                if (this._innerStream != null)
                {
                    this._innerStream.Dispose();
                    if (File.Exists(this._currentTempName))
                    {
                        File.Delete(this._currentTempName);
                    }
                }
                for (uint i = this.CurrentSegment - 1; i > diskNumber; i--)
                {
                    string path = this._NameForSegment(i);
                    if (File.Exists(path))
                    {
                        File.Delete(path);
                    }
                }
                this.CurrentSegment = diskNumber;
                for (int j = 0; j < 3; j++)
                {
                    try
                    {
                        this._currentTempName = SharedUtilities.InternalGetTempFileName();
                        File.Move(this.CurrentName, this._currentTempName);
                        break;
                    }
                    catch (IOException)
                    {
                        if (j == 2)
                        {
                            throw;
                        }
                    }
                }
                this._innerStream = new FileStream(this._currentTempName, FileMode.Open);
            }
            return this._innerStream.Seek(offset, SeekOrigin.Begin);
        }

        /// <summary>
        /// Write to the stream.
        /// </summary>
        /// <param name="buffer">the buffer from which to write</param>
        /// <param name="offset">the offset at which to start writing</param>
        /// <param name="count">the number of bytes to write</param>
        public override void Write(byte[] buffer, int offset, int count)
        {
            if (this.rwMode != RwMode.Write)
            {
                this._exceptionPending = true;
                throw new InvalidOperationException("Stream Error: Cannot Write.");
            }
            if (!this.ContiguousWrite)
            {
                while ((this._innerStream.Position + count) > this._maxSegmentSize)
                {
                    int num = this._maxSegmentSize - ((int) this._innerStream.Position);
                    this._innerStream.Write(buffer, offset, num);
                    this._SetWriteStream(1);
                    count -= num;
                    offset += num;
                }
            }
            else if ((this._innerStream.Position + count) > this._maxSegmentSize)
            {
                this._SetWriteStream(1);
            }
            this._innerStream.Write(buffer, offset, count);
        }

        public override bool CanRead
        {
            get
            {
                return (((this.rwMode == RwMode.ReadOnly) && (this._innerStream != null)) && this._innerStream.CanRead);
            }
        }

        public override bool CanSeek
        {
            get
            {
                return ((this._innerStream != null) && this._innerStream.CanSeek);
            }
        }

        public override bool CanWrite
        {
            get
            {
                return (((this.rwMode == RwMode.Write) && (this._innerStream != null)) && this._innerStream.CanWrite);
            }
        }

        public bool ContiguousWrite { get; set; }

        /// <summary>
        /// Name of the filesystem file corresponding to the current segment.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The name is not always the name currently being used in the
        /// filesystem.  When rwMode is RwMode.Write, the filesystem file has a
        /// temporary name until the stream is closed or until the next segment is
        /// started.
        /// </para>
        /// </remarks>
        public string CurrentName
        {
            get
            {
                if (this._currentName == null)
                {
                    this._currentName = this._NameForSegment(this.CurrentSegment);
                }
                return this._currentName;
            }
        }

        public uint CurrentSegment
        {
            get
            {
                return this._currentDiskNumber;
            }
            private set
            {
                this._currentDiskNumber = value;
                this._currentName = null;
            }
        }

        public string CurrentTempName
        {
            get
            {
                return this._currentTempName;
            }
        }

        public override long Length
        {
            get
            {
                return this._innerStream.Length;
            }
        }

        public override long Position
        {
            get
            {
                return this._innerStream.Position;
            }
            set
            {
                this._innerStream.Position = value;
            }
        }

        private enum RwMode
        {
            None,
            ReadOnly,
            Write
        }
    }
}

