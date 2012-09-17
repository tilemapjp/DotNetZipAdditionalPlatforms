namespace DotNetZipAdditionalPlatforms.Zip
{
    using System;

    /// <summary>
    /// Provides information about the progress of a save, read, or extract operation.
    /// This is a base class; you will probably use one of the classes derived from this one.
    /// </summary>
    public class ZipProgressEventArgs : EventArgs
    {
        private string archiveNameField;
        private long bytesTransferredField;
        private bool cancelField;
        private int entriesTotalField;
        private ZipProgressEventType flavorField;
        private ZipEntry latestEntryField;
        private long totalBytesToTransferField;

        internal ZipProgressEventArgs()
        {
        }

        internal ZipProgressEventArgs(string archiveName, ZipProgressEventType flavor)
        {
            this.archiveNameField = archiveName;
            this.flavorField = flavor;
        }

        /// <summary>
        /// Returns the archive name associated to this event.
        /// </summary>
        public string ArchiveName
        {
            get
            {
                return this.archiveNameField;
            }
            set
            {
                this.archiveNameField = value;
            }
        }

        /// <summary>
        /// The number of bytes read or written so far for this entry.
        /// </summary>
        public long BytesTransferred
        {
            get
            {
                return this.bytesTransferredField;
            }
            set
            {
                this.bytesTransferredField = value;
            }
        }

        /// <summary>
        /// In an event handler, set this to cancel the save or extract
        /// operation that is in progress.
        /// </summary>
        public bool Cancel
        {
            get
            {
                return this.cancelField;
            }
            set
            {
                this.cancelField = this.cancelField || value;
            }
        }

        /// <summary>
        /// The name of the last entry saved or extracted.
        /// </summary>
        public ZipEntry CurrentEntry
        {
            get
            {
                return this.latestEntryField;
            }
            set
            {
                this.latestEntryField = value;
            }
        }

        /// <summary>
        /// The total number of entries to be saved or extracted.
        /// </summary>
        public int EntriesTotal
        {
            get
            {
                return this.entriesTotalField;
            }
            set
            {
                this.entriesTotalField = value;
            }
        }

        /// <summary>
        /// The type of event being reported.
        /// </summary>
        public ZipProgressEventType EventType
        {
            get
            {
                return this.flavorField;
            }
            set
            {
                this.flavorField = value;
            }
        }

        /// <summary>
        /// Total number of bytes that will be read or written for this entry.
        /// This number will be -1 if the value cannot be determined.
        /// </summary>
        public long TotalBytesToTransfer
        {
            get
            {
                return this.totalBytesToTransferField;
            }
            set
            {
                this.totalBytesToTransferField = value;
            }
        }
    }
}

