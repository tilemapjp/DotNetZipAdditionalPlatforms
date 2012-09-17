namespace DotNetZipAdditionalPlatforms.Zip
{
    using DotNetZipAdditionalPlatforms.BZip2;
    using DotNetZipAdditionalPlatforms.Crc;
    using DotNetZipAdditionalPlatforms.Zlib;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Threading;

    /// <summary>
    /// Represents a single entry in a ZipFile. Typically, applications get a ZipEntry
    /// by enumerating the entries within a ZipFile, or by adding an entry to a ZipFile.
    /// </summary>
    public class ZipEntry
    {
        private long fileDataPositionField = -1L;
        private Encoding actualEncodingField;
        private WinZipAesCrypto aesCryptoForExtractField;
        private WinZipAesCrypto aesCryptoForWriteField;
        internal Stream archiveStreamField;
        private DateTime atimeField;
        internal short bitFieldField;
        private CloseDelegate closeDelegateField;
        internal string commentField;
        private byte[] commentBytesField;
        private short commentLengthField;
        internal long compressedFileDataSizeField;
        internal long compressedSizeField;
        private CompressionLevel compressionLevelField = CompressionLevel.Default;
        internal short compressionMethodField = 8;
        private short compressionMethodFromZipFileField;
        internal ZipContainer containerField;
        internal int crc32Field;
        private bool crcCalculatedField;
        private DateTime ctimeField;
        private uint diskNumberField;
        private bool emitNtfsTimesField = true;
        private bool emitUnixTimesField;
        internal EncryptionAlgorithm encryptionField = EncryptionAlgorithm.None;
        internal EncryptionAlgorithm encryptionFromZipFileField;
        private byte[] entryHeaderField;
        private bool? entryRequiresZip64Field;
        private int externalFileAttrsField;
        internal byte[] extraField;
        private short extraFieldLengthField;
        private string fileNameInArchiveField;
        private short filenameLengthField;
        private long futureRolhField;
        private Stream inputDecryptorStreamField;
        internal bool inputUsesZip64Field;
        private short internalFileAttrsField;
        private bool ioOperationCanceledField;
        private bool isDirectoryField;
        private bool isTextField;
        internal DateTime lastModifiedField;
        private int lengthOfHeaderField;
        private int lengthOfTrailerField;
        internal string localFileNameField;
        private bool metadataChangedField;
        private DateTime mtimeField;
        private bool ntfsTimesAreSetField;
        private OpenDelegate openDelegateField;
        private object outputLockField = new object();
        private bool? outputUsesZip64Field;
        internal string passwordField;
        private bool presumeZip64Field;
        private int readExtraDepthField;
        internal long relativeOffsetOfLocalHeaderField;
        private bool restreamRequiredOnSaveField;
        private bool skippedDuringSaveField;
        internal ZipEntrySource sourceField = ZipEntrySource.None;
        private bool sourceIsEncryptedField;
        private Stream sourceStreamField;
        private long? sourceStreamOriginalPositionField;
        private bool sourceWasJitProvidedField;
        internal int timeBlobField;
        private ZipEntryTimestamp timestampField;
        private long totalEntrySizeField;
        private bool trimVolumeFromFullyQualifiedPathsField = true;
        internal long uncompressedSizeField;
        private static DateTime unixEpochField = new DateTime(0x7b2, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        private uint unsupportedAlgorithmIdField;
        private short versionMadeByField;
        internal short versionNeededField;
        internal byte[] weakEncryptionHeaderField;
        private static DateTime win32EpochField = DateTime.FromFileTimeUtc(0L);
        private short winZipAesMethodField;
        private WriteDelegate writeDelegateField;
        private static DateTime zeroHourField = new DateTime(1, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        private ZipCrypto zipCryptoForExtractField;
        private ZipCrypto zipCryptoForWriteField;
        private static Encoding ibm437Field = Encoding.GetEncoding("IBM437");

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <remarks>
        /// Applications should never need to call this directly.  It is exposed to
        /// support COM Automation environments.
        /// </remarks>
        public ZipEntry()
        {
            this.AlternateEncoding = Encoding.GetEncoding("IBM437");
            this.AlternateEncodingUsage = ZipOption.Default;
        }

        private void _CheckRead(int nbytes)
        {
            if (nbytes == 0)
            {
                throw new BadReadException(string.Format(CultureInfo.InvariantCulture, "bad read of entry {0} from compressed archive.", this.FileName));
            }
        }

        internal void _SetTimes(string fileOrDirectory, bool isFile)
        {
            try
            {
                if (this.ntfsTimesAreSetField)
                {
                    if (isFile)
                    {
                        if (File.Exists(fileOrDirectory))
                        {
                            File.SetCreationTimeUtc(fileOrDirectory, this.ctimeField);
                            File.SetLastAccessTimeUtc(fileOrDirectory, this.atimeField);
                            File.SetLastWriteTimeUtc(fileOrDirectory, this.mtimeField);
                        }
                    }
                    else if (Directory.Exists(fileOrDirectory))
                    {
                        Directory.SetCreationTimeUtc(fileOrDirectory, this.ctimeField);
                        Directory.SetLastAccessTimeUtc(fileOrDirectory, this.atimeField);
                        Directory.SetLastWriteTimeUtc(fileOrDirectory, this.mtimeField);
                    }
                }
                else
                {
                    DateTime lastWriteTime = SharedUtilities.AdjustTime_Reverse(this.LastModified);
                    if (isFile)
                    {
                        File.SetLastWriteTime(fileOrDirectory, lastWriteTime);
                    }
                    else
                    {
                        Directory.SetLastWriteTime(fileOrDirectory, lastWriteTime);
                    }
                }
            }
            catch (IOException exception)
            {
                this.WriteStatus("failed to set time on {0}: {1}", new object[] { fileOrDirectory, exception.Message });
            }
        }

        private void _WriteEntryData(Stream s)
        {
            Stream input = null;
            long position = -1L;
            try
            {
                position = s.Position;
            }
            catch (Exception)
            {
            }
            try
            {
                Stream stream3;
                Stream stream4;
                long streamLength = this.SetInputAndFigureFileLength(ref input);
                CountingStream stream2 = new CountingStream(s);
                if (streamLength != 0L)
                {
                    stream3 = this.MaybeApplyEncryption(stream2);
                    stream4 = this.MaybeApplyCompression(stream3, streamLength);
                }
                else
                {
                    stream3 = stream4 = stream2;
                }
                CrcCalculatorStream stream = new CrcCalculatorStream(stream4, true);
                if (this.sourceField == ZipEntrySource.WriteDelegate)
                {
                    this.writeDelegateField(this.FileName, stream);
                }
                else
                {
                    int num3;
                    byte[] buffer = new byte[this.BufferSize];
                    while ((num3 = SharedUtilities.ReadWithRetry(input, buffer, 0, buffer.Length, this.FileName)) != 0)
                    {
                        stream.Write(buffer, 0, num3);
                        this.OnWriteBlock(stream.TotalBytesSlurped, streamLength);
                        if (this.ioOperationCanceledField)
                        {
                            break;
                        }
                    }
                }
                this.FinishOutputStream(s, stream2, stream3, stream4, stream);
            }
            finally
            {
                if (this.sourceField == ZipEntrySource.JitStream)
                {
                    if (this.closeDelegateField != null)
                    {
                        this.closeDelegateField(this.FileName, input);
                    }
                }
                else if (input is FileStream)
                {
                    input.Dispose();
                }
            }
            if (!this.ioOperationCanceledField)
            {
                this.fileDataPositionField = position;
                this.PostProcessOutput(s);
            }
        }

        private int CheckExtractExistingFile(string baseDir, string targetFileName)
        {
            int num = 0;
        Label_0003:
            switch (this.ExtractExistingFile)
            {
                case ExtractExistingFileAction.OverwriteSilently:
                    this.WriteStatus("the file {0} exists; will overwrite it...", new object[] { targetFileName });
                    return 0;

                case ExtractExistingFileAction.DoNotOverwrite:
                    this.WriteStatus("the file {0} exists; not extracting entry...", new object[] { this.FileName });
                    this.OnAfterExtract(baseDir);
                    return 1;

                case ExtractExistingFileAction.InvokeExtractProgressEvent:
                    if (num > 0)
                    {
                        throw new ZipException(string.Format(CultureInfo.InvariantCulture, "The file {0} already exists.", targetFileName));
                    }
                    this.OnExtractExisting(baseDir);
                    if (!this.ioOperationCanceledField)
                    {
                        num++;
                        goto Label_0003;
                    }
                    return 2;
            }
            throw new ZipException(string.Format(CultureInfo.InvariantCulture, "The file {0} already exists.", targetFileName));
        }

        private byte[] ConstructExtraField(bool forCentralDirectory)
        {
            byte[] buffer;
            int num2;
            List<byte[]> list = new List<byte[]>();
            if (!((this.containerField.Zip64 == Zip64Option.Always) ? false : ((this.containerField.Zip64 != Zip64Option.AsNecessary) ? true : (!forCentralDirectory ? false : !this.entryRequiresZip64Field.Value))))
            {
                int num = 4 + (forCentralDirectory ? 0x1c : 0x10);
                buffer = new byte[num];
                num2 = 0;
                if (this.presumeZip64Field || forCentralDirectory)
                {
                    buffer[num2++] = 1;
                    buffer[num2++] = 0;
                }
                else
                {
                    buffer[num2++] = 0x99;
                    buffer[num2++] = 0x99;
                }
                buffer[num2++] = (byte) (num - 4);
                buffer[num2++] = 0;
                Array.Copy(BitConverter.GetBytes(this.uncompressedSizeField), 0, buffer, num2, 8);
                num2 += 8;
                Array.Copy(BitConverter.GetBytes(this.compressedSizeField), 0, buffer, num2, 8);
                num2 += 8;
                if (forCentralDirectory)
                {
                    Array.Copy(BitConverter.GetBytes(this.relativeOffsetOfLocalHeaderField), 0, buffer, num2, 8);
                    num2 += 8;
                    Array.Copy(BitConverter.GetBytes(0), 0, buffer, num2, 4);
                }
                list.Add(buffer);
            }
            if ((this.Encryption == EncryptionAlgorithm.WinZipAes128) || (this.Encryption == EncryptionAlgorithm.WinZipAes256))
            {
                buffer = new byte[11];
                num2 = 0;
                buffer[num2++] = 1;
                buffer[num2++] = 0x99;
                buffer[num2++] = 7;
                buffer[num2++] = 0;
                buffer[num2++] = 1;
                buffer[num2++] = 0;
                buffer[num2++] = 0x41;
                buffer[num2++] = 0x45;
                int keyStrengthInBits = GetKeyStrengthInBits(this.Encryption);
                if (keyStrengthInBits == 0x80)
                {
                    buffer[num2] = 1;
                }
                else if (keyStrengthInBits == 0x100)
                {
                    buffer[num2] = 3;
                }
                else
                {
                    buffer[num2] = 0xff;
                }
                num2++;
                buffer[num2++] = (byte) (this.compressionMethodField & 0xff);
                buffer[num2++] = (byte) (this.compressionMethodField & 0xff00);
                list.Add(buffer);
            }
            if (this.ntfsTimesAreSetField && this.emitNtfsTimesField)
            {
                buffer = new byte[0x24];
                num2 = 0;
                buffer[num2++] = 10;
                buffer[num2++] = 0;
                buffer[num2++] = 0x20;
                buffer[num2++] = 0;
                num2 += 4;
                buffer[num2++] = 1;
                buffer[num2++] = 0;
                buffer[num2++] = 0x18;
                buffer[num2++] = 0;
                Array.Copy(BitConverter.GetBytes(this.mtimeField.ToFileTime()), 0, buffer, num2, 8);
                num2 += 8;
                Array.Copy(BitConverter.GetBytes(this.atimeField.ToFileTime()), 0, buffer, num2, 8);
                num2 += 8;
                Array.Copy(BitConverter.GetBytes(this.ctimeField.ToFileTime()), 0, buffer, num2, 8);
                num2 += 8;
                list.Add(buffer);
            }
            if (this.ntfsTimesAreSetField && this.emitUnixTimesField)
            {
                int num5 = 9;
                if (!forCentralDirectory)
                {
                    num5 += 8;
                }
                buffer = new byte[num5];
                num2 = 0;
                buffer[num2++] = 0x55;
                buffer[num2++] = 0x54;
                buffer[num2++] = (byte) (num5 - 4);
                buffer[num2++] = 0;
                buffer[num2++] = 7;
                TimeSpan span = (TimeSpan) (this.mtimeField - unixEpochField);
                int totalSeconds = (int) span.TotalSeconds;
                Array.Copy(BitConverter.GetBytes(totalSeconds), 0, buffer, num2, 4);
                num2 += 4;
                if (!forCentralDirectory)
                {
                    span = (TimeSpan) (this.atimeField - unixEpochField);
                    totalSeconds = (int) span.TotalSeconds;
                    Array.Copy(BitConverter.GetBytes(totalSeconds), 0, buffer, num2, 4);
                    num2 += 4;
                    span = (TimeSpan) (this.ctimeField - unixEpochField);
                    totalSeconds = (int) span.TotalSeconds;
                    Array.Copy(BitConverter.GetBytes(totalSeconds), 0, buffer, num2, 4);
                    num2 += 4;
                }
                list.Add(buffer);
            }
            byte[] destinationArray = null;
            if (list.Count > 0)
            {
                int num7 = 0;
                int destinationIndex = 0;
                for (num2 = 0; num2 < list.Count; num2++)
                {
                    num7 += list[num2].Length;
                }
                destinationArray = new byte[num7];
                for (num2 = 0; num2 < list.Count; num2++)
                {
                    Array.Copy(list[num2], 0, destinationArray, destinationIndex, list[num2].Length);
                    destinationIndex += list[num2].Length;
                }
            }
            return destinationArray;
        }

        /// <summary>
        /// Copy metadata that may have been changed by the app.  We do this when
        /// resetting the zipFile instance.  If the app calls Save() on a ZipFile, then
        /// tries to party on that file some more, we may need to Reset() it , which
        /// means re-reading the entries and then copying the metadata.  I think.
        /// </summary>
        internal void CopyMetaData(ZipEntry source)
        {
            this.fileDataPositionField = source.fileDataPositionField;
            this.CompressionMethod = source.CompressionMethod;
            this.compressionMethodFromZipFileField = source.compressionMethodFromZipFileField;
            this.compressedFileDataSizeField = source.compressedFileDataSizeField;
            this.uncompressedSizeField = source.uncompressedSizeField;
            this.bitFieldField = source.bitFieldField;
            this.sourceField = source.sourceField;
            this.lastModifiedField = source.lastModifiedField;
            this.mtimeField = source.mtimeField;
            this.atimeField = source.atimeField;
            this.ctimeField = source.ctimeField;
            this.ntfsTimesAreSetField = source.ntfsTimesAreSetField;
            this.emitUnixTimesField = source.emitUnixTimesField;
            this.emitNtfsTimesField = source.emitNtfsTimesField;
        }

        private void CopyThroughOneEntry(Stream outStream)
        {
            if (this.LengthOfHeader == 0)
            {
                throw new BadStateException("Bad header length.");
            }
            if ((((this.metadataChangedField || (this.ArchiveStream is ZipSegmentedStream)) || (outStream is ZipSegmentedStream)) || (this.inputUsesZip64Field && (this.containerField.UseZip64WhenSaving == Zip64Option.Default))) || (!this.inputUsesZip64Field && (this.containerField.UseZip64WhenSaving == Zip64Option.Always)))
            {
                this.CopyThroughWithRecompute(outStream);
            }
            else
            {
                this.CopyThroughWithNoChange(outStream);
            }
            this.entryRequiresZip64Field = new bool?(((this.compressedSizeField >= 0xffffffffL) || (this.uncompressedSizeField >= 0xffffffffL)) || (this.relativeOffsetOfLocalHeaderField >= 0xffffffffL));
            this.outputUsesZip64Field = new bool?((this.containerField.Zip64 == Zip64Option.Always) ? true : this.entryRequiresZip64Field.Value);
        }

        private void CopyThroughWithNoChange(Stream outstream)
        {
            byte[] buffer = new byte[this.BufferSize];
            CountingStream stream = new CountingStream(this.ArchiveStream);
            stream.Seek(this.relativeOffsetOfLocalHeaderField, SeekOrigin.Begin);
            if (this.totalEntrySizeField == 0L)
            {
                this.totalEntrySizeField = (this.lengthOfHeaderField + this.compressedFileDataSizeField) + this.lengthOfTrailerField;
            }
            CountingStream stream2 = outstream as CountingStream;
            this.relativeOffsetOfLocalHeaderField = (stream2 != null) ? stream2.ComputedPosition : outstream.Position;
            long num2 = this.totalEntrySizeField;
            while (num2 > 0L)
            {
                int count = (num2 > buffer.Length) ? buffer.Length : ((int) num2);
                int num = stream.Read(buffer, 0, count);
                outstream.Write(buffer, 0, num);
                num2 -= num;
                this.OnWriteBlock(stream.BytesRead, this.totalEntrySizeField);
                if (this.ioOperationCanceledField)
                {
                    break;
                }
            }
        }

        private void CopyThroughWithRecompute(Stream outstream)
        {
            byte[] buffer = new byte[this.BufferSize];
            CountingStream stream = new CountingStream(this.ArchiveStream);
            long num2 = this.relativeOffsetOfLocalHeaderField;
            int lengthOfHeader = this.LengthOfHeader;
            this.WriteHeader(outstream, 0);
            this.StoreRelativeOffset();
            if (!this.FileName.EndsWith("/"))
            {
                long offset = num2 + lengthOfHeader;
                int lengthOfCryptoHeaderBytes = GetLengthOfCryptoHeaderBytes(this.encryptionFromZipFileField);
                offset -= lengthOfCryptoHeaderBytes;
                this.lengthOfHeaderField += lengthOfCryptoHeaderBytes;
                stream.Seek(offset, SeekOrigin.Begin);
                long num6 = this.compressedSizeField;
                while (num6 > 0L)
                {
                    lengthOfCryptoHeaderBytes = (num6 > buffer.Length) ? buffer.Length : ((int) num6);
                    int count = stream.Read(buffer, 0, lengthOfCryptoHeaderBytes);
                    outstream.Write(buffer, 0, count);
                    num6 -= count;
                    this.OnWriteBlock(stream.BytesRead, this.compressedSizeField);
                    if (this.ioOperationCanceledField)
                    {
                        break;
                    }
                }
                if ((this.bitFieldField & 8) == 8)
                {
                    int num7 = 0x10;
                    if (this.inputUsesZip64Field)
                    {
                        num7 += 8;
                    }
                    byte[] buffer2 = new byte[num7];
                    stream.Read(buffer2, 0, num7);
                    if (this.inputUsesZip64Field && (this.containerField.UseZip64WhenSaving == Zip64Option.Default))
                    {
                        outstream.Write(buffer2, 0, 8);
                        if (this.compressedSizeField > 0xffffffffL)
                        {
                            throw new InvalidOperationException("ZIP64 is required");
                        }
                        outstream.Write(buffer2, 8, 4);
                        if (this.uncompressedSizeField > 0xffffffffL)
                        {
                            throw new InvalidOperationException("ZIP64 is required");
                        }
                        outstream.Write(buffer2, 0x10, 4);
                        this.lengthOfTrailerField -= 8;
                    }
                    else if (!(this.inputUsesZip64Field || (this.containerField.UseZip64WhenSaving != Zip64Option.Always)))
                    {
                        byte[] buffer3 = new byte[4];
                        outstream.Write(buffer2, 0, 8);
                        outstream.Write(buffer2, 8, 4);
                        outstream.Write(buffer3, 0, 4);
                        outstream.Write(buffer2, 12, 4);
                        outstream.Write(buffer3, 0, 4);
                        this.lengthOfTrailerField += 8;
                    }
                    else
                    {
                        outstream.Write(buffer2, 0, num7);
                    }
                }
            }
            this.totalEntrySizeField = (this.lengthOfHeaderField + this.compressedFileDataSizeField) + this.lengthOfTrailerField;
        }

        private static ZipEntry Create(string nameInArchive, ZipEntrySource source, object arg1, object arg2)
        {
            if (string.IsNullOrEmpty(nameInArchive))
            {
                throw new ZipException("The entry name must be non-null and non-empty.");
            }
            ZipEntry entry = new ZipEntry();
            entry.versionMadeByField = 0x2d;
            entry.sourceField = source;
            entry.mtimeField = entry.atimeField = entry.ctimeField = DateTime.UtcNow;
            if (source == ZipEntrySource.Stream)
            {
                entry.sourceStreamField = arg1 as Stream;
            }
            else if (source == ZipEntrySource.WriteDelegate)
            {
                entry.writeDelegateField = arg1 as WriteDelegate;
            }
            else if (source == ZipEntrySource.JitStream)
            {
                entry.openDelegateField = arg1 as OpenDelegate;
                entry.closeDelegateField = arg2 as CloseDelegate;
            }
            else if (source != ZipEntrySource.ZipOutputStream)
            {
                if (source == ZipEntrySource.None)
                {
                    entry.sourceField = ZipEntrySource.FileSystem;
                }
                else
                {
                    string str = arg1 as string;
                    if (string.IsNullOrEmpty(str))
                    {
                        throw new ZipException("The filename must be non-null and non-empty.");
                    }
                    try
                    {
                        entry.mtimeField = File.GetLastWriteTime(str).ToUniversalTime();
                        entry.ctimeField = File.GetCreationTime(str).ToUniversalTime();
                        entry.atimeField = File.GetLastAccessTime(str).ToUniversalTime();
                        if (File.Exists(str) || Directory.Exists(str))
                        {
                            entry.externalFileAttrsField = (int) File.GetAttributes(str);
                        }
                        entry.ntfsTimesAreSetField = true;
                        entry.localFileNameField = Path.GetFullPath(str);
                    }
                    catch (PathTooLongException exception)
                    {
                        throw new ZipException(string.Format(CultureInfo.InvariantCulture, "The path is too long, filename={0}", str), exception);
                    }
                }
            }
            entry.lastModifiedField = entry.mtimeField;
            entry.fileNameInArchiveField = SharedUtilities.NormalizePathForUseInZipFile(nameInArchive);
            return entry;
        }

        internal static ZipEntry CreateForJitStreamProvider(string nameInArchive, OpenDelegate opener, CloseDelegate closer)
        {
            return Create(nameInArchive, ZipEntrySource.JitStream, opener, closer);
        }

        internal static ZipEntry CreateForStream(string entryName, Stream s)
        {
            return Create(entryName, ZipEntrySource.Stream, s, null);
        }

        internal static ZipEntry CreateForWriter(string entryName, WriteDelegate d)
        {
            return Create(entryName, ZipEntrySource.WriteDelegate, d, null);
        }

        internal static ZipEntry CreateForZipOutputStream(string nameInArchive)
        {
            return Create(nameInArchive, ZipEntrySource.ZipOutputStream, null, null);
        }

        internal static ZipEntry CreateFromFile(string filename, string nameInArchive)
        {
            return Create(nameInArchive, ZipEntrySource.FileSystem, filename, null);
        }

        internal static ZipEntry CreateFromNothing(string nameInArchive)
        {
            return Create(nameInArchive, ZipEntrySource.None, null, null);
        }

        /// <summary>
        /// Extract the entry to the filesystem, starting at the current
        /// working directory.
        /// </summary>
        /// 
        /// <overloads>
        /// This method has a bunch of overloads! One of them is sure to
        /// be the right one for you... If you don't like these, check
        /// out the <c>ExtractWithPassword()</c> methods.
        /// </overloads>
        /// 
        /// <seealso cref="P:ZipEntry.ExtractExistingFile" />
        /// <seealso cref="M:ZipEntry.Extract(ExtractExistingFileAction)" />
        /// 
        /// <remarks>
        /// 
        /// <para>
        /// This method extracts an entry from a zip file into the current
        /// working directory.  The path of the entry as extracted is the full
        /// path as specified in the zip archive, relative to the current
        /// working directory.  After the file is extracted successfully, the
        /// file attributes and timestamps are set.
        /// </para>
        /// 
        /// <para>
        /// The action taken when extraction an entry would overwrite an
        /// existing file is determined by the <see cref="P:ZipEntry.ExtractExistingFile" /> property.
        /// </para>
        /// 
        /// <para>
        /// Within the call to <c>Extract()</c>, the content for the entry is
        /// written into a filesystem file, and then the last modified time of the
        /// file is set according to the <see cref="P:ZipEntry.LastModified" /> property on
        /// the entry. See the remarks the <see cref="P:ZipEntry.LastModified" /> property for
        /// some details about the last modified time.
        /// </para>
        /// 
        /// </remarks>
        public void Extract()
        {
            this.InternalExtract(".", null, null);
        }

        /// <summary>
        /// Extract the entry to a file in the filesystem, using the specified
        /// behavior when extraction would overwrite an existing file.
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// See the remarks on the <see cref="P:ZipEntry.LastModified" /> property, for some
        /// details about how the last modified time of the file is set after
        /// extraction.
        /// </para>
        /// </remarks>
        /// 
        /// <param name="extractExistingFile">
        /// The action to take if extraction would overwrite an existing file.
        /// </param>
        public void Extract(ExtractExistingFileAction extractExistingFile)
        {
            this.ExtractExistingFile = extractExistingFile;
            this.InternalExtract(".", null, null);
        }

        /// <summary>
        /// Extracts the entry to the specified stream.
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// The caller can specify any write-able stream, for example a <see cref="T:System.IO.FileStream" />, a <see cref="T:System.IO.MemoryStream" />, or ASP.NET's
        /// <c>Response.OutputStream</c>.  The content will be decrypted and
        /// decompressed as necessary. If the entry is encrypted and no password
        /// is provided, this method will throw.
        /// </para>
        /// <para>
        /// The position on the stream is not reset by this method before it extracts.
        /// You may want to call stream.Seek() before calling ZipEntry.Extract().
        /// </para>
        /// </remarks>
        /// 
        /// <param name="stream">
        /// the stream to which the entry should be extracted.
        /// </param>
        public void Extract(Stream stream)
        {
            this.InternalExtract(null, stream, null);
        }

        /// <summary>
        /// Extract the entry to the filesystem, starting at the specified base
        /// directory.
        /// </summary>
        /// 
        /// <param name="baseDirectory">the pathname of the base directory</param>
        /// 
        /// <seealso cref="P:ZipEntry.ExtractExistingFile" />
        /// <seealso cref="M:ZipEntry.Extract(System.String,ExtractExistingFileAction)" />
        /// 
        /// <example>
        /// This example extracts only the entries in a zip file that are .txt files,
        /// into a directory called "textfiles".
        /// <code lang="C#">
        /// using (ZipFile zip = ZipFile.Read("PackedDocuments.zip"))
        /// {
        /// foreach (string s1 in zip.EntryFilenames)
        /// {
        /// if (s1.EndsWith(".txt"))
        /// {
        /// zip[s1].Extract("textfiles");
        /// }
        /// }
        /// }
        /// </code>
        /// <code lang="VB">
        /// Using zip As ZipFile = ZipFile.Read("PackedDocuments.zip")
        /// Dim s1 As String
        /// For Each s1 In zip.EntryFilenames
        /// If s1.EndsWith(".txt") Then
        /// zip(s1).Extract("textfiles")
        /// End If
        /// Next
        /// End Using
        /// </code>
        /// </example>
        /// 
        /// <remarks>
        /// 
        /// <para>
        /// Using this method, existing entries in the filesystem will not be
        /// overwritten. If you would like to force the overwrite of existing
        /// files, see the <see cref="P:ZipEntry.ExtractExistingFile" /> property, or call
        /// <see cref="M:ZipEntry.Extract(System.String,ExtractExistingFileAction)" />.
        /// </para>
        /// 
        /// <para>
        /// See the remarks on the <see cref="P:ZipEntry.LastModified" /> property, for some
        /// details about how the last modified time of the created file is set.
        /// </para>
        /// </remarks>
        public void Extract(string baseDirectory)
        {
            this.InternalExtract(baseDirectory, null, null);
        }

        /// <summary>
        /// Extract the entry to the filesystem, starting at the specified base
        /// directory, and using the specified behavior when extraction would
        /// overwrite an existing file.
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// See the remarks on the <see cref="P:ZipEntry.LastModified" /> property, for some
        /// details about how the last modified time of the created file is set.
        /// </para>
        /// </remarks>
        /// 
        /// <example>
        /// <code lang="C#">
        /// String sZipPath = "Airborne.zip";
        /// String sFilePath = "Readme.txt";
        /// String sRootFolder = "Digado";
        /// using (ZipFile zip = ZipFile.Read(sZipPath))
        /// {
        /// if (zip.EntryFileNames.Contains(sFilePath))
        /// {
        /// // use the string indexer on the zip file
        /// zip[sFileName].Extract(sRootFolder,
        /// ExtractExistingFileAction.OverwriteSilently);
        /// }
        /// }
        /// </code>
        /// 
        /// <code lang="VB">
        /// Dim sZipPath as String = "Airborne.zip"
        /// Dim sFilePath As String = "Readme.txt"
        /// Dim sRootFolder As String = "Digado"
        /// Using zip As ZipFile = ZipFile.Read(sZipPath)
        /// If zip.EntryFileNames.Contains(sFilePath)
        /// ' use the string indexer on the zip file
        /// zip(sFilePath).Extract(sRootFolder, _
        /// ExtractExistingFileAction.OverwriteSilently)
        /// End If
        /// End Using
        /// </code>
        /// </example>
        /// 
        /// <param name="baseDirectory">the pathname of the base directory</param>
        /// <param name="extractExistingFile">
        /// The action to take if extraction would overwrite an existing file.
        /// </param>
        public void Extract(string baseDirectory, ExtractExistingFileAction extractExistingFile)
        {
            this.ExtractExistingFile = extractExistingFile;
            this.InternalExtract(baseDirectory, null, null);
        }

        private int ExtractOne(Stream output)
        {
            int crc = 0;
            Stream archiveStream = this.ArchiveStream;
            try
            {
                archiveStream.Seek(this.FileDataPosition, SeekOrigin.Begin);
                byte[] buffer = new byte[this.BufferSize];
                long num2 = (this.compressionMethodFromZipFileField != 0) ? this.UncompressedSize : this.compressedFileDataSizeField;
                this.inputDecryptorStreamField = this.GetExtractDecryptor(archiveStream);
                Stream extractDecompressor = this.GetExtractDecompressor(this.inputDecryptorStreamField);
                long bytesWritten = 0L;
                using (CrcCalculatorStream stream3 = new CrcCalculatorStream(extractDecompressor))
                {
                    while (num2 > 0L)
                    {
                        int count = (num2 > buffer.Length) ? buffer.Length : ((int) num2);
                        int nbytes = stream3.Read(buffer, 0, count);
                        this._CheckRead(nbytes);
                        output.Write(buffer, 0, nbytes);
                        num2 -= nbytes;
                        bytesWritten += nbytes;
                        this.OnExtractProgress(bytesWritten, this.UncompressedSize);
                        if (this.ioOperationCanceledField)
                        {
                            break;
                        }
                    }
                    crc = stream3.Crc;
                }
            }
            finally
            {
                ZipSegmentedStream stream4 = archiveStream as ZipSegmentedStream;
                if (stream4 != null)
                {
                    stream4.Dispose();
                    this.archiveStreamField = null;
                }
            }
            return crc;
        }

        /// <summary>
        /// Extract the entry to the filesystem, using the current working directory
        /// and the specified password.
        /// </summary>
        /// 
        /// <overloads>
        /// This method has a bunch of overloads! One of them is sure to be
        /// the right one for you...
        /// </overloads>
        /// 
        /// <seealso cref="P:ZipEntry.ExtractExistingFile" />
        /// <seealso cref="M:ZipEntry.ExtractWithPassword(ExtractExistingFileAction,System.String)" />
        /// 
        /// <remarks>
        /// 
        /// <para>
        /// Existing entries in the filesystem will not be overwritten. If you
        /// would like to force the overwrite of existing files, see the <see cref="P:ZipEntry.ExtractExistingFile" />property, or call
        /// <see cref="M:ZipEntry.ExtractWithPassword(ExtractExistingFileAction,System.String)" />.
        /// </para>
        /// 
        /// <para>
        /// See the remarks on the <see cref="P:ZipEntry.LastModified" /> property for some
        /// details about how the "last modified" time of the created file is
        /// set.
        /// </para>
        /// </remarks>
        /// 
        /// <example>
        /// In this example, entries that use encryption are extracted using a
        /// particular password.
        /// <code>
        /// using (var zip = ZipFile.Read(FilePath))
        /// {
        /// foreach (ZipEntry e in zip)
        /// {
        /// if (e.UsesEncryption)
        /// e.ExtractWithPassword("Secret!");
        /// else
        /// e.Extract();
        /// }
        /// }
        /// </code>
        /// <code lang="VB">
        /// Using zip As ZipFile = ZipFile.Read(FilePath)
        /// Dim e As ZipEntry
        /// For Each e In zip
        /// If (e.UsesEncryption)
        /// e.ExtractWithPassword("Secret!")
        /// Else
        /// e.Extract
        /// End If
        /// Next
        /// End Using
        /// </code>
        /// </example>
        /// <param name="password">The Password to use for decrypting the entry.</param>
        public void ExtractWithPassword(string password)
        {
            this.InternalExtract(".", null, password);
        }

        /// <summary>
        /// Extract the entry to a file in the filesystem, relative to the
        /// current directory, using the specified behavior when extraction
        /// would overwrite an existing file.
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// See the remarks on the <see cref="P:ZipEntry.LastModified" /> property, for some
        /// details about how the last modified time of the created file is set.
        /// </para>
        /// </remarks>
        /// 
        /// <param name="password">The Password to use for decrypting the entry.</param>
        /// 
        /// <param name="extractExistingFile">
        /// The action to take if extraction would overwrite an existing file.
        /// </param>
        public void ExtractWithPassword(ExtractExistingFileAction extractExistingFile, string password)
        {
            this.ExtractExistingFile = extractExistingFile;
            this.InternalExtract(".", null, password);
        }

        /// <summary>
        /// Extracts the entry to the specified stream, using the specified
        /// Password.  For example, the caller could extract to Console.Out, or
        /// to a MemoryStream.
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// The caller can specify any write-able stream, for example a <see cref="T:System.IO.FileStream" />, a <see cref="T:System.IO.MemoryStream" />, or ASP.NET's
        /// <c>Response.OutputStream</c>.  The content will be decrypted and
        /// decompressed as necessary. If the entry is encrypted and no password
        /// is provided, this method will throw.
        /// </para>
        /// <para>
        /// The position on the stream is not reset by this method before it extracts.
        /// You may want to call stream.Seek() before calling ZipEntry.Extract().
        /// </para>
        /// </remarks>
        /// 
        /// 
        /// <param name="stream">
        /// the stream to which the entry should be extracted.
        /// </param>
        /// <param name="password">
        /// The password to use for decrypting the entry.
        /// </param>
        public void ExtractWithPassword(Stream stream, string password)
        {
            this.InternalExtract(null, stream, password);
        }

        /// <summary>
        /// Extract the entry to the filesystem, starting at the specified base
        /// directory, and using the specified password.
        /// </summary>
        /// 
        /// <seealso cref="P:ZipEntry.ExtractExistingFile" />
        /// <seealso cref="M:ZipEntry.ExtractWithPassword(System.String,ExtractExistingFileAction,System.String)" />
        /// 
        /// <remarks>
        /// <para>
        /// Existing entries in the filesystem will not be overwritten. If you
        /// would like to force the overwrite of existing files, see the <see cref="P:ZipEntry.ExtractExistingFile" />property, or call
        /// <see cref="M:ZipEntry.ExtractWithPassword(ExtractExistingFileAction,System.String)" />.
        /// </para>
        /// 
        /// <para>
        /// See the remarks on the <see cref="P:ZipEntry.LastModified" /> property, for some
        /// details about how the last modified time of the created file is set.
        /// </para>
        /// </remarks>
        /// 
        /// <param name="baseDirectory">The pathname of the base directory.</param>
        /// <param name="password">The Password to use for decrypting the entry.</param>
        public void ExtractWithPassword(string baseDirectory, string password)
        {
            this.InternalExtract(baseDirectory, null, password);
        }

        /// <summary>
        /// Extract the entry to the filesystem, starting at the specified base
        /// directory, and using the specified behavior when extraction would
        /// overwrite an existing file.
        /// </summary>
        /// 
        /// <remarks>
        /// See the remarks on the <see cref="P:ZipEntry.LastModified" /> property, for some
        /// details about how the last modified time of the created file is set.
        /// </remarks>
        /// 
        /// <param name="baseDirectory">the pathname of the base directory</param>
        /// 
        /// <param name="extractExistingFile">The action to take if extraction would
        /// overwrite an existing file.</param>
        /// 
        /// <param name="password">The Password to use for decrypting the entry.</param>
        public void ExtractWithPassword(string baseDirectory, ExtractExistingFileAction extractExistingFile, string password)
        {
            this.ExtractExistingFile = extractExistingFile;
            this.InternalExtract(baseDirectory, null, password);
        }

        private int FigureCrc32()
        {
            if (!this.crcCalculatedField)
            {
                Stream input = null;
                if (this.sourceField == ZipEntrySource.WriteDelegate)
                {
                    CrcCalculatorStream stream = new CrcCalculatorStream(Stream.Null);
                    this.writeDelegateField(this.FileName, stream);
                    this.crc32Field = stream.Crc;
                }
                else if (this.sourceField != ZipEntrySource.ZipFile)
                {
                    if (this.sourceField == ZipEntrySource.Stream)
                    {
                        this.PrepSourceStream();
                        input = this.sourceStreamField;
                    }
                    else if (this.sourceField == ZipEntrySource.JitStream)
                    {
                        if (this.sourceStreamField == null)
                        {
                            this.sourceStreamField = this.openDelegateField(this.FileName);
                        }
                        this.PrepSourceStream();
                        input = this.sourceStreamField;
                    }
                    else if (this.sourceField != ZipEntrySource.ZipOutputStream)
                    {
                        input = File.Open(this.LocalFileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    }
                    this.crc32Field = new CRC32().GetCrc32(input);
                    if (this.sourceStreamField == null)
                    {
                        input.Dispose();
                    }
                }
                this.crcCalculatedField = true;
            }
            return this.crc32Field;
        }

        /// <summary>
        /// Finds a particular segment in the given extra field.
        /// This is used when modifying a previously-generated
        /// extra field, in particular when removing the AES crypto
        /// segment in the extra field.
        /// </summary>
        internal static int FindExtraFieldSegment(byte[] extra, int offx, ushort targetHeaderId)
        {
            short num3;
            for (int i = offx; (i + 3) < extra.Length; i += num3)
            {
                ushort num2 = (ushort) (extra[i++] + (extra[i++] * 0x100));
                if (num2 == targetHeaderId)
                {
                    return (i - 2);
                }
                num3 = (short) (extra[i++] + (extra[i++] * 0x100));
            }
            return -1;
        }

        internal void FinishOutputStream(Stream s, CountingStream entryCounter, Stream encryptor, Stream compressor, CrcCalculatorStream output)
        {
            if (output != null)
            {
                output.Close();
                if (compressor is DeflateStream)
                {
                    compressor.Close();
                }
                else if (compressor is BZip2OutputStream)
                {
                    compressor.Close();
                }
                else if (compressor is ParallelBZip2OutputStream)
                {
                    compressor.Close();
                }
                else if (compressor is ParallelDeflateOutputStream)
                {
                    compressor.Close();
                }
                encryptor.Flush();
                encryptor.Close();
                this.lengthOfTrailerField = 0;
                this.uncompressedSizeField = output.TotalBytesSlurped;
                WinZipAesCipherStream stream = encryptor as WinZipAesCipherStream;
                if ((stream != null) && (this.uncompressedSizeField > 0L))
                {
                    s.Write(stream.FinalAuthentication, 0, 10);
                    this.lengthOfTrailerField += 10;
                }
                this.compressedFileDataSizeField = entryCounter.BytesWritten;
                this.compressedSizeField = this.compressedFileDataSizeField;
                this.crc32Field = output.Crc;
                this.StoreRelativeOffset();
            }
        }

        /// <summary>
        /// generate and return a byte array that encodes the filename
        /// for the entry.
        /// </summary>
        /// <remarks>
        /// <para>
        /// side effects: generate and store into _CommentBytes the
        /// byte array for any comment attached to the entry. Also
        /// sets _actualEncoding to indicate the actual encoding
        /// used. The same encoding is used for both filename and
        /// comment.
        /// </para>
        /// </remarks>
        private byte[] GetEncodedFileNameBytes()
        {
            string s = this.NormalizeFileName();
            switch (this.AlternateEncodingUsage)
            {
                case ZipOption.Default:
                    if ((this.commentField != null) && (this.commentField.Length != 0))
                    {
                        this.commentBytesField = ibm437Field.GetBytes(this.commentField);
                    }
                    this.actualEncodingField = ibm437Field;
                    return ibm437Field.GetBytes(s);

                case ZipOption.Always:
                    if ((this.commentField != null) && (this.commentField.Length != 0))
                    {
                        this.commentBytesField = this.AlternateEncoding.GetBytes(this.commentField);
                    }
                    this.actualEncodingField = this.AlternateEncoding;
                    return this.AlternateEncoding.GetBytes(s);
            }
            byte[] bytes = ibm437Field.GetBytes(s);
            string str2 = ibm437Field.GetString(bytes, 0, bytes.Length);
            this.commentBytesField = null;
            if (str2 != s)
            {
                bytes = this.AlternateEncoding.GetBytes(s);
                if ((this.commentField != null) && (this.commentField.Length != 0))
                {
                    this.commentBytesField = this.AlternateEncoding.GetBytes(this.commentField);
                }
                this.actualEncodingField = this.AlternateEncoding;
                return bytes;
            }
            this.actualEncodingField = ibm437Field;
            if ((this.commentField != null) && (this.commentField.Length != 0))
            {
                byte[] buffer2 = ibm437Field.GetBytes(this.commentField);
                if (ibm437Field.GetString(buffer2, 0, buffer2.Length) != this.Comment)
                {
                    bytes = this.AlternateEncoding.GetBytes(s);
                    this.commentBytesField = this.AlternateEncoding.GetBytes(this.commentField);
                    this.actualEncodingField = this.AlternateEncoding;
                    return bytes;
                }
                this.commentBytesField = buffer2;
            }
            return bytes;
        }

        internal Stream GetExtractDecompressor(Stream input2)
        {
            switch (this.compressionMethodFromZipFileField)
            {
                case 0:
                    return input2;

                case 8:
                    return new DeflateStream(input2, CompressionMode.Decompress, true);

                case 12:
                    return new BZip2InputStream(input2, true);
            }
            return null;
        }

        internal Stream GetExtractDecryptor(Stream input)
        {
            if (this.encryptionFromZipFileField == EncryptionAlgorithm.PkzipWeak)
            {
                return new ZipCipherStream(input, this.zipCryptoForExtractField, CryptoMode.Decrypt);
            }
            if ((this.encryptionFromZipFileField == EncryptionAlgorithm.WinZipAes128) || (this.encryptionFromZipFileField == EncryptionAlgorithm.WinZipAes256))
            {
                return new WinZipAesCipherStream(input, this.aesCryptoForExtractField, this.compressedFileDataSizeField, CryptoMode.Decrypt);
            }
            return input;
        }

        private static int GetKeyStrengthInBits(EncryptionAlgorithm a)
        {
            if (a == EncryptionAlgorithm.WinZipAes256)
            {
                return 0x100;
            }
            if (a == EncryptionAlgorithm.WinZipAes128)
            {
                return 0x80;
            }
            return -1;
        }

        internal static int GetLengthOfCryptoHeaderBytes(EncryptionAlgorithm a)
        {
            if (a == EncryptionAlgorithm.None)
            {
                return 0;
            }
            if ((a == EncryptionAlgorithm.WinZipAes128) || (a == EncryptionAlgorithm.WinZipAes256))
            {
                return (((GetKeyStrengthInBits(a) / 8) / 2) + 2);
            }
            if (a != EncryptionAlgorithm.PkzipWeak)
            {
                throw new ZipException("internal error");
            }
            return 12;
        }

        internal static void HandlePK00Prefix(Stream s)
        {
            if (SharedUtilities.ReadInt(s) != 0x30304b50)
            {
                s.Seek(-4L, SeekOrigin.Current);
            }
        }

        private static void HandleUnexpectedDataDescriptor(ZipEntry entry)
        {
            Stream archiveStream = entry.ArchiveStream;
            if (((ulong) SharedUtilities.ReadInt(archiveStream)) == (ulong)entry.crc32Field)
            {
                if (SharedUtilities.ReadInt(archiveStream) == entry.compressedSizeField)
                {
                    if (SharedUtilities.ReadInt(archiveStream) != entry.uncompressedSizeField)
                    {
                        archiveStream.Seek(-12L, SeekOrigin.Current);
                    }
                }
                else
                {
                    archiveStream.Seek(-8L, SeekOrigin.Current);
                }
            }
            else
            {
                archiveStream.Seek(-4L, SeekOrigin.Current);
            }
        }

        private void InternalExtract(string baseDir, Stream outstream, string password)
        {
            if (this.containerField == null)
            {
                throw new BadStateException("This entry is an orphan");
            }
            if (this.containerField.ZipFile == null)
            {
                throw new InvalidOperationException("Use Extract() only with ZipFile.");
            }
            this.containerField.ZipFile.Reset(false);
            if (this.sourceField != ZipEntrySource.ZipFile)
            {
                throw new BadStateException("You must call ZipFile.Save before calling any Extract method");
            }
            this.OnBeforeExtract(baseDir);
            this.ioOperationCanceledField = false;
            string outFileName = null;
            Stream output = null;
            bool flag = false;
            bool flag2 = false;
            try
            {
                this.ValidateCompression();
                this.ValidateEncryption();
                if (this.ValidateOutput(baseDir, outstream, out outFileName))
                {
                    this.WriteStatus("extract dir {0}...", new object[] { outFileName });
                    this.OnAfterExtract(baseDir);
                }
                else
                {
                    if ((outFileName != null) && File.Exists(outFileName))
                    {
                        flag = true;
                        switch (this.CheckExtractExistingFile(baseDir, outFileName))
                        {
                            case 2:
                            case 1:
                                return;
                        }
                    }
                    string str2 = password ?? (this.passwordField ?? this.containerField.Password);
                    if (this.encryptionFromZipFileField != EncryptionAlgorithm.None)
                    {
                        if (str2 == null)
                        {
                            throw new BadPasswordException();
                        }
                        this.SetupCryptoForExtract(str2);
                    }
                    if (outFileName != null)
                    {
                        this.WriteStatus("extract file {0}...", new object[] { outFileName });
                        outFileName = outFileName + ".tmp";
                        string directoryName = Path.GetDirectoryName(outFileName);
                        if (!Directory.Exists(directoryName))
                        {
                            Directory.CreateDirectory(directoryName);
                        }
                        else if (this.containerField.ZipFile != null)
                        {
                            flag2 = this.containerField.ZipFile.inExtractAllField;
                        }
                        output = new FileStream(outFileName, FileMode.CreateNew);
                    }
                    else
                    {
                        this.WriteStatus("extract entry {0} to stream...", new object[] { this.FileName });
                        output = outstream;
                    }
                    if (!this.ioOperationCanceledField)
                    {
                        int num2 = this.ExtractOne(output);
                        if (!this.ioOperationCanceledField)
                        {
                            this.VerifyCrcAfterExtract(num2);
                            if (outFileName != null)
                            {
                                output.Close();
                                output = null;
                                string sourceFileName = outFileName;
                                string destFileName = null;
                                outFileName = sourceFileName.Substring(0, sourceFileName.Length - 4);
                                if (flag)
                                {
                                    destFileName = outFileName + ".PendingOverwrite";
                                    File.Move(outFileName, destFileName);
                                }
                                File.Move(sourceFileName, outFileName);
                                this._SetTimes(outFileName, true);
                                if ((destFileName != null) && File.Exists(destFileName))
                                {
                                    ReallyDelete(destFileName);
                                }
                                if (flag2 && (this.FileName.IndexOf('/') != -1))
                                {
                                    string str6 = Path.GetDirectoryName(this.FileName);
                                    if (this.containerField.ZipFile[str6] == null)
                                    {
                                        this._SetTimes(Path.GetDirectoryName(outFileName), false);
                                    }
                                }
                                if (((this.versionMadeByField & 0xff00) == 0xa00) || ((this.versionMadeByField & 0xff00) == 0))
                                {
                                    File.SetAttributes(outFileName, (FileAttributes) this.externalFileAttrsField);
                                }
                            }
                            this.OnAfterExtract(baseDir);
                        }
                    }
                }
            }
            catch (Exception)
            {
                this.ioOperationCanceledField = true;
                throw;
            }
            finally
            {
                if (this.ioOperationCanceledField && (outFileName != null))
                {
                    try
                    {
                        if (output != null)
                        {
                            output.Close();
                        }
                        if (!(!File.Exists(outFileName) || flag))
                        {
                            File.Delete(outFileName);
                        }
                    }
                    finally
                    {
                    }
                }
            }
        }

        internal CrcCalculatorStream InternalOpenReader(string password)
        {
            this.ValidateCompression();
            this.ValidateEncryption();
            this.SetupCryptoForExtract(password);
            if (this.sourceField != ZipEntrySource.ZipFile)
            {
                throw new BadStateException("You must call ZipFile.Save before calling OpenReader");
            }
            long length = (this.compressionMethodFromZipFileField == 0) ? this.compressedFileDataSizeField : this.UncompressedSize;
            Stream archiveStream = this.ArchiveStream;
            this.ArchiveStream.Seek(this.FileDataPosition, SeekOrigin.Begin);
            this.inputDecryptorStreamField = this.GetExtractDecryptor(archiveStream);
            return new CrcCalculatorStream(this.GetExtractDecompressor(this.inputDecryptorStreamField), length);
        }

        private static bool IsNotValidSig(int signature)
        {
            return (signature != 0x4034b50);
        }

        /// <summary>
        /// Returns true if the passed-in value is a valid signature for a ZipDirEntry.
        /// </summary>
        /// <param name="signature">the candidate 4-byte signature value.</param>
        /// <returns>true, if the signature is valid according to the PKWare spec.</returns>
        internal static bool IsNotValidZipDirEntrySig(int signature)
        {
            return (signature != 0x2014b50);
        }

        internal void MarkAsDirectory()
        {
            this.isDirectoryField = true;
            if (!this.fileNameInArchiveField.EndsWith("/"))
            {
                this.fileNameInArchiveField = this.fileNameInArchiveField + "/";
            }
        }

        private Stream MaybeApplyCompression(Stream s, long streamLength)
        {
            if ((this.compressionMethodField == 8) && (this.CompressionLevel != CompressionLevel.None))
            {
                if ((this.containerField.ParallelDeflateThreshold == 0L) || ((streamLength > this.containerField.ParallelDeflateThreshold) && (this.containerField.ParallelDeflateThreshold > 0L)))
                {
                    if (this.containerField.ParallelDeflater == null)
                    {
                        this.containerField.ParallelDeflater = new ParallelDeflateOutputStream(s, this.CompressionLevel, this.containerField.Strategy, true);
                        if (this.containerField.CodecBufferSize > 0)
                        {
                            this.containerField.ParallelDeflater.BufferSize = this.containerField.CodecBufferSize;
                        }
                        if (this.containerField.ParallelDeflateMaxBufferPairs > 0)
                        {
                            this.containerField.ParallelDeflater.MaxBufferPairs = this.containerField.ParallelDeflateMaxBufferPairs;
                        }
                    }
                    ParallelDeflateOutputStream parallelDeflater = this.containerField.ParallelDeflater;
                    parallelDeflater.Reset(s);
                    return parallelDeflater;
                }
                DeflateStream stream2 = new DeflateStream(s, CompressionMode.Compress, this.CompressionLevel, true);
                if (this.containerField.CodecBufferSize > 0)
                {
                    stream2.BufferSize = this.containerField.CodecBufferSize;
                }
                stream2.Strategy = this.containerField.Strategy;
                return stream2;
            }
            if (this.compressionMethodField == 12)
            {
                if ((this.containerField.ParallelDeflateThreshold == 0L) || ((streamLength > this.containerField.ParallelDeflateThreshold) && (this.containerField.ParallelDeflateThreshold > 0L)))
                {
                    return new ParallelBZip2OutputStream(s, true);
                }
                return new BZip2OutputStream(s, true);
            }
            return s;
        }

        private Stream MaybeApplyEncryption(Stream s)
        {
            if (this.Encryption == EncryptionAlgorithm.PkzipWeak)
            {
                return new ZipCipherStream(s, this.zipCryptoForWriteField, CryptoMode.Encrypt);
            }
            if ((this.Encryption == EncryptionAlgorithm.WinZipAes128) || (this.Encryption == EncryptionAlgorithm.WinZipAes256))
            {
                return new WinZipAesCipherStream(s, this.aesCryptoForWriteField, CryptoMode.Encrypt);
            }
            return s;
        }

        private void MaybeUnsetCompressionMethodForWriting(int cycle)
        {
            if (cycle > 1)
            {
                this.compressionMethodField = 0;
            }
            else if (this.IsDirectory)
            {
                this.compressionMethodField = 0;
            }
            else if (this.sourceField != ZipEntrySource.ZipFile)
            {
                if (this.sourceField == ZipEntrySource.Stream)
                {
                    if (((this.sourceStreamField != null) && this.sourceStreamField.CanSeek) && (this.sourceStreamField.Length == 0L))
                    {
                        this.compressionMethodField = 0;
                        return;
                    }
                }
                else if ((this.sourceField == ZipEntrySource.FileSystem) && (SharedUtilities.GetFileLength(this.LocalFileName) == 0L))
                {
                    this.compressionMethodField = 0;
                    return;
                }
                if (this.SetCompression != null)
                {
                    this.CompressionLevel = this.SetCompression(this.LocalFileName, this.fileNameInArchiveField);
                }
                if ((this.CompressionLevel == CompressionLevel.None) && (this.CompressionMethod == CompressionMethod.Deflate))
                {
                    this.compressionMethodField = 0;
                }
            }
        }

        internal static string NameInArchive(string filename, string directoryPathInArchive)
        {
            string pathName = null;
            if (directoryPathInArchive == null)
            {
                pathName = filename;
            }
            else if (string.IsNullOrEmpty(directoryPathInArchive))
            {
                pathName = Path.GetFileName(filename);
            }
            else
            {
                pathName = Path.Combine(directoryPathInArchive, Path.GetFileName(filename));
            }
            return SharedUtilities.NormalizePathForUseInZipFile(pathName);
        }

        private string NormalizeFileName()
        {
            string str = this.FileName.Replace(@"\", "/");
            if (((this.trimVolumeFromFullyQualifiedPathsField && (this.FileName.Length >= 3)) && (this.FileName[1] == ':')) && (str[2] == '/'))
            {
                return str.Substring(3);
            }
            if ((this.FileName.Length >= 4) && ((str[0] == '/') && (str[1] == '/')))
            {
                int index = str.IndexOf('/', 2);
                if (index == -1)
                {
                    throw new ArgumentException("The path for that entry appears to be badly formatted");
                }
                return str.Substring(index + 1);
            }
            if ((this.FileName.Length >= 3) && ((str[0] == '.') && (str[1] == '/')))
            {
                return str.Substring(2);
            }
            return str;
        }

        internal void NotifySaveComplete()
        {
            this.encryptionFromZipFileField = this.encryptionField;
            this.compressionMethodFromZipFileField = this.compressionMethodField;
            this.restreamRequiredOnSaveField = false;
            this.metadataChangedField = false;
            this.sourceField = ZipEntrySource.ZipFile;
        }

        private void OnAfterExtract(string path)
        {
            if ((this.containerField.ZipFile != null) && !this.containerField.ZipFile.inExtractAllField)
            {
                this.containerField.ZipFile.OnSingleEntryExtract(this, path, false);
            }
        }

        private void OnBeforeExtract(string path)
        {
            if ((this.containerField.ZipFile != null) && !this.containerField.ZipFile.inExtractAllField)
            {
                this.ioOperationCanceledField = this.containerField.ZipFile.OnSingleEntryExtract(this, path, true);
            }
        }

        private void OnExtractExisting(string path)
        {
            if (this.containerField.ZipFile != null)
            {
                this.ioOperationCanceledField = this.containerField.ZipFile.OnExtractExisting(this, path);
            }
        }

        private void OnExtractProgress(long bytesWritten, long totalBytesToWrite)
        {
            if (this.containerField.ZipFile != null)
            {
                this.ioOperationCanceledField = this.containerField.ZipFile.OnExtractBlock(this, bytesWritten, totalBytesToWrite);
            }
        }

        private void OnWriteBlock(long bytesXferred, long totalBytesToXfer)
        {
            if (this.containerField.ZipFile != null)
            {
                this.ioOperationCanceledField = this.containerField.ZipFile.OnSaveBlock(this, bytesXferred, totalBytesToXfer);
            }
        }

        private void OnZipErrorWhileSaving(Exception e)
        {
            if (this.containerField.ZipFile != null)
            {
                this.ioOperationCanceledField = this.containerField.ZipFile.OnZipErrorSaving(this, e);
            }
        }

        /// <summary>
        /// Opens a readable stream corresponding to the zip entry in the
        /// archive.  The stream decompresses and decrypts as necessary, as it
        /// is read.
        /// </summary>
        /// 
        /// <remarks>
        /// 
        /// <para>
        /// DotNetZip offers a variety of ways to extract entries from a zip
        /// file.  This method allows an application to extract an entry by
        /// reading a <see cref="T:System.IO.Stream" />.
        /// </para>
        /// 
        /// <para>
        /// The return value is of type <see cref="T:CrcCalculatorStream" />.  Use it as you would any
        /// stream for reading.  When an application calls <see cref="M:System.IO.Stream.Read(System.Byte[],System.Int32,System.Int32)" /> on that stream, it will
        /// receive data from the zip entry that is decrypted and decompressed
        /// as necessary.
        /// </para>
        /// 
        /// <para>
        /// <c>CrcCalculatorStream</c> adds one additional feature: it keeps a
        /// CRC32 checksum on the bytes of the stream as it is read.  The CRC
        /// value is available in the <see cref="P:CrcCalculatorStream.Crc" /> property on the
        /// <c>CrcCalculatorStream</c>.  When the read is complete, your
        /// application
        /// <em>should</em> check this CRC against the <see cref="P:ZipEntry.Crc" />
        /// property on the <c>ZipEntry</c> to validate the content of the
        /// ZipEntry. You don't have to validate the entry using the CRC, but
        /// you should, to verify integrity. Check the example for how to do
        /// this.
        /// </para>
        /// 
        /// <para>
        /// If the entry is protected with a password, then you need to provide
        /// a password prior to calling <see cref="M:ZipEntry.OpenReader" />, either by
        /// setting the <see cref="P:ZipEntry.Password" /> property on the entry, or the
        /// <see cref="P:ZipFile.Password" /> property on the <c>ZipFile</c>
        /// itself. Or, you can use <see cref="M:ZipEntry.OpenReader(System.String)" />, the
        /// overload of OpenReader that accepts a password parameter.
        /// </para>
        /// 
        /// <para>
        /// If you want to extract entry data into a write-able stream that is
        /// already opened, like a <see cref="T:System.IO.FileStream" />, do not
        /// use this method. Instead, use <see cref="M:ZipEntry.Extract(System.IO.Stream)" />.
        /// </para>
        /// 
        /// <para>
        /// Your application may use only one stream created by OpenReader() at
        /// a time, and you should not call other Extract methods before
        /// completing your reads on a stream obtained from OpenReader().  This
        /// is because there is really only one source stream for the compressed
        /// content.  A call to OpenReader() seeks in the source stream, to the
        /// beginning of the compressed content.  A subsequent call to
        /// OpenReader() on a different entry will seek to a different position
        /// in the source stream, as will a call to Extract() or one of its
        /// overloads.  This will corrupt the state for the decompressing stream
        /// from the original call to OpenReader().
        /// </para>
        /// 
        /// <para>
        /// The <c>OpenReader()</c> method works only when the ZipEntry is
        /// obtained from an instance of <c>ZipFile</c>. This method will throw
        /// an exception if the ZipEntry is obtained from a <see cref="T:ZipInputStream" />.
        /// </para>
        /// </remarks>
        /// 
        /// <example>
        /// This example shows how to open a zip archive, then read in a named
        /// entry via a stream. After the read loop is complete, the code
        /// compares the calculated during the read loop with the expected CRC
        /// on the <c>ZipEntry</c>, to verify the extraction.
        /// <code>
        /// using (ZipFile zip = new ZipFile(ZipFileToRead))
        /// {
        /// ZipEntry e1= zip["Elevation.mp3"];
        /// using (CrcCalculatorStream s = e1.OpenReader())
        /// {
        /// byte[] buffer = new byte[4096];
        /// int n, totalBytesRead= 0;
        /// do {
        /// n = s.Read(buffer,0, buffer.Length);
        /// totalBytesRead+=n;
        /// } while (n&gt;0);
        /// if (s.Crc32 != e1.Crc32)
        /// throw new Exception(string.Format(CultureInfo.InvariantCulture, "The Zip Entry failed the CRC Check. (0x{0:X8}!=0x{1:X8})", s.Crc32, e1.Crc32));
        /// if (totalBytesRead != e1.UncompressedSize)
        /// throw new Exception(string.Format(CultureInfo.InvariantCulture, "We read an unexpected number of bytes. ({0}!={1})", totalBytesRead, e1.UncompressedSize));
        /// }
        /// }
        /// </code>
        /// <code lang="VB">
        /// Using zip As New ZipFile(ZipFileToRead)
        /// Dim e1 As ZipEntry = zip.Item("Elevation.mp3")
        /// Using s As CrcCalculatorStream = e1.OpenReader
        /// Dim n As Integer
        /// Dim buffer As Byte() = New Byte(4096) {}
        /// Dim totalBytesRead As Integer = 0
        /// Do
        /// n = s.Read(buffer, 0, buffer.Length)
        /// totalBytesRead = (totalBytesRead + n)
        /// Loop While (n &gt; 0)
        /// If (s.Crc32 &lt;&gt; e1.Crc32) Then
        /// Throw New Exception(string.Format(CultureInfo.InvariantCulture, "The Zip Entry failed the CRC Check. (0x{0:X8}!=0x{1:X8})", s.Crc32, e1.Crc32))
        /// End If
        /// If (totalBytesRead &lt;&gt; e1.UncompressedSize) Then
        /// Throw New Exception(string.Format(CultureInfo.InvariantCulture, "We read an unexpected number of bytes. ({0}!={1})", totalBytesRead, e1.UncompressedSize))
        /// End If
        /// End Using
        /// End Using
        /// </code>
        /// </example>
        /// <seealso cref="M:ZipEntry.Extract(System.IO.Stream)" />
        /// <returns>The Stream for reading.</returns>
        public CrcCalculatorStream OpenReader()
        {
            if (this.containerField.ZipFile == null)
            {
                throw new InvalidOperationException("Use OpenReader() only with ZipFile.");
            }
            return this.InternalOpenReader(this.passwordField ?? this.containerField.Password);
        }

        /// <summary>
        /// Opens a readable stream for an encrypted zip entry in the archive.
        /// The stream decompresses and decrypts as necessary, as it is read.
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// See the documentation on the <see cref="M:ZipEntry.OpenReader" /> method for
        /// full details. This overload allows the application to specify a
        /// password for the <c>ZipEntry</c> to be read.
        /// </para>
        /// </remarks>
        /// 
        /// <param name="password">The password to use for decrypting the entry.</param>
        /// <returns>The Stream for reading.</returns>
        public CrcCalculatorStream OpenReader(string password)
        {
            if (this.containerField.ZipFile == null)
            {
                throw new InvalidOperationException("Use OpenReader() only with ZipFile.");
            }
            return this.InternalOpenReader(password);
        }

        internal void PostProcessOutput(Stream s)
        {
            int num2;
            short num9;
            CountingStream stream = s as CountingStream;
            if ((this.uncompressedSizeField == 0L) && (this.compressedSizeField == 0L))
            {
                if (this.sourceField == ZipEntrySource.ZipOutputStream)
                {
                    return;
                }
                if (this.passwordField != null)
                {
                    int num = 0;
                    if (this.Encryption == EncryptionAlgorithm.PkzipWeak)
                    {
                        num = 12;
                    }
                    else if ((this.Encryption == EncryptionAlgorithm.WinZipAes128) || (this.Encryption == EncryptionAlgorithm.WinZipAes256))
                    {
                        num = this.aesCryptoForWriteField.saltField.Length + this.aesCryptoForWriteField.GeneratedPV.Length;
                    }
                    if (!((this.sourceField != ZipEntrySource.ZipOutputStream) || s.CanSeek))
                    {
                        throw new ZipException("Zero bytes written, encryption in use, and non-seekable output.");
                    }
                    if (this.Encryption != EncryptionAlgorithm.None)
                    {
                        s.Seek((long) (-1 * num), SeekOrigin.Current);
                        s.SetLength(s.Position);
                        if (stream != null)
                        {
                            stream.Adjust((long) num);
                        }
                        this.lengthOfHeaderField -= num;
                        this.fileDataPositionField -= num;
                    }
                    this.passwordField = null;
                    this.bitFieldField = (short) (this.bitFieldField & -2);
                    num2 = 6;
                    this.entryHeaderField[num2++] = (byte) (this.bitFieldField & 0xff);
                    this.entryHeaderField[num2++] = (byte) ((this.bitFieldField & 0xff00) >> 8);
                    if ((this.Encryption == EncryptionAlgorithm.WinZipAes128) || (this.Encryption == EncryptionAlgorithm.WinZipAes256))
                    {
                        short num3 = (short) (this.entryHeaderField[0x1a] + (this.entryHeaderField[0x1b] * 0x100));
                        int offx = 30 + num3;
                        int num5 = FindExtraFieldSegment(this.entryHeaderField, offx, 0x9901);
                        if (num5 >= 0)
                        {
                            this.entryHeaderField[num5++] = 0x99;
                            this.entryHeaderField[num5++] = 0x99;
                        }
                    }
                }
                this.CompressionMethod = CompressionMethod.None;
                this.Encryption = EncryptionAlgorithm.None;
            }
            else if ((this.zipCryptoForWriteField != null) || (this.aesCryptoForWriteField != null))
            {
                if (this.Encryption == EncryptionAlgorithm.PkzipWeak)
                {
                    this.compressedSizeField += 12L;
                }
                else if ((this.Encryption == EncryptionAlgorithm.WinZipAes128) || (this.Encryption == EncryptionAlgorithm.WinZipAes256))
                {
                    this.compressedSizeField += this.aesCryptoForWriteField.SizeOfEncryptionMetadata;
                }
            }
            int destinationIndex = 8;
            this.entryHeaderField[destinationIndex++] = (byte) (this.compressionMethodField & 0xff);
            this.entryHeaderField[destinationIndex++] = (byte) ((this.compressionMethodField & 0xff00) >> 8);
            destinationIndex = 14;
            this.entryHeaderField[destinationIndex++] = (byte) (this.crc32Field & 0xff);
            this.entryHeaderField[destinationIndex++] = (byte) ((this.crc32Field & 0xff00) >> 8);
            this.entryHeaderField[destinationIndex++] = (byte) ((this.crc32Field & 0xff0000) >> 0x10);
            this.entryHeaderField[destinationIndex++] = (byte) ((this.crc32Field & 0xff000000L) >> 0x18);
            this.SetZip64Flags();
            short num7 = (short) (this.entryHeaderField[0x1a] + (this.entryHeaderField[0x1b] * 0x100));
            short num8 = (short) (this.entryHeaderField[0x1c] + (this.entryHeaderField[0x1d] * 0x100));
            if (this.outputUsesZip64Field.Value)
            {
                this.entryHeaderField[4] = 0x2d;
                this.entryHeaderField[5] = 0;
                for (num2 = 0; num2 < 8; num2++)
                {
                    this.entryHeaderField[destinationIndex++] = 0xff;
                }
                destinationIndex = 30 + num7;
                this.entryHeaderField[destinationIndex++] = 1;
                this.entryHeaderField[destinationIndex++] = 0;
                destinationIndex += 2;
                Array.Copy(BitConverter.GetBytes(this.uncompressedSizeField), 0, this.entryHeaderField, destinationIndex, 8);
                destinationIndex += 8;
                Array.Copy(BitConverter.GetBytes(this.compressedSizeField), 0, this.entryHeaderField, destinationIndex, 8);
            }
            else
            {
                this.entryHeaderField[4] = 20;
                this.entryHeaderField[5] = 0;
                destinationIndex = 0x12;
                this.entryHeaderField[destinationIndex++] = (byte) (this.compressedSizeField & 0xffL);
                this.entryHeaderField[destinationIndex++] = (byte) ((this.compressedSizeField & 0xff00L) >> 8);
                this.entryHeaderField[destinationIndex++] = (byte) ((this.compressedSizeField & 0xff0000L) >> 0x10);
                this.entryHeaderField[destinationIndex++] = (byte) ((this.compressedSizeField & 0xff000000L) >> 0x18);
                this.entryHeaderField[destinationIndex++] = (byte) (this.uncompressedSizeField & 0xffL);
                this.entryHeaderField[destinationIndex++] = (byte) ((this.uncompressedSizeField & 0xff00L) >> 8);
                this.entryHeaderField[destinationIndex++] = (byte) ((this.uncompressedSizeField & 0xff0000L) >> 0x10);
                this.entryHeaderField[destinationIndex++] = (byte) ((this.uncompressedSizeField & 0xff000000L) >> 0x18);
                if (num8 != 0)
                {
                    destinationIndex = 30 + num7;
                    num9 = (short) (this.entryHeaderField[destinationIndex + 2] + (this.entryHeaderField[destinationIndex + 3] * 0x100));
                    if (num9 == 0x10)
                    {
                        this.entryHeaderField[destinationIndex++] = 0x99;
                        this.entryHeaderField[destinationIndex++] = 0x99;
                    }
                }
            }
            if ((this.Encryption == EncryptionAlgorithm.WinZipAes128) || (this.Encryption == EncryptionAlgorithm.WinZipAes256))
            {
                destinationIndex = 8;
                this.entryHeaderField[destinationIndex++] = 0x63;
                this.entryHeaderField[destinationIndex++] = 0;
                destinationIndex = 30 + num7;
                do
                {
                    ushort num10 = (ushort) (this.entryHeaderField[destinationIndex] + (this.entryHeaderField[destinationIndex + 1] * 0x100));
                    num9 = (short) (this.entryHeaderField[destinationIndex + 2] + (this.entryHeaderField[destinationIndex + 3] * 0x100));
                    if (num10 != 0x9901)
                    {
                        destinationIndex += num9 + 4;
                    }
                    else
                    {
                        destinationIndex += 9;
                        this.entryHeaderField[destinationIndex++] = (byte) (this.compressionMethodField & 0xff);
                        this.entryHeaderField[destinationIndex++] = (byte) (this.compressionMethodField & 0xff00);
                    }
                }
                while (destinationIndex < ((num8 - 30) - num7));
            }
            if (((this.bitFieldField & 8) != 8) || ((this.sourceField == ZipEntrySource.ZipOutputStream) && s.CanSeek))
            {
                ZipSegmentedStream stream2 = s as ZipSegmentedStream;
                if ((stream2 != null) && (this.diskNumberField != stream2.CurrentSegment))
                {
                    using (Stream stream3 = ZipSegmentedStream.ForUpdate(this.containerField.ZipFile.Name, this.diskNumberField))
                    {
                        stream3.Seek(this.relativeOffsetOfLocalHeaderField, SeekOrigin.Begin);
                        stream3.Write(this.entryHeaderField, 0, this.entryHeaderField.Length);
                    }
                }
                else
                {
                    s.Seek(this.relativeOffsetOfLocalHeaderField, SeekOrigin.Begin);
                    s.Write(this.entryHeaderField, 0, this.entryHeaderField.Length);
                    if (stream != null)
                    {
                        stream.Adjust((long) this.entryHeaderField.Length);
                    }
                    s.Seek(this.compressedSizeField, SeekOrigin.Current);
                }
            }
            if (((this.bitFieldField & 8) == 8) && !this.IsDirectory)
            {
                byte[] destinationArray = new byte[0x10 + (this.outputUsesZip64Field.Value ? 8 : 0)];
                destinationIndex = 0;
                Array.Copy(BitConverter.GetBytes(0x8074b50), 0, destinationArray, destinationIndex, 4);
                destinationIndex += 4;
                Array.Copy(BitConverter.GetBytes(this.crc32Field), 0, destinationArray, destinationIndex, 4);
                destinationIndex += 4;
                if (this.outputUsesZip64Field.Value)
                {
                    Array.Copy(BitConverter.GetBytes(this.compressedSizeField), 0, destinationArray, destinationIndex, 8);
                    destinationIndex += 8;
                    Array.Copy(BitConverter.GetBytes(this.uncompressedSizeField), 0, destinationArray, destinationIndex, 8);
                    destinationIndex += 8;
                }
                else
                {
                    destinationArray[destinationIndex++] = (byte) (this.compressedSizeField & 0xffL);
                    destinationArray[destinationIndex++] = (byte) ((this.compressedSizeField & 0xff00L) >> 8);
                    destinationArray[destinationIndex++] = (byte) ((this.compressedSizeField & 0xff0000L) >> 0x10);
                    destinationArray[destinationIndex++] = (byte) ((this.compressedSizeField & 0xff000000L) >> 0x18);
                    destinationArray[destinationIndex++] = (byte) (this.uncompressedSizeField & 0xffL);
                    destinationArray[destinationIndex++] = (byte) ((this.uncompressedSizeField & 0xff00L) >> 8);
                    destinationArray[destinationIndex++] = (byte) ((this.uncompressedSizeField & 0xff0000L) >> 0x10);
                    destinationArray[destinationIndex++] = (byte) ((this.uncompressedSizeField & 0xff000000L) >> 0x18);
                }
                s.Write(destinationArray, 0, destinationArray.Length);
                this.lengthOfTrailerField += destinationArray.Length;
            }
        }

        /// <summary>
        /// Prepare the given stream for output - wrap it in a CountingStream, and
        /// then in a CRC stream, and an encryptor and deflator as appropriate.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Previously this was used in ZipEntry.Write(), but in an effort to
        /// introduce some efficiencies in that method I've refactored to put the
        /// code inline.  This method still gets called by ZipOutputStream.
        /// </para>
        /// </remarks>
        internal void PrepOutputStream(Stream s, long streamLength, out CountingStream outputCounter, out Stream encryptor, out Stream compressor, out CrcCalculatorStream output)
        {
            outputCounter = new CountingStream(s);
            if (streamLength != 0L)
            {
                encryptor = this.MaybeApplyEncryption(outputCounter);
                compressor = this.MaybeApplyCompression(encryptor, streamLength);
            }
            else
            {
                encryptor = compressor = outputCounter;
            }
            output = new CrcCalculatorStream(compressor, true);
        }

        /// <summary>
        /// Stores the position of the entry source stream, or, if the position is
        /// already stored, seeks to that position.
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// This method is called in prep for reading the source stream.  If PKZIP
        /// encryption is used, then we need to calc the CRC32 before doing the
        /// encryption, because the CRC is used in the 12th byte of the PKZIP
        /// encryption header.  So, we need to be able to seek backward in the source
        /// when saving the ZipEntry. This method is called from the place that
        /// calculates the CRC, and also from the method that does the encryption of
        /// the file data.
        /// </para>
        /// 
        /// <para>
        /// The first time through, this method sets the _sourceStreamOriginalPosition
        /// field. Subsequent calls to this method seek to that position.
        /// </para>
        /// </remarks>
        private void PrepSourceStream()
        {
            if (this.sourceStreamField == null)
            {
                throw new ZipException(string.Format(CultureInfo.InvariantCulture, "The input stream is null for entry '{0}'.", this.FileName));
            }
            if (this.sourceStreamOriginalPositionField.HasValue)
            {
                this.sourceStreamField.Position = this.sourceStreamOriginalPositionField.Value;
            }
            else if (this.sourceStreamField.CanSeek)
            {
                this.sourceStreamOriginalPositionField = new long?(this.sourceStreamField.Position);
            }
            else if ((this.Encryption == EncryptionAlgorithm.PkzipWeak) && ((this.sourceField != ZipEntrySource.ZipFile) && ((this.bitFieldField & 8) != 8)))
            {
                throw new ZipException("It is not possible to use PKZIP encryption on a non-seekable input stream");
            }
        }

        /// <summary>
        /// At current cursor position in the stream, read the extra
        /// field, and set the properties on the ZipEntry instance
        /// appropriately.  This can be called when processing the
        /// Extra field in the Central Directory, or in the local
        /// header.
        /// </summary>
        internal int ProcessExtraField(Stream s, short extraFieldLength)
        {
            int num = 0;
            if (extraFieldLength > 0)
            {
                int num4;
                short num6;
                byte[] buffer = this.extraField = new byte[extraFieldLength];
                num = s.Read(buffer, 0, buffer.Length);
                long posn = s.Position - num;
                for (int i = 0; (i + 3) < buffer.Length; i = (num4 + num6) + 4)
                {
                    num4 = i;
                    ushort num5 = (ushort) (buffer[i++] + (buffer[i++] * 0x100));
                    num6 = (short) (buffer[i++] + (buffer[i++] * 0x100));
                    ushort num8 = num5;
                    if (num8 <= 0x5455)
                    {
                        switch (num8)
                        {
                            case 0x17:
                                i = this.ProcessExtraFieldPkwareStrongEncryption(buffer, i);
                                break;

                            case 0x5455:
                                i = this.ProcessExtraFieldUnixTimes(buffer, i, num6, posn);
                                break;

                            case 1:
                                goto Label_0113;

                            case 10:
                                goto Label_00E5;
                        }
                    }
                    else if (num8 <= 0x7855)
                    {
                        switch (num8)
                        {
                            case 0x5855:
                                goto Label_0101;
                        }
                    }
                    else if ((num8 != 0x7875) && (num8 == 0x9901))
                    {
                        goto Label_0121;
                    }
                    continue;
                Label_00E5:
                    i = this.ProcessExtraFieldWindowsTimes(buffer, i, num6, posn);
                    continue;
                Label_0101:
                    i = this.ProcessExtraFieldInfoZipTimes(buffer, i, num6, posn);
                    continue;
                Label_0113:
                    i = this.ProcessExtraFieldZip64(buffer, i, num6, posn);
                    continue;
                Label_0121:
                    i = this.ProcessExtraFieldWinZipAes(buffer, i, num6, posn);
                }
            }
            return num;
        }

        private int ProcessExtraFieldInfoZipTimes(byte[] buffer, int j, short dataSize, long posn)
        {
            if ((dataSize != 12) && (dataSize != 8))
            {
                throw new BadReadException(string.Format(CultureInfo.InvariantCulture, "  Unexpected size (0x{0:X4}) for InfoZip v1 extra field at position 0x{1:X16}", dataSize, posn));
            }
            int num = BitConverter.ToInt32(buffer, j);
            this.mtimeField = unixEpochField.AddSeconds((double) num);
            j += 4;
            num = BitConverter.ToInt32(buffer, j);
            this.atimeField = unixEpochField.AddSeconds((double) num);
            j += 4;
            this.ctimeField = DateTime.UtcNow;
            this.ntfsTimesAreSetField = true;
            this.timestampField |= ZipEntryTimestamp.InfoZip1;
            return j;
        }

        private int ProcessExtraFieldPkwareStrongEncryption(byte[] Buffer, int j)
        {
            j += 2;
            this.unsupportedAlgorithmIdField = (ushort) (Buffer[j++] + (Buffer[j++] * 0x100));
            this.encryptionFromZipFileField = this.encryptionField = EncryptionAlgorithm.Unsupported;
            return j;
        }

        private int ProcessExtraFieldUnixTimes(byte[] buffer, int j, short dataSize, long posn)
        {
            if (((dataSize != 13) && (dataSize != 9)) && (dataSize != 5))
            {
                throw new BadReadException(string.Format(CultureInfo.InvariantCulture, "  Unexpected size (0x{0:X4}) for Extended Timestamp extra field at position 0x{1:X16}", dataSize, posn));
            }
            int remainingData = dataSize;
            Func<DateTime> func = delegate {
                int num = BitConverter.ToInt32(buffer, j);
                j += 4;
                remainingData -= 4;
                return unixEpochField.AddSeconds((double) num);
            };
            if ((dataSize == 13) || (this.readExtraDepthField > 0))
            {
                byte num = buffer[j++];
                remainingData--;
                if (((num & 1) != 0) && (remainingData >= 4))
                {
                    this.mtimeField = func();
                }
                this.atimeField = (((num & 2) != 0) && (remainingData >= 4)) ? func() : DateTime.UtcNow;
                this.ctimeField = (((num & 4) != 0) && (remainingData >= 4)) ? func() : DateTime.UtcNow;
                this.timestampField |= ZipEntryTimestamp.Unix;
                this.ntfsTimesAreSetField = true;
                this.emitUnixTimesField = true;
            }
            else
            {
                this.ReadExtraField();
            }
            return j;
        }

        private int ProcessExtraFieldWindowsTimes(byte[] buffer, int j, short dataSize, long posn)
        {
            if (dataSize != 0x20)
            {
                throw new BadReadException(string.Format(CultureInfo.InvariantCulture, "  Unexpected size (0x{0:X4}) for NTFS times extra field at position 0x{1:X16}", dataSize, posn));
            }
            j += 4;
            short num = (short) (buffer[j] + (buffer[j + 1] * 0x100));
            short num2 = (short) (buffer[j + 2] + (buffer[j + 3] * 0x100));
            j += 4;
            if ((num == 1) && (num2 == 0x18))
            {
                long fileTime = BitConverter.ToInt64(buffer, j);
                this.mtimeField = DateTime.FromFileTimeUtc(fileTime);
                j += 8;
                fileTime = BitConverter.ToInt64(buffer, j);
                this.atimeField = DateTime.FromFileTimeUtc(fileTime);
                j += 8;
                fileTime = BitConverter.ToInt64(buffer, j);
                this.ctimeField = DateTime.FromFileTimeUtc(fileTime);
                j += 8;
                this.ntfsTimesAreSetField = true;
                this.timestampField |= ZipEntryTimestamp.Windows;
                this.emitNtfsTimesField = true;
            }
            return j;
        }

        private int ProcessExtraFieldWinZipAes(byte[] buffer, int j, short dataSize, long posn)
        {
            if (this.compressionMethodField == 0x63)
            {
                if ((this.bitFieldField & 1) != 1)
                {
                    throw new BadReadException(string.Format(CultureInfo.InvariantCulture, "  Inconsistent metadata at position 0x{0:X16}", posn));
                }
                this.sourceIsEncryptedField = true;
                if (dataSize != 7)
                {
                    throw new BadReadException(string.Format(CultureInfo.InvariantCulture, "  Inconsistent size (0x{0:X4}) in WinZip AES field at position 0x{1:X16}", dataSize, posn));
                }
                this.winZipAesMethodField = BitConverter.ToInt16(buffer, j);
                j += 2;
                if ((this.winZipAesMethodField != 1) && (this.winZipAesMethodField != 2))
                {
                    throw new BadReadException(string.Format(CultureInfo.InvariantCulture, "  Unexpected vendor version number (0x{0:X4}) for WinZip AES metadata at position 0x{1:X16}", this.winZipAesMethodField, posn));
                }
                short num = BitConverter.ToInt16(buffer, j);
                j += 2;
                if (num != 0x4541)
                {
                    throw new BadReadException(string.Format(CultureInfo.InvariantCulture, "  Unexpected vendor ID (0x{0:X4}) for WinZip AES metadata at position 0x{1:X16}", num, posn));
                }
                int num2 = (buffer[j] == 1) ? 0x80 : ((buffer[j] == 3) ? 0x100 : -1);
                if (num2 < 0)
                {
                    throw new BadReadException(string.Format(CultureInfo.InvariantCulture, "Invalid key strength ({0})", num2));
                }
                this.encryptionFromZipFileField = this.encryptionField = (num2 == 0x80) ? EncryptionAlgorithm.WinZipAes128 : EncryptionAlgorithm.WinZipAes256;
                j++;
                this.compressionMethodFromZipFileField = this.compressionMethodField = BitConverter.ToInt16(buffer, j);
                j += 2;
            }
            return j;
        }

        private int ProcessExtraFieldZip64(byte[] buffer, int j, short dataSize, long posn)
        {
            this.inputUsesZip64Field = true;
            if (dataSize > 0x1c)
            {
                throw new BadReadException(string.Format(CultureInfo.InvariantCulture, "  Inconsistent size (0x{0:X4}) for ZIP64 extra field at position 0x{1:X16}", dataSize, posn));
            }
            int remainingData = dataSize;
            Func<long> func = delegate {
                if (remainingData < 8)
                {
                    throw new BadReadException(string.Format(CultureInfo.InvariantCulture, "  Missing data for ZIP64 extra field, position 0x{0:X16}", posn));
                }
                long num = BitConverter.ToInt64(buffer, j);
                j += 8;
                remainingData -= 8;
                return num;
            };
            if (this.uncompressedSizeField == 0xffffffffL)
            {
                this.uncompressedSizeField = func();
            }
            if (this.compressedSizeField == 0xffffffffL)
            {
                this.compressedSizeField = func();
            }
            if (this.relativeOffsetOfLocalHeaderField == 0xffffffffL)
            {
                this.relativeOffsetOfLocalHeaderField = func();
            }
            return j;
        }

        /// <summary>
        /// Reads one entry from the zip directory structure in the zip file.
        /// </summary>
        /// 
        /// <param name="zf">
        /// The zipfile for which a directory entry will be read.  From this param, the
        /// method gets the ReadStream and the expected text encoding
        /// (ProvisionalAlternateEncoding) which is used if the entry is not marked
        /// UTF-8.
        /// </param>
        /// 
        /// <param name="previouslySeen">
        /// a list of previously seen entry names; used to prevent duplicates.
        /// </param>
        /// 
        /// <returns>the entry read from the archive.</returns>
        internal static ZipEntry ReadDirEntry(ZipFile zf, Dictionary<string, object> previouslySeen)
        {
            Stream readStream = zf.ReadStream;
            Encoding encoding = (zf.AlternateEncodingUsage == ZipOption.Always) ? zf.AlternateEncoding : ZipFile.DefaultEncoding;
            int signature = SharedUtilities.ReadSignature(readStream);
            if (IsNotValidZipDirEntrySig(signature))
            {
                readStream.Seek(-4L, SeekOrigin.Current);
                if (((signature != 0x6054b50L) && (signature != 0x6064b50L)) && (signature != 0x4034b50))
                {
                    throw new BadReadException(string.Format(CultureInfo.InvariantCulture, "  Bad signature (0x{0:X8}) at position 0x{1:X8}", signature, readStream.Position));
                }
                return null;
            }
            int num2 = 0x2e;
            byte[] buffer = new byte[0x2a];
            if (readStream.Read(buffer, 0, buffer.Length) != buffer.Length)
            {
                return null;
            }
            int num4 = 0;
            ZipEntry entry = new ZipEntry();
            entry.AlternateEncoding = encoding;
            entry.sourceField = ZipEntrySource.ZipFile;
            entry.containerField = new ZipContainer(zf);
            entry.versionMadeByField = (short) (buffer[num4++] + (buffer[num4++] * 0x100));
            entry.versionNeededField = (short) (buffer[num4++] + (buffer[num4++] * 0x100));
            entry.bitFieldField = (short) (buffer[num4++] + (buffer[num4++] * 0x100));
            entry.compressionMethodField = (short) (buffer[num4++] + (buffer[num4++] * 0x100));
            entry.timeBlobField = ((buffer[num4++] + (buffer[num4++] * 0x100)) + ((buffer[num4++] * 0x100) * 0x100)) + (((buffer[num4++] * 0x100) * 0x100) * 0x100);
            entry.lastModifiedField = SharedUtilities.PackedToDateTime(entry.timeBlobField);
            entry.timestampField |= ZipEntryTimestamp.DOS;
            entry.crc32Field = ((buffer[num4++] + (buffer[num4++] * 0x100)) + ((buffer[num4++] * 0x100) * 0x100)) + (((buffer[num4++] * 0x100) * 0x100) * 0x100);
            entry.compressedSizeField = (long) ((ulong) (((buffer[num4++] + (buffer[num4++] * 0x100)) + ((buffer[num4++] * 0x100) * 0x100)) + (((buffer[num4++] * 0x100) * 0x100) * 0x100)));
            entry.uncompressedSizeField = (long) ((ulong) (((buffer[num4++] + (buffer[num4++] * 0x100)) + ((buffer[num4++] * 0x100) * 0x100)) + (((buffer[num4++] * 0x100) * 0x100) * 0x100)));
            entry.compressionMethodFromZipFileField = entry.compressionMethodField;
            entry.filenameLengthField = (short) (buffer[num4++] + (buffer[num4++] * 0x100));
            entry.extraFieldLengthField = (short) (buffer[num4++] + (buffer[num4++] * 0x100));
            entry.commentLengthField = (short) (buffer[num4++] + (buffer[num4++] * 0x100));
            entry.diskNumberField = (uint) (buffer[num4++] + (buffer[num4++] * 0x100));
            entry.internalFileAttrsField = (short) (buffer[num4++] + (buffer[num4++] * 0x100));
            entry.externalFileAttrsField = ((buffer[num4++] + (buffer[num4++] * 0x100)) + ((buffer[num4++] * 0x100) * 0x100)) + (((buffer[num4++] * 0x100) * 0x100) * 0x100);
            entry.relativeOffsetOfLocalHeaderField = (long) ((ulong) (((buffer[num4++] + (buffer[num4++] * 0x100)) + ((buffer[num4++] * 0x100) * 0x100)) + (((buffer[num4++] * 0x100) * 0x100) * 0x100)));
            entry.IsText = (entry.internalFileAttrsField & 1) == 1;
            buffer = new byte[entry.filenameLengthField];
            int num3 = readStream.Read(buffer, 0, buffer.Length);
            num2 += num3;
            if ((entry.bitFieldField & 0x800) == 0x800)
            {
                entry.fileNameInArchiveField = SharedUtilities.Utf8StringFromBuffer(buffer);
            }
            else
            {
                entry.fileNameInArchiveField = SharedUtilities.StringFromBuffer(buffer, encoding);
            }
            while (previouslySeen.ContainsKey(entry.fileNameInArchiveField))
            {
                entry.fileNameInArchiveField = CopyHelper.AppendCopyToFileName(entry.fileNameInArchiveField);
                entry.metadataChangedField = true;
            }
            if (entry.AttributesIndicateDirectory)
            {
                entry.MarkAsDirectory();
            }
            else if (entry.fileNameInArchiveField.EndsWith("/"))
            {
                entry.MarkAsDirectory();
            }
            entry.compressedFileDataSizeField = entry.compressedSizeField;
            if ((entry.bitFieldField & 1) == 1)
            {
                entry.encryptionFromZipFileField = entry.encryptionField = EncryptionAlgorithm.PkzipWeak;
                entry.sourceIsEncryptedField = true;
            }
            if (entry.extraFieldLengthField > 0)
            {
                entry.inputUsesZip64Field = ((entry.compressedSizeField == 0xffffffffL) || (entry.uncompressedSizeField == 0xffffffffL)) || (entry.relativeOffsetOfLocalHeaderField == 0xffffffffL);
                num2 += entry.ProcessExtraField(readStream, entry.extraFieldLengthField);
                entry.compressedFileDataSizeField = entry.compressedSizeField;
            }
            if (entry.encryptionField == EncryptionAlgorithm.PkzipWeak)
            {
                entry.compressedFileDataSizeField -= 12L;
            }
            else if ((entry.Encryption == EncryptionAlgorithm.WinZipAes128) || (entry.Encryption == EncryptionAlgorithm.WinZipAes256))
            {
                entry.compressedFileDataSizeField = entry.CompressedSize - (GetLengthOfCryptoHeaderBytes(entry.Encryption) + 10);
                entry.lengthOfTrailerField = 10;
            }
            if ((entry.bitFieldField & 8) == 8)
            {
                if (entry.inputUsesZip64Field)
                {
                    entry.lengthOfTrailerField += 0x18;
                }
                else
                {
                    entry.lengthOfTrailerField += 0x10;
                }
            }
            entry.AlternateEncoding = ((entry.bitFieldField & 0x800) == 0x800) ? Encoding.UTF8 : encoding;
            entry.AlternateEncodingUsage = ZipOption.Always;
            if (entry.commentLengthField > 0)
            {
                buffer = new byte[entry.commentLengthField];
                num3 = readStream.Read(buffer, 0, buffer.Length);
                num2 += num3;
                if ((entry.bitFieldField & 0x800) == 0x800)
                {
                    entry.commentField = SharedUtilities.Utf8StringFromBuffer(buffer);
                }
                else
                {
                    entry.commentField = SharedUtilities.StringFromBuffer(buffer, encoding);
                }
            }
            return entry;
        }

        /// <summary>
        /// Reads one <c>ZipEntry</c> from the given stream.  The content for
        /// the entry does not get decompressed or decrypted.  This method
        /// basically reads metadata, and seeks.
        /// </summary>
        /// <param name="zc">the ZipContainer this entry belongs to.</param>
        /// <param name="first">
        /// true of this is the first entry being read from the stream.
        /// </param>
        /// <returns>the <c>ZipEntry</c> read from the stream.</returns>
        internal static ZipEntry ReadEntry(ZipContainer zc, bool first)
        {
            ZipFile zipFile = zc.ZipFile;
            Stream readStream = zc.ReadStream;
            Encoding alternateEncoding = zc.AlternateEncoding;
            ZipEntry ze = new ZipEntry();
            ze.sourceField = ZipEntrySource.ZipFile;
            ze.containerField = zc;
            ze.archiveStreamField = readStream;
            if (zipFile != null)
            {
                zipFile.OnReadEntry(true, null);
            }
            if (first)
            {
                HandlePK00Prefix(readStream);
            }
            if (!ReadHeader(ze, alternateEncoding))
            {
                return null;
            }
            ze.fileDataPositionField = ze.ArchiveStream.Position;
            readStream.Seek(ze.compressedFileDataSizeField + ze.lengthOfTrailerField, SeekOrigin.Current);
            HandleUnexpectedDataDescriptor(ze);
            if (zipFile != null)
            {
                zipFile.OnReadBytes(ze);
                zipFile.OnReadEntry(false, ze);
            }
            return ze;
        }

        private void ReadExtraField()
        {
            this.readExtraDepthField++;
            long position = this.ArchiveStream.Position;
            this.ArchiveStream.Seek(this.relativeOffsetOfLocalHeaderField, SeekOrigin.Begin);
            byte[] buffer = new byte[30];
            this.ArchiveStream.Read(buffer, 0, buffer.Length);
            int num2 = 0x1a;
            short num3 = (short) (buffer[num2++] + (buffer[num2++] * 0x100));
            short extraFieldLength = (short) (buffer[num2++] + (buffer[num2++] * 0x100));
            this.ArchiveStream.Seek((long) num3, SeekOrigin.Current);
            this.ProcessExtraField(this.ArchiveStream, extraFieldLength);
            this.ArchiveStream.Seek(position, SeekOrigin.Begin);
            this.readExtraDepthField--;
        }

        private static bool ReadHeader(ZipEntry ze, Encoding defaultEncoding)
        {
            int num = 0;
            ze.relativeOffsetOfLocalHeaderField = ze.ArchiveStream.Position;
            int signature = SharedUtilities.ReadEntrySignature(ze.ArchiveStream);
            num += 4;
            if (IsNotValidSig(signature))
            {
                ze.ArchiveStream.Seek(-4L, SeekOrigin.Current);
                if (IsNotValidZipDirEntrySig(signature) && (signature != 0x6054b50L))
                {
                    throw new BadReadException(string.Format(CultureInfo.InvariantCulture, "  Bad signature (0x{0:X8}) at position  0x{1:X8}", signature, ze.ArchiveStream.Position));
                }
                return false;
            }
            byte[] buffer = new byte[0x1a];
            int num3 = ze.ArchiveStream.Read(buffer, 0, buffer.Length);
            if (num3 != buffer.Length)
            {
                return false;
            }
            num += num3;
            int startIndex = 0;
            ze.versionNeededField = (short) (buffer[startIndex++] + (buffer[startIndex++] * 0x100));
            ze.bitFieldField = (short) (buffer[startIndex++] + (buffer[startIndex++] * 0x100));
            ze.compressionMethodFromZipFileField = ze.compressionMethodField = (short) (buffer[startIndex++] + (buffer[startIndex++] * 0x100));
            ze.timeBlobField = ((buffer[startIndex++] + (buffer[startIndex++] * 0x100)) + ((buffer[startIndex++] * 0x100) * 0x100)) + (((buffer[startIndex++] * 0x100) * 0x100) * 0x100);
            ze.lastModifiedField = SharedUtilities.PackedToDateTime(ze.timeBlobField);
            ze.timestampField |= ZipEntryTimestamp.DOS;
            if ((ze.bitFieldField & 1) == 1)
            {
                ze.encryptionFromZipFileField = ze.encryptionField = EncryptionAlgorithm.PkzipWeak;
                ze.sourceIsEncryptedField = true;
            }
            ze.crc32Field = ((buffer[startIndex++] + (buffer[startIndex++] * 0x100)) + ((buffer[startIndex++] * 0x100) * 0x100)) + (((buffer[startIndex++] * 0x100) * 0x100) * 0x100);
            ze.compressedSizeField = (long) ((ulong) (((buffer[startIndex++] + (buffer[startIndex++] * 0x100)) + ((buffer[startIndex++] * 0x100) * 0x100)) + (((buffer[startIndex++] * 0x100) * 0x100) * 0x100)));
            ze.uncompressedSizeField = (long) ((ulong) (((buffer[startIndex++] + (buffer[startIndex++] * 0x100)) + ((buffer[startIndex++] * 0x100) * 0x100)) + (((buffer[startIndex++] * 0x100) * 0x100) * 0x100)));
            if ((((uint) ze.compressedSizeField) == uint.MaxValue) || (((uint) ze.uncompressedSizeField) == uint.MaxValue))
            {
                ze.inputUsesZip64Field = true;
            }
            short num5 = (short) (buffer[startIndex++] + (buffer[startIndex++] * 0x100));
            short extraFieldLength = (short) (buffer[startIndex++] + (buffer[startIndex++] * 0x100));
            buffer = new byte[num5];
            num3 = ze.ArchiveStream.Read(buffer, 0, buffer.Length);
            num += num3;
            if ((ze.bitFieldField & 0x800) == 0x800)
            {
                ze.AlternateEncoding = Encoding.UTF8;
                ze.AlternateEncodingUsage = ZipOption.Always;
            }
            ze.fileNameInArchiveField = ze.AlternateEncoding.GetString(buffer, 0, buffer.Length);
            if (ze.fileNameInArchiveField.EndsWith("/"))
            {
                ze.MarkAsDirectory();
            }
            num += ze.ProcessExtraField(ze.ArchiveStream, extraFieldLength);
            ze.lengthOfTrailerField = 0;
            if (!ze.fileNameInArchiveField.EndsWith("/") && ((ze.bitFieldField & 8) == 8))
            {
                long position = ze.ArchiveStream.Position;
                bool flag = true;
                long num8 = 0L;
                int num9 = 0;
                while (flag)
                {
                    num9++;
                    if (ze.containerField.ZipFile != null)
                    {
                        ze.containerField.ZipFile.OnReadBytes(ze);
                    }
                    long num10 = SharedUtilities.FindSignature(ze.ArchiveStream, 0x8074b50);
                    if (num10 == -1L)
                    {
                        return false;
                    }
                    num8 += num10;
                    if (ze.inputUsesZip64Field)
                    {
                        buffer = new byte[20];
                        if (ze.ArchiveStream.Read(buffer, 0, buffer.Length) != 20)
                        {
                            return false;
                        }
                        startIndex = 0;
                        ze.crc32Field = ((buffer[startIndex++] + (buffer[startIndex++] * 0x100)) + ((buffer[startIndex++] * 0x100) * 0x100)) + (((buffer[startIndex++] * 0x100) * 0x100) * 0x100);
                        ze.compressedSizeField = BitConverter.ToInt64(buffer, startIndex);
                        startIndex += 8;
                        ze.uncompressedSizeField = BitConverter.ToInt64(buffer, startIndex);
                        startIndex += 8;
                        ze.lengthOfTrailerField += 0x18;
                    }
                    else
                    {
                        buffer = new byte[12];
                        if (ze.ArchiveStream.Read(buffer, 0, buffer.Length) != 12)
                        {
                            return false;
                        }
                        startIndex = 0;
                        ze.crc32Field = ((buffer[startIndex++] + (buffer[startIndex++] * 0x100)) + ((buffer[startIndex++] * 0x100) * 0x100)) + (((buffer[startIndex++] * 0x100) * 0x100) * 0x100);
                        ze.compressedSizeField = (long) ((ulong) (((buffer[startIndex++] + (buffer[startIndex++] * 0x100)) + ((buffer[startIndex++] * 0x100) * 0x100)) + (((buffer[startIndex++] * 0x100) * 0x100) * 0x100)));
                        ze.uncompressedSizeField = (long) ((ulong) (((buffer[startIndex++] + (buffer[startIndex++] * 0x100)) + ((buffer[startIndex++] * 0x100) * 0x100)) + (((buffer[startIndex++] * 0x100) * 0x100) * 0x100)));
                        ze.lengthOfTrailerField += 0x10;
                    }
                    if (num8 != ze.compressedSizeField)
                    {
                        ze.ArchiveStream.Seek(-12L, SeekOrigin.Current);
                        num8 += 4L;
                    }
                }
                ze.ArchiveStream.Seek(position, SeekOrigin.Begin);
            }
            ze.compressedFileDataSizeField = ze.compressedSizeField;
            if ((ze.bitFieldField & 1) == 1)
            {
                if ((ze.Encryption == EncryptionAlgorithm.WinZipAes128) || (ze.Encryption == EncryptionAlgorithm.WinZipAes256))
                {
                    int keyStrengthInBits = GetKeyStrengthInBits(ze.encryptionFromZipFileField);
                    ze.aesCryptoForExtractField = WinZipAesCrypto.ReadFromStream(null, keyStrengthInBits, ze.ArchiveStream);
                    num += ze.aesCryptoForExtractField.SizeOfEncryptionMetadata - 10;
                    ze.compressedFileDataSizeField -= ze.aesCryptoForExtractField.SizeOfEncryptionMetadata;
                    ze.lengthOfTrailerField += 10;
                }
                else
                {
                    ze.weakEncryptionHeaderField = new byte[12];
                    num += ReadWeakEncryptionHeader(ze.archiveStreamField, ze.weakEncryptionHeaderField);
                    ze.compressedFileDataSizeField -= 12L;
                }
            }
            ze.lengthOfHeaderField = num;
            ze.totalEntrySizeField = (ze.lengthOfHeaderField + ze.compressedFileDataSizeField) + ze.lengthOfTrailerField;
            return true;
        }

        internal static int ReadWeakEncryptionHeader(Stream s, byte[] buffer)
        {
            int num = s.Read(buffer, 0, 12);
            if (num != 12)
            {
                throw new ZipException(string.Format(CultureInfo.InvariantCulture, "Unexpected end of data at position 0x{0:X8}", s.Position));
            }
            return num;
        }

        private static void ReallyDelete(string fileName)
        {
            if ((File.GetAttributes(fileName) & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
            {
                File.SetAttributes(fileName, FileAttributes.Normal);
            }
            File.Delete(fileName);
        }

        internal void ResetDirEntry()
        {
            this.fileDataPositionField = -1L;
            this.lengthOfHeaderField = 0;
        }

        /// <summary>
        /// Sets the NTFS Creation, Access, and Modified times for the given entry.
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// When adding an entry from a file or directory, the Creation, Access, and
        /// Modified times for the given entry are automatically set from the
        /// filesystem values. When adding an entry from a stream or string, the
        /// values are implicitly set to DateTime.Now.  The application may wish to
        /// set these values to some arbitrary value, before saving the archive, and
        /// can do so using the various setters.  If you want to set all of the times,
        /// this method is more efficient.
        /// </para>
        /// 
        /// <para>
        /// The values you set here will be retrievable with the <see cref="P:ZipEntry.ModifiedTime" />, <see cref="P:ZipEntry.CreationTime" /> and <see cref="P:ZipEntry.AccessedTime" /> properties.
        /// </para>
        /// 
        /// <para>
        /// When this method is called, if both <see cref="P:ZipEntry.EmitTimesInWindowsFormatWhenSaving" /> and <see cref="P:ZipEntry.EmitTimesInUnixFormatWhenSaving" /> are false, then the
        /// <c>EmitTimesInWindowsFormatWhenSaving</c> flag is automatically set.
        /// </para>
        /// 
        /// <para>
        /// DateTime values provided here without a DateTimeKind are assumed to be Local Time.
        /// </para>
        /// 
        /// </remarks>
        /// <param name="created">the creation time of the entry.</param>
        /// <param name="accessed">the last access time of the entry.</param>
        /// <param name="modified">the last modified time of the entry.</param>
        /// 
        /// <seealso cref="P:ZipEntry.EmitTimesInWindowsFormatWhenSaving" />
        /// <seealso cref="P:ZipEntry.EmitTimesInUnixFormatWhenSaving" />
        /// <seealso cref="P:ZipEntry.AccessedTime" />
        /// <seealso cref="P:ZipEntry.CreationTime" />
        /// <seealso cref="P:ZipEntry.ModifiedTime" />
        public void SetEntryTimes(DateTime created, DateTime accessed, DateTime modified)
        {
            this.ntfsTimesAreSetField = true;
            if ((created == zeroHourField) && (created.Kind == zeroHourField.Kind))
            {
                created = win32EpochField;
            }
            if ((accessed == zeroHourField) && (accessed.Kind == zeroHourField.Kind))
            {
                accessed = win32EpochField;
            }
            if ((modified == zeroHourField) && (modified.Kind == zeroHourField.Kind))
            {
                modified = win32EpochField;
            }
            this.ctimeField = created.ToUniversalTime();
            this.atimeField = accessed.ToUniversalTime();
            this.mtimeField = modified.ToUniversalTime();
            this.lastModifiedField = this.mtimeField;
            if (!(this.emitUnixTimesField || this.emitNtfsTimesField))
            {
                this.emitNtfsTimesField = true;
            }
            this.metadataChangedField = true;
        }

        private void SetFdpLoh()
        {
            long position = this.ArchiveStream.Position;
            try
            {
                this.ArchiveStream.Seek(this.relativeOffsetOfLocalHeaderField, SeekOrigin.Begin);
            }
            catch (IOException exception)
            {
                throw new BadStateException(string.Format(CultureInfo.InvariantCulture, "Exception seeking  entry({0}) offset(0x{1:X8}) len(0x{2:X8})", this.FileName, this.relativeOffsetOfLocalHeaderField, this.ArchiveStream.Length), exception);
            }
            byte[] buffer = new byte[30];
            this.ArchiveStream.Read(buffer, 0, buffer.Length);
            short num2 = (short) (buffer[0x1a] + (buffer[0x1b] * 0x100));
            short num3 = (short) (buffer[0x1c] + (buffer[0x1d] * 0x100));
            this.ArchiveStream.Seek((long) (num2 + num3), SeekOrigin.Current);
            this.lengthOfHeaderField = ((30 + num3) + num2) + GetLengthOfCryptoHeaderBytes(this.encryptionFromZipFileField);
            this.fileDataPositionField = this.relativeOffsetOfLocalHeaderField + this.lengthOfHeaderField;
            this.ArchiveStream.Seek(position, SeekOrigin.Begin);
        }

        /// <summary>
        /// Set the input stream and get its length, if possible.  The length is
        /// used for progress updates, AND, to allow an optimization in case of
        /// a stream/file of zero length. In that case we skip the Encrypt and
        /// compression Stream. (like DeflateStream or BZip2OutputStream)
        /// </summary>
        private long SetInputAndFigureFileLength(ref Stream input)
        {
            long length = -1L;
            if (this.sourceField == ZipEntrySource.Stream)
            {
                this.PrepSourceStream();
                input = this.sourceStreamField;
                try
                {
                    length = this.sourceStreamField.Length;
                }
                catch (NotSupportedException)
                {
                }
                return length;
            }
            if (this.sourceField == ZipEntrySource.ZipFile)
            {
                string password = (this.encryptionFromZipFileField == EncryptionAlgorithm.None) ? null : (this.passwordField ?? this.containerField.Password);
                this.sourceStreamField = this.InternalOpenReader(password);
                this.PrepSourceStream();
                input = this.sourceStreamField;
                return this.sourceStreamField.Length;
            }
            if (this.sourceField == ZipEntrySource.JitStream)
            {
                if (this.sourceStreamField == null)
                {
                    this.sourceStreamField = this.openDelegateField(this.FileName);
                }
                this.PrepSourceStream();
                input = this.sourceStreamField;
                try
                {
                    length = this.sourceStreamField.Length;
                }
                catch (NotSupportedException)
                {
                }
                return length;
            }
            if (this.sourceField == ZipEntrySource.FileSystem)
            {
                FileShare readWrite = FileShare.ReadWrite;
                readWrite |= FileShare.Delete;
                input = File.Open(this.LocalFileName, FileMode.Open, FileAccess.Read, readWrite);
                length = input.Length;
            }
            return length;
        }

        private void SetupCryptoForExtract(string password)
        {
            if (this.encryptionFromZipFileField != EncryptionAlgorithm.None)
            {
                if (this.encryptionFromZipFileField == EncryptionAlgorithm.PkzipWeak)
                {
                    if (password == null)
                    {
                        throw new ZipException("Missing password.");
                    }
                    this.ArchiveStream.Seek(this.FileDataPosition - 12L, SeekOrigin.Begin);
                    this.zipCryptoForExtractField = ZipCrypto.ForRead(password, this);
                }
                else if ((this.encryptionFromZipFileField == EncryptionAlgorithm.WinZipAes128) || (this.encryptionFromZipFileField == EncryptionAlgorithm.WinZipAes256))
                {
                    if (password == null)
                    {
                        throw new ZipException("Missing password.");
                    }
                    if (this.aesCryptoForExtractField != null)
                    {
                        this.aesCryptoForExtractField.Password = password;
                    }
                    else
                    {
                        int lengthOfCryptoHeaderBytes = GetLengthOfCryptoHeaderBytes(this.encryptionFromZipFileField);
                        this.ArchiveStream.Seek(this.FileDataPosition - lengthOfCryptoHeaderBytes, SeekOrigin.Begin);
                        int keyStrengthInBits = GetKeyStrengthInBits(this.encryptionFromZipFileField);
                        this.aesCryptoForExtractField = WinZipAesCrypto.ReadFromStream(password, keyStrengthInBits, this.ArchiveStream);
                    }
                }
            }
        }

        private void SetZip64Flags()
        {
            this.entryRequiresZip64Field = new bool?(((this.compressedSizeField >= 0xffffffffL) || (this.uncompressedSizeField >= 0xffffffffL)) || (this.relativeOffsetOfLocalHeaderField >= 0xffffffffL));
            if (!((this.containerField.Zip64 != Zip64Option.Default) ? true : !this.entryRequiresZip64Field.Value))
            {
                throw new ZipException("Compressed or Uncompressed size, or offset exceeds the maximum value. Consider setting the UseZip64WhenSaving property on the ZipFile instance.");
            }
            this.outputUsesZip64Field = new bool?((this.containerField.Zip64 == Zip64Option.Always) ? true : this.entryRequiresZip64Field.Value);
        }

        internal void StoreRelativeOffset()
        {
            this.relativeOffsetOfLocalHeaderField = this.futureRolhField;
        }

        /// <summary>Provides a string representation of the instance.</summary>
        /// <returns>a string representation of the instance.</returns>
        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture, "ZipEntry::{0}", this.FileName);
        }

        [Conditional("Trace")]
        private void TraceWriteLine(string format, params object[] varParams)
        {
            lock (this.outputLockField)
            {
                int hashCode = Thread.CurrentThread.GetHashCode();
                Console.ForegroundColor = (ConsoleColor) ((hashCode % 8) + 8);
                Console.Write("{0:000} ZipEntry.Write ", hashCode);
                Console.WriteLine(format, varParams);
                Console.ResetColor();
            }
        }

        private void ValidateCompression()
        {
            if (((this.compressionMethodFromZipFileField != 0) && (this.compressionMethodFromZipFileField != 8)) && (this.compressionMethodFromZipFileField != 12))
            {
                throw new ZipException(string.Format(CultureInfo.InvariantCulture, "Entry {0} uses an unsupported compression method (0x{1:X2}, {2})", this.FileName, this.compressionMethodFromZipFileField, this.UnsupportedCompressionMethod));
            }
        }

        internal void ValidateEncryption()
        {
            if ((((this.Encryption != EncryptionAlgorithm.PkzipWeak) && (this.Encryption != EncryptionAlgorithm.WinZipAes128)) && (this.Encryption != EncryptionAlgorithm.WinZipAes256)) && (this.Encryption != EncryptionAlgorithm.None))
            {
                if (this.unsupportedAlgorithmIdField != 0)
                {
                    throw new ZipException(string.Format(CultureInfo.InvariantCulture, "Cannot extract: Entry {0} is encrypted with an algorithm not supported by DotNetZip: {1}", this.FileName, this.UnsupportedAlgorithm));
                }
                throw new ZipException(string.Format(CultureInfo.InvariantCulture, "Cannot extract: Entry {0} uses an unsupported encryption algorithm ({1:X2})", this.FileName, (int) this.Encryption));
            }
        }

        /// <summary>
        /// Validates that the args are consistent.
        /// </summary>
        /// <remarks>
        /// Only one of {baseDir, outStream} can be non-null.
        /// If baseDir is non-null, then the outputFile is created.
        /// </remarks>
        private bool ValidateOutput(string basedir, Stream outstream, out string outFileName)
        {
            if (basedir != null)
            {
                string path = this.FileName.Replace(@"\", "/");
                if (path.IndexOf(':') == 1)
                {
                    path = path.Substring(2);
                }
                if (path.StartsWith("/"))
                {
                    path = path.Substring(1);
                }
                if (this.containerField.ZipFile.FlattenFoldersOnExtract)
                {
                    outFileName = Path.Combine(basedir, (path.IndexOf('/') != -1) ? Path.GetFileName(path) : path);
                }
                else
                {
                    outFileName = Path.Combine(basedir, path);
                }
                outFileName = outFileName.Replace("/", @"\");
                if (!this.IsDirectory && !this.FileName.EndsWith("/"))
                {
                    return false;
                }
                if (!Directory.Exists(outFileName))
                {
                    Directory.CreateDirectory(outFileName);
                    this._SetTimes(outFileName, false);
                }
                else if (this.ExtractExistingFile == ExtractExistingFileAction.OverwriteSilently)
                {
                    this._SetTimes(outFileName, false);
                }
                return true;
            }
            if (outstream == null)
            {
                throw new ArgumentNullException("outstream");
            }
            outFileName = null;
            return (this.IsDirectory || this.FileName.EndsWith("/"));
        }

        internal void VerifyCrcAfterExtract(int actualCrc32)
        {
            if ((actualCrc32 != this.crc32Field) && (((this.Encryption != EncryptionAlgorithm.WinZipAes128) && (this.Encryption != EncryptionAlgorithm.WinZipAes256)) || (this.winZipAesMethodField != 2)))
            {
                throw new BadCrcException("CRC error: the file being extracted appears to be corrupted. " + string.Format(CultureInfo.InvariantCulture, "Expected 0x{0:X8}, Actual 0x{1:X8}", this.crc32Field, actualCrc32));
            }
            if ((this.UncompressedSize != 0L) && ((this.Encryption == EncryptionAlgorithm.WinZipAes128) || (this.Encryption == EncryptionAlgorithm.WinZipAes256)))
            {
                WinZipAesCipherStream stream = this.inputDecryptorStreamField as WinZipAesCipherStream;
                this.aesCryptoForExtractField.calculatedMacField = stream.FinalAuthentication;
                this.aesCryptoForExtractField.ReadAndVerifyMac(this.ArchiveStream);
            }
        }

        private bool WantReadAgain()
        {
            if (this.uncompressedSizeField < 0x10L)
            {
                return false;
            }
            if (this.compressionMethodField == 0)
            {
                return false;
            }
            if (this.CompressionLevel == CompressionLevel.None)
            {
                return false;
            }
            if (this.compressedSizeField < this.uncompressedSizeField)
            {
                return false;
            }
            if (!((this.sourceField != ZipEntrySource.Stream) || this.sourceStreamField.CanSeek))
            {
                return false;
            }
            if ((this.aesCryptoForWriteField != null) && ((this.CompressedSize - this.aesCryptoForWriteField.SizeOfEncryptionMetadata) <= (this.UncompressedSize + 0x10L)))
            {
                return false;
            }
            if ((this.zipCryptoForWriteField != null) && ((this.CompressedSize - 12L) <= this.UncompressedSize))
            {
                return false;
            }
            return true;
        }

        internal void Write(Stream s)
        {
            CountingStream stream = s as CountingStream;
            ZipSegmentedStream stream2 = s as ZipSegmentedStream;
            bool flag = false;
        Label_0011:;
            try
            {
                if (!((this.sourceField != ZipEntrySource.ZipFile) || this.restreamRequiredOnSaveField))
                {
                    this.CopyThroughOneEntry(s);
                    return;
                }
                if (this.IsDirectory)
                {
                    this.WriteHeader(s, 1);
                    this.StoreRelativeOffset();
                    this.entryRequiresZip64Field = new bool?(this.relativeOffsetOfLocalHeaderField >= 0xffffffffL);
                    this.outputUsesZip64Field = new bool?((this.containerField.Zip64 == Zip64Option.Always) ? true : this.entryRequiresZip64Field.Value);
                    if (stream2 != null)
                    {
                        this.diskNumberField = stream2.CurrentSegment;
                    }
                    return;
                }
                bool flag2 = true;
                int cycle = 0;
                do
                {
                    cycle++;
                    this.WriteHeader(s, cycle);
                    this.WriteSecurityMetadata(s);
                    this._WriteEntryData(s);
                    this.totalEntrySizeField = (this.lengthOfHeaderField + this.compressedFileDataSizeField) + this.lengthOfTrailerField;
                    if (cycle > 1)
                    {
                        flag2 = false;
                    }
                    else if (!s.CanSeek)
                    {
                        flag2 = false;
                    }
                    else
                    {
                        flag2 = this.WantReadAgain();
                    }
                    if (flag2)
                    {
                        if (stream2 != null)
                        {
                            stream2.TruncateBackward(this.diskNumberField, this.relativeOffsetOfLocalHeaderField);
                        }
                        else
                        {
                            s.Seek(this.relativeOffsetOfLocalHeaderField, SeekOrigin.Begin);
                        }
                        s.SetLength(s.Position);
                        if (stream != null)
                        {
                            stream.Adjust(this.totalEntrySizeField);
                        }
                    }
                }
                while (flag2);
                this.skippedDuringSaveField = false;
                flag = true;
            }
            catch (Exception exception)
            {
                ZipErrorAction zipErrorAction = this.ZipErrorAction;
                int num2 = 0;
            Label_01A5:
                if (this.ZipErrorAction == ZipErrorAction.Throw)
                {
                    throw;
                }
                if ((this.ZipErrorAction == ZipErrorAction.Skip) || (this.ZipErrorAction == ZipErrorAction.Retry))
                {
                    long num3 = (stream != null) ? stream.ComputedPosition : s.Position;
                    long offset = num3 - this.futureRolhField;
                    if (offset > 0L)
                    {
                        s.Seek(offset, SeekOrigin.Current);
                        long position = s.Position;
                        s.SetLength(s.Position);
                        if (stream != null)
                        {
                            stream.Adjust(num3 - position);
                        }
                    }
                    if (this.ZipErrorAction == ZipErrorAction.Skip)
                    {
                        this.WriteStatus("Skipping file {0} (exception: {1})", new object[] { this.LocalFileName, exception.ToString() });
                        this.skippedDuringSaveField = true;
                        flag = true;
                    }
                    else
                    {
                        this.ZipErrorAction = zipErrorAction;
                    }
                }
                else
                {
                    if (num2 > 0)
                    {
                        throw;
                    }
                    if (this.ZipErrorAction == ZipErrorAction.InvokeErrorEvent)
                    {
                        this.OnZipErrorWhileSaving(exception);
                        if (this.ioOperationCanceledField)
                        {
                            flag = true;
                            goto Label_02E8;
                        }
                    }
                    num2++;
                    goto Label_01A5;
                }
            }
        Label_02E8:
            if (!flag)
            {
                goto Label_0011;
            }
        }

        internal void WriteCentralDirectoryEntry(Stream s)
        {
            byte[] dst = new byte[0x1000];
            int dstOffset = 0;
            dst[dstOffset++] = 80;
            dst[dstOffset++] = 0x4b;
            dst[dstOffset++] = 1;
            dst[dstOffset++] = 2;
            dst[dstOffset++] = (byte) (this.versionMadeByField & 0xff);
            dst[dstOffset++] = (byte) ((this.versionMadeByField & 0xff00) >> 8);
            short num2 = (this.VersionNeeded != 0) ? this.VersionNeeded : ((short) 20);
            if (!this.outputUsesZip64Field.HasValue)
            {
                this.outputUsesZip64Field = new bool?(this.containerField.Zip64 == Zip64Option.Always);
            }
            short num3 = this.outputUsesZip64Field.Value ? ((short) 0x2d) : num2;
            if (this.CompressionMethod == CompressionMethod.BZip2)
            {
                num3 = 0x2e;
            }
            dst[dstOffset++] = (byte) (num3 & 0xff);
            dst[dstOffset++] = (byte) ((num3 & 0xff00) >> 8);
            dst[dstOffset++] = (byte) (this.bitFieldField & 0xff);
            dst[dstOffset++] = (byte) ((this.bitFieldField & 0xff00) >> 8);
            dst[dstOffset++] = (byte) (this.compressionMethodField & 0xff);
            dst[dstOffset++] = (byte) ((this.compressionMethodField & 0xff00) >> 8);
            if ((this.Encryption == EncryptionAlgorithm.WinZipAes128) || (this.Encryption == EncryptionAlgorithm.WinZipAes256))
            {
                dstOffset -= 2;
                dst[dstOffset++] = 0x63;
                dst[dstOffset++] = 0;
            }
            dst[dstOffset++] = (byte) (this.timeBlobField & 0xff);
            dst[dstOffset++] = (byte) ((this.timeBlobField & 0xff00) >> 8);
            dst[dstOffset++] = (byte) ((this.timeBlobField & 0xff0000) >> 0x10);
            dst[dstOffset++] = (byte) ((this.timeBlobField & 0xff000000L) >> 0x18);
            dst[dstOffset++] = (byte) (this.crc32Field & 0xff);
            dst[dstOffset++] = (byte) ((this.crc32Field & 0xff00) >> 8);
            dst[dstOffset++] = (byte) ((this.crc32Field & 0xff0000) >> 0x10);
            dst[dstOffset++] = (byte) ((this.crc32Field & 0xff000000L) >> 0x18);
            int num4 = 0;
            if (this.outputUsesZip64Field.Value)
            {
                for (num4 = 0; num4 < 8; num4++)
                {
                    dst[dstOffset++] = 0xff;
                }
            }
            else
            {
                dst[dstOffset++] = (byte) (this.compressedSizeField & 0xffL);
                dst[dstOffset++] = (byte) ((this.compressedSizeField & 0xff00L) >> 8);
                dst[dstOffset++] = (byte) ((this.compressedSizeField & 0xff0000L) >> 0x10);
                dst[dstOffset++] = (byte) ((this.compressedSizeField & 0xff000000L) >> 0x18);
                dst[dstOffset++] = (byte) (this.uncompressedSizeField & 0xffL);
                dst[dstOffset++] = (byte) ((this.uncompressedSizeField & 0xff00L) >> 8);
                dst[dstOffset++] = (byte) ((this.uncompressedSizeField & 0xff0000L) >> 0x10);
                dst[dstOffset++] = (byte) ((this.uncompressedSizeField & 0xff000000L) >> 0x18);
            }
            byte[] encodedFileNameBytes = this.GetEncodedFileNameBytes();
            short length = (short) encodedFileNameBytes.Length;
            dst[dstOffset++] = (byte) (length & 0xff);
            dst[dstOffset++] = (byte) ((length & 0xff00) >> 8);
            this.presumeZip64Field = this.outputUsesZip64Field.Value;
            this.extraField = this.ConstructExtraField(true);
            short count = (this.extraField == null) ? ((short) 0) : ((short) this.extraField.Length);
            dst[dstOffset++] = (byte) (count & 0xff);
            dst[dstOffset++] = (byte) ((count & 0xff00) >> 8);
            int num7 = (this.commentBytesField == null) ? 0 : this.commentBytesField.Length;
            if ((num7 + dstOffset) > dst.Length)
            {
                num7 = dst.Length - dstOffset;
            }
            dst[dstOffset++] = (byte) (num7 & 0xff);
            dst[dstOffset++] = (byte) ((num7 & 0xff00) >> 8);
            if ((this.containerField.ZipFile != null) && (this.containerField.ZipFile.MaxOutputSegmentSize != 0))
            {
                dst[dstOffset++] = (byte) (this.diskNumberField & 0xff);
                dst[dstOffset++] = (byte) ((this.diskNumberField & 0xff00) >> 8);
            }
            else
            {
                dst[dstOffset++] = 0;
                dst[dstOffset++] = 0;
            }
            dst[dstOffset++] = this.isTextField ? ((byte) 1) : ((byte) 0);
            dst[dstOffset++] = 0;
            dst[dstOffset++] = (byte) (this.externalFileAttrsField & 0xff);
            dst[dstOffset++] = (byte) ((this.externalFileAttrsField & 0xff00) >> 8);
            dst[dstOffset++] = (byte) ((this.externalFileAttrsField & 0xff0000) >> 0x10);
            dst[dstOffset++] = (byte) ((this.externalFileAttrsField & 0xff000000L) >> 0x18);
            if (this.relativeOffsetOfLocalHeaderField > 0xffffffffL)
            {
                dst[dstOffset++] = 0xff;
                dst[dstOffset++] = 0xff;
                dst[dstOffset++] = 0xff;
                dst[dstOffset++] = 0xff;
            }
            else
            {
                dst[dstOffset++] = (byte) (this.relativeOffsetOfLocalHeaderField & 0xffL);
                dst[dstOffset++] = (byte) ((this.relativeOffsetOfLocalHeaderField & 0xff00L) >> 8);
                dst[dstOffset++] = (byte) ((this.relativeOffsetOfLocalHeaderField & 0xff0000L) >> 0x10);
                dst[dstOffset++] = (byte) ((this.relativeOffsetOfLocalHeaderField & 0xff000000L) >> 0x18);
            }
            Buffer.BlockCopy(encodedFileNameBytes, 0, dst, dstOffset, length);
            dstOffset += length;
            if (this.extraField != null)
            {
                byte[] src = this.extraField;
                int srcOffset = 0;
                Buffer.BlockCopy(src, srcOffset, dst, dstOffset, count);
                dstOffset += count;
            }
            if (num7 != 0)
            {
                Buffer.BlockCopy(this.commentBytesField, 0, dst, dstOffset, num7);
                dstOffset += num7;
            }
            s.Write(dst, 0, dstOffset);
        }

        internal void WriteHeader(Stream s, int cycle)
        {
            CountingStream stream = s as CountingStream;
            this.futureRolhField = (stream != null) ? stream.ComputedPosition : s.Position;
            int num = 0;
            int count = 0;
            byte[] src = new byte[30];
            src[count++] = 80;
            src[count++] = 0x4b;
            src[count++] = 3;
            src[count++] = 4;
            this.presumeZip64Field = (this.containerField.Zip64 == Zip64Option.Always) || ((this.containerField.Zip64 == Zip64Option.AsNecessary) && !s.CanSeek);
            short num3 = this.presumeZip64Field ? ((short) 0x2d) : ((short) 20);
            if (this.CompressionMethod == CompressionMethod.BZip2)
            {
                num3 = 0x2e;
            }
            src[count++] = (byte) (num3 & 0xff);
            src[count++] = (byte) ((num3 & 0xff00) >> 8);
            byte[] encodedFileNameBytes = this.GetEncodedFileNameBytes();
            short length = (short) encodedFileNameBytes.Length;
            if (this.encryptionField == EncryptionAlgorithm.None)
            {
                this.bitFieldField = (short) (this.bitFieldField & -2);
            }
            else
            {
                this.bitFieldField = (short) (this.bitFieldField | 1);
            }
            if (this.actualEncodingField.CodePage == Encoding.UTF8.CodePage)
            {
                this.bitFieldField = (short) (this.bitFieldField | 0x800);
            }
            if (this.IsDirectory || (cycle == 0x63))
            {
                this.bitFieldField = (short) (this.bitFieldField & -9);
                this.bitFieldField = (short) (this.bitFieldField & -2);
                this.Encryption = EncryptionAlgorithm.None;
                this.Password = null;
            }
            else if (!s.CanSeek)
            {
                this.bitFieldField = (short) (this.bitFieldField | 8);
            }
            src[count++] = (byte) (this.bitFieldField & 0xff);
            src[count++] = (byte) ((this.bitFieldField & 0xff00) >> 8);
            if (this.fileDataPositionField == -1L)
            {
                this.compressedSizeField = 0L;
                this.crcCalculatedField = false;
            }
            this.MaybeUnsetCompressionMethodForWriting(cycle);
            src[count++] = (byte) (this.compressionMethodField & 0xff);
            src[count++] = (byte) ((this.compressionMethodField & 0xff00) >> 8);
            if (cycle == 0x63)
            {
                this.SetZip64Flags();
            }
            else if ((this.Encryption == EncryptionAlgorithm.WinZipAes128) || (this.Encryption == EncryptionAlgorithm.WinZipAes256))
            {
                count -= 2;
                src[count++] = 0x63;
                src[count++] = 0;
            }
            this.timeBlobField = SharedUtilities.DateTimeToPacked(this.LastModified);
            src[count++] = (byte) (this.timeBlobField & 0xff);
            src[count++] = (byte) ((this.timeBlobField & 0xff00) >> 8);
            src[count++] = (byte) ((this.timeBlobField & 0xff0000) >> 0x10);
            src[count++] = (byte) ((this.timeBlobField & 0xff000000L) >> 0x18);
            src[count++] = (byte) (this.crc32Field & 0xff);
            src[count++] = (byte) ((this.crc32Field & 0xff00) >> 8);
            src[count++] = (byte) ((this.crc32Field & 0xff0000) >> 0x10);
            src[count++] = (byte) ((this.crc32Field & 0xff000000L) >> 0x18);
            if (this.presumeZip64Field)
            {
                for (num = 0; num < 8; num++)
                {
                    src[count++] = 0xff;
                }
            }
            else
            {
                src[count++] = (byte) (this.compressedSizeField & 0xffL);
                src[count++] = (byte) ((this.compressedSizeField & 0xff00L) >> 8);
                src[count++] = (byte) ((this.compressedSizeField & 0xff0000L) >> 0x10);
                src[count++] = (byte) ((this.compressedSizeField & 0xff000000L) >> 0x18);
                src[count++] = (byte) (this.uncompressedSizeField & 0xffL);
                src[count++] = (byte) ((this.uncompressedSizeField & 0xff00L) >> 8);
                src[count++] = (byte) ((this.uncompressedSizeField & 0xff0000L) >> 0x10);
                src[count++] = (byte) ((this.uncompressedSizeField & 0xff000000L) >> 0x18);
            }
            src[count++] = (byte) (length & 0xff);
            src[count++] = (byte) ((length & 0xff00) >> 8);
            this.extraField = this.ConstructExtraField(false);
            short num5 = (this.extraField == null) ? ((short) 0) : ((short) this.extraField.Length);
            src[count++] = (byte) (num5 & 0xff);
            src[count++] = (byte) ((num5 & 0xff00) >> 8);
            byte[] dst = new byte[(count + length) + num5];
            Buffer.BlockCopy(src, 0, dst, 0, count);
            Buffer.BlockCopy(encodedFileNameBytes, 0, dst, count, encodedFileNameBytes.Length);
            count += encodedFileNameBytes.Length;
            if (this.extraField != null)
            {
                Buffer.BlockCopy(this.extraField, 0, dst, count, this.extraField.Length);
                count += this.extraField.Length;
            }
            this.lengthOfHeaderField = count;
            ZipSegmentedStream stream2 = s as ZipSegmentedStream;
            if (stream2 != null)
            {
                stream2.ContiguousWrite = true;
                uint num6 = stream2.ComputeSegment(count);
                if (num6 != stream2.CurrentSegment)
                {
                    this.futureRolhField = 0L;
                }
                else
                {
                    this.futureRolhField = stream2.Position;
                }
                this.diskNumberField = num6;
            }
            if ((this.containerField.Zip64 == Zip64Option.Default) && (((uint) this.relativeOffsetOfLocalHeaderField) >= uint.MaxValue))
            {
                throw new ZipException("Offset within the zip archive exceeds 0xFFFFFFFF. Consider setting the UseZip64WhenSaving property on the ZipFile instance.");
            }
            s.Write(dst, 0, count);
            if (stream2 != null)
            {
                stream2.ContiguousWrite = false;
            }
            this.entryHeaderField = dst;
        }

        internal void WriteSecurityMetadata(Stream outstream)
        {
            if (this.Encryption != EncryptionAlgorithm.None)
            {
                string password = this.passwordField;
                if ((this.sourceField == ZipEntrySource.ZipFile) && (password == null))
                {
                    password = this.containerField.Password;
                }
                if (password == null)
                {
                    this.zipCryptoForWriteField = null;
                    this.aesCryptoForWriteField = null;
                }
                else if (this.Encryption == EncryptionAlgorithm.PkzipWeak)
                {
                    this.zipCryptoForWriteField = ZipCrypto.ForWrite(password);
                    Random random = new Random();
                    byte[] buffer = new byte[12];
                    random.NextBytes(buffer);
                    if ((this.bitFieldField & 8) == 8)
                    {
                        this.timeBlobField = SharedUtilities.DateTimeToPacked(this.LastModified);
                        buffer[11] = (byte) ((this.timeBlobField >> 8) & 0xff);
                    }
                    else
                    {
                        this.FigureCrc32();
                        buffer[11] = (byte) ((this.crc32Field >> 0x18) & 0xff);
                    }
                    byte[] buffer2 = this.zipCryptoForWriteField.EncryptMessage(buffer, buffer.Length);
                    outstream.Write(buffer2, 0, buffer2.Length);
                    this.lengthOfHeaderField += buffer2.Length;
                }
                else if ((this.Encryption == EncryptionAlgorithm.WinZipAes128) || (this.Encryption == EncryptionAlgorithm.WinZipAes256))
                {
                    int keyStrengthInBits = GetKeyStrengthInBits(this.Encryption);
                    this.aesCryptoForWriteField = WinZipAesCrypto.Generate(password, keyStrengthInBits);
                    outstream.Write(this.aesCryptoForWriteField.Salt, 0, this.aesCryptoForWriteField.saltField.Length);
                    outstream.Write(this.aesCryptoForWriteField.GeneratedPV, 0, this.aesCryptoForWriteField.GeneratedPV.Length);
                    this.lengthOfHeaderField += this.aesCryptoForWriteField.saltField.Length + this.aesCryptoForWriteField.GeneratedPV.Length;
                }
            }
        }

        private void WriteStatus(string format, params object[] args)
        {
            if ((this.containerField.ZipFile != null) && this.containerField.ZipFile.Verbose)
            {
                this.containerField.ZipFile.StatusMessageTextWriter.WriteLine(format, args);
            }
        }

        /// <summary>
        /// Last Access time for the file represented by the entry.
        /// </summary>
        /// <remarks>
        /// This value may or may not be meaningful.  If the <c>ZipEntry</c> was read from an existing
        /// Zip archive, this information may not be available. For an explanation of why, see
        /// <see cref="P:ZipEntry.ModifiedTime" />.
        /// </remarks>
        /// <seealso cref="P:ZipEntry.ModifiedTime" />
        /// <seealso cref="P:ZipEntry.CreationTime" />
        /// <seealso cref="M:ZipEntry.SetEntryTimes(System.DateTime,System.DateTime,System.DateTime)" />
        public DateTime AccessedTime
        {
            get
            {
                return this.atimeField;
            }
            set
            {
                this.SetEntryTimes(this.ctimeField, value, this.mtimeField);
            }
        }

        /// <summary>
        /// Specifies the alternate text encoding used by this ZipEntry
        /// </summary>
        /// <remarks>
        /// <para>
        /// The default text encoding used in Zip files for encoding filenames and
        /// comments is IBM437, which is something like a superset of ASCII.  In
        /// cases where this is insufficient, applications can specify an
        /// alternate encoding.
        /// </para>
        /// <para>
        /// When creating a zip file, the usage of the alternate encoding is
        /// governed by the <see cref="P:ZipEntry.AlternateEncodingUsage" /> property.
        /// Typically you would set both properties to tell DotNetZip to employ an
        /// encoding that is not IBM437 in the zipfile you are creating.
        /// </para>
        /// <para>
        /// Keep in mind that because the ZIP specification states that the only
        /// valid encodings to use are IBM437 and UTF-8, if you use something
        /// other than that, then zip tools and libraries may not be able to
        /// successfully read the zip archive you generate.
        /// </para>
        /// <para>
        /// The zip specification states that applications should presume that
        /// IBM437 is in use, except when a special bit is set, which indicates
        /// UTF-8. There is no way to specify an arbitrary code page, within the
        /// zip file itself. When you create a zip file encoded with gb2312 or
        /// ibm861 or anything other than IBM437 or UTF-8, then the application
        /// that reads the zip file needs to "know" which code page to use. In
        /// some cases, the code page used when reading is chosen implicitly. For
        /// example, WinRar uses the ambient code page for the host desktop
        /// operating system. The pitfall here is that if you create a zip in
        /// Copenhagen and send it to Tokyo, the reader of the zipfile may not be
        /// able to decode successfully.
        /// </para>
        /// </remarks>
        /// <example>
        /// This example shows how to create a zipfile encoded with a
        /// language-specific encoding:
        /// <code>
        /// using (var zip = new ZipFile())
        /// {
        /// zip.AlternateEnoding = System.Text.Encoding.GetEncoding("ibm861");
        /// zip.AlternateEnodingUsage = ZipOption.Always;
        /// zip.AddFileS(arrayOfFiles);
        /// zip.Save("Myarchive-Encoded-in-IBM861.zip");
        /// }
        /// </code>
        /// </example>
        /// <seealso cref="P:ZipFile.AlternateEncodingUsage" />
        public Encoding AlternateEncoding { get; set; }

        /// <summary>
        /// Describes if and when this instance should apply
        /// AlternateEncoding to encode the FileName and Comment, when
        /// saving.
        /// </summary>
        /// <seealso cref="P:ZipFile.AlternateEncoding" />
        public ZipOption AlternateEncodingUsage { get; set; }

        internal Stream ArchiveStream
        {
            get
            {
                if (this.archiveStreamField == null)
                {
                    if (this.containerField.ZipFile != null)
                    {
                        ZipFile zipFile = this.containerField.ZipFile;
                        zipFile.Reset(false);
                        this.archiveStreamField = zipFile.StreamForDiskNumber(this.diskNumberField);
                    }
                    else
                    {
                        this.archiveStreamField = this.containerField.ZipOutputStream.OutputStream;
                    }
                }
                return this.archiveStreamField;
            }
        }

        /// <summary>
        /// The file attributes for the entry.
        /// </summary>
        /// 
        /// <remarks>
        /// 
        /// <para>
        /// The <see cref="T:System.IO.FileAttributes">attributes</see> in NTFS include
        /// ReadOnly, Archive, Hidden, System, and Indexed.  When adding a
        /// <c>ZipEntry</c> to a ZipFile, these attributes are set implicitly when
        /// adding an entry from the filesystem.  When adding an entry from a stream
        /// or string, the Attributes are not set implicitly.  Regardless of the way
        /// an entry was added to a <c>ZipFile</c>, you can set the attributes
        /// explicitly if you like.
        /// </para>
        /// 
        /// <para>
        /// When reading a <c>ZipEntry</c> from a <c>ZipFile</c>, the attributes are
        /// set according to the data stored in the <c>ZipFile</c>. If you extract the
        /// entry from the archive to a filesystem file, DotNetZip will set the
        /// attributes on the resulting file accordingly.
        /// </para>
        /// 
        /// <para>
        /// The attributes can be set explicitly by the application.  For example the
        /// application may wish to set the <c>FileAttributes.ReadOnly</c> bit for all
        /// entries added to an archive, so that on unpack, this attribute will be set
        /// on the extracted file.  Any changes you make to this property are made
        /// permanent only when you call a <c>Save()</c> method on the <c>ZipFile</c>
        /// instance that contains the ZipEntry.
        /// </para>
        /// 
        /// <para>
        /// For example, an application may wish to zip up a directory and set the
        /// ReadOnly bit on every file in the archive, so that upon later extraction,
        /// the resulting files will be marked as ReadOnly.  Not every extraction tool
        /// respects these attributes, but if you unpack with DotNetZip, as for
        /// example in a self-extracting archive, then the attributes will be set as
        /// they are stored in the <c>ZipFile</c>.
        /// </para>
        /// 
        /// <para>
        /// These attributes may not be interesting or useful if the resulting archive
        /// is extracted on a non-Windows platform.  How these attributes get used
        /// upon extraction depends on the platform and tool used.
        /// </para>
        /// 
        /// <para>
        /// This property is only partially supported in the Silverlight version
        /// of the library: applications can read attributes on entries within
        /// ZipFiles. But extracting entries within Silverlight will not set the
        /// attributes on the extracted files.
        /// </para>
        /// 
        /// </remarks>
        public FileAttributes Attributes
        {
            get
            {
                return (FileAttributes) this.externalFileAttrsField;
            }
            set
            {
                this.externalFileAttrsField = (int) value;
                this.versionMadeByField = 0x2d;
                this.metadataChangedField = true;
            }
        }

        /// <summary>
        /// True if the referenced entry is a directory.
        /// </summary>
        internal bool AttributesIndicateDirectory
        {
            get
            {
                return ((this.internalFileAttrsField == 0) && ((this.externalFileAttrsField & 0x10) == 0x10));
            }
        }

        /// <summary>
        /// The bitfield for the entry as defined in the zip spec. You probably
        /// never need to look at this.
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// You probably do not need to concern yourself with the contents of this
        /// property, but in case you do:
        /// </para>
        /// 
        /// <list type="table">
        /// <listheader>
        /// <term>bit</term>
        /// <description>meaning</description>
        /// </listheader>
        /// 
        /// <item>
        /// <term>0</term>
        /// <description>set if encryption is used.</description>
        /// </item>
        /// 
        /// <item>
        /// <term>1-2</term>
        /// <description>
        /// set to determine whether normal, max, fast deflation.  DotNetZip library
        /// always leaves these bits unset when writing (indicating "normal"
        /// deflation"), but can read an entry with any value here.
        /// </description>
        /// </item>
        /// 
        /// <item>
        /// <term>3</term>
        /// <description>
        /// Indicates that the Crc32, Compressed and Uncompressed sizes are zero in the
        /// local header.  This bit gets set on an entry during writing a zip file, when
        /// it is saved to a non-seekable output stream.
        /// </description>
        /// </item>
        /// 
        /// 
        /// <item>
        /// <term>4</term>
        /// <description>reserved for "enhanced deflating". This library doesn't do enhanced deflating.</description>
        /// </item>
        /// 
        /// <item>
        /// <term>5</term>
        /// <description>set to indicate the zip is compressed patched data.  This library doesn't do that.</description>
        /// </item>
        /// 
        /// <item>
        /// <term>6</term>
        /// <description>
        /// set if PKWare's strong encryption is used (must also set bit 1 if bit 6 is
        /// set). This bit is not set if WinZip's AES encryption is set.</description>
        /// </item>
        /// 
        /// <item>
        /// <term>7</term>
        /// <description>not used</description>
        /// </item>
        /// 
        /// <item>
        /// <term>8</term>
        /// <description>not used</description>
        /// </item>
        /// 
        /// <item>
        /// <term>9</term>
        /// <description>not used</description>
        /// </item>
        /// 
        /// <item>
        /// <term>10</term>
        /// <description>not used</description>
        /// </item>
        /// 
        /// <item>
        /// <term>11</term>
        /// <description>
        /// Language encoding flag (EFS).  If this bit is set, the filename and comment
        /// fields for this file must be encoded using UTF-8. This library currently
        /// does not support UTF-8.
        /// </description>
        /// </item>
        /// 
        /// <item>
        /// <term>12</term>
        /// <description>Reserved by PKWARE for enhanced compression.</description>
        /// </item>
        /// 
        /// <item>
        /// <term>13</term>
        /// <description>
        /// Used when encrypting the Central Directory to indicate selected data
        /// values in the Local Header are masked to hide their actual values.  See
        /// the section in <a href="http://www.pkware.com/documents/casestudies/APPNOTE.TXT">the Zip
        /// specification</a> describing the Strong Encryption Specification for
        /// details.
        /// </description>
        /// </item>
        /// 
        /// <item>
        /// <term>14</term>
        /// <description>Reserved by PKWARE.</description>
        /// </item>
        /// 
        /// <item>
        /// <term>15</term>
        /// <description>Reserved by PKWARE.</description>
        /// </item>
        /// 
        /// </list>
        /// 
        /// </remarks>
        public short BitField
        {
            get
            {
                return this.bitFieldField;
            }
        }

        private int BufferSize
        {
            get
            {
                return this.containerField.BufferSize;
            }
        }

        /// <summary>
        /// The comment attached to the ZipEntry.
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// Each entry in a zip file can optionally have a comment associated to
        /// it. The comment might be displayed by a zip tool during extraction, for
        /// example.
        /// </para>
        /// 
        /// <para>
        /// By default, the <c>Comment</c> is encoded in IBM437 code page. You can
        /// specify an alternative with <see cref="P:ZipEntry.AlternateEncoding" /> and
        /// <see cref="P:ZipEntry.AlternateEncodingUsage" />.
        /// </para>
        /// </remarks>
        /// <seealso cref="P:ZipEntry.AlternateEncoding" />
        /// <seealso cref="P:ZipEntry.AlternateEncodingUsage" />
        public string Comment
        {
            get
            {
                return this.commentField;
            }
            set
            {
                this.commentField = value;
                this.metadataChangedField = true;
            }
        }

        /// <summary>
        /// The compressed size of the file, in bytes, within the zip archive.
        /// </summary>
        /// 
        /// <remarks>
        /// When reading a <c>ZipFile</c>, this value is read in from the existing
        /// zip file. When creating or updating a <c>ZipFile</c>, the compressed
        /// size is computed during compression.  Therefore the value on a
        /// <c>ZipEntry</c> is valid after a call to <c>Save()</c> (or one of its
        /// overloads) in that case.
        /// </remarks>
        /// 
        /// <seealso cref="P:ZipEntry.UncompressedSize" />
        public long CompressedSize
        {
            get
            {
                return this.compressedSizeField;
            }
        }

        /// <summary>
        /// Sets the compression level to be used for the entry when saving the zip
        /// archive. This applies only for CompressionMethod = DEFLATE.
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// When using the DEFLATE compression method, Varying the compression
        /// level used on entries can affect the size-vs-speed tradeoff when
        /// compression and decompressing data streams or files.
        /// </para>
        /// 
        /// <para>
        /// If you do not set this property, the default compression level is used,
        /// which normally gives a good balance of compression efficiency and
        /// compression speed.  In some tests, using <c>BestCompression</c> can
        /// double the time it takes to compress, while delivering just a small
        /// increase in compression efficiency.  This behavior will vary with the
        /// type of data you compress.  If you are in doubt, just leave this setting
        /// alone, and accept the default.
        /// </para>
        /// 
        /// <para>
        /// When setting this property on a <c>ZipEntry</c> that is read from an
        /// existing zip file, calling <c>ZipFile.Save()</c> will cause the new
        /// <c>CompressionLevel</c> to be used on the entry in the newly saved zip file.
        /// </para>
        /// 
        /// <para>
        /// Setting this property may have the side effect of modifying the
        /// <c>CompressionMethod</c> property. If you set the <c>CompressionLevel</c>
        /// to a value other than <c>None</c>, <c>CompressionMethod</c> will be set
        /// to <c>Deflate</c>, if it was previously <c>None</c>.
        /// </para>
        /// 
        /// <para>
        /// Setting this property has no effect if the <c>CompressionMethod</c> is something
        /// other than <c>Deflate</c> or <c>None</c>.
        /// </para>
        /// </remarks>
        /// 
        /// <seealso cref="P:ZipEntry.CompressionMethod" />
        public CompressionLevel CompressionLevel
        {
            get
            {
                return this.compressionLevelField;
            }
            set
            {
                if (((this.compressionMethodField == 8) || (this.compressionMethodField == 0)) && ((value != CompressionLevel.Default) || (this.compressionMethodField != 8)))
                {
                    this.compressionLevelField = value;
                    if ((value != CompressionLevel.None) || (this.compressionMethodField != 0))
                    {
                        if (this.compressionLevelField == CompressionLevel.None)
                        {
                            this.compressionMethodField = 0;
                        }
                        else
                        {
                            this.compressionMethodField = 8;
                        }
                        if (this.containerField.ZipFile != null)
                        {
                            this.containerField.ZipFile.NotifyEntryChanged();
                        }
                        this.restreamRequiredOnSaveField = true;
                    }
                }
            }
        }

        /// <summary>
        /// The compression method employed for this ZipEntry.
        /// </summary>
        /// 
        /// <remarks>
        /// 
        /// <para>
        /// <see href="http://www.pkware.com/documents/casestudies/APPNOTE.TXT">The
        /// Zip specification</see> allows a variety of compression methods.  This
        /// library supports just two: 0x08 = Deflate.  0x00 = Store (no compression),
        /// for reading or writing.
        /// </para>
        /// 
        /// <para>
        /// When reading an entry from an existing zipfile, the value you retrieve
        /// here indicates the compression method used on the entry by the original
        /// creator of the zip.  When writing a zipfile, you can specify either 0x08
        /// (Deflate) or 0x00 (None).  If you try setting something else, you will get
        /// an exception.
        /// </para>
        /// 
        /// <para>
        /// You may wish to set <c>CompressionMethod</c> to <c>CompressionMethod.None</c> (0)
        /// when zipping already-compressed data like a jpg, png, or mp3 file.
        /// This can save time and cpu cycles.
        /// </para>
        /// 
        /// <para>
        /// When setting this property on a <c>ZipEntry</c> that is read from an
        /// existing zip file, calling <c>ZipFile.Save()</c> will cause the new
        /// CompressionMethod to be used on the entry in the newly saved zip file.
        /// </para>
        /// 
        /// <para>
        /// Setting this property may have the side effect of modifying the
        /// <c>CompressionLevel</c> property. If you set the <c>CompressionMethod</c> to a
        /// value other than <c>None</c>, and <c>CompressionLevel</c> is previously
        /// set to <c>None</c>, then <c>CompressionLevel</c> will be set to
        /// <c>Default</c>.
        /// </para>
        /// </remarks>
        /// 
        /// <seealso cref="P:ZipEntry.CompressionMethod" />
        /// 
        /// <example>
        /// In this example, the first entry added to the zip archive uses the default
        /// behavior - compression is used where it makes sense.  The second entry,
        /// the MP3 file, is added to the archive without being compressed.
        /// <code>
        /// using (ZipFile zip = new ZipFile(ZipFileToCreate))
        /// {
        /// ZipEntry e1= zip.AddFile(@"notes\Readme.txt");
        /// ZipEntry e2= zip.AddFile(@"music\StopThisTrain.mp3");
        /// e2.CompressionMethod = CompressionMethod.None;
        /// zip.Save();
        /// }
        /// </code>
        /// 
        /// <code lang="VB">
        /// Using zip As New ZipFile(ZipFileToCreate)
        /// zip.AddFile("notes\Readme.txt")
        /// Dim e2 as ZipEntry = zip.AddFile("music\StopThisTrain.mp3")
        /// e2.CompressionMethod = CompressionMethod.None
        /// zip.Save
        /// End Using
        /// </code>
        /// </example>
        public CompressionMethod CompressionMethod
        {
            get
            {
                return (CompressionMethod) this.compressionMethodField;
            }
            set
            {
                if (value != ((CompressionMethod) this.compressionMethodField))
                {
                    if (((value != CompressionMethod.None) && (value != CompressionMethod.Deflate)) && (value != CompressionMethod.BZip2))
                    {
                        throw new InvalidOperationException("Unsupported compression method.");
                    }
                    this.compressionMethodField = (short) value;
                    if (this.compressionMethodField == 0)
                    {
                        this.compressionLevelField = CompressionLevel.None;
                    }
                    else if (this.CompressionLevel == CompressionLevel.None)
                    {
                        this.compressionLevelField = CompressionLevel.Default;
                    }
                    if (this.containerField.ZipFile != null)
                    {
                        this.containerField.ZipFile.NotifyEntryChanged();
                    }
                    this.restreamRequiredOnSaveField = true;
                }
            }
        }

        /// <summary>
        /// The ratio of compressed size to uncompressed size of the ZipEntry.
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// This is a ratio of the compressed size to the uncompressed size of the
        /// entry, expressed as a double in the range of 0 to 100+. A value of 100
        /// indicates no compression at all.  It could be higher than 100 when the
        /// compression algorithm actually inflates the data, as may occur for small
        /// files, or uncompressible data that is encrypted.
        /// </para>
        /// 
        /// <para>
        /// You could format it for presentation to a user via a format string of
        /// "{3,5:F0}%" to see it as a percentage.
        /// </para>
        /// 
        /// <para>
        /// If the size of the original uncompressed file is 0, implying a
        /// denominator of 0, the return value will be zero.
        /// </para>
        /// 
        /// <para>
        /// This property is valid after reading in an existing zip file, or after
        /// saving the <c>ZipFile</c> that contains the ZipEntry. You cannot know the
        /// effect of a compression transform until you try it.
        /// </para>
        /// 
        /// </remarks>
        public double CompressionRatio
        {
            get
            {
                if (this.UncompressedSize == 0L)
                {
                    return 0.0;
                }
                return (100.0 * (1.0 - ((1.0 * this.CompressedSize) / (1.0 * this.UncompressedSize))));
            }
        }

        /// <summary>
        /// The 32-bit CRC (Cyclic Redundancy Check) on the contents of the ZipEntry.
        /// </summary>
        /// 
        /// <remarks>
        /// 
        /// <para> You probably don't need to concern yourself with this. It is used
        /// internally by DotNetZip to verify files or streams upon extraction.  </para>
        /// 
        /// <para> The value is a <see href="http://en.wikipedia.org/wiki/CRC32">32-bit
        /// CRC</see> using 0xEDB88320 for the polynomial. This is the same CRC-32 used in
        /// PNG, MPEG-2, and other protocols and formats.  It is a read-only property; when
        /// creating a Zip archive, the CRC for each entry is set only after a call to
        /// <c>Save()</c> on the containing ZipFile. When reading an existing zip file, the value
        /// of this property reflects the stored CRC for the entry.  </para>
        /// 
        /// </remarks>
        public int Crc
        {
            get
            {
                return this.crc32Field;
            }
        }

        /// <summary>
        /// The file creation time for the file represented by the entry.
        /// </summary>
        /// 
        /// <remarks>
        /// This value may or may not be meaningful.  If the <c>ZipEntry</c> was read
        /// from an existing zip archive, and the creation time was not set on the entry
        /// when the zip file was created, then this property may be meaningless. For an
        /// explanation of why, see <see cref="P:ZipEntry.ModifiedTime" />.
        /// </remarks>
        /// <seealso cref="P:ZipEntry.ModifiedTime" />
        /// <seealso cref="P:ZipEntry.AccessedTime" />
        /// <seealso cref="M:ZipEntry.SetEntryTimes(System.DateTime,System.DateTime,System.DateTime)" />
        public DateTime CreationTime
        {
            get
            {
                return this.ctimeField;
            }
            set
            {
                this.SetEntryTimes(value, this.atimeField, this.mtimeField);
            }
        }

        /// <summary>
        /// Specifies whether the Creation, Access, and Modified times for the given
        /// entry will be emitted in "Unix(tm) format" when the zip archive is saved.
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// An application creating a zip archive can use this flag to explicitly
        /// specify that the file times for the entry should or should not be stored
        /// in the zip archive in the format used by Unix. By default this flag is
        /// <c>false</c>, meaning the Unix-format times are not stored in the zip
        /// archive.
        /// </para>
        /// 
        /// <para>
        /// When adding an entry from a file or directory, the Creation (<see cref="P:ZipEntry.CreationTime" />), Access (<see cref="P:ZipEntry.AccessedTime" />), and Modified
        /// (<see cref="P:ZipEntry.ModifiedTime" />) times for the given entry are automatically
        /// set from the filesystem values. When adding an entry from a stream or
        /// string, all three values are implicitly set to DateTime.Now.  Applications
        /// can also explicitly set those times by calling <see cref="M:ZipEntry.SetEntryTimes(System.DateTime,System.DateTime,System.DateTime)" />.
        /// </para>
        /// 
        /// <para>
        /// <see href="http://www.pkware.com/documents/casestudies/APPNOTE.TXT">PKWARE's
        /// zip specification</see> describes multiple ways to format these times in a
        /// zip file. One is the format Windows applications normally use: 100ns ticks
        /// since Jan 1, 1601 UTC.  The other is a format Unix applications typically
        /// use: seconds since Jan 1, 1970 UTC.  Each format can be stored in an
        /// "extra field" in the zip entry when saving the zip archive. The former
        /// uses an extra field with a Header Id of 0x000A, while the latter uses a
        /// header ID of 0x5455.
        /// </para>
        /// 
        /// <para>
        /// Not all tools and libraries can interpret these fields.  Windows
        /// compressed folders is one that can read the Windows Format timestamps,
        /// while I believe the <see href="http://www.info-zip.org/">Infozip</see>
        /// tools can read the Unix format timestamps. Although the time values are
        /// easily convertible, subject to a loss of precision, some tools and
        /// libraries may be able to read only one or the other. DotNetZip can read or
        /// write times in either or both formats.
        /// </para>
        /// 
        /// <para>
        /// The times stored are taken from <see cref="P:ZipEntry.ModifiedTime" />, <see cref="P:ZipEntry.AccessedTime" />, and <see cref="P:ZipEntry.CreationTime" />.
        /// </para>
        /// 
        /// <para>
        /// This property is not mutually exclusive from the <see cref="P:ZipEntry.EmitTimesInWindowsFormatWhenSaving" /> property.  It is
        /// possible that a zip entry can embed the timestamps in both forms, one
        /// form, or neither.  But, there are no guarantees that a program running on
        /// Mac or Linux will gracefully handle NTFS Formatted times, or that a
        /// non-DotNetZip-powered application running on Windows will be able to
        /// handle file times in Unix format. When in doubt, test.
        /// </para>
        /// 
        /// <para>
        /// Normally you will use the <see cref="P:ZipFile.EmitTimesInUnixFormatWhenSaving">ZipFile.EmitTimesInUnixFormatWhenSaving</see>
        /// property, to specify the behavior for all entries, rather than the
        /// property on each individual entry.
        /// </para>
        /// </remarks>
        /// 
        /// <seealso cref="M:ZipEntry.SetEntryTimes(System.DateTime,System.DateTime,System.DateTime)" />
        /// <seealso cref="P:ZipEntry.EmitTimesInWindowsFormatWhenSaving" />
        /// <seealso cref="P:ZipFile.EmitTimesInUnixFormatWhenSaving" />
        /// <seealso cref="P:ZipEntry.CreationTime" />
        /// <seealso cref="P:ZipEntry.AccessedTime" />
        /// <seealso cref="P:ZipEntry.ModifiedTime" />
        public bool EmitTimesInUnixFormatWhenSaving
        {
            get
            {
                return this.emitUnixTimesField;
            }
            set
            {
                this.emitUnixTimesField = value;
                this.metadataChangedField = true;
            }
        }

        /// <summary>
        /// Specifies whether the Creation, Access, and Modified times for the given
        /// entry will be emitted in "Windows format" when the zip archive is saved.
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// An application creating a zip archive can use this flag to explicitly
        /// specify that the file times for the entry should or should not be stored
        /// in the zip archive in the format used by Windows. The default value of
        /// this property is <c>true</c>.
        /// </para>
        /// 
        /// <para>
        /// When adding an entry from a file or directory, the Creation (<see cref="P:ZipEntry.CreationTime" />), Access (<see cref="P:ZipEntry.AccessedTime" />), and Modified
        /// (<see cref="P:ZipEntry.ModifiedTime" />) times for the given entry are automatically
        /// set from the filesystem values. When adding an entry from a stream or
        /// string, all three values are implicitly set to DateTime.Now.  Applications
        /// can also explicitly set those times by calling <see cref="M:ZipEntry.SetEntryTimes(System.DateTime,System.DateTime,System.DateTime)" />.
        /// </para>
        /// 
        /// <para>
        /// <see href="http://www.pkware.com/documents/casestudies/APPNOTE.TXT">PKWARE's
        /// zip specification</see> describes multiple ways to format these times in a
        /// zip file. One is the format Windows applications normally use: 100ns ticks
        /// since Jan 1, 1601 UTC.  The other is a format Unix applications typically
        /// use: seconds since January 1, 1970 UTC.  Each format can be stored in an
        /// "extra field" in the zip entry when saving the zip archive. The former
        /// uses an extra field with a Header Id of 0x000A, while the latter uses a
        /// header ID of 0x5455.
        /// </para>
        /// 
        /// <para>
        /// Not all zip tools and libraries can interpret these fields.  Windows
        /// compressed folders is one that can read the Windows Format timestamps,
        /// while I believe the <see href="http://www.info-zip.org/">Infozip</see>
        /// tools can read the Unix format timestamps. Although the time values are
        /// easily convertible, subject to a loss of precision, some tools and
        /// libraries may be able to read only one or the other. DotNetZip can read or
        /// write times in either or both formats.
        /// </para>
        /// 
        /// <para>
        /// The times stored are taken from <see cref="P:ZipEntry.ModifiedTime" />, <see cref="P:ZipEntry.AccessedTime" />, and <see cref="P:ZipEntry.CreationTime" />.
        /// </para>
        /// 
        /// <para>
        /// This property is not mutually exclusive from the <see cref="P:ZipEntry.EmitTimesInUnixFormatWhenSaving" /> property.  It is
        /// possible that a zip entry can embed the timestamps in both forms, one
        /// form, or neither.  But, there are no guarantees that a program running on
        /// Mac or Linux will gracefully handle NTFS Formatted times, or that a
        /// non-DotNetZip-powered application running on Windows will be able to
        /// handle file times in Unix format. When in doubt, test.
        /// </para>
        /// 
        /// <para>
        /// Normally you will use the <see cref="P:ZipFile.EmitTimesInWindowsFormatWhenSaving">ZipFile.EmitTimesInWindowsFormatWhenSaving</see>
        /// property, to specify the behavior for all entries in a zip, rather than
        /// the property on each individual entry.
        /// </para>
        /// 
        /// </remarks>
        /// 
        /// <seealso cref="M:ZipEntry.SetEntryTimes(System.DateTime,System.DateTime,System.DateTime)" />
        /// <seealso cref="P:ZipEntry.EmitTimesInUnixFormatWhenSaving" />
        /// <seealso cref="P:ZipEntry.CreationTime" />
        /// <seealso cref="P:ZipEntry.AccessedTime" />
        /// <seealso cref="P:ZipEntry.ModifiedTime" />
        public bool EmitTimesInWindowsFormatWhenSaving
        {
            get
            {
                return this.emitNtfsTimesField;
            }
            set
            {
                this.emitNtfsTimesField = value;
                this.metadataChangedField = true;
            }
        }

        /// <summary>
        /// Set this to specify which encryption algorithm to use for the entry when
        /// saving it to a zip archive.
        /// </summary>
        /// 
        /// <remarks>
        /// 
        /// <para>
        /// Set this property in order to encrypt the entry when the <c>ZipFile</c> is
        /// saved. When setting this property, you must also set a <see cref="P:ZipEntry.Password" /> on the entry.  If you set a value other than <see cref="F:EncryptionAlgorithm.None" /> on this property and do not set a
        /// <c>Password</c> then the entry will not be encrypted. The <c>ZipEntry</c>
        /// data is encrypted as the <c>ZipFile</c> is saved, when you call <see cref="M:ZipFile.Save" /> or one of its cousins on the containing
        /// <c>ZipFile</c> instance. You do not need to specify the <c>Encryption</c>
        /// when extracting entries from an archive.
        /// </para>
        /// 
        /// <para>
        /// The Zip specification from PKWare defines a set of encryption algorithms,
        /// and the data formats for the zip archive that support them, and PKWare
        /// supports those algorithms in the tools it produces. Other vendors of tools
        /// and libraries, such as WinZip or Xceed, typically support <em>a
        /// subset</em> of the algorithms specified by PKWare. These tools can
        /// sometimes support additional different encryption algorithms and data
        /// formats, not specified by PKWare. The AES Encryption specified and
        /// supported by WinZip is the most popular example. This library supports a
        /// subset of the complete set of algorithms specified by PKWare and other
        /// vendors.
        /// </para>
        /// 
        /// <para>
        /// There is no common, ubiquitous multi-vendor standard for strong encryption
        /// within zip files. There is broad support for so-called "traditional" Zip
        /// encryption, sometimes called Zip 2.0 encryption, as <see href="http://www.pkware.com/documents/casestudies/APPNOTE.TXT">specified
        /// by PKWare</see>, but this encryption is considered weak and
        /// breakable. This library currently supports the Zip 2.0 "weak" encryption,
        /// and also a stronger WinZip-compatible AES encryption, using either 128-bit
        /// or 256-bit key strength. If you want DotNetZip to support an algorithm
        /// that is not currently supported, call the author of this library and maybe
        /// we can talk business.
        /// </para>
        /// 
        /// <para>
        /// The <see cref="T:ZipFile" /> class also has a <see cref="P:ZipFile.Encryption" /> property.  In most cases you will use
        /// <em>that</em> property when setting encryption. This property takes
        /// precedence over any <c>Encryption</c> set on the <c>ZipFile</c> itself.
        /// Typically, you would use the per-entry Encryption when most entries in the
        /// zip archive use one encryption algorithm, and a few entries use a
        /// different one.  If all entries in the zip file use the same Encryption,
        /// then it is simpler to just set this property on the ZipFile itself, when
        /// creating a zip archive.
        /// </para>
        /// 
        /// <para>
        /// Some comments on updating archives: If you read a <c>ZipFile</c>, you can
        /// modify the Encryption on an encrypted entry: you can remove encryption
        /// from an entry that was encrypted; you can encrypt an entry that was not
        /// encrypted previously; or, you can change the encryption algorithm.  The
        /// changes in encryption are not made permanent until you call Save() on the
        /// <c>ZipFile</c>.  To effect changes in encryption, the entry content is
        /// streamed through several transformations, depending on the modification
        /// the application has requested. For example if the entry is not encrypted
        /// and the application sets <c>Encryption</c> to <c>PkzipWeak</c>, then at
        /// the time of <c>Save()</c>, the original entry is read and decompressed,
        /// then re-compressed and encrypted.  Conversely, if the original entry is
        /// encrypted with <c>PkzipWeak</c> encryption, and the application sets the
        /// <c>Encryption</c> property to <c>WinZipAes128</c>, then at the time of
        /// <c>Save()</c>, the original entry is decrypted via PKZIP encryption and
        /// decompressed, then re-compressed and re-encrypted with AES.  This all
        /// happens automatically within the library, but it can be time-consuming for
        /// large entries.
        /// </para>
        /// 
        /// <para>
        /// Additionally, when updating archives, it is not possible to change the
        /// password when changing the encryption algorithm.  To change both the
        /// algorithm and the password, you need to Save() the zipfile twice.  First
        /// set the <c>Encryption</c> to None, then call <c>Save()</c>.  Then set the
        /// <c>Encryption</c> to the new value (not "None"), then call <c>Save()</c>
        /// once again.
        /// </para>
        /// 
        /// <para>
        /// The WinZip AES encryption algorithms are not supported on the .NET Compact
        /// Framework.
        /// </para>
        /// </remarks>
        /// 
        /// <example>
        /// <para>
        /// This example creates a zip archive that uses encryption, and then extracts
        /// entries from the archive.  When creating the zip archive, the ReadMe.txt
        /// file is zipped without using a password or encryption.  The other file
        /// uses encryption.
        /// </para>
        /// <code>
        /// // Create a zip archive with AES Encryption.
        /// using (ZipFile zip = new ZipFile())
        /// {
        /// zip.AddFile("ReadMe.txt")
        /// ZipEntry e1= zip.AddFile("2008-Regional-Sales-Report.pdf");
        /// e1.Encryption= EncryptionAlgorithm.WinZipAes256;
        /// e1.Password= "Top.Secret.No.Peeking!";
        /// zip.Save("EncryptedArchive.zip");
        /// }
        /// 
        /// // Extract a zip archive that uses AES Encryption.
        /// // You do not need to specify the algorithm during extraction.
        /// using (ZipFile zip = ZipFile.Read("EncryptedArchive.zip"))
        /// {
        /// // Specify the password that is used during extraction, for
        /// // all entries that require a password:
        /// zip.Password= "Top.Secret.No.Peeking!";
        /// zip.ExtractAll("extractDirectory");
        /// }
        /// </code>
        /// 
        /// <code lang="VB">
        /// ' Create a zip that uses Encryption.
        /// Using zip As New ZipFile()
        /// zip.AddFile("ReadMe.txt")
        /// Dim e1 as ZipEntry
        /// e1= zip.AddFile("2008-Regional-Sales-Report.pdf")
        /// e1.Encryption= EncryptionAlgorithm.WinZipAes256
        /// e1.Password= "Top.Secret.No.Peeking!"
        /// zip.Save("EncryptedArchive.zip")
        /// End Using
        /// 
        /// ' Extract a zip archive that uses AES Encryption.
        /// ' You do not need to specify the algorithm during extraction.
        /// Using (zip as ZipFile = ZipFile.Read("EncryptedArchive.zip"))
        /// ' Specify the password that is used during extraction, for
        /// ' all entries that require a password:
        /// zip.Password= "Top.Secret.No.Peeking!"
        /// zip.ExtractAll("extractDirectory")
        /// End Using
        /// </code>
        /// 
        /// </example>
        /// 
        /// <exception cref="T:System.InvalidOperationException">
        /// Thrown in the setter if EncryptionAlgorithm.Unsupported is specified.
        /// </exception>
        /// 
        /// <seealso cref="P:ZipEntry.Password">ZipEntry.Password</seealso>
        /// <seealso cref="P:ZipFile.Encryption">ZipFile.Encryption</seealso>
        public EncryptionAlgorithm Encryption
        {
            get
            {
                return this.encryptionField;
            }
            set
            {
                if (value != this.encryptionField)
                {
                    if (value == EncryptionAlgorithm.Unsupported)
                    {
                        throw new InvalidOperationException("You may not set Encryption to that value.");
                    }
                    this.encryptionField = value;
                    this.restreamRequiredOnSaveField = true;
                    if (this.containerField.ZipFile != null)
                    {
                        this.containerField.ZipFile.NotifyEntryChanged();
                    }
                }
            }
        }

        /// <summary>
        /// The action the library should take when extracting a file that already exists.
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// This property affects the behavior of the Extract methods (one of the
        /// <c>Extract()</c> or <c>ExtractWithPassword()</c> overloads), when
        /// extraction would would overwrite an existing filesystem file. If you do
        /// not set this property, the library throws an exception when extracting
        /// an entry would overwrite an existing file.
        /// </para>
        /// 
        /// <para>
        /// This property has no effect when extracting to a stream, or when the file to be
        /// extracted does not already exist.
        /// </para>
        /// 
        /// </remarks>
        /// <seealso cref="P:ZipFile.ExtractExistingFile" />
        /// 
        /// <example>
        /// This example shows how to set the <c>ExtractExistingFile</c> property in
        /// an <c>ExtractProgress</c> event, in response to user input. The
        /// <c>ExtractProgress</c> event is invoked if and only if the
        /// <c>ExtractExistingFile</c> property was previously set to
        /// <c>ExtractExistingFileAction.InvokeExtractProgressEvent</c>.
        /// <code lang="C#">
        /// public static void ExtractProgress(object sender, ExtractProgressEventArgs e)
        /// {
        /// if (e.EventType == ZipProgressEventType.Extracting_BeforeExtractEntry)
        /// Console.WriteLine("extract {0} ", e.CurrentEntry.FileName);
        /// 
        /// else if (e.EventType == ZipProgressEventType.Extracting_ExtractEntryWouldOverwrite)
        /// {
        /// ZipEntry entry = e.CurrentEntry;
        /// string response = null;
        /// // Ask the user if he wants overwrite the file
        /// do
        /// {
        /// Console.Write("Overwrite {0} in {1} ? (y/n/C) ", entry.FileName, e.ExtractLocation);
        /// response = Console.ReadLine();
        /// Console.WriteLine();
        /// 
        /// } while (response != null &amp;&amp; response[0]!='Y' &amp;&amp;
        /// response[0]!='N' &amp;&amp; response[0]!='C');
        /// 
        /// if  (response[0]=='C')
        /// e.Cancel = true;
        /// else if (response[0]=='Y')
        /// entry.ExtractExistingFile = ExtractExistingFileAction.OverwriteSilently;
        /// else
        /// entry.ExtractExistingFile= ExtractExistingFileAction.DoNotOverwrite;
        /// }
        /// }
        /// </code>
        /// </example>
        public ExtractExistingFileAction ExtractExistingFile { get; set; }

        internal long FileDataPosition
        {
            get
            {
                if (this.fileDataPositionField == -1L)
                {
                    this.SetFdpLoh();
                }
                return this.fileDataPositionField;
            }
        }

        /// <summary>
        /// The name of the file contained in the ZipEntry.
        /// </summary>
        /// 
        /// <remarks>
        /// 
        /// <para>
        /// This is the name of the entry in the <c>ZipFile</c> itself.  When creating
        /// a zip archive, if the <c>ZipEntry</c> has been created from a filesystem
        /// file, via a call to <see cref="M:ZipFile.AddFile(System.String,System.String)" /> or <see cref="M:ZipFile.AddItem(System.String,System.String)" />, or a related overload, the value
        /// of this property is derived from the name of that file. The
        /// <c>FileName</c> property does not include drive letters, and may include a
        /// different directory path, depending on the value of the
        /// <c>directoryPathInArchive</c> parameter used when adding the entry into
        /// the <c>ZipFile</c>.
        /// </para>
        /// 
        /// <para>
        /// In some cases there is no related filesystem file - for example when a
        /// <c>ZipEntry</c> is created using <see cref="M:ZipFile.AddEntry(System.String,System.String)" /> or one of the similar overloads.  In this case, the value of
        /// this property is derived from the fileName and the directory path passed
        /// to that method.
        /// </para>
        /// 
        /// <para>
        /// When reading a zip file, this property takes the value of the entry name
        /// as stored in the zip file. If you extract such an entry, the extracted
        /// file will take the name given by this property.
        /// </para>
        /// 
        /// <para>
        /// Applications can set this property when creating new zip archives or when
        /// reading existing archives. When setting this property, the actual value
        /// that is set will replace backslashes with forward slashes, in accordance
        /// with <see href="http://www.pkware.com/documents/casestudies/APPNOTE.TXT">the Zip
        /// specification</see>, for compatibility with Unix(tm) and ... get
        /// this.... Amiga!
        /// </para>
        /// 
        /// <para>
        /// If an application reads a <c>ZipFile</c> via <see cref="M:ZipFile.Read(System.String)" /> or a related overload, and then explicitly
        /// sets the FileName on an entry contained within the <c>ZipFile</c>, and
        /// then calls <see cref="M:ZipFile.Save" />, the application will effectively
        /// rename the entry within the zip archive.
        /// </para>
        /// 
        /// <para>
        /// If an application sets the value of <c>FileName</c>, then calls
        /// <c>Extract()</c> on the entry, the entry is extracted to a file using the
        /// newly set value as the filename.  The <c>FileName</c> value is made
        /// permanent in the zip archive only <em>after</em> a call to one of the
        /// <c>ZipFile.Save()</c> methods on the <c>ZipFile</c> that contains the
        /// ZipEntry.
        /// </para>
        /// 
        /// <para>
        /// If an application attempts to set the <c>FileName</c> to a value that
        /// would result in a duplicate entry in the <c>ZipFile</c>, an exception is
        /// thrown.
        /// </para>
        /// 
        /// <para>
        /// When a <c>ZipEntry</c> is contained within a <c>ZipFile</c>, applications
        /// cannot rename the entry within the context of a <c>foreach</c> (<c>For
        /// Each</c> in VB) loop, because of the way the <c>ZipFile</c> stores
        /// entries.  If you need to enumerate through all the entries and rename one
        /// or more of them, use <see cref="P:ZipFile.EntriesSorted">ZipFile.EntriesSorted</see> as the
        /// collection.  See also, <see cref="M:ZipFile.GetEnumerator">ZipFile.GetEnumerator()</see>.
        /// </para>
        /// 
        /// </remarks>
        public string FileName
        {
            get
            {
                return this.fileNameInArchiveField;
            }
            set
            {
                if (this.containerField.ZipFile == null)
                {
                    throw new ZipException("Cannot rename; this is not supported in ZipOutputStream/ZipInputStream.");
                }
                if (string.IsNullOrEmpty(value))
                {
                    throw new ZipException("The FileName must be non empty and non-null.");
                }
                string name = NameInArchive(value, null);
                if (!(this.fileNameInArchiveField == name))
                {
                    this.containerField.ZipFile.RemoveEntry(this);
                    this.containerField.ZipFile.InternalAddEntry(name, this);
                    this.fileNameInArchiveField = name;
                    this.containerField.ZipFile.NotifyEntryChanged();
                    this.metadataChangedField = true;
                }
            }
        }

        /// <summary>
        /// Indicates whether the entry was included in the most recent save.
        /// </summary>
        /// <remarks>
        /// An entry can be excluded or skipped from a save if there is an error
        /// opening or reading the entry.
        /// </remarks>
        /// <seealso cref="P:ZipEntry.ZipErrorAction" />
        public bool IncludedInMostRecentSave
        {
            get
            {
                return !this.skippedDuringSaveField;
            }
        }

        /// <summary>
        /// Provides a human-readable string with information about the ZipEntry.
        /// </summary>
        public string Info
        {
            get
            {
                StringBuilder builder = new StringBuilder();
                builder.Append(string.Format(CultureInfo.InvariantCulture, "          ZipEntry: {0}\n", this.FileName)).Append(string.Format(CultureInfo.InvariantCulture, "   Version Made By: {0}\n", this.versionMadeByField)).Append(string.Format(CultureInfo.InvariantCulture, " Needed to extract: {0}\n", this.VersionNeeded));
                if (this.isDirectoryField)
                {
                    builder.Append("        Entry type: directory\n");
                }
                else
                {
                    builder.Append(string.Format(CultureInfo.InvariantCulture, "         File type: {0}\n", this.isTextField ? "text" : "binary")).Append(string.Format(CultureInfo.InvariantCulture, "       Compression: {0}\n", this.CompressionMethod)).Append(string.Format(CultureInfo.InvariantCulture, "        Compressed: 0x{0:X}\n", this.CompressedSize)).Append(string.Format(CultureInfo.InvariantCulture, "      Uncompressed: 0x{0:X}\n", this.UncompressedSize)).Append(string.Format(CultureInfo.InvariantCulture, "             CRC32: 0x{0:X8}\n", this.crc32Field));
                }
                builder.Append(string.Format(CultureInfo.InvariantCulture, "       Disk Number: {0}\n", this.diskNumberField));
                if (this.relativeOffsetOfLocalHeaderField > 0xffffffffL)
                {
                    builder.Append(string.Format(CultureInfo.InvariantCulture, "   Relative Offset: 0x{0:X16}\n", this.relativeOffsetOfLocalHeaderField));
                }
                else
                {
                    builder.Append(string.Format(CultureInfo.InvariantCulture, "   Relative Offset: 0x{0:X8}\n", this.relativeOffsetOfLocalHeaderField));
                }
                builder.Append(string.Format(CultureInfo.InvariantCulture, "         Bit Field: 0x{0:X4}\n", this.bitFieldField)).Append(string.Format(CultureInfo.InvariantCulture, "        Encrypted?: {0}\n", this.sourceIsEncryptedField)).Append(string.Format(CultureInfo.InvariantCulture, "          Timeblob: 0x{0:X8}\n", this.timeBlobField)).Append(string.Format(CultureInfo.InvariantCulture, "              Time: {0}\n", SharedUtilities.PackedToDateTime(this.timeBlobField)));
                builder.Append(string.Format(CultureInfo.InvariantCulture, "         Is Zip64?: {0}\n", this.inputUsesZip64Field));
                if (!string.IsNullOrEmpty(this.commentField))
                {
                    builder.Append(string.Format(CultureInfo.InvariantCulture, "           Comment: {0}\n", this.commentField));
                }
                builder.Append("\n");
                return builder.ToString();
            }
        }

        /// <summary>
        /// The stream that provides content for the ZipEntry.
        /// </summary>
        /// 
        /// <remarks>
        /// 
        /// <para>
        /// The application can use this property to set the input stream for an
        /// entry on a just-in-time basis. Imagine a scenario where the application
        /// creates a <c>ZipFile</c> comprised of content obtained from hundreds of
        /// files, via calls to <c>AddFile()</c>. The DotNetZip library opens streams
        /// on these files on a just-in-time basis, only when writing the entry out to
        /// an external store within the scope of a <c>ZipFile.Save()</c> call.  Only
        /// one input stream is opened at a time, as each entry is being written out.
        /// </para>
        /// 
        /// <para>
        /// Now imagine a different application that creates a <c>ZipFile</c>
        /// with content obtained from hundreds of streams, added through <see cref="M:ZipFile.AddEntry(System.String,System.IO.Stream)" />.  Normally the
        /// application would supply an open stream to that call.  But when large
        /// numbers of streams are being added, this can mean many open streams at one
        /// time, unnecessarily.
        /// </para>
        /// 
        /// <para>
        /// To avoid this, call <see cref="M:ZipFile.AddEntry(System.String,OpenDelegate,CloseDelegate)" /> and specify delegates that open and close the stream at
        /// the time of Save.
        /// </para>
        /// 
        /// 
        /// <para>
        /// Setting the value of this property when the entry was not added from a
        /// stream (for example, when the <c>ZipEntry</c> was added with <see cref="M:ZipFile.AddFile(System.String)" /> or <see cref="M:ZipFile.AddDirectory(System.String)" />, or when the entry was added by
        /// reading an existing zip archive) will throw an exception.
        /// </para>
        /// 
        /// </remarks>
        public Stream InputStream
        {
            get
            {
                return this.sourceStreamField;
            }
            set
            {
                if (this.sourceField != ZipEntrySource.Stream)
                {
                    throw new ZipException("You must not set the input stream for this entry.");
                }
                this.sourceWasJitProvidedField = true;
                this.sourceStreamField = value;
            }
        }

        /// <summary>
        /// A flag indicating whether the InputStream was provided Just-in-time.
        /// </summary>
        /// 
        /// <remarks>
        /// 
        /// <para>
        /// When creating a zip archive, an application can obtain content for one or
        /// more of the <c>ZipEntry</c> instances from streams, using the <see cref="M:ZipFile.AddEntry(System.String,System.IO.Stream)" /> method.  At the time
        /// of calling that method, the application can supply null as the value of
        /// the stream parameter.  By doing so, the application indicates to the
        /// library that it will provide a stream for the entry on a just-in-time
        /// basis, at the time one of the <c>ZipFile.Save()</c> methods is called and
        /// the data for the various entries are being compressed and written out.
        /// </para>
        /// 
        /// <para>
        /// In this case, the application can set the <see cref="P:ZipEntry.InputStream" />
        /// property, typically within the SaveProgress event (event type: <see cref="F:ZipProgressEventType.Saving_BeforeWriteEntry" />) for that entry.
        /// </para>
        /// 
        /// <para>
        /// The application will later want to call Close() and Dispose() on that
        /// stream.  In the SaveProgress event, when the event type is <see cref="F:ZipProgressEventType.Saving_AfterWriteEntry" />, the application can
        /// do so.  This flag indicates that the stream has been provided by the
        /// application on a just-in-time basis and that it is the application's
        /// responsibility to call Close/Dispose on that stream.
        /// </para>
        /// 
        /// </remarks>
        /// <seealso cref="P:ZipEntry.InputStream" />
        public bool InputStreamWasJitProvided
        {
            get
            {
                return this.sourceWasJitProvidedField;
            }
        }

        internal bool IsChanged
        {
            get
            {
                return (this.restreamRequiredOnSaveField | this.metadataChangedField);
            }
        }

        /// <summary>
        /// True if the entry is a directory (not a file).
        /// This is a readonly property on the entry.
        /// </summary>
        public bool IsDirectory
        {
            get
            {
                return this.isDirectoryField;
            }
        }

        /// <summary>
        /// Indicates whether an entry is marked as a text file. Be careful when
        /// using on this property. Unless you have a good reason, you should
        /// probably ignore this property.
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// The ZIP format includes a provision for specifying whether an entry in
        /// the zip archive is a text or binary file.  This property exposes that
        /// metadata item. Be careful when using this property: It's not clear
        /// that this property as a firm meaning, across tools and libraries.
        /// </para>
        /// 
        /// <para>
        /// To be clear, when reading a zip file, the property value may or may
        /// not be set, and its value may or may not be valid.  Not all entries
        /// that you may think of as "text" entries will be so marked, and entries
        /// marked as "text" are not guaranteed in any way to be text entries.
        /// Whether the value is set and set correctly depends entirely on the
        /// application that produced the zip file.
        /// </para>
        /// 
        /// <para>
        /// There are many zip tools available, and when creating zip files, some
        /// of them "respect" the IsText metadata field, and some of them do not.
        /// Unfortunately, even when an application tries to do "the right thing",
        /// it's not always clear what "the right thing" is.
        /// </para>
        /// 
        /// <para>
        /// There's no firm definition of just what it means to be "a text file",
        /// and the zip specification does not help in this regard. Twenty years
        /// ago, text was ASCII, each byte was less than 127. IsText meant, all
        /// bytes in the file were less than 127.  These days, it is not the case
        /// that all text files have all bytes less than 127.  Any unicode file
        /// may have bytes that are above 0x7f.  The zip specification has nothing
        /// to say on this topic. Therefore, it's not clear what IsText really
        /// means.
        /// </para>
        /// 
        /// <para>
        /// This property merely tells a reading application what is stored in the
        /// metadata for an entry, without guaranteeing its validity or its
        /// meaning.
        /// </para>
        /// 
        /// <para>
        /// When DotNetZip is used to create a zipfile, it attempts to set this
        /// field "correctly." For example, if a file ends in ".txt", this field
        /// will be set. Your application may override that default setting.  When
        /// writing a zip file, you must set the property before calling
        /// <c>Save()</c> on the ZipFile.
        /// </para>
        /// 
        /// <para>
        /// When reading a zip file, a more general way to decide just what kind
        /// of file is contained in a particular entry is to use the file type
        /// database stored in the operating system.  The operating system stores
        /// a table that says, a file with .jpg extension is a JPG image file, a
        /// file with a .xml extension is an XML document, a file with a .txt is a
        /// pure ASCII text document, and so on.  To get this information on
        /// Windows, <see href="http://www.codeproject.com/KB/cs/GetFileTypeAndIcon.aspx"> you
        /// need to read and parse the registry.</see> </para>
        /// </remarks>
        /// 
        /// <example>
        /// <code>
        /// using (var zip = new ZipFile())
        /// {
        /// var e = zip.UpdateFile("Descriptions.mme", "");
        /// e.IsText = true;
        /// zip.Save(zipPath);
        /// }
        /// </code>
        /// 
        /// <code lang="VB">
        /// Using zip As New ZipFile
        /// Dim e2 as ZipEntry = zip.AddFile("Descriptions.mme", "")
        /// e.IsText= True
        /// zip.Save(zipPath)
        /// End Using
        /// </code>
        /// </example>
        public bool IsText
        {
            get
            {
                return this.isTextField;
            }
            set
            {
                this.isTextField = value;
            }
        }

        /// <summary>
        /// The time and date at which the file indicated by the <c>ZipEntry</c> was
        /// last modified.
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// The DotNetZip library sets the LastModified value for an entry, equal to
        /// the Last Modified time of the file in the filesystem.  If an entry is
        /// added from a stream, the library uses <c>System.DateTime.Now</c> for this
        /// value, for the given entry.
        /// </para>
        /// 
        /// <para>
        /// This property allows the application to retrieve and possibly set the
        /// LastModified value on an entry, to an arbitrary value.  <see cref="T:System.DateTime" /> values with a <see cref="T:System.DateTimeKind" />
        /// setting of <c>DateTimeKind.Unspecified</c> are taken to be expressed as
        /// <c>DateTimeKind.Local</c>.
        /// </para>
        /// 
        /// <para>
        /// Be aware that because of the way <see href="http://www.pkware.com/documents/casestudies/APPNOTE.TXT">PKWare's
        /// Zip specification</see> describes how times are stored in the zip file,
        /// the full precision of the <c>System.DateTime</c> datatype is not stored
        /// for the last modified time when saving zip files.  For more information on
        /// how times are formatted, see the PKZip specification.
        /// </para>
        /// 
        /// <para>
        /// The actual last modified time of a file can be stored in multiple ways in
        /// the zip file, and they are not mutually exclusive:
        /// </para>
        /// 
        /// <list type="bullet">
        /// <item>
        /// In the so-called "DOS" format, which has a 2-second precision. Values
        /// are rounded to the nearest even second. For example, if the time on the
        /// file is 12:34:43, then it will be stored as 12:34:44. This first value
        /// is accessible via the <c>LastModified</c> property. This value is always
        /// present in the metadata for each zip entry.  In some cases the value is
        /// invalid, or zero.
        /// </item>
        /// 
        /// <item>
        /// In the so-called "Windows" or "NTFS" format, as an 8-byte integer
        /// quantity expressed as the number of 1/10 milliseconds (in other words
        /// the number of 100 nanosecond units) since January 1, 1601 (UTC).  This
        /// format is how Windows represents file times.  This time is accessible
        /// via the <c>ModifiedTime</c> property.
        /// </item>
        /// 
        /// <item>
        /// In the "Unix" format, a 4-byte quantity specifying the number of seconds since
        /// January 1, 1970 UTC.
        /// </item>
        /// 
        /// <item>
        /// In an older format, now deprecated but still used by some current
        /// tools. This format is also a 4-byte quantity specifying the number of
        /// seconds since January 1, 1970 UTC.
        /// </item>
        /// 
        /// </list>
        /// 
        /// <para>
        /// Zip tools and libraries will always at least handle (read or write) the
        /// DOS time, and may also handle the other time formats.  Keep in mind that
        /// while the names refer to particular operating systems, there is nothing in
        /// the time formats themselves that prevents their use on other operating
        /// systems.
        /// </para>
        /// 
        /// <para>
        /// When reading ZIP files, the DotNetZip library reads the Windows-formatted
        /// time, if it is stored in the entry, and sets both <c>LastModified</c> and
        /// <c>ModifiedTime</c> to that value. When writing ZIP files, the DotNetZip
        /// library by default will write both time quantities. It can also emit the
        /// Unix-formatted time if desired (See <see cref="P:ZipEntry.EmitTimesInUnixFormatWhenSaving" />.)
        /// </para>
        /// 
        /// <para>
        /// The last modified time of the file created upon a call to
        /// <c>ZipEntry.Extract()</c> may be adjusted during extraction to compensate
        /// for differences in how the .NET Base Class Library deals with daylight
        /// saving time (DST) versus how the Windows filesystem deals with daylight
        /// saving time.  Raymond Chen <see href="http://blogs.msdn.com/oldnewthing/archive/2003/10/24/55413.aspx">provides
        /// some good context</see>.
        /// </para>
        /// 
        /// <para>
        /// In a nutshell: Daylight savings time rules change regularly.  In 2007, for
        /// example, the inception week of DST changed.  In 1977, DST was in place all
        /// year round. In 1945, likewise.  And so on.  Win32 does not attempt to
        /// guess which time zone rules were in effect at the time in question.  It
        /// will render a time as "standard time" and allow the app to change to DST
        /// as necessary.  .NET makes a different choice.
        /// </para>
        /// 
        /// <para>
        /// Compare the output of FileInfo.LastWriteTime.ToString("f") with what you
        /// see in the Windows Explorer property sheet for a file that was last
        /// written to on the other side of the DST transition. For example, suppose
        /// the file was last modified on October 17, 2003, during DST but DST is not
        /// currently in effect. Explorer's file properties reports Thursday, October
        /// 17, 2003, 8:45:38 AM, but .NETs FileInfo reports Thursday, October 17,
        /// 2003, 9:45 AM.
        /// </para>
        /// 
        /// <para>
        /// Win32 says, "Thursday, October 17, 2002 8:45:38 AM PST". Note: Pacific
        /// STANDARD Time. Even though October 17 of that year occurred during Pacific
        /// Daylight Time, Win32 displays the time as standard time because that's
        /// what time it is NOW.
        /// </para>
        /// 
        /// <para>
        /// .NET BCL assumes that the current DST rules were in place at the time in
        /// question.  So, .NET says, "Well, if the rules in effect now were also in
        /// effect on October 17, 2003, then that would be daylight time" so it
        /// displays "Thursday, October 17, 2003, 9:45 AM PDT" - daylight time.
        /// </para>
        /// 
        /// <para>
        /// So .NET gives a value which is more intuitively correct, but is also
        /// potentially incorrect, and which is not invertible. Win32 gives a value
        /// which is intuitively incorrect, but is strictly correct.
        /// </para>
        /// 
        /// <para>
        /// Because of this funkiness, this library adds one hour to the LastModified
        /// time on the extracted file, if necessary.  That is to say, if the time in
        /// question had occurred in what the .NET Base Class Library assumed to be
        /// DST. This assumption may be wrong given the constantly changing DST rules,
        /// but it is the best we can do.
        /// </para>
        /// 
        /// </remarks>
        public DateTime LastModified
        {
            get
            {
                return this.lastModifiedField.ToLocalTime();
            }
            set
            {
                this.lastModifiedField = (value.Kind == DateTimeKind.Unspecified) ? DateTime.SpecifyKind(value, DateTimeKind.Local) : value.ToLocalTime();
                this.mtimeField = SharedUtilities.AdjustTime_Reverse(this.lastModifiedField).ToUniversalTime();
                this.metadataChangedField = true;
            }
        }

        private int LengthOfHeader
        {
            get
            {
                if (this.lengthOfHeaderField == 0)
                {
                    this.SetFdpLoh();
                }
                return this.lengthOfHeaderField;
            }
        }

        /// <summary>
        /// The name of the filesystem file, referred to by the ZipEntry.
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// This property specifies the thing-to-be-zipped on disk, and is set only
        /// when the <c>ZipEntry</c> is being created from a filesystem file.  If the
        /// <c>ZipFile</c> is instantiated by reading an existing .zip archive, then
        /// the LocalFileName will be <c>null</c> (<c>Nothing</c> in VB).
        /// </para>
        /// 
        /// <para>
        /// When it is set, the value of this property may be different than <see cref="P:ZipEntry.FileName" />, which is the path used in the archive itself.  If you
        /// call <c>Zip.AddFile("foop.txt", AlternativeDirectory)</c>, then the path
        /// used for the <c>ZipEntry</c> within the zip archive will be different
        /// than this path.
        /// </para>
        /// 
        /// <para>
        /// If the entry is being added from a stream, then this is null (Nothing in VB).
        /// </para>
        /// 
        /// </remarks>
        /// <seealso cref="P:ZipEntry.FileName" />
        internal string LocalFileName
        {
            get
            {
                return this.localFileNameField;
            }
        }

        /// <summary>
        /// Last Modified time for the file represented by the entry.
        /// </summary>
        /// 
        /// <remarks>
        /// 
        /// <para>
        /// This value corresponds to the "last modified" time in the NTFS file times
        /// as described in <see href="http://www.pkware.com/documents/casestudies/APPNOTE.TXT">the Zip
        /// specification</see>.  When getting this property, the value may be
        /// different from <see cref="P:ZipEntry.LastModified" />.  When setting the property,
        /// the <see cref="P:ZipEntry.LastModified" /> property also gets set, but with a lower
        /// precision.
        /// </para>
        /// 
        /// <para>
        /// Let me explain. It's going to take a while, so get
        /// comfortable. Originally, waaaaay back in 1989 when the ZIP specification
        /// was originally described by the esteemed Mr. Phil Katz, the dominant
        /// operating system of the time was MS-DOS. MSDOS stored file times with a
        /// 2-second precision, because, c'mon, <em>who is ever going to need better
        /// resolution than THAT?</em> And so ZIP files, regardless of the platform on
        /// which the zip file was created, store file times in exactly <see href="http://www.vsft.com/hal/dostime.htm">the same format that DOS used
        /// in 1989</see>.
        /// </para>
        /// 
        /// <para>
        /// Since then, the ZIP spec has evolved, but the internal format for file
        /// timestamps remains the same.  Despite the fact that the way times are
        /// stored in a zip file is rooted in DOS heritage, any program on any
        /// operating system can format a time in this way, and most zip tools and
        /// libraries DO - they round file times to the nearest even second and store
        /// it just like DOS did 25+ years ago.
        /// </para>
        /// 
        /// <para>
        /// PKWare extended the ZIP specification to allow a zip file to store what
        /// are called "NTFS Times" and "Unix(tm) times" for a file.  These are the
        /// <em>last write</em>, <em>last access</em>, and <em>file creation</em>
        /// times of a particular file. These metadata are not actually specific
        /// to NTFS or Unix. They are tracked for each file by NTFS and by various
        /// Unix filesystems, but they are also tracked by other filesystems, too.
        /// The key point is that the times are <em>formatted in the zip file</em>
        /// in the same way that NTFS formats the time (ticks since win32 epoch),
        /// or in the same way that Unix formats the time (seconds since Unix
        /// epoch). As with the DOS time, any tool or library running on any
        /// operating system is capable of formatting a time in one of these ways
        /// and embedding it into the zip file.
        /// </para>
        /// 
        /// <para>
        /// These extended times are higher precision quantities than the DOS time.
        /// As described above, the (DOS) LastModified has a precision of 2 seconds.
        /// The Unix time is stored with a precision of 1 second. The NTFS time is
        /// stored with a precision of 0.0000001 seconds. The quantities are easily
        /// convertible, except for the loss of precision you may incur.
        /// </para>
        /// 
        /// <para>
        /// A zip archive can store the {C,A,M} times in NTFS format, in Unix format,
        /// or not at all.  Often a tool running on Unix or Mac will embed the times
        /// in Unix format (1 second precision), while WinZip running on Windows might
        /// embed the times in NTFS format (precision of of 0.0000001 seconds).  When
        /// reading a zip file with these "extended" times, in either format,
        /// DotNetZip represents the values with the
        /// <c>ModifiedTime</c>, <c>AccessedTime</c> and <c>CreationTime</c>
        /// properties on the <c>ZipEntry</c>.
        /// </para>
        /// 
        /// <para>
        /// While any zip application or library, regardless of the platform it
        /// runs on, could use any of the time formats allowed by the ZIP
        /// specification, not all zip tools or libraries do support all these
        /// formats.  Storing the higher-precision times for each entry is
        /// optional for zip files, and many tools and libraries don't use the
        /// higher precision quantities at all. The old DOS time, represented by
        /// <see cref="P:ZipEntry.LastModified" />, is guaranteed to be present, though it
        /// sometimes unset.
        /// </para>
        /// 
        /// <para>
        /// Ok, getting back to the question about how the <c>LastModified</c>
        /// property relates to this <c>ModifiedTime</c>
        /// property... <c>LastModified</c> is always set, while
        /// <c>ModifiedTime</c> is not. (The other times stored in the <em>NTFS
        /// times extension</em>, <c>CreationTime</c> and <c>AccessedTime</c> also
        /// may not be set on an entry that is read from an existing zip file.)
        /// When reading a zip file, then <c>LastModified</c> takes the DOS time
        /// that is stored with the file. If the DOS time has been stored as zero
        /// in the zipfile, then this library will use <c>DateTime.Now</c> for the
        /// <c>LastModified</c> value.  If the ZIP file was created by an evolved
        /// tool, then there will also be higher precision NTFS or Unix times in
        /// the zip file.  In that case, this library will read those times, and
        /// set <c>LastModified</c> and <c>ModifiedTime</c> to the same value, the
        /// one corresponding to the last write time of the file.  If there are no
        /// higher precision times stored for the entry, then <c>ModifiedTime</c>
        /// remains unset (likewise <c>AccessedTime</c> and <c>CreationTime</c>),
        /// and <c>LastModified</c> keeps its DOS time.
        /// </para>
        /// 
        /// <para>
        /// When creating zip files with this library, by default the extended time
        /// properties (<c>ModifiedTime</c>, <c>AccessedTime</c>, and
        /// <c>CreationTime</c>) are set on the ZipEntry instance, and these data are
        /// stored in the zip archive for each entry, in NTFS format. If you add an
        /// entry from an actual filesystem file, then the entry gets the actual file
        /// times for that file, to NTFS-level precision.  If you add an entry from a
        /// stream, or a string, then the times get the value <c>DateTime.Now</c>.  In
        /// this case <c>LastModified</c> and <c>ModifiedTime</c> will be identical,
        /// to 2 seconds of precision.  You can explicitly set the
        /// <c>CreationTime</c>, <c>AccessedTime</c>, and <c>ModifiedTime</c> of an
        /// entry using the property setters.  If you want to set all of those
        /// quantities, it's more efficient to use the <see cref="M:ZipEntry.SetEntryTimes(System.DateTime,System.DateTime,System.DateTime)" /> method.  Those
        /// changes are not made permanent in the zip file until you call <see cref="M:ZipFile.Save" /> or one of its cousins.
        /// </para>
        /// 
        /// <para>
        /// When creating a zip file, you can override the default behavior of
        /// this library for formatting times in the zip file, disabling the
        /// embedding of file times in NTFS format or enabling the storage of file
        /// times in Unix format, or both.  You may want to do this, for example,
        /// when creating a zip file on Windows, that will be consumed on a Mac,
        /// by an application that is not hip to the "NTFS times" format. To do
        /// this, use the <see cref="P:ZipEntry.EmitTimesInWindowsFormatWhenSaving" /> and
        /// <see cref="P:ZipEntry.EmitTimesInUnixFormatWhenSaving" /> properties.  A valid zip
        /// file may store the file times in both formats.  But, there are no
        /// guarantees that a program running on Mac or Linux will gracefully
        /// handle the NTFS-formatted times when Unix times are present, or that a
        /// non-DotNetZip-powered application running on Windows will be able to
        /// handle file times in Unix format. DotNetZip will always do something
        /// reasonable; other libraries or tools may not. When in doubt, test.
        /// </para>
        /// 
        /// <para>
        /// I'll bet you didn't think one person could type so much about time, eh?
        /// And reading it was so enjoyable, too!  Well, in appreciation, <see href="http://cheeso.members.winisp.net/DotNetZipDonate.aspx">maybe you
        /// should donate</see>?
        /// </para>
        /// </remarks>
        /// 
        /// <seealso cref="P:ZipEntry.AccessedTime" />
        /// <seealso cref="P:ZipEntry.CreationTime" />
        /// <seealso cref="P:ZipEntry.LastModified" />
        /// <seealso cref="M:ZipEntry.SetEntryTimes(System.DateTime,System.DateTime,System.DateTime)" />
        public DateTime ModifiedTime
        {
            get
            {
                return this.mtimeField;
            }
            set
            {
                this.SetEntryTimes(this.ctimeField, this.atimeField, value);
            }
        }

        /// <summary>
        /// Indicates whether the entry actually used ZIP64 extensions, as it was most
        /// recently written to the output file or stream.
        /// </summary>
        /// 
        /// <remarks>
        /// 
        /// <para>
        /// This Nullable property is null (Nothing in VB) until a <c>Save()</c>
        /// method on the containing <see cref="T:ZipFile" /> instance has been
        /// called. <c>HasValue</c> is true only after a <c>Save()</c> method has been
        /// called.
        /// </para>
        /// 
        /// <para>
        /// The value of this property for a particular <c>ZipEntry</c> may change
        /// over successive calls to <c>Save()</c> methods on the containing ZipFile,
        /// even if the file that corresponds to the <c>ZipEntry</c> does not. This
        /// may happen if other entries contained in the <c>ZipFile</c> expand,
        /// causing the offset for this particular entry to exceed 0xFFFFFFFF.
        /// </para>
        /// </remarks>
        /// <seealso cref="P:ZipEntry.RequiresZip64" />
        public bool? OutputUsedZip64
        {
            get
            {
                return this.outputUsesZip64Field;
            }
        }

        /// <summary>
        /// The Password to be used when encrypting a <c>ZipEntry</c> upon
        /// <c>ZipFile.Save()</c>, or when decrypting an entry upon Extract().
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// This is a write-only property on the entry. Set this to request that the
        /// entry be encrypted when writing the zip archive, or set it to specify the
        /// password to be used when extracting an existing entry that is encrypted.
        /// </para>
        /// 
        /// <para>
        /// The password set here is implicitly used to encrypt the entry during the
        /// <see cref="M:ZipFile.Save" /> operation, or to decrypt during the <see cref="M:ZipEntry.Extract" /> or <see cref="M:ZipEntry.OpenReader" /> operation.  If you set
        /// the Password on a <c>ZipEntry</c> after calling <c>Save()</c>, there is no
        /// effect.
        /// </para>
        /// 
        /// <para>
        /// Consider setting the <see cref="P:ZipEntry.Encryption" /> property when using a
        /// password. Answering concerns that the standard password protection
        /// supported by all zip tools is weak, WinZip has extended the ZIP
        /// specification with a way to use AES Encryption to protect entries in the
        /// Zip file. Unlike the "PKZIP 2.0" encryption specified in the PKZIP
        /// specification, <see href="http://en.wikipedia.org/wiki/Advanced_Encryption_Standard">AES
        /// Encryption</see> uses a standard, strong, tested, encryption
        /// algorithm. DotNetZip can create zip archives that use WinZip-compatible
        /// AES encryption, if you set the <see cref="P:ZipEntry.Encryption" /> property. But,
        /// archives created that use AES encryption may not be readable by all other
        /// tools and libraries. For example, Windows Explorer cannot read a
        /// "compressed folder" (a zip file) that uses AES encryption, though it can
        /// read a zip file that uses "PKZIP encryption."
        /// </para>
        /// 
        /// <para>
        /// The <see cref="T:ZipFile" /> class also has a <see cref="P:ZipFile.Password" />
        /// property.  This property takes precedence over any password set on the
        /// ZipFile itself.  Typically, you would use the per-entry Password when most
        /// entries in the zip archive use one password, and a few entries use a
        /// different password.  If all entries in the zip file use the same password,
        /// then it is simpler to just set this property on the ZipFile itself,
        /// whether creating a zip archive or extracting a zip archive.
        /// </para>
        /// 
        /// <para>
        /// Some comments on updating archives: If you read a <c>ZipFile</c>, you
        /// cannot modify the password on any encrypted entry, except by extracting
        /// the entry with the original password (if any), removing the original entry
        /// via <see cref="M:ZipFile.RemoveEntry(ZipEntry)" />, and then adding a new
        /// entry with a new Password.
        /// </para>
        /// 
        /// <para>
        /// For example, suppose you read a <c>ZipFile</c>, and there is an encrypted
        /// entry.  Setting the Password property on that <c>ZipEntry</c> and then
        /// calling <c>Save()</c> on the <c>ZipFile</c> does not update the password
        /// on that entry in the archive.  Neither is an exception thrown. Instead,
        /// what happens during the <c>Save()</c> is the existing entry is copied
        /// through to the new zip archive, in its original encrypted form. Upon
        /// re-reading that archive, the entry can be decrypted with its original
        /// password.
        /// </para>
        /// 
        /// <para>
        /// If you read a ZipFile, and there is an un-encrypted entry, you can set the
        /// <c>Password</c> on the entry and then call Save() on the ZipFile, and get
        /// encryption on that entry.
        /// </para>
        /// 
        /// </remarks>
        /// 
        /// <example>
        /// <para>
        /// This example creates a zip file with two entries, and then extracts the
        /// entries from the zip file.  When creating the zip file, the two files are
        /// added to the zip file using password protection. Each entry uses a
        /// different password.  During extraction, each file is extracted with the
        /// appropriate password.
        /// </para>
        /// <code>
        /// // create a file with encryption
        /// using (ZipFile zip = new ZipFile())
        /// {
        /// ZipEntry entry;
        /// entry= zip.AddFile("Declaration.txt");
        /// entry.Password= "123456!";
        /// entry = zip.AddFile("Report.xls");
        /// entry.Password= "1Secret!";
        /// zip.Save("EncryptedArchive.zip");
        /// }
        /// 
        /// // extract entries that use encryption
        /// using (ZipFile zip = ZipFile.Read("EncryptedArchive.zip"))
        /// {
        /// ZipEntry entry;
        /// entry = zip["Declaration.txt"];
        /// entry.Password = "123456!";
        /// entry.Extract("extractDir");
        /// entry = zip["Report.xls"];
        /// entry.Password = "1Secret!";
        /// entry.Extract("extractDir");
        /// }
        /// 
        /// </code>
        /// 
        /// <code lang="VB">
        /// Using zip As New ZipFile
        /// Dim entry as ZipEntry
        /// entry= zip.AddFile("Declaration.txt")
        /// entry.Password= "123456!"
        /// entry = zip.AddFile("Report.xls")
        /// entry.Password= "1Secret!"
        /// zip.Save("EncryptedArchive.zip")
        /// End Using
        /// 
        /// 
        /// ' extract entries that use encryption
        /// Using (zip as ZipFile = ZipFile.Read("EncryptedArchive.zip"))
        /// Dim entry as ZipEntry
        /// entry = zip("Declaration.txt")
        /// entry.Password = "123456!"
        /// entry.Extract("extractDir")
        /// entry = zip("Report.xls")
        /// entry.Password = "1Secret!"
        /// entry.Extract("extractDir")
        /// End Using
        /// 
        /// </code>
        /// 
        /// </example>
        /// 
        /// <seealso cref="P:ZipEntry.Encryption" />
        /// <seealso cref="P:ZipFile.Password">ZipFile.Password</seealso>
        public string Password
        {
            private get
            {
                return this.passwordField;
            }
            set
            {
                this.passwordField = value;
                if (this.passwordField == null)
                {
                    this.encryptionField = EncryptionAlgorithm.None;
                }
                else
                {
                    if (!((this.sourceField != ZipEntrySource.ZipFile) || this.sourceIsEncryptedField))
                    {
                        this.restreamRequiredOnSaveField = true;
                    }
                    if (this.Encryption == EncryptionAlgorithm.None)
                    {
                        this.encryptionField = EncryptionAlgorithm.PkzipWeak;
                    }
                }
            }
        }

        /// <summary>
        /// Indicates whether the entry requires ZIP64 extensions.
        /// </summary>
        /// 
        /// <remarks>
        /// 
        /// <para>
        /// This property is null (Nothing in VB) until a <c>Save()</c> method on the
        /// containing <see cref="T:ZipFile" /> instance has been called. The property is
        /// non-null (<c>HasValue</c> is true) only after a <c>Save()</c> method has
        /// been called.
        /// </para>
        /// 
        /// <para>
        /// After the containing <c>ZipFile</c> has been saved, the Value of this
        /// property is true if any of the following three conditions holds: the
        /// uncompressed size of the entry is larger than 0xFFFFFFFF; the compressed
        /// size of the entry is larger than 0xFFFFFFFF; the relative offset of the
        /// entry within the zip archive is larger than 0xFFFFFFFF.  These quantities
        /// are not known until a <c>Save()</c> is attempted on the zip archive and
        /// the compression is applied.
        /// </para>
        /// 
        /// <para>
        /// If none of the three conditions holds, then the <c>Value</c> is false.
        /// </para>
        /// 
        /// <para>
        /// A <c>Value</c> of false does not indicate that the entry, as saved in the
        /// zip archive, does not use ZIP64.  It merely indicates that ZIP64 is
        /// <em>not required</em>.  An entry may use ZIP64 even when not required if
        /// the <see cref="P:ZipFile.UseZip64WhenSaving" /> property on the containing
        /// <c>ZipFile</c> instance is set to <see cref="F:Zip64Option.Always" />, or if
        /// the <see cref="P:ZipFile.UseZip64WhenSaving" /> property on the containing
        /// <c>ZipFile</c> instance is set to <see cref="F:Zip64Option.AsNecessary" />
        /// and the output stream was not seekable.
        /// </para>
        /// 
        /// </remarks>
        /// <seealso cref="P:ZipEntry.OutputUsedZip64" />
        public bool? RequiresZip64
        {
            get
            {
                return this.entryRequiresZip64Field;
            }
        }

        /// <summary>
        /// A callback that allows the application to specify the compression to use
        /// for a given entry that is about to be added to the zip archive.
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// See <see cref="P:ZipFile.SetCompression" />
        /// </para>
        /// </remarks>
        public SetCompressionCallback SetCompression { get; set; }

        /// <summary>
        /// An enum indicating the source of the ZipEntry.
        /// </summary>
        public ZipEntrySource Source
        {
            get
            {
                return this.sourceField;
            }
        }

        /// <summary>
        /// The type of timestamp attached to the ZipEntry.
        /// </summary>
        /// 
        /// <remarks>
        /// This property is valid only for a ZipEntry that was read from a zip archive.
        /// It indicates the type of timestamp attached to the entry.
        /// </remarks>
        /// 
        /// <seealso cref="P:ZipEntry.EmitTimesInWindowsFormatWhenSaving" />
        /// <seealso cref="P:ZipEntry.EmitTimesInUnixFormatWhenSaving" />
        public ZipEntryTimestamp Timestamp
        {
            get
            {
                return this.timestampField;
            }
        }

        /// <summary>
        /// The size of the file, in bytes, before compression, or after extraction.
        /// </summary>
        /// 
        /// <remarks>
        /// When reading a <c>ZipFile</c>, this value is read in from the existing
        /// zip file. When creating or updating a <c>ZipFile</c>, the uncompressed
        /// size is computed during compression.  Therefore the value on a
        /// <c>ZipEntry</c> is valid after a call to <c>Save()</c> (or one of its
        /// overloads) in that case.
        /// </remarks>
        /// 
        /// <seealso cref="P:ZipEntry.CompressedSize" />
        public long UncompressedSize
        {
            get
            {
                return this.uncompressedSizeField;
            }
        }

        private string UnsupportedAlgorithm
        {
            get
            {
                switch (this.unsupportedAlgorithmIdField)
                {
                    case 0x660e:
                        return "PKWare AES128";

                    case 0x660f:
                        return "PKWare AES192";

                    case 0x6610:
                        return "PKWare AES256";

                    case 0x6609:
                        return "3DES-112";

                    case 0x6601:
                        return "DES";

                    case 0x6602:
                        return "RC2";

                    case 0x6603:
                        return "3DES-168";

                    case 0:
                        return "--";

                    case 0x6720:
                        return "Blowfish";

                    case 0x6721:
                        return "Twofish";

                    case 0x6702:
                        return "RC2";

                    case 0x6801:
                        return "RC4";
                }
                return string.Format(CultureInfo.InvariantCulture, "Unknown (0x{0:X4})", this.unsupportedAlgorithmIdField);
            }
        }

        private string UnsupportedCompressionMethod
        {
            get
            {
                switch (this.compressionMethodField)
                {
                    case 0x13:
                        return "LZ77";

                    case 0x62:
                        return "PPMd";

                    case 0:
                        return "Store";

                    case 1:
                        return "Shrink";

                    case 8:
                        return "DEFLATE";

                    case 9:
                        return "Deflate64";

                    case 12:
                        return "BZIP2";

                    case 14:
                        return "LZMA";
                }
                return string.Format(CultureInfo.InvariantCulture, "Unknown (0x{0:X4})", this.compressionMethodField);
            }
        }

        /// <summary>
        /// A derived property that is <c>true</c> if the entry uses encryption.
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// This is a readonly property on the entry.  When reading a zip file,
        /// the value for the <c>ZipEntry</c> is determined by the data read
        /// from the zip file.  After saving a ZipFile, the value of this
        /// property for each <c>ZipEntry</c> indicates whether encryption was
        /// actually used (which will have been true if the <see cref="P:ZipEntry.Password" /> was set and the <see cref="P:ZipEntry.Encryption" /> property
        /// was something other than <see cref="F:EncryptionAlgorithm.None" />.
        /// </para>
        /// </remarks>
        public bool UsesEncryption
        {
            get
            {
                return (this.encryptionFromZipFileField != EncryptionAlgorithm.None);
            }
        }

        /// <summary>
        /// The version of the zip engine needed to read the ZipEntry.
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// This is a readonly property, indicating the version of <a href="http://www.pkware.com/documents/casestudies/APPNOTE.TXT">the Zip
        /// specification</a> that the extracting tool or library must support to
        /// extract the given entry.  Generally higher versions indicate newer
        /// features.  Older zip engines obviously won't know about new features, and
        /// won't be able to extract entries that depend on those newer features.
        /// </para>
        /// 
        /// <list type="table">
        /// <listheader>
        /// <term>value</term>
        /// <description>Features</description>
        /// </listheader>
        /// 
        /// <item>
        /// <term>20</term>
        /// <description>a basic Zip Entry, potentially using PKZIP encryption.
        /// </description>
        /// </item>
        /// 
        /// <item>
        /// <term>45</term>
        /// <description>The ZIP64 extension is used on the entry.
        /// </description>
        /// </item>
        /// 
        /// <item>
        /// <term>46</term>
        /// <description> File is compressed using BZIP2 compression*</description>
        /// </item>
        /// 
        /// <item>
        /// <term>50</term>
        /// <description> File is encrypted using PkWare's DES, 3DES, (broken) RC2 or RC4</description>
        /// </item>
        /// 
        /// <item>
        /// <term>51</term>
        /// <description> File is encrypted using PKWare's AES encryption or corrected RC2 encryption.</description>
        /// </item>
        /// 
        /// <item>
        /// <term>52</term>
        /// <description> File is encrypted using corrected RC2-64 encryption**</description>
        /// </item>
        /// 
        /// <item>
        /// <term>61</term>
        /// <description> File is encrypted using non-OAEP key wrapping***</description>
        /// </item>
        /// 
        /// <item>
        /// <term>63</term>
        /// <description> File is compressed using LZMA, PPMd+, Blowfish, or Twofish</description>
        /// </item>
        /// 
        /// </list>
        /// 
        /// <para>
        /// There are other values possible, not listed here. DotNetZip supports
        /// regular PKZip encryption, and ZIP64 extensions.  DotNetZip cannot extract
        /// entries that require a zip engine higher than 45.
        /// </para>
        /// 
        /// <para>
        /// This value is set upon reading an existing zip file, or after saving a zip
        /// archive.
        /// </para>
        /// </remarks>
        public short VersionNeeded
        {
            get
            {
                return this.versionNeededField;
            }
        }

        /// <summary>
        /// The action to take when an error is encountered while
        /// opening or reading files as they are saved into a zip archive.
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// Errors can occur within a call to <see cref="M:ZipFile.Save">ZipFile.Save</see>, as the various files contained
        /// in a ZipFile are being saved into the zip archive.  During the
        /// <c>Save</c>, DotNetZip will perform a <c>File.Open</c> on the file
        /// associated to the ZipEntry, and then will read the entire contents of
        /// the file as it is zipped. Either the open or the Read may fail, because
        /// of lock conflicts or other reasons.  Using this property, you can
        /// specify the action to take when such errors occur.
        /// </para>
        /// 
        /// <para>
        /// Typically you will NOT set this property on individual ZipEntry
        /// instances.  Instead, you will set the <see cref="P:ZipFile.ZipErrorAction">ZipFile.ZipErrorAction</see> property on
        /// the ZipFile instance, before adding any entries to the
        /// <c>ZipFile</c>. If you do this, errors encountered on behalf of any of
        /// the entries in the ZipFile will be handled the same way.
        /// </para>
        /// 
        /// <para>
        /// But, if you use a <see cref="E:ZipFile.ZipError" /> handler, you will want
        /// to set this property on the <c>ZipEntry</c> within the handler, to
        /// communicate back to DotNetZip what you would like to do with the
        /// particular error.
        /// </para>
        /// 
        /// </remarks>
        /// <seealso cref="P:ZipFile.ZipErrorAction" />
        /// <seealso cref="E:ZipFile.ZipError" />
        public ZipErrorAction ZipErrorAction { get; set; }

        private class CopyHelper
        {
            private static int callCount = 0;
            private static Regex re = new Regex(@" \(copy (\d+)\)$");

            internal static string AppendCopyToFileName(string f)
            {
                Match match;
                string str;
                callCount++;
                if (callCount > 0x19)
                {
                    throw new OverflowException("overflow while creating filename");
                }
                int num = 1;
                int length = f.LastIndexOf(".");
                if (length == -1)
                {
                    match = re.Match(f);
                    if (match.Success)
                    {
                        num = int.Parse(match.Groups[1].Value) + 1;
                        str = string.Format(CultureInfo.InvariantCulture, " (copy {0})", num);
                        f = f.Substring(0, match.Index) + str;
                        return f;
                    }
                    str = string.Format(CultureInfo.InvariantCulture, " (copy {0})", num);
                    f = f + str;
                    return f;
                }
                match = re.Match(f.Substring(0, length));
                if (match.Success)
                {
                    num = int.Parse(match.Groups[1].Value) + 1;
                    str = string.Format(CultureInfo.InvariantCulture, " (copy {0})", num);
                    f = f.Substring(0, match.Index) + str + f.Substring(length);
                    return f;
                }
                str = string.Format(CultureInfo.InvariantCulture, " (copy {0})", num);
                f = f.Substring(0, length) + str + f.Substring(length);
                return f;
            }
        }

        private delegate T Func<T>();
    }
}

