﻿namespace DotNetZipAdditionalPlatforms.Zip
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
        internal RijndaelManaged _aesCipher;
        private bool _finalBlock;
        private byte[] _iobuf;
        private long _length;
        internal HMACSHA1 _mac;
        private CryptoMode _mode;
        private int _nonce;
        private object _outputLock;
        private WinZipAesCrypto _params;
        private int _pendingCount;
        private byte[] _PendingWriteBlock;
        private Stream _s;
        private long _totalBytesXferred;
        internal ICryptoTransform _xform;
        private const int BLOCK_SIZE_IN_BYTES = 0x10;
        private byte[] counter;
        private byte[] counterOut;

        internal WinZipAesCipherStream(Stream s, WinZipAesCrypto cryptoParams, CryptoMode mode)
        {
            this.counter = new byte[0x10];
            this.counterOut = new byte[0x10];
            this._outputLock = new object();
            this._params = cryptoParams;
            this._s = s;
            this._mode = mode;
            this._nonce = 1;
            if (this._params == null)
            {
                throw new BadPasswordException("Supply a password to use AES encryption.");
            }
            int num = this._params.KeyBytes.Length * 8;
            if (((num != 0x100) && (num != 0x80)) && (num != 0xc0))
            {
                throw new ArgumentOutOfRangeException("keysize", "size of key must be 128, 192, or 256");
            }
            this._mac = new HMACSHA1(this._params.MacIv);
            this._aesCipher = new RijndaelManaged();
            this._aesCipher.BlockSize = 0x80;
            this._aesCipher.KeySize = num;
            this._aesCipher.Mode = CipherMode.ECB;
            this._aesCipher.Padding = PaddingMode.None;
            byte[] rgbIV = new byte[0x10];
            this._xform = this._aesCipher.CreateEncryptor(this._params.KeyBytes, rgbIV);
            if (this._mode == CryptoMode.Encrypt)
            {
                this._iobuf = new byte[0x800];
                this._PendingWriteBlock = new byte[0x10];
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
            this._length = length;
        }

        /// <summary>
        /// Close the stream.
        /// </summary>
        public override void Close()
        {
            if (this._pendingCount > 0)
            {
                this.WriteTransformFinalBlock();
                this._s.Write(this._PendingWriteBlock, 0, this._pendingCount);
                this._totalBytesXferred += this._pendingCount;
                this._pendingCount = 0;
            }
            this._s.Close();
        }

        /// <summary>
        /// Flush the content in the stream.
        /// </summary>
        public override void Flush()
        {
            this._s.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (this._mode == CryptoMode.Encrypt)
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
            if (this._totalBytesXferred >= this._length)
            {
                return 0;
            }
            long num2 = this._length - this._totalBytesXferred;
            if (num2 < count)
            {
                num = (int) num2;
            }
            int num3 = this._s.Read(buffer, offset, num);
            this.ReadTransformBlocks(buffer, offset, num);
            this._totalBytesXferred += num3;
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
            if (this._finalBlock)
            {
                throw new NotSupportedException();
            }
            int num = last - offset;
            int inputCount = (num > 0x10) ? 0x10 : num;
            Array.Copy(BitConverter.GetBytes(this._nonce++), 0, this.counter, 0, 4);
            if (((inputCount == num) && (this._length > 0L)) && ((this._totalBytesXferred + last) == this._length))
            {
                this._mac.TransformFinalBlock(buffer, offset, inputCount);
                this.counterOut = this._xform.TransformFinalBlock(this.counter, 0, 0x10);
                this._finalBlock = true;
            }
            else
            {
                this._mac.TransformBlock(buffer, offset, inputCount, null, 0);
                this._xform.TransformBlock(this.counter, 0, 0x10, this.counterOut, 0);
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
            lock (this._outputLock)
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
            if (this._finalBlock)
            {
                throw new InvalidOperationException("The final block has already been transformed.");
            }
            if (this._mode == CryptoMode.Decrypt)
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
                if ((count + this._pendingCount) <= 0x10)
                {
                    Buffer.BlockCopy(buffer, offset, this._PendingWriteBlock, this._pendingCount, count);
                    this._pendingCount += count;
                }
                else
                {
                    int num = count;
                    int srcOffset = offset;
                    if (this._pendingCount != 0)
                    {
                        int num3 = 0x10 - this._pendingCount;
                        if (num3 > 0)
                        {
                            Buffer.BlockCopy(buffer, offset, this._PendingWriteBlock, this._pendingCount, num3);
                            num -= num3;
                            srcOffset += num3;
                        }
                        this.WriteTransformOneBlock(this._PendingWriteBlock, 0);
                        this._s.Write(this._PendingWriteBlock, 0, 0x10);
                        this._totalBytesXferred += 0x10L;
                        this._pendingCount = 0;
                    }
                    int num4 = (num - 1) / 0x10;
                    this._pendingCount = num - (num4 * 0x10);
                    Buffer.BlockCopy(buffer, (srcOffset + num) - this._pendingCount, this._PendingWriteBlock, 0, this._pendingCount);
                    num -= this._pendingCount;
                    this._totalBytesXferred += num;
                    if (num4 > 0)
                    {
                        do
                        {
                            int length = this._iobuf.Length;
                            if (length > num)
                            {
                                length = num;
                            }
                            Buffer.BlockCopy(buffer, srcOffset, this._iobuf, 0, length);
                            this.WriteTransformBlocks(this._iobuf, 0, length);
                            this._s.Write(this._iobuf, 0, length);
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
            if (this._pendingCount == 0)
            {
                throw new InvalidOperationException("No bytes available.");
            }
            if (this._finalBlock)
            {
                throw new InvalidOperationException("The final block has already been transformed.");
            }
            Array.Copy(BitConverter.GetBytes(this._nonce++), 0, this.counter, 0, 4);
            this.counterOut = this._xform.TransformFinalBlock(this.counter, 0, 0x10);
            this.XorInPlace(this._PendingWriteBlock, 0, this._pendingCount);
            this._mac.TransformFinalBlock(this._PendingWriteBlock, 0, this._pendingCount);
            this._finalBlock = true;
        }

        private void WriteTransformOneBlock(byte[] buffer, int offset)
        {
            Array.Copy(BitConverter.GetBytes(this._nonce++), 0, this.counter, 0, 4);
            this._xform.TransformBlock(this.counter, 0, 0x10, this.counterOut, 0);
            this.XorInPlace(buffer, offset, 0x10);
            this._mac.TransformBlock(buffer, offset, 0x10, null, 0);
        }

        private void XorInPlace(byte[] buffer, int offset, int count)
        {
            for (int i = 0; i < count; i++)
            {
                buffer[offset + i] = (byte) (this.counterOut[i] ^ buffer[offset + i]);
            }
        }

        /// <summary>
        /// Returns true if the stream can be read.
        /// </summary>
        public override bool CanRead
        {
            get
            {
                if (this._mode != CryptoMode.Decrypt)
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
                return (this._mode == CryptoMode.Encrypt);
            }
        }

        /// <summary>
        /// Returns the final HMAC-SHA1-80 for the data that was encrypted.
        /// </summary>
        public byte[] FinalAuthentication
        {
            get
            {
                if (!this._finalBlock)
                {
                    if (this._totalBytesXferred != 0L)
                    {
                        throw new BadStateException("The final hash has not been computed.");
                    }
                    byte[] buffer = new byte[0];
                    this._mac.ComputeHash(buffer);
                }
                byte[] destinationArray = new byte[10];
                Array.Copy(this._mac.Hash, 0, destinationArray, 0, 10);
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

