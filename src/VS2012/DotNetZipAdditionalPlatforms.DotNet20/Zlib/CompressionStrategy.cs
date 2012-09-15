namespace DotNetZipAdditionalPlatforms.Zlib
{
    using System;

    /// <summary>
    /// Describes options for how the compression algorithm is executed.  Different strategies
    /// work better on different sorts of data.  The strategy parameter can affect the compression
    /// ratio and the speed of compression but not the correctness of the compresssion.
    /// </summary>
    public enum CompressionStrategy
    {
        /// <summary>
        /// Use the default compression strategy.
        /// </summary>
        Default,

        /// <summary>
        /// Use the filtered compression strategy.
        /// </summary>
        Filtered,

        /// <summary>
        /// Use the Huffman only compression strategy.
        /// </summary>
        HuffmanOnly
    }
}

