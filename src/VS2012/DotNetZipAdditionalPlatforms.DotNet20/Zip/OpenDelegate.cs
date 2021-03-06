﻿namespace DotNetZipAdditionalPlatforms.Zip
{
    using System;
    using System.IO;
    using System.Runtime.CompilerServices;

    /// <summary>
    /// Delegate in which the application opens the stream, just-in-time, for the named entry.
    /// </summary>
    /// 
    /// <param name="entryName">
    /// The name of the ZipEntry that the application should open the stream for.
    /// </param>
    /// 
    /// <remarks>
    /// When you add an entry via <see cref="M:ZipFile.AddEntry(System.String,OpenDelegate,CloseDelegate)" />, the application code provides the logic that
    /// opens and closes the stream for the given ZipEntry.
    /// </remarks>
    /// 
    /// <seealso cref="M:ZipFile.AddEntry(System.String,OpenDelegate,CloseDelegate)" />
    public delegate Stream OpenDelegate(string entryName);
}

