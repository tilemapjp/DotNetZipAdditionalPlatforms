namespace DotNetZipAdditionalPlatforms.Zip
{
    using System;

    /// <summary>
    /// Provides information about the progress of the extract operation.
    /// </summary>
    public class ExtractProgressEventArgs : ZipProgressEventArgs
    {
        private int entriesExtractedField;
        private string targetField;

        internal ExtractProgressEventArgs()
        {
        }

        internal ExtractProgressEventArgs(string archiveName, ZipProgressEventType flavor) : base(archiveName, flavor)
        {
        }

        /// <summary>
        /// Constructor for the ExtractProgressEventArgs.
        /// </summary>
        /// <param name="archiveName">the name of the zip archive.</param>
        /// <param name="before">whether this is before saving the entry, or after</param>
        /// <param name="entriesTotal">The total number of entries in the zip archive.</param>
        /// <param name="entriesExtracted">Number of entries that have been extracted.</param>
        /// <param name="entry">The entry involved in the event.</param>
        /// <param name="extractLocation">The location to which entries are extracted.</param>
        internal ExtractProgressEventArgs(string archiveName, bool before, int entriesTotal, int entriesExtracted, ZipEntry entry, string extractLocation) : base(archiveName, before ? ZipProgressEventType.Extracting_BeforeExtractEntry : ZipProgressEventType.Extracting_AfterExtractEntry)
        {
            base.EntriesTotal = entriesTotal;
            base.CurrentEntry = entry;
            this.entriesExtractedField = entriesExtracted;
            this.targetField = extractLocation;
        }

        internal static ExtractProgressEventArgs AfterExtractEntry(string archiveName, ZipEntry entry, string extractLocation)
        {
            ExtractProgressEventArgs args2 = new ExtractProgressEventArgs();
            args2.ArchiveName = archiveName;
            args2.EventType = ZipProgressEventType.Extracting_AfterExtractEntry;
            args2.CurrentEntry = entry;
            args2.targetField = extractLocation;
            return args2;
        }

        internal static ExtractProgressEventArgs BeforeExtractEntry(string archiveName, ZipEntry entry, string extractLocation)
        {
            ExtractProgressEventArgs args2 = new ExtractProgressEventArgs();
            args2.ArchiveName = archiveName;
            args2.EventType = ZipProgressEventType.Extracting_BeforeExtractEntry;
            args2.CurrentEntry = entry;
            args2.targetField = extractLocation;
            return args2;
        }

        internal static ExtractProgressEventArgs ByteUpdate(string archiveName, ZipEntry entry, long bytesWritten, long totalBytes)
        {
            ExtractProgressEventArgs args = new ExtractProgressEventArgs(archiveName, ZipProgressEventType.Extracting_EntryBytesWritten);
            args.ArchiveName = archiveName;
            args.CurrentEntry = entry;
            args.BytesTransferred = bytesWritten;
            args.TotalBytesToTransfer = totalBytes;
            return args;
        }

        internal static ExtractProgressEventArgs ExtractAllCompleted(string archiveName, string extractLocation)
        {
            ExtractProgressEventArgs args = new ExtractProgressEventArgs(archiveName, ZipProgressEventType.Extracting_AfterExtractAll);
            args.targetField = extractLocation;
            return args;
        }

        internal static ExtractProgressEventArgs ExtractAllStarted(string archiveName, string extractLocation)
        {
            ExtractProgressEventArgs args = new ExtractProgressEventArgs(archiveName, ZipProgressEventType.Extracting_BeforeExtractAll);
            args.targetField = extractLocation;
            return args;
        }

        internal static ExtractProgressEventArgs ExtractExisting(string archiveName, ZipEntry entry, string extractLocation)
        {
            ExtractProgressEventArgs args2 = new ExtractProgressEventArgs();
            args2.ArchiveName = archiveName;
            args2.EventType = ZipProgressEventType.Extracting_ExtractEntryWouldOverwrite;
            args2.CurrentEntry = entry;
            args2.targetField = extractLocation;
            return args2;
        }

        /// <summary>
        /// Number of entries extracted so far.  This is set only if the
        /// EventType is Extracting_BeforeExtractEntry or Extracting_AfterExtractEntry, and
        /// the Extract() is occurring witin the scope of a call to ExtractAll().
        /// </summary>
        public int EntriesExtracted
        {
            get
            {
                return this.entriesExtractedField;
            }
        }

        /// <summary>
        /// Returns the extraction target location, a filesystem path.
        /// </summary>
        public string ExtractLocation
        {
            get
            {
                return this.targetField;
            }
        }
    }
}

