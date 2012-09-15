namespace DotNetZipAdditionalPlatforms.Zip
{
    using System;

    /// <summary>
    /// Provides information about the progress of a Add operation.
    /// </summary>
    public class AddProgressEventArgs : ZipProgressEventArgs
    {
        internal AddProgressEventArgs()
        {
        }

        private AddProgressEventArgs(string archiveName, ZipProgressEventType flavor) : base(archiveName, flavor)
        {
        }

        internal static AddProgressEventArgs AfterEntry(string archiveName, ZipEntry entry, int entriesTotal)
        {
            AddProgressEventArgs args = new AddProgressEventArgs(archiveName, ZipProgressEventType.Adding_AfterAddEntry);
            args.EntriesTotal = entriesTotal;
            args.CurrentEntry = entry;
            return args;
        }

        internal static AddProgressEventArgs Completed(string archiveName)
        {
            return new AddProgressEventArgs(archiveName, ZipProgressEventType.Adding_Completed);
        }

        internal static AddProgressEventArgs Started(string archiveName)
        {
            return new AddProgressEventArgs(archiveName, ZipProgressEventType.Adding_Started);
        }
    }
}

