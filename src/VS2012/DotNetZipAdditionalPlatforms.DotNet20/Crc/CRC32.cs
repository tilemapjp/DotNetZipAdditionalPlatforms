namespace DotNetZipAdditionalPlatforms.Crc
{
    using System;
    using System.IO;
    using System.Runtime.InteropServices;

    /// <summary>
    /// Computes a CRC-32. The CRC-32 algorithm is parameterized - you
    /// can set the polynomial and enable or disable bit
    /// reversal. This can be used for GZIP, BZip2, or ZIP.
    /// </summary>
    /// <remarks>
    /// This type is used internally by DotNetZip; it is generally not used
    /// directly by applications wishing to create, read, or manipulate zip
    /// archive files.
    /// </remarks>
    public class CRC32
    {
        private uint registerField;
        private long totalBytesReadField;
        private const int BUFFER_SIZE = 0x2000;
        private uint[] crc32TableField;
        private uint dwPolynomialField;
        private bool reverseBitsField;

        /// <summary>
        /// Create an instance of the CRC32 class using the default settings: no
        /// bit reversal, and a polynomial of 0xEDB88320.
        /// </summary>
        public CRC32() : this(false)
        {
        }

        /// <summary>
        /// Create an instance of the CRC32 class, specifying whether to reverse
        /// data bits or not.
        /// </summary>
        /// <param name="reverseBits">
        /// specify true if the instance should reverse data bits.
        /// </param>
        /// <remarks>
        /// <para>
        /// In the CRC-32 used by BZip2, the bits are reversed. Therefore if you
        /// want a CRC32 with compatibility with BZip2, you should pass true
        /// here. In the CRC-32 used by GZIP and PKZIP, the bits are not
        /// reversed; Therefore if you want a CRC32 with compatibility with
        /// those, you should pass false.
        /// </para>
        /// </remarks>
        public CRC32(bool reverseBits) : this(-306674912, reverseBits)
        {
        }

        /// <summary>
        /// Create an instance of the CRC32 class, specifying the polynomial and
        /// whether to reverse data bits or not.
        /// </summary>
        /// <param name="polynomial">
        /// The polynomial to use for the CRC, expressed in the reversed (LSB)
        /// format: the highest ordered bit in the polynomial value is the
        /// coefficient of the 0th power; the second-highest order bit is the
        /// coefficient of the 1 power, and so on. Expressed this way, the
        /// polynomial for the CRC-32C used in IEEE 802.3, is 0xEDB88320.
        /// </param>
        /// <param name="reverseBits">
        /// specify true if the instance should reverse data bits.
        /// </param>
        /// 
        /// <remarks>
        /// <para>
        /// In the CRC-32 used by BZip2, the bits are reversed. Therefore if you
        /// want a CRC32 with compatibility with BZip2, you should pass true
        /// here for the <c>reverseBits</c> parameter. In the CRC-32 used by
        /// GZIP and PKZIP, the bits are not reversed; Therefore if you want a
        /// CRC32 with compatibility with those, you should pass false for the
        /// <c>reverseBits</c> parameter.
        /// </para>
        /// </remarks>
        public CRC32(int polynomial, bool reverseBits)
        {
            this.registerField = uint.MaxValue;
            this.reverseBitsField = reverseBits;
            this.dwPolynomialField = (uint) polynomial;
            this.GenerateLookupTable();
        }

        internal int _InternalComputeCrc32(uint W, byte B)
        {
            return (int) (this.crc32TableField[(int) ((IntPtr) ((W ^ B) & 0xff))] ^ (W >> 8));
        }

        /// <summary>
        /// Combines the given CRC32 value with the current running total.
        /// </summary>
        /// <remarks>
        /// This is useful when using a divide-and-conquer approach to
        /// calculating a CRC.  Multiple threads can each calculate a
        /// CRC32 on a segment of the data, and then combine the
        /// individual CRC32 values at the end.
        /// </remarks>
        /// <param name="crc">the crc value to be combined with this one</param>
        /// <param name="length">the length of data the CRC value was calculated on</param>
        public void Combine(int crc, int length)
        {
            uint[] square = new uint[0x20];
            uint[] mat = new uint[0x20];
            if (length != 0)
            {
                uint vec = ~this.registerField;
                uint num2 = (uint) crc;
                mat[0] = this.dwPolynomialField;
                uint num3 = 1;
                for (int i = 1; i < 0x20; i++)
                {
                    mat[i] = num3;
                    num3 = num3 << 1;
                }
                this.gf2_matrix_square(square, mat);
                this.gf2_matrix_square(mat, square);
                uint num5 = (uint) length;
                do
                {
                    this.gf2_matrix_square(square, mat);
                    if ((num5 & 1) == 1)
                    {
                        vec = this.gf2_matrix_times(square, vec);
                    }
                    num5 = num5 >> 1;
                    if (num5 == 0)
                    {
                        break;
                    }
                    this.gf2_matrix_square(mat, square);
                    if ((num5 & 1) == 1)
                    {
                        vec = this.gf2_matrix_times(mat, vec);
                    }
                    num5 = num5 >> 1;
                }
                while (num5 != 0);
                vec ^= num2;
                this.registerField = ~vec;
            }
        }

        /// <summary>
        /// Get the CRC32 for the given (word,byte) combo.  This is a
        /// computation defined by PKzip for PKZIP 2.0 (weak) encryption.
        /// </summary>
        /// <param name="W">The word to start with.</param>
        /// <param name="B">The byte to combine it with.</param>
        /// <returns>The CRC-ized result.</returns>
        public int ComputeCrc32(int W, byte B)
        {
            return this._InternalComputeCrc32((uint) W, B);
        }

        private void GenerateLookupTable()
        {
            this.crc32TableField = new uint[0x100];
            byte data = 0;
            do
            {
                uint num = data;
                for (byte i = 8; i > 0; i = (byte) (i - 1))
                {
                    if ((num & 1) == 1)
                    {
                        num = (num >> 1) ^ this.dwPolynomialField;
                    }
                    else
                    {
                        num = num >> 1;
                    }
                }
                if (this.reverseBitsField)
                {
                    this.crc32TableField[ReverseBits(data)] = ReverseBits(num);
                }
                else
                {
                    this.crc32TableField[data] = num;
                }
                data = (byte) (data + 1);
            }
            while (data != 0);
        }

        /// <summary>
        /// Returns the CRC32 for the specified stream.
        /// </summary>
        /// <param name="input">The stream over which to calculate the CRC32</param>
        /// <returns>the CRC32 calculation</returns>
        public int GetCrc32(Stream input)
        {
            return this.GetCrc32AndCopy(input, null);
        }

        /// <summary>
        /// Returns the CRC32 for the specified stream, and writes the input into the
        /// output stream.
        /// </summary>
        /// <param name="input">The stream over which to calculate the CRC32</param>
        /// <param name="output">The stream into which to deflate the input</param>
        /// <returns>the CRC32 calculation</returns>
        public int GetCrc32AndCopy(Stream input, Stream output)
        {
            if (input == null)
            {
                throw new Exception("The input stream must not be null.");
            }
            byte[] buffer = new byte[0x2000];
            int count = 0x2000;
            this.totalBytesReadField = 0L;
            int num2 = input.Read(buffer, 0, count);
            if (output != null)
            {
                output.Write(buffer, 0, num2);
            }
            this.totalBytesReadField += num2;
            while (num2 > 0)
            {
                this.SlurpBlock(buffer, 0, num2);
                num2 = input.Read(buffer, 0, count);
                if (output != null)
                {
                    output.Write(buffer, 0, num2);
                }
                this.totalBytesReadField += num2;
            }
            return (int) ~this.registerField;
        }

        private void gf2_matrix_square(uint[] square, uint[] mat)
        {
            for (int i = 0; i < 0x20; i++)
            {
                square[i] = this.gf2_matrix_times(mat, mat[i]);
            }
        }

        private uint gf2_matrix_times(uint[] matrix, uint vec)
        {
            uint num = 0;
            for (int i = 0; vec != 0; i++)
            {
                if ((vec & 1) == 1)
                {
                    num ^= matrix[i];
                }
                vec = vec >> 1;
            }
            return num;
        }

        /// <summary>
        /// Reset the CRC-32 class - clear the CRC "remainder register."
        /// </summary>
        /// <remarks>
        /// <para>
        /// Use this when employing a single instance of this class to compute
        /// multiple, distinct CRCs on multiple, distinct data blocks.
        /// </para>
        /// </remarks>
        public void Reset()
        {
            this.registerField = uint.MaxValue;
        }

        private static byte ReverseBits(byte data)
        {
            uint num = (uint) (data * 0x20202);
            uint num2 = 0x1044010;
            uint num3 = num & num2;
            uint num4 = (num << 2) & (num2 << 1);
            return (byte) ((0x1001001 * (num3 + num4)) >> 0x18);
        }

        private static uint ReverseBits(uint data)
        {
            uint num = data;
            num = ((uint) ((num & 0x55555555) << 1)) | ((num >> 1) & 0x55555555);
            num = ((uint) ((num & 0x33333333) << 2)) | ((num >> 2) & 0x33333333);
            num = ((uint) ((num & 0xf0f0f0f) << 4)) | ((num >> 4) & 0xf0f0f0f);
            return ((((num << 0x18) | ((uint) ((num & 0xff00) << 8))) | ((num >> 8) & 0xff00)) | (num >> 0x18));
        }

        /// <summary>
        /// Update the value for the running CRC32 using the given block of bytes.
        /// This is useful when using the CRC32() class in a Stream.
        /// </summary>
        /// <param name="block">block of bytes to slurp</param>
        /// <param name="offset">starting point in the block</param>
        /// <param name="count">how many bytes within the block to slurp</param>
        public void SlurpBlock(byte[] block, int offset, int count)
        {
            if (block == null)
            {
                throw new Exception("The data buffer must not be null.");
            }
            for (int i = 0; i < count; i++)
            {
                uint num4;
                int index = offset + i;
                byte num3 = block[index];
                if (this.reverseBitsField)
                {
                    num4 = (this.registerField >> 0x18) ^ num3;
                    this.registerField = (this.registerField << 8) ^ this.crc32TableField[num4];
                }
                else
                {
                    num4 = (this.registerField & 0xff) ^ num3;
                    this.registerField = (this.registerField >> 8) ^ this.crc32TableField[num4];
                }
            }
            this.totalBytesReadField += count;
        }

        /// <summary>
        /// Process one byte in the CRC.
        /// </summary>
        /// <param name="b">the byte to include into the CRC .  </param>
        public void UpdateCRC(byte b)
        {
            uint num;
            if (this.reverseBitsField)
            {
                num = (this.registerField >> 0x18) ^ b;
                this.registerField = (this.registerField << 8) ^ this.crc32TableField[num];
            }
            else
            {
                num = (this.registerField & 0xff) ^ b;
                this.registerField = (this.registerField >> 8) ^ this.crc32TableField[num];
            }
        }

        /// <summary>
        /// Process a run of N identical bytes into the CRC.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This method serves as an optimization for updating the CRC when a
        /// run of identical bytes is found. Rather than passing in a buffer of
        /// length n, containing all identical bytes b, this method accepts the
        /// byte value and the length of the (virtual) buffer - the length of
        /// the run.
        /// </para>
        /// </remarks>
        /// <param name="b">the byte to include into the CRC.  </param>
        /// <param name="n">the number of times that byte should be repeated. </param>
        public void UpdateCRC(byte b, int n)
        {
            while (n-- > 0)
            {
                uint num;
                if (this.reverseBitsField)
                {
                    num = (this.registerField >> 0x18) ^ b;
                    this.registerField = (this.registerField << 8) ^ this.crc32TableField[(num >= 0) ? ((int) num) : ((int) (num + 0x100))];
                }
                else
                {
                    num = (this.registerField & 0xff) ^ b;
                    this.registerField = (this.registerField >> 8) ^ this.crc32TableField[(num >= 0) ? ((int) num) : ((int) (num + 0x100))];
                }
            }
        }

        /// <summary>
        /// Indicates the current CRC for all blocks slurped in.
        /// </summary>
        public int Crc32Result
        {
            get
            {
                return (int) ~this.registerField;
            }
        }

        /// <summary>
        /// Indicates the total number of bytes applied to the CRC.
        /// </summary>
        public long TotalBytesRead
        {
            get
            {
                return this.totalBytesReadField;
            }
        }
    }
}

