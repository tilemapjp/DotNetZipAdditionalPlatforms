namespace DotNetZipAdditionalPlatforms.Zip
{
    using DotNetZipAdditionalPlatforms.Zlib;
    using System;
    using System.IO;
    using System.Text;

    internal class ZipContainer
    {
        private ZipFile zipFileField;
        private ZipInputStream zipInputStreamField;
        private ZipOutputStream zipOutputStreamField;

        public ZipContainer(object o)
        {
            this.zipFileField = o as ZipFile;
            this.zipOutputStreamField = o as ZipOutputStream;
            this.zipInputStreamField = o as ZipInputStream;
        }

        public Encoding AlternateEncoding
        {
            get
            {
                if (this.zipFileField != null)
                {
                    return this.zipFileField.AlternateEncoding;
                }
                if (this.zipOutputStreamField != null)
                {
                    return this.zipOutputStreamField.AlternateEncoding;
                }
                return null;
            }
        }

        public ZipOption AlternateEncodingUsage
        {
            get
            {
                if (this.zipFileField != null)
                {
                    return this.zipFileField.AlternateEncodingUsage;
                }
                if (this.zipOutputStreamField != null)
                {
                    return this.zipOutputStreamField.AlternateEncodingUsage;
                }
                return ZipOption.Default;
            }
        }

        public int BufferSize
        {
            get
            {
                if (this.zipFileField != null)
                {
                    return this.zipFileField.BufferSize;
                }
                if (this.zipInputStreamField != null)
                {
                    throw new NotSupportedException();
                }
                return 0;
            }
        }

        public int CodecBufferSize
        {
            get
            {
                if (this.zipFileField != null)
                {
                    return this.zipFileField.CodecBufferSize;
                }
                if (this.zipInputStreamField != null)
                {
                    return this.zipInputStreamField.CodecBufferSize;
                }
                return this.zipOutputStreamField.CodecBufferSize;
            }
        }

        public Encoding DefaultEncoding
        {
            get
            {
                if (this.zipFileField != null)
                {
                    return ZipFile.DefaultEncoding;
                }
                if (this.zipOutputStreamField != null)
                {
                    return ZipOutputStream.DefaultEncoding;
                }
                return null;
            }
        }

        public string Name
        {
            get
            {
                if (this.zipFileField != null)
                {
                    return this.zipFileField.Name;
                }
                if (this.zipInputStreamField != null)
                {
                    throw new NotSupportedException();
                }
                return this.zipOutputStreamField.Name;
            }
        }

        public int ParallelDeflateMaxBufferPairs
        {
            get
            {
                if (this.zipFileField != null)
                {
                    return this.zipFileField.ParallelDeflateMaxBufferPairs;
                }
                return this.zipOutputStreamField.ParallelDeflateMaxBufferPairs;
            }
        }

        public ParallelDeflateOutputStream ParallelDeflater
        {
            get
            {
                if (this.zipFileField != null)
                {
                    return this.zipFileField.parallelDeflaterField;
                }
                if (this.zipInputStreamField != null)
                {
                    return null;
                }
                return this.zipOutputStreamField.parallelDeflaterField;
            }
            set
            {
                if (this.zipFileField != null)
                {
                    this.zipFileField.parallelDeflaterField = value;
                }
                else if (this.zipOutputStreamField != null)
                {
                    this.zipOutputStreamField.parallelDeflaterField = value;
                }
            }
        }

        public long ParallelDeflateThreshold
        {
            get
            {
                if (this.zipFileField != null)
                {
                    return this.zipFileField.ParallelDeflateThreshold;
                }
                return this.zipOutputStreamField.ParallelDeflateThreshold;
            }
        }

        public string Password
        {
            get
            {
                if (this.zipFileField != null)
                {
                    return this.zipFileField.passwordField;
                }
                if (this.zipInputStreamField != null)
                {
                    return this.zipInputStreamField.passwordField;
                }
                return this.zipOutputStreamField.passwordField;
            }
        }

        public Stream ReadStream
        {
            get
            {
                if (this.zipFileField != null)
                {
                    return this.zipFileField.ReadStream;
                }
                return this.zipInputStreamField.ReadStream;
            }
        }

        public CompressionStrategy Strategy
        {
            get
            {
                if (this.zipFileField != null)
                {
                    return this.zipFileField.Strategy;
                }
                return this.zipOutputStreamField.Strategy;
            }
        }

        public Zip64Option UseZip64WhenSaving
        {
            get
            {
                if (this.zipFileField != null)
                {
                    return this.zipFileField.UseZip64WhenSaving;
                }
                return this.zipOutputStreamField.EnableZip64;
            }
        }

        public Zip64Option Zip64
        {
            get
            {
                if (this.zipFileField != null)
                {
                    return this.zipFileField.zip64Field;
                }
                if (this.zipInputStreamField != null)
                {
                    throw new NotSupportedException();
                }
                return this.zipOutputStreamField.zip64Field;
            }
        }

        public ZipFile ZipFile
        {
            get
            {
                return this.zipFileField;
            }
        }

        public ZipOutputStream ZipOutputStream
        {
            get
            {
                return this.zipOutputStreamField;
            }
        }
    }
}

