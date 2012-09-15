namespace DotNetZipAdditionalPlatforms.Zip
{
    using System;

    /// <summary>
    /// Provides information about the progress of a save operation.
    /// </summary>
    public class SaveProgressEventArgs : ZipProgressEventArgs
    {
        private int _entriesSaved;

        internal SaveProgressEventArgs()
        {
        }

        internal SaveProgressEventArgs(string archiveName, ZipProgressEventType flavor) : base(archiveName, flavor)
        {
        }

        /// <summary>
        /// Constructor for the SaveProgressEventArgs.
        /// </summary>
        /// <param name="archiveName">the name of the zip archive.</param>
        /// <param name="before">whether this is before saving the entry, or after</param>
        /// <param name="entriesTotal">The total number of entries in the zip archive.</param>
        /// <param name="entriesSaved">Number of entries that have been saved.</param>
        /// <param name="entry">The entry involved in the event.</param>
        internal SaveProgressEventArgs(string archiveName, bool before, int entriesTotal, int entriesSaved, ZipEntry entry) : base(archiveName, before ? ZipProgressEventType.Saving_BeforeWriteEntry : ZipProgressEventType.Saving_AfterWriteEntry)
        {
            base.EntriesTotal = entriesTotal;
            base.CurrentEntry = entry;
            this._entriesSaved = entriesSaved;
        }

        internal static SaveProgressEventArgs ByteUpdate(string archiveName, ZipEntry entry, long bytesXferred, long totalBytes)
        {
            SaveProgressEventArgs args = new SaveProgressEventArgs(archiveName, ZipProgressEventType.Saving_EntryBytesRead);
            args.ArchiveName = archiveName;
            args.CurrentEntry = entry;
            args.BytesTransferred = bytesXferred;
            args.TotalBytesToTransfer = totalBytes;
            return args;
        }

        internal static SaveProgressEventArgs Completed(string archiveName)
        {
            return new SaveProgressEventArgs(archiveName, ZipProgressEventType.Saving_Completed);
        }

        internal static SaveProgressEventArgs Started(string archiveName)
        {
            return new SaveProgressEventArgs(archiveName, ZipProgressEventType.Saving_Started);
        }

        /// <summary>
        /// Number of entries saved so far.
        /// </summary>
        public int EntriesSaved
        {
            get
            {
                return this._entriesSaved;
            }
        }
    }
}

