namespace DotNetZipAdditionalPlatforms.Zip
{
    using System;
    using System.IO;

    internal class OffsetStream : Stream, IDisposable
    {
        private Stream _innerStream;
        private long _originalPosition;

        public OffsetStream(Stream s)
        {
            this._originalPosition = s.Position;
            this._innerStream = s;
        }

        public override void Close()
        {
            base.Close();
        }

        public override void Flush()
        {
            this._innerStream.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return this._innerStream.Read(buffer, offset, count);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return (this._innerStream.Seek(this._originalPosition + offset, origin) - this._originalPosition);
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        void IDisposable.Dispose()
        {
            this.Close();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        public override bool CanRead
        {
            get
            {
                return this._innerStream.CanRead;
            }
        }

        public override bool CanSeek
        {
            get
            {
                return this._innerStream.CanSeek;
            }
        }

        public override bool CanWrite
        {
            get
            {
                return false;
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
                return (this._innerStream.Position - this._originalPosition);
            }
            set
            {
                this._innerStream.Position = this._originalPosition + value;
            }
        }
    }
}

