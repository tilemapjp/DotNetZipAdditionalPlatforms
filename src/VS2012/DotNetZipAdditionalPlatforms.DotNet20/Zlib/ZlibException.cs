namespace DotNetZipAdditionalPlatforms.Zlib
{
    using System;
    using System.Runtime.InteropServices;

    /// <summary>
    /// A general purpose exception class for exceptions in the Zlib library.
    /// </summary>
    public class ZlibException : Exception
    {
        /// <summary>
        /// The ZlibException class captures exception information generated
        /// by the Zlib library.
        /// </summary>
        public ZlibException()
        {
        }

        /// <summary>
        /// This ctor collects a message attached to the exception.
        /// </summary>
        /// <param name="s">the message for the exception.</param>
        public ZlibException(string s) : base(s)
        {
        }
    }
}

