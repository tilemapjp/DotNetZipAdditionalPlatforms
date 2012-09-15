namespace DotNetZipAdditionalPlatforms.Zip
{
    using System;

    /// <summary>
    /// Options for using ZIP64 extensions when saving zip archives.
    /// </summary>
    /// 
    /// <remarks>
    /// 
    /// <para>
    /// Designed many years ago, the <see href="http://www.pkware.com/documents/casestudies/APPNOTE.TXT">original zip
    /// specification from PKWARE</see> allowed for 32-bit quantities for the
    /// compressed and uncompressed sizes of zip entries, as well as a 32-bit quantity
    /// for specifying the length of the zip archive itself, and a maximum of 65535
    /// entries.  These limits are now regularly exceeded in many backup and archival
    /// scenarios.  Recently, PKWare added extensions to the original zip spec, called
    /// "ZIP64 extensions", to raise those limitations.  This property governs whether
    /// DotNetZip will use those extensions when writing zip archives. The use of
    /// these extensions is optional and explicit in DotNetZip because, despite the
    /// status of ZIP64 as a bona fide standard, many other zip tools and libraries do
    /// not support ZIP64, and therefore a zip file with ZIP64 extensions may be
    /// unreadable by some of those other tools.
    /// </para>
    /// 
    /// <para>
    /// Set this property to <see cref="F:Zip64Option.Always" /> to always use ZIP64
    /// extensions when saving, regardless of whether your zip archive needs it.
    /// Suppose you add 5 files, each under 100k, to a ZipFile. If you specify Always
    /// for this flag, you will get a ZIP64 archive, though the archive does not need
    /// to use ZIP64 because none of the original zip limits had been exceeded.
    /// </para>
    /// 
    /// <para>
    /// Set this property to <see cref="F:Zip64Option.Never" /> to tell the DotNetZip
    /// library to never use ZIP64 extensions.  This is useful for maximum
    /// compatibility and interoperability, at the expense of the capability of
    /// handling large files or large archives.  NB: Windows Explorer in Windows XP
    /// and Windows Vista cannot currently extract files from a zip64 archive, so if
    /// you want to guarantee that a zip archive produced by this library will work in
    /// Windows Explorer, use <c>Never</c>. If you set this property to <see cref="F:Zip64Option.Never" />, and your application creates a zip that would
    /// exceed one of the Zip limits, the library will throw an exception while saving
    /// the zip file.
    /// </para>
    /// 
    /// <para>
    /// Set this property to <see cref="F:Zip64Option.AsNecessary" /> to tell the
    /// DotNetZip library to use the ZIP64 extensions when required by the
    /// entry. After the file is compressed, the original and compressed sizes are
    /// checked, and if they exceed the limits described above, then zip64 can be
    /// used. That is the general idea, but there is an additional wrinkle when saving
    /// to a non-seekable device, like the ASP.NET <c>Response.OutputStream</c>, or
    /// <c>Console.Out</c>.  When using non-seekable streams for output, the entry
    /// header - which indicates whether zip64 is in use - is emitted before it is
    /// known if zip64 is necessary.  It is only after all entries have been saved
    /// that it can be known if ZIP64 will be required.  On seekable output streams,
    /// after saving all entries, the library can seek backward and re-emit the zip
    /// file header to be consistent with the actual ZIP64 requirement.  But using a
    /// non-seekable output stream, the library cannot seek backward, so the header
    /// can never be changed. In other words, the archive's use of ZIP64 extensions is
    /// not alterable after the header is emitted.  Therefore, when saving to
    /// non-seekable streams, using <see cref="F:Zip64Option.AsNecessary" /> is the same
    /// as using <see cref="F:Zip64Option.Always" />: it will always produce a zip
    /// archive that uses ZIP64 extensions.
    /// </para>
    /// 
    /// </remarks>
    public enum Zip64Option
    {
        /// <summary>
        /// Always use ZIP64 extensions when writing zip archives, even when unnecessary.
        /// (For COM clients, this is a 2.)
        /// </summary>
        Always = 2,
        /// <summary>
        /// Use ZIP64 extensions when writing zip archives, as necessary.
        /// For example, when a single entry exceeds 0xFFFFFFFF in size, or when the archive as a whole
        /// exceeds 0xFFFFFFFF in size, or when there are more than 65535 entries in an archive.
        /// (For COM clients, this is a 1.)
        /// </summary>
        AsNecessary = 1,
        /// <summary>
        /// The default behavior, which is "Never".
        /// (For COM clients, this is a 0 (zero).)
        /// </summary>
        Default = 0,
        /// <summary>
        /// Do not use ZIP64 extensions when writing zip archives.
        /// (For COM clients, this is a 0 (zero).)
        /// </summary>
        Never = 0
    }
}

