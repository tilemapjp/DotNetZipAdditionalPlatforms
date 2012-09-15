namespace DotNetZipAdditionalPlatforms.Zip
{
    using System;
    using System.Runtime.InteropServices;
    using System.Runtime.Serialization;

    /// <summary>
    /// Issued when errors occur saving a self-extracting archive.
    /// </summary>
    [Serializable, Guid("ebc25cf6-9120-4283-b972-0e5520d00008")]
    public class SfxGenerationException : ZipException
    {
        /// <summary>
        /// Default ctor.
        /// </summary>
        public SfxGenerationException()
        {
        }

        /// <summary>
        /// Come on, you know how exceptions work. Why are you looking at this documentation?
        /// </summary>
        /// <param name="message">The message in the exception.</param>
        public SfxGenerationException(string message) : base(message)
        {
        }

        /// <summary>
        /// Come on, you know how exceptions work. Why are you looking at this documentation?
        /// </summary>
        /// <param name="info">The serialization info for the exception.</param>
        /// <param name="context">The streaming context from which to deserialize.</param>
        protected SfxGenerationException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}

