namespace DotNetZipAdditionalPlatforms.Zip
{
    using DotNetZipAdditionalPlatforms.Zlib;
    using System;
    using System.IO;
    using System.Text;

    internal class ZipContainer
    {
        private ZipFile _zf;
        private ZipInputStream _zis;
        private ZipOutputStream _zos;

        public ZipContainer(object o)
        {
            this._zf = o as ZipFile;
            this._zos = o as ZipOutputStream;
            this._zis = o as ZipInputStream;
        }

        public Encoding AlternateEncoding
        {
            get
            {
                if (this._zf != null)
                {
                    return this._zf.AlternateEncoding;
                }
                if (this._zos != null)
                {
                    return this._zos.AlternateEncoding;
                }
                return null;
            }
        }

        public ZipOption AlternateEncodingUsage
        {
            get
            {
                if (this._zf != null)
                {
                    return this._zf.AlternateEncodingUsage;
                }
                if (this._zos != null)
                {
                    return this._zos.AlternateEncodingUsage;
                }
                return ZipOption.Default;
            }
        }

        public int BufferSize
        {
            get
            {
                if (this._zf != null)
                {
                    return this._zf.BufferSize;
                }
                if (this._zis != null)
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
                if (this._zf != null)
                {
                    return this._zf.CodecBufferSize;
                }
                if (this._zis != null)
                {
                    return this._zis.CodecBufferSize;
                }
                return this._zos.CodecBufferSize;
            }
        }

        public Encoding DefaultEncoding
        {
            get
            {
                if (this._zf != null)
                {
                    return ZipFile.DefaultEncoding;
                }
                if (this._zos != null)
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
                if (this._zf != null)
                {
                    return this._zf.Name;
                }
                if (this._zis != null)
                {
                    throw new NotSupportedException();
                }
                return this._zos.Name;
            }
        }

        public int ParallelDeflateMaxBufferPairs
        {
            get
            {
                if (this._zf != null)
                {
                    return this._zf.ParallelDeflateMaxBufferPairs;
                }
                return this._zos.ParallelDeflateMaxBufferPairs;
            }
        }

        public ParallelDeflateOutputStream ParallelDeflater
        {
            get
            {
                if (this._zf != null)
                {
                    return this._zf.ParallelDeflater;
                }
                if (this._zis != null)
                {
                    return null;
                }
                return this._zos.ParallelDeflater;
            }
            set
            {
                if (this._zf != null)
                {
                    this._zf.ParallelDeflater = value;
                }
                else if (this._zos != null)
                {
                    this._zos.ParallelDeflater = value;
                }
            }
        }

        public long ParallelDeflateThreshold
        {
            get
            {
                if (this._zf != null)
                {
                    return this._zf.ParallelDeflateThreshold;
                }
                return this._zos.ParallelDeflateThreshold;
            }
        }

        public string Password
        {
            get
            {
                if (this._zf != null)
                {
                    return this._zf._Password;
                }
                if (this._zis != null)
                {
                    return this._zis._Password;
                }
                return this._zos._password;
            }
        }

        public Stream ReadStream
        {
            get
            {
                if (this._zf != null)
                {
                    return this._zf.ReadStream;
                }
                return this._zis.ReadStream;
            }
        }

        public CompressionStrategy Strategy
        {
            get
            {
                if (this._zf != null)
                {
                    return this._zf.Strategy;
                }
                return this._zos.Strategy;
            }
        }

        public Zip64Option UseZip64WhenSaving
        {
            get
            {
                if (this._zf != null)
                {
                    return this._zf.UseZip64WhenSaving;
                }
                return this._zos.EnableZip64;
            }
        }

        public Zip64Option Zip64
        {
            get
            {
                if (this._zf != null)
                {
                    return this._zf._zip64;
                }
                if (this._zis != null)
                {
                    throw new NotSupportedException();
                }
                return this._zos._zip64;
            }
        }

        public ZipFile ZipFile
        {
            get
            {
                return this._zf;
            }
        }

        public ZipOutputStream ZipOutputStream
        {
            get
            {
                return this._zos;
            }
        }
    }
}

