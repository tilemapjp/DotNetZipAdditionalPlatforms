namespace DotNetZipAdditionalPlatforms.Zip
{
    using System;

    /// <summary>
    /// An enum providing the options when an error occurs during opening or reading
    /// of a file or directory that is being saved to a zip file. 
    /// </summary>
    /// 
    /// <remarks>
    /// <para>
    /// This enum describes the actions that the library can take when an error occurs
    /// opening or reading a file, as it is being saved into a Zip archive. 
    /// </para>
    /// 
    /// <para>
    /// In some cases an error will occur when DotNetZip tries to open a file to be
    /// added to the zip archive.  In other cases, an error might occur after the
    /// file has been successfully opened, while DotNetZip is reading the file.
    /// </para>
    /// 
    /// <para>
    /// The first problem might occur when calling AddDirectory() on a directory
    /// that contains a Clipper .dbf file; the file is locked by Clipper and
    /// cannot be opened by another process. An example of the second problem is
    /// the ERROR_LOCK_VIOLATION that results when a file is opened by another
    /// process, but not locked, and a range lock has been taken on the file.
    /// Microsoft Outlook takes range locks on .PST files.
    /// </para>
    /// </remarks>
    public enum ZipErrorAction
    {
        /// <summary>
        /// Throw exception if an error occurs during opening or reading
        /// of a file or directory that is being saved to a zip file.
        /// </summary>
        Throw,

        /// <summary>
        /// Don't do anything if an error occurs during opening or reading
        /// of a file or directory that is being saved to a zip file.
        /// </summary>
        Skip,

        /// <summary>
        /// Retry if an error occurs during opening or reading
        /// of a file or directory that is being saved to a zip file.
        /// </summary>
        Retry,

        /// <summary>
        /// Invoke error event if an error occurs during opening or reading
        /// of a file or directory that is being saved to a zip file.
        /// </summary>
        InvokeErrorEvent
    }
}

