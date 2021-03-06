﻿namespace DotNetZipAdditionalPlatforms.Zip
{
    using System;

    /// <summary>
    /// Provides information about the an error that occurred while zipping.
    /// </summary>
    public class ZipErrorEventArgs : ZipProgressEventArgs
    {
        private Exception exceptionField;

        private ZipErrorEventArgs()
        {
        }

        internal static ZipErrorEventArgs Saving(string archiveName, ZipEntry entry, Exception exception)
        {
            ZipErrorEventArgs args2 = new ZipErrorEventArgs();
            args2.EventType = ZipProgressEventType.Error_Saving;
            args2.ArchiveName = archiveName;
            args2.CurrentEntry = entry;
            args2.exceptionField = exception;
            return args2;
        }

        /// <summary>
        /// Returns the exception that occurred, if any.
        /// </summary>
        public Exception Exception
        {
            get
            {
                return this.exceptionField;
            }
        }

        /// <summary>
        /// Returns the name of the file that caused the exception, if any.
        /// </summary>
        public string FileName
        {
            get
            {
                return base.CurrentEntry.LocalFileName;
            }
        }
    }
}

