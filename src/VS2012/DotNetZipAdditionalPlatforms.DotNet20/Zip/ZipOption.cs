namespace DotNetZipAdditionalPlatforms.Zip
{
    using System;

    /// <summary>
    /// An enum representing the values on a three-way toggle switch
    /// for various options in the library. This might be used to
    /// specify whether to employ a particular text encoding, or to use
    /// ZIP64 extensions, or some other option.
    /// </summary>
    public enum ZipOption
    {
        /// <summary>
        /// Use the associated behavior Always, whether necessary or not.
        /// (For COM clients, this is a 2.)
        /// </summary>
        Always = 2,
        /// <summary>
        /// Use the associated behavior "as necessary."
        /// (For COM clients, this is a 1.)
        /// </summary>
        AsNecessary = 1,
        /// <summary>
        /// The default behavior. This is the same as "Never".
        /// (For COM clients, this is a 0 (zero).)
        /// </summary>
        Default = 0,
        /// <summary>
        /// Never use the associated option.
        /// (For COM clients, this is a 0 (zero).)
        /// </summary>
        Never = 0
    }
}

