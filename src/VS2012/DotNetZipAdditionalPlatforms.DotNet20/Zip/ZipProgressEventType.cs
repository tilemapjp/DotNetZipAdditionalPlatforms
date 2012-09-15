namespace DotNetZipAdditionalPlatforms.Zip
{
    using System;

    /// <summary>
    /// In an EventArgs type, indicates which sort of progress event is being
    /// reported.
    /// </summary>
    /// <remarks>
    /// There are events for reading, events for saving, and events for
    /// extracting. This enumeration allows a single EventArgs type to be sued to
    /// describe one of multiple subevents. For example, a SaveProgress event is
    /// invoked before, after, and during the saving of a single entry.  The value
    /// of an enum with this type, specifies which event is being triggered.  The
    /// same applies to Extraction, Reading and Adding events.
    /// </remarks>
    public enum ZipProgressEventType
    {
        /// <summary>
        /// Adding started.
        /// </summary>
        Adding_Started,

        /// <summary>
        /// Adding after add entry.
        /// </summary>
        Adding_AfterAddEntry,

        /// <summary>
        /// Adding completed.
        /// </summary>
        Adding_Completed,

        /// <summary>
        /// Reading started
        /// </summary>
        Reading_Started,

        /// <summary>
        /// Reading before read entry.
        /// </summary>
        Reading_BeforeReadEntry,

        /// <summary>
        /// Reading after read entry.
        /// </summary>
        Reading_AfterReadEntry,

        /// <summary>
        /// Reading completed.
        /// </summary>
        Reading_Completed,

        /// <summary>
        /// Reading archive bytes read.
        /// </summary>
        Reading_ArchiveBytesRead,

        /// <summary>
        /// Saving started.
        /// </summary>
        Saving_Started,

        /// <summary>
        /// Saving before write entry.
        /// </summary>
        Saving_BeforeWriteEntry,

        /// <summary>
        /// Saving after write entry.
        /// </summary>
        Saving_AfterWriteEntry,

        /// <summary>
        /// Saving completed.
        /// </summary>
        Saving_Completed,

        /// <summary>
        /// Saving after save temp archive.
        /// </summary>
        Saving_AfterSaveTempArchive,

        /// <summary>
        /// Saving before rename temp archive.
        /// </summary>
        Saving_BeforeRenameTempArchive,

        /// <summary>
        /// Saving after rename temp archive.
        /// </summary>
        Saving_AfterRenameTempArchive,

        /// <summary>
        /// Saving after compile self extractor.
        /// </summary>
        Saving_AfterCompileSelfExtractor,

        /// <summary>
        /// Saving entry bytes read.
        /// </summary>
        Saving_EntryBytesRead,

        /// <summary>
        /// Extracting before extract entry.
        /// </summary>
        Extracting_BeforeExtractEntry,

        /// <summary>
        /// Extracting after extract entry.
        /// </summary>
        Extracting_AfterExtractEntry,

        /// <summary>
        /// Extracting extract entry would overwrite.
        /// </summary>
        Extracting_ExtractEntryWouldOverwrite,

        /// <summary>
        /// Extracting entry bytes written.
        /// </summary>
        Extracting_EntryBytesWritten,

        /// <summary>
        /// Extracting before extract all.
        /// </summary>
        Extracting_BeforeExtractAll,

        /// <summary>
        /// Extracting after extract all.
        /// </summary>
        Extracting_AfterExtractAll,

        /// <summary>
        /// Error saving.
        /// </summary>
        Error_Saving
    }
}

