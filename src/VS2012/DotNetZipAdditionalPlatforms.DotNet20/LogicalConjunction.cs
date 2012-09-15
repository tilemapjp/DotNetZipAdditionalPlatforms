namespace DotNetZipAdditionalPlatforms
{
    using System;

    /// <summary>
    /// Enumerates the options for a logical conjunction. This enum is intended for use
    /// internally by the FileSelector class.
    /// </summary>
    internal enum LogicalConjunction
    {
        NONE,
        AND,
        OR,
        XOR
    }
}

