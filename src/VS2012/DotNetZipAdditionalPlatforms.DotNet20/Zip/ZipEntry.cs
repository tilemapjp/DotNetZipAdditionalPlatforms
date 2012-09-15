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
    [Guid("ebc25cf6-9120-4283-b972-0e5520d00004"), ClassInterface(ClassInterfaceType.AutoDispatch), ComVisible(true)]
    public class ZipEntry
    {
        private long __FileDataPosition = -1L;
        private Encoding _actualEncoding;
        private WinZipAesCrypto _aesCrypto_forExtract;
        private WinZipAesCrypto _aesCrypto_forWrite;
        internal Stream _archiveStream;
        private DateTime _Atime;
        internal short _BitField;
        private CloseDelegate _CloseDelegate;
        internal string _Comment;
        private byte[] _CommentBytes;
        private short _commentLength;
        internal long _CompressedFileDataSize;
        internal long _CompressedSize;
        private DotNetZipAdditionalPlatforms.Zlib.CompressionLevel _CompressionLevel = DotNetZipAdditionalPlatforms.Zlib.CompressionLevel.Default;
        internal short _CompressionMethod = 8;
        private short _CompressionMethod_FromZipFile;
        internal ZipContainer _container;
        internal int _Crc32;
        private bool _crcCalculated;
        private DateTime _Ctime;
        private uint _diskNumber;
        private bool _emitNtfsTimes = true;
        private bool _emitUnixTimes;
        internal EncryptionAlgorithm _Encryption = EncryptionAlgorithm.None;
        internal EncryptionAlgorithm _Encryption_FromZipFile;
        private byte[] _EntryHeader;
        private bool? _entryRequiresZip64;
        private int _ExternalFileAttrs;
        internal byte[] _Extra;
        private short _extraFieldLength;
        private string _FileNameInArchive;
        private short _filenameLength;
        private long _future_ROLH;
        private Stream _inputDecryptorStream;
        internal bool _InputUsesZip64;
        private short _InternalFileAttrs;
        private bool _ioOperationCanceled;
        private bool _IsDirectory;
        private bool _IsText;
        internal DateTime _LastModified;
        private int _LengthOfHeader;
        private int _LengthOfTrailer;
        internal string _LocalFileName;
        private bool _metadataChanged;
        private DateTime _Mtime;
        private bool _ntfsTimesAreSet;
        private OpenDelegate _OpenDelegate;
        private object _outputLock = new object();
        private bool? _OutputUsesZip64;
        internal string _Password;
        private bool _presumeZip64;
        private int _readExtraDepth;
        internal long _RelativeOffsetOfLocalHeader;
        private bool _restreamRequiredOnSave;
        private bool _skippedDuringSave;
        internal ZipEntrySource _Source = ZipEntrySource.None;
        private bool _sourceIsEncrypted;
        private Stream _sourceStream;
        private long? _sourceStreamOriginalPosition;
        private bool _sourceWasJitProvided;
        internal int _TimeBlob;
        private ZipEntryTimestamp _timestamp;
        private long _TotalEntrySize;
        private bool _TrimVolumeFromFullyQualifiedPaths = true;
        internal long _UncompressedSize;
        private static DateTime _unixEpoch = new DateTime(0x7b2, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        private uint _UnsupportedAlgorithmId;
        private short _VersionMadeBy;
        internal short _VersionNeeded;
        internal byte[] _WeakEncryptionHeader;
        private static DateTime _win32Epoch = DateTime.FromFileTimeUtc(0L);
        private short _WinZipAesMethod;
        private DotNetZipAdditionalPlatforms.Zip.WriteDelegate _WriteDelegate;
        private static DateTime _zeroHour = new DateTime(1, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        private ZipCrypto _zipCrypto_forExtract;
        private ZipCrypto _zipCrypto_forWrite;
        private static Encoding ibm437 = Encoding.GetEncoding("IBM437");

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
                if (this._ntfsTimesAreSet)
                {
                    if (isFile)
                    {
                        if (File.Exists(fileOrDirectory))
                        {
                            File.SetCreationTimeUtc(fileOrDirectory, this._Ctime);
                            File.SetLastAccessTimeUtc(fileOrDirectory, this._Atime);
                            File.SetLastWriteTimeUtc(fileOrDirectory, this._Mtime);
                        }
                    }
                    else if (Directory.Exists(fileOrDirectory))
                    {
                        Directory.SetCreationTimeUtc(fileOrDirectory, this._Ctime);
                        Directory.SetLastAccessTimeUtc(fileOrDirectory, this._Atime);
                        Directory.SetLastWriteTimeUtc(fileOrDirectory, this._Mtime);
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
                if (this._Source == ZipEntrySource.WriteDelegate)
                {
                    this._WriteDelegate(this.FileName, stream);
                }
                else
                {
                    int num3;
                    byte[] buffer = new byte[this.BufferSize];
                    while ((num3 = SharedUtilities.ReadWithRetry(input, buffer, 0, buffer.Length, this.FileName)) != 0)
                    {
                        stream.Write(buffer, 0, num3);
                        this.OnWriteBlock(stream.TotalBytesSlurped, streamLength);
                        if (this._ioOperationCanceled)
                        {
                            break;
                        }
                    }
                }
                this.FinishOutputStream(s, stream2, stream3, stream4, stream);
            }
            finally
            {
                if (this._Source == ZipEntrySource.JitStream)
                {
                    if (this._CloseDelegate != null)
                    {
                        this._CloseDelegate(this.FileName, input);
                    }
                }
                else if (input is FileStream)
                {
                    input.Dispose();
                }
            }
            if (!this._ioOperationCanceled)
            {
                this.__FileDataPosition = position;
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
                    if (!this._ioOperationCanceled)
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
            if (!((this._container.Zip64 == Zip64Option.Always) ? false : ((this._container.Zip64 != Zip64Option.AsNecessary) ? true : (!forCentralDirectory ? false : !this._entryRequiresZip64.Value))))
            {
                int num = 4 + (forCentralDirectory ? 0x1c : 0x10);
                buffer = new byte[num];
                num2 = 0;
                if (this._presumeZip64 || forCentralDirectory)
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
                Array.Copy(BitConverter.GetBytes(this._UncompressedSize), 0, buffer, num2, 8);
                num2 += 8;
                Array.Copy(BitConverter.GetBytes(this._CompressedSize), 0, buffer, num2, 8);
                num2 += 8;
                if (forCentralDirectory)
                {
                    Array.Copy(BitConverter.GetBytes(this._RelativeOffsetOfLocalHeader), 0, buffer, num2, 8);
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
                buffer[num2++] = (byte) (this._CompressionMethod & 0xff);
                buffer[num2++] = (byte) (this._CompressionMethod & 0xff00);
                list.Add(buffer);
            }
            if (this._ntfsTimesAreSet && this._emitNtfsTimes)
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
                Array.Copy(BitConverter.GetBytes(this._Mtime.ToFileTime()), 0, buffer, num2, 8);
                num2 += 8;
                Array.Copy(BitConverter.GetBytes(this._Atime.ToFileTime()), 0, buffer, num2, 8);
                num2 += 8;
                Array.Copy(BitConverter.GetBytes(this._Ctime.ToFileTime()), 0, buffer, num2, 8);
                num2 += 8;
                list.Add(buffer);
            }
            if (this._ntfsTimesAreSet && this._emitUnixTimes)
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
                TimeSpan span = (TimeSpan) (this._Mtime - _unixEpoch);
                int totalSeconds = (int) span.TotalSeconds;
                Array.Copy(BitConverter.GetBytes(totalSeconds), 0, buffer, num2, 4);
                num2 += 4;
                if (!forCentralDirectory)
                {
                    span = (TimeSpan) (this._Atime - _unixEpoch);
                    totalSeconds = (int) span.TotalSeconds;
                    Array.Copy(BitConverter.GetBytes(totalSeconds), 0, buffer, num2, 4);
                    num2 += 4;
                    span = (TimeSpan) (this._Ctime - _unixEpoch);
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
            this.__FileDataPosition = source.__FileDataPosition;
            this.CompressionMethod = source.CompressionMethod;
            this._CompressionMethod_FromZipFile = source._CompressionMethod_FromZipFile;
            this._CompressedFileDataSize = source._CompressedFileDataSize;
            this._UncompressedSize = source._UncompressedSize;
            this._BitField = source._BitField;
            this._Source = source._Source;
            this._LastModified = source._LastModified;
            this._Mtime = source._Mtime;
            this._Atime = source._Atime;
            this._Ctime = source._Ctime;
            this._ntfsTimesAreSet = source._ntfsTimesAreSet;
            this._emitUnixTimes = source._emitUnixTimes;
            this._emitNtfsTimes = source._emitNtfsTimes;
        }

        private void CopyThroughOneEntry(Stream outStream)
        {
            if (this.LengthOfHeader == 0)
            {
                throw new BadStateException("Bad header length.");
            }
            if ((((this._metadataChanged || (this.ArchiveStream is ZipSegmentedStream)) || (outStream is ZipSegmentedStream)) || (this._InputUsesZip64 && (this._container.UseZip64WhenSaving == Zip64Option.Default))) || (!this._InputUsesZip64 && (this._container.UseZip64WhenSaving == Zip64Option.Always)))
            {
                this.CopyThroughWithRecompute(outStream);
            }
            else
            {
                this.CopyThroughWithNoChange(outStream);
            }
            this._entryRequiresZip64 = new bool?(((this._CompressedSize >= 0xffffffffL) || (this._UncompressedSize >= 0xffffffffL)) || (this._RelativeOffsetOfLocalHeader >= 0xffffffffL));
            this._OutputUsesZip64 = new bool?((this._container.Zip64 == Zip64Option.Always) ? true : this._entryRequiresZip64.Value);
        }

        private void CopyThroughWithNoChange(Stream outstream)
        {
            byte[] buffer = new byte[this.BufferSize];
            CountingStream stream = new CountingStream(this.ArchiveStream);
            stream.Seek(this._RelativeOffsetOfLocalHeader, SeekOrigin.Begin);
            if (this._TotalEntrySize == 0L)
            {
                this._TotalEntrySize = (this._LengthOfHeader + this._CompressedFileDataSize) + this._LengthOfTrailer;
            }
            CountingStream stream2 = outstream as CountingStream;
            this._RelativeOffsetOfLocalHeader = (stream2 != null) ? stream2.ComputedPosition : outstream.Position;
            long num2 = this._TotalEntrySize;
            while (num2 > 0L)
            {
                int count = (num2 > buffer.Length) ? buffer.Length : ((int) num2);
                int num = stream.Read(buffer, 0, count);
                outstream.Write(buffer, 0, num);
                num2 -= num;
                this.OnWriteBlock(stream.BytesRead, this._TotalEntrySize);
                if (this._ioOperationCanceled)
                {
                    break;
                }
            }
        }

        private void CopyThroughWithRecompute(Stream outstream)
        {
            byte[] buffer = new byte[this.BufferSize];
            CountingStream stream = new CountingStream(this.ArchiveStream);
            long num2 = this._RelativeOffsetOfLocalHeader;
            int lengthOfHeader = this.LengthOfHeader;
            this.WriteHeader(outstream, 0);
            this.StoreRelativeOffset();
            if (!this.FileName.EndsWith("/"))
            {
                long offset = num2 + lengthOfHeader;
                int lengthOfCryptoHeaderBytes = GetLengthOfCryptoHeaderBytes(this._Encryption_FromZipFile);
                offset -= lengthOfCryptoHeaderBytes;
                this._LengthOfHeader += lengthOfCryptoHeaderBytes;
                stream.Seek(offset, SeekOrigin.Begin);
                long num6 = this._CompressedSize;
                while (num6 > 0L)
                {
                    lengthOfCryptoHeaderBytes = (num6 > buffer.Length) ? buffer.Length : ((int) num6);
                    int count = stream.Read(buffer, 0, lengthOfCryptoHeaderBytes);
                    outstream.Write(buffer, 0, count);
                    num6 -= count;
                    this.OnWriteBlock(stream.BytesRead, this._CompressedSize);
                    if (this._ioOperationCanceled)
                    {
                        break;
                    }
                }
                if ((this._BitField & 8) == 8)
                {
                    int num7 = 0x10;
                    if (this._InputUsesZip64)
                    {
                        num7 += 8;
                    }
                    byte[] buffer2 = new byte[num7];
                    stream.Read(buffer2, 0, num7);
                    if (this._InputUsesZip64 && (this._container.UseZip64WhenSaving == Zip64Option.Default))
                    {
                        outstream.Write(buffer2, 0, 8);
                        if (this._CompressedSize > 0xffffffffL)
                        {
                            throw new InvalidOperationException("ZIP64 is required");
                        }
                        outstream.Write(buffer2, 8, 4);
                        if (this._UncompressedSize > 0xffffffffL)
                        {
                            throw new InvalidOperationException("ZIP64 is required");
                        }
                        outstream.Write(buffer2, 0x10, 4);
                        this._LengthOfTrailer -= 8;
                    }
                    else if (!(this._InputUsesZip64 || (this._container.UseZip64WhenSaving != Zip64Option.Always)))
                    {
                        byte[] buffer3 = new byte[4];
                        outstream.Write(buffer2, 0, 8);
                        outstream.Write(buffer2, 8, 4);
                        outstream.Write(buffer3, 0, 4);
                        outstream.Write(buffer2, 12, 4);
                        outstream.Write(buffer3, 0, 4);
                        this._LengthOfTrailer += 8;
                    }
                    else
                    {
                        outstream.Write(buffer2, 0, num7);
                    }
                }
            }
            this._TotalEntrySize = (this._LengthOfHeader + this._CompressedFileDataSize) + this._LengthOfTrailer;
        }

        private static ZipEntry Create(string nameInArchive, ZipEntrySource source, object arg1, object arg2)
        {
            if (string.IsNullOrEmpty(nameInArchive))
            {
                throw new ZipException("The entry name must be non-null and non-empty.");
            }
            ZipEntry entry = new ZipEntry();
            entry._VersionMadeBy = 0x2d;
            entry._Source = source;
            entry._Mtime = entry._Atime = entry._Ctime = DateTime.UtcNow;
            if (source == ZipEntrySource.Stream)
            {
                entry._sourceStream = arg1 as Stream;
            }
            else if (source == ZipEntrySource.WriteDelegate)
            {
                entry._WriteDelegate = arg1 as DotNetZipAdditionalPlatforms.Zip.WriteDelegate;
            }
            else if (source == ZipEntrySource.JitStream)
            {
                entry._OpenDelegate = arg1 as OpenDelegate;
                entry._CloseDelegate = arg2 as CloseDelegate;
            }
            else if (source != ZipEntrySource.ZipOutputStream)
            {
                if (source == ZipEntrySource.None)
                {
                    entry._Source = ZipEntrySource.FileSystem;
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
                        entry._Mtime = File.GetLastWriteTime(str).ToUniversalTime();
                        entry._Ctime = File.GetCreationTime(str).ToUniversalTime();
                        entry._Atime = File.GetLastAccessTime(str).ToUniversalTime();
                        if (File.Exists(str) || Directory.Exists(str))
                        {
                            entry._ExternalFileAttrs = (int) File.GetAttributes(str);
                        }
                        entry._ntfsTimesAreSet = true;
                        entry._LocalFileName = Path.GetFullPath(str);
                    }
                    catch (PathTooLongException exception)
                    {
                        throw new ZipException(string.Format(CultureInfo.InvariantCulture, "The path is too long, filename={0}", str), exception);
                    }
                }
            }
            entry._LastModified = entry._Mtime;
            entry._FileNameInArchive = SharedUtilities.NormalizePathForUseInZipFile(nameInArchive);
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

        internal static ZipEntry CreateForWriter(string entryName, DotNetZipAdditionalPlatforms.Zip.WriteDelegate d)
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
        /// <seealso cref="P:DotNetZipAdditionalPlatforms.Zip.ZipEntry.ExtractExistingFile" />
        /// <seealso cref="M:DotNetZipAdditionalPlatforms.Zip.ZipEntry.Extract(DotNetZipAdditionalPlatforms.Zip.ExtractExistingFileAction)" />
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
        /// existing file is determined by the <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipEntry.ExtractExistingFile" /> property.
        /// </para>
        /// 
        /// <para>
        /// Within the call to <c>Extract()</c>, the content for the entry is
        /// written into a filesystem file, and then the last modified time of the
        /// file is set according to the <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipEntry.LastModified" /> property on
        /// the entry. See the remarks the <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipEntry.LastModified" /> property for
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
        /// See the remarks on the <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipEntry.LastModified" /> property, for some
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
        /// <seealso cref="P:DotNetZipAdditionalPlatforms.Zip.ZipEntry.ExtractExistingFile" />
        /// <seealso cref="M:DotNetZipAdditionalPlatforms.Zip.ZipEntry.Extract(System.String,DotNetZipAdditionalPlatforms.Zip.ExtractExistingFileAction)" />
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
        /// files, see the <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipEntry.ExtractExistingFile" /> property, or call
        /// <see cref="M:DotNetZipAdditionalPlatforms.Zip.ZipEntry.Extract(System.String,DotNetZipAdditionalPlatforms.Zip.ExtractExistingFileAction)" />.
        /// </para>
        /// 
        /// <para>
        /// See the remarks on the <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipEntry.LastModified" /> property, for some
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
        /// See the remarks on the <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipEntry.LastModified" /> property, for some
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
                long num2 = (this._CompressionMethod_FromZipFile != 0) ? this.UncompressedSize : this._CompressedFileDataSize;
                this._inputDecryptorStream = this.GetExtractDecryptor(archiveStream);
                Stream extractDecompressor = this.GetExtractDecompressor(this._inputDecryptorStream);
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
                        if (this._ioOperationCanceled)
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
                    this._archiveStream = null;
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
        /// <seealso cref="P:DotNetZipAdditionalPlatforms.Zip.ZipEntry.ExtractExistingFile" />
        /// <seealso cref="M:DotNetZipAdditionalPlatforms.Zip.ZipEntry.ExtractWithPassword(DotNetZipAdditionalPlatforms.Zip.ExtractExistingFileAction,System.String)" />
        /// 
        /// <remarks>
        /// 
        /// <para>
        /// Existing entries in the filesystem will not be overwritten. If you
        /// would like to force the overwrite of existing files, see the <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipEntry.ExtractExistingFile" />property, or call
        /// <see cref="M:DotNetZipAdditionalPlatforms.Zip.ZipEntry.ExtractWithPassword(DotNetZipAdditionalPlatforms.Zip.ExtractExistingFileAction,System.String)" />.
        /// </para>
        /// 
        /// <para>
        /// See the remarks on the <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipEntry.LastModified" /> property for some
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
        /// See the remarks on the <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipEntry.LastModified" /> property, for some
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
        /// <seealso cref="P:DotNetZipAdditionalPlatforms.Zip.ZipEntry.ExtractExistingFile" />
        /// <seealso cref="M:DotNetZipAdditionalPlatforms.Zip.ZipEntry.ExtractWithPassword(System.String,DotNetZipAdditionalPlatforms.Zip.ExtractExistingFileAction,System.String)" />
        /// 
        /// <remarks>
        /// <para>
        /// Existing entries in the filesystem will not be overwritten. If you
        /// would like to force the overwrite of existing files, see the <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipEntry.ExtractExistingFile" />property, or call
        /// <see cref="M:DotNetZipAdditionalPlatforms.Zip.ZipEntry.ExtractWithPassword(DotNetZipAdditionalPlatforms.Zip.ExtractExistingFileAction,System.String)" />.
        /// </para>
        /// 
        /// <para>
        /// See the remarks on the <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipEntry.LastModified" /> property, for some
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
        /// See the remarks on the <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipEntry.LastModified" /> property, for some
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
            if (!this._crcCalculated)
            {
                Stream input = null;
                if (this._Source == ZipEntrySource.WriteDelegate)
                {
                    CrcCalculatorStream stream = new CrcCalculatorStream(Stream.Null);
                    this._WriteDelegate(this.FileName, stream);
                    this._Crc32 = stream.Crc;
                }
                else if (this._Source != ZipEntrySource.ZipFile)
                {
                    if (this._Source == ZipEntrySource.Stream)
                    {
                        this.PrepSourceStream();
                        input = this._sourceStream;
                    }
                    else if (this._Source == ZipEntrySource.JitStream)
                    {
                        if (this._sourceStream == null)
                        {
                            this._sourceStream = this._OpenDelegate(this.FileName);
                        }
                        this.PrepSourceStream();
                        input = this._sourceStream;
                    }
                    else if (this._Source != ZipEntrySource.ZipOutputStream)
                    {
                        input = File.Open(this.LocalFileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    }
                    this._Crc32 = new CRC32().GetCrc32(input);
                    if (this._sourceStream == null)
                    {
                        input.Dispose();
                    }
                }
                this._crcCalculated = true;
            }
            return this._Crc32;
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
                this._LengthOfTrailer = 0;
                this._UncompressedSize = output.TotalBytesSlurped;
                WinZipAesCipherStream stream = encryptor as WinZipAesCipherStream;
                if ((stream != null) && (this._UncompressedSize > 0L))
                {
                    s.Write(stream.FinalAuthentication, 0, 10);
                    this._LengthOfTrailer += 10;
                }
                this._CompressedFileDataSize = entryCounter.BytesWritten;
                this._CompressedSize = this._CompressedFileDataSize;
                this._Crc32 = output.Crc;
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
                    if ((this._Comment != null) && (this._Comment.Length != 0))
                    {
                        this._CommentBytes = ibm437.GetBytes(this._Comment);
                    }
                    this._actualEncoding = ibm437;
                    return ibm437.GetBytes(s);

                case ZipOption.Always:
                    if ((this._Comment != null) && (this._Comment.Length != 0))
                    {
                        this._CommentBytes = this.AlternateEncoding.GetBytes(this._Comment);
                    }
                    this._actualEncoding = this.AlternateEncoding;
                    return this.AlternateEncoding.GetBytes(s);
            }
            byte[] bytes = ibm437.GetBytes(s);
            string str2 = ibm437.GetString(bytes, 0, bytes.Length);
            this._CommentBytes = null;
            if (str2 != s)
            {
                bytes = this.AlternateEncoding.GetBytes(s);
                if ((this._Comment != null) && (this._Comment.Length != 0))
                {
                    this._CommentBytes = this.AlternateEncoding.GetBytes(this._Comment);
                }
                this._actualEncoding = this.AlternateEncoding;
                return bytes;
            }
            this._actualEncoding = ibm437;
            if ((this._Comment != null) && (this._Comment.Length != 0))
            {
                byte[] buffer2 = ibm437.GetBytes(this._Comment);
                if (ibm437.GetString(buffer2, 0, buffer2.Length) != this.Comment)
                {
                    bytes = this.AlternateEncoding.GetBytes(s);
                    this._CommentBytes = this.AlternateEncoding.GetBytes(this._Comment);
                    this._actualEncoding = this.AlternateEncoding;
                    return bytes;
                }
                this._CommentBytes = buffer2;
            }
            return bytes;
        }

        internal Stream GetExtractDecompressor(Stream input2)
        {
            switch (this._CompressionMethod_FromZipFile)
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
            if (this._Encryption_FromZipFile == EncryptionAlgorithm.PkzipWeak)
            {
                return new ZipCipherStream(input, this._zipCrypto_forExtract, CryptoMode.Decrypt);
            }
            if ((this._Encryption_FromZipFile == EncryptionAlgorithm.WinZipAes128) || (this._Encryption_FromZipFile == EncryptionAlgorithm.WinZipAes256))
            {
                return new WinZipAesCipherStream(input, this._aesCrypto_forExtract, this._CompressedFileDataSize, CryptoMode.Decrypt);
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
            if (((ulong) SharedUtilities.ReadInt(archiveStream)) == (ulong)entry._Crc32)
            {
                if (SharedUtilities.ReadInt(archiveStream) == entry._CompressedSize)
                {
                    if (SharedUtilities.ReadInt(archiveStream) != entry._UncompressedSize)
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
            if (this._container == null)
            {
                throw new BadStateException("This entry is an orphan");
            }
            if (this._container.ZipFile == null)
            {
                throw new InvalidOperationException("Use Extract() only with ZipFile.");
            }
            this._container.ZipFile.Reset(false);
            if (this._Source != ZipEntrySource.ZipFile)
            {
                throw new BadStateException("You must call ZipFile.Save before calling any Extract method");
            }
            this.OnBeforeExtract(baseDir);
            this._ioOperationCanceled = false;
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
                    string str2 = password ?? (this._Password ?? this._container.Password);
                    if (this._Encryption_FromZipFile != EncryptionAlgorithm.None)
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
                        else if (this._container.ZipFile != null)
                        {
                            flag2 = this._container.ZipFile._inExtractAll;
                        }
                        output = new FileStream(outFileName, FileMode.CreateNew);
                    }
                    else
                    {
                        this.WriteStatus("extract entry {0} to stream...", new object[] { this.FileName });
                        output = outstream;
                    }
                    if (!this._ioOperationCanceled)
                    {
                        int num2 = this.ExtractOne(output);
                        if (!this._ioOperationCanceled)
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
                                    if (this._container.ZipFile[str6] == null)
                                    {
                                        this._SetTimes(Path.GetDirectoryName(outFileName), false);
                                    }
                                }
                                if (((this._VersionMadeBy & 0xff00) == 0xa00) || ((this._VersionMadeBy & 0xff00) == 0))
                                {
                                    File.SetAttributes(outFileName, (FileAttributes) this._ExternalFileAttrs);
                                }
                            }
                            this.OnAfterExtract(baseDir);
                        }
                    }
                }
            }
            catch (Exception)
            {
                this._ioOperationCanceled = true;
                throw;
            }
            finally
            {
                if (this._ioOperationCanceled && (outFileName != null))
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
            if (this._Source != ZipEntrySource.ZipFile)
            {
                throw new BadStateException("You must call ZipFile.Save before calling OpenReader");
            }
            long length = (this._CompressionMethod_FromZipFile == 0) ? this._CompressedFileDataSize : this.UncompressedSize;
            Stream archiveStream = this.ArchiveStream;
            this.ArchiveStream.Seek(this.FileDataPosition, SeekOrigin.Begin);
            this._inputDecryptorStream = this.GetExtractDecryptor(archiveStream);
            return new CrcCalculatorStream(this.GetExtractDecompressor(this._inputDecryptorStream), length);
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
            this._IsDirectory = true;
            if (!this._FileNameInArchive.EndsWith("/"))
            {
                this._FileNameInArchive = this._FileNameInArchive + "/";
            }
        }

        private Stream MaybeApplyCompression(Stream s, long streamLength)
        {
            if ((this._CompressionMethod == 8) && (this.CompressionLevel != DotNetZipAdditionalPlatforms.Zlib.CompressionLevel.None))
            {
                if ((this._container.ParallelDeflateThreshold == 0L) || ((streamLength > this._container.ParallelDeflateThreshold) && (this._container.ParallelDeflateThreshold > 0L)))
                {
                    if (this._container.ParallelDeflater == null)
                    {
                        this._container.ParallelDeflater = new ParallelDeflateOutputStream(s, this.CompressionLevel, this._container.Strategy, true);
                        if (this._container.CodecBufferSize > 0)
                        {
                            this._container.ParallelDeflater.BufferSize = this._container.CodecBufferSize;
                        }
                        if (this._container.ParallelDeflateMaxBufferPairs > 0)
                        {
                            this._container.ParallelDeflater.MaxBufferPairs = this._container.ParallelDeflateMaxBufferPairs;
                        }
                    }
                    ParallelDeflateOutputStream parallelDeflater = this._container.ParallelDeflater;
                    parallelDeflater.Reset(s);
                    return parallelDeflater;
                }
                DeflateStream stream2 = new DeflateStream(s, CompressionMode.Compress, this.CompressionLevel, true);
                if (this._container.CodecBufferSize > 0)
                {
                    stream2.BufferSize = this._container.CodecBufferSize;
                }
                stream2.Strategy = this._container.Strategy;
                return stream2;
            }
            if (this._CompressionMethod == 12)
            {
                if ((this._container.ParallelDeflateThreshold == 0L) || ((streamLength > this._container.ParallelDeflateThreshold) && (this._container.ParallelDeflateThreshold > 0L)))
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
                return new ZipCipherStream(s, this._zipCrypto_forWrite, CryptoMode.Encrypt);
            }
            if ((this.Encryption == EncryptionAlgorithm.WinZipAes128) || (this.Encryption == EncryptionAlgorithm.WinZipAes256))
            {
                return new WinZipAesCipherStream(s, this._aesCrypto_forWrite, CryptoMode.Encrypt);
            }
            return s;
        }

        private void MaybeUnsetCompressionMethodForWriting(int cycle)
        {
            if (cycle > 1)
            {
                this._CompressionMethod = 0;
            }
            else if (this.IsDirectory)
            {
                this._CompressionMethod = 0;
            }
            else if (this._Source != ZipEntrySource.ZipFile)
            {
                if (this._Source == ZipEntrySource.Stream)
                {
                    if (((this._sourceStream != null) && this._sourceStream.CanSeek) && (this._sourceStream.Length == 0L))
                    {
                        this._CompressionMethod = 0;
                        return;
                    }
                }
                else if ((this._Source == ZipEntrySource.FileSystem) && (SharedUtilities.GetFileLength(this.LocalFileName) == 0L))
                {
                    this._CompressionMethod = 0;
                    return;
                }
                if (this.SetCompression != null)
                {
                    this.CompressionLevel = this.SetCompression(this.LocalFileName, this._FileNameInArchive);
                }
                if ((this.CompressionLevel == DotNetZipAdditionalPlatforms.Zlib.CompressionLevel.None) && (this.CompressionMethod == DotNetZipAdditionalPlatforms.Zip.CompressionMethod.Deflate))
                {
                    this._CompressionMethod = 0;
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
            if (((this._TrimVolumeFromFullyQualifiedPaths && (this.FileName.Length >= 3)) && (this.FileName[1] == ':')) && (str[2] == '/'))
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
            this._Encryption_FromZipFile = this._Encryption;
            this._CompressionMethod_FromZipFile = this._CompressionMethod;
            this._restreamRequiredOnSave = false;
            this._metadataChanged = false;
            this._Source = ZipEntrySource.ZipFile;
        }

        private void OnAfterExtract(string path)
        {
            if ((this._container.ZipFile != null) && !this._container.ZipFile._inExtractAll)
            {
                this._container.ZipFile.OnSingleEntryExtract(this, path, false);
            }
        }

        private void OnBeforeExtract(string path)
        {
            if ((this._container.ZipFile != null) && !this._container.ZipFile._inExtractAll)
            {
                this._ioOperationCanceled = this._container.ZipFile.OnSingleEntryExtract(this, path, true);
            }
        }

        private void OnExtractExisting(string path)
        {
            if (this._container.ZipFile != null)
            {
                this._ioOperationCanceled = this._container.ZipFile.OnExtractExisting(this, path);
            }
        }

        private void OnExtractProgress(long bytesWritten, long totalBytesToWrite)
        {
            if (this._container.ZipFile != null)
            {
                this._ioOperationCanceled = this._container.ZipFile.OnExtractBlock(this, bytesWritten, totalBytesToWrite);
            }
        }

        private void OnWriteBlock(long bytesXferred, long totalBytesToXfer)
        {
            if (this._container.ZipFile != null)
            {
                this._ioOperationCanceled = this._container.ZipFile.OnSaveBlock(this, bytesXferred, totalBytesToXfer);
            }
        }

        private void OnZipErrorWhileSaving(Exception e)
        {
            if (this._container.ZipFile != null)
            {
                this._ioOperationCanceled = this._container.ZipFile.OnZipErrorSaving(this, e);
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
        /// The return value is of type <see cref="T:DotNetZipAdditionalPlatforms.Crc.CrcCalculatorStream" />.  Use it as you would any
        /// stream for reading.  When an application calls <see cref="M:System.IO.Stream.Read(System.Byte[],System.Int32,System.Int32)" /> on that stream, it will
        /// receive data from the zip entry that is decrypted and decompressed
        /// as necessary.
        /// </para>
        /// 
        /// <para>
        /// <c>CrcCalculatorStream</c> adds one additional feature: it keeps a
        /// CRC32 checksum on the bytes of the stream as it is read.  The CRC
        /// value is available in the <see cref="P:DotNetZipAdditionalPlatforms.Crc.CrcCalculatorStream.Crc" /> property on the
        /// <c>CrcCalculatorStream</c>.  When the read is complete, your
        /// application
        /// <em>should</em> check this CRC against the <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipEntry.Crc" />
        /// property on the <c>ZipEntry</c> to validate the content of the
        /// ZipEntry. You don't have to validate the entry using the CRC, but
        /// you should, to verify integrity. Check the example for how to do
        /// this.
        /// </para>
        /// 
        /// <para>
        /// If the entry is protected with a password, then you need to provide
        /// a password prior to calling <see cref="M:DotNetZipAdditionalPlatforms.Zip.ZipEntry.OpenReader" />, either by
        /// setting the <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipEntry.Password" /> property on the entry, or the
        /// <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.Password" /> property on the <c>ZipFile</c>
        /// itself. Or, you can use <see cref="M:DotNetZipAdditionalPlatforms.Zip.ZipEntry.OpenReader(System.String)" />, the
        /// overload of OpenReader that accepts a password parameter.
        /// </para>
        /// 
        /// <para>
        /// If you want to extract entry data into a write-able stream that is
        /// already opened, like a <see cref="T:System.IO.FileStream" />, do not
        /// use this method. Instead, use <see cref="M:DotNetZipAdditionalPlatforms.Zip.ZipEntry.Extract(System.IO.Stream)" />.
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
        /// an exception if the ZipEntry is obtained from a <see cref="T:DotNetZipAdditionalPlatforms.Zip.ZipInputStream" />.
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
        /// using (DotNetZipAdditionalPlatforms.Zlib.CrcCalculatorStream s = e1.OpenReader())
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
        /// Using s As DotNetZipAdditionalPlatforms.Zlib.CrcCalculatorStream = e1.OpenReader
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
        /// <seealso cref="M:DotNetZipAdditionalPlatforms.Zip.ZipEntry.Extract(System.IO.Stream)" />
        /// <returns>The Stream for reading.</returns>
        public CrcCalculatorStream OpenReader()
        {
            if (this._container.ZipFile == null)
            {
                throw new InvalidOperationException("Use OpenReader() only with ZipFile.");
            }
            return this.InternalOpenReader(this._Password ?? this._container.Password);
        }

        /// <summary>
        /// Opens a readable stream for an encrypted zip entry in the archive.
        /// The stream decompresses and decrypts as necessary, as it is read.
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// See the documentation on the <see cref="M:DotNetZipAdditionalPlatforms.Zip.ZipEntry.OpenReader" /> method for
        /// full details. This overload allows the application to specify a
        /// password for the <c>ZipEntry</c> to be read.
        /// </para>
        /// </remarks>
        /// 
        /// <param name="password">The password to use for decrypting the entry.</param>
        /// <returns>The Stream for reading.</returns>
        public CrcCalculatorStream OpenReader(string password)
        {
            if (this._container.ZipFile == null)
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
            if ((this._UncompressedSize == 0L) && (this._CompressedSize == 0L))
            {
                if (this._Source == ZipEntrySource.ZipOutputStream)
                {
                    return;
                }
                if (this._Password != null)
                {
                    int num = 0;
                    if (this.Encryption == EncryptionAlgorithm.PkzipWeak)
                    {
                        num = 12;
                    }
                    else if ((this.Encryption == EncryptionAlgorithm.WinZipAes128) || (this.Encryption == EncryptionAlgorithm.WinZipAes256))
                    {
                        num = this._aesCrypto_forWrite._Salt.Length + this._aesCrypto_forWrite.GeneratedPV.Length;
                    }
                    if (!((this._Source != ZipEntrySource.ZipOutputStream) || s.CanSeek))
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
                        this._LengthOfHeader -= num;
                        this.__FileDataPosition -= num;
                    }
                    this._Password = null;
                    this._BitField = (short) (this._BitField & -2);
                    num2 = 6;
                    this._EntryHeader[num2++] = (byte) (this._BitField & 0xff);
                    this._EntryHeader[num2++] = (byte) ((this._BitField & 0xff00) >> 8);
                    if ((this.Encryption == EncryptionAlgorithm.WinZipAes128) || (this.Encryption == EncryptionAlgorithm.WinZipAes256))
                    {
                        short num3 = (short) (this._EntryHeader[0x1a] + (this._EntryHeader[0x1b] * 0x100));
                        int offx = 30 + num3;
                        int num5 = FindExtraFieldSegment(this._EntryHeader, offx, 0x9901);
                        if (num5 >= 0)
                        {
                            this._EntryHeader[num5++] = 0x99;
                            this._EntryHeader[num5++] = 0x99;
                        }
                    }
                }
                this.CompressionMethod = DotNetZipAdditionalPlatforms.Zip.CompressionMethod.None;
                this.Encryption = EncryptionAlgorithm.None;
            }
            else if ((this._zipCrypto_forWrite != null) || (this._aesCrypto_forWrite != null))
            {
                if (this.Encryption == EncryptionAlgorithm.PkzipWeak)
                {
                    this._CompressedSize += 12L;
                }
                else if ((this.Encryption == EncryptionAlgorithm.WinZipAes128) || (this.Encryption == EncryptionAlgorithm.WinZipAes256))
                {
                    this._CompressedSize += this._aesCrypto_forWrite.SizeOfEncryptionMetadata;
                }
            }
            int destinationIndex = 8;
            this._EntryHeader[destinationIndex++] = (byte) (this._CompressionMethod & 0xff);
            this._EntryHeader[destinationIndex++] = (byte) ((this._CompressionMethod & 0xff00) >> 8);
            destinationIndex = 14;
            this._EntryHeader[destinationIndex++] = (byte) (this._Crc32 & 0xff);
            this._EntryHeader[destinationIndex++] = (byte) ((this._Crc32 & 0xff00) >> 8);
            this._EntryHeader[destinationIndex++] = (byte) ((this._Crc32 & 0xff0000) >> 0x10);
            this._EntryHeader[destinationIndex++] = (byte) ((this._Crc32 & 0xff000000L) >> 0x18);
            this.SetZip64Flags();
            short num7 = (short) (this._EntryHeader[0x1a] + (this._EntryHeader[0x1b] * 0x100));
            short num8 = (short) (this._EntryHeader[0x1c] + (this._EntryHeader[0x1d] * 0x100));
            if (this._OutputUsesZip64.Value)
            {
                this._EntryHeader[4] = 0x2d;
                this._EntryHeader[5] = 0;
                for (num2 = 0; num2 < 8; num2++)
                {
                    this._EntryHeader[destinationIndex++] = 0xff;
                }
                destinationIndex = 30 + num7;
                this._EntryHeader[destinationIndex++] = 1;
                this._EntryHeader[destinationIndex++] = 0;
                destinationIndex += 2;
                Array.Copy(BitConverter.GetBytes(this._UncompressedSize), 0, this._EntryHeader, destinationIndex, 8);
                destinationIndex += 8;
                Array.Copy(BitConverter.GetBytes(this._CompressedSize), 0, this._EntryHeader, destinationIndex, 8);
            }
            else
            {
                this._EntryHeader[4] = 20;
                this._EntryHeader[5] = 0;
                destinationIndex = 0x12;
                this._EntryHeader[destinationIndex++] = (byte) (this._CompressedSize & 0xffL);
                this._EntryHeader[destinationIndex++] = (byte) ((this._CompressedSize & 0xff00L) >> 8);
                this._EntryHeader[destinationIndex++] = (byte) ((this._CompressedSize & 0xff0000L) >> 0x10);
                this._EntryHeader[destinationIndex++] = (byte) ((this._CompressedSize & 0xff000000L) >> 0x18);
                this._EntryHeader[destinationIndex++] = (byte) (this._UncompressedSize & 0xffL);
                this._EntryHeader[destinationIndex++] = (byte) ((this._UncompressedSize & 0xff00L) >> 8);
                this._EntryHeader[destinationIndex++] = (byte) ((this._UncompressedSize & 0xff0000L) >> 0x10);
                this._EntryHeader[destinationIndex++] = (byte) ((this._UncompressedSize & 0xff000000L) >> 0x18);
                if (num8 != 0)
                {
                    destinationIndex = 30 + num7;
                    num9 = (short) (this._EntryHeader[destinationIndex + 2] + (this._EntryHeader[destinationIndex + 3] * 0x100));
                    if (num9 == 0x10)
                    {
                        this._EntryHeader[destinationIndex++] = 0x99;
                        this._EntryHeader[destinationIndex++] = 0x99;
                    }
                }
            }
            if ((this.Encryption == EncryptionAlgorithm.WinZipAes128) || (this.Encryption == EncryptionAlgorithm.WinZipAes256))
            {
                destinationIndex = 8;
                this._EntryHeader[destinationIndex++] = 0x63;
                this._EntryHeader[destinationIndex++] = 0;
                destinationIndex = 30 + num7;
                do
                {
                    ushort num10 = (ushort) (this._EntryHeader[destinationIndex] + (this._EntryHeader[destinationIndex + 1] * 0x100));
                    num9 = (short) (this._EntryHeader[destinationIndex + 2] + (this._EntryHeader[destinationIndex + 3] * 0x100));
                    if (num10 != 0x9901)
                    {
                        destinationIndex += num9 + 4;
                    }
                    else
                    {
                        destinationIndex += 9;
                        this._EntryHeader[destinationIndex++] = (byte) (this._CompressionMethod & 0xff);
                        this._EntryHeader[destinationIndex++] = (byte) (this._CompressionMethod & 0xff00);
                    }
                }
                while (destinationIndex < ((num8 - 30) - num7));
            }
            if (((this._BitField & 8) != 8) || ((this._Source == ZipEntrySource.ZipOutputStream) && s.CanSeek))
            {
                ZipSegmentedStream stream2 = s as ZipSegmentedStream;
                if ((stream2 != null) && (this._diskNumber != stream2.CurrentSegment))
                {
                    using (Stream stream3 = ZipSegmentedStream.ForUpdate(this._container.ZipFile.Name, this._diskNumber))
                    {
                        stream3.Seek(this._RelativeOffsetOfLocalHeader, SeekOrigin.Begin);
                        stream3.Write(this._EntryHeader, 0, this._EntryHeader.Length);
                    }
                }
                else
                {
                    s.Seek(this._RelativeOffsetOfLocalHeader, SeekOrigin.Begin);
                    s.Write(this._EntryHeader, 0, this._EntryHeader.Length);
                    if (stream != null)
                    {
                        stream.Adjust((long) this._EntryHeader.Length);
                    }
                    s.Seek(this._CompressedSize, SeekOrigin.Current);
                }
            }
            if (((this._BitField & 8) == 8) && !this.IsDirectory)
            {
                byte[] destinationArray = new byte[0x10 + (this._OutputUsesZip64.Value ? 8 : 0)];
                destinationIndex = 0;
                Array.Copy(BitConverter.GetBytes(0x8074b50), 0, destinationArray, destinationIndex, 4);
                destinationIndex += 4;
                Array.Copy(BitConverter.GetBytes(this._Crc32), 0, destinationArray, destinationIndex, 4);
                destinationIndex += 4;
                if (this._OutputUsesZip64.Value)
                {
                    Array.Copy(BitConverter.GetBytes(this._CompressedSize), 0, destinationArray, destinationIndex, 8);
                    destinationIndex += 8;
                    Array.Copy(BitConverter.GetBytes(this._UncompressedSize), 0, destinationArray, destinationIndex, 8);
                    destinationIndex += 8;
                }
                else
                {
                    destinationArray[destinationIndex++] = (byte) (this._CompressedSize & 0xffL);
                    destinationArray[destinationIndex++] = (byte) ((this._CompressedSize & 0xff00L) >> 8);
                    destinationArray[destinationIndex++] = (byte) ((this._CompressedSize & 0xff0000L) >> 0x10);
                    destinationArray[destinationIndex++] = (byte) ((this._CompressedSize & 0xff000000L) >> 0x18);
                    destinationArray[destinationIndex++] = (byte) (this._UncompressedSize & 0xffL);
                    destinationArray[destinationIndex++] = (byte) ((this._UncompressedSize & 0xff00L) >> 8);
                    destinationArray[destinationIndex++] = (byte) ((this._UncompressedSize & 0xff0000L) >> 0x10);
                    destinationArray[destinationIndex++] = (byte) ((this._UncompressedSize & 0xff000000L) >> 0x18);
                }
                s.Write(destinationArray, 0, destinationArray.Length);
                this._LengthOfTrailer += destinationArray.Length;
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
            if (this._sourceStream == null)
            {
                throw new ZipException(string.Format(CultureInfo.InvariantCulture, "The input stream is null for entry '{0}'.", this.FileName));
            }
            if (this._sourceStreamOriginalPosition.HasValue)
            {
                this._sourceStream.Position = this._sourceStreamOriginalPosition.Value;
            }
            else if (this._sourceStream.CanSeek)
            {
                this._sourceStreamOriginalPosition = new long?(this._sourceStream.Position);
            }
            else if ((this.Encryption == EncryptionAlgorithm.PkzipWeak) && ((this._Source != ZipEntrySource.ZipFile) && ((this._BitField & 8) != 8)))
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
                byte[] buffer = this._Extra = new byte[extraFieldLength];
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
            this._Mtime = _unixEpoch.AddSeconds((double) num);
            j += 4;
            num = BitConverter.ToInt32(buffer, j);
            this._Atime = _unixEpoch.AddSeconds((double) num);
            j += 4;
            this._Ctime = DateTime.UtcNow;
            this._ntfsTimesAreSet = true;
            this._timestamp |= ZipEntryTimestamp.InfoZip1;
            return j;
        }

        private int ProcessExtraFieldPkwareStrongEncryption(byte[] Buffer, int j)
        {
            j += 2;
            this._UnsupportedAlgorithmId = (ushort) (Buffer[j++] + (Buffer[j++] * 0x100));
            this._Encryption_FromZipFile = this._Encryption = EncryptionAlgorithm.Unsupported;
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
                return _unixEpoch.AddSeconds((double) num);
            };
            if ((dataSize == 13) || (this._readExtraDepth > 0))
            {
                byte num = buffer[j++];
                remainingData--;
                if (((num & 1) != 0) && (remainingData >= 4))
                {
                    this._Mtime = func();
                }
                this._Atime = (((num & 2) != 0) && (remainingData >= 4)) ? func() : DateTime.UtcNow;
                this._Ctime = (((num & 4) != 0) && (remainingData >= 4)) ? func() : DateTime.UtcNow;
                this._timestamp |= ZipEntryTimestamp.Unix;
                this._ntfsTimesAreSet = true;
                this._emitUnixTimes = true;
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
                this._Mtime = DateTime.FromFileTimeUtc(fileTime);
                j += 8;
                fileTime = BitConverter.ToInt64(buffer, j);
                this._Atime = DateTime.FromFileTimeUtc(fileTime);
                j += 8;
                fileTime = BitConverter.ToInt64(buffer, j);
                this._Ctime = DateTime.FromFileTimeUtc(fileTime);
                j += 8;
                this._ntfsTimesAreSet = true;
                this._timestamp |= ZipEntryTimestamp.Windows;
                this._emitNtfsTimes = true;
            }
            return j;
        }

        private int ProcessExtraFieldWinZipAes(byte[] buffer, int j, short dataSize, long posn)
        {
            if (this._CompressionMethod == 0x63)
            {
                if ((this._BitField & 1) != 1)
                {
                    throw new BadReadException(string.Format(CultureInfo.InvariantCulture, "  Inconsistent metadata at position 0x{0:X16}", posn));
                }
                this._sourceIsEncrypted = true;
                if (dataSize != 7)
                {
                    throw new BadReadException(string.Format(CultureInfo.InvariantCulture, "  Inconsistent size (0x{0:X4}) in WinZip AES field at position 0x{1:X16}", dataSize, posn));
                }
                this._WinZipAesMethod = BitConverter.ToInt16(buffer, j);
                j += 2;
                if ((this._WinZipAesMethod != 1) && (this._WinZipAesMethod != 2))
                {
                    throw new BadReadException(string.Format(CultureInfo.InvariantCulture, "  Unexpected vendor version number (0x{0:X4}) for WinZip AES metadata at position 0x{1:X16}", this._WinZipAesMethod, posn));
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
                this._Encryption_FromZipFile = this._Encryption = (num2 == 0x80) ? EncryptionAlgorithm.WinZipAes128 : EncryptionAlgorithm.WinZipAes256;
                j++;
                this._CompressionMethod_FromZipFile = this._CompressionMethod = BitConverter.ToInt16(buffer, j);
                j += 2;
            }
            return j;
        }

        private int ProcessExtraFieldZip64(byte[] buffer, int j, short dataSize, long posn)
        {
            this._InputUsesZip64 = true;
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
            if (this._UncompressedSize == 0xffffffffL)
            {
                this._UncompressedSize = func();
            }
            if (this._CompressedSize == 0xffffffffL)
            {
                this._CompressedSize = func();
            }
            if (this._RelativeOffsetOfLocalHeader == 0xffffffffL)
            {
                this._RelativeOffsetOfLocalHeader = func();
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
            entry._Source = ZipEntrySource.ZipFile;
            entry._container = new ZipContainer(zf);
            entry._VersionMadeBy = (short) (buffer[num4++] + (buffer[num4++] * 0x100));
            entry._VersionNeeded = (short) (buffer[num4++] + (buffer[num4++] * 0x100));
            entry._BitField = (short) (buffer[num4++] + (buffer[num4++] * 0x100));
            entry._CompressionMethod = (short) (buffer[num4++] + (buffer[num4++] * 0x100));
            entry._TimeBlob = ((buffer[num4++] + (buffer[num4++] * 0x100)) + ((buffer[num4++] * 0x100) * 0x100)) + (((buffer[num4++] * 0x100) * 0x100) * 0x100);
            entry._LastModified = SharedUtilities.PackedToDateTime(entry._TimeBlob);
            entry._timestamp |= ZipEntryTimestamp.DOS;
            entry._Crc32 = ((buffer[num4++] + (buffer[num4++] * 0x100)) + ((buffer[num4++] * 0x100) * 0x100)) + (((buffer[num4++] * 0x100) * 0x100) * 0x100);
            entry._CompressedSize = (long) ((ulong) (((buffer[num4++] + (buffer[num4++] * 0x100)) + ((buffer[num4++] * 0x100) * 0x100)) + (((buffer[num4++] * 0x100) * 0x100) * 0x100)));
            entry._UncompressedSize = (long) ((ulong) (((buffer[num4++] + (buffer[num4++] * 0x100)) + ((buffer[num4++] * 0x100) * 0x100)) + (((buffer[num4++] * 0x100) * 0x100) * 0x100)));
            entry._CompressionMethod_FromZipFile = entry._CompressionMethod;
            entry._filenameLength = (short) (buffer[num4++] + (buffer[num4++] * 0x100));
            entry._extraFieldLength = (short) (buffer[num4++] + (buffer[num4++] * 0x100));
            entry._commentLength = (short) (buffer[num4++] + (buffer[num4++] * 0x100));
            entry._diskNumber = (uint) (buffer[num4++] + (buffer[num4++] * 0x100));
            entry._InternalFileAttrs = (short) (buffer[num4++] + (buffer[num4++] * 0x100));
            entry._ExternalFileAttrs = ((buffer[num4++] + (buffer[num4++] * 0x100)) + ((buffer[num4++] * 0x100) * 0x100)) + (((buffer[num4++] * 0x100) * 0x100) * 0x100);
            entry._RelativeOffsetOfLocalHeader = (long) ((ulong) (((buffer[num4++] + (buffer[num4++] * 0x100)) + ((buffer[num4++] * 0x100) * 0x100)) + (((buffer[num4++] * 0x100) * 0x100) * 0x100)));
            entry.IsText = (entry._InternalFileAttrs & 1) == 1;
            buffer = new byte[entry._filenameLength];
            int num3 = readStream.Read(buffer, 0, buffer.Length);
            num2 += num3;
            if ((entry._BitField & 0x800) == 0x800)
            {
                entry._FileNameInArchive = SharedUtilities.Utf8StringFromBuffer(buffer);
            }
            else
            {
                entry._FileNameInArchive = SharedUtilities.StringFromBuffer(buffer, encoding);
            }
            while (previouslySeen.ContainsKey(entry._FileNameInArchive))
            {
                entry._FileNameInArchive = CopyHelper.AppendCopyToFileName(entry._FileNameInArchive);
                entry._metadataChanged = true;
            }
            if (entry.AttributesIndicateDirectory)
            {
                entry.MarkAsDirectory();
            }
            else if (entry._FileNameInArchive.EndsWith("/"))
            {
                entry.MarkAsDirectory();
            }
            entry._CompressedFileDataSize = entry._CompressedSize;
            if ((entry._BitField & 1) == 1)
            {
                entry._Encryption_FromZipFile = entry._Encryption = EncryptionAlgorithm.PkzipWeak;
                entry._sourceIsEncrypted = true;
            }
            if (entry._extraFieldLength > 0)
            {
                entry._InputUsesZip64 = ((entry._CompressedSize == 0xffffffffL) || (entry._UncompressedSize == 0xffffffffL)) || (entry._RelativeOffsetOfLocalHeader == 0xffffffffL);
                num2 += entry.ProcessExtraField(readStream, entry._extraFieldLength);
                entry._CompressedFileDataSize = entry._CompressedSize;
            }
            if (entry._Encryption == EncryptionAlgorithm.PkzipWeak)
            {
                entry._CompressedFileDataSize -= 12L;
            }
            else if ((entry.Encryption == EncryptionAlgorithm.WinZipAes128) || (entry.Encryption == EncryptionAlgorithm.WinZipAes256))
            {
                entry._CompressedFileDataSize = entry.CompressedSize - (GetLengthOfCryptoHeaderBytes(entry.Encryption) + 10);
                entry._LengthOfTrailer = 10;
            }
            if ((entry._BitField & 8) == 8)
            {
                if (entry._InputUsesZip64)
                {
                    entry._LengthOfTrailer += 0x18;
                }
                else
                {
                    entry._LengthOfTrailer += 0x10;
                }
            }
            entry.AlternateEncoding = ((entry._BitField & 0x800) == 0x800) ? Encoding.UTF8 : encoding;
            entry.AlternateEncodingUsage = ZipOption.Always;
            if (entry._commentLength > 0)
            {
                buffer = new byte[entry._commentLength];
                num3 = readStream.Read(buffer, 0, buffer.Length);
                num2 += num3;
                if ((entry._BitField & 0x800) == 0x800)
                {
                    entry._Comment = SharedUtilities.Utf8StringFromBuffer(buffer);
                }
                else
                {
                    entry._Comment = SharedUtilities.StringFromBuffer(buffer, encoding);
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
            ze._Source = ZipEntrySource.ZipFile;
            ze._container = zc;
            ze._archiveStream = readStream;
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
            ze.__FileDataPosition = ze.ArchiveStream.Position;
            readStream.Seek(ze._CompressedFileDataSize + ze._LengthOfTrailer, SeekOrigin.Current);
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
            this._readExtraDepth++;
            long position = this.ArchiveStream.Position;
            this.ArchiveStream.Seek(this._RelativeOffsetOfLocalHeader, SeekOrigin.Begin);
            byte[] buffer = new byte[30];
            this.ArchiveStream.Read(buffer, 0, buffer.Length);
            int num2 = 0x1a;
            short num3 = (short) (buffer[num2++] + (buffer[num2++] * 0x100));
            short extraFieldLength = (short) (buffer[num2++] + (buffer[num2++] * 0x100));
            this.ArchiveStream.Seek((long) num3, SeekOrigin.Current);
            this.ProcessExtraField(this.ArchiveStream, extraFieldLength);
            this.ArchiveStream.Seek(position, SeekOrigin.Begin);
            this._readExtraDepth--;
        }

        private static bool ReadHeader(ZipEntry ze, Encoding defaultEncoding)
        {
            int num = 0;
            ze._RelativeOffsetOfLocalHeader = ze.ArchiveStream.Position;
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
            ze._VersionNeeded = (short) (buffer[startIndex++] + (buffer[startIndex++] * 0x100));
            ze._BitField = (short) (buffer[startIndex++] + (buffer[startIndex++] * 0x100));
            ze._CompressionMethod_FromZipFile = ze._CompressionMethod = (short) (buffer[startIndex++] + (buffer[startIndex++] * 0x100));
            ze._TimeBlob = ((buffer[startIndex++] + (buffer[startIndex++] * 0x100)) + ((buffer[startIndex++] * 0x100) * 0x100)) + (((buffer[startIndex++] * 0x100) * 0x100) * 0x100);
            ze._LastModified = SharedUtilities.PackedToDateTime(ze._TimeBlob);
            ze._timestamp |= ZipEntryTimestamp.DOS;
            if ((ze._BitField & 1) == 1)
            {
                ze._Encryption_FromZipFile = ze._Encryption = EncryptionAlgorithm.PkzipWeak;
                ze._sourceIsEncrypted = true;
            }
            ze._Crc32 = ((buffer[startIndex++] + (buffer[startIndex++] * 0x100)) + ((buffer[startIndex++] * 0x100) * 0x100)) + (((buffer[startIndex++] * 0x100) * 0x100) * 0x100);
            ze._CompressedSize = (long) ((ulong) (((buffer[startIndex++] + (buffer[startIndex++] * 0x100)) + ((buffer[startIndex++] * 0x100) * 0x100)) + (((buffer[startIndex++] * 0x100) * 0x100) * 0x100)));
            ze._UncompressedSize = (long) ((ulong) (((buffer[startIndex++] + (buffer[startIndex++] * 0x100)) + ((buffer[startIndex++] * 0x100) * 0x100)) + (((buffer[startIndex++] * 0x100) * 0x100) * 0x100)));
            if ((((uint) ze._CompressedSize) == uint.MaxValue) || (((uint) ze._UncompressedSize) == uint.MaxValue))
            {
                ze._InputUsesZip64 = true;
            }
            short num5 = (short) (buffer[startIndex++] + (buffer[startIndex++] * 0x100));
            short extraFieldLength = (short) (buffer[startIndex++] + (buffer[startIndex++] * 0x100));
            buffer = new byte[num5];
            num3 = ze.ArchiveStream.Read(buffer, 0, buffer.Length);
            num += num3;
            if ((ze._BitField & 0x800) == 0x800)
            {
                ze.AlternateEncoding = Encoding.UTF8;
                ze.AlternateEncodingUsage = ZipOption.Always;
            }
            ze._FileNameInArchive = ze.AlternateEncoding.GetString(buffer, 0, buffer.Length);
            if (ze._FileNameInArchive.EndsWith("/"))
            {
                ze.MarkAsDirectory();
            }
            num += ze.ProcessExtraField(ze.ArchiveStream, extraFieldLength);
            ze._LengthOfTrailer = 0;
            if (!ze._FileNameInArchive.EndsWith("/") && ((ze._BitField & 8) == 8))
            {
                long position = ze.ArchiveStream.Position;
                bool flag = true;
                long num8 = 0L;
                int num9 = 0;
                while (flag)
                {
                    num9++;
                    if (ze._container.ZipFile != null)
                    {
                        ze._container.ZipFile.OnReadBytes(ze);
                    }
                    long num10 = SharedUtilities.FindSignature(ze.ArchiveStream, 0x8074b50);
                    if (num10 == -1L)
                    {
                        return false;
                    }
                    num8 += num10;
                    if (ze._InputUsesZip64)
                    {
                        buffer = new byte[20];
                        if (ze.ArchiveStream.Read(buffer, 0, buffer.Length) != 20)
                        {
                            return false;
                        }
                        startIndex = 0;
                        ze._Crc32 = ((buffer[startIndex++] + (buffer[startIndex++] * 0x100)) + ((buffer[startIndex++] * 0x100) * 0x100)) + (((buffer[startIndex++] * 0x100) * 0x100) * 0x100);
                        ze._CompressedSize = BitConverter.ToInt64(buffer, startIndex);
                        startIndex += 8;
                        ze._UncompressedSize = BitConverter.ToInt64(buffer, startIndex);
                        startIndex += 8;
                        ze._LengthOfTrailer += 0x18;
                    }
                    else
                    {
                        buffer = new byte[12];
                        if (ze.ArchiveStream.Read(buffer, 0, buffer.Length) != 12)
                        {
                            return false;
                        }
                        startIndex = 0;
                        ze._Crc32 = ((buffer[startIndex++] + (buffer[startIndex++] * 0x100)) + ((buffer[startIndex++] * 0x100) * 0x100)) + (((buffer[startIndex++] * 0x100) * 0x100) * 0x100);
                        ze._CompressedSize = (long) ((ulong) (((buffer[startIndex++] + (buffer[startIndex++] * 0x100)) + ((buffer[startIndex++] * 0x100) * 0x100)) + (((buffer[startIndex++] * 0x100) * 0x100) * 0x100)));
                        ze._UncompressedSize = (long) ((ulong) (((buffer[startIndex++] + (buffer[startIndex++] * 0x100)) + ((buffer[startIndex++] * 0x100) * 0x100)) + (((buffer[startIndex++] * 0x100) * 0x100) * 0x100)));
                        ze._LengthOfTrailer += 0x10;
                    }
                    if (num8 != ze._CompressedSize)
                    {
                        ze.ArchiveStream.Seek(-12L, SeekOrigin.Current);
                        num8 += 4L;
                    }
                }
                ze.ArchiveStream.Seek(position, SeekOrigin.Begin);
            }
            ze._CompressedFileDataSize = ze._CompressedSize;
            if ((ze._BitField & 1) == 1)
            {
                if ((ze.Encryption == EncryptionAlgorithm.WinZipAes128) || (ze.Encryption == EncryptionAlgorithm.WinZipAes256))
                {
                    int keyStrengthInBits = GetKeyStrengthInBits(ze._Encryption_FromZipFile);
                    ze._aesCrypto_forExtract = WinZipAesCrypto.ReadFromStream(null, keyStrengthInBits, ze.ArchiveStream);
                    num += ze._aesCrypto_forExtract.SizeOfEncryptionMetadata - 10;
                    ze._CompressedFileDataSize -= ze._aesCrypto_forExtract.SizeOfEncryptionMetadata;
                    ze._LengthOfTrailer += 10;
                }
                else
                {
                    ze._WeakEncryptionHeader = new byte[12];
                    num += ReadWeakEncryptionHeader(ze._archiveStream, ze._WeakEncryptionHeader);
                    ze._CompressedFileDataSize -= 12L;
                }
            }
            ze._LengthOfHeader = num;
            ze._TotalEntrySize = (ze._LengthOfHeader + ze._CompressedFileDataSize) + ze._LengthOfTrailer;
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
            this.__FileDataPosition = -1L;
            this._LengthOfHeader = 0;
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
        /// The values you set here will be retrievable with the <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipEntry.ModifiedTime" />, <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipEntry.CreationTime" /> and <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipEntry.AccessedTime" /> properties.
        /// </para>
        /// 
        /// <para>
        /// When this method is called, if both <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipEntry.EmitTimesInWindowsFormatWhenSaving" /> and <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipEntry.EmitTimesInUnixFormatWhenSaving" /> are false, then the
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
        /// <seealso cref="P:DotNetZipAdditionalPlatforms.Zip.ZipEntry.EmitTimesInWindowsFormatWhenSaving" />
        /// <seealso cref="P:DotNetZipAdditionalPlatforms.Zip.ZipEntry.EmitTimesInUnixFormatWhenSaving" />
        /// <seealso cref="P:DotNetZipAdditionalPlatforms.Zip.ZipEntry.AccessedTime" />
        /// <seealso cref="P:DotNetZipAdditionalPlatforms.Zip.ZipEntry.CreationTime" />
        /// <seealso cref="P:DotNetZipAdditionalPlatforms.Zip.ZipEntry.ModifiedTime" />
        public void SetEntryTimes(DateTime created, DateTime accessed, DateTime modified)
        {
            this._ntfsTimesAreSet = true;
            if ((created == _zeroHour) && (created.Kind == _zeroHour.Kind))
            {
                created = _win32Epoch;
            }
            if ((accessed == _zeroHour) && (accessed.Kind == _zeroHour.Kind))
            {
                accessed = _win32Epoch;
            }
            if ((modified == _zeroHour) && (modified.Kind == _zeroHour.Kind))
            {
                modified = _win32Epoch;
            }
            this._Ctime = created.ToUniversalTime();
            this._Atime = accessed.ToUniversalTime();
            this._Mtime = modified.ToUniversalTime();
            this._LastModified = this._Mtime;
            if (!(this._emitUnixTimes || this._emitNtfsTimes))
            {
                this._emitNtfsTimes = true;
            }
            this._metadataChanged = true;
        }

        private void SetFdpLoh()
        {
            long position = this.ArchiveStream.Position;
            try
            {
                this.ArchiveStream.Seek(this._RelativeOffsetOfLocalHeader, SeekOrigin.Begin);
            }
            catch (IOException exception)
            {
                throw new BadStateException(string.Format(CultureInfo.InvariantCulture, "Exception seeking  entry({0}) offset(0x{1:X8}) len(0x{2:X8})", this.FileName, this._RelativeOffsetOfLocalHeader, this.ArchiveStream.Length), exception);
            }
            byte[] buffer = new byte[30];
            this.ArchiveStream.Read(buffer, 0, buffer.Length);
            short num2 = (short) (buffer[0x1a] + (buffer[0x1b] * 0x100));
            short num3 = (short) (buffer[0x1c] + (buffer[0x1d] * 0x100));
            this.ArchiveStream.Seek((long) (num2 + num3), SeekOrigin.Current);
            this._LengthOfHeader = ((30 + num3) + num2) + GetLengthOfCryptoHeaderBytes(this._Encryption_FromZipFile);
            this.__FileDataPosition = this._RelativeOffsetOfLocalHeader + this._LengthOfHeader;
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
            if (this._Source == ZipEntrySource.Stream)
            {
                this.PrepSourceStream();
                input = this._sourceStream;
                try
                {
                    length = this._sourceStream.Length;
                }
                catch (NotSupportedException)
                {
                }
                return length;
            }
            if (this._Source == ZipEntrySource.ZipFile)
            {
                string password = (this._Encryption_FromZipFile == EncryptionAlgorithm.None) ? null : (this._Password ?? this._container.Password);
                this._sourceStream = this.InternalOpenReader(password);
                this.PrepSourceStream();
                input = this._sourceStream;
                return this._sourceStream.Length;
            }
            if (this._Source == ZipEntrySource.JitStream)
            {
                if (this._sourceStream == null)
                {
                    this._sourceStream = this._OpenDelegate(this.FileName);
                }
                this.PrepSourceStream();
                input = this._sourceStream;
                try
                {
                    length = this._sourceStream.Length;
                }
                catch (NotSupportedException)
                {
                }
                return length;
            }
            if (this._Source == ZipEntrySource.FileSystem)
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
            if (this._Encryption_FromZipFile != EncryptionAlgorithm.None)
            {
                if (this._Encryption_FromZipFile == EncryptionAlgorithm.PkzipWeak)
                {
                    if (password == null)
                    {
                        throw new ZipException("Missing password.");
                    }
                    this.ArchiveStream.Seek(this.FileDataPosition - 12L, SeekOrigin.Begin);
                    this._zipCrypto_forExtract = ZipCrypto.ForRead(password, this);
                }
                else if ((this._Encryption_FromZipFile == EncryptionAlgorithm.WinZipAes128) || (this._Encryption_FromZipFile == EncryptionAlgorithm.WinZipAes256))
                {
                    if (password == null)
                    {
                        throw new ZipException("Missing password.");
                    }
                    if (this._aesCrypto_forExtract != null)
                    {
                        this._aesCrypto_forExtract.Password = password;
                    }
                    else
                    {
                        int lengthOfCryptoHeaderBytes = GetLengthOfCryptoHeaderBytes(this._Encryption_FromZipFile);
                        this.ArchiveStream.Seek(this.FileDataPosition - lengthOfCryptoHeaderBytes, SeekOrigin.Begin);
                        int keyStrengthInBits = GetKeyStrengthInBits(this._Encryption_FromZipFile);
                        this._aesCrypto_forExtract = WinZipAesCrypto.ReadFromStream(password, keyStrengthInBits, this.ArchiveStream);
                    }
                }
            }
        }

        private void SetZip64Flags()
        {
            this._entryRequiresZip64 = new bool?(((this._CompressedSize >= 0xffffffffL) || (this._UncompressedSize >= 0xffffffffL)) || (this._RelativeOffsetOfLocalHeader >= 0xffffffffL));
            if (!((this._container.Zip64 != Zip64Option.Default) ? true : !this._entryRequiresZip64.Value))
            {
                throw new ZipException("Compressed or Uncompressed size, or offset exceeds the maximum value. Consider setting the UseZip64WhenSaving property on the ZipFile instance.");
            }
            this._OutputUsesZip64 = new bool?((this._container.Zip64 == Zip64Option.Always) ? true : this._entryRequiresZip64.Value);
        }

        internal void StoreRelativeOffset()
        {
            this._RelativeOffsetOfLocalHeader = this._future_ROLH;
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
            lock (this._outputLock)
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
            if (((this._CompressionMethod_FromZipFile != 0) && (this._CompressionMethod_FromZipFile != 8)) && (this._CompressionMethod_FromZipFile != 12))
            {
                throw new ZipException(string.Format(CultureInfo.InvariantCulture, "Entry {0} uses an unsupported compression method (0x{1:X2}, {2})", this.FileName, this._CompressionMethod_FromZipFile, this.UnsupportedCompressionMethod));
            }
        }

        internal void ValidateEncryption()
        {
            if ((((this.Encryption != EncryptionAlgorithm.PkzipWeak) && (this.Encryption != EncryptionAlgorithm.WinZipAes128)) && (this.Encryption != EncryptionAlgorithm.WinZipAes256)) && (this.Encryption != EncryptionAlgorithm.None))
            {
                if (this._UnsupportedAlgorithmId != 0)
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
                if (this._container.ZipFile.FlattenFoldersOnExtract)
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
            if ((actualCrc32 != this._Crc32) && (((this.Encryption != EncryptionAlgorithm.WinZipAes128) && (this.Encryption != EncryptionAlgorithm.WinZipAes256)) || (this._WinZipAesMethod != 2)))
            {
                throw new BadCrcException("CRC error: the file being extracted appears to be corrupted. " + string.Format(CultureInfo.InvariantCulture, "Expected 0x{0:X8}, Actual 0x{1:X8}", this._Crc32, actualCrc32));
            }
            if ((this.UncompressedSize != 0L) && ((this.Encryption == EncryptionAlgorithm.WinZipAes128) || (this.Encryption == EncryptionAlgorithm.WinZipAes256)))
            {
                WinZipAesCipherStream stream = this._inputDecryptorStream as WinZipAesCipherStream;
                this._aesCrypto_forExtract.CalculatedMac = stream.FinalAuthentication;
                this._aesCrypto_forExtract.ReadAndVerifyMac(this.ArchiveStream);
            }
        }

        private bool WantReadAgain()
        {
            if (this._UncompressedSize < 0x10L)
            {
                return false;
            }
            if (this._CompressionMethod == 0)
            {
                return false;
            }
            if (this.CompressionLevel == DotNetZipAdditionalPlatforms.Zlib.CompressionLevel.None)
            {
                return false;
            }
            if (this._CompressedSize < this._UncompressedSize)
            {
                return false;
            }
            if (!((this._Source != ZipEntrySource.Stream) || this._sourceStream.CanSeek))
            {
                return false;
            }
            if ((this._aesCrypto_forWrite != null) && ((this.CompressedSize - this._aesCrypto_forWrite.SizeOfEncryptionMetadata) <= (this.UncompressedSize + 0x10L)))
            {
                return false;
            }
            if ((this._zipCrypto_forWrite != null) && ((this.CompressedSize - 12L) <= this.UncompressedSize))
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
                if (!((this._Source != ZipEntrySource.ZipFile) || this._restreamRequiredOnSave))
                {
                    this.CopyThroughOneEntry(s);
                    return;
                }
                if (this.IsDirectory)
                {
                    this.WriteHeader(s, 1);
                    this.StoreRelativeOffset();
                    this._entryRequiresZip64 = new bool?(this._RelativeOffsetOfLocalHeader >= 0xffffffffL);
                    this._OutputUsesZip64 = new bool?((this._container.Zip64 == Zip64Option.Always) ? true : this._entryRequiresZip64.Value);
                    if (stream2 != null)
                    {
                        this._diskNumber = stream2.CurrentSegment;
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
                    this._TotalEntrySize = (this._LengthOfHeader + this._CompressedFileDataSize) + this._LengthOfTrailer;
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
                            stream2.TruncateBackward(this._diskNumber, this._RelativeOffsetOfLocalHeader);
                        }
                        else
                        {
                            s.Seek(this._RelativeOffsetOfLocalHeader, SeekOrigin.Begin);
                        }
                        s.SetLength(s.Position);
                        if (stream != null)
                        {
                            stream.Adjust(this._TotalEntrySize);
                        }
                    }
                }
                while (flag2);
                this._skippedDuringSave = false;
                flag = true;
            }
            catch (Exception exception)
            {
                DotNetZipAdditionalPlatforms.Zip.ZipErrorAction zipErrorAction = this.ZipErrorAction;
                int num2 = 0;
            Label_01A5:
                if (this.ZipErrorAction == DotNetZipAdditionalPlatforms.Zip.ZipErrorAction.Throw)
                {
                    throw;
                }
                if ((this.ZipErrorAction == DotNetZipAdditionalPlatforms.Zip.ZipErrorAction.Skip) || (this.ZipErrorAction == DotNetZipAdditionalPlatforms.Zip.ZipErrorAction.Retry))
                {
                    long num3 = (stream != null) ? stream.ComputedPosition : s.Position;
                    long offset = num3 - this._future_ROLH;
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
                    if (this.ZipErrorAction == DotNetZipAdditionalPlatforms.Zip.ZipErrorAction.Skip)
                    {
                        this.WriteStatus("Skipping file {0} (exception: {1})", new object[] { this.LocalFileName, exception.ToString() });
                        this._skippedDuringSave = true;
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
                    if (this.ZipErrorAction == DotNetZipAdditionalPlatforms.Zip.ZipErrorAction.InvokeErrorEvent)
                    {
                        this.OnZipErrorWhileSaving(exception);
                        if (this._ioOperationCanceled)
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
            dst[dstOffset++] = (byte) (this._VersionMadeBy & 0xff);
            dst[dstOffset++] = (byte) ((this._VersionMadeBy & 0xff00) >> 8);
            short num2 = (this.VersionNeeded != 0) ? this.VersionNeeded : ((short) 20);
            if (!this._OutputUsesZip64.HasValue)
            {
                this._OutputUsesZip64 = new bool?(this._container.Zip64 == Zip64Option.Always);
            }
            short num3 = this._OutputUsesZip64.Value ? ((short) 0x2d) : num2;
            if (this.CompressionMethod == DotNetZipAdditionalPlatforms.Zip.CompressionMethod.BZip2)
            {
                num3 = 0x2e;
            }
            dst[dstOffset++] = (byte) (num3 & 0xff);
            dst[dstOffset++] = (byte) ((num3 & 0xff00) >> 8);
            dst[dstOffset++] = (byte) (this._BitField & 0xff);
            dst[dstOffset++] = (byte) ((this._BitField & 0xff00) >> 8);
            dst[dstOffset++] = (byte) (this._CompressionMethod & 0xff);
            dst[dstOffset++] = (byte) ((this._CompressionMethod & 0xff00) >> 8);
            if ((this.Encryption == EncryptionAlgorithm.WinZipAes128) || (this.Encryption == EncryptionAlgorithm.WinZipAes256))
            {
                dstOffset -= 2;
                dst[dstOffset++] = 0x63;
                dst[dstOffset++] = 0;
            }
            dst[dstOffset++] = (byte) (this._TimeBlob & 0xff);
            dst[dstOffset++] = (byte) ((this._TimeBlob & 0xff00) >> 8);
            dst[dstOffset++] = (byte) ((this._TimeBlob & 0xff0000) >> 0x10);
            dst[dstOffset++] = (byte) ((this._TimeBlob & 0xff000000L) >> 0x18);
            dst[dstOffset++] = (byte) (this._Crc32 & 0xff);
            dst[dstOffset++] = (byte) ((this._Crc32 & 0xff00) >> 8);
            dst[dstOffset++] = (byte) ((this._Crc32 & 0xff0000) >> 0x10);
            dst[dstOffset++] = (byte) ((this._Crc32 & 0xff000000L) >> 0x18);
            int num4 = 0;
            if (this._OutputUsesZip64.Value)
            {
                for (num4 = 0; num4 < 8; num4++)
                {
                    dst[dstOffset++] = 0xff;
                }
            }
            else
            {
                dst[dstOffset++] = (byte) (this._CompressedSize & 0xffL);
                dst[dstOffset++] = (byte) ((this._CompressedSize & 0xff00L) >> 8);
                dst[dstOffset++] = (byte) ((this._CompressedSize & 0xff0000L) >> 0x10);
                dst[dstOffset++] = (byte) ((this._CompressedSize & 0xff000000L) >> 0x18);
                dst[dstOffset++] = (byte) (this._UncompressedSize & 0xffL);
                dst[dstOffset++] = (byte) ((this._UncompressedSize & 0xff00L) >> 8);
                dst[dstOffset++] = (byte) ((this._UncompressedSize & 0xff0000L) >> 0x10);
                dst[dstOffset++] = (byte) ((this._UncompressedSize & 0xff000000L) >> 0x18);
            }
            byte[] encodedFileNameBytes = this.GetEncodedFileNameBytes();
            short length = (short) encodedFileNameBytes.Length;
            dst[dstOffset++] = (byte) (length & 0xff);
            dst[dstOffset++] = (byte) ((length & 0xff00) >> 8);
            this._presumeZip64 = this._OutputUsesZip64.Value;
            this._Extra = this.ConstructExtraField(true);
            short count = (this._Extra == null) ? ((short) 0) : ((short) this._Extra.Length);
            dst[dstOffset++] = (byte) (count & 0xff);
            dst[dstOffset++] = (byte) ((count & 0xff00) >> 8);
            int num7 = (this._CommentBytes == null) ? 0 : this._CommentBytes.Length;
            if ((num7 + dstOffset) > dst.Length)
            {
                num7 = dst.Length - dstOffset;
            }
            dst[dstOffset++] = (byte) (num7 & 0xff);
            dst[dstOffset++] = (byte) ((num7 & 0xff00) >> 8);
            if ((this._container.ZipFile != null) && (this._container.ZipFile.MaxOutputSegmentSize != 0))
            {
                dst[dstOffset++] = (byte) (this._diskNumber & 0xff);
                dst[dstOffset++] = (byte) ((this._diskNumber & 0xff00) >> 8);
            }
            else
            {
                dst[dstOffset++] = 0;
                dst[dstOffset++] = 0;
            }
            dst[dstOffset++] = this._IsText ? ((byte) 1) : ((byte) 0);
            dst[dstOffset++] = 0;
            dst[dstOffset++] = (byte) (this._ExternalFileAttrs & 0xff);
            dst[dstOffset++] = (byte) ((this._ExternalFileAttrs & 0xff00) >> 8);
            dst[dstOffset++] = (byte) ((this._ExternalFileAttrs & 0xff0000) >> 0x10);
            dst[dstOffset++] = (byte) ((this._ExternalFileAttrs & 0xff000000L) >> 0x18);
            if (this._RelativeOffsetOfLocalHeader > 0xffffffffL)
            {
                dst[dstOffset++] = 0xff;
                dst[dstOffset++] = 0xff;
                dst[dstOffset++] = 0xff;
                dst[dstOffset++] = 0xff;
            }
            else
            {
                dst[dstOffset++] = (byte) (this._RelativeOffsetOfLocalHeader & 0xffL);
                dst[dstOffset++] = (byte) ((this._RelativeOffsetOfLocalHeader & 0xff00L) >> 8);
                dst[dstOffset++] = (byte) ((this._RelativeOffsetOfLocalHeader & 0xff0000L) >> 0x10);
                dst[dstOffset++] = (byte) ((this._RelativeOffsetOfLocalHeader & 0xff000000L) >> 0x18);
            }
            Buffer.BlockCopy(encodedFileNameBytes, 0, dst, dstOffset, length);
            dstOffset += length;
            if (this._Extra != null)
            {
                byte[] src = this._Extra;
                int srcOffset = 0;
                Buffer.BlockCopy(src, srcOffset, dst, dstOffset, count);
                dstOffset += count;
            }
            if (num7 != 0)
            {
                Buffer.BlockCopy(this._CommentBytes, 0, dst, dstOffset, num7);
                dstOffset += num7;
            }
            s.Write(dst, 0, dstOffset);
        }

        internal void WriteHeader(Stream s, int cycle)
        {
            CountingStream stream = s as CountingStream;
            this._future_ROLH = (stream != null) ? stream.ComputedPosition : s.Position;
            int num = 0;
            int count = 0;
            byte[] src = new byte[30];
            src[count++] = 80;
            src[count++] = 0x4b;
            src[count++] = 3;
            src[count++] = 4;
            this._presumeZip64 = (this._container.Zip64 == Zip64Option.Always) || ((this._container.Zip64 == Zip64Option.AsNecessary) && !s.CanSeek);
            short num3 = this._presumeZip64 ? ((short) 0x2d) : ((short) 20);
            if (this.CompressionMethod == DotNetZipAdditionalPlatforms.Zip.CompressionMethod.BZip2)
            {
                num3 = 0x2e;
            }
            src[count++] = (byte) (num3 & 0xff);
            src[count++] = (byte) ((num3 & 0xff00) >> 8);
            byte[] encodedFileNameBytes = this.GetEncodedFileNameBytes();
            short length = (short) encodedFileNameBytes.Length;
            if (this._Encryption == EncryptionAlgorithm.None)
            {
                this._BitField = (short) (this._BitField & -2);
            }
            else
            {
                this._BitField = (short) (this._BitField | 1);
            }
            if (this._actualEncoding.CodePage == Encoding.UTF8.CodePage)
            {
                this._BitField = (short) (this._BitField | 0x800);
            }
            if (this.IsDirectory || (cycle == 0x63))
            {
                this._BitField = (short) (this._BitField & -9);
                this._BitField = (short) (this._BitField & -2);
                this.Encryption = EncryptionAlgorithm.None;
                this.Password = null;
            }
            else if (!s.CanSeek)
            {
                this._BitField = (short) (this._BitField | 8);
            }
            src[count++] = (byte) (this._BitField & 0xff);
            src[count++] = (byte) ((this._BitField & 0xff00) >> 8);
            if (this.__FileDataPosition == -1L)
            {
                this._CompressedSize = 0L;
                this._crcCalculated = false;
            }
            this.MaybeUnsetCompressionMethodForWriting(cycle);
            src[count++] = (byte) (this._CompressionMethod & 0xff);
            src[count++] = (byte) ((this._CompressionMethod & 0xff00) >> 8);
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
            this._TimeBlob = SharedUtilities.DateTimeToPacked(this.LastModified);
            src[count++] = (byte) (this._TimeBlob & 0xff);
            src[count++] = (byte) ((this._TimeBlob & 0xff00) >> 8);
            src[count++] = (byte) ((this._TimeBlob & 0xff0000) >> 0x10);
            src[count++] = (byte) ((this._TimeBlob & 0xff000000L) >> 0x18);
            src[count++] = (byte) (this._Crc32 & 0xff);
            src[count++] = (byte) ((this._Crc32 & 0xff00) >> 8);
            src[count++] = (byte) ((this._Crc32 & 0xff0000) >> 0x10);
            src[count++] = (byte) ((this._Crc32 & 0xff000000L) >> 0x18);
            if (this._presumeZip64)
            {
                for (num = 0; num < 8; num++)
                {
                    src[count++] = 0xff;
                }
            }
            else
            {
                src[count++] = (byte) (this._CompressedSize & 0xffL);
                src[count++] = (byte) ((this._CompressedSize & 0xff00L) >> 8);
                src[count++] = (byte) ((this._CompressedSize & 0xff0000L) >> 0x10);
                src[count++] = (byte) ((this._CompressedSize & 0xff000000L) >> 0x18);
                src[count++] = (byte) (this._UncompressedSize & 0xffL);
                src[count++] = (byte) ((this._UncompressedSize & 0xff00L) >> 8);
                src[count++] = (byte) ((this._UncompressedSize & 0xff0000L) >> 0x10);
                src[count++] = (byte) ((this._UncompressedSize & 0xff000000L) >> 0x18);
            }
            src[count++] = (byte) (length & 0xff);
            src[count++] = (byte) ((length & 0xff00) >> 8);
            this._Extra = this.ConstructExtraField(false);
            short num5 = (this._Extra == null) ? ((short) 0) : ((short) this._Extra.Length);
            src[count++] = (byte) (num5 & 0xff);
            src[count++] = (byte) ((num5 & 0xff00) >> 8);
            byte[] dst = new byte[(count + length) + num5];
            Buffer.BlockCopy(src, 0, dst, 0, count);
            Buffer.BlockCopy(encodedFileNameBytes, 0, dst, count, encodedFileNameBytes.Length);
            count += encodedFileNameBytes.Length;
            if (this._Extra != null)
            {
                Buffer.BlockCopy(this._Extra, 0, dst, count, this._Extra.Length);
                count += this._Extra.Length;
            }
            this._LengthOfHeader = count;
            ZipSegmentedStream stream2 = s as ZipSegmentedStream;
            if (stream2 != null)
            {
                stream2.ContiguousWrite = true;
                uint num6 = stream2.ComputeSegment(count);
                if (num6 != stream2.CurrentSegment)
                {
                    this._future_ROLH = 0L;
                }
                else
                {
                    this._future_ROLH = stream2.Position;
                }
                this._diskNumber = num6;
            }
            if ((this._container.Zip64 == Zip64Option.Default) && (((uint) this._RelativeOffsetOfLocalHeader) >= uint.MaxValue))
            {
                throw new ZipException("Offset within the zip archive exceeds 0xFFFFFFFF. Consider setting the UseZip64WhenSaving property on the ZipFile instance.");
            }
            s.Write(dst, 0, count);
            if (stream2 != null)
            {
                stream2.ContiguousWrite = false;
            }
            this._EntryHeader = dst;
        }

        internal void WriteSecurityMetadata(Stream outstream)
        {
            if (this.Encryption != EncryptionAlgorithm.None)
            {
                string password = this._Password;
                if ((this._Source == ZipEntrySource.ZipFile) && (password == null))
                {
                    password = this._container.Password;
                }
                if (password == null)
                {
                    this._zipCrypto_forWrite = null;
                    this._aesCrypto_forWrite = null;
                }
                else if (this.Encryption == EncryptionAlgorithm.PkzipWeak)
                {
                    this._zipCrypto_forWrite = ZipCrypto.ForWrite(password);
                    Random random = new Random();
                    byte[] buffer = new byte[12];
                    random.NextBytes(buffer);
                    if ((this._BitField & 8) == 8)
                    {
                        this._TimeBlob = SharedUtilities.DateTimeToPacked(this.LastModified);
                        buffer[11] = (byte) ((this._TimeBlob >> 8) & 0xff);
                    }
                    else
                    {
                        this.FigureCrc32();
                        buffer[11] = (byte) ((this._Crc32 >> 0x18) & 0xff);
                    }
                    byte[] buffer2 = this._zipCrypto_forWrite.EncryptMessage(buffer, buffer.Length);
                    outstream.Write(buffer2, 0, buffer2.Length);
                    this._LengthOfHeader += buffer2.Length;
                }
                else if ((this.Encryption == EncryptionAlgorithm.WinZipAes128) || (this.Encryption == EncryptionAlgorithm.WinZipAes256))
                {
                    int keyStrengthInBits = GetKeyStrengthInBits(this.Encryption);
                    this._aesCrypto_forWrite = WinZipAesCrypto.Generate(password, keyStrengthInBits);
                    outstream.Write(this._aesCrypto_forWrite.Salt, 0, this._aesCrypto_forWrite._Salt.Length);
                    outstream.Write(this._aesCrypto_forWrite.GeneratedPV, 0, this._aesCrypto_forWrite.GeneratedPV.Length);
                    this._LengthOfHeader += this._aesCrypto_forWrite._Salt.Length + this._aesCrypto_forWrite.GeneratedPV.Length;
                }
            }
        }

        private void WriteStatus(string format, params object[] args)
        {
            if ((this._container.ZipFile != null) && this._container.ZipFile.Verbose)
            {
                this._container.ZipFile.StatusMessageTextWriter.WriteLine(format, args);
            }
        }

        /// <summary>
        /// Last Access time for the file represented by the entry.
        /// </summary>
        /// <remarks>
        /// This value may or may not be meaningful.  If the <c>ZipEntry</c> was read from an existing
        /// Zip archive, this information may not be available. For an explanation of why, see
        /// <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipEntry.ModifiedTime" />.
        /// </remarks>
        /// <seealso cref="P:DotNetZipAdditionalPlatforms.Zip.ZipEntry.ModifiedTime" />
        /// <seealso cref="P:DotNetZipAdditionalPlatforms.Zip.ZipEntry.CreationTime" />
        /// <seealso cref="M:DotNetZipAdditionalPlatforms.Zip.ZipEntry.SetEntryTimes(System.DateTime,System.DateTime,System.DateTime)" />
        public DateTime AccessedTime
        {
            get
            {
                return this._Atime;
            }
            set
            {
                this.SetEntryTimes(this._Ctime, value, this._Mtime);
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
        /// governed by the <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipEntry.AlternateEncodingUsage" /> property.
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
        /// <seealso cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.AlternateEncodingUsage" />
        public Encoding AlternateEncoding { get; set; }

        /// <summary>
        /// Describes if and when this instance should apply
        /// AlternateEncoding to encode the FileName and Comment, when
        /// saving.
        /// </summary>
        /// <seealso cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.AlternateEncoding" />
        public ZipOption AlternateEncodingUsage { get; set; }

        internal Stream ArchiveStream
        {
            get
            {
                if (this._archiveStream == null)
                {
                    if (this._container.ZipFile != null)
                    {
                        ZipFile zipFile = this._container.ZipFile;
                        zipFile.Reset(false);
                        this._archiveStream = zipFile.StreamForDiskNumber(this._diskNumber);
                    }
                    else
                    {
                        this._archiveStream = this._container.ZipOutputStream.OutputStream;
                    }
                }
                return this._archiveStream;
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
                return (FileAttributes) this._ExternalFileAttrs;
            }
            set
            {
                this._ExternalFileAttrs = (int) value;
                this._VersionMadeBy = 0x2d;
                this._metadataChanged = true;
            }
        }

        /// <summary>
        /// True if the referenced entry is a directory.
        /// </summary>
        internal bool AttributesIndicateDirectory
        {
            get
            {
                return ((this._InternalFileAttrs == 0) && ((this._ExternalFileAttrs & 0x10) == 0x10));
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
                return this._BitField;
            }
        }

        private int BufferSize
        {
            get
            {
                return this._container.BufferSize;
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
        /// specify an alternative with <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipEntry.AlternateEncoding" /> and
        /// <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipEntry.AlternateEncodingUsage" />.
        /// </para>
        /// </remarks>
        /// <seealso cref="P:DotNetZipAdditionalPlatforms.Zip.ZipEntry.AlternateEncoding" />
        /// <seealso cref="P:DotNetZipAdditionalPlatforms.Zip.ZipEntry.AlternateEncodingUsage" />
        public string Comment
        {
            get
            {
                return this._Comment;
            }
            set
            {
                this._Comment = value;
                this._metadataChanged = true;
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
        /// <seealso cref="P:DotNetZipAdditionalPlatforms.Zip.ZipEntry.UncompressedSize" />
        public long CompressedSize
        {
            get
            {
                return this._CompressedSize;
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
        /// <seealso cref="P:DotNetZipAdditionalPlatforms.Zip.ZipEntry.CompressionMethod" />
        public DotNetZipAdditionalPlatforms.Zlib.CompressionLevel CompressionLevel
        {
            get
            {
                return this._CompressionLevel;
            }
            set
            {
                if (((this._CompressionMethod == 8) || (this._CompressionMethod == 0)) && ((value != DotNetZipAdditionalPlatforms.Zlib.CompressionLevel.Default) || (this._CompressionMethod != 8)))
                {
                    this._CompressionLevel = value;
                    if ((value != DotNetZipAdditionalPlatforms.Zlib.CompressionLevel.None) || (this._CompressionMethod != 0))
                    {
                        if (this._CompressionLevel == DotNetZipAdditionalPlatforms.Zlib.CompressionLevel.None)
                        {
                            this._CompressionMethod = 0;
                        }
                        else
                        {
                            this._CompressionMethod = 8;
                        }
                        if (this._container.ZipFile != null)
                        {
                            this._container.ZipFile.NotifyEntryChanged();
                        }
                        this._restreamRequiredOnSave = true;
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
        /// <seealso cref="P:DotNetZipAdditionalPlatforms.Zip.ZipEntry.CompressionMethod" />
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
        public DotNetZipAdditionalPlatforms.Zip.CompressionMethod CompressionMethod
        {
            get
            {
                return (DotNetZipAdditionalPlatforms.Zip.CompressionMethod) this._CompressionMethod;
            }
            set
            {
                if (value != ((DotNetZipAdditionalPlatforms.Zip.CompressionMethod) this._CompressionMethod))
                {
                    if (((value != DotNetZipAdditionalPlatforms.Zip.CompressionMethod.None) && (value != DotNetZipAdditionalPlatforms.Zip.CompressionMethod.Deflate)) && (value != DotNetZipAdditionalPlatforms.Zip.CompressionMethod.BZip2))
                    {
                        throw new InvalidOperationException("Unsupported compression method.");
                    }
                    this._CompressionMethod = (short) value;
                    if (this._CompressionMethod == 0)
                    {
                        this._CompressionLevel = DotNetZipAdditionalPlatforms.Zlib.CompressionLevel.None;
                    }
                    else if (this.CompressionLevel == DotNetZipAdditionalPlatforms.Zlib.CompressionLevel.None)
                    {
                        this._CompressionLevel = DotNetZipAdditionalPlatforms.Zlib.CompressionLevel.Default;
                    }
                    if (this._container.ZipFile != null)
                    {
                        this._container.ZipFile.NotifyEntryChanged();
                    }
                    this._restreamRequiredOnSave = true;
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
                return this._Crc32;
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
        /// explanation of why, see <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipEntry.ModifiedTime" />.
        /// </remarks>
        /// <seealso cref="P:DotNetZipAdditionalPlatforms.Zip.ZipEntry.ModifiedTime" />
        /// <seealso cref="P:DotNetZipAdditionalPlatforms.Zip.ZipEntry.AccessedTime" />
        /// <seealso cref="M:DotNetZipAdditionalPlatforms.Zip.ZipEntry.SetEntryTimes(System.DateTime,System.DateTime,System.DateTime)" />
        public DateTime CreationTime
        {
            get
            {
                return this._Ctime;
            }
            set
            {
                this.SetEntryTimes(value, this._Atime, this._Mtime);
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
        /// When adding an entry from a file or directory, the Creation (<see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipEntry.CreationTime" />), Access (<see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipEntry.AccessedTime" />), and Modified
        /// (<see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipEntry.ModifiedTime" />) times for the given entry are automatically
        /// set from the filesystem values. When adding an entry from a stream or
        /// string, all three values are implicitly set to DateTime.Now.  Applications
        /// can also explicitly set those times by calling <see cref="M:DotNetZipAdditionalPlatforms.Zip.ZipEntry.SetEntryTimes(System.DateTime,System.DateTime,System.DateTime)" />.
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
        /// The times stored are taken from <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipEntry.ModifiedTime" />, <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipEntry.AccessedTime" />, and <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipEntry.CreationTime" />.
        /// </para>
        /// 
        /// <para>
        /// This property is not mutually exclusive from the <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipEntry.EmitTimesInWindowsFormatWhenSaving" /> property.  It is
        /// possible that a zip entry can embed the timestamps in both forms, one
        /// form, or neither.  But, there are no guarantees that a program running on
        /// Mac or Linux will gracefully handle NTFS Formatted times, or that a
        /// non-DotNetZip-powered application running on Windows will be able to
        /// handle file times in Unix format. When in doubt, test.
        /// </para>
        /// 
        /// <para>
        /// Normally you will use the <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.EmitTimesInUnixFormatWhenSaving">ZipFile.EmitTimesInUnixFormatWhenSaving</see>
        /// property, to specify the behavior for all entries, rather than the
        /// property on each individual entry.
        /// </para>
        /// </remarks>
        /// 
        /// <seealso cref="M:DotNetZipAdditionalPlatforms.Zip.ZipEntry.SetEntryTimes(System.DateTime,System.DateTime,System.DateTime)" />
        /// <seealso cref="P:DotNetZipAdditionalPlatforms.Zip.ZipEntry.EmitTimesInWindowsFormatWhenSaving" />
        /// <seealso cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.EmitTimesInUnixFormatWhenSaving" />
        /// <seealso cref="P:DotNetZipAdditionalPlatforms.Zip.ZipEntry.CreationTime" />
        /// <seealso cref="P:DotNetZipAdditionalPlatforms.Zip.ZipEntry.AccessedTime" />
        /// <seealso cref="P:DotNetZipAdditionalPlatforms.Zip.ZipEntry.ModifiedTime" />
        public bool EmitTimesInUnixFormatWhenSaving
        {
            get
            {
                return this._emitUnixTimes;
            }
            set
            {
                this._emitUnixTimes = value;
                this._metadataChanged = true;
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
        /// When adding an entry from a file or directory, the Creation (<see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipEntry.CreationTime" />), Access (<see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipEntry.AccessedTime" />), and Modified
        /// (<see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipEntry.ModifiedTime" />) times for the given entry are automatically
        /// set from the filesystem values. When adding an entry from a stream or
        /// string, all three values are implicitly set to DateTime.Now.  Applications
        /// can also explicitly set those times by calling <see cref="M:DotNetZipAdditionalPlatforms.Zip.ZipEntry.SetEntryTimes(System.DateTime,System.DateTime,System.DateTime)" />.
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
        /// The times stored are taken from <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipEntry.ModifiedTime" />, <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipEntry.AccessedTime" />, and <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipEntry.CreationTime" />.
        /// </para>
        /// 
        /// <para>
        /// This property is not mutually exclusive from the <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipEntry.EmitTimesInUnixFormatWhenSaving" /> property.  It is
        /// possible that a zip entry can embed the timestamps in both forms, one
        /// form, or neither.  But, there are no guarantees that a program running on
        /// Mac or Linux will gracefully handle NTFS Formatted times, or that a
        /// non-DotNetZip-powered application running on Windows will be able to
        /// handle file times in Unix format. When in doubt, test.
        /// </para>
        /// 
        /// <para>
        /// Normally you will use the <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.EmitTimesInWindowsFormatWhenSaving">ZipFile.EmitTimesInWindowsFormatWhenSaving</see>
        /// property, to specify the behavior for all entries in a zip, rather than
        /// the property on each individual entry.
        /// </para>
        /// 
        /// </remarks>
        /// 
        /// <seealso cref="M:DotNetZipAdditionalPlatforms.Zip.ZipEntry.SetEntryTimes(System.DateTime,System.DateTime,System.DateTime)" />
        /// <seealso cref="P:DotNetZipAdditionalPlatforms.Zip.ZipEntry.EmitTimesInUnixFormatWhenSaving" />
        /// <seealso cref="P:DotNetZipAdditionalPlatforms.Zip.ZipEntry.CreationTime" />
        /// <seealso cref="P:DotNetZipAdditionalPlatforms.Zip.ZipEntry.AccessedTime" />
        /// <seealso cref="P:DotNetZipAdditionalPlatforms.Zip.ZipEntry.ModifiedTime" />
        public bool EmitTimesInWindowsFormatWhenSaving
        {
            get
            {
                return this._emitNtfsTimes;
            }
            set
            {
                this._emitNtfsTimes = value;
                this._metadataChanged = true;
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
        /// saved. When setting this property, you must also set a <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipEntry.Password" /> on the entry.  If you set a value other than <see cref="F:DotNetZipAdditionalPlatforms.Zip.EncryptionAlgorithm.None" /> on this property and do not set a
        /// <c>Password</c> then the entry will not be encrypted. The <c>ZipEntry</c>
        /// data is encrypted as the <c>ZipFile</c> is saved, when you call <see cref="M:DotNetZipAdditionalPlatforms.Zip.ZipFile.Save" /> or one of its cousins on the containing
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
        /// The <see cref="T:DotNetZipAdditionalPlatforms.Zip.ZipFile" /> class also has a <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.Encryption" /> property.  In most cases you will use
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
        /// <seealso cref="P:DotNetZipAdditionalPlatforms.Zip.ZipEntry.Password">ZipEntry.Password</seealso>
        /// <seealso cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.Encryption">ZipFile.Encryption</seealso>
        public EncryptionAlgorithm Encryption
        {
            get
            {
                return this._Encryption;
            }
            set
            {
                if (value != this._Encryption)
                {
                    if (value == EncryptionAlgorithm.Unsupported)
                    {
                        throw new InvalidOperationException("You may not set Encryption to that value.");
                    }
                    this._Encryption = value;
                    this._restreamRequiredOnSave = true;
                    if (this._container.ZipFile != null)
                    {
                        this._container.ZipFile.NotifyEntryChanged();
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
        /// <seealso cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.ExtractExistingFile" />
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
                if (this.__FileDataPosition == -1L)
                {
                    this.SetFdpLoh();
                }
                return this.__FileDataPosition;
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
        /// file, via a call to <see cref="M:DotNetZipAdditionalPlatforms.Zip.ZipFile.AddFile(System.String,System.String)" /> or <see cref="M:DotNetZipAdditionalPlatforms.Zip.ZipFile.AddItem(System.String,System.String)" />, or a related overload, the value
        /// of this property is derived from the name of that file. The
        /// <c>FileName</c> property does not include drive letters, and may include a
        /// different directory path, depending on the value of the
        /// <c>directoryPathInArchive</c> parameter used when adding the entry into
        /// the <c>ZipFile</c>.
        /// </para>
        /// 
        /// <para>
        /// In some cases there is no related filesystem file - for example when a
        /// <c>ZipEntry</c> is created using <see cref="M:DotNetZipAdditionalPlatforms.Zip.ZipFile.AddEntry(System.String,System.String)" /> or one of the similar overloads.  In this case, the value of
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
        /// If an application reads a <c>ZipFile</c> via <see cref="M:DotNetZipAdditionalPlatforms.Zip.ZipFile.Read(System.String)" /> or a related overload, and then explicitly
        /// sets the FileName on an entry contained within the <c>ZipFile</c>, and
        /// then calls <see cref="M:DotNetZipAdditionalPlatforms.Zip.ZipFile.Save" />, the application will effectively
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
        /// or more of them, use <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.EntriesSorted">ZipFile.EntriesSorted</see> as the
        /// collection.  See also, <see cref="M:DotNetZipAdditionalPlatforms.Zip.ZipFile.GetEnumerator">ZipFile.GetEnumerator()</see>.
        /// </para>
        /// 
        /// </remarks>
        public string FileName
        {
            get
            {
                return this._FileNameInArchive;
            }
            set
            {
                if (this._container.ZipFile == null)
                {
                    throw new ZipException("Cannot rename; this is not supported in ZipOutputStream/ZipInputStream.");
                }
                if (string.IsNullOrEmpty(value))
                {
                    throw new ZipException("The FileName must be non empty and non-null.");
                }
                string name = NameInArchive(value, null);
                if (!(this._FileNameInArchive == name))
                {
                    this._container.ZipFile.RemoveEntry(this);
                    this._container.ZipFile.InternalAddEntry(name, this);
                    this._FileNameInArchive = name;
                    this._container.ZipFile.NotifyEntryChanged();
                    this._metadataChanged = true;
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
        /// <seealso cref="P:DotNetZipAdditionalPlatforms.Zip.ZipEntry.ZipErrorAction" />
        public bool IncludedInMostRecentSave
        {
            get
            {
                return !this._skippedDuringSave;
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
                builder.Append(string.Format(CultureInfo.InvariantCulture, "          ZipEntry: {0}\n", this.FileName)).Append(string.Format(CultureInfo.InvariantCulture, "   Version Made By: {0}\n", this._VersionMadeBy)).Append(string.Format(CultureInfo.InvariantCulture, " Needed to extract: {0}\n", this.VersionNeeded));
                if (this._IsDirectory)
                {
                    builder.Append("        Entry type: directory\n");
                }
                else
                {
                    builder.Append(string.Format(CultureInfo.InvariantCulture, "         File type: {0}\n", this._IsText ? "text" : "binary")).Append(string.Format(CultureInfo.InvariantCulture, "       Compression: {0}\n", this.CompressionMethod)).Append(string.Format(CultureInfo.InvariantCulture, "        Compressed: 0x{0:X}\n", this.CompressedSize)).Append(string.Format(CultureInfo.InvariantCulture, "      Uncompressed: 0x{0:X}\n", this.UncompressedSize)).Append(string.Format(CultureInfo.InvariantCulture, "             CRC32: 0x{0:X8}\n", this._Crc32));
                }
                builder.Append(string.Format(CultureInfo.InvariantCulture, "       Disk Number: {0}\n", this._diskNumber));
                if (this._RelativeOffsetOfLocalHeader > 0xffffffffL)
                {
                    builder.Append(string.Format(CultureInfo.InvariantCulture, "   Relative Offset: 0x{0:X16}\n", this._RelativeOffsetOfLocalHeader));
                }
                else
                {
                    builder.Append(string.Format(CultureInfo.InvariantCulture, "   Relative Offset: 0x{0:X8}\n", this._RelativeOffsetOfLocalHeader));
                }
                builder.Append(string.Format(CultureInfo.InvariantCulture, "         Bit Field: 0x{0:X4}\n", this._BitField)).Append(string.Format(CultureInfo.InvariantCulture, "        Encrypted?: {0}\n", this._sourceIsEncrypted)).Append(string.Format(CultureInfo.InvariantCulture, "          Timeblob: 0x{0:X8}\n", this._TimeBlob)).Append(string.Format(CultureInfo.InvariantCulture, "              Time: {0}\n", SharedUtilities.PackedToDateTime(this._TimeBlob)));
                builder.Append(string.Format(CultureInfo.InvariantCulture, "         Is Zip64?: {0}\n", this._InputUsesZip64));
                if (!string.IsNullOrEmpty(this._Comment))
                {
                    builder.Append(string.Format(CultureInfo.InvariantCulture, "           Comment: {0}\n", this._Comment));
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
        /// with content obtained from hundreds of streams, added through <see cref="M:DotNetZipAdditionalPlatforms.Zip.ZipFile.AddEntry(System.String,System.IO.Stream)" />.  Normally the
        /// application would supply an open stream to that call.  But when large
        /// numbers of streams are being added, this can mean many open streams at one
        /// time, unnecessarily.
        /// </para>
        /// 
        /// <para>
        /// To avoid this, call <see cref="M:DotNetZipAdditionalPlatforms.Zip.ZipFile.AddEntry(System.String,DotNetZipAdditionalPlatforms.Zip.OpenDelegate,DotNetZipAdditionalPlatforms.Zip.CloseDelegate)" /> and specify delegates that open and close the stream at
        /// the time of Save.
        /// </para>
        /// 
        /// 
        /// <para>
        /// Setting the value of this property when the entry was not added from a
        /// stream (for example, when the <c>ZipEntry</c> was added with <see cref="M:DotNetZipAdditionalPlatforms.Zip.ZipFile.AddFile(System.String)" /> or <see cref="M:DotNetZipAdditionalPlatforms.Zip.ZipFile.AddDirectory(System.String)" />, or when the entry was added by
        /// reading an existing zip archive) will throw an exception.
        /// </para>
        /// 
        /// </remarks>
        public Stream InputStream
        {
            get
            {
                return this._sourceStream;
            }
            set
            {
                if (this._Source != ZipEntrySource.Stream)
                {
                    throw new ZipException("You must not set the input stream for this entry.");
                }
                this._sourceWasJitProvided = true;
                this._sourceStream = value;
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
        /// more of the <c>ZipEntry</c> instances from streams, using the <see cref="M:DotNetZipAdditionalPlatforms.Zip.ZipFile.AddEntry(System.String,System.IO.Stream)" /> method.  At the time
        /// of calling that method, the application can supply null as the value of
        /// the stream parameter.  By doing so, the application indicates to the
        /// library that it will provide a stream for the entry on a just-in-time
        /// basis, at the time one of the <c>ZipFile.Save()</c> methods is called and
        /// the data for the various entries are being compressed and written out.
        /// </para>
        /// 
        /// <para>
        /// In this case, the application can set the <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipEntry.InputStream" />
        /// property, typically within the SaveProgress event (event type: <see cref="F:DotNetZipAdditionalPlatforms.Zip.ZipProgressEventType.Saving_BeforeWriteEntry" />) for that entry.
        /// </para>
        /// 
        /// <para>
        /// The application will later want to call Close() and Dispose() on that
        /// stream.  In the SaveProgress event, when the event type is <see cref="F:DotNetZipAdditionalPlatforms.Zip.ZipProgressEventType.Saving_AfterWriteEntry" />, the application can
        /// do so.  This flag indicates that the stream has been provided by the
        /// application on a just-in-time basis and that it is the application's
        /// responsibility to call Close/Dispose on that stream.
        /// </para>
        /// 
        /// </remarks>
        /// <seealso cref="P:DotNetZipAdditionalPlatforms.Zip.ZipEntry.InputStream" />
        public bool InputStreamWasJitProvided
        {
            get
            {
                return this._sourceWasJitProvided;
            }
        }

        internal bool IsChanged
        {
            get
            {
                return (this._restreamRequiredOnSave | this._metadataChanged);
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
                return this._IsDirectory;
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
                return this._IsText;
            }
            set
            {
                this._IsText = value;
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
        /// Unix-formatted time if desired (See <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipEntry.EmitTimesInUnixFormatWhenSaving" />.)
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
                return this._LastModified.ToLocalTime();
            }
            set
            {
                this._LastModified = (value.Kind == DateTimeKind.Unspecified) ? DateTime.SpecifyKind(value, DateTimeKind.Local) : value.ToLocalTime();
                this._Mtime = SharedUtilities.AdjustTime_Reverse(this._LastModified).ToUniversalTime();
                this._metadataChanged = true;
            }
        }

        private int LengthOfHeader
        {
            get
            {
                if (this._LengthOfHeader == 0)
                {
                    this.SetFdpLoh();
                }
                return this._LengthOfHeader;
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
        /// When it is set, the value of this property may be different than <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipEntry.FileName" />, which is the path used in the archive itself.  If you
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
        /// <seealso cref="P:DotNetZipAdditionalPlatforms.Zip.ZipEntry.FileName" />
        internal string LocalFileName
        {
            get
            {
                return this._LocalFileName;
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
        /// different from <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipEntry.LastModified" />.  When setting the property,
        /// the <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipEntry.LastModified" /> property also gets set, but with a lower
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
        /// <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipEntry.LastModified" />, is guaranteed to be present, though it
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
        /// quantities, it's more efficient to use the <see cref="M:DotNetZipAdditionalPlatforms.Zip.ZipEntry.SetEntryTimes(System.DateTime,System.DateTime,System.DateTime)" /> method.  Those
        /// changes are not made permanent in the zip file until you call <see cref="M:DotNetZipAdditionalPlatforms.Zip.ZipFile.Save" /> or one of its cousins.
        /// </para>
        /// 
        /// <para>
        /// When creating a zip file, you can override the default behavior of
        /// this library for formatting times in the zip file, disabling the
        /// embedding of file times in NTFS format or enabling the storage of file
        /// times in Unix format, or both.  You may want to do this, for example,
        /// when creating a zip file on Windows, that will be consumed on a Mac,
        /// by an application that is not hip to the "NTFS times" format. To do
        /// this, use the <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipEntry.EmitTimesInWindowsFormatWhenSaving" /> and
        /// <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipEntry.EmitTimesInUnixFormatWhenSaving" /> properties.  A valid zip
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
        /// <seealso cref="P:DotNetZipAdditionalPlatforms.Zip.ZipEntry.AccessedTime" />
        /// <seealso cref="P:DotNetZipAdditionalPlatforms.Zip.ZipEntry.CreationTime" />
        /// <seealso cref="P:DotNetZipAdditionalPlatforms.Zip.ZipEntry.LastModified" />
        /// <seealso cref="M:DotNetZipAdditionalPlatforms.Zip.ZipEntry.SetEntryTimes(System.DateTime,System.DateTime,System.DateTime)" />
        public DateTime ModifiedTime
        {
            get
            {
                return this._Mtime;
            }
            set
            {
                this.SetEntryTimes(this._Ctime, this._Atime, value);
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
        /// method on the containing <see cref="T:DotNetZipAdditionalPlatforms.Zip.ZipFile" /> instance has been
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
        /// <seealso cref="P:DotNetZipAdditionalPlatforms.Zip.ZipEntry.RequiresZip64" />
        public bool? OutputUsedZip64
        {
            get
            {
                return this._OutputUsesZip64;
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
        /// <see cref="M:DotNetZipAdditionalPlatforms.Zip.ZipFile.Save" /> operation, or to decrypt during the <see cref="M:DotNetZipAdditionalPlatforms.Zip.ZipEntry.Extract" /> or <see cref="M:DotNetZipAdditionalPlatforms.Zip.ZipEntry.OpenReader" /> operation.  If you set
        /// the Password on a <c>ZipEntry</c> after calling <c>Save()</c>, there is no
        /// effect.
        /// </para>
        /// 
        /// <para>
        /// Consider setting the <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipEntry.Encryption" /> property when using a
        /// password. Answering concerns that the standard password protection
        /// supported by all zip tools is weak, WinZip has extended the ZIP
        /// specification with a way to use AES Encryption to protect entries in the
        /// Zip file. Unlike the "PKZIP 2.0" encryption specified in the PKZIP
        /// specification, <see href="http://en.wikipedia.org/wiki/Advanced_Encryption_Standard">AES
        /// Encryption</see> uses a standard, strong, tested, encryption
        /// algorithm. DotNetZip can create zip archives that use WinZip-compatible
        /// AES encryption, if you set the <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipEntry.Encryption" /> property. But,
        /// archives created that use AES encryption may not be readable by all other
        /// tools and libraries. For example, Windows Explorer cannot read a
        /// "compressed folder" (a zip file) that uses AES encryption, though it can
        /// read a zip file that uses "PKZIP encryption."
        /// </para>
        /// 
        /// <para>
        /// The <see cref="T:DotNetZipAdditionalPlatforms.Zip.ZipFile" /> class also has a <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.Password" />
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
        /// via <see cref="M:DotNetZipAdditionalPlatforms.Zip.ZipFile.RemoveEntry(DotNetZipAdditionalPlatforms.Zip.ZipEntry)" />, and then adding a new
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
        /// <seealso cref="P:DotNetZipAdditionalPlatforms.Zip.ZipEntry.Encryption" />
        /// <seealso cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.Password">ZipFile.Password</seealso>
        public string Password
        {
            private get
            {
                return this._Password;
            }
            set
            {
                this._Password = value;
                if (this._Password == null)
                {
                    this._Encryption = EncryptionAlgorithm.None;
                }
                else
                {
                    if (!((this._Source != ZipEntrySource.ZipFile) || this._sourceIsEncrypted))
                    {
                        this._restreamRequiredOnSave = true;
                    }
                    if (this.Encryption == EncryptionAlgorithm.None)
                    {
                        this._Encryption = EncryptionAlgorithm.PkzipWeak;
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
        /// containing <see cref="T:DotNetZipAdditionalPlatforms.Zip.ZipFile" /> instance has been called. The property is
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
        /// the <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.UseZip64WhenSaving" /> property on the containing
        /// <c>ZipFile</c> instance is set to <see cref="F:DotNetZipAdditionalPlatforms.Zip.Zip64Option.Always" />, or if
        /// the <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.UseZip64WhenSaving" /> property on the containing
        /// <c>ZipFile</c> instance is set to <see cref="F:DotNetZipAdditionalPlatforms.Zip.Zip64Option.AsNecessary" />
        /// and the output stream was not seekable.
        /// </para>
        /// 
        /// </remarks>
        /// <seealso cref="P:DotNetZipAdditionalPlatforms.Zip.ZipEntry.OutputUsedZip64" />
        public bool? RequiresZip64
        {
            get
            {
                return this._entryRequiresZip64;
            }
        }

        /// <summary>
        /// A callback that allows the application to specify the compression to use
        /// for a given entry that is about to be added to the zip archive.
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// See <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.SetCompression" />
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
                return this._Source;
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
        /// <seealso cref="P:DotNetZipAdditionalPlatforms.Zip.ZipEntry.EmitTimesInWindowsFormatWhenSaving" />
        /// <seealso cref="P:DotNetZipAdditionalPlatforms.Zip.ZipEntry.EmitTimesInUnixFormatWhenSaving" />
        public ZipEntryTimestamp Timestamp
        {
            get
            {
                return this._timestamp;
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
        /// <seealso cref="P:DotNetZipAdditionalPlatforms.Zip.ZipEntry.CompressedSize" />
        public long UncompressedSize
        {
            get
            {
                return this._UncompressedSize;
            }
        }

        private string UnsupportedAlgorithm
        {
            get
            {
                switch (this._UnsupportedAlgorithmId)
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
                return string.Format(CultureInfo.InvariantCulture, "Unknown (0x{0:X4})", this._UnsupportedAlgorithmId);
            }
        }

        private string UnsupportedCompressionMethod
        {
            get
            {
                switch (this._CompressionMethod)
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
                return string.Format(CultureInfo.InvariantCulture, "Unknown (0x{0:X4})", this._CompressionMethod);
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
        /// actually used (which will have been true if the <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipEntry.Password" /> was set and the <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipEntry.Encryption" /> property
        /// was something other than <see cref="F:DotNetZipAdditionalPlatforms.Zip.EncryptionAlgorithm.None" />.
        /// </para>
        /// </remarks>
        public bool UsesEncryption
        {
            get
            {
                return (this._Encryption_FromZipFile != EncryptionAlgorithm.None);
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
                return this._VersionNeeded;
            }
        }

        /// <summary>
        /// The action to take when an error is encountered while
        /// opening or reading files as they are saved into a zip archive.
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// Errors can occur within a call to <see cref="M:DotNetZipAdditionalPlatforms.Zip.ZipFile.Save">ZipFile.Save</see>, as the various files contained
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
        /// instances.  Instead, you will set the <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.ZipErrorAction">ZipFile.ZipErrorAction</see> property on
        /// the ZipFile instance, before adding any entries to the
        /// <c>ZipFile</c>. If you do this, errors encountered on behalf of any of
        /// the entries in the ZipFile will be handled the same way.
        /// </para>
        /// 
        /// <para>
        /// But, if you use a <see cref="E:DotNetZipAdditionalPlatforms.Zip.ZipFile.ZipError" /> handler, you will want
        /// to set this property on the <c>ZipEntry</c> within the handler, to
        /// communicate back to DotNetZip what you would like to do with the
        /// particular error.
        /// </para>
        /// 
        /// </remarks>
        /// <seealso cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.ZipErrorAction" />
        /// <seealso cref="E:DotNetZipAdditionalPlatforms.Zip.ZipFile.ZipError" />
        public DotNetZipAdditionalPlatforms.Zip.ZipErrorAction ZipErrorAction { get; set; }

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

