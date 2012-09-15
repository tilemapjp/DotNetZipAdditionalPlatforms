namespace DotNetZipAdditionalPlatforms.Zip
{
    using System;

    /// <summary>
    /// Provides information about the progress of a Read operation.
    /// </summary>
    public class ReadProgressEventArgs : ZipProgressEventArgs
    {
        internal ReadProgressEventArgs()
        {
        }

        private ReadProgressEventArgs(string archiveName, ZipProgressEventType flavor) : base(archiveName, flavor)
        {
        }

        internal static ReadProgressEventArgs After(string archiveName, ZipEntry entry, int entriesTotal)
        {
            ReadProgressEventArgs args = new ReadProgressEventArgs(archiveName, ZipProgressEventType.Reading_AfterReadEntry);
            args.EntriesTotal = entriesTotal;
            args.CurrentEntry = entry;
            return args;
        }

        internal static ReadProgressEventArgs Before(string archiveName, int entriesTotal)
        {
            ReadProgressEventArgs args = new ReadProgressEventArgs(archiveName, ZipProgressEventType.Reading_BeforeReadEntry);
            args.EntriesTotal = entriesTotal;
            return args;
        }

        internal static ReadProgressEventArgs ByteUpdate(string archiveName, ZipEntry entry, long bytesXferred, long totalBytes)
        {
            ReadProgressEventArgs args = new ReadProgressEventArgs(archiveName, ZipProgressEventType.Reading_ArchiveBytesRead);
            args.CurrentEntry = entry;
            args.BytesTransferred = bytesXferred;
            args.TotalBytesToTransfer = totalBytes;
            return args;
        }

        internal static ReadProgressEventArgs Completed(string archiveName)
        {
            return new ReadProgressEventArgs(archiveName, ZipProgressEventType.Reading_Completed);
        }

        internal static ReadProgressEventArgs Started(string archiveName)
        {
            return new ReadProgressEventArgs(archiveName, ZipProgressEventType.Reading_Started);
        }
    }
}

