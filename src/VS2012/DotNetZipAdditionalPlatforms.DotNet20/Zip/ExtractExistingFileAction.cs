namespace DotNetZipAdditionalPlatforms.Zip
{
    using System;

    /// <summary>
    /// An enum for the options when extracting an entry would overwrite an existing file. 
    /// </summary>
    /// 
    /// <remarks>
    /// <para>
    /// This enum describes the actions that the library can take when an
    /// <c>Extract()</c> or <c>ExtractWithPassword()</c> method is called to extract an
    /// entry to a filesystem, and the extraction would overwrite an existing filesystem
    /// file.
    /// </para>
    /// </remarks>
    public enum ExtractExistingFileAction
    {
        /// <summary>
        /// When extracting an entry would overwrite an existing file, throw an exception.
        /// </summary>
        Throw,

        /// <summary>
        /// When extracting an entry would overwrite an existing file, overwrite the file silently.
        /// </summary>
        OverwriteSilently,

        /// <summary>
        /// When extracting an entry would overwrite an existing file, just skip the file and do not overwrite it.
        /// </summary>
        DoNotOverwrite,

        /// <summary>
        /// When extracting an entry would overwrite an existing file, invoke the extract progress event.
        /// </summary>
        InvokeExtractProgressEvent
    }
}

