namespace DotNetZipAdditionalPlatforms.Zip
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Security.Cryptography;
    using System.Threading;

    /// <summary>
    /// A stream that encrypts as it writes, or decrypts as it reads.  The
    /// Crypto is AES in CTR (counter) mode, which is compatible with the AES
    /// encryption employed by WinZip 12.0.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The AES/CTR encryption protocol used by WinZip works like this:
    /// 
    /// - start with a counter, initialized to zero.
    /// 
    /// - to encrypt, take the data by 16-byte blocks. For each block:
    /// - apply the transform to the counter
    /// - increement the counter
    /// - XOR the result of the transform with the plaintext to
    /// get the ciphertext.
    /// - compute the mac on the encrypted bytes
    /// - when finished with all blocks, store the computed MAC.
    /// 
    /// - to decrypt, take the data by 16-byte blocks. For each block:
    /// - compute the mac on the encrypted bytes,
    /// - apply the transform to the counter
    /// - increement the counter
    /// - XOR the result of the transform with the ciphertext to
    /// get the plaintext.
    /// - when finished with all blocks, compare the computed MAC against
    /// the stored MAC
    /// 
    /// </para>
    /// </remarks>
    internal class WinZipAesCipherStream : Stream
    {
        internal RijndaelManaged aesCipherField;
        private bool finalBlockField;
        private byte[] iobufField;
        private long lengthField;
        internal HMACSHA1 macField;
        private CryptoMode modeField;
        private int nonceField;
        private object outputLockField;
        private WinZipAesCrypto paramsField;
        private int pendingCountField;
        private byte[] pendingWriteBlockField;
        private Stream streamField;
        private long totalBytesXferredField;
        internal ICryptoTransform xformField;
        private const int BLOCK_SIZE_IN_BYTES = 0x10;
        private byte[] counterField;
        private byte[] counterOutField;

        internal WinZipAesCipherStream(Stream s, WinZipAesCrypto cryptoParams, CryptoMode mode)
        {
            this.counterField = new byte[0x10];
            this.counterOutField = new byte[0x10];
            this.outputLockField = new object();
            this.paramsField = cryptoParams;
            this.streamField = s;
            this.modeField = mode;
            this.nonceField = 1;
            if (this.paramsField == null)
            {
                throw new BadPasswordException("Supply a password to use AES encryption.");
            }
            int num = this.paramsField.KeyBytes.Length * 8;
            if (((num != 0x100) && (num != 0x80)) && (num != 0xc0))
            {
                throw new ArgumentOutOfRangeException("keysize", "size of key must be 128, 192, or 256");
            }
            this.macField = new HMACSHA1(this.paramsField.MacIv);
            this.aesCipherField = new RijndaelManaged();
            this.aesCipherField.BlockSize = 0x80;
            this.aesCipherField.KeySize = num;
            this.aesCipherField.Mode = CipherMode.ECB;
            this.aesCipherField.Padding = PaddingMode.None;
            byte[] rgbIV = new byte[0x10];
            this.xformField = this.aesCipherField.CreateEncryptor(this.paramsField.KeyBytes, rgbIV);
            if (this.modeField == CryptoMode.Encrypt)
            {
                this.iobufField = new byte[0x800];
                this.pendingWriteBlockField = new byte[0x10];
            }
        }

        /// <summary>
        /// The constructor.
        /// </summary>
        /// <param name="s">The underlying stream</param>
        /// <param name="mode">To either encrypt or decrypt.</param>
        /// <param name="cryptoParams">The pre-initialized WinZipAesCrypto object.</param>
        /// <param name="length">The maximum number of bytes to read from the stream.</param>
        internal WinZipAesCipherStream(Stream s, WinZipAesCrypto cryptoParams, long length, CryptoMode mode) : this(s, cryptoParams, mode)
        {
            this.lengthField = length;
        }

        /// <summary>
        /// Close the stream.
        /// </summary>
        public override void Close()
        {
            if (this.pendingCountField > 0)
            {
                this.WriteTransformFinalBlock();
                this.streamField.Write(this.pendingWriteBlockField, 0, this.pendingCountField);
                this.totalBytesXferredField += this.pendingCountField;
                this.pendingCountField = 0;
            }
            this.streamField.Close();
        }

        /// <summary>
        /// Flush the content in the stream.
        /// </summary>
        public override void Flush()
        {
            this.streamField.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (this.modeField == CryptoMode.Encrypt)
            {
                throw new NotSupportedException();
            }
            if (buffer == null)
            {
                throw new ArgumentNullException("buffer");
            }
            if (offset < 0)
            {
                throw new ArgumentOutOfRangeException("offset", "Must not be less than zero.");
            }
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException("count", "Must not be less than zero.");
            }
            if (buffer.Length < (offset + count))
            {
                throw new ArgumentException("The buffer is too small");
            }
            int num = count;
            if (this.totalBytesXferredField >= this.lengthField)
            {
                return 0;
            }
            long num2 = this.lengthField - this.totalBytesXferredField;
            if (num2 < count)
            {
                num = (int) num2;
            }
            int num3 = this.streamField.Read(buffer, offset, num);
            this.ReadTransformBlocks(buffer, offset, num);
            this.totalBytesXferredField += num3;
            return num3;
        }

        private void ReadTransformBlocks(byte[] buffer, int offset, int count)
        {
            int num = offset;
            int last = count + offset;
            while ((num < buffer.Length) && (num < last))
            {
                int num3 = this.ReadTransformOneBlock(buffer, num, last);
                num += num3;
            }
        }

        private int ReadTransformOneBlock(byte[] buffer, int offset, int last)
        {
            if (this.finalBlockField)
            {
                throw new NotSupportedException();
            }
            int num = last - offset;
            int inputCount = (num > 0x10) ? 0x10 : num;
            Array.Copy(BitConverter.GetBytes(this.nonceField++), 0, this.counterField, 0, 4);
            if (((inputCount == num) && (this.lengthField > 0L)) && ((this.totalBytesXferredField + last) == this.lengthField))
            {
                this.macField.TransformFinalBlock(buffer, offset, inputCount);
                this.counterOutField = this.xformField.TransformFinalBlock(this.counterField, 0, 0x10);
                this.finalBlockField = true;
            }
            else
            {
                this.macField.TransformBlock(buffer, offset, inputCount, null, 0);
                this.xformField.TransformBlock(this.counterField, 0, 0x10, this.counterOutField, 0);
            }
            this.XorInPlace(buffer, offset, inputCount);
            return inputCount;
        }

        /// <summary>
        /// This method throws a NotImplementedException.
        /// </summary>
        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// This method throws a NotImplementedException.
        /// </summary>
        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        [Conditional("Trace")]
        private void TraceOutput(string format, params object[] varParams)
        {
            lock (this.outputLockField)
            {
                int hashCode = Thread.CurrentThread.GetHashCode();
                Console.ForegroundColor = (ConsoleColor) ((hashCode % 8) + 8);
                Console.Write("{0:000} WZACS ", hashCode);
                Console.WriteLine(format, varParams);
                Console.ResetColor();
            }
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (this.finalBlockField)
            {
                throw new InvalidOperationException("The final block has already been transformed.");
            }
            if (this.modeField == CryptoMode.Decrypt)
            {
                throw new NotSupportedException();
            }
            if (buffer == null)
            {
                throw new ArgumentNullException("buffer");
            }
            if (offset < 0)
            {
                throw new ArgumentOutOfRangeException("offset", "Must not be less than zero.");
            }
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException("count", "Must not be less than zero.");
            }
            if (buffer.Length < (offset + count))
            {
                throw new ArgumentException("The offset and count are too large");
            }
            if (count != 0)
            {
                if ((count + this.pendingCountField) <= 0x10)
                {
                    Buffer.BlockCopy(buffer, offset, this.pendingWriteBlockField, this.pendingCountField, count);
                    this.pendingCountField += count;
                }
                else
                {
                    int num = count;
                    int srcOffset = offset;
                    if (this.pendingCountField != 0)
                    {
                        int num3 = 0x10 - this.pendingCountField;
                        if (num3 > 0)
                        {
                            Buffer.BlockCopy(buffer, offset, this.pendingWriteBlockField, this.pendingCountField, num3);
                            num -= num3;
                            srcOffset += num3;
                        }
                        this.WriteTransformOneBlock(this.pendingWriteBlockField, 0);
                        this.streamField.Write(this.pendingWriteBlockField, 0, 0x10);
                        this.totalBytesXferredField += 0x10L;
                        this.pendingCountField = 0;
                    }
                    int num4 = (num - 1) / 0x10;
                    this.pendingCountField = num - (num4 * 0x10);
                    Buffer.BlockCopy(buffer, (srcOffset + num) - this.pendingCountField, this.pendingWriteBlockField, 0, this.pendingCountField);
                    num -= this.pendingCountField;
                    this.totalBytesXferredField += num;
                    if (num4 > 0)
                    {
                        do
                        {
                            int length = this.iobufField.Length;
                            if (length > num)
                            {
                                length = num;
                            }
                            Buffer.BlockCopy(buffer, srcOffset, this.iobufField, 0, length);
                            this.WriteTransformBlocks(this.iobufField, 0, length);
                            this.streamField.Write(this.iobufField, 0, length);
                            num -= length;
                            srcOffset += length;
                        }
                        while (num > 0);
                    }
                }
            }
        }

        private void WriteTransformBlocks(byte[] buffer, int offset, int count)
        {
            int num = offset;
            int num2 = count + offset;
            while ((num < buffer.Length) && (num < num2))
            {
                this.WriteTransformOneBlock(buffer, num);
                num += 0x10;
            }
        }

        private void WriteTransformFinalBlock()
        {
            if (this.pendingCountField == 0)
            {
                throw new InvalidOperationException("No bytes available.");
            }
            if (this.finalBlockField)
            {
                throw new InvalidOperationException("The final block has already been transformed.");
            }
            Array.Copy(BitConverter.GetBytes(this.nonceField++), 0, this.counterField, 0, 4);
            this.counterOutField = this.xformField.TransformFinalBlock(this.counterField, 0, 0x10);
            this.XorInPlace(this.pendingWriteBlockField, 0, this.pendingCountField);
            this.macField.TransformFinalBlock(this.pendingWriteBlockField, 0, this.pendingCountField);
            this.finalBlockField = true;
        }

        private void WriteTransformOneBlock(byte[] buffer, int offset)
        {
            Array.Copy(BitConverter.GetBytes(this.nonceField++), 0, this.counterField, 0, 4);
            this.xformField.TransformBlock(this.counterField, 0, 0x10, this.counterOutField, 0);
            this.XorInPlace(buffer, offset, 0x10);
            this.macField.TransformBlock(buffer, offset, 0x10, null, 0);
        }

        private void XorInPlace(byte[] buffer, int offset, int count)
        {
            for (int i = 0; i < count; i++)
            {
                buffer[offset + i] = (byte) (this.counterOutField[i] ^ buffer[offset + i]);
            }
        }

        /// <summary>
        /// Returns true if the stream can be read.
        /// </summary>
        public override bool CanRead
        {
            get
            {
                if (this.modeField != CryptoMode.Decrypt)
                {
                    return false;
                }
                return true;
            }
        }

        /// <summary>
        /// Always returns false.
        /// </summary>
        public override bool CanSeek
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Returns true if the CryptoMode is Encrypt.
        /// </summary>
        public override bool CanWrite
        {
            get
            {
                return (this.modeField == CryptoMode.Encrypt);
            }
        }

        /// <summary>
        /// Returns the final HMAC-SHA1-80 for the data that was encrypted.
        /// </summary>
        public byte[] FinalAuthentication
        {
            get
            {
                if (!this.finalBlockField)
                {
                    if (this.totalBytesXferredField != 0L)
                    {
                        throw new BadStateException("The final hash has not been computed.");
                    }
                    byte[] buffer = new byte[0];
                    this.macField.ComputeHash(buffer);
                }
                byte[] destinationArray = new byte[10];
                Array.Copy(this.macField.Hash, 0, destinationArray, 0, 10);
                return destinationArray;
            }
        }

        /// <summary>
        /// Getting this property throws a NotImplementedException.
        /// </summary>
        public override long Length
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Getting or Setting this property throws a NotImplementedException.
        /// </summary>
        public override long Position
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }
    }
}

