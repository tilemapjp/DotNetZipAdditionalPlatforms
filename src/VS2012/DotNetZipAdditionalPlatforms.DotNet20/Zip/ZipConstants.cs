namespace DotNetZipAdditionalPlatforms.Zip
{
    using System;

    internal static class ZipConstants
    {
        public const ushort AesAlgId128 = 0x660e;
        public const ushort AesAlgId192 = 0x660f;
        public const ushort AesAlgId256 = 0x6610;
        public const int AesBlockSize = 0x80;
        public const int AesKeySize = 0xc0;
        public const uint EndOfCentralDirectorySignature = 0x6054b50;
        public const uint PackedToRemovableMedia = 0x30304b50;
        public const int SplitArchiveSignature = 0x8074b50;
        public const uint Zip64EndOfCentralDirectoryLocatorSignature = 0x7064b50;
        public const uint Zip64EndOfCentralDirectoryRecordSignature = 0x6064b50;
        public const int ZipDirEntrySignature = 0x2014b50;
        public const int ZipEntryDataDescriptorSignature = 0x8074b50;
        public const int ZipEntrySignature = 0x4034b50;
    }
}

