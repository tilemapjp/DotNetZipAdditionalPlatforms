namespace DotNetZipAdditionalPlatforms.Zip
{
    using System;
    using System.IO;

    /// <summary>
    /// A Stream for reading and concurrently decrypting data from a zip file,
    /// or for writing and concurrently encrypting data to a zip file.
    /// </summary>
    internal class ZipCipherStream : Stream
    {
        private ZipCrypto _cipher;
        private CryptoMode _mode;
        private Stream _s;

        /// <summary>  The constructor. </summary>
        /// <param name="s">The underlying stream</param>
        /// <param name="mode">To either encrypt or decrypt.</param>
        /// <param name="cipher">The pre-initialized ZipCrypto object.</param>
        public ZipCipherStream(Stream s, ZipCrypto cipher, CryptoMode mode)
        {
            this._cipher = cipher;
            this._s = s;
            this._mode = mode;
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (this._mode == CryptoMode.Encrypt)
            {
                throw new NotSupportedException("This stream does not encrypt via Read()");
            }
            if (buffer == null)
            {
                throw new ArgumentNullException("buffer");
            }
            byte[] buffer2 = new byte[count];
            int length = this._s.Read(buffer2, 0, count);
            byte[] buffer3 = this._cipher.DecryptMessage(buffer2, length);
            for (int i = 0; i < length; i++)
            {
                buffer[offset + i] = buffer3[i];
            }
            return length;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (this._mode == CryptoMode.Decrypt)
            {
                throw new NotSupportedException("This stream does not Decrypt via Write()");
            }
            if (buffer == null)
            {
                throw new ArgumentNullException("buffer");
            }
            if (count != 0)
            {
                byte[] plainText = null;
                if (offset != 0)
                {
                    plainText = new byte[count];
                    for (int i = 0; i < count; i++)
                    {
                        plainText[i] = buffer[offset + i];
                    }
                }
                else
                {
                    plainText = buffer;
                }
                byte[] buffer3 = this._cipher.EncryptMessage(plainText, count);
                this._s.Write(buffer3, 0, buffer3.Length);
            }
        }

        public override bool CanRead
        {
            get
            {
                return (this._mode == CryptoMode.Decrypt);
            }
        }

        public override bool CanSeek
        {
            get
            {
                return false;
            }
        }

        public override bool CanWrite
        {
            get
            {
                return (this._mode == CryptoMode.Encrypt);
            }
        }

        public override long Length
        {
            get
            {
                throw new NotSupportedException();
            }
        }

        public override long Position
        {
            get
            {
                throw new NotSupportedException();
            }
            set
            {
                throw new NotSupportedException();
            }
        }
    }
}

