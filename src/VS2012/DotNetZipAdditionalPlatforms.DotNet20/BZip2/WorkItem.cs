namespace DotNetZipAdditionalPlatforms.BZip2
{
    using System;
    using System.IO;
    using System.Runtime.CompilerServices;

    internal class WorkItem
    {
        public BitWriter bw;
        public int index;
        public MemoryStream ms = new MemoryStream();
        public int ordinal;

        public WorkItem(int ix, int blockSize)
        {
            this.bw = new BitWriter(this.ms);
            this.Compressor = new BZip2Compressor(this.bw, blockSize);
            this.index = ix;
        }

        public BZip2Compressor Compressor { get; set; }
    }
}

