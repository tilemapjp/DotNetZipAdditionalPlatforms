namespace DotNetZipAdditionalPlatforms.Zip
{
    using System;
    using System.IO;

    internal class OffsetStream : Stream, IDisposable
    {
        private Stream innerStreamField;
        private long originalPositionField;

        public OffsetStream(Stream s)
        {
            this.originalPositionField = s.Position;
            this.innerStreamField = s;
        }

        public override void Close()
        {
            base.Close();
        }

        public override void Flush()
        {
            this.innerStreamField.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return this.innerStreamField.Read(buffer, offset, count);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return (this.innerStreamField.Seek(this.originalPositionField + offset, origin) - this.originalPositionField);
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
                return this.innerStreamField.CanRead;
            }
        }

        public override bool CanSeek
        {
            get
            {
                return this.innerStreamField.CanSeek;
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
                return this.innerStreamField.Length;
            }
        }

        public override long Position
        {
            get
            {
                return (this.innerStreamField.Position - this.originalPositionField);
            }
            set
            {
                this.innerStreamField.Position = this.originalPositionField + value;
            }
        }
    }
}

