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
        private ZipCrypto cipherField;
        private CryptoMode modeField;
        private Stream streamField;

        /// <summary>  The constructor. </summary>
        /// <param name="s">The underlying stream</param>
        /// <param name="mode">To either encrypt or decrypt.</param>
        /// <param name="cipher">The pre-initialized ZipCrypto object.</param>
        public ZipCipherStream(Stream s, ZipCrypto cipher, CryptoMode mode)
        {
            this.cipherField = cipher;
            this.streamField = s;
            this.modeField = mode;
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (this.modeField == CryptoMode.Encrypt)
            {
                throw new NotSupportedException("This stream does not encrypt via Read()");
            }
            if (buffer == null)
            {
                throw new ArgumentNullException("buffer");
            }
            byte[] buffer2 = new byte[count];
            int length = this.streamField.Read(buffer2, 0, count);
            byte[] buffer3 = this.cipherField.DecryptMessage(buffer2, length);
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
            if (this.modeField == CryptoMode.Decrypt)
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
                byte[] buffer3 = this.cipherField.EncryptMessage(plainText, count);
                this.streamField.Write(buffer3, 0, buffer3.Length);
            }
        }

        public override bool CanRead
        {
            get
            {
                return (this.modeField == CryptoMode.Decrypt);
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
                return (this.modeField == CryptoMode.Encrypt);
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

