namespace DotNetZipAdditionalPlatforms.Zlib
{
    using System;

    internal class WorkItem
    {
        public byte[] buffer;
        public byte[] compressed;
        public int compressedBytesAvailable;
        public ZlibCodec compressor;
        public int crc;
        public int index;
        public int inputBytesAvailable;
        public int ordinal;

        public WorkItem(int size, CompressionLevel compressLevel, CompressionStrategy strategy, int ix)
        {
            this.buffer = new byte[size];
            int num = size + ((((size / 0x8000) + 1) * 5) * 2);
            this.compressed = new byte[num];
            this.compressor = new ZlibCodec();
            this.compressor.InitializeDeflate(compressLevel, false);
            this.compressor.OutputBuffer = this.compressed;
            this.compressor.InputBuffer = this.buffer;
            this.index = ix;
        }
    }
}

