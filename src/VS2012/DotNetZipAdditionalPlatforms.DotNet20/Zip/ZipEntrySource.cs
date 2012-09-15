namespace DotNetZipAdditionalPlatforms.Zip
{
    using System;

    /// <summary>
    /// An enum that specifies the source of the ZipEntry. 
    /// </summary>
    public enum ZipEntrySource
    {
        /// <summary>
        /// The source of this ZipEntry is not known.
        /// </summary>
        None,

        /// <summary>
        /// This ZipEntry came from the file system.
        /// </summary>
        FileSystem,

        /// <summary>
        /// This ZipEntry came from the stream.
        /// </summary>
        Stream,

        /// <summary>
        /// This ZipEntry came from a zip file.
        /// </summary>
        ZipFile,

        /// <summary>
        /// This ZipEntry came from the write delegate.
        /// </summary>
        WriteDelegate,

        /// <summary>
        /// This ZipEntry came from the Jit stream.
        /// </summary>
        JitStream,

        /// <summary>
        /// This ZipEntry came from the zip output stream.
        /// </summary>
        ZipOutputStream
    }
}

