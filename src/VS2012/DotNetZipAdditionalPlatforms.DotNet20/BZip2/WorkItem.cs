namespace DotNetZipAdditionalPlatforms.BZip2
{
    using System;
    using System.IO;
    using System.Runtime.CompilerServices;

    internal class WorkItem
    {
        public BitWriter bitWriterField;
        public int indexField;
        public MemoryStream memoryStreamField = new MemoryStream();
        public int ordinalField;

        public WorkItem(int ix, int blockSize)
        {
            this.bitWriterField = new BitWriter(this.memoryStreamField);
            this.Compressor = new BZip2Compressor(this.bitWriterField, blockSize);
            this.indexField = ix;
        }

        public BZip2Compressor Compressor { get; set; }
    }
}

