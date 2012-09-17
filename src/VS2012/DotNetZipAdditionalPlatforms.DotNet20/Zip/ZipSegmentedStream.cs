namespace DotNetZipAdditionalPlatforms.Zip
{
    using System;
    using System.Globalization;
    using System.IO;
    using System.Runtime.CompilerServices;

    internal class ZipSegmentedStream : Stream
    {
        private string baseDirField;
        private string baseNameField;
        private uint currentDiskNumberField;
        private string currentNameField;
        private string currentTempNameField;
        private bool exceptionPendingField = false;
        private Stream innerStreamField;
        private uint maxDiskNumberField;
        private int maxSegmentSizeField;
        private RwMode rwModeField;

        private ZipSegmentedStream()
        {
        }

        private string _NameForSegment(uint diskNumber)
        {
            if (diskNumber >= 0x63)
            {
                this.exceptionPendingField = true;
                throw new OverflowException("The number of zip segments would exceed 99.");
            }
            return string.Format(CultureInfo.InvariantCulture, "{0}.z{1:D2}", Path.Combine(Path.GetDirectoryName(this.baseNameField), Path.GetFileNameWithoutExtension(this.baseNameField)), diskNumber + 1);
        }

        private void _SetReadStream()
        {
            if (this.innerStreamField != null)
            {
                this.innerStreamField.Dispose();
            }
            if ((this.CurrentSegment + 1) == this.maxDiskNumberField)
            {
                this.currentNameField = this.baseNameField;
            }
            this.innerStreamField = File.OpenRead(this.CurrentName);
        }

        private void _SetWriteStream(uint increment)
        {
            if (this.innerStreamField != null)
            {
                this.innerStreamField.Dispose();
                if (File.Exists(this.CurrentName))
                {
                    File.Delete(this.CurrentName);
                }
                File.Move(this.currentTempNameField, this.CurrentName);
            }
            if (increment > 0)
            {
                this.CurrentSegment += increment;
            }
            SharedUtilities.CreateAndOpenUniqueTempFile(this.baseDirField, out this.innerStreamField, out this.currentTempNameField);
            if (this.CurrentSegment == 0)
            {
                this.innerStreamField.Write(BitConverter.GetBytes(0x8074b50), 0, 4);
            }
        }

        public uint ComputeSegment(int length)
        {
            if ((this.innerStreamField.Position + length) > this.maxSegmentSizeField)
            {
                return (this.CurrentSegment + 1);
            }
            return this.CurrentSegment;
        }

        protected override void Dispose(bool disposing)
        {
            try
            {
                if (this.innerStreamField != null)
                {
                    this.innerStreamField.Dispose();
                    if ((this.rwModeField == RwMode.Write) && this.exceptionPendingField)
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
            this.innerStreamField.Flush();
        }

        public static ZipSegmentedStream ForReading(string name, uint initialDiskNumber, uint maxDiskNumber)
        {
            ZipSegmentedStream stream2 = new ZipSegmentedStream();
            stream2.rwModeField = RwMode.ReadOnly;
            stream2.CurrentSegment = initialDiskNumber;
            stream2.maxDiskNumberField = maxDiskNumber;
            stream2.baseNameField = name;
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
            stream2.rwModeField = RwMode.Write;
            stream2.CurrentSegment = 0;
            stream2.baseNameField = name;
            stream2.maxSegmentSizeField = maxSegmentSize;
            stream2.baseDirField = Path.GetDirectoryName(name);
            ZipSegmentedStream stream = stream2;
            if (stream.baseDirField == "")
            {
                stream.baseDirField = ".";
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
            if (this.rwModeField != RwMode.ReadOnly)
            {
                this.exceptionPendingField = true;
                throw new InvalidOperationException("Stream Error: Cannot Read.");
            }
            int num = this.innerStreamField.Read(buffer, offset, count);
            int num2 = num;
            while (num2 != count)
            {
                if (this.innerStreamField.Position != this.innerStreamField.Length)
                {
                    this.exceptionPendingField = true;
                    throw new ZipException(string.Format(CultureInfo.InvariantCulture, "Read error in file {0}", this.CurrentName));
                }
                if ((this.CurrentSegment + 1) == this.maxDiskNumberField)
                {
                    return num;
                }
                this.CurrentSegment++;
                this._SetReadStream();
                offset += num2;
                count -= num2;
                num2 = this.innerStreamField.Read(buffer, offset, count);
                num += num2;
            }
            return num;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return this.innerStreamField.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            if (this.rwModeField != RwMode.Write)
            {
                this.exceptionPendingField = true;
                throw new InvalidOperationException();
            }
            this.innerStreamField.SetLength(value);
        }

        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture, "{0}[{1}][{2}], pos=0x{3:X})", new object[] { "ZipSegmentedStream", this.CurrentName, this.rwModeField.ToString(), this.Position });
        }

        public long TruncateBackward(uint diskNumber, long offset)
        {
            if (diskNumber >= 0x63)
            {
                throw new ArgumentOutOfRangeException("diskNumber");
            }
            if (this.rwModeField != RwMode.Write)
            {
                this.exceptionPendingField = true;
                throw new ZipException("bad state.");
            }
            if (diskNumber != this.CurrentSegment)
            {
                if (this.innerStreamField != null)
                {
                    this.innerStreamField.Dispose();
                    if (File.Exists(this.currentTempNameField))
                    {
                        File.Delete(this.currentTempNameField);
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
                        this.currentTempNameField = SharedUtilities.InternalGetTempFileName();
                        File.Move(this.CurrentName, this.currentTempNameField);
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
                this.innerStreamField = new FileStream(this.currentTempNameField, FileMode.Open);
            }
            return this.innerStreamField.Seek(offset, SeekOrigin.Begin);
        }

        /// <summary>
        /// Write to the stream.
        /// </summary>
        /// <param name="buffer">the buffer from which to write</param>
        /// <param name="offset">the offset at which to start writing</param>
        /// <param name="count">the number of bytes to write</param>
        public override void Write(byte[] buffer, int offset, int count)
        {
            if (this.rwModeField != RwMode.Write)
            {
                this.exceptionPendingField = true;
                throw new InvalidOperationException("Stream Error: Cannot Write.");
            }
            if (!this.ContiguousWrite)
            {
                while ((this.innerStreamField.Position + count) > this.maxSegmentSizeField)
                {
                    int num = this.maxSegmentSizeField - ((int) this.innerStreamField.Position);
                    this.innerStreamField.Write(buffer, offset, num);
                    this._SetWriteStream(1);
                    count -= num;
                    offset += num;
                }
            }
            else if ((this.innerStreamField.Position + count) > this.maxSegmentSizeField)
            {
                this._SetWriteStream(1);
            }
            this.innerStreamField.Write(buffer, offset, count);
        }

        public override bool CanRead
        {
            get
            {
                return (((this.rwModeField == RwMode.ReadOnly) && (this.innerStreamField != null)) && this.innerStreamField.CanRead);
            }
        }

        public override bool CanSeek
        {
            get
            {
                return ((this.innerStreamField != null) && this.innerStreamField.CanSeek);
            }
        }

        public override bool CanWrite
        {
            get
            {
                return (((this.rwModeField == RwMode.Write) && (this.innerStreamField != null)) && this.innerStreamField.CanWrite);
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
                if (this.currentNameField == null)
                {
                    this.currentNameField = this._NameForSegment(this.CurrentSegment);
                }
                return this.currentNameField;
            }
        }

        public uint CurrentSegment
        {
            get
            {
                return this.currentDiskNumberField;
            }
            private set
            {
                this.currentDiskNumberField = value;
                this.currentNameField = null;
            }
        }

        public string CurrentTempName
        {
            get
            {
                return this.currentTempNameField;
            }
        }

        public override long Length
        {
            get
            {
                return this.innerStreamField.Length;
            }
        }

        public override long Position
        {
            get
            {
                return this.innerStreamField.Position;
            }
            set
            {
                this.innerStreamField.Position = value;
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

