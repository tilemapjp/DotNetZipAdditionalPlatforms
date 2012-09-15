namespace DotNetZipAdditionalPlatforms.Zlib
{
    using System;

    /// <summary>
    /// Describes how to flush the current deflate operation.
    /// </summary>
    /// <remarks>
    /// The different FlushType values are useful when using a Deflate in a streaming application.
    /// </remarks>
    public enum FlushType
    {
        /// <summary>
        /// Do not flush when using Deflate in a streaming application.
        /// </summary>
        None,

        /// <summary>
        /// Do a partial flush when using Deflate in a streaming application.
        /// </summary>
        Partial,

        /// <summary>
        /// Do a sync flush when using Deflate in a streaming application.
        /// </summary>
        Sync,

        /// <summary>
        /// Do a full flush when using Deflate in a streaming application.
        /// </summary>
        Full,

        /// <summary>
        /// Flush when finished when using Deflate in a streaming application.
        /// </summary>
        Finish
    }
}

