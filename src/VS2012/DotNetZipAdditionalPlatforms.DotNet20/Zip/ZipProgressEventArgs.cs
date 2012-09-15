namespace DotNetZipAdditionalPlatforms.Zip
{
    using System;

    /// <summary>
    /// Provides information about the progress of a save, read, or extract operation.
    /// This is a base class; you will probably use one of the classes derived from this one.
    /// </summary>
    public class ZipProgressEventArgs : EventArgs
    {
        private string _archiveName;
        private long _bytesTransferred;
        private bool _cancel;
        private int _entriesTotal;
        private ZipProgressEventType _flavor;
        private ZipEntry _latestEntry;
        private long _totalBytesToTransfer;

        internal ZipProgressEventArgs()
        {
        }

        internal ZipProgressEventArgs(string archiveName, ZipProgressEventType flavor)
        {
            this._archiveName = archiveName;
            this._flavor = flavor;
        }

        /// <summary>
        /// Returns the archive name associated to this event.
        /// </summary>
        public string ArchiveName
        {
            get
            {
                return this._archiveName;
            }
            set
            {
                this._archiveName = value;
            }
        }

        /// <summary>
        /// The number of bytes read or written so far for this entry.
        /// </summary>
        public long BytesTransferred
        {
            get
            {
                return this._bytesTransferred;
            }
            set
            {
                this._bytesTransferred = value;
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
                return this._cancel;
            }
            set
            {
                this._cancel = this._cancel || value;
            }
        }

        /// <summary>
        /// The name of the last entry saved or extracted.
        /// </summary>
        public ZipEntry CurrentEntry
        {
            get
            {
                return this._latestEntry;
            }
            set
            {
                this._latestEntry = value;
            }
        }

        /// <summary>
        /// The total number of entries to be saved or extracted.
        /// </summary>
        public int EntriesTotal
        {
            get
            {
                return this._entriesTotal;
            }
            set
            {
                this._entriesTotal = value;
            }
        }

        /// <summary>
        /// The type of event being reported.
        /// </summary>
        public ZipProgressEventType EventType
        {
            get
            {
                return this._flavor;
            }
            set
            {
                this._flavor = value;
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
                return this._totalBytesToTransfer;
            }
            set
            {
                this._totalBytesToTransfer = value;
            }
        }
    }
}

