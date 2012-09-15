namespace DotNetZipAdditionalPlatforms.Zip
{
    using System;

    /// <summary>
    /// An enum that specifies the type of timestamp available on the ZipEntry.
    /// </summary>
    /// 
    /// <remarks>
    /// 
    /// <para>
    /// The last modified time of a file can be stored in multiple ways in
    /// a zip file, and they are not mutually exclusive:
    /// </para>
    /// 
    /// <list type="bullet">
    /// <item>
    /// In the so-called "DOS" format, which has a 2-second precision. Values
    /// are rounded to the nearest even second. For example, if the time on the
    /// file is 12:34:43, then it will be stored as 12:34:44. This first value
    /// is accessible via the <c>LastModified</c> property. This value is always
    /// present in the metadata for each zip entry.  In some cases the value is
    /// invalid, or zero.
    /// </item>
    /// 
    /// <item>
    /// In the so-called "Windows" or "NTFS" format, as an 8-byte integer
    /// quantity expressed as the number of 1/10 milliseconds (in other words
    /// the number of 100 nanosecond units) since January 1, 1601 (UTC).  This
    /// format is how Windows represents file times.  This time is accessible
    /// via the <c>ModifiedTime</c> property.
    /// </item>
    /// 
    /// <item>
    /// In the "Unix" format, a 4-byte quantity specifying the number of seconds since
    /// January 1, 1970 UTC.
    /// </item>
    /// 
    /// <item>
    /// In an older format, now deprecated but still used by some current
    /// tools. This format is also a 4-byte quantity specifying the number of
    /// seconds since January 1, 1970 UTC.
    /// </item>
    /// 
    /// </list>
    /// 
    /// <para>
    /// This bit field describes which of the formats were found in a <c>ZipEntry</c> that was read.
    /// </para>
    /// 
    /// </remarks>
    [Flags]
    public enum ZipEntryTimestamp
    {
        /// <summary>
        /// A DOS timestamp with 2-second precision.
        /// </summary>
        DOS = 1,
        /// <summary>
        /// A Unix timestamp with 1-second precision, stored in InfoZip v1 format.  This
        /// format is outdated and is supported for reading archives only.
        /// </summary>
        InfoZip1 = 8,
        /// <summary>
        /// Default value.
        /// </summary>
        None = 0,
        /// <summary>
        /// A Unix timestamp with 1-second precision.
        /// </summary>
        Unix = 4,
        /// <summary>
        /// A Windows timestamp with 100-ns precision.
        /// </summary>
        Windows = 2
    }
}

