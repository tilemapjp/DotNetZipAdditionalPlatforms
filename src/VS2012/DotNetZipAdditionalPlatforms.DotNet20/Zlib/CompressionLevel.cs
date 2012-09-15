namespace DotNetZipAdditionalPlatforms.Zlib
{
    using System;

    /// <summary>
    /// The compression level to be used when using a DeflateStream or ZlibStream with CompressionMode.Compress.
    /// </summary>
    public enum CompressionLevel
    {
        /// <summary>
        /// The "best" compression, where best means greatest reduction in size of the input data stream.
        /// This is also the slowest compression.
        /// </summary>
        BestCompression = 9,
        /// <summary>
        /// The fastest but least effective compression.
        /// </summary>
        BestSpeed = 1,
        /// <summary>
        /// The default compression level, with a good balance of speed and compression efficiency.
        /// </summary>
        Default = 6,
        /// <summary>
        /// Same as None.
        /// </summary>
        Level0 = 0,
        /// <summary>
        /// A synonym for BestSpeed.
        /// </summary>
        Level1 = 1,
        /// <summary>
        /// A little slower, but better, than level 1.
        /// </summary>
        Level2 = 2,
        /// <summary>
        /// A little slower, but better, than level 2.
        /// </summary>
        Level3 = 3,
        /// <summary>
        /// A little slower, but better, than level 3.
        /// </summary>
        Level4 = 4,
        /// <summary>
        /// A little slower than level 4, but with better compression.
        /// </summary>
        Level5 = 5,
        /// <summary>
        /// A synonym for Default.
        /// </summary>
        Level6 = 6,
        /// <summary>
        /// Pretty good compression!
        /// </summary>
        Level7 = 7,
        /// <summary>
        /// Better compression than Level7!
        /// </summary>
        Level8 = 8,
        /// <summary>
        /// A synonym for BestCompression.
        /// </summary>
        Level9 = 9,
        /// <summary>
        /// None means that the data will be simply stored, with no change at all.
        /// If you are producing ZIPs for use on Mac OSX, be aware that archives produced with CompressionLevel.None
        /// cannot be opened with the default zip reader. Use a different CompressionLevel.
        /// </summary>
        None = 0
    }
}

