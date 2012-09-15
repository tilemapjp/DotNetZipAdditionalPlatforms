namespace DotNetZipAdditionalPlatforms.Zip
{
    using DotNetZipAdditionalPlatforms.Zlib;
    using System;
    using System.Runtime.CompilerServices;

    /// <summary>
    /// Delegate for the callback by which the application tells the
    /// library the CompressionLevel to use for a file.
    /// </summary>
    /// 
    /// <remarks>
    /// <para>
    /// Using this callback, the application can, for example, specify that
    /// previously-compressed files (.mp3, .png, .docx, etc) should use a
    /// <c>CompressionLevel</c> of <c>None</c>, or can set the compression level based
    /// on any other factor.
    /// </para>
    /// </remarks>
    /// <seealso cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.SetCompression" />
    public delegate CompressionLevel SetCompressionCallback(string localFileName, string fileNameInArchive);
}

