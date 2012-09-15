namespace DotNetZipAdditionalPlatforms.Zip
{
    using DotNetZipAdditionalPlatforms;
    using DotNetZipAdditionalPlatforms.Zlib;
    using Microsoft.CSharp;
    using System;
    using System.CodeDom.Compiler;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Reflection;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Threading;

    /// <summary>
    /// The ZipFile type represents a zip archive file.
    /// </summary>
    /// 
    /// <remarks>
    /// <para>
    /// This is the main type in the DotNetZip class library. This class reads and
    /// writes zip files, as defined in the <see href="http://www.pkware.com/documents/casestudies/APPNOTE.TXT">specification
    /// for zip files described by PKWare</see>.  The compression for this
    /// implementation is provided by a managed-code version of Zlib, included with
    /// DotNetZip in the classes in the DotNetZipAdditionalPlatforms.Zlib namespace.
    /// </para>
    /// 
    /// <para>
    /// This class provides a general purpose zip file capability.  Use it to read,
    /// create, or update zip files.  When you want to create zip files using a
    /// <c>Stream</c> type to write the zip file, you may want to consider the <see cref="T:DotNetZipAdditionalPlatforms.Zip.ZipOutputStream" /> class.
    /// </para>
    /// 
    /// <para>
    /// Both the <c>ZipOutputStream</c> class and the <c>ZipFile</c> class can
    /// be used to create zip files. Both of them support many of the common zip
    /// features, including Unicode, different compression methods and levels,
    /// and ZIP64. They provide very similar performance when creating zip
    /// files.
    /// </para>
    /// 
    /// <para>
    /// The <c>ZipFile</c> class is generally easier to use than
    /// <c>ZipOutputStream</c> and should be considered a higher-level interface.  For
    /// example, when creating a zip file via calls to the <c>PutNextEntry()</c> and
    /// <c>Write()</c> methods on the <c>ZipOutputStream</c> class, the caller is
    /// responsible for opening the file, reading the bytes from the file, writing
    /// those bytes into the <c>ZipOutputStream</c>, setting the attributes on the
    /// <c>ZipEntry</c>, and setting the created, last modified, and last accessed
    /// timestamps on the zip entry. All of these things are done automatically by a
    /// call to <see cref="M:DotNetZipAdditionalPlatforms.Zip.ZipFile.AddFile(System.String,System.String)">ZipFile.AddFile()</see>.
    /// For this reason, the <c>ZipOutputStream</c> is generally recommended for use
    /// only when your application emits arbitrary data, not necessarily data from a
    /// filesystem file, directly into a zip file, and does so using a <c>Stream</c>
    /// metaphor.
    /// </para>
    /// 
    /// <para>
    /// Aside from the differences in programming model, there are other
    /// differences in capability between the two classes.
    /// </para>
    /// 
    /// <list type="bullet">
    /// <item>
    /// <c>ZipFile</c> can be used to read and extract zip files, in addition to
    /// creating zip files. <c>ZipOutputStream</c> cannot read zip files. If you want
    /// to use a stream to read zip files, check out the <see cref="T:DotNetZipAdditionalPlatforms.Zip.ZipInputStream" /> class.
    /// </item>
    /// 
    /// <item>
    /// <c>ZipOutputStream</c> does not support the creation of segmented or spanned
    /// zip files.
    /// </item>
    /// 
    /// <item>
    /// <c>ZipOutputStream</c> cannot produce a self-extracting archive.
    /// </item>
    /// </list>
    /// 
    /// <para>
    /// Be aware that the <c>ZipFile</c> class implements the <see cref="T:System.IDisposable" /> interface.  In order for <c>ZipFile</c> to
    /// produce a valid zip file, you use use it within a using clause (<c>Using</c>
    /// in VB), or call the <c>Dispose()</c> method explicitly.  See the examples
    /// for how to employ a using clause.
    /// </para>
    /// 
    /// </remarks>
    [ClassInterface(ClassInterfaceType.AutoDispatch), Guid("ebc25cf6-9120-4283-b972-0e5520d00005"), ComVisible(true)]
    public class ZipFile : IEnumerable<ZipEntry>, IEnumerable, IDisposable
    {
        private bool _addOperationCanceled;
        private Encoding _alternateEncoding;
        private ZipOption _alternateEncodingUsage;
        private int _BufferSize;
        private bool _CaseSensitiveRetrieval;
        private string _Comment;
        private DotNetZipAdditionalPlatforms.Zip.CompressionMethod _compressionMethod;
        private bool _contentsChanged;
        private static Encoding _defaultEncoding = Encoding.GetEncoding("IBM437");
        private uint _diskNumberWithCd;
        private bool _disposed;
        private bool _emitNtfsTimes;
        private bool _emitUnixTimes;
        private EncryptionAlgorithm _Encryption;
        private Dictionary<string, ZipEntry> _entries;
        private bool _extractOperationCanceled;
        private bool _fileAlreadyExists;
        private bool _hasBeenSaved;
        internal bool _inExtractAll;
        private bool _JustSaved;
        private long _lengthOfReadStream;
        private long _locEndOfCDS;
        private int _maxBufferPairs;
        private int _maxOutputSegmentSize;
        private string _name;
        private uint _numberOfSegmentsForMostRecentSave;
        private uint _OffsetOfCentralDirectory;
        private long _OffsetOfCentralDirectory64;
        private bool? _OutputUsesZip64;
        private long _ParallelDeflateThreshold;
        internal string _Password;
        private string _readName;
        private Stream _readstream;
        private bool _ReadStreamIsOurs;
        private bool _saveOperationCanceled;
        private bool _SavingSfx;
        private TextWriter _StatusMessageTextWriter;
        private CompressionStrategy _Strategy;
        private string _TempFileFolder;
        private string _temporaryFileName;
        private ushort _versionMadeBy;
        private ushort _versionNeededToExtract;
        private Stream _writestream;
        internal Zip64Option _zip64;
        private List<ZipEntry> _zipEntriesAsList;
        private DotNetZipAdditionalPlatforms.Zip.ZipErrorAction _zipErrorAction;
        /// <summary>
        /// Default size of the buffer used for IO.
        /// </summary>
        public const int BufferSizeDefault = 0x8000;
        private object LOCK;
        internal ParallelDeflateOutputStream ParallelDeflater;
        private static ExtractorSettings[] SettingsList;

        /// <summary>
        /// An event handler invoked before, during, and after Adding entries to a zip archive.
        /// </summary>
        /// 
        /// <remarks>
        /// Adding a large number of entries to a zip file can take a long
        /// time.  For example, when calling <see cref="M:DotNetZipAdditionalPlatforms.Zip.ZipFile.AddDirectory(System.String)" /> on a
        /// directory that contains 50,000 files, it could take 3 minutes or so.
        /// This event handler allws an application to track the progress of the Add
        /// operation, and to optionally cancel a lengthy Add operation.
        /// </remarks>
        /// 
        /// <example>
        /// <code lang="C#">
        /// 
        /// int _numEntriesToAdd= 0;
        /// int _numEntriesAdded= 0;
        /// void AddProgressHandler(object sender, AddProgressEventArgs e)
        /// {
        /// switch (e.EventType)
        /// {
        /// case ZipProgressEventType.Adding_Started:
        /// Console.WriteLine("Adding files to the zip...");
        /// break;
        /// case ZipProgressEventType.Adding_AfterAddEntry:
        /// _numEntriesAdded++;
        /// Console.WriteLine(string.Format(CultureInfo.InvariantCulture, "Adding file {0}/{1} :: {2}",
        /// _numEntriesAdded, _numEntriesToAdd, e.CurrentEntry.FileName));
        /// break;
        /// case ZipProgressEventType.Adding_Completed:
        /// Console.WriteLine("Added all files");
        /// break;
        /// }
        /// }
        /// 
        /// void CreateTheZip()
        /// {
        /// using (ZipFile zip = new ZipFile())
        /// {
        /// zip.AddProgress += AddProgressHandler;
        /// zip.AddDirectory(System.IO.Path.GetFileName(DirToZip));
        /// zip.Save(ZipFileToCreate);
        /// }
        /// }
        /// 
        /// </code>
        /// 
        /// <code lang="VB">
        /// 
        /// Private Sub AddProgressHandler(ByVal sender As Object, ByVal e As AddProgressEventArgs)
        /// Select Case e.EventType
        /// Case ZipProgressEventType.Adding_Started
        /// Console.WriteLine("Adding files to the zip...")
        /// Exit Select
        /// Case ZipProgressEventType.Adding_AfterAddEntry
        /// Console.WriteLine(string.Format(CultureInfo.InvariantCulture, "Adding file {0}", e.CurrentEntry.FileName))
        /// Exit Select
        /// Case ZipProgressEventType.Adding_Completed
        /// Console.WriteLine("Added all files")
        /// Exit Select
        /// End Select
        /// End Sub
        /// 
        /// Sub CreateTheZip()
        /// Using zip as ZipFile = New ZipFile
        /// AddHandler zip.AddProgress, AddressOf AddProgressHandler
        /// zip.AddDirectory(System.IO.Path.GetFileName(DirToZip))
        /// zip.Save(ZipFileToCreate);
        /// End Using
        /// End Sub
        /// 
        /// </code>
        /// 
        /// </example>
        /// 
        /// <seealso cref="E:DotNetZipAdditionalPlatforms.Zip.ZipFile.SaveProgress" />
        /// <seealso cref="E:DotNetZipAdditionalPlatforms.Zip.ZipFile.ReadProgress" />
        /// <seealso cref="E:DotNetZipAdditionalPlatforms.Zip.ZipFile.ExtractProgress" />
        public event EventHandler<AddProgressEventArgs> AddProgress;

        /// <summary>
        /// An event handler invoked before, during, and after extraction of
        /// entries in the zip archive.
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// Depending on the particular event, different properties on the <see cref="T:DotNetZipAdditionalPlatforms.Zip.ExtractProgressEventArgs" /> parameter are set.  The following
        /// table summarizes the available EventTypes and the conditions under
        /// which this event handler is invoked with a
        /// <c>ExtractProgressEventArgs</c> with the given EventType.
        /// </para>
        /// 
        /// <list type="table">
        /// <listheader>
        /// <term>value of EntryType</term>
        /// <description>Meaning and conditions</description>
        /// </listheader>
        /// 
        /// <item>
        /// <term>ZipProgressEventType.Extracting_BeforeExtractAll</term>
        /// <description>
        /// Set when ExtractAll() begins. The ArchiveName, Overwrite, and
        /// ExtractLocation properties are meaningful.</description>
        /// </item>
        /// 
        /// <item>
        /// <term>ZipProgressEventType.Extracting_AfterExtractAll</term>
        /// <description>
        /// Set when ExtractAll() has completed.  The ArchiveName, Overwrite,
        /// and ExtractLocation properties are meaningful.
        /// </description>
        /// </item>
        /// 
        /// <item>
        /// <term>ZipProgressEventType.Extracting_BeforeExtractEntry</term>
        /// <description>
        /// Set when an Extract() on an entry in the ZipFile has begun.
        /// Properties that are meaningful: ArchiveName, EntriesTotal,
        /// CurrentEntry, Overwrite, ExtractLocation, EntriesExtracted.
        /// </description>
        /// </item>
        /// 
        /// <item>
        /// <term>ZipProgressEventType.Extracting_AfterExtractEntry</term>
        /// <description>
        /// Set when an Extract() on an entry in the ZipFile has completed.
        /// Properties that are meaningful: ArchiveName, EntriesTotal,
        /// CurrentEntry, Overwrite, ExtractLocation, EntriesExtracted.
        /// </description>
        /// </item>
        /// 
        /// <item>
        /// <term>ZipProgressEventType.Extracting_EntryBytesWritten</term>
        /// <description>
        /// Set within a call to Extract() on an entry in the ZipFile, as data
        /// is extracted for the entry.  Properties that are meaningful:
        /// ArchiveName, CurrentEntry, BytesTransferred, TotalBytesToTransfer.
        /// </description>
        /// </item>
        /// 
        /// <item>
        /// <term>ZipProgressEventType.Extracting_ExtractEntryWouldOverwrite</term>
        /// <description>
        /// Set within a call to Extract() on an entry in the ZipFile, when the
        /// extraction would overwrite an existing file. This event type is used
        /// only when <c>ExtractExistingFileAction</c> on the <c>ZipFile</c> or
        /// <c>ZipEntry</c> is set to <c>InvokeExtractProgressEvent</c>.
        /// </description>
        /// </item>
        /// 
        /// </list>
        /// 
        /// </remarks>
        /// 
        /// <example>
        /// <code>
        /// private static bool justHadByteUpdate = false;
        /// public static void ExtractProgress(object sender, ExtractProgressEventArgs e)
        /// {
        /// if(e.EventType == ZipProgressEventType.Extracting_EntryBytesWritten)
        /// {
        /// if (justHadByteUpdate)
        /// Console.SetCursorPosition(0, Console.CursorTop);
        /// 
        /// Console.Write("   {0}/{1} ({2:N0}%)", e.BytesTransferred, e.TotalBytesToTransfer,
        /// e.BytesTransferred / (0.01 * e.TotalBytesToTransfer ));
        /// justHadByteUpdate = true;
        /// }
        /// else if(e.EventType == ZipProgressEventType.Extracting_BeforeExtractEntry)
        /// {
        /// if (justHadByteUpdate)
        /// Console.WriteLine();
        /// Console.WriteLine("Extracting: {0}", e.CurrentEntry.FileName);
        /// justHadByteUpdate= false;
        /// }
        /// }
        /// 
        /// public static ExtractZip(string zipToExtract, string directory)
        /// {
        /// string TargetDirectory= "extract";
        /// using (var zip = ZipFile.Read(zipToExtract)) {
        /// zip.ExtractProgress += ExtractProgress;
        /// foreach (var e in zip1)
        /// {
        /// e.Extract(TargetDirectory, true);
        /// }
        /// }
        /// }
        /// 
        /// </code>
        /// <code lang="VB">
        /// Public Shared Sub Main(ByVal args As String())
        /// Dim ZipToUnpack As String = "C1P3SML.zip"
        /// Dim TargetDir As String = "ExtractTest_Extract"
        /// Console.WriteLine("Extracting file {0} to {1}", ZipToUnpack, TargetDir)
        /// Using zip1 As ZipFile = ZipFile.Read(ZipToUnpack)
        /// AddHandler zip1.ExtractProgress, AddressOf MyExtractProgress
        /// Dim e As ZipEntry
        /// For Each e In zip1
        /// e.Extract(TargetDir, True)
        /// Next
        /// End Using
        /// End Sub
        /// 
        /// Private Shared justHadByteUpdate As Boolean = False
        /// 
        /// Public Shared Sub MyExtractProgress(ByVal sender As Object, ByVal e As ExtractProgressEventArgs)
        /// If (e.EventType = ZipProgressEventType.Extracting_EntryBytesWritten) Then
        /// If ExtractTest.justHadByteUpdate Then
        /// Console.SetCursorPosition(0, Console.CursorTop)
        /// End If
        /// Console.Write("   {0}/{1} ({2:N0}%)", e.BytesTransferred, e.TotalBytesToTransfer, (CDbl(e.BytesTransferred) / (0.01 * e.TotalBytesToTransfer)))
        /// ExtractTest.justHadByteUpdate = True
        /// ElseIf (e.EventType = ZipProgressEventType.Extracting_BeforeExtractEntry) Then
        /// If ExtractTest.justHadByteUpdate Then
        /// Console.WriteLine
        /// End If
        /// Console.WriteLine("Extracting: {0}", e.CurrentEntry.FileName)
        /// ExtractTest.justHadByteUpdate = False
        /// End If
        /// End Sub
        /// </code>
        /// </example>
        /// 
        /// <seealso cref="E:DotNetZipAdditionalPlatforms.Zip.ZipFile.SaveProgress" />
        /// <seealso cref="E:DotNetZipAdditionalPlatforms.Zip.ZipFile.ReadProgress" />
        /// <seealso cref="E:DotNetZipAdditionalPlatforms.Zip.ZipFile.AddProgress" />
        public event EventHandler<ExtractProgressEventArgs> ExtractProgress;

        /// <summary>
        /// An event handler invoked before, during, and after the reading of a zip archive.
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// Depending on the particular event being signaled, different properties on the
        /// <see cref="T:DotNetZipAdditionalPlatforms.Zip.ReadProgressEventArgs" /> parameter are set.  The following table
        /// summarizes the available EventTypes and the conditions under which this
        /// event handler is invoked with a <c>ReadProgressEventArgs</c> with the given EventType.
        /// </para>
        /// 
        /// <list type="table">
        /// <listheader>
        /// <term>value of EntryType</term>
        /// <description>Meaning and conditions</description>
        /// </listheader>
        /// 
        /// <item>
        /// <term>ZipProgressEventType.Reading_Started</term>
        /// <description>Fired just as ZipFile.Read() begins. Meaningful properties: ArchiveName.
        /// </description>
        /// </item>
        /// 
        /// <item>
        /// <term>ZipProgressEventType.Reading_Completed</term>
        /// <description>Fired when ZipFile.Read() has completed. Meaningful properties: ArchiveName.
        /// </description>
        /// </item>
        /// 
        /// <item>
        /// <term>ZipProgressEventType.Reading_ArchiveBytesRead</term>
        /// <description>Fired while reading, updates the number of bytes read for the entire archive.
        /// Meaningful properties: ArchiveName, CurrentEntry, BytesTransferred, TotalBytesToTransfer.
        /// </description>
        /// </item>
        /// 
        /// <item>
        /// <term>ZipProgressEventType.Reading_BeforeReadEntry</term>
        /// <description>Indicates an entry is about to be read from the archive.
        /// Meaningful properties: ArchiveName, EntriesTotal.
        /// </description>
        /// </item>
        /// 
        /// <item>
        /// <term>ZipProgressEventType.Reading_AfterReadEntry</term>
        /// <description>Indicates an entry has just been read from the archive.
        /// Meaningful properties: ArchiveName, EntriesTotal, CurrentEntry.
        /// </description>
        /// </item>
        /// 
        /// </list>
        /// </remarks>
        /// 
        /// <seealso cref="E:DotNetZipAdditionalPlatforms.Zip.ZipFile.SaveProgress" />
        /// <seealso cref="E:DotNetZipAdditionalPlatforms.Zip.ZipFile.AddProgress" />
        /// <seealso cref="E:DotNetZipAdditionalPlatforms.Zip.ZipFile.ExtractProgress" />
        public event EventHandler<ReadProgressEventArgs> ReadProgress;

        /// <summary>
        /// An event handler invoked when a Save() starts, before and after each
        /// entry has been written to the archive, when a Save() completes, and
        /// during other Save events.
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// Depending on the particular event, different properties on the <see cref="T:DotNetZipAdditionalPlatforms.Zip.SaveProgressEventArgs" /> parameter are set.  The following
        /// table summarizes the available EventTypes and the conditions under
        /// which this event handler is invoked with a
        /// <c>SaveProgressEventArgs</c> with the given EventType.
        /// </para>
        /// 
        /// <list type="table">
        /// <listheader>
        /// <term>value of EntryType</term>
        /// <description>Meaning and conditions</description>
        /// </listheader>
        /// 
        /// <item>
        /// <term>ZipProgressEventType.Saving_Started</term>
        /// <description>Fired when ZipFile.Save() begins.
        /// </description>
        /// </item>
        /// 
        /// <item>
        /// <term>ZipProgressEventType.Saving_BeforeSaveEntry</term>
        /// <description>
        /// Fired within ZipFile.Save(), just before writing data for each
        /// particular entry.
        /// </description>
        /// </item>
        /// 
        /// <item>
        /// <term>ZipProgressEventType.Saving_AfterSaveEntry</term>
        /// <description>
        /// Fired within ZipFile.Save(), just after having finished writing data
        /// for each particular entry.
        /// </description>
        /// </item>
        /// 
        /// <item>
        /// <term>ZipProgressEventType.Saving_Completed</term>
        /// <description>Fired when ZipFile.Save() has completed.
        /// </description>
        /// </item>
        /// 
        /// <item>
        /// <term>ZipProgressEventType.Saving_AfterSaveTempArchive</term>
        /// <description>
        /// Fired after the temporary file has been created.  This happens only
        /// when saving to a disk file.  This event will not be invoked when
        /// saving to a stream.
        /// </description>
        /// </item>
        /// 
        /// <item>
        /// <term>ZipProgressEventType.Saving_BeforeRenameTempArchive</term>
        /// <description>
        /// Fired just before renaming the temporary file to the permanent
        /// location.  This happens only when saving to a disk file.  This event
        /// will not be invoked when saving to a stream.
        /// </description>
        /// </item>
        /// 
        /// <item>
        /// <term>ZipProgressEventType.Saving_AfterRenameTempArchive</term>
        /// <description>
        /// Fired just after renaming the temporary file to the permanent
        /// location.  This happens only when saving to a disk file.  This event
        /// will not be invoked when saving to a stream.
        /// </description>
        /// </item>
        /// 
        /// <item>
        /// <term>ZipProgressEventType.Saving_AfterCompileSelfExtractor</term>
        /// <description>
        /// Fired after a self-extracting archive has finished compiling.  This
        /// EventType is used only within SaveSelfExtractor().
        /// </description>
        /// </item>
        /// 
        /// <item>
        /// <term>ZipProgressEventType.Saving_BytesRead</term>
        /// <description>
        /// Set during the save of a particular entry, to update progress of the
        /// Save().  When this EventType is set, the BytesTransferred is the
        /// number of bytes that have been read from the source stream.  The
        /// TotalBytesToTransfer is the number of bytes in the uncompressed
        /// file.
        /// </description>
        /// </item>
        /// 
        /// </list>
        /// </remarks>
        /// 
        /// <example>
        /// 
        /// This example uses an anonymous method to handle the
        /// SaveProgress event, by updating a progress bar.
        /// 
        /// <code lang="C#">
        /// progressBar1.Value = 0;
        /// progressBar1.Max = listbox1.Items.Count;
        /// using (ZipFile zip = new ZipFile())
        /// {
        /// // listbox1 contains a list of filenames
        /// zip.AddFiles(listbox1.Items);
        /// 
        /// // do the progress bar:
        /// zip.SaveProgress += (sender, e) =&gt; {
        /// if (e.EventType == ZipProgressEventType.Saving_BeforeWriteEntry) {
        /// progressBar1.PerformStep();
        /// }
        /// };
        /// 
        /// zip.Save(fs);
        /// }
        /// </code>
        /// </example>
        /// 
        /// <example>
        /// This example uses a named method as the
        /// <c>SaveProgress</c> event handler, to update the user, in a
        /// console-based application.
        /// 
        /// <code lang="C#">
        /// static bool justHadByteUpdate= false;
        /// public static void SaveProgress(object sender, SaveProgressEventArgs e)
        /// {
        /// if (e.EventType == ZipProgressEventType.Saving_Started)
        /// Console.WriteLine("Saving: {0}", e.ArchiveName);
        /// 
        /// else if (e.EventType == ZipProgressEventType.Saving_Completed)
        /// {
        /// justHadByteUpdate= false;
        /// Console.WriteLine();
        /// Console.WriteLine("Done: {0}", e.ArchiveName);
        /// }
        /// 
        /// else if (e.EventType == ZipProgressEventType.Saving_BeforeWriteEntry)
        /// {
        /// if (justHadByteUpdate)
        /// Console.WriteLine();
        /// Console.WriteLine("  Writing: {0} ({1}/{2})",
        /// e.CurrentEntry.FileName, e.EntriesSaved, e.EntriesTotal);
        /// justHadByteUpdate= false;
        /// }
        /// 
        /// else if (e.EventType == ZipProgressEventType.Saving_EntryBytesRead)
        /// {
        /// if (justHadByteUpdate)
        /// Console.SetCursorPosition(0, Console.CursorTop);
        /// Console.Write("     {0}/{1} ({2:N0}%)", e.BytesTransferred, e.TotalBytesToTransfer,
        /// e.BytesTransferred / (0.01 * e.TotalBytesToTransfer ));
        /// justHadByteUpdate= true;
        /// }
        /// }
        /// 
        /// public static ZipUp(string targetZip, string directory)
        /// {
        /// using (var zip = new ZipFile()) {
        /// zip.SaveProgress += SaveProgress;
        /// zip.AddDirectory(directory);
        /// zip.Save(targetZip);
        /// }
        /// }
        /// 
        /// </code>
        /// 
        /// <code lang="VB">
        /// Public Sub ZipUp(ByVal targetZip As String, ByVal directory As String)
        /// Using zip As ZipFile = New ZipFile
        /// AddHandler zip.SaveProgress, AddressOf MySaveProgress
        /// zip.AddDirectory(directory)
        /// zip.Save(targetZip)
        /// End Using
        /// End Sub
        /// 
        /// Private Shared justHadByteUpdate As Boolean = False
        /// 
        /// Public Shared Sub MySaveProgress(ByVal sender As Object, ByVal e As SaveProgressEventArgs)
        /// If (e.EventType Is ZipProgressEventType.Saving_Started) Then
        /// Console.WriteLine("Saving: {0}", e.ArchiveName)
        /// 
        /// ElseIf (e.EventType Is ZipProgressEventType.Saving_Completed) Then
        /// justHadByteUpdate = False
        /// Console.WriteLine
        /// Console.WriteLine("Done: {0}", e.ArchiveName)
        /// 
        /// ElseIf (e.EventType Is ZipProgressEventType.Saving_BeforeWriteEntry) Then
        /// If justHadByteUpdate Then
        /// Console.WriteLine
        /// End If
        /// Console.WriteLine("  Writing: {0} ({1}/{2})", e.CurrentEntry.FileName, e.EntriesSaved, e.EntriesTotal)
        /// justHadByteUpdate = False
        /// 
        /// ElseIf (e.EventType Is ZipProgressEventType.Saving_EntryBytesRead) Then
        /// If justHadByteUpdate Then
        /// Console.SetCursorPosition(0, Console.CursorTop)
        /// End If
        /// Console.Write("     {0}/{1} ({2:N0}%)", e.BytesTransferred, _
        /// e.TotalBytesToTransfer, _
        /// (CDbl(e.BytesTransferred) / (0.01 * e.TotalBytesToTransfer)))
        /// justHadByteUpdate = True
        /// End If
        /// End Sub
        /// </code>
        /// </example>
        /// 
        /// <example>
        /// 
        /// This is a more complete example of using the SaveProgress
        /// events in a Windows Forms application, with a
        /// Thread object.
        /// 
        /// <code lang="C#">
        /// delegate void SaveEntryProgress(SaveProgressEventArgs e);
        /// delegate void ButtonClick(object sender, EventArgs e);
        /// 
        /// public class WorkerOptions
        /// {
        /// public string ZipName;
        /// public string Folder;
        /// public string Encoding;
        /// public string Comment;
        /// public int ZipFlavor;
        /// public Zip64Option Zip64;
        /// }
        /// 
        /// private int _progress2MaxFactor;
        /// private bool _saveCanceled;
        /// private long _totalBytesBeforeCompress;
        /// private long _totalBytesAfterCompress;
        /// private Thread _workerThread;
        /// 
        /// 
        /// private void btnZipup_Click(object sender, EventArgs e)
        /// {
        /// KickoffZipup();
        /// }
        /// 
        /// private void btnCancel_Click(object sender, EventArgs e)
        /// {
        /// if (this.lblStatus.InvokeRequired)
        /// {
        /// this.lblStatus.Invoke(new ButtonClick(this.btnCancel_Click), new object[] { sender, e });
        /// }
        /// else
        /// {
        /// _saveCanceled = true;
        /// lblStatus.Text = "Canceled...";
        /// ResetState();
        /// }
        /// }
        /// 
        /// private void KickoffZipup()
        /// {
        /// _folderName = tbDirName.Text;
        /// 
        /// if (_folderName == null || _folderName == "") return;
        /// if (this.tbZipName.Text == null || this.tbZipName.Text == "") return;
        /// 
        /// // check for existence of the zip file:
        /// if (System.IO.File.Exists(this.tbZipName.Text))
        /// {
        /// var dlgResult = MessageBox.Show(string.Format(CultureInfo.InvariantCulture, "The file you have specified ({0}) already exists." +
        /// "  Do you want to overwrite this file?", this.tbZipName.Text),
        /// "Confirmation is Required", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        /// if (dlgResult != DialogResult.Yes) return;
        /// System.IO.File.Delete(this.tbZipName.Text);
        /// }
        /// 
        /// _saveCanceled = false;
        /// _nFilesCompleted = 0;
        /// _totalBytesAfterCompress = 0;
        /// _totalBytesBeforeCompress = 0;
        /// this.btnOk.Enabled = false;
        /// this.btnOk.Text = "Zipping...";
        /// this.btnCancel.Enabled = true;
        /// lblStatus.Text = "Zipping...";
        /// 
        /// var options = new WorkerOptions
        /// {
        /// ZipName = this.tbZipName.Text,
        /// Folder = _folderName,
        /// Encoding = "ibm437"
        /// };
        /// 
        /// if (this.comboBox1.SelectedIndex != 0)
        /// {
        /// options.Encoding = this.comboBox1.SelectedItem.ToString();
        /// }
        /// 
        /// if (this.radioFlavorSfxCmd.Checked)
        /// options.ZipFlavor = 2;
        /// else if (this.radioFlavorSfxGui.Checked)
        /// options.ZipFlavor = 1;
        /// else options.ZipFlavor = 0;
        /// 
        /// if (this.radioZip64AsNecessary.Checked)
        /// options.Zip64 = Zip64Option.AsNecessary;
        /// else if (this.radioZip64Always.Checked)
        /// options.Zip64 = Zip64Option.Always;
        /// else options.Zip64 = Zip64Option.Never;
        /// 
        /// options.Comment = string.Format(CultureInfo.InvariantCulture, "Encoding:{0} || Flavor:{1} || ZIP64:{2}\r\nCreated at {3} || {4}\r\n",
        /// options.Encoding,
        /// FlavorToString(options.ZipFlavor),
        /// options.Zip64.ToString(),
        /// System.DateTime.Now.ToString("yyyy-MMM-dd HH:mm:ss"),
        /// this.Text);
        /// 
        /// if (this.tbComment.Text != TB_COMMENT_NOTE)
        /// options.Comment += this.tbComment.Text;
        /// 
        /// _workerThread = new Thread(this.DoSave);
        /// _workerThread.Name = "Zip Saver thread";
        /// _workerThread.Start(options);
        /// this.Cursor = Cursors.WaitCursor;
        /// }
        /// 
        /// 
        /// private void DoSave(Object p)
        /// {
        /// WorkerOptions options = p as WorkerOptions;
        /// try
        /// {
        /// using (var zip1 = new ZipFile())
        /// {
        /// zip1.ProvisionalAlternateEncoding = System.Text.Encoding.GetEncoding(options.Encoding);
        /// zip1.Comment = options.Comment;
        /// zip1.AddDirectory(options.Folder);
        /// _entriesToZip = zip1.EntryFileNames.Count;
        /// SetProgressBars();
        /// zip1.SaveProgress += this.zip1_SaveProgress;
        /// 
        /// zip1.UseZip64WhenSaving = options.Zip64;
        /// 
        /// if (options.ZipFlavor == 1)
        /// zip1.SaveSelfExtractor(options.ZipName, SelfExtractorFlavor.WinFormsApplication);
        /// else if (options.ZipFlavor == 2)
        /// zip1.SaveSelfExtractor(options.ZipName, SelfExtractorFlavor.ConsoleApplication);
        /// else
        /// zip1.Save(options.ZipName);
        /// }
        /// }
        /// catch (System.Exception exc1)
        /// {
        /// MessageBox.Show(string.Format(CultureInfo.InvariantCulture, "Exception while zipping: {0}", exc1.Message));
        /// btnCancel_Click(null, null);
        /// }
        /// }
        /// 
        /// 
        /// 
        /// void zip1_SaveProgress(object sender, SaveProgressEventArgs e)
        /// {
        /// switch (e.EventType)
        /// {
        /// case ZipProgressEventType.Saving_AfterWriteEntry:
        /// StepArchiveProgress(e);
        /// break;
        /// case ZipProgressEventType.Saving_EntryBytesRead:
        /// StepEntryProgress(e);
        /// break;
        /// case ZipProgressEventType.Saving_Completed:
        /// SaveCompleted();
        /// break;
        /// case ZipProgressEventType.Saving_AfterSaveTempArchive:
        /// // this event only occurs when saving an SFX file
        /// TempArchiveSaved();
        /// break;
        /// }
        /// if (_saveCanceled)
        /// e.Cancel = true;
        /// }
        /// 
        /// 
        /// 
        /// private void StepArchiveProgress(SaveProgressEventArgs e)
        /// {
        /// if (this.progressBar1.InvokeRequired)
        /// {
        /// this.progressBar1.Invoke(new SaveEntryProgress(this.StepArchiveProgress), new object[] { e });
        /// }
        /// else
        /// {
        /// if (!_saveCanceled)
        /// {
        /// _nFilesCompleted++;
        /// this.progressBar1.PerformStep();
        /// _totalBytesAfterCompress += e.CurrentEntry.CompressedSize;
        /// _totalBytesBeforeCompress += e.CurrentEntry.UncompressedSize;
        /// 
        /// // reset the progress bar for the entry:
        /// this.progressBar2.Value = this.progressBar2.Maximum = 1;
        /// 
        /// this.Update();
        /// }
        /// }
        /// }
        /// 
        /// 
        /// private void StepEntryProgress(SaveProgressEventArgs e)
        /// {
        /// if (this.progressBar2.InvokeRequired)
        /// {
        /// this.progressBar2.Invoke(new SaveEntryProgress(this.StepEntryProgress), new object[] { e });
        /// }
        /// else
        /// {
        /// if (!_saveCanceled)
        /// {
        /// if (this.progressBar2.Maximum == 1)
        /// {
        /// // reset
        /// Int64 max = e.TotalBytesToTransfer;
        /// _progress2MaxFactor = 0;
        /// while (max &gt; System.Int32.MaxValue)
        /// {
        /// max /= 2;
        /// _progress2MaxFactor++;
        /// }
        /// this.progressBar2.Maximum = (int)max;
        /// lblStatus.Text = string.Format(CultureInfo.InvariantCulture, "{0} of {1} files...({2})",
        /// _nFilesCompleted + 1, _entriesToZip, e.CurrentEntry.FileName);
        /// }
        /// 
        /// int xferred = e.BytesTransferred &gt;&gt; _progress2MaxFactor;
        /// 
        /// this.progressBar2.Value = (xferred &gt;= this.progressBar2.Maximum)
        /// ? this.progressBar2.Maximum
        /// : xferred;
        /// 
        /// this.Update();
        /// }
        /// }
        /// }
        /// 
        /// private void SaveCompleted()
        /// {
        /// if (this.lblStatus.InvokeRequired)
        /// {
        /// this.lblStatus.Invoke(new MethodInvoker(this.SaveCompleted));
        /// }
        /// else
        /// {
        /// lblStatus.Text = string.Format(CultureInfo.InvariantCulture, "Done, Compressed {0} files, {1:N0}% of original.",
        /// _nFilesCompleted, (100.00 * _totalBytesAfterCompress) / _totalBytesBeforeCompress);
        /// ResetState();
        /// }
        /// }
        /// 
        /// private void ResetState()
        /// {
        /// this.btnCancel.Enabled = false;
        /// this.btnOk.Enabled = true;
        /// this.btnOk.Text = "Zip it!";
        /// this.progressBar1.Value = 0;
        /// this.progressBar2.Value = 0;
        /// this.Cursor = Cursors.Default;
        /// if (!_workerThread.IsAlive)
        /// _workerThread.Join();
        /// }
        /// </code>
        /// 
        /// </example>
        /// 
        /// <seealso cref="E:DotNetZipAdditionalPlatforms.Zip.ZipFile.ReadProgress" />
        /// <seealso cref="E:DotNetZipAdditionalPlatforms.Zip.ZipFile.AddProgress" />
        /// <seealso cref="E:DotNetZipAdditionalPlatforms.Zip.ZipFile.ExtractProgress" />
        public event EventHandler<SaveProgressEventArgs> SaveProgress;

        /// <summary>
        /// An event that is raised when an error occurs during open or read of files
        /// while saving a zip archive.
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// Errors can occur as a file is being saved to the zip archive.  For
        /// example, the File.Open may fail, or a File.Read may fail, because of
        /// lock conflicts or other reasons.  If you add a handler to this event,
        /// you can handle such errors in your own code.  If you don't add a
        /// handler, the library will throw an exception if it encounters an I/O
        /// error during a call to <c>Save()</c>.
        /// </para>
        /// 
        /// <para>
        /// Setting a handler implicitly sets <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.ZipErrorAction" /> to
        /// <c>ZipErrorAction.InvokeErrorEvent</c>.
        /// </para>
        /// 
        /// <para>
        /// The handler you add applies to all <see cref="T:DotNetZipAdditionalPlatforms.Zip.ZipEntry" /> items that are
        /// subsequently added to the <c>ZipFile</c> instance. If you set this
        /// property after you have added items to the <c>ZipFile</c>, but before you
        /// have called <c>Save()</c>, errors that occur while saving those items
        /// will not cause the error handler to be invoked.
        /// </para>
        /// 
        /// <para>
        /// If you want to handle any errors that occur with any entry in the zip
        /// file using the same error handler, then add your error handler once,
        /// before adding any entries to the zip archive.
        /// </para>
        /// 
        /// <para>
        /// In the error handler method, you need to set the <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipEntry.ZipErrorAction" /> property on the
        /// <c>ZipErrorEventArgs.CurrentEntry</c>.  This communicates back to
        /// DotNetZip what you would like to do with this particular error.  Within
        /// an error handler, if you set the <c>ZipEntry.ZipErrorAction</c> property
        /// on the <c>ZipEntry</c> to <c>ZipErrorAction.InvokeErrorEvent</c> or if
        /// you don't set it at all, the library will throw the exception. (It is the
        /// same as if you had set the <c>ZipEntry.ZipErrorAction</c> property on the
        /// <c>ZipEntry</c> to <c>ZipErrorAction.Throw</c>.) If you set the
        /// <c>ZipErrorEventArgs.Cancel</c> to true, the entire <c>Save()</c> will be
        /// canceled.
        /// </para>
        /// 
        /// <para>
        /// In the case that you use <c>ZipErrorAction.Skip</c>, implying that
        /// you want to skip the entry for which there's been an error, DotNetZip
        /// tries to seek backwards in the output stream, and truncate all bytes
        /// written on behalf of that particular entry. This works only if the
        /// output stream is seekable.  It will not work, for example, when using
        /// ASPNET's Response.OutputStream.
        /// </para>
        /// 
        /// </remarks>
        /// 
        /// <example>
        /// 
        /// This example shows how to use an event handler to handle
        /// errors during save of the zip file.
        /// <code lang="C#">
        /// 
        /// public static void MyZipError(object sender, ZipErrorEventArgs e)
        /// {
        /// Console.WriteLine("Error saving {0}...", e.FileName);
        /// Console.WriteLine("   Exception: {0}", e.exception);
        /// ZipEntry entry = e.CurrentEntry;
        /// string response = null;
        /// // Ask the user whether he wants to skip this error or not
        /// do
        /// {
        /// Console.Write("Retry, Skip, Throw, or Cancel ? (R/S/T/C) ");
        /// response = Console.ReadLine();
        /// Console.WriteLine();
        /// 
        /// } while (response != null &amp;&amp;
        /// response[0]!='S' &amp;&amp; response[0]!='s' &amp;&amp;
        /// response[0]!='R' &amp;&amp; response[0]!='r' &amp;&amp;
        /// response[0]!='T' &amp;&amp; response[0]!='t' &amp;&amp;
        /// response[0]!='C' &amp;&amp; response[0]!='c');
        /// 
        /// e.Cancel = (response[0]=='C' || response[0]=='c');
        /// 
        /// if (response[0]=='S' || response[0]=='s')
        /// entry.ZipErrorAction = ZipErrorAction.Skip;
        /// else if (response[0]=='R' || response[0]=='r')
        /// entry.ZipErrorAction = ZipErrorAction.Retry;
        /// else if (response[0]=='T' || response[0]=='t')
        /// entry.ZipErrorAction = ZipErrorAction.Throw;
        /// }
        /// 
        /// public void SaveTheFile()
        /// {
        /// string directoryToZip = "fodder";
        /// string directoryInArchive = "files";
        /// string zipFileToCreate = "Archive.zip";
        /// using (var zip = new ZipFile())
        /// {
        /// // set the event handler before adding any entries
        /// zip.ZipError += MyZipError;
        /// zip.AddDirectory(directoryToZip, directoryInArchive);
        /// zip.Save(zipFileToCreate);
        /// }
        /// }
        /// </code>
        /// 
        /// <code lang="VB">
        /// Private Sub MyZipError(ByVal sender As Object, ByVal e As DotNetZipAdditionalPlatforms.Zip.ZipErrorEventArgs)
        /// ' At this point, the application could prompt the user for an action to take.
        /// ' But in this case, this application will simply automatically skip the file, in case of error.
        /// Console.WriteLine("Zip Error,  entry {0}", e.CurrentEntry.FileName)
        /// Console.WriteLine("   Exception: {0}", e.exception)
        /// ' set the desired ZipErrorAction on the CurrentEntry to communicate that to DotNetZip
        /// e.CurrentEntry.ZipErrorAction = Zip.ZipErrorAction.Skip
        /// End Sub
        /// 
        /// Public Sub SaveTheFile()
        /// Dim directoryToZip As String = "fodder"
        /// Dim directoryInArchive As String = "files"
        /// Dim zipFileToCreate as String = "Archive.zip"
        /// Using zipArchive As ZipFile = New ZipFile
        /// ' set the event handler before adding any entries
        /// AddHandler zipArchive.ZipError, AddressOf MyZipError
        /// zipArchive.AddDirectory(directoryToZip, directoryInArchive)
        /// zipArchive.Save(zipFileToCreate)
        /// End Using
        /// End Sub
        /// 
        /// </code>
        /// </example>
        /// 
        /// <seealso cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.ZipErrorAction" />
        public event EventHandler<ZipErrorEventArgs> ZipError;

        static ZipFile()
        {
            ExtractorSettings[] settingsArray = new ExtractorSettings[2];
            ExtractorSettings settings = new ExtractorSettings();
            settings.Flavor = SelfExtractorFlavor.WinFormsApplication;
            List<string> list = new List<string>();
            list.Add("System.dll");
            list.Add("System.Windows.Forms.dll");
            list.Add("System.Drawing.dll");
            settings.ReferencedAssemblies = list;
            List<string> list2 = new List<string>();
            list2.Add("DotNetZipAdditionalPlatforms.Zip.WinFormsSelfExtractorStub.resources");
            list2.Add("DotNetZipAdditionalPlatforms.Zip.Forms.PasswordDialog.resources");
            list2.Add("DotNetZipAdditionalPlatforms.Zip.Forms.ZipContentsDialog.resources");
            settings.CopyThroughResources = list2;
            List<string> list3 = new List<string>();
            list3.Add("WinFormsSelfExtractorStub.cs");
            list3.Add("WinFormsSelfExtractorStub.Designer.cs");
            list3.Add("PasswordDialog.cs");
            list3.Add("PasswordDialog.Designer.cs");
            list3.Add("ZipContentsDialog.cs");
            list3.Add("ZipContentsDialog.Designer.cs");
            list3.Add("FolderBrowserDialogEx.cs");
            settings.ResourcesToCompile = list3;
            settingsArray[0] = settings;
            ExtractorSettings settings2 = new ExtractorSettings();
            settings2.Flavor = SelfExtractorFlavor.ConsoleApplication;
            List<string> list4 = new List<string>();
            list4.Add("System.dll");
            settings2.ReferencedAssemblies = list4;
            settings2.CopyThroughResources = null;
            List<string> list5 = new List<string>();
            list5.Add("CommandLineSelfExtractorStub.cs");
            settings2.ResourcesToCompile = list5;
            settingsArray[1] = settings2;
            SettingsList = settingsArray;
        }

        /// <summary>
        /// Create a zip file, without specifying a target filename or stream to save to.
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// See the documentation on the <see cref="M:DotNetZipAdditionalPlatforms.Zip.ZipFile.#ctor(System.String)">ZipFile
        /// constructor that accepts a single string argument</see> for basic
        /// information on all the <c>ZipFile</c> constructors.
        /// </para>
        /// 
        /// <para>
        /// After instantiating with this constructor and adding entries to the
        /// archive, the application should call <see cref="M:DotNetZipAdditionalPlatforms.Zip.ZipFile.Save(System.String)" /> or
        /// <see cref="M:DotNetZipAdditionalPlatforms.Zip.ZipFile.Save(System.IO.Stream)" /> to save to a file or a
        /// stream, respectively.  The application can also set the <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.Name" />
        /// property and then call the no-argument <see cref="M:DotNetZipAdditionalPlatforms.Zip.ZipFile.Save" /> method.  (This
        /// is the preferred approach for applications that use the library through
        /// COM interop.)  If you call the no-argument <see cref="M:DotNetZipAdditionalPlatforms.Zip.ZipFile.Save" /> method
        /// without having set the <c>Name</c> of the <c>ZipFile</c>, either through
        /// the parameterized constructor or through the explicit property , the
        /// Save() will throw, because there is no place to save the file.  </para>
        /// 
        /// <para>
        /// Instances of the <c>ZipFile</c> class are not multi-thread safe.  You may
        /// have multiple threads that each use a distinct <c>ZipFile</c> instance, or
        /// you can synchronize multi-thread access to a single instance.  </para>
        /// 
        /// </remarks>
        /// 
        /// <example>
        /// This example creates a Zip archive called Backup.zip, containing all the files
        /// in the directory DirectoryToZip. Files within subdirectories are not zipped up.
        /// <code>
        /// using (ZipFile zip = new ZipFile())
        /// {
        /// // Store all files found in the top level directory, into the zip archive.
        /// // note: this code does not recurse subdirectories!
        /// String[] filenames = System.IO.Directory.GetFiles(DirectoryToZip);
        /// zip.AddFiles(filenames, "files");
        /// zip.Save("Backup.zip");
        /// }
        /// </code>
        /// 
        /// <code lang="VB">
        /// Using zip As New ZipFile
        /// ' Store all files found in the top level directory, into the zip archive.
        /// ' note: this code does not recurse subdirectories!
        /// Dim filenames As String() = System.IO.Directory.GetFiles(DirectoryToZip)
        /// zip.AddFiles(filenames, "files")
        /// zip.Save("Backup.zip")
        /// End Using
        /// </code>
        /// </example>
        public ZipFile()
        {
            this._emitNtfsTimes = true;
            this._Strategy = CompressionStrategy.Default;
            this._compressionMethod = DotNetZipAdditionalPlatforms.Zip.CompressionMethod.Deflate;
            this._ReadStreamIsOurs = true;
            this.LOCK = new object();
            this._locEndOfCDS = -1L;
            this._alternateEncoding = Encoding.GetEncoding("IBM437");
            this._alternateEncodingUsage = ZipOption.Default;
            this._BufferSize = BufferSizeDefault;
            this._maxBufferPairs = 0x10;
            this._zip64 = Zip64Option.Default;
            this._lengthOfReadStream = -99L;
            this._InitInstance(null, null);
        }

        /// <summary>
        /// Creates a new <c>ZipFile</c> instance, using the specified filename.
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// Applications can use this constructor to create a new ZipFile for writing,
        /// or to slurp in an existing zip archive for read and update purposes.
        /// </para>
        /// 
        /// <para>
        /// To create a new zip archive, an application can call this constructor,
        /// passing the name of a file that does not exist.  The name may be a fully
        /// qualified path. Then the application can add directories or files to the
        /// <c>ZipFile</c> via <c>AddDirectory()</c>, <c>AddFile()</c>, <c>AddItem()</c>
        /// and then write the zip archive to the disk by calling <c>Save()</c>. The
        /// zip file is not actually opened and written to the disk until the
        /// application calls <c>ZipFile.Save()</c>.  At that point the new zip file
        /// with the given name is created.
        /// </para>
        /// 
        /// <para>
        /// If you won't know the name of the <c>Zipfile</c> until the time you call
        /// <c>ZipFile.Save()</c>, or if you plan to save to a stream (which has no
        /// name), then you should use the no-argument constructor.
        /// </para>
        /// 
        /// <para>
        /// The application can also call this constructor to read an existing zip
        /// archive.  passing the name of a valid zip file that does exist. But, it's
        /// better form to use the static <see cref="M:DotNetZipAdditionalPlatforms.Zip.ZipFile.Read(System.String)" /> method,
        /// passing the name of the zip file, because using <c>ZipFile.Read()</c> in
        /// your code communicates very clearly what you are doing.  In either case,
        /// the file is then read into the <c>ZipFile</c> instance.  The app can then
        /// enumerate the entries or can modify the zip file, for example adding
        /// entries, removing entries, changing comments, and so on.
        /// </para>
        /// 
        /// <para>
        /// One advantage to this parameterized constructor: it allows applications to
        /// use the same code to add items to a zip archive, regardless of whether the
        /// zip file exists.
        /// </para>
        /// 
        /// <para>
        /// Instances of the <c>ZipFile</c> class are not multi-thread safe.  You may
        /// not party on a single instance with multiple threads.  You may have
        /// multiple threads that each use a distinct <c>ZipFile</c> instance, or you
        /// can synchronize multi-thread access to a single instance.
        /// </para>
        /// 
        /// <para>
        /// By the way, since DotNetZip is so easy to use, don't you think <see href="http://cheeso.members.winisp.net/DotNetZipDonate.aspx">you should
        /// donate $5 or $10</see>?
        /// </para>
        /// 
        /// </remarks>
        /// 
        /// <exception cref="T:DotNetZipAdditionalPlatforms.Zip.ZipException">
        /// Thrown if name refers to an existing file that is not a valid zip file.
        /// </exception>
        /// 
        /// <example>
        /// This example shows how to create a zipfile, and add a few files into it.
        /// <code>
        /// String ZipFileToCreate = "archive1.zip";
        /// String DirectoryToZip  = "c:\\reports";
        /// using (ZipFile zip = new ZipFile())
        /// {
        /// // Store all files found in the top level directory, into the zip archive.
        /// String[] filenames = System.IO.Directory.GetFiles(DirectoryToZip);
        /// zip.AddFiles(filenames, "files");
        /// zip.Save(ZipFileToCreate);
        /// }
        /// </code>
        /// 
        /// <code lang="VB">
        /// Dim ZipFileToCreate As String = "archive1.zip"
        /// Dim DirectoryToZip As String = "c:\reports"
        /// Using zip As ZipFile = New ZipFile()
        /// Dim filenames As String() = System.IO.Directory.GetFiles(DirectoryToZip)
        /// zip.AddFiles(filenames, "files")
        /// zip.Save(ZipFileToCreate)
        /// End Using
        /// </code>
        /// </example>
        /// 
        /// <param name="fileName">The filename to use for the new zip archive.</param>
        public ZipFile(string fileName)
        {
            this._emitNtfsTimes = true;
            this._Strategy = CompressionStrategy.Default;
            this._compressionMethod = DotNetZipAdditionalPlatforms.Zip.CompressionMethod.Deflate;
            this._ReadStreamIsOurs = true;
            this.LOCK = new object();
            this._locEndOfCDS = -1L;
            this._alternateEncoding = Encoding.GetEncoding("IBM437");
            this._alternateEncodingUsage = ZipOption.Default;
            this._BufferSize = BufferSizeDefault;
            this._maxBufferPairs = 0x10;
            this._zip64 = Zip64Option.Default;
            this._lengthOfReadStream = -99L;
            try
            {
                this._InitInstance(fileName, null);
            }
            catch (Exception exception)
            {
                throw new ZipException(string.Format(CultureInfo.InvariantCulture, "Could not read {0} as a zip file", fileName), exception);
            }
        }

        /// <summary>
        /// Create a zip file, specifying a text Encoding, but without specifying a
        /// target filename or stream to save to.
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// See the documentation on the <see cref="M:DotNetZipAdditionalPlatforms.Zip.ZipFile.#ctor(System.String)">ZipFile
        /// constructor that accepts a single string argument</see> for basic
        /// information on all the <c>ZipFile</c> constructors.
        /// </para>
        /// 
        /// </remarks>
        /// 
        /// <param name="encoding">
        /// The Encoding is used as the default alternate encoding for entries with
        /// filenames or comments that cannot be encoded with the IBM437 code page.
        /// </param>
        public ZipFile(Encoding encoding)
        {
            this._emitNtfsTimes = true;
            this._Strategy = CompressionStrategy.Default;
            this._compressionMethod = DotNetZipAdditionalPlatforms.Zip.CompressionMethod.Deflate;
            this._ReadStreamIsOurs = true;
            this.LOCK = new object();
            this._locEndOfCDS = -1L;
            this._alternateEncoding = Encoding.GetEncoding("IBM437");
            this._alternateEncodingUsage = ZipOption.Default;
            this._BufferSize = BufferSizeDefault;
            this._maxBufferPairs = 0x10;
            this._zip64 = Zip64Option.Default;
            this._lengthOfReadStream = -99L;
            this.AlternateEncoding = encoding;
            this.AlternateEncodingUsage = ZipOption.Always;
            this._InitInstance(null, null);
        }

        /// <summary>
        /// Creates a new <c>ZipFile</c> instance, using the specified name for the
        /// filename, and the specified status message writer.
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// See the documentation on the <see cref="M:DotNetZipAdditionalPlatforms.Zip.ZipFile.#ctor(System.String)">ZipFile
        /// constructor that accepts a single string argument</see> for basic
        /// information on all the <c>ZipFile</c> constructors.
        /// </para>
        /// 
        /// <para>
        /// This version of the constructor allows the caller to pass in a TextWriter,
        /// to which verbose messages will be written during extraction or creation of
        /// the zip archive.  A console application may wish to pass
        /// System.Console.Out to get messages on the Console. A graphical or headless
        /// application may wish to capture the messages in a different
        /// <c>TextWriter</c>, for example, a <c>StringWriter</c>, and then display
        /// the messages in a TextBox, or generate an audit log of ZipFile operations.
        /// </para>
        /// 
        /// <para>
        /// To encrypt the data for the files added to the <c>ZipFile</c> instance,
        /// set the Password property after creating the <c>ZipFile</c> instance.
        /// </para>
        /// 
        /// <para>
        /// Instances of the <c>ZipFile</c> class are not multi-thread safe.  You may
        /// not party on a single instance with multiple threads.  You may have
        /// multiple threads that each use a distinct <c>ZipFile</c> instance, or you
        /// can synchronize multi-thread access to a single instance.
        /// </para>
        /// 
        /// </remarks>
        /// 
        /// <exception cref="T:DotNetZipAdditionalPlatforms.Zip.ZipException">
        /// Thrown if name refers to an existing file that is not a valid zip file.
        /// </exception>
        /// 
        /// <example>
        /// <code>
        /// using (ZipFile zip = new ZipFile("Backup.zip", Console.Out))
        /// {
        /// // Store all files found in the top level directory, into the zip archive.
        /// // note: this code does not recurse subdirectories!
        /// // Status messages will be written to Console.Out
        /// String[] filenames = System.IO.Directory.GetFiles(DirectoryToZip);
        /// zip.AddFiles(filenames);
        /// zip.Save();
        /// }
        /// </code>
        /// 
        /// <code lang="VB">
        /// Using zip As New ZipFile("Backup.zip", Console.Out)
        /// ' Store all files found in the top level directory, into the zip archive.
        /// ' note: this code does not recurse subdirectories!
        /// ' Status messages will be written to Console.Out
        /// Dim filenames As String() = System.IO.Directory.GetFiles(DirectoryToZip)
        /// zip.AddFiles(filenames)
        /// zip.Save()
        /// End Using
        /// </code>
        /// </example>
        /// 
        /// <param name="fileName">The filename to use for the new zip archive.</param>
        /// <param name="statusMessageWriter">A TextWriter to use for writing
        /// verbose status messages.</param>
        public ZipFile(string fileName, TextWriter statusMessageWriter)
        {
            this._emitNtfsTimes = true;
            this._Strategy = CompressionStrategy.Default;
            this._compressionMethod = DotNetZipAdditionalPlatforms.Zip.CompressionMethod.Deflate;
            this._ReadStreamIsOurs = true;
            this.LOCK = new object();
            this._locEndOfCDS = -1L;
            this._alternateEncoding = Encoding.GetEncoding("IBM437");
            this._alternateEncodingUsage = ZipOption.Default;
            this._BufferSize = BufferSizeDefault;
            this._maxBufferPairs = 0x10;
            this._zip64 = Zip64Option.Default;
            this._lengthOfReadStream = -99L;
            try
            {
                this._InitInstance(fileName, statusMessageWriter);
            }
            catch (Exception exception)
            {
                throw new ZipException(string.Format(CultureInfo.InvariantCulture, "{0} is not a valid zip file", fileName), exception);
            }
        }

        /// <summary>
        /// Creates a new <c>ZipFile</c> instance, using the specified name for the
        /// filename, and the specified Encoding.
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// See the documentation on the <see cref="M:DotNetZipAdditionalPlatforms.Zip.ZipFile.#ctor(System.String)">ZipFile
        /// constructor that accepts a single string argument</see> for basic
        /// information on all the <c>ZipFile</c> constructors.
        /// </para>
        /// 
        /// <para>
        /// The Encoding is used as the default alternate encoding for entries with
        /// filenames or comments that cannot be encoded with the IBM437 code page.
        /// This is equivalent to setting the <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.ProvisionalAlternateEncoding" /> property on the <c>ZipFile</c>
        /// instance after construction.
        /// </para>
        /// 
        /// <para>
        /// Instances of the <c>ZipFile</c> class are not multi-thread safe.  You may
        /// not party on a single instance with multiple threads.  You may have
        /// multiple threads that each use a distinct <c>ZipFile</c> instance, or you
        /// can synchronize multi-thread access to a single instance.
        /// </para>
        /// 
        /// </remarks>
        /// 
        /// <exception cref="T:DotNetZipAdditionalPlatforms.Zip.ZipException">
        /// Thrown if name refers to an existing file that is not a valid zip file.
        /// </exception>
        /// 
        /// <param name="fileName">The filename to use for the new zip archive.</param>
        /// <param name="encoding">The Encoding is used as the default alternate
        /// encoding for entries with filenames or comments that cannot be encoded
        /// with the IBM437 code page. </param>
        public ZipFile(string fileName, Encoding encoding)
        {
            this._emitNtfsTimes = true;
            this._Strategy = CompressionStrategy.Default;
            this._compressionMethod = DotNetZipAdditionalPlatforms.Zip.CompressionMethod.Deflate;
            this._ReadStreamIsOurs = true;
            this.LOCK = new object();
            this._locEndOfCDS = -1L;
            this._alternateEncoding = Encoding.GetEncoding("IBM437");
            this._alternateEncodingUsage = ZipOption.Default;
            this._BufferSize = BufferSizeDefault;
            this._maxBufferPairs = 0x10;
            this._zip64 = Zip64Option.Default;
            this._lengthOfReadStream = -99L;
            try
            {
                this.AlternateEncoding = encoding;
                this.AlternateEncodingUsage = ZipOption.Always;
                this._InitInstance(fileName, null);
            }
            catch (Exception exception)
            {
                throw new ZipException(string.Format(CultureInfo.InvariantCulture, "{0} is not a valid zip file", fileName), exception);
            }
        }

        /// <summary>
        /// Creates a new <c>ZipFile</c> instance, using the specified name for the
        /// filename, the specified status message writer, and the specified Encoding.
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// This constructor works like the <see cref="M:DotNetZipAdditionalPlatforms.Zip.ZipFile.#ctor(System.String)">ZipFile
        /// constructor that accepts a single string argument.</see> See that
        /// reference for detail on what this constructor does.
        /// </para>
        /// 
        /// <para>
        /// This version of the constructor allows the caller to pass in a
        /// <c>TextWriter</c>, and an Encoding.  The <c>TextWriter</c> will collect
        /// verbose messages that are generated by the library during extraction or
        /// creation of the zip archive.  A console application may wish to pass
        /// <c>System.Console.Out</c> to get messages on the Console. A graphical or
        /// headless application may wish to capture the messages in a different
        /// <c>TextWriter</c>, for example, a <c>StringWriter</c>, and then display
        /// the messages in a <c>TextBox</c>, or generate an audit log of
        /// <c>ZipFile</c> operations.
        /// </para>
        /// 
        /// <para>
        /// The <c>Encoding</c> is used as the default alternate encoding for entries
        /// with filenames or comments that cannot be encoded with the IBM437 code
        /// page.  This is a equivalent to setting the <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.ProvisionalAlternateEncoding" /> property on the <c>ZipFile</c>
        /// instance after construction.
        /// </para>
        /// 
        /// <para>
        /// To encrypt the data for the files added to the <c>ZipFile</c> instance,
        /// set the <c>Password</c> property after creating the <c>ZipFile</c>
        /// instance.
        /// </para>
        /// 
        /// <para>
        /// Instances of the <c>ZipFile</c> class are not multi-thread safe.  You may
        /// not party on a single instance with multiple threads.  You may have
        /// multiple threads that each use a distinct <c>ZipFile</c> instance, or you
        /// can synchronize multi-thread access to a single instance.
        /// </para>
        /// 
        /// </remarks>
        /// 
        /// <exception cref="T:DotNetZipAdditionalPlatforms.Zip.ZipException">
        /// Thrown if <c>fileName</c> refers to an existing file that is not a valid zip file.
        /// </exception>
        /// 
        /// <param name="fileName">The filename to use for the new zip archive.</param>
        /// <param name="statusMessageWriter">A TextWriter to use for writing verbose
        /// status messages.</param>
        /// <param name="encoding">
        /// The Encoding is used as the default alternate encoding for entries with
        /// filenames or comments that cannot be encoded with the IBM437 code page.
        /// </param>
        public ZipFile(string fileName, TextWriter statusMessageWriter, Encoding encoding)
        {
            this._emitNtfsTimes = true;
            this._Strategy = CompressionStrategy.Default;
            this._compressionMethod = DotNetZipAdditionalPlatforms.Zip.CompressionMethod.Deflate;
            this._ReadStreamIsOurs = true;
            this.LOCK = new object();
            this._locEndOfCDS = -1L;
            this._alternateEncoding = Encoding.GetEncoding("IBM437");
            this._alternateEncodingUsage = ZipOption.Default;
            this._BufferSize = BufferSizeDefault;
            this._maxBufferPairs = 0x10;
            this._zip64 = Zip64Option.Default;
            this._lengthOfReadStream = -99L;
            try
            {
                this.AlternateEncoding = encoding;
                this.AlternateEncodingUsage = ZipOption.Always;
                this._InitInstance(fileName, statusMessageWriter);
            }
            catch (Exception exception)
            {
                throw new ZipException(string.Format(CultureInfo.InvariantCulture, "{0} is not a valid zip file", fileName), exception);
            }
        }

        private void _AddOrUpdateSelectedFiles(string selectionCriteria, string directoryOnDisk, string directoryPathInArchive, bool recurseDirectories, bool wantUpdate)
        {
            if ((directoryOnDisk == null) && Directory.Exists(selectionCriteria))
            {
                directoryOnDisk = selectionCriteria;
                selectionCriteria = "*.*";
            }
            else if (string.IsNullOrEmpty(directoryOnDisk))
            {
                directoryOnDisk = ".";
            }
            while (directoryOnDisk.EndsWith(@"\"))
            {
                directoryOnDisk = directoryOnDisk.Substring(0, directoryOnDisk.Length - 1);
            }
            if (this.Verbose)
            {
                this.StatusMessageTextWriter.WriteLine("adding selection '{0}' from dir '{1}'...", selectionCriteria, directoryOnDisk);
            }
            ReadOnlyCollection<string> onlys = new FileSelector(selectionCriteria, this.AddDirectoryWillTraverseReparsePoints).SelectFiles(directoryOnDisk, recurseDirectories);
            if (this.Verbose)
            {
                this.StatusMessageTextWriter.WriteLine("found {0} files...", onlys.Count);
            }
            this.OnAddStarted();
            AddOrUpdateAction action = wantUpdate ? AddOrUpdateAction.AddOrUpdate : AddOrUpdateAction.AddOnly;
            foreach (string str in onlys)
            {
                string str2 = (directoryPathInArchive == null) ? null : ReplaceLeadingDirectory(Path.GetDirectoryName(str), directoryOnDisk, directoryPathInArchive);
                if (File.Exists(str))
                {
                    if (wantUpdate)
                    {
                        this.UpdateFile(str, str2);
                    }
                    else
                    {
                        this.AddFile(str, str2);
                    }
                }
                else
                {
                    this.AddOrUpdateDirectoryImpl(str, str2, action, false, 0);
                }
            }
            this.OnAddCompleted();
        }

        private void _initEntriesDictionary()
        {
            StringComparer comparer = this.CaseSensitiveRetrieval ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase;
            this._entries = (this._entries == null) ? new Dictionary<string, ZipEntry>(comparer) : new Dictionary<string, ZipEntry>(this._entries, comparer);
        }

        private void _InitInstance(string zipFileName, TextWriter statusMessageWriter)
        {
            this._name = zipFileName;
            this._StatusMessageTextWriter = statusMessageWriter;
            this._contentsChanged = true;
            this.AddDirectoryWillTraverseReparsePoints = true;
            this.CompressionLevel = DotNetZipAdditionalPlatforms.Zlib.CompressionLevel.Default;
            this.ParallelDeflateThreshold = 0x80000L;
            this._initEntriesDictionary();
            if (File.Exists(this._name))
            {
                if (this.FullScan)
                {
                    ReadIntoInstance_Orig(this);
                }
                else
                {
                    ReadIntoInstance(this);
                }
                this._fileAlreadyExists = true;
            }
        }

        private ZipEntry _InternalAddEntry(ZipEntry ze)
        {
            ze._container = new ZipContainer(this);
            ze.CompressionMethod = this.CompressionMethod;
            ze.CompressionLevel = this.CompressionLevel;
            ze.ExtractExistingFile = this.ExtractExistingFile;
            ze.ZipErrorAction = this.ZipErrorAction;
            ze.SetCompression = this.SetCompression;
            ze.AlternateEncoding = this.AlternateEncoding;
            ze.AlternateEncodingUsage = this.AlternateEncodingUsage;
            ze.Password = this._Password;
            ze.Encryption = this.Encryption;
            ze.EmitTimesInWindowsFormatWhenSaving = this._emitNtfsTimes;
            ze.EmitTimesInUnixFormatWhenSaving = this._emitUnixTimes;
            this.InternalAddEntry(ze.FileName, ze);
            this.AfterAddEntry(ze);
            return ze;
        }

        private void _InternalExtractAll(string path, bool overrideExtractExistingProperty)
        {
            bool verbose = this.Verbose;
            this._inExtractAll = true;
            try
            {
                this.OnExtractAllStarted(path);
                int current = 0;
                foreach (ZipEntry entry in this._entries.Values)
                {
                    if (verbose)
                    {
                        this.StatusMessageTextWriter.WriteLine("\n{1,-22} {2,-8} {3,4}   {4,-8}  {0}", new object[] { "Name", "Modified", "Size", "Ratio", "Packed" });
                        this.StatusMessageTextWriter.WriteLine(new string('-', 0x48));
                        verbose = false;
                    }
                    if (this.Verbose)
                    {
                        this.StatusMessageTextWriter.WriteLine("{1,-22} {2,-8} {3,4:F0}%   {4,-8} {0}", new object[] { entry.FileName, entry.LastModified.ToString("yyyy-MM-dd HH:mm:ss"), entry.UncompressedSize, entry.CompressionRatio, entry.CompressedSize });
                        if (!string.IsNullOrEmpty(entry.Comment))
                        {
                            this.StatusMessageTextWriter.WriteLine("  Comment: {0}", entry.Comment);
                        }
                    }
                    entry.Password = this._Password;
                    this.OnExtractEntry(current, true, entry, path);
                    if (overrideExtractExistingProperty)
                    {
                        entry.ExtractExistingFile = this.ExtractExistingFile;
                    }
                    entry.Extract(path);
                    current++;
                    this.OnExtractEntry(current, false, entry, path);
                    if (this._extractOperationCanceled)
                    {
                        break;
                    }
                }
                if (!this._extractOperationCanceled)
                {
                    foreach (ZipEntry entry in this._entries.Values)
                    {
                        if (entry.IsDirectory || entry.FileName.EndsWith("/"))
                        {
                            string fileOrDirectory = entry.FileName.StartsWith("/") ? Path.Combine(path, entry.FileName.Substring(1)) : Path.Combine(path, entry.FileName);
                            entry._SetTimes(fileOrDirectory, false);
                        }
                    }
                    this.OnExtractAllCompleted(path);
                }
            }
            finally
            {
                this._inExtractAll = false;
            }
        }

        private void _SaveSfxStub(string exeToGenerate, SelfExtractorSaveOptions options)
        {
            string str = null;
            string path = null;
            string str3 = null;
            string dir = null;
            try
            {
                if (File.Exists(exeToGenerate) && this.Verbose)
                {
                    this.StatusMessageTextWriter.WriteLine("The existing file ({0}) will be overwritten.", exeToGenerate);
                }
                if (!exeToGenerate.EndsWith(".exe") && this.Verbose)
                {
                    this.StatusMessageTextWriter.WriteLine("Warning: The generated self-extracting file will not have an .exe extension.");
                }
                dir = this.TempFileFolder ?? Path.GetDirectoryName(exeToGenerate);
                path = GenerateTempPathname(dir, "exe");
                Assembly assembly = typeof(ZipFile).Assembly;
                using (CSharpCodeProvider provider = new CSharpCodeProvider())
                {
                    ExtractorSettings settings = null;
                    foreach (ExtractorSettings settings2 in SettingsList)
                    {
                        if (settings2.Flavor == options.Flavor)
                        {
                            settings = settings2;
                            break;
                        }
                    }
                    if (settings == null)
                    {
                        throw new BadStateException(string.Format(CultureInfo.InvariantCulture, "While saving a Self-Extracting Zip, Cannot find that flavor ({0})?", options.Flavor));
                    }
                    CompilerParameters parameters = new CompilerParameters();
                    parameters.ReferencedAssemblies.Add(assembly.Location);
                    if (settings.ReferencedAssemblies != null)
                    {
                        foreach (string str5 in settings.ReferencedAssemblies)
                        {
                            parameters.ReferencedAssemblies.Add(str5);
                        }
                    }
                    parameters.GenerateInMemory = false;
                    parameters.GenerateExecutable = true;
                    parameters.IncludeDebugInformation = false;
                    parameters.CompilerOptions = "";
                    Assembly executingAssembly = Assembly.GetExecutingAssembly();
                    StringBuilder builder = new StringBuilder();
                    string str6 = GenerateTempPathname(dir, "cs");
                    using (ZipFile file = Read(executingAssembly.GetManifestResourceStream("DotNetZipAdditionalPlatforms.Zip.Resources.ZippedResources.zip")))
                    {
                        str3 = GenerateTempPathname(dir, "tmp");
                        if (string.IsNullOrEmpty(options.IconFile))
                        {
                            Directory.CreateDirectory(str3);
                            ZipEntry entry = file["zippedFile.ico"];
                            if ((entry.Attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
                            {
                                entry.Attributes ^= FileAttributes.ReadOnly;
                            }
                            entry.Extract(str3);
                            str = Path.Combine(str3, "zippedFile.ico");
                            parameters.CompilerOptions = parameters.CompilerOptions + string.Format(CultureInfo.InvariantCulture, "/win32icon:\"{0}\"", str);
                        }
                        else
                        {
                            parameters.CompilerOptions = parameters.CompilerOptions + string.Format(CultureInfo.InvariantCulture, "/win32icon:\"{0}\"", options.IconFile);
                        }
                        parameters.OutputAssembly = path;
                        if (options.Flavor == SelfExtractorFlavor.WinFormsApplication)
                        {
                            parameters.CompilerOptions = parameters.CompilerOptions + " /target:winexe";
                        }
                        if (!string.IsNullOrEmpty(options.AdditionalCompilerSwitches))
                        {
                            parameters.CompilerOptions = parameters.CompilerOptions + " " + options.AdditionalCompilerSwitches;
                        }
                        if (string.IsNullOrEmpty(parameters.CompilerOptions))
                        {
                            parameters.CompilerOptions = null;
                        }
                        if ((settings.CopyThroughResources != null) && (settings.CopyThroughResources.Count != 0))
                        {
                            if (!Directory.Exists(str3))
                            {
                                Directory.CreateDirectory(str3);
                            }
                            foreach (string str7 in settings.CopyThroughResources)
                            {
                                string filename = Path.Combine(str3, str7);
                                ExtractResourceToFile(executingAssembly, str7, filename);
                                parameters.EmbeddedResources.Add(filename);
                            }
                        }
                        parameters.EmbeddedResources.Add(assembly.Location);
                        builder.Append("// " + Path.GetFileName(str6) + "\n").Append("// --------------------------------------------\n//\n").Append("// This SFX source file was generated by DotNetZip ").Append(LibraryVersion.ToString()).Append("\n//         at ").Append(DateTime.Now.ToString("yyyy MMMM dd  HH:mm:ss")).Append("\n//\n// --------------------------------------------\n\n\n");
                        if (!string.IsNullOrEmpty(options.Description))
                        {
                            builder.Append("[assembly: System.Reflection.AssemblyTitle(\"" + options.Description.Replace("\"", "") + "\")]\n");
                        }
                        else
                        {
                            builder.Append("[assembly: System.Reflection.AssemblyTitle(\"DotNetZip SFX Archive\")]\n");
                        }
                        if (!string.IsNullOrEmpty(options.ProductVersion))
                        {
                            builder.Append("[assembly: System.Reflection.AssemblyInformationalVersion(\"" + options.ProductVersion.Replace("\"", "") + "\")]\n");
                        }
                        string str9 = string.IsNullOrEmpty(options.Copyright) ? "Extractor: Copyright \x00a9 Dino Chiesa 2008-2011" : options.Copyright.Replace("\"", "");
                        if (!string.IsNullOrEmpty(options.ProductName))
                        {
                            builder.Append("[assembly: System.Reflection.AssemblyProduct(\"").Append(options.ProductName.Replace("\"", "")).Append("\")]\n");
                        }
                        else
                        {
                            builder.Append("[assembly: System.Reflection.AssemblyProduct(\"DotNetZip\")]\n");
                        }
                        builder.Append("[assembly: System.Reflection.AssemblyCopyright(\"" + str9 + "\")]\n").Append(string.Format(CultureInfo.InvariantCulture, "[assembly: System.Reflection.AssemblyVersion(\"{0}\")]\n", LibraryVersion.ToString()));
                        if (options.FileVersion != null)
                        {
                            builder.Append(string.Format(CultureInfo.InvariantCulture, "[assembly: System.Reflection.AssemblyFileVersion(\"{0}\")]\n", options.FileVersion.ToString()));
                        }
                        builder.Append("\n\n\n");
                        string defaultExtractDirectory = options.DefaultExtractDirectory;
                        if (defaultExtractDirectory != null)
                        {
                            defaultExtractDirectory = defaultExtractDirectory.Replace("\"", "").Replace(@"\", @"\\");
                        }
                        string postExtractCommandLine = options.PostExtractCommandLine;
                        if (postExtractCommandLine != null)
                        {
                            postExtractCommandLine = postExtractCommandLine.Replace(@"\", @"\\").Replace("\"", "\\\"");
                        }
                        foreach (string str12 in settings.ResourcesToCompile)
                        {
                            using (Stream stream = file[str12].OpenReader())
                            {
                                if (stream == null)
                                {
                                    throw new ZipException(string.Format(CultureInfo.InvariantCulture, "missing resource '{0}'", str12));
                                }
                                using (StreamReader reader = new StreamReader(stream))
                                {
                                    while (reader.Peek() >= 0)
                                    {
                                        string str13 = reader.ReadLine();
                                        if (defaultExtractDirectory != null)
                                        {
                                            str13 = str13.Replace("@@EXTRACTLOCATION", defaultExtractDirectory);
                                        }
                                        str13 = str13.Replace("@@REMOVE_AFTER_EXECUTE", options.RemoveUnpackedFilesAfterExecute.ToString()).Replace("@@QUIET", options.Quiet.ToString());
                                        if (!string.IsNullOrEmpty(options.SfxExeWindowTitle))
                                        {
                                            str13 = str13.Replace("@@SFX_EXE_WINDOW_TITLE", options.SfxExeWindowTitle);
                                        }
                                        str13 = str13.Replace("@@EXTRACT_EXISTING_FILE", ((int) options.ExtractExistingFile).ToString());
                                        if (postExtractCommandLine != null)
                                        {
                                            str13 = str13.Replace("@@POST_UNPACK_CMD_LINE", postExtractCommandLine);
                                        }
                                        builder.Append(str13).Append("\n");
                                    }
                                }
                                builder.Append("\n\n");
                            }
                        }
                    }
                    string str14 = builder.ToString();
                    CompilerResults results = provider.CompileAssemblyFromSource(parameters, new string[] { str14 });
                    if (results == null)
                    {
                        throw new SfxGenerationException("Cannot compile the extraction logic!");
                    }
                    if (this.Verbose)
                    {
                        foreach (string str15 in results.Output)
                        {
                            this.StatusMessageTextWriter.WriteLine(str15);
                        }
                    }
                    if (results.Errors.Count != 0)
                    {
                        using (TextWriter writer = new StreamWriter(str6))
                        {
                            writer.Write(str14);
                            writer.Write("\n\n\n// ------------------------------------------------------------------\n");
                            writer.Write("// Errors during compilation: \n//\n");
                            string fileName = Path.GetFileName(str6);
                            foreach (CompilerError error in results.Errors)
                            {
                                writer.Write(string.Format(CultureInfo.InvariantCulture, "//   {0}({1},{2}): {3} {4}: {5}\n//\n", new object[] { fileName, error.Line, error.Column, error.IsWarning ? "Warning" : "error", error.ErrorNumber, error.ErrorText }));
                            }
                        }
                        throw new SfxGenerationException(string.Format(CultureInfo.InvariantCulture, "Errors compiling the extraction logic!  {0}", str6));
                    }
                    this.OnSaveEvent(ZipProgressEventType.Saving_AfterCompileSelfExtractor);
                    using (Stream stream2 = File.OpenRead(path))
                    {
                        byte[] buffer = new byte[0xfa0];
                        int count = 1;
                        while (count != 0)
                        {
                            count = stream2.Read(buffer, 0, buffer.Length);
                            if (count != 0)
                            {
                                this.WriteStream.Write(buffer, 0, count);
                            }
                        }
                    }
                }
                this.OnSaveEvent(ZipProgressEventType.Saving_AfterSaveTempArchive);
            }
            finally
            {
                try
                {
                    IOException exception;
                    if (Directory.Exists(str3))
                    {
                        try
                        {
                            Directory.Delete(str3, true);
                        }
                        catch (IOException exception1)
                        {
                            exception = exception1;
                            this.StatusMessageTextWriter.WriteLine("Warning: Exception: {0}", exception);
                        }
                    }
                    if (File.Exists(path))
                    {
                        try
                        {
                            File.Delete(path);
                        }
                        catch (IOException exception2)
                        {
                            exception = exception2;
                            this.StatusMessageTextWriter.WriteLine("Warning: Exception: {0}", exception);
                        }
                    }
                }
                catch (IOException)
                {
                }
            }
        }

        /// <summary>
        /// Adds the contents of a filesystem directory to a Zip file archive.
        /// </summary>
        /// 
        /// <remarks>
        /// 
        /// <para>
        /// The name of the directory may be a relative path or a fully-qualified
        /// path. Any files within the named directory are added to the archive.  Any
        /// subdirectories within the named directory are also added to the archive,
        /// recursively.
        /// </para>
        /// 
        /// <para>
        /// Top-level entries in the named directory will appear as top-level entries
        /// in the zip archive.  Entries in subdirectories in the named directory will
        /// result in entries in subdirectories in the zip archive.
        /// </para>
        /// 
        /// <para>
        /// If you want the entries to appear in a containing directory in the zip
        /// archive itself, then you should call the AddDirectory() overload that
        /// allows you to explicitly specify a directory path for use in the archive.
        /// </para>
        /// 
        /// <para>
        /// For <c>ZipFile</c> properties including <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.Encryption" />, <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.Password" />, <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.SetCompression" />, <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.ProvisionalAlternateEncoding" />, <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.ExtractExistingFile" />,
        /// <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.ZipErrorAction" />, and <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.CompressionLevel" />, their
        /// respective values at the time of this call will be applied to each
        /// ZipEntry added.
        /// </para>
        /// 
        /// </remarks>
        /// 
        /// <seealso cref="M:DotNetZipAdditionalPlatforms.Zip.ZipFile.AddItem(System.String)" />
        /// <seealso cref="M:DotNetZipAdditionalPlatforms.Zip.ZipFile.AddFile(System.String)" />
        /// <seealso cref="M:DotNetZipAdditionalPlatforms.Zip.ZipFile.UpdateDirectory(System.String)" />
        /// <seealso cref="M:DotNetZipAdditionalPlatforms.Zip.ZipFile.AddDirectory(System.String,System.String)" />
        /// 
        /// <overloads>This method has 2 overloads.</overloads>
        /// 
        /// <param name="directoryName">The name of the directory to add.</param>
        /// <returns>The <c>ZipEntry</c> added.</returns>
        public ZipEntry AddDirectory(string directoryName)
        {
            return this.AddDirectory(directoryName, null);
        }

        /// <summary>
        /// Adds the contents of a filesystem directory to a Zip file archive,
        /// overriding the path to be used for entries in the archive.
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// The name of the directory may be a relative path or a fully-qualified
        /// path. The add operation is recursive, so that any files or subdirectories
        /// within the name directory are also added to the archive.
        /// </para>
        /// 
        /// <para>
        /// Top-level entries in the named directory will appear as top-level entries
        /// in the zip archive.  Entries in subdirectories in the named directory will
        /// result in entries in subdirectories in the zip archive.
        /// </para>
        /// 
        /// <para>
        /// For <c>ZipFile</c> properties including <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.Encryption" />, <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.Password" />, <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.SetCompression" />, <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.ProvisionalAlternateEncoding" />, <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.ExtractExistingFile" />,
        /// <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.ZipErrorAction" />, and <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.CompressionLevel" />, their
        /// respective values at the time of this call will be applied to each
        /// ZipEntry added.
        /// </para>
        /// 
        /// </remarks>
        /// 
        /// <example>
        /// <para>
        /// In this code, calling the ZipUp() method with a value of "c:\reports" for
        /// the directory parameter will result in a zip file structure in which all
        /// entries are contained in a toplevel "reports" directory.
        /// </para>
        /// 
        /// <code lang="C#">
        /// public void ZipUp(string targetZip, string directory)
        /// {
        /// using (var zip = new ZipFile())
        /// {
        /// zip.AddDirectory(directory, System.IO.Path.GetFileName(directory));
        /// zip.Save(targetZip);
        /// }
        /// }
        /// </code>
        /// </example>
        /// 
        /// <seealso cref="M:DotNetZipAdditionalPlatforms.Zip.ZipFile.AddItem(System.String,System.String)" />
        /// <seealso cref="M:DotNetZipAdditionalPlatforms.Zip.ZipFile.AddFile(System.String,System.String)" />
        /// <seealso cref="M:DotNetZipAdditionalPlatforms.Zip.ZipFile.UpdateDirectory(System.String,System.String)" />
        /// 
        /// <param name="directoryName">The name of the directory to add.</param>
        /// 
        /// <param name="directoryPathInArchive">
        /// Specifies a directory path to use to override any path in the
        /// DirectoryName.  This path may, or may not, correspond to a real directory
        /// in the current filesystem.  If the zip is later extracted, this is the
        /// path used for the extracted file or directory.  Passing <c>null</c>
        /// (<c>Nothing</c> in VB) or the empty string ("") will insert the items at
        /// the root path within the archive.
        /// </param>
        /// 
        /// <returns>The <c>ZipEntry</c> added.</returns>
        public ZipEntry AddDirectory(string directoryName, string directoryPathInArchive)
        {
            return this.AddOrUpdateDirectoryImpl(directoryName, directoryPathInArchive, AddOrUpdateAction.AddOnly);
        }

        /// <summary>
        /// Creates a directory in the zip archive.
        /// </summary>
        /// 
        /// <remarks>
        /// 
        /// <para>
        /// Use this when you want to create a directory in the archive but there is
        /// no corresponding filesystem representation for that directory.
        /// </para>
        /// 
        /// <para>
        /// You will probably not need to do this in your code. One of the only times
        /// you will want to do this is if you want an empty directory in the zip
        /// archive.  The reason: if you add a file to a zip archive that is stored
        /// within a multi-level directory, all of the directory tree is implicitly
        /// created in the zip archive.
        /// </para>
        /// 
        /// </remarks>
        /// 
        /// <param name="directoryNameInArchive">
        /// The name of the directory to create in the archive.
        /// </param>
        /// <returns>The <c>ZipEntry</c> added.</returns>
        public ZipEntry AddDirectoryByName(string directoryNameInArchive)
        {
            ZipEntry entry = ZipEntry.CreateFromNothing(directoryNameInArchive);
            entry._container = new ZipContainer(this);
            entry.MarkAsDirectory();
            entry.AlternateEncoding = this.AlternateEncoding;
            entry.AlternateEncodingUsage = this.AlternateEncodingUsage;
            entry.SetEntryTimes(DateTime.Now, DateTime.Now, DateTime.Now);
            entry.EmitTimesInWindowsFormatWhenSaving = this._emitNtfsTimes;
            entry.EmitTimesInUnixFormatWhenSaving = this._emitUnixTimes;
            entry._Source = ZipEntrySource.Stream;
            this.InternalAddEntry(entry.FileName, entry);
            this.AfterAddEntry(entry);
            return entry;
        }

        /// <summary>
        /// Add an entry into the zip archive using the given filename and
        /// directory path within the archive, and the given content for the
        /// file. No file is created in the filesystem.
        /// </summary>
        /// 
        /// <param name="byteContent">The data to use for the entry.</param>
        /// 
        /// <param name="entryName">
        /// The name, including any path, to use within the archive for the entry.
        /// </param>
        /// 
        /// <returns>The <c>ZipEntry</c> added.</returns>
        public ZipEntry AddEntry(string entryName, byte[] byteContent)
        {
            if (byteContent == null)
            {
                throw new ArgumentException("bad argument", "byteContent");
            }
            MemoryStream stream = new MemoryStream(byteContent);
            return this.AddEntry(entryName, stream);
        }

        /// <summary>
        /// Add a ZipEntry for which content is written directly by the application.
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// When the application needs to write the zip entry data, use this
        /// method to add the ZipEntry. For example, in the case that the
        /// application wishes to write the XML representation of a DataSet into
        /// a ZipEntry, the application can use this method to do so.
        /// </para>
        /// 
        /// <para>
        /// For <c>ZipFile</c> properties including <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.Encryption" />, <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.Password" />, <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.SetCompression" />, <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.ProvisionalAlternateEncoding" />, <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.ExtractExistingFile" />,
        /// <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.ZipErrorAction" />, and <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.CompressionLevel" />, their
        /// respective values at the time of this call will be applied to the
        /// <c>ZipEntry</c> added.
        /// </para>
        /// 
        /// <para>
        /// About progress events: When using the WriteDelegate, DotNetZip does
        /// not issue any SaveProgress events with <c>EventType</c> = <see cref="F:DotNetZipAdditionalPlatforms.Zip.ZipProgressEventType.Saving_EntryBytesRead">
        /// Saving_EntryBytesRead</see>. (This is because it is the
        /// application's code that runs in WriteDelegate - there's no way for
        /// DotNetZip to know when to issue a EntryBytesRead event.)
        /// Applications that want to update a progress bar or similar status
        /// indicator should do so from within the WriteDelegate
        /// itself. DotNetZip will issue the other SaveProgress events,
        /// including <see cref="F:DotNetZipAdditionalPlatforms.Zip.ZipProgressEventType.Saving_Started">
        /// Saving_Started</see>,
        /// <see cref="F:DotNetZipAdditionalPlatforms.Zip.ZipProgressEventType.Saving_BeforeWriteEntry">
        /// Saving_BeforeWriteEntry</see>, and <see cref="F:DotNetZipAdditionalPlatforms.Zip.ZipProgressEventType.Saving_AfterWriteEntry">
        /// Saving_AfterWriteEntry</see>.
        /// </para>
        /// 
        /// <para>
        /// Note: When you use PKZip encryption, it's normally necessary to
        /// compute the CRC of the content to be encrypted, before compressing or
        /// encrypting it. Therefore, when using PKZip encryption with a
        /// WriteDelegate, the WriteDelegate CAN BE called twice: once to compute
        /// the CRC, and the second time to potentially compress and
        /// encrypt. Surprising, but true. This is because PKWARE specified that
        /// the encryption initialization data depends on the CRC.
        /// If this happens, for each call of the delegate, your
        /// application must stream the same entry data in its entirety. If your
        /// application writes different data during the second call, it will
        /// result in a corrupt zip file.
        /// </para>
        /// 
        /// <para>
        /// The double-read behavior happens with all types of entries, not only
        /// those that use WriteDelegate. It happens if you add an entry from a
        /// filesystem file, or using a string, or a stream, or an opener/closer
        /// pair. But in those cases, DotNetZip takes care of reading twice; in
        /// the case of the WriteDelegate, the application code gets invoked
        /// twice. Be aware.
        /// </para>
        /// 
        /// <para>
        /// As you can imagine, this can cause performance problems for large
        /// streams, and it can lead to correctness problems when you use a
        /// <c>WriteDelegate</c>. This is a pretty big pitfall.  There are two
        /// ways to avoid it.  First, and most preferred: don't use PKZIP
        /// encryption.  If you use the WinZip AES encryption, this problem
        /// doesn't occur, because the encryption protocol doesn't require the CRC
        /// up front. Second: if you do choose to use PKZIP encryption, write out
        /// to a non-seekable stream (like standard output, or the
        /// Response.OutputStream in an ASP.NET application).  In this case,
        /// DotNetZip will use an alternative encryption protocol that does not
        /// rely on the CRC of the content.  This also implies setting bit 3 in
        /// the zip entry, which still presents problems for some zip tools.
        /// </para>
        /// 
        /// <para>
        /// In the future I may modify DotNetZip to *always* use bit 3 when PKZIP
        /// encryption is in use.  This seems like a win overall, but there will
        /// be some work involved.  If you feel strongly about it, visit the
        /// DotNetZip forums and vote up <see href="http://dotnetzip.codeplex.com/workitem/13686">the Workitem
        /// tracking this issue</see>.
        /// </para>
        /// 
        /// </remarks>
        /// 
        /// <param name="entryName">the name of the entry to add</param>
        /// <param name="writer">the delegate which will write the entry content</param>
        /// <returns>the ZipEntry added</returns>
        /// 
        /// <example>
        /// 
        /// This example shows an application filling a DataSet, then saving the
        /// contents of that DataSet as XML, into a ZipEntry in a ZipFile, using an
        /// anonymous delegate in C#. The DataSet XML is never saved to a disk file.
        /// 
        /// <code lang="C#">
        /// var c1= new System.Data.SqlClient.SqlConnection(connstring1);
        /// var da = new System.Data.SqlClient.SqlDataAdapter()
        /// {
        /// SelectCommand=  new System.Data.SqlClient.SqlCommand(strSelect, c1)
        /// };
        /// 
        /// DataSet ds1 = new DataSet();
        /// da.Fill(ds1, "Invoices");
        /// 
        /// using(DotNetZipAdditionalPlatforms.Zip.ZipFile zip = new DotNetZipAdditionalPlatforms.Zip.ZipFile())
        /// {
        /// zip.AddEntry(zipEntryName, (name,stream) =&gt; ds1.WriteXml(stream) );
        /// zip.Save(zipFileName);
        /// }
        /// </code>
        /// </example>
        /// 
        /// <example>
        /// 
        /// This example uses an anonymous method in C# as the WriteDelegate to provide
        /// the data for the ZipEntry. The example is a bit contrived - the
        /// <c>AddFile()</c> method is a simpler way to insert the contents of a file
        /// into an entry in a zip file. On the other hand, if there is some sort of
        /// processing or transformation of the file contents required before writing,
        /// the application could use the <c>WriteDelegate</c> to do it, in this way.
        /// 
        /// <code lang="C#">
        /// using (var input = File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite ))
        /// {
        /// using(DotNetZipAdditionalPlatforms.Zip.ZipFile zip = new DotNetZipAdditionalPlatforms.Zip.ZipFile())
        /// {
        /// zip.AddEntry(zipEntryName, (name,output) =&gt;
        /// {
        /// byte[] buffer = new byte[BufferSize];
        /// int n;
        /// while ((n = input.Read(buffer, 0, buffer.Length)) != 0)
        /// {
        /// // could transform the data here...
        /// output.Write(buffer, 0, n);
        /// // could update a progress bar here
        /// }
        /// });
        /// 
        /// zip.Save(zipFileName);
        /// }
        /// }
        /// </code>
        /// </example>
        /// 
        /// <example>
        /// 
        /// This example uses a named delegate in VB to write data for the given
        /// ZipEntry (VB9 does not have anonymous delegates). The example here is a bit
        /// contrived - a simpler way to add the contents of a file to a ZipEntry is to
        /// simply use the appropriate <c>AddFile()</c> method.  The key scenario for
        /// which the <c>WriteDelegate</c> makes sense is saving a DataSet, in XML
        /// format, to the zip file. The DataSet can write XML to a stream, and the
        /// WriteDelegate is the perfect place to write into the zip file.  There may be
        /// other data structures that can write to a stream, but cannot be read as a
        /// stream.  The <c>WriteDelegate</c> would be appropriate for those cases as
        /// well.
        /// 
        /// <code lang="VB">
        /// Private Sub WriteEntry (ByVal name As String, ByVal output As Stream)
        /// Using input As FileStream = File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)
        /// Dim n As Integer = -1
        /// Dim buffer As Byte() = New Byte(BufferSize){}
        /// Do While n &lt;&gt; 0
        /// n = input.Read(buffer, 0, buffer.Length)
        /// output.Write(buffer, 0, n)
        /// Loop
        /// End Using
        /// End Sub
        /// 
        /// Public Sub Run()
        /// Using zip = New ZipFile
        /// zip.AddEntry(zipEntryName, New WriteDelegate(AddressOf WriteEntry))
        /// zip.Save(zipFileName)
        /// End Using
        /// End Sub
        /// </code>
        /// </example>
        public ZipEntry AddEntry(string entryName, DotNetZipAdditionalPlatforms.Zip.WriteDelegate writer)
        {
            ZipEntry ze = ZipEntry.CreateForWriter(entryName, writer);
            if (this.Verbose)
            {
                this.StatusMessageTextWriter.WriteLine("adding {0}...", entryName);
            }
            return this._InternalAddEntry(ze);
        }

        /// <summary>
        /// Create an entry in the <c>ZipFile</c> using the given <c>Stream</c>
        /// as input.  The entry will have the given filename.
        /// </summary>
        /// 
        /// <remarks>
        /// 
        /// <para>
        /// The application should provide an open, readable stream; in this case it
        /// will be read during the call to <see cref="M:DotNetZipAdditionalPlatforms.Zip.ZipFile.Save" /> or one of
        /// its overloads.
        /// </para>
        /// 
        /// <para>
        /// The passed stream will be read from its current position. If
        /// necessary, callers should set the position in the stream before
        /// calling AddEntry(). This might be appropriate when using this method
        /// with a MemoryStream, for example.
        /// </para>
        /// 
        /// <para>
        /// In cases where a large number of streams will be added to the
        /// <c>ZipFile</c>, the application may wish to avoid maintaining all of the
        /// streams open simultaneously.  To handle this situation, the application
        /// should use the <see cref="M:DotNetZipAdditionalPlatforms.Zip.ZipFile.AddEntry(System.String,DotNetZipAdditionalPlatforms.Zip.OpenDelegate,DotNetZipAdditionalPlatforms.Zip.CloseDelegate)" />
        /// overload.
        /// </para>
        /// 
        /// <para>
        /// For <c>ZipFile</c> properties including <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.Encryption" />, <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.Password" />, <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.SetCompression" />, <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.ProvisionalAlternateEncoding" />, <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.ExtractExistingFile" />,
        /// <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.ZipErrorAction" />, and <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.CompressionLevel" />, their
        /// respective values at the time of this call will be applied to the
        /// <c>ZipEntry</c> added.
        /// </para>
        /// 
        /// </remarks>
        /// 
        /// <example>
        /// <para>
        /// This example adds a single entry to a <c>ZipFile</c> via a <c>Stream</c>.
        /// </para>
        /// 
        /// <code lang="C#">
        /// String zipToCreate = "Content.zip";
        /// String fileNameInArchive = "Content-From-Stream.bin";
        /// using (System.IO.Stream streamToRead = MyStreamOpener())
        /// {
        /// using (ZipFile zip = new ZipFile())
        /// {
        /// ZipEntry entry= zip.AddEntry(fileNameInArchive, streamToRead);
        /// zip.AddFile("Readme.txt");
        /// zip.Save(zipToCreate);  // the stream is read implicitly here
        /// }
        /// }
        /// </code>
        /// 
        /// <code lang="VB">
        /// Dim zipToCreate As String = "Content.zip"
        /// Dim fileNameInArchive As String = "Content-From-Stream.bin"
        /// Using streamToRead as System.IO.Stream = MyStreamOpener()
        /// Using zip As ZipFile = New ZipFile()
        /// Dim entry as ZipEntry = zip.AddEntry(fileNameInArchive, streamToRead)
        /// zip.AddFile("Readme.txt")
        /// zip.Save(zipToCreate)  '' the stream is read implicitly, here
        /// End Using
        /// End Using
        /// </code>
        /// </example>
        /// 
        /// <seealso cref="M:DotNetZipAdditionalPlatforms.Zip.ZipFile.UpdateEntry(System.String,System.IO.Stream)" />
        /// 
        /// <param name="entryName">
        /// The name, including any path, which is shown in the zip file for the added
        /// entry.
        /// </param>
        /// <param name="stream">
        /// The input stream from which to grab content for the file
        /// </param>
        /// <returns>The <c>ZipEntry</c> added.</returns>
        public ZipEntry AddEntry(string entryName, Stream stream)
        {
            ZipEntry ze = ZipEntry.CreateForStream(entryName, stream);
            ze.SetEntryTimes(DateTime.Now, DateTime.Now, DateTime.Now);
            if (this.Verbose)
            {
                this.StatusMessageTextWriter.WriteLine("adding {0}...", entryName);
            }
            return this._InternalAddEntry(ze);
        }

        /// <summary>
        /// Adds a named entry into the zip archive, taking content for the entry
        /// from a string.
        /// </summary>
        /// 
        /// <remarks>
        /// Calling this method creates an entry using the given fileName and
        /// directory path within the archive.  There is no need for a file by the
        /// given name to exist in the filesystem; the name is used within the zip
        /// archive only. The content for the entry is encoded using the default text
        /// encoding for the machine, or on Silverlight, using UTF-8.
        /// </remarks>
        /// 
        /// <param name="content">
        /// The content of the file, should it be extracted from the zip.
        /// </param>
        /// 
        /// <param name="entryName">
        /// The name, including any path, to use for the entry within the archive.
        /// </param>
        /// 
        /// <returns>The <c>ZipEntry</c> added.</returns>
        /// 
        /// <example>
        /// 
        /// This example shows how to add an entry to the zipfile, using a string as
        /// content for that entry.
        /// 
        /// <code lang="C#">
        /// string Content = "This string will be the content of the Readme.txt file in the zip archive.";
        /// using (ZipFile zip1 = new ZipFile())
        /// {
        /// zip1.AddFile("MyDocuments\\Resume.doc", "files");
        /// zip1.AddEntry("Readme.txt", Content);
        /// zip1.Comment = "This zip file was created at " + System.DateTime.Now.ToString("G");
        /// zip1.Save("Content.zip");
        /// }
        /// 
        /// </code>
        /// <code lang="VB">
        /// Public Sub Run()
        /// Dim Content As String = "This string will be the content of the Readme.txt file in the zip archive."
        /// Using zip1 As ZipFile = New ZipFile
        /// zip1.AddEntry("Readme.txt", Content)
        /// zip1.AddFile("MyDocuments\Resume.doc", "files")
        /// zip1.Comment = ("This zip file was created at " &amp; DateTime.Now.ToString("G"))
        /// zip1.Save("Content.zip")
        /// End Using
        /// End Sub
        /// </code>
        /// </example>
        public ZipEntry AddEntry(string entryName, string content)
        {
            return this.AddEntry(entryName, content, Encoding.Default);
        }

        /// <summary>
        /// Add an entry, for which the application will provide a stream,
        /// just-in-time.
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// In cases where the application wishes to open the stream that holds
        /// the content for the ZipEntry, on a just-in-time basis, the application
        /// can use this method and provide delegates to open and close the
        /// stream.
        /// </para>
        /// 
        /// <para>
        /// For <c>ZipFile</c> properties including <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.Encryption" />, <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.Password" />, <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.SetCompression" />, <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.ProvisionalAlternateEncoding" />, <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.ExtractExistingFile" />,
        /// <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.ZipErrorAction" />, and <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.CompressionLevel" />, their
        /// respective values at the time of this call will be applied to the
        /// <c>ZipEntry</c> added.
        /// </para>
        /// 
        /// </remarks>
        /// 
        /// <example>
        /// 
        /// This example uses anonymous methods in C# to open and close the
        /// source stream for the content for a zip entry.  In a real
        /// application, the logic for the OpenDelegate would probably be more
        /// involved.
        /// 
        /// <code lang="C#">
        /// using(DotNetZipAdditionalPlatforms.Zip.ZipFile zip = new DotNetZipAdditionalPlatforms.Zip.ZipFile())
        /// {
        /// zip.AddEntry(zipEntryName,
        /// (name) =&gt;  File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite ),
        /// (name, stream) =&gt;  stream.Close()
        /// );
        /// 
        /// zip.Save(zipFileName);
        /// }
        /// </code>
        /// 
        /// </example>
        /// 
        /// <example>
        /// 
        /// This example uses delegates in VB.NET to open and close the
        /// the source stream for the content for a zip entry.  VB 9.0 lacks
        /// support for "Sub" lambda expressions, and so the CloseDelegate must
        /// be an actual, named Sub.
        /// 
        /// <code lang="VB">
        /// 
        /// Function MyStreamOpener(ByVal entryName As String) As Stream
        /// '' This simply opens a file.  You probably want to do somethinig
        /// '' more involved here: open a stream to read from a database,
        /// '' open a stream on an HTTP connection, and so on.
        /// Return File.OpenRead(entryName)
        /// End Function
        /// 
        /// Sub MyStreamCloser(entryName As String, stream As Stream)
        /// stream.Close()
        /// End Sub
        /// 
        /// Public Sub Run()
        /// Dim dirToZip As String = "fodder"
        /// Dim zipFileToCreate As String = "Archive.zip"
        /// Dim opener As OpenDelegate = AddressOf MyStreamOpener
        /// Dim closer As CloseDelegate = AddressOf MyStreamCloser
        /// Dim numFilestoAdd As Int32 = 4
        /// Using zip As ZipFile = New ZipFile
        /// Dim i As Integer
        /// For i = 0 To numFilesToAdd - 1
        /// zip.AddEntry(string.Format(CultureInfo.InvariantCulture, "content-{0:000}.txt"), opener, closer)
        /// Next i
        /// zip.Save(zipFileToCreate)
        /// End Using
        /// End Sub
        /// 
        /// </code>
        /// </example>
        /// 
        /// <param name="entryName">the name of the entry to add</param>
        /// <param name="opener">
        /// the delegate that will be invoked to open the stream
        /// </param>
        /// <param name="closer">
        /// the delegate that will be invoked to close the stream
        /// </param>
        /// <returns>the ZipEntry added</returns>
        public ZipEntry AddEntry(string entryName, OpenDelegate opener, CloseDelegate closer)
        {
            ZipEntry ze = ZipEntry.CreateForJitStreamProvider(entryName, opener, closer);
            ze.SetEntryTimes(DateTime.Now, DateTime.Now, DateTime.Now);
            if (this.Verbose)
            {
                this.StatusMessageTextWriter.WriteLine("adding {0}...", entryName);
            }
            return this._InternalAddEntry(ze);
        }

        /// <summary>
        /// Adds a named entry into the zip archive, taking content for the entry
        /// from a string, and using the specified text encoding.
        /// </summary>
        /// 
        /// <remarks>
        /// 
        /// <para>
        /// Calling this method creates an entry using the given fileName and
        /// directory path within the archive.  There is no need for a file by the
        /// given name to exist in the filesystem; the name is used within the zip
        /// archive only.
        /// </para>
        /// 
        /// <para>
        /// The content for the entry, a string value, is encoded using the given
        /// text encoding. A BOM (byte-order-mark) is emitted into the file, if the
        /// Encoding parameter is set for that.
        /// </para>
        /// 
        /// <para>
        /// Most Encoding classes support a constructor that accepts a boolean,
        /// indicating whether to emit a BOM or not. For example see <see cref="M:System.Text.UTF8Encoding.#ctor(System.Boolean)" />.
        /// </para>
        /// 
        /// </remarks>
        /// 
        /// <param name="entryName">
        /// The name, including any path, to use within the archive for the entry.
        /// </param>
        /// 
        /// <param name="content">
        /// The content of the file, should it be extracted from the zip.
        /// </param>
        /// 
        /// <param name="encoding">
        /// The text encoding to use when encoding the string. Be aware: This is
        /// distinct from the text encoding used to encode the fileName, as specified
        /// in <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.ProvisionalAlternateEncoding" />.
        /// </param>
        /// 
        /// <returns>The <c>ZipEntry</c> added.</returns>
        public ZipEntry AddEntry(string entryName, string content, Encoding encoding)
        {
            MemoryStream stream = new MemoryStream();
            StreamWriter writer = new StreamWriter(stream, encoding);
            writer.Write(content);
            writer.Flush();
            stream.Seek(0L, SeekOrigin.Begin);
            return this.AddEntry(entryName, stream);
        }

        /// <summary>
        /// Adds a File to a Zip file archive.
        /// </summary>
        /// <remarks>
        /// 
        /// <para>
        /// This call collects metadata for the named file in the filesystem,
        /// including the file attributes and the timestamp, and inserts that metadata
        /// into the resulting ZipEntry.  Only when the application calls Save() on
        /// the <c>ZipFile</c>, does DotNetZip read the file from the filesystem and
        /// then write the content to the zip file archive.
        /// </para>
        /// 
        /// <para>
        /// This method will throw an exception if an entry with the same name already
        /// exists in the <c>ZipFile</c>.
        /// </para>
        /// 
        /// <para>
        /// For <c>ZipFile</c> properties including <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.Encryption" />, <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.Password" />, <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.SetCompression" />, <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.ProvisionalAlternateEncoding" />, <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.ExtractExistingFile" />,
        /// <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.ZipErrorAction" />, and <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.CompressionLevel" />, their
        /// respective values at the time of this call will be applied to the
        /// <c>ZipEntry</c> added.
        /// </para>
        /// 
        /// </remarks>
        /// 
        /// <example>
        /// <para>
        /// In this example, three files are added to a Zip archive. The ReadMe.txt
        /// file will be placed in the root of the archive. The .png file will be
        /// placed in a folder within the zip called photos\personal.  The pdf file
        /// will be included into a folder within the zip called Desktop.
        /// </para>
        /// <code>
        /// try
        /// {
        /// using (ZipFile zip = new ZipFile())
        /// {
        /// zip.AddFile("c:\\photos\\personal\\7440-N49th.png");
        /// zip.AddFile("c:\\Desktop\\2008-Regional-Sales-Report.pdf");
        /// zip.AddFile("ReadMe.txt");
        /// 
        /// zip.Save("Package.zip");
        /// }
        /// }
        /// catch (System.Exception ex1)
        /// {
        /// System.Console.Error.WriteLine("exception: " + ex1);
        /// }
        /// </code>
        /// 
        /// <code lang="VB">
        /// Try
        /// Using zip As ZipFile = New ZipFile
        /// zip.AddFile("c:\photos\personal\7440-N49th.png")
        /// zip.AddFile("c:\Desktop\2008-Regional-Sales-Report.pdf")
        /// zip.AddFile("ReadMe.txt")
        /// zip.Save("Package.zip")
        /// End Using
        /// Catch ex1 As Exception
        /// Console.Error.WriteLine("exception: {0}", ex1.ToString)
        /// End Try
        /// </code>
        /// </example>
        /// 
        /// <overloads>This method has two overloads.</overloads>
        /// 
        /// <seealso cref="M:DotNetZipAdditionalPlatforms.Zip.ZipFile.AddItem(System.String)" />
        /// <seealso cref="M:DotNetZipAdditionalPlatforms.Zip.ZipFile.AddDirectory(System.String)" />
        /// <seealso cref="M:DotNetZipAdditionalPlatforms.Zip.ZipFile.UpdateFile(System.String)" />
        /// 
        /// <param name="fileName">
        /// The name of the file to add. It should refer to a file in the filesystem.
        /// The name of the file may be a relative path or a fully-qualified path.
        /// </param>
        /// <returns>The <c>ZipEntry</c> corresponding to the File added.</returns>
        public ZipEntry AddFile(string fileName)
        {
            return this.AddFile(fileName, null);
        }

        /// <summary>
        /// Adds a File to a Zip file archive, potentially overriding the path to be
        /// used within the zip archive.
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// The file added by this call to the <c>ZipFile</c> is not written to the
        /// zip file archive until the application calls Save() on the <c>ZipFile</c>.
        /// </para>
        /// 
        /// <para>
        /// This method will throw an exception if an entry with the same name already
        /// exists in the <c>ZipFile</c>.
        /// </para>
        /// 
        /// <para>
        /// This version of the method allows the caller to explicitly specify the
        /// directory path to be used in the archive.
        /// </para>
        /// 
        /// <para>
        /// For <c>ZipFile</c> properties including <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.Encryption" />, <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.Password" />, <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.SetCompression" />, <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.ProvisionalAlternateEncoding" />, <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.ExtractExistingFile" />,
        /// <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.ZipErrorAction" />, and <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.CompressionLevel" />, their
        /// respective values at the time of this call will be applied to the
        /// <c>ZipEntry</c> added.
        /// </para>
        /// 
        /// </remarks>
        /// 
        /// <example>
        /// <para>
        /// In this example, three files are added to a Zip archive. The ReadMe.txt
        /// file will be placed in the root of the archive. The .png file will be
        /// placed in a folder within the zip called images.  The pdf file will be
        /// included into a folder within the zip called files\docs, and will be
        /// encrypted with the given password.
        /// </para>
        /// <code>
        /// try
        /// {
        /// using (ZipFile zip = new ZipFile())
        /// {
        /// // the following entry will be inserted at the root in the archive.
        /// zip.AddFile("c:\\datafiles\\ReadMe.txt", "");
        /// // this image file will be inserted into the "images" directory in the archive.
        /// zip.AddFile("c:\\photos\\personal\\7440-N49th.png", "images");
        /// // the following will result in a password-protected file called
        /// // files\\docs\\2008-Regional-Sales-Report.pdf  in the archive.
        /// zip.Password = "EncryptMe!";
        /// zip.AddFile("c:\\Desktop\\2008-Regional-Sales-Report.pdf", "files\\docs");
        /// zip.Save("Archive.zip");
        /// }
        /// }
        /// catch (System.Exception ex1)
        /// {
        /// System.Console.Error.WriteLine("exception: {0}", ex1);
        /// }
        /// </code>
        /// 
        /// <code lang="VB">
        /// Try
        /// Using zip As ZipFile = New ZipFile
        /// ' the following entry will be inserted at the root in the archive.
        /// zip.AddFile("c:\datafiles\ReadMe.txt", "")
        /// ' this image file will be inserted into the "images" directory in the archive.
        /// zip.AddFile("c:\photos\personal\7440-N49th.png", "images")
        /// ' the following will result in a password-protected file called
        /// ' files\\docs\\2008-Regional-Sales-Report.pdf  in the archive.
        /// zip.Password = "EncryptMe!"
        /// zip.AddFile("c:\Desktop\2008-Regional-Sales-Report.pdf", "files\documents")
        /// zip.Save("Archive.zip")
        /// End Using
        /// Catch ex1 As Exception
        /// Console.Error.WriteLine("exception: {0}", ex1)
        /// End Try
        /// </code>
        /// </example>
        /// 
        /// <seealso cref="M:DotNetZipAdditionalPlatforms.Zip.ZipFile.AddItem(System.String,System.String)" />
        /// <seealso cref="M:DotNetZipAdditionalPlatforms.Zip.ZipFile.AddDirectory(System.String,System.String)" />
        /// <seealso cref="M:DotNetZipAdditionalPlatforms.Zip.ZipFile.UpdateFile(System.String,System.String)" />
        /// 
        /// <param name="fileName">
        /// The name of the file to add.  The name of the file may be a relative path
        /// or a fully-qualified path.
        /// </param>
        /// 
        /// <param name="directoryPathInArchive">
        /// Specifies a directory path to use to override any path in the fileName.
        /// This path may, or may not, correspond to a real directory in the current
        /// filesystem.  If the files within the zip are later extracted, this is the
        /// path used for the extracted file.  Passing <c>null</c> (<c>Nothing</c> in
        /// VB) will use the path on the fileName, if any.  Passing the empty string
        /// ("") will insert the item at the root path within the archive.
        /// </param>
        /// 
        /// <returns>The <c>ZipEntry</c> corresponding to the file added.</returns>
        public ZipEntry AddFile(string fileName, string directoryPathInArchive)
        {
            string nameInArchive = ZipEntry.NameInArchive(fileName, directoryPathInArchive);
            ZipEntry ze = ZipEntry.CreateFromFile(fileName, nameInArchive);
            if (this.Verbose)
            {
                this.StatusMessageTextWriter.WriteLine("adding {0}...", fileName);
            }
            return this._InternalAddEntry(ze);
        }

        /// <summary>
        /// This method adds a set of files to the <c>ZipFile</c>.
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// Use this method to add a set of files to the zip archive, in one call.
        /// For example, a list of files received from
        /// <c>System.IO.Directory.GetFiles()</c> can be added to a zip archive in one
        /// call.
        /// </para>
        /// 
        /// <para>
        /// For <c>ZipFile</c> properties including <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.Encryption" />, <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.Password" />, <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.SetCompression" />, <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.ProvisionalAlternateEncoding" />, <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.ExtractExistingFile" />,
        /// <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.ZipErrorAction" />, and <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.CompressionLevel" />, their
        /// respective values at the time of this call will be applied to each
        /// ZipEntry added.
        /// </para>
        /// </remarks>
        /// 
        /// <param name="fileNames">
        /// The collection of names of the files to add. Each string should refer to a
        /// file in the filesystem. The name of the file may be a relative path or a
        /// fully-qualified path.
        /// </param>
        /// 
        /// <example>
        /// This example shows how to create a zip file, and add a few files into it.
        /// <code>
        /// String ZipFileToCreate = "archive1.zip";
        /// String DirectoryToZip = "c:\\reports";
        /// using (ZipFile zip = new ZipFile())
        /// {
        /// // Store all files found in the top level directory, into the zip archive.
        /// String[] filenames = System.IO.Directory.GetFiles(DirectoryToZip);
        /// zip.AddFiles(filenames);
        /// zip.Save(ZipFileToCreate);
        /// }
        /// </code>
        /// 
        /// <code lang="VB">
        /// Dim ZipFileToCreate As String = "archive1.zip"
        /// Dim DirectoryToZip As String = "c:\reports"
        /// Using zip As ZipFile = New ZipFile
        /// ' Store all files found in the top level directory, into the zip archive.
        /// Dim filenames As String() = System.IO.Directory.GetFiles(DirectoryToZip)
        /// zip.AddFiles(filenames)
        /// zip.Save(ZipFileToCreate)
        /// End Using
        /// </code>
        /// </example>
        /// 
        /// <seealso cref="M:DotNetZipAdditionalPlatforms.Zip.ZipFile.AddSelectedFiles(System.String,System.String)" />
        public void AddFiles(IEnumerable<string> fileNames)
        {
            this.AddFiles(fileNames, null);
        }

        /// <summary>
        /// Adds a set of files to the <c>ZipFile</c>, using the
        /// specified directory path in the archive.
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// Any directory structure that may be present in the
        /// filenames contained in the list is "flattened" in the
        /// archive.  Each file in the list is added to the archive in
        /// the specified top-level directory.
        /// </para>
        /// 
        /// <para>
        /// For <c>ZipFile</c> properties including <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.Encryption" />, <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.Password" />, <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.SetCompression" />, <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.ProvisionalAlternateEncoding" />, <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.ExtractExistingFile" />, <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.ZipErrorAction" />, and <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.CompressionLevel" />, their respective values at the
        /// time of this call will be applied to each ZipEntry added.
        /// </para>
        /// </remarks>
        /// 
        /// <param name="fileNames">
        /// The names of the files to add. Each string should refer to
        /// a file in the filesystem.  The name of the file may be a
        /// relative path or a fully-qualified path.
        /// </param>
        /// 
        /// <param name="directoryPathInArchive">
        /// Specifies a directory path to use to override any path in the file name.
        /// Th is path may, or may not, correspond to a real directory in the current
        /// filesystem.  If the files within the zip are later extracted, this is the
        /// path used for the extracted file.  Passing <c>null</c> (<c>Nothing</c> in
        /// VB) will use the path on each of the <c>fileNames</c>, if any.  Passing
        /// the empty string ("") will insert the item at the root path within the
        /// archive.
        /// </param>
        /// 
        /// <seealso cref="M:DotNetZipAdditionalPlatforms.Zip.ZipFile.AddSelectedFiles(System.String,System.String)" />
        public void AddFiles(IEnumerable<string> fileNames, string directoryPathInArchive)
        {
            this.AddFiles(fileNames, false, directoryPathInArchive);
        }

        /// <summary>
        /// Adds a set of files to the <c>ZipFile</c>, using the specified directory
        /// path in the archive, and preserving the full directory structure in the
        /// filenames.
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// If preserveDirHierarchy is true, any directory structure present in the
        /// filenames contained in the list is preserved in the archive.  On the other
        /// hand, if preserveDirHierarchy is false, any directory structure that may
        /// be present in the filenames contained in the list is "flattened" in the
        /// archive; Each file in the list is added to the archive in the specified
        /// top-level directory.
        /// </para>
        /// 
        /// <para>
        /// For <c>ZipFile</c> properties including <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.Encryption" />, <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.Password" />, <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.SetCompression" />, <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.ProvisionalAlternateEncoding" />, <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.ExtractExistingFile" />,
        /// <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.ZipErrorAction" />, and <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.CompressionLevel" />, their
        /// respective values at the time of this call will be applied to each
        /// ZipEntry added.
        /// </para>
        /// 
        /// </remarks>
        /// 
        /// <param name="fileNames">
        /// The names of the files to add. Each string should refer to a file in the
        /// filesystem.  The name of the file may be a relative path or a
        /// fully-qualified path.
        /// </param>
        /// 
        /// <param name="directoryPathInArchive">
        /// Specifies a directory path to use to override any path in the file name.
        /// This path may, or may not, correspond to a real directory in the current
        /// filesystem.  If the files within the zip are later extracted, this is the
        /// path used for the extracted file.  Passing <c>null</c> (<c>Nothing</c> in
        /// VB) will use the path on each of the <c>fileNames</c>, if any.  Passing
        /// the empty string ("") will insert the item at the root path within the
        /// archive.
        /// </param>
        /// 
        /// <param name="preserveDirHierarchy">
        /// whether the entries in the zip archive will reflect the directory
        /// hierarchy that is present in the various filenames.  For example, if <paramref name="fileNames" />
        /// includes two paths, \Animalia\Chordata\Mammalia\Info.txt and
        /// \Plantae\Magnoliophyta\Dicotyledon\Info.txt, then calling this method with
        /// <paramref name="preserveDirHierarchy" /> = <c>false</c> will result in an
        /// exception because of a duplicate entry name, while calling this method
        /// with <paramref name="preserveDirHierarchy" /> = <c>true</c> will result in the
        /// full direcory paths being included in the entries added to the ZipFile.
        /// </param>
        /// <seealso cref="M:DotNetZipAdditionalPlatforms.Zip.ZipFile.AddSelectedFiles(System.String,System.String)" />
        public void AddFiles(IEnumerable<string> fileNames, bool preserveDirHierarchy, string directoryPathInArchive)
        {
            if (fileNames == null)
            {
                throw new ArgumentNullException("fileNames");
            }
            this._addOperationCanceled = false;
            this.OnAddStarted();
            if (preserveDirHierarchy)
            {
                foreach (string str in fileNames)
                {
                    if (this._addOperationCanceled)
                    {
                        break;
                    }
                    if (directoryPathInArchive != null)
                    {
                        string fullPath = Path.GetFullPath(Path.Combine(directoryPathInArchive, Path.GetDirectoryName(str)));
                        this.AddFile(str, fullPath);
                    }
                    else
                    {
                        this.AddFile(str, null);
                    }
                }
            }
            else
            {
                foreach (string str in fileNames)
                {
                    if (this._addOperationCanceled)
                    {
                        break;
                    }
                    this.AddFile(str, directoryPathInArchive);
                }
            }
            if (!this._addOperationCanceled)
            {
                this.OnAddCompleted();
            }
        }

        /// <summary>
        /// Adds an item, either a file or a directory, to a zip file archive.
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// This method is handy if you are adding things to zip archive and don't
        /// want to bother distinguishing between directories or files.  Any files are
        /// added as single entries.  A directory added through this method is added
        /// recursively: all files and subdirectories contained within the directory
        /// are added to the <c>ZipFile</c>.
        /// </para>
        /// 
        /// <para>
        /// The name of the item may be a relative path or a fully-qualified
        /// path. Remember, the items contained in <c>ZipFile</c> instance get written
        /// to the disk only when you call <see cref="M:DotNetZipAdditionalPlatforms.Zip.ZipFile.Save" /> or a similar
        /// save method.
        /// </para>
        /// 
        /// <para>
        /// The directory name used for the file within the archive is the same
        /// as the directory name (potentially a relative path) specified in the
        /// <paramref name="fileOrDirectoryName" />.
        /// </para>
        /// 
        /// <para>
        /// For <c>ZipFile</c> properties including <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.Encryption" />, <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.Password" />, <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.SetCompression" />, <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.ProvisionalAlternateEncoding" />, <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.ExtractExistingFile" />,
        /// <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.ZipErrorAction" />, and <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.CompressionLevel" />, their
        /// respective values at the time of this call will be applied to the
        /// <c>ZipEntry</c> added.
        /// </para>
        /// 
        /// </remarks>
        /// 
        /// <seealso cref="M:DotNetZipAdditionalPlatforms.Zip.ZipFile.AddFile(System.String)" />
        /// <seealso cref="M:DotNetZipAdditionalPlatforms.Zip.ZipFile.AddDirectory(System.String)" />
        /// <seealso cref="M:DotNetZipAdditionalPlatforms.Zip.ZipFile.UpdateItem(System.String)" />
        /// 
        /// <overloads>This method has two overloads.</overloads>
        /// <param name="fileOrDirectoryName">
        /// the name of the file or directory to add.</param>
        /// 
        /// <returns>The <c>ZipEntry</c> added.</returns>
        public ZipEntry AddItem(string fileOrDirectoryName)
        {
            return this.AddItem(fileOrDirectoryName, null);
        }

        /// <summary>
        /// Adds an item, either a file or a directory, to a zip file archive,
        /// explicitly specifying the directory path to be used in the archive.
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// If adding a directory, the add is recursive on all files and
        /// subdirectories contained within it.
        /// </para>
        /// <para>
        /// The name of the item may be a relative path or a fully-qualified path.
        /// The item added by this call to the <c>ZipFile</c> is not read from the
        /// disk nor written to the zip file archive until the application calls
        /// Save() on the <c>ZipFile</c>.
        /// </para>
        /// 
        /// <para>
        /// This version of the method allows the caller to explicitly specify the
        /// directory path to be used in the archive, which would override the
        /// "natural" path of the filesystem file.
        /// </para>
        /// 
        /// <para>
        /// Encryption will be used on the file data if the <c>Password</c> has
        /// been set on the <c>ZipFile</c> object, prior to calling this method.
        /// </para>
        /// 
        /// <para>
        /// For <c>ZipFile</c> properties including <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.Encryption" />, <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.Password" />, <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.SetCompression" />, <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.ProvisionalAlternateEncoding" />, <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.ExtractExistingFile" />,
        /// <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.ZipErrorAction" />, and <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.CompressionLevel" />, their
        /// respective values at the time of this call will be applied to the
        /// <c>ZipEntry</c> added.
        /// </para>
        /// 
        /// </remarks>
        /// 
        /// <exception cref="T:System.IO.FileNotFoundException">
        /// Thrown if the file or directory passed in does not exist.
        /// </exception>
        /// 
        /// <param name="fileOrDirectoryName">the name of the file or directory to add.
        /// </param>
        /// 
        /// <param name="directoryPathInArchive">
        /// The name of the directory path to use within the zip archive.  This path
        /// need not refer to an extant directory in the current filesystem.  If the
        /// files within the zip are later extracted, this is the path used for the
        /// extracted file.  Passing <c>null</c> (<c>Nothing</c> in VB) will use the
        /// path on the fileOrDirectoryName.  Passing the empty string ("") will
        /// insert the item at the root path within the archive.
        /// </param>
        /// 
        /// <seealso cref="M:DotNetZipAdditionalPlatforms.Zip.ZipFile.AddFile(System.String,System.String)" />
        /// <seealso cref="M:DotNetZipAdditionalPlatforms.Zip.ZipFile.AddDirectory(System.String,System.String)" />
        /// <seealso cref="M:DotNetZipAdditionalPlatforms.Zip.ZipFile.UpdateItem(System.String,System.String)" />
        /// 
        /// <example>
        /// This example shows how to zip up a set of files into a flat hierarchy,
        /// regardless of where in the filesystem the files originated. The resulting
        /// zip archive will contain a toplevel directory named "flat", which itself
        /// will contain files Readme.txt, MyProposal.docx, and Image1.jpg.  A
        /// subdirectory under "flat" called SupportFiles will contain all the files
        /// in the "c:\SupportFiles" directory on disk.
        /// 
        /// <code>
        /// String[] itemnames= {
        /// "c:\\fixedContent\\Readme.txt",
        /// "MyProposal.docx",
        /// "c:\\SupportFiles",  // a directory
        /// "images\\Image1.jpg"
        /// };
        /// 
        /// try
        /// {
        /// using (ZipFile zip = new ZipFile())
        /// {
        /// for (int i = 1; i &lt; itemnames.Length; i++)
        /// {
        /// // will add Files or Dirs, recurses and flattens subdirectories
        /// zip.AddItem(itemnames[i],"flat");
        /// }
        /// zip.Save(ZipToCreate);
        /// }
        /// }
        /// catch (System.Exception ex1)
        /// {
        /// System.Console.Error.WriteLine("exception: {0}", ex1);
        /// }
        /// </code>
        /// 
        /// <code lang="VB">
        /// Dim itemnames As String() = _
        /// New String() { "c:\fixedContent\Readme.txt", _
        /// "MyProposal.docx", _
        /// "SupportFiles", _
        /// "images\Image1.jpg" }
        /// Try
        /// Using zip As New ZipFile
        /// Dim i As Integer
        /// For i = 1 To itemnames.Length - 1
        /// ' will add Files or Dirs, recursing and flattening subdirectories.
        /// zip.AddItem(itemnames(i), "flat")
        /// Next i
        /// zip.Save(ZipToCreate)
        /// End Using
        /// Catch ex1 As Exception
        /// Console.Error.WriteLine("exception: {0}", ex1.ToString())
        /// End Try
        /// </code>
        /// </example>
        /// <returns>The <c>ZipEntry</c> added.</returns>
        public ZipEntry AddItem(string fileOrDirectoryName, string directoryPathInArchive)
        {
            if (File.Exists(fileOrDirectoryName))
            {
                return this.AddFile(fileOrDirectoryName, directoryPathInArchive);
            }
            if (!Directory.Exists(fileOrDirectoryName))
            {
                throw new FileNotFoundException(string.Format(CultureInfo.InvariantCulture, "That file or directory ({0}) does not exist!", fileOrDirectoryName));
            }
            return this.AddDirectory(fileOrDirectoryName, directoryPathInArchive);
        }

        private ZipEntry AddOrUpdateDirectoryImpl(string directoryName, string rootDirectoryPathInArchive, AddOrUpdateAction action)
        {
            if (rootDirectoryPathInArchive == null)
            {
                rootDirectoryPathInArchive = "";
            }
            return this.AddOrUpdateDirectoryImpl(directoryName, rootDirectoryPathInArchive, action, true, 0);
        }

        private ZipEntry AddOrUpdateDirectoryImpl(string directoryName, string rootDirectoryPathInArchive, AddOrUpdateAction action, bool recurse, int level)
        {
            if (this.Verbose)
            {
                this.StatusMessageTextWriter.WriteLine("{0} {1}...", (action == AddOrUpdateAction.AddOnly) ? "adding" : "Adding or updating", directoryName);
            }
            if (level == 0)
            {
                this._addOperationCanceled = false;
                this.OnAddStarted();
            }
            if (this._addOperationCanceled)
            {
                return null;
            }
            string fileName = rootDirectoryPathInArchive;
            ZipEntry entry = null;
            if (level > 0)
            {
                int length = directoryName.Length;
                for (int i = level; i > 0; i--)
                {
                    length = directoryName.LastIndexOfAny(@"/\".ToCharArray(), length - 1, length - 1);
                }
                fileName = directoryName.Substring(length + 1);
                fileName = Path.Combine(rootDirectoryPathInArchive, fileName);
            }
            if ((level > 0) || (rootDirectoryPathInArchive != ""))
            {
                entry = ZipEntry.CreateFromFile(directoryName, fileName);
                entry._container = new ZipContainer(this);
                entry.AlternateEncoding = this.AlternateEncoding;
                entry.AlternateEncodingUsage = this.AlternateEncodingUsage;
                entry.MarkAsDirectory();
                entry.EmitTimesInWindowsFormatWhenSaving = this._emitNtfsTimes;
                entry.EmitTimesInUnixFormatWhenSaving = this._emitUnixTimes;
                if (!this._entries.ContainsKey(entry.FileName))
                {
                    this.InternalAddEntry(entry.FileName, entry);
                    this.AfterAddEntry(entry);
                }
                fileName = entry.FileName;
            }
            if (!this._addOperationCanceled)
            {
                string[] files = Directory.GetFiles(directoryName);
                if (recurse)
                {
                    foreach (string str2 in files)
                    {
                        if (this._addOperationCanceled)
                        {
                            break;
                        }
                        if (action == AddOrUpdateAction.AddOnly)
                        {
                            this.AddFile(str2, fileName);
                        }
                        else
                        {
                            this.UpdateFile(str2, fileName);
                        }
                    }
                    if (!this._addOperationCanceled)
                    {
                        string[] directories = Directory.GetDirectories(directoryName);
                        foreach (string str3 in directories)
                        {
                            FileAttributes attributes = File.GetAttributes(str3);
                            if (this.AddDirectoryWillTraverseReparsePoints || ((attributes & FileAttributes.ReparsePoint) == 0))
                            {
                                this.AddOrUpdateDirectoryImpl(str3, rootDirectoryPathInArchive, action, recurse, level + 1);
                            }
                        }
                    }
                }
            }
            if (level == 0)
            {
                this.OnAddCompleted();
            }
            return entry;
        }

        /// <summary>
        /// Adds to the ZipFile a set of files from the current working directory on
        /// disk, that conform to the specified criteria.
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// This method selects files from the the current working directory matching
        /// the specified criteria, and adds them to the ZipFile.
        /// </para>
        /// 
        /// <para>
        /// Specify the criteria in statements of 3 elements: a noun, an operator, and
        /// a value.  Consider the string "name != *.doc" .  The noun is "name".  The
        /// operator is "!=", implying "Not Equal".  The value is "*.doc".  That
        /// criterion, in English, says "all files with a name that does not end in
        /// the .doc extension."
        /// </para>
        /// 
        /// <para>
        /// Supported nouns include "name" (or "filename") for the filename; "atime",
        /// "mtime", and "ctime" for last access time, last modfied time, and created
        /// time of the file, respectively; "attributes" (or "attrs") for the file
        /// attributes; "size" (or "length") for the file length (uncompressed), and
        /// "type" for the type of object, either a file or a directory.  The
        /// "attributes", "name" and "type" nouns both support = and != as operators.
        /// The "size", "atime", "mtime", and "ctime" nouns support = and !=, and
        /// &gt;, &gt;=, &lt;, &lt;= as well. The times are taken to be expressed in
        /// local time.
        /// </para>
        /// 
        /// <para>
        /// Specify values for the file attributes as a string with one or more of the
        /// characters H,R,S,A,I,L in any order, implying file attributes of Hidden,
        /// ReadOnly, System, Archive, NotContextIndexed, and ReparsePoint (symbolic
        /// link) respectively.
        /// </para>
        /// 
        /// <para>
        /// To specify a time, use YYYY-MM-DD-HH:mm:ss or YYYY/MM/DD-HH:mm:ss as the
        /// format.  If you omit the HH:mm:ss portion, it is assumed to be 00:00:00
        /// (midnight).
        /// </para>
        /// 
        /// <para>
        /// The value for a size criterion is expressed in integer quantities of bytes,
        /// kilobytes (use k or kb after the number), megabytes (m or mb), or gigabytes
        /// (g or gb).
        /// </para>
        /// 
        /// <para>
        /// The value for a name is a pattern to match against the filename, potentially
        /// including wildcards.  The pattern follows CMD.exe glob rules: * implies one
        /// or more of any character, while ?  implies one character.  If the name
        /// pattern contains any slashes, it is matched to the entire filename,
        /// including the path; otherwise, it is matched against only the filename
        /// without the path.  This means a pattern of "*\*.*" matches all files one
        /// directory level deep, while a pattern of "*.*" matches all files in all
        /// directories.
        /// </para>
        /// 
        /// <para>
        /// To specify a name pattern that includes spaces, use single quotes around the
        /// pattern.  A pattern of "'* *.*'" will match all files that have spaces in
        /// the filename.  The full criteria string for that would be "name = '* *.*'" .
        /// </para>
        /// 
        /// <para>
        /// The value for a type criterion is either F (implying a file) or D (implying
        /// a directory).
        /// </para>
        /// 
        /// <para>
        /// Some examples:
        /// </para>
        /// 
        /// <list type="table">
        /// <listheader>
        /// <term>criteria</term>
        /// <description>Files retrieved</description>
        /// </listheader>
        /// 
        /// <item>
        /// <term>name != *.xls </term>
        /// <description>any file with an extension that is not .xls
        /// </description>
        /// </item>
        /// 
        /// <item>
        /// <term>name = *.mp3 </term>
        /// <description>any file with a .mp3 extension.
        /// </description>
        /// </item>
        /// 
        /// <item>
        /// <term>*.mp3</term>
        /// <description>(same as above) any file with a .mp3 extension.
        /// </description>
        /// </item>
        /// 
        /// <item>
        /// <term>attributes = A </term>
        /// <description>all files whose attributes include the Archive bit.
        /// </description>
        /// </item>
        /// 
        /// <item>
        /// <term>attributes != H </term>
        /// <description>all files whose attributes do not include the Hidden bit.
        /// </description>
        /// </item>
        /// 
        /// <item>
        /// <term>mtime &gt; 2009-01-01</term>
        /// <description>all files with a last modified time after January 1st, 2009.
        /// </description>
        /// </item>
        /// 
        /// <item>
        /// <term>size &gt; 2gb</term>
        /// <description>all files whose uncompressed size is greater than 2gb.
        /// </description>
        /// </item>
        /// 
        /// <item>
        /// <term>type = D</term>
        /// <description>all directories in the filesystem. </description>
        /// </item>
        /// 
        /// </list>
        /// 
        /// <para>
        /// You can combine criteria with the conjunctions AND or OR. Using a string
        /// like "name = *.txt AND size &gt;= 100k" for the selectionCriteria retrieves
        /// entries whose names end in .txt, and whose uncompressed size is greater than
        /// or equal to 100 kilobytes.
        /// </para>
        /// 
        /// <para>
        /// For more complex combinations of criteria, you can use parenthesis to group
        /// clauses in the boolean logic.  Without parenthesis, the precedence of the
        /// criterion atoms is determined by order of appearance.  Unlike the C#
        /// language, the AND conjunction does not take precendence over the logical OR.
        /// This is important only in strings that contain 3 or more criterion atoms.
        /// In other words, "name = *.txt and size &gt; 1000 or attributes = H" implies
        /// "((name = *.txt AND size &gt; 1000) OR attributes = H)" while "attributes =
        /// H OR name = *.txt and size &gt; 1000" evaluates to "((attributes = H OR name
        /// = *.txt) AND size &gt; 1000)".  When in doubt, use parenthesis.
        /// </para>
        /// 
        /// <para>
        /// Using time properties requires some extra care. If you want to retrieve all
        /// entries that were last updated on 2009 February 14, specify a time range
        /// like so:"mtime &gt;= 2009-02-14 AND mtime &lt; 2009-02-15".  Read this to
        /// say: all files updated after 12:00am on February 14th, until 12:00am on
        /// February 15th.  You can use the same bracketing approach to specify any time
        /// period - a year, a month, a week, and so on.
        /// </para>
        /// 
        /// <para>
        /// The syntax allows one special case: if you provide a string with no spaces, it is
        /// treated as a pattern to match for the filename.  Therefore a string like "*.xls"
        /// will be equivalent to specifying "name = *.xls".
        /// </para>
        /// 
        /// <para>
        /// There is no logic in this method that insures that the file inclusion
        /// criteria are internally consistent.  For example, it's possible to specify
        /// criteria that says the file must have a size of less than 100 bytes, as well
        /// as a size that is greater than 1000 bytes. Obviously no file will ever
        /// satisfy such criteria, but this method does not detect such logical
        /// inconsistencies. The caller is responsible for insuring the criteria are
        /// sensible.
        /// </para>
        /// 
        /// <para>
        /// Using this method, the file selection does not recurse into
        /// subdirectories, and the full path of the selected files is included in the
        /// entries added into the zip archive.  If you don't like these behaviors,
        /// see the other overloads of this method.
        /// </para>
        /// </remarks>
        /// 
        /// <example>
        /// This example zips up all *.csv files in the current working directory.
        /// <code>
        /// using (ZipFile zip = new ZipFile())
        /// {
        /// // To just match on filename wildcards,
        /// // use the shorthand form of the selectionCriteria string.
        /// zip.AddSelectedFiles("*.csv");
        /// zip.Save(PathToZipArchive);
        /// }
        /// </code>
        /// <code lang="VB">
        /// Using zip As ZipFile = New ZipFile()
        /// zip.AddSelectedFiles("*.csv")
        /// zip.Save(PathToZipArchive)
        /// End Using
        /// </code>
        /// </example>
        /// 
        /// <param name="selectionCriteria">The criteria for file selection</param>
        public void AddSelectedFiles(string selectionCriteria)
        {
            this.AddSelectedFiles(selectionCriteria, ".", null, false);
        }

        /// <summary>
        /// Adds to the ZipFile a set of files from the disk that conform to the
        /// specified criteria, optionally recursing into subdirectories.
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// This method selects files from the the current working directory matching
        /// the specified criteria, and adds them to the ZipFile.  If
        /// <c>recurseDirectories</c> is true, files are also selected from
        /// subdirectories, and the directory structure in the filesystem is
        /// reproduced in the zip archive, rooted at the current working directory.
        /// </para>
        /// 
        /// <para>
        /// Using this method, the full path of the selected files is included in the
        /// entries added into the zip archive.  If you don't want this behavior, use
        /// one of the overloads of this method that allows the specification of a
        /// <c>directoryInArchive</c>.
        /// </para>
        /// 
        /// <para>
        /// For details on the syntax for the selectionCriteria parameter, see <see cref="M:DotNetZipAdditionalPlatforms.Zip.ZipFile.AddSelectedFiles(System.String)" />.
        /// </para>
        /// 
        /// </remarks>
        /// 
        /// <example>
        /// 
        /// This example zips up all *.xml files in the current working directory, or any
        /// subdirectory, that are larger than 1mb.
        /// 
        /// <code>
        /// using (ZipFile zip = new ZipFile())
        /// {
        /// // Use a compound expression in the selectionCriteria string.
        /// zip.AddSelectedFiles("name = *.xml  and  size &gt; 1024kb", true);
        /// zip.Save(PathToZipArchive);
        /// }
        /// </code>
        /// <code lang="VB">
        /// Using zip As ZipFile = New ZipFile()
        /// ' Use a compound expression in the selectionCriteria string.
        /// zip.AddSelectedFiles("name = *.xml  and  size &gt; 1024kb", true)
        /// zip.Save(PathToZipArchive)
        /// End Using
        /// </code>
        /// </example>
        /// 
        /// <param name="selectionCriteria">The criteria for file selection</param>
        /// 
        /// <param name="recurseDirectories">
        /// If true, the file selection will recurse into subdirectories.
        /// </param>
        public void AddSelectedFiles(string selectionCriteria, bool recurseDirectories)
        {
            this.AddSelectedFiles(selectionCriteria, ".", null, recurseDirectories);
        }

        /// <summary>
        /// Adds to the ZipFile a set of files from a specified directory in the
        /// filesystem, that conform to the specified criteria.
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// This method selects files that conform to the specified criteria, from the
        /// the specified directory on disk, and adds them to the ZipFile.  The search
        /// does not recurse into subdirectores.
        /// </para>
        /// 
        /// <para>
        /// Using this method, the full filesystem path of the files on disk is
        /// reproduced on the entries added to the zip file.  If you don't want this
        /// behavior, use one of the other overloads of this method.
        /// </para>
        /// 
        /// <para>
        /// For details on the syntax for the selectionCriteria parameter, see <see cref="M:DotNetZipAdditionalPlatforms.Zip.ZipFile.AddSelectedFiles(System.String)" />.
        /// </para>
        /// 
        /// </remarks>
        /// 
        /// <example>
        /// 
        /// This example zips up all *.xml files larger than 1mb in the directory
        /// given by "d:\rawdata".
        /// 
        /// <code>
        /// using (ZipFile zip = new ZipFile())
        /// {
        /// // Use a compound expression in the selectionCriteria string.
        /// zip.AddSelectedFiles("name = *.xml  and  size &gt; 1024kb", "d:\\rawdata");
        /// zip.Save(PathToZipArchive);
        /// }
        /// </code>
        /// 
        /// <code lang="VB">
        /// Using zip As ZipFile = New ZipFile()
        /// ' Use a compound expression in the selectionCriteria string.
        /// zip.AddSelectedFiles("name = *.xml  and  size &gt; 1024kb", "d:\rawdata)
        /// zip.Save(PathToZipArchive)
        /// End Using
        /// </code>
        /// </example>
        /// 
        /// <param name="selectionCriteria">The criteria for file selection</param>
        /// 
        /// <param name="directoryOnDisk">
        /// The name of the directory on the disk from which to select files.
        /// </param>
        public void AddSelectedFiles(string selectionCriteria, string directoryOnDisk)
        {
            this.AddSelectedFiles(selectionCriteria, directoryOnDisk, null, false);
        }

        /// <summary>
        /// Adds to the ZipFile a set of files from the specified directory on disk,
        /// that conform to the specified criteria.
        /// </summary>
        /// 
        /// <remarks>
        /// 
        /// <para>
        /// This method selects files from the the specified disk directory matching
        /// the specified selection criteria, and adds them to the ZipFile.  If
        /// <c>recurseDirectories</c> is true, files are also selected from
        /// subdirectories.
        /// </para>
        /// 
        /// <para>
        /// The full directory structure in the filesystem is reproduced on the
        /// entries added to the zip archive.  If you don't want this behavior, use
        /// one of the overloads of this method that allows the specification of a
        /// <c>directoryInArchive</c>.
        /// </para>
        /// 
        /// <para>
        /// For details on the syntax for the selectionCriteria parameter, see <see cref="M:DotNetZipAdditionalPlatforms.Zip.ZipFile.AddSelectedFiles(System.String)" />.
        /// </para>
        /// </remarks>
        /// 
        /// <example>
        /// 
        /// This example zips up all *.csv files in the "files" directory, or any
        /// subdirectory, that have been saved since 2009 February 14th.
        /// 
        /// <code>
        /// using (ZipFile zip = new ZipFile())
        /// {
        /// // Use a compound expression in the selectionCriteria string.
        /// zip.AddSelectedFiles("name = *.csv  and  mtime &gt; 2009-02-14", "files", true);
        /// zip.Save(PathToZipArchive);
        /// }
        /// </code>
        /// <code lang="VB">
        /// Using zip As ZipFile = New ZipFile()
        /// ' Use a compound expression in the selectionCriteria string.
        /// zip.AddSelectedFiles("name = *.csv  and  mtime &gt; 2009-02-14", "files", true)
        /// zip.Save(PathToZipArchive)
        /// End Using
        /// </code>
        /// </example>
        /// 
        /// <example>
        /// This example zips up all files in the current working
        /// directory, and all its child directories, except those in
        /// the <c>excludethis</c> subdirectory.
        /// <code lang="VB">
        /// Using Zip As ZipFile = New ZipFile(zipfile)
        /// Zip.AddSelectedFfiles("name != 'excludethis\*.*'", datapath, True)
        /// Zip.Save()
        /// End Using
        /// </code>
        /// </example>
        /// 
        /// <param name="selectionCriteria">The criteria for file selection</param>
        /// 
        /// <param name="directoryOnDisk">
        /// The filesystem path from which to select files.
        /// </param>
        /// 
        /// <param name="recurseDirectories">
        /// If true, the file selection will recurse into subdirectories.
        /// </param>
        public void AddSelectedFiles(string selectionCriteria, string directoryOnDisk, bool recurseDirectories)
        {
            this.AddSelectedFiles(selectionCriteria, directoryOnDisk, null, recurseDirectories);
        }

        /// <summary>
        /// Adds to the ZipFile a selection of files from the specified directory on
        /// disk, that conform to the specified criteria, and using a specified root
        /// path for entries added to the zip archive.
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// This method selects files from the specified disk directory matching the
        /// specified selection criteria, and adds those files to the ZipFile, using
        /// the specified directory path in the archive.  The search does not recurse
        /// into subdirectories.  For details on the syntax for the selectionCriteria
        /// parameter, see <see cref="M:DotNetZipAdditionalPlatforms.Zip.ZipFile.AddSelectedFiles(System.String)" />.
        /// </para>
        /// 
        /// </remarks>
        /// 
        /// <example>
        /// 
        /// This example zips up all *.psd files in the "photos" directory that have
        /// been saved since 2009 February 14th, and puts them all in a zip file,
        /// using the directory name of "content" in the zip archive itself. When the
        /// zip archive is unzipped, the folder containing the .psd files will be
        /// named "content".
        /// 
        /// <code>
        /// using (ZipFile zip = new ZipFile())
        /// {
        /// // Use a compound expression in the selectionCriteria string.
        /// zip.AddSelectedFiles("name = *.psd  and  mtime &gt; 2009-02-14", "photos", "content");
        /// zip.Save(PathToZipArchive);
        /// }
        /// </code>
        /// <code lang="VB">
        /// Using zip As ZipFile = New ZipFile
        /// zip.AddSelectedFiles("name = *.psd  and  mtime &gt; 2009-02-14", "photos", "content")
        /// zip.Save(PathToZipArchive)
        /// End Using
        /// </code>
        /// </example>
        /// 
        /// <param name="selectionCriteria">
        /// The criteria for selection of files to add to the <c>ZipFile</c>.
        /// </param>
        /// 
        /// <param name="directoryOnDisk">
        /// The path to the directory in the filesystem from which to select files.
        /// </param>
        /// 
        /// <param name="directoryPathInArchive">
        /// Specifies a directory path to use to in place of the
        /// <c>directoryOnDisk</c>.  This path may, or may not, correspond to a real
        /// directory in the current filesystem.  If the files within the zip are
        /// later extracted, this is the path used for the extracted file.  Passing
        /// null (nothing in VB) will use the path on the file name, if any; in other
        /// words it would use <c>directoryOnDisk</c>, plus any subdirectory.  Passing
        /// the empty string ("") will insert the item at the root path within the
        /// archive.
        /// </param>
        public void AddSelectedFiles(string selectionCriteria, string directoryOnDisk, string directoryPathInArchive)
        {
            this.AddSelectedFiles(selectionCriteria, directoryOnDisk, directoryPathInArchive, false);
        }

        /// <summary>
        /// Adds to the ZipFile a selection of files from the specified directory on
        /// disk, that conform to the specified criteria, optionally recursing through
        /// subdirectories, and using a specified root path for entries added to the
        /// zip archive.
        /// </summary>
        /// 
        /// <remarks>
        /// This method selects files from the specified disk directory that match the
        /// specified selection criteria, and adds those files to the ZipFile, using
        /// the specified directory path in the archive. If <c>recurseDirectories</c>
        /// is true, files are also selected from subdirectories, and the directory
        /// structure in the filesystem is reproduced in the zip archive, rooted at
        /// the directory specified by <c>directoryOnDisk</c>.  For details on the
        /// syntax for the selectionCriteria parameter, see <see cref="M:DotNetZipAdditionalPlatforms.Zip.ZipFile.AddSelectedFiles(System.String)" />.
        /// </remarks>
        /// 
        /// <example>
        /// 
        /// This example zips up all files that are NOT *.pst files, in the current
        /// working directory and any subdirectories.
        /// 
        /// <code>
        /// using (ZipFile zip = new ZipFile())
        /// {
        /// zip.AddSelectedFiles("name != *.pst", SourceDirectory, "backup", true);
        /// zip.Save(PathToZipArchive);
        /// }
        /// </code>
        /// <code lang="VB">
        /// Using zip As ZipFile = New ZipFile
        /// zip.AddSelectedFiles("name != *.pst", SourceDirectory, "backup", true)
        /// zip.Save(PathToZipArchive)
        /// End Using
        /// </code>
        /// </example>
        /// 
        /// <param name="selectionCriteria">
        /// The criteria for selection of files to add to the <c>ZipFile</c>.
        /// </param>
        /// 
        /// <param name="directoryOnDisk">
        /// The path to the directory in the filesystem from which to select files.
        /// </param>
        /// 
        /// <param name="directoryPathInArchive">
        /// Specifies a directory path to use to in place of the
        /// <c>directoryOnDisk</c>.  This path may, or may not, correspond to a real
        /// directory in the current filesystem.  If the files within the zip are
        /// later extracted, this is the path used for the extracted file.  Passing
        /// null (nothing in VB) will use the path on the file name, if any; in other
        /// words it would use <c>directoryOnDisk</c>, plus any subdirectory.  Passing
        /// the empty string ("") will insert the item at the root path within the
        /// archive.
        /// </param>
        /// 
        /// <param name="recurseDirectories">
        /// If true, the method also scans subdirectories for files matching the
        /// criteria.
        /// </param>
        public void AddSelectedFiles(string selectionCriteria, string directoryOnDisk, string directoryPathInArchive, bool recurseDirectories)
        {
            this._AddOrUpdateSelectedFiles(selectionCriteria, directoryOnDisk, directoryPathInArchive, recurseDirectories, false);
        }

        internal void AfterAddEntry(ZipEntry entry)
        {
            EventHandler<AddProgressEventArgs> addProgress = this.AddProgress;
            if (addProgress != null)
            {
                AddProgressEventArgs e = AddProgressEventArgs.AfterEntry(this.ArchiveNameForEvent, entry, this._entries.Count);
                addProgress(this, e);
                if (e.Cancel)
                {
                    this._addOperationCanceled = true;
                }
            }
        }

        /// <summary>
        /// Checks a zip file to see if its directory is consistent.
        /// </summary>
        /// 
        /// <remarks>
        /// 
        /// <para>
        /// In cases of data error, the directory within a zip file can get out
        /// of synch with the entries in the zip file.  This method checks the
        /// given zip file and returns true if this has occurred.
        /// </para>
        /// 
        /// <para> This method may take a long time to run for large zip files.  </para>
        /// 
        /// <para>
        /// This method is not supported in the Reduced or Compact Framework
        /// versions of DotNetZip.
        /// </para>
        /// 
        /// <para>
        /// Developers using COM can use the <see cref="M:DotNetZipAdditionalPlatforms.Zip.ComHelper.CheckZip(System.String)">ComHelper.CheckZip(String)</see>
        /// method.
        /// </para>
        /// 
        /// </remarks>
        /// 
        /// <param name="zipFileName">The filename to of the zip file to check.</param>
        /// 
        /// <returns>true if the named zip file checks OK. Otherwise, false. </returns>
        /// 
        /// <seealso cref="M:DotNetZipAdditionalPlatforms.Zip.ZipFile.FixZipDirectory(System.String)" />
        /// <seealso cref="M:DotNetZipAdditionalPlatforms.Zip.ZipFile.CheckZip(System.String,System.Boolean,System.IO.TextWriter)" />
        public static bool CheckZip(string zipFileName)
        {
            return CheckZip(zipFileName, false, null);
        }

        /// <summary>
        /// Checks a zip file to see if its directory is consistent,
        /// and optionally fixes the directory if necessary.
        /// </summary>
        /// 
        /// <remarks>
        /// 
        /// <para>
        /// In cases of data error, the directory within a zip file can get out of
        /// synch with the entries in the zip file.  This method checks the given
        /// zip file, and returns true if this has occurred. It also optionally
        /// fixes the zipfile, saving the fixed copy in <em>Name</em>_Fixed.zip.
        /// </para>
        /// 
        /// <para>
        /// This method may take a long time to run for large zip files.  It
        /// will take even longer if the file actually needs to be fixed, and if
        /// <c>fixIfNecessary</c> is true.
        /// </para>
        /// 
        /// <para>
        /// This method is not supported in the Reduced or Compact
        /// Framework versions of DotNetZip.
        /// </para>
        /// 
        /// </remarks>
        /// 
        /// <param name="zipFileName">The filename to of the zip file to check.</param>
        /// 
        /// <param name="fixIfNecessary">If true, the method will fix the zip file if
        /// necessary.</param>
        /// 
        /// <param name="writer">
        /// a TextWriter in which messages generated while checking will be written.
        /// </param>
        /// 
        /// <returns>true if the named zip is OK; false if the file needs to be fixed.</returns>
        /// 
        /// <seealso cref="M:DotNetZipAdditionalPlatforms.Zip.ZipFile.CheckZip(System.String)" />
        /// <seealso cref="M:DotNetZipAdditionalPlatforms.Zip.ZipFile.FixZipDirectory(System.String)" />
        public static bool CheckZip(string zipFileName, bool fixIfNecessary, TextWriter writer)
        {
            ZipFile file = null;
            ZipFile file2 = null;
            bool flag = true;
            try
            {
                file = new ZipFile();
                file.FullScan = true;
                file.Initialize(zipFileName);
                file2 = Read(zipFileName);
                foreach (ZipEntry entry in file)
                {
                    foreach (ZipEntry entry2 in file2)
                    {
                        if (entry.FileName == entry2.FileName)
                        {
                            if (entry._RelativeOffsetOfLocalHeader != entry2._RelativeOffsetOfLocalHeader)
                            {
                                flag = false;
                                if (writer != null)
                                {
                                    writer.WriteLine("{0}: mismatch in RelativeOffsetOfLocalHeader  (0x{1:X16} != 0x{2:X16})", entry.FileName, entry._RelativeOffsetOfLocalHeader, entry2._RelativeOffsetOfLocalHeader);
                                }
                            }
                            if (entry._CompressedSize != entry2._CompressedSize)
                            {
                                flag = false;
                                if (writer != null)
                                {
                                    writer.WriteLine("{0}: mismatch in CompressedSize  (0x{1:X16} != 0x{2:X16})", entry.FileName, entry._CompressedSize, entry2._CompressedSize);
                                }
                            }
                            if (entry._UncompressedSize != entry2._UncompressedSize)
                            {
                                flag = false;
                                if (writer != null)
                                {
                                    writer.WriteLine("{0}: mismatch in UncompressedSize  (0x{1:X16} != 0x{2:X16})", entry.FileName, entry._UncompressedSize, entry2._UncompressedSize);
                                }
                            }
                            if (entry.CompressionMethod != entry2.CompressionMethod)
                            {
                                flag = false;
                                if (writer != null)
                                {
                                    writer.WriteLine("{0}: mismatch in CompressionMethod  (0x{1:X4} != 0x{2:X4})", entry.FileName, entry.CompressionMethod, entry2.CompressionMethod);
                                }
                            }
                            if (entry.Crc != entry2.Crc)
                            {
                                flag = false;
                                if (writer != null)
                                {
                                    writer.WriteLine("{0}: mismatch in Crc32  (0x{1:X4} != 0x{2:X4})", entry.FileName, entry.Crc, entry2.Crc);
                                }
                            }
                            break;
                        }
                    }
                }
                file2.Dispose();
                file2 = null;
                if (!(flag || !fixIfNecessary))
                {
                    string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(zipFileName);
                    fileNameWithoutExtension = string.Format(CultureInfo.InvariantCulture, "{0}_fixed.zip", fileNameWithoutExtension);
                    file.Save(fileNameWithoutExtension);
                }
            }
            finally
            {
                if (file != null)
                {
                    file.Dispose();
                }
                if (file2 != null)
                {
                    file2.Dispose();
                }
            }
            return flag;
        }

        /// <summary>
        /// Verify the password on a zip file.
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// Keep in mind that passwords in zipfiles are applied to
        /// zip entries, not to the entire zip file. So testing a
        /// zipfile for a particular password doesn't work in the
        /// general case. On the other hand, it's often the case
        /// that a single password will be used on all entries in a
        /// zip file. This method works for that case.
        /// </para>
        /// <para>
        /// There is no way to check a password without doing the
        /// decryption. So this code decrypts and extracts the given
        /// zipfile into <see cref="F:System.IO.Stream.Null" />
        /// </para>
        /// </remarks>
        /// 
        /// <param name="zipFileName">The filename to of the zip file to fix.</param>
        /// 
        /// <param name="password">The password to check.</param>
        /// 
        /// <returns>a bool indicating whether the password matches.</returns>
        public static bool CheckZipPassword(string zipFileName, string password)
        {
            bool flag = false;
            try
            {
                using (ZipFile file = Read(zipFileName))
                {
                    foreach (ZipEntry entry in file)
                    {
                        if (!(entry.IsDirectory || !entry.UsesEncryption))
                        {
                            entry.ExtractWithPassword(Stream.Null, password);
                        }
                    }
                }
                flag = true;
            }
            catch (BadPasswordException)
            {
            }
            return flag;
        }

        private void CleanupAfterSaveOperation()
        {
            if (this._name != null)
            {
                if (this._writestream != null)
                {
                    try
                    {
                        this._writestream.Dispose();
                    }
                    catch (IOException)
                    {
                    }
                }
                this._writestream = null;
                if (this._temporaryFileName != null)
                {
                    this.RemoveTempFile();
                    this._temporaryFileName = null;
                }
            }
        }

        /// <summary>
        /// Returns true if an entry by the given name exists in the ZipFile.
        /// </summary>
        /// 
        /// <param name="name">the name of the entry to find</param>
        /// <returns>true if an entry with the given name exists; otherwise false.
        /// </returns>
        public bool ContainsEntry(string name)
        {
            return this._entries.ContainsKey(SharedUtilities.NormalizePathForUseInZipFile(name));
        }

        /// <summary>
        /// Delete file with retry on UnauthorizedAccessException.
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// When calling File.Delete() on a file that has been "recently"
        /// created, the call sometimes fails with
        /// UnauthorizedAccessException. This method simply retries the Delete 3
        /// times with a sleep between tries.
        /// </para>
        /// </remarks>
        /// 
        /// <param name="filename">the name of the file to be deleted</param>
        private void DeleteFileWithRetry(string filename)
        {
            bool flag = false;
            int num = 3;
            for (int i = 0; (i < num) && !flag; i++)
            {
                try
                {
                    File.Delete(filename);
                    flag = true;
                }
                catch (UnauthorizedAccessException)
                {
                    Console.WriteLine("************************************************** Retry delete.");
                    Thread.Sleep((int) (200 + (i * 200)));
                }
            }
        }

        /// <summary>
        /// Closes the read and write streams associated
        /// to the <c>ZipFile</c>, if necessary.
        /// </summary>
        /// 
        /// <remarks>
        /// The Dispose() method is generally employed implicitly, via a <c>using(..) {..}</c>
        /// statement. (<c>Using...End Using</c> in VB) If you do not employ a using
        /// statement, insure that your application calls Dispose() explicitly.  For
        /// example, in a Powershell application, or an application that uses the COM
        /// interop interface, you must call Dispose() explicitly.
        /// </remarks>
        /// 
        /// <example>
        /// This example extracts an entry selected by name, from the Zip file to the
        /// Console.
        /// <code>
        /// using (ZipFile zip = ZipFile.Read(zipfile))
        /// {
        /// foreach (ZipEntry e in zip)
        /// {
        /// if (WantThisEntry(e.FileName))
        /// zip.Extract(e.FileName, Console.OpenStandardOutput());
        /// }
        /// } // Dispose() is called implicitly here.
        /// </code>
        /// 
        /// <code lang="VB">
        /// Using zip As ZipFile = ZipFile.Read(zipfile)
        /// Dim e As ZipEntry
        /// For Each e In zip
        /// If WantThisEntry(e.FileName) Then
        /// zip.Extract(e.FileName, Console.OpenStandardOutput())
        /// End If
        /// Next
        /// End Using ' Dispose is implicity called here
        /// </code>
        /// </example>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes any managed resources, if the flag is set, then marks the
        /// instance disposed.  This method is typically not called explicitly from
        /// application code.
        /// </summary>
        /// 
        /// <remarks>
        /// Applications should call <see cref="M:DotNetZipAdditionalPlatforms.Zip.ZipFile.Dispose">the no-arg Dispose method</see>.
        /// </remarks>
        /// 
        /// <param name="disposeManagedResources">
        /// indicates whether the method should dispose streams or not.
        /// </param>
        protected virtual void Dispose(bool disposeManagedResources)
        {
            if (!this._disposed)
            {
                if (disposeManagedResources)
                {
                    if (this._ReadStreamIsOurs && (this._readstream != null))
                    {
                        this._readstream.Dispose();
                        this._readstream = null;
                    }
                    if (((this._temporaryFileName != null) && (this._name != null)) && (this._writestream != null))
                    {
                        this._writestream.Dispose();
                        this._writestream = null;
                    }
                    if (this.ParallelDeflater != null)
                    {
                        this.ParallelDeflater.Dispose();
                        this.ParallelDeflater = null;
                    }
                }
                this._disposed = true;
            }
        }

        private string EnsureendInSlash(string s)
        {
            if (s.EndsWith(@"\"))
            {
                return s;
            }
            return (s + @"\");
        }

        /// <summary>
        /// Extracts all of the items in the zip archive, to the specified path in the
        /// filesystem.  The path can be relative or fully-qualified.
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// This method will extract all entries in the <c>ZipFile</c> to the
        /// specified path.
        /// </para>
        /// 
        /// <para>
        /// If an extraction of a file from the zip archive would overwrite an
        /// existing file in the filesystem, the action taken is dictated by the
        /// ExtractExistingFile property, which overrides any setting you may have
        /// made on individual ZipEntry instances.  By default, if you have not
        /// set that property on the <c>ZipFile</c> instance, the entry will not
        /// be extracted, the existing file will not be overwritten and an
        /// exception will be thrown. To change this, set the property, or use the
        /// <see cref="M:DotNetZipAdditionalPlatforms.Zip.ZipFile.ExtractAll(System.String,DotNetZipAdditionalPlatforms.Zip.ExtractExistingFileAction)" /> overload that allows you to
        /// specify an ExtractExistingFileAction parameter.
        /// </para>
        /// 
        /// <para>
        /// The action to take when an extract would overwrite an existing file
        /// applies to all entries.  If you want to set this on a per-entry basis,
        /// then you must use one of the <see cref="M:DotNetZipAdditionalPlatforms.Zip.ZipEntry.Extract">ZipEntry.Extract</see> methods.
        /// </para>
        /// 
        /// <para>
        /// This method will send verbose output messages to the <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.StatusMessageTextWriter" />, if it is set on the <c>ZipFile</c>
        /// instance.
        /// </para>
        /// 
        /// <para>
        /// You may wish to take advantage of the <c>ExtractProgress</c> event.
        /// </para>
        /// 
        /// <para>
        /// About timestamps: When extracting a file entry from a zip archive, the
        /// extracted file gets the last modified time of the entry as stored in
        /// the archive. The archive may also store extended file timestamp
        /// information, including last accessed and created times. If these are
        /// present in the <c>ZipEntry</c>, then the extracted file will also get
        /// these times.
        /// </para>
        /// 
        /// <para>
        /// A Directory entry is somewhat different. It will get the times as
        /// described for a file entry, but, if there are file entries in the zip
        /// archive that, when extracted, appear in the just-created directory,
        /// then when those file entries are extracted, the last modified and last
        /// accessed times of the directory will change, as a side effect.  The
        /// result is that after an extraction of a directory and a number of
        /// files within the directory, the last modified and last accessed
        /// timestamps on the directory will reflect the time that the last file
        /// was extracted into the directory, rather than the time stored in the
        /// zip archive for the directory.
        /// </para>
        /// 
        /// <para>
        /// To compensate, when extracting an archive with <c>ExtractAll</c>,
        /// DotNetZip will extract all the file and directory entries as described
        /// above, but it will then make a second pass on the directories, and
        /// reset the times on the directories to reflect what is stored in the
        /// zip archive.
        /// </para>
        /// 
        /// <para>
        /// This compensation is performed only within the context of an
        /// <c>ExtractAll</c>. If you call <c>ZipEntry.Extract</c> on a directory
        /// entry, the timestamps on directory in the filesystem will reflect the
        /// times stored in the zip.  If you then call <c>ZipEntry.Extract</c> on
        /// a file entry, which is extracted into the directory, the timestamps on
        /// the directory will be updated to the current time.
        /// </para>
        /// </remarks>
        /// 
        /// <example>
        /// This example extracts all the entries in a zip archive file, to the
        /// specified target directory.  The extraction will overwrite any
        /// existing files silently.
        /// 
        /// <code>
        /// String TargetDirectory= "unpack";
        /// using(ZipFile zip= ZipFile.Read(ZipFileToExtract))
        /// {
        /// zip.ExtractExistingFile= ExtractExistingFileAction.OverwriteSilently;
        /// zip.ExtractAll(TargetDirectory);
        /// }
        /// </code>
        /// 
        /// <code lang="VB">
        /// Dim TargetDirectory As String = "unpack"
        /// Using zip As ZipFile = ZipFile.Read(ZipFileToExtract)
        /// zip.ExtractExistingFile= ExtractExistingFileAction.OverwriteSilently
        /// zip.ExtractAll(TargetDirectory)
        /// End Using
        /// </code>
        /// </example>
        /// 
        /// <seealso cref="E:DotNetZipAdditionalPlatforms.Zip.ZipFile.ExtractProgress" />
        /// <seealso cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.ExtractExistingFile" />
        /// 
        /// <param name="path">
        /// The path to which the contents of the zipfile will be extracted.
        /// The path can be relative or fully-qualified.
        /// </param>
        public void ExtractAll(string path)
        {
            this._InternalExtractAll(path, true);
        }

        /// <summary>
        /// Extracts all of the items in the zip archive, to the specified path in the
        /// filesystem, using the specified behavior when extraction would overwrite an
        /// existing file.
        /// </summary>
        /// 
        /// <remarks>
        /// 
        /// <para>
        /// This method will extract all entries in the <c>ZipFile</c> to the specified
        /// path.  For an extraction that would overwrite an existing file, the behavior
        /// is dictated by <paramref name="extractExistingFile" />, which overrides any
        /// setting you may have made on individual ZipEntry instances.
        /// </para>
        /// 
        /// <para>
        /// The action to take when an extract would overwrite an existing file
        /// applies to all entries.  If you want to set this on a per-entry basis,
        /// then you must use <see cref="M:DotNetZipAdditionalPlatforms.Zip.ZipEntry.Extract(System.String,DotNetZipAdditionalPlatforms.Zip.ExtractExistingFileAction)" /> or one of the similar methods.
        /// </para>
        /// 
        /// <para>
        /// Calling this method is equivalent to setting the <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.ExtractExistingFile" /> property and then calling <see cref="M:DotNetZipAdditionalPlatforms.Zip.ZipFile.ExtractAll(System.String)" />.
        /// </para>
        /// 
        /// <para>
        /// This method will send verbose output messages to the
        /// <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.StatusMessageTextWriter" />, if it is set on the <c>ZipFile</c> instance.
        /// </para>
        /// </remarks>
        /// 
        /// <example>
        /// This example extracts all the entries in a zip archive file, to the
        /// specified target directory.  It does not overwrite any existing files.
        /// <code>
        /// String TargetDirectory= "c:\\unpack";
        /// using(ZipFile zip= ZipFile.Read(ZipFileToExtract))
        /// {
        /// zip.ExtractAll(TargetDirectory, ExtractExistingFileAction.DontOverwrite);
        /// }
        /// </code>
        /// 
        /// <code lang="VB">
        /// Dim TargetDirectory As String = "c:\unpack"
        /// Using zip As ZipFile = ZipFile.Read(ZipFileToExtract)
        /// zip.ExtractAll(TargetDirectory, ExtractExistingFileAction.DontOverwrite)
        /// End Using
        /// </code>
        /// </example>
        /// 
        /// <param name="path">
        /// The path to which the contents of the zipfile will be extracted.
        /// The path can be relative or fully-qualified.
        /// </param>
        /// 
        /// <param name="extractExistingFile">
        /// The action to take if extraction would overwrite an existing file.
        /// </param>
        /// <seealso cref="M:DotNetZipAdditionalPlatforms.Zip.ZipFile.ExtractSelectedEntries(System.String,DotNetZipAdditionalPlatforms.Zip.ExtractExistingFileAction)" />
        public void ExtractAll(string path, ExtractExistingFileAction extractExistingFile)
        {
            this.ExtractExistingFile = extractExistingFile;
            this._InternalExtractAll(path, true);
        }

        private static void ExtractResourceToFile(Assembly a, string resourceName, string filename)
        {
            int count = 0;
            byte[] buffer = new byte[0x400];
            using (Stream stream = a.GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                {
                    throw new ZipException(string.Format(CultureInfo.InvariantCulture, "missing resource '{0}'", resourceName));
                }
                using (FileStream stream2 = File.OpenWrite(filename))
                {
                    do
                    {
                        count = stream.Read(buffer, 0, buffer.Length);
                        stream2.Write(buffer, 0, count);
                    }
                    while (count > 0);
                }
            }
        }

        /// <summary>
        /// Selects and Extracts a set of Entries from the ZipFile.
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// The entries are extracted into the current working directory.
        /// </para>
        /// 
        /// <para>
        /// If any of the files to be extracted already exist, then the action taken is as
        /// specified in the <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipEntry.ExtractExistingFile" /> property on the
        /// corresponding ZipEntry instance.  By default, the action taken in this case is to
        /// throw an exception.
        /// </para>
        /// 
        /// <para>
        /// For information on the syntax of the selectionCriteria string,
        /// see <see cref="M:DotNetZipAdditionalPlatforms.Zip.ZipFile.AddSelectedFiles(System.String)" />.
        /// </para>
        /// </remarks>
        /// 
        /// <example>
        /// This example shows how extract all XML files modified after 15 January 2009.
        /// <code>
        /// using (ZipFile zip = ZipFile.Read(zipArchiveName))
        /// {
        /// zip.ExtractSelectedEntries("name = *.xml  and  mtime &gt; 2009-01-15");
        /// }
        /// </code>
        /// </example>
        /// <param name="selectionCriteria">the selection criteria for entries to extract.</param>
        /// 
        /// <seealso cref="M:DotNetZipAdditionalPlatforms.Zip.ZipFile.ExtractSelectedEntries(System.String,DotNetZipAdditionalPlatforms.Zip.ExtractExistingFileAction)" />
        public void ExtractSelectedEntries(string selectionCriteria)
        {
            foreach (ZipEntry entry in this.SelectEntries(selectionCriteria))
            {
                entry.Password = this._Password;
                entry.Extract();
            }
        }

        /// <summary>
        /// Selects and Extracts a set of Entries from the ZipFile.
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// The entries are extracted into the current working directory. When extraction would would
        /// overwrite an existing filesystem file, the action taken is as specified in the
        /// <paramref name="extractExistingFile" /> parameter.
        /// </para>
        /// 
        /// <para>
        /// For information on the syntax of the string describing the entry selection criteria,
        /// see <see cref="M:DotNetZipAdditionalPlatforms.Zip.ZipFile.AddSelectedFiles(System.String)" />.
        /// </para>
        /// </remarks>
        /// 
        /// <example>
        /// This example shows how extract all XML files modified after 15 January 2009,
        /// overwriting any existing files.
        /// <code>
        /// using (ZipFile zip = ZipFile.Read(zipArchiveName))
        /// {
        /// zip.ExtractSelectedEntries("name = *.xml  and  mtime &gt; 2009-01-15",
        /// ExtractExistingFileAction.OverwriteSilently);
        /// }
        /// </code>
        /// </example>
        /// 
        /// <param name="selectionCriteria">the selection criteria for entries to extract.</param>
        /// 
        /// <param name="extractExistingFile">
        /// The action to take if extraction would overwrite an existing file.
        /// </param>
        public void ExtractSelectedEntries(string selectionCriteria, ExtractExistingFileAction extractExistingFile)
        {
            foreach (ZipEntry entry in this.SelectEntries(selectionCriteria))
            {
                entry.Password = this._Password;
                entry.Extract(extractExistingFile);
            }
        }

        /// <summary>
        /// Selects and Extracts a set of Entries from the ZipFile.
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// The entries are selected from the specified directory within the archive, and then
        /// extracted into the current working directory.
        /// </para>
        /// 
        /// <para>
        /// If any of the files to be extracted already exist, then the action taken is as
        /// specified in the <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipEntry.ExtractExistingFile" /> property on the
        /// corresponding ZipEntry instance.  By default, the action taken in this case is to
        /// throw an exception.
        /// </para>
        /// 
        /// <para>
        /// For information on the syntax of the string describing the entry selection criteria,
        /// see <see cref="M:DotNetZipAdditionalPlatforms.Zip.ZipFile.AddSelectedFiles(System.String)" />.
        /// </para>
        /// </remarks>
        /// 
        /// <example>
        /// This example shows how extract all XML files modified after 15 January 2009,
        /// and writes them to the "unpack" directory.
        /// <code>
        /// using (ZipFile zip = ZipFile.Read(zipArchiveName))
        /// {
        /// zip.ExtractSelectedEntries("name = *.xml  and  mtime &gt; 2009-01-15","unpack");
        /// }
        /// </code>
        /// </example>
        /// 
        /// <param name="selectionCriteria">the selection criteria for entries to extract.</param>
        /// 
        /// <param name="directoryPathInArchive">
        /// the directory in the archive from which to select entries. If null, then
        /// all directories in the archive are used.
        /// </param>
        /// 
        /// <seealso cref="M:DotNetZipAdditionalPlatforms.Zip.ZipFile.ExtractSelectedEntries(System.String,System.String,System.String,DotNetZipAdditionalPlatforms.Zip.ExtractExistingFileAction)" />
        public void ExtractSelectedEntries(string selectionCriteria, string directoryPathInArchive)
        {
            foreach (ZipEntry entry in this.SelectEntries(selectionCriteria, directoryPathInArchive))
            {
                entry.Password = this._Password;
                entry.Extract();
            }
        }

        /// <summary>
        /// Selects and Extracts a set of Entries from the ZipFile.
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// The entries are extracted into the specified directory. If any of the files to be
        /// extracted already exist, an exception will be thrown.
        /// </para>
        /// <para>
        /// For information on the syntax of the string describing the entry selection criteria,
        /// see <see cref="M:DotNetZipAdditionalPlatforms.Zip.ZipFile.AddSelectedFiles(System.String)" />.
        /// </para>
        /// </remarks>
        /// 
        /// <param name="selectionCriteria">the selection criteria for entries to extract.</param>
        /// 
        /// <param name="directoryInArchive">
        /// the directory in the archive from which to select entries. If null, then
        /// all directories in the archive are used.
        /// </param>
        /// 
        /// <param name="extractDirectory">
        /// the directory on the disk into which to extract. It will be created
        /// if it does not exist.
        /// </param>
        public void ExtractSelectedEntries(string selectionCriteria, string directoryInArchive, string extractDirectory)
        {
            foreach (ZipEntry entry in this.SelectEntries(selectionCriteria, directoryInArchive))
            {
                entry.Password = this._Password;
                entry.Extract(extractDirectory);
            }
        }

        /// <summary>
        /// Selects and Extracts a set of Entries from the ZipFile.
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// The entries are extracted into the specified directory. When extraction would would
        /// overwrite an existing filesystem file, the action taken is as specified in the
        /// <paramref name="extractExistingFile" /> parameter.
        /// </para>
        /// 
        /// <para>
        /// For information on the syntax of the string describing the entry selection criteria,
        /// see <see cref="M:DotNetZipAdditionalPlatforms.Zip.ZipFile.AddSelectedFiles(System.String)" />.
        /// </para>
        /// </remarks>
        /// 
        /// <example>
        /// This example shows how extract all files  with an XML extension or with  a size larger than 100,000 bytes,
        /// and puts them in the unpack directory.  For any files that already exist in
        /// that destination directory, they will not be overwritten.
        /// <code>
        /// using (ZipFile zip = ZipFile.Read(zipArchiveName))
        /// {
        /// zip.ExtractSelectedEntries("name = *.xml  or  size &gt; 100000",
        /// null,
        /// "unpack",
        /// ExtractExistingFileAction.DontOverwrite);
        /// }
        /// </code>
        /// </example>
        /// 
        /// <param name="selectionCriteria">the selection criteria for entries to extract.</param>
        /// 
        /// <param name="extractDirectory">
        /// The directory on the disk into which to extract. It will be created if it does not exist.
        /// </param>
        /// 
        /// <param name="directoryPathInArchive">
        /// The directory in the archive from which to select entries. If null, then
        /// all directories in the archive are used.
        /// </param>
        /// 
        /// <param name="extractExistingFile">
        /// The action to take if extraction would overwrite an existing file.
        /// </param>
        public void ExtractSelectedEntries(string selectionCriteria, string directoryPathInArchive, string extractDirectory, ExtractExistingFileAction extractExistingFile)
        {
            foreach (ZipEntry entry in this.SelectEntries(selectionCriteria, directoryPathInArchive))
            {
                entry.Password = this._Password;
                entry.Extract(extractDirectory, extractExistingFile);
            }
        }

        /// <summary>
        /// Rewrite the directory within a zipfile.
        /// </summary>
        /// 
        /// <remarks>
        /// 
        /// <para>
        /// In cases of data error, the directory in a zip file can get out of
        /// synch with the entries in the zip file.  This method attempts to fix
        /// the zip file if this has occurred.
        /// </para>
        /// 
        /// <para> This can take a long time for large zip files. </para>
        /// 
        /// <para> This won't work if the zip file uses a non-standard
        /// code page - neither IBM437 nor UTF-8. </para>
        /// 
        /// <para>
        /// This method is not supported in the Reduced or Compact Framework
        /// versions of DotNetZip.
        /// </para>
        /// 
        /// <para>
        /// Developers using COM can use the <see cref="M:DotNetZipAdditionalPlatforms.Zip.ComHelper.FixZipDirectory(System.String)">ComHelper.FixZipDirectory(String)</see>
        /// method.
        /// </para>
        /// 
        /// </remarks>
        /// 
        /// <param name="zipFileName">The filename to of the zip file to fix.</param>
        /// 
        /// <seealso cref="M:DotNetZipAdditionalPlatforms.Zip.ZipFile.CheckZip(System.String)" />
        /// <seealso cref="M:DotNetZipAdditionalPlatforms.Zip.ZipFile.CheckZip(System.String,System.Boolean,System.IO.TextWriter)" />
        public static void FixZipDirectory(string zipFileName)
        {
            using (ZipFile file = new ZipFile())
            {
                file.FullScan = true;
                file.Initialize(zipFileName);
                file.Save(zipFileName);
            }
        }

        internal static string GenerateTempPathname(string dir, string extension)
        {
            string path = null;
            string name = Assembly.GetExecutingAssembly().GetName().Name;
            do
            {
                string str3 = Guid.NewGuid().ToString();
                string str4 = string.Format(CultureInfo.InvariantCulture, "{0}-{1}-{2}.{3}", new object[] { name, DateTime.Now.ToString("yyyyMMMdd-HHmmss"), str3, extension });
                path = Path.Combine(dir, str4);
            }
            while (File.Exists(path) || Directory.Exists(path));
            return path;
        }

        /// <summary>
        /// Generic IEnumerator support, for use of a ZipFile in an enumeration.
        /// </summary>
        /// 
        /// <remarks>
        /// You probably do not want to call <c>GetEnumerator</c> explicitly. Instead
        /// it is implicitly called when you use a <see langword="foreach" /> loop in C#, or a
        /// <c>For Each</c> loop in VB.NET.
        /// </remarks>
        /// 
        /// <example>
        /// This example reads a zipfile of a given name, then enumerates the
        /// entries in that zip file, and displays the information about each
        /// entry on the Console.
        /// <code>
        /// using (ZipFile zip = ZipFile.Read(zipfile))
        /// {
        /// bool header = true;
        /// foreach (ZipEntry e in zip)
        /// {
        /// if (header)
        /// {
        /// System.Console.WriteLine("Zipfile: {0}", zip.Name);
        /// System.Console.WriteLine("Version Needed: 0x{0:X2}", e.VersionNeeded);
        /// System.Console.WriteLine("BitField: 0x{0:X2}", e.BitField);
        /// System.Console.WriteLine("Compression Method: 0x{0:X2}", e.CompressionMethod);
        /// System.Console.WriteLine("\n{1,-22} {2,-6} {3,4}   {4,-8}  {0}",
        /// "Filename", "Modified", "Size", "Ratio", "Packed");
        /// System.Console.WriteLine(new System.String('-', 72));
        /// header = false;
        /// }
        /// 
        /// System.Console.WriteLine("{1,-22} {2,-6} {3,4:F0}%   {4,-8}  {0}",
        /// e.FileName,
        /// e.LastModified.ToString("yyyy-MM-dd HH:mm:ss"),
        /// e.UncompressedSize,
        /// e.CompressionRatio,
        /// e.CompressedSize);
        /// 
        /// e.Extract();
        /// }
        /// }
        /// </code>
        /// 
        /// <code lang="VB">
        /// Dim ZipFileToExtract As String = "c:\foo.zip"
        /// Using zip As ZipFile = ZipFile.Read(ZipFileToExtract)
        /// Dim header As Boolean = True
        /// Dim e As ZipEntry
        /// For Each e In zip
        /// If header Then
        /// Console.WriteLine("Zipfile: {0}", zip.Name)
        /// Console.WriteLine("Version Needed: 0x{0:X2}", e.VersionNeeded)
        /// Console.WriteLine("BitField: 0x{0:X2}", e.BitField)
        /// Console.WriteLine("Compression Method: 0x{0:X2}", e.CompressionMethod)
        /// Console.WriteLine(ChrW(10) &amp; "{1,-22} {2,-6} {3,4}   {4,-8}  {0}", _
        /// "Filename", "Modified", "Size", "Ratio", "Packed" )
        /// Console.WriteLine(New String("-"c, 72))
        /// header = False
        /// End If
        /// Console.WriteLine("{1,-22} {2,-6} {3,4:F0}%   {4,-8}  {0}", _
        /// e.FileName, _
        /// e.LastModified.ToString("yyyy-MM-dd HH:mm:ss"), _
        /// e.UncompressedSize, _
        /// e.CompressionRatio, _
        /// e.CompressedSize )
        /// e.Extract
        /// Next
        /// End Using
        /// </code>
        /// </example>
        /// 
        /// <returns>A generic enumerator suitable for use  within a foreach loop.</returns>
        public IEnumerator<ZipEntry> GetEnumerator()
        {
            foreach (ZipEntry iteratorVariable0 in this._entries.Values)
            {
                yield return iteratorVariable0;
            }
        }

        /// <summary>
        /// An IEnumerator, for use of a ZipFile in a foreach construct.
        /// </summary>
        /// 
        /// <remarks>
        /// This method is included for COM support.  An application generally does not call
        /// this method directly.  It is called implicitly by COM clients when enumerating
        /// the entries in the ZipFile instance.  In VBScript, this is done with a <c>For Each</c>
        /// statement.  In Javascript, this is done with <c>new Enumerator(zipfile)</c>.
        /// </remarks>
        /// 
        /// <returns>
        /// The IEnumerator over the entries in the ZipFile.
        /// </returns>
        [DispId(-4)]
        public IEnumerator GetNewEnum()
        {
            return this.GetEnumerator();
        }

        /// <summary>
        /// Initialize a <c>ZipFile</c> instance by reading in a zip file.
        /// </summary>
        /// 
        /// <remarks>
        /// 
        /// <para>
        /// This method is primarily useful from COM Automation environments, when
        /// reading or extracting zip files. In COM, it is not possible to invoke
        /// parameterized constructors for a class. A COM Automation application can
        /// update a zip file by using the <see cref="M:DotNetZipAdditionalPlatforms.Zip.ZipFile.#ctor">default (no argument)
        /// constructor</see>, then calling <c>Initialize()</c> to read the contents
        /// of an on-disk zip archive into the <c>ZipFile</c> instance.
        /// </para>
        /// 
        /// <para>
        /// .NET applications are encouraged to use the <c>ZipFile.Read()</c> methods
        /// for better clarity.
        /// </para>
        /// 
        /// </remarks>
        /// <param name="fileName">the name of the existing zip file to read in.</param>
        public void Initialize(string fileName)
        {
            try
            {
                this._InitInstance(fileName, null);
            }
            catch (Exception exception)
            {
                throw new ZipException(string.Format(CultureInfo.InvariantCulture, "{0} is not a valid zip file", fileName), exception);
            }
        }

        internal void InternalAddEntry(string name, ZipEntry entry)
        {
            this._entries.Add(name, entry);
            this._zipEntriesAsList = null;
            this._contentsChanged = true;
        }

        /// <summary>
        /// Checks the given file to see if it appears to be a valid zip file.
        /// </summary>
        /// <remarks>
        /// 
        /// <para>
        /// Calling this method is equivalent to calling <see cref="M:DotNetZipAdditionalPlatforms.Zip.ZipFile.IsZipFile(System.String,System.Boolean)" /> with the testExtract parameter set to false.
        /// </para>
        /// </remarks>
        /// 
        /// <param name="fileName">The file to check.</param>
        /// <returns>true if the file appears to be a zip file.</returns>
        public static bool IsZipFile(string fileName)
        {
            return IsZipFile(fileName, false);
        }

        /// <summary>
        /// Checks a stream to see if it contains a valid zip archive.
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// This method reads the zip archive contained in the specified stream, verifying
        /// the ZIP metadata as it reads.  If testExtract is true, this method also extracts
        /// each entry in the archive, dumping all the bits into <see cref="F:System.IO.Stream.Null" />.
        /// </para>
        /// 
        /// <para>
        /// If everything succeeds, then the method returns true.  If anything fails -
        /// for example if an incorrect signature or CRC is found, indicating a corrupt
        /// file, the the method returns false.  This method also returns false for a
        /// file that does not exist.
        /// </para>
        /// 
        /// <para>
        /// If <c>testExtract</c> is true, this method reads in the content for each
        /// entry, expands it, and checks CRCs.  This provides an additional check
        /// beyond verifying the zip header data.
        /// </para>
        /// 
        /// <para>
        /// If <c>testExtract</c> is true, and if any of the zip entries are protected
        /// with a password, this method will return false.  If you want to verify a
        /// ZipFile that has entries which are protected with a password, you will need
        /// to do that manually.
        /// </para>
        /// </remarks>
        /// 
        /// <seealso cref="M:DotNetZipAdditionalPlatforms.Zip.ZipFile.IsZipFile(System.String,System.Boolean)" />
        /// 
        /// <param name="stream">The stream to check.</param>
        /// <param name="testExtract">true if the caller wants to extract each entry.</param>
        /// <returns>true if the stream contains a valid zip archive.</returns>
        public static bool IsZipFile(Stream stream, bool testExtract)
        {
            if (stream == null)
            {
                throw new ArgumentNullException("stream");
            }
            bool flag = false;
            try
            {
                if (!stream.CanRead)
                {
                    return false;
                }
                Stream @null = Stream.Null;
                using (ZipFile file = Read(stream, null, null, null))
                {
                    if (testExtract)
                    {
                        foreach (ZipEntry entry in file)
                        {
                            if (!entry.IsDirectory)
                            {
                                entry.Extract(@null);
                            }
                        }
                    }
                }
                flag = true;
            }
            catch (IOException)
            {
            }
            catch (ZipException)
            {
            }
            return flag;
        }

        /// <summary>
        /// Checks a file to see if it is a valid zip file.
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// This method opens the specified zip file, reads in the zip archive,
        /// verifying the ZIP metadata as it reads.
        /// </para>
        /// 
        /// <para>
        /// If everything succeeds, then the method returns true.  If anything fails -
        /// for example if an incorrect signature or CRC is found, indicating a
        /// corrupt file, the the method returns false.  This method also returns
        /// false for a file that does not exist.
        /// </para>
        /// 
        /// <para>
        /// If <paramref name="testExtract" /> is true, as part of its check, this
        /// method reads in the content for each entry, expands it, and checks CRCs.
        /// This provides an additional check beyond verifying the zip header and
        /// directory data.
        /// </para>
        /// 
        /// <para>
        /// If <paramref name="testExtract" /> is true, and if any of the zip entries
        /// are protected with a password, this method will return false.  If you want
        /// to verify a <c>ZipFile</c> that has entries which are protected with a
        /// password, you will need to do that manually.
        /// </para>
        /// 
        /// </remarks>
        /// 
        /// <param name="fileName">The zip file to check.</param>
        /// <param name="testExtract">true if the caller wants to extract each entry.</param>
        /// <returns>true if the file contains a valid zip file.</returns>
        public static bool IsZipFile(string fileName, bool testExtract)
        {
            bool flag = false;
            try
            {
                if (!File.Exists(fileName))
                {
                    return false;
                }
                using (FileStream stream = File.Open(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    flag = IsZipFile(stream, testExtract);
                }
            }
            catch (IOException)
            {
            }
            catch (ZipException)
            {
            }
            return flag;
        }

        private static void NotifyEntriesSaveComplete(ICollection<ZipEntry> c)
        {
            foreach (ZipEntry entry in c)
            {
                entry.NotifySaveComplete();
            }
        }

        internal void NotifyEntryChanged()
        {
            this._contentsChanged = true;
        }

        private void OnAddCompleted()
        {
            EventHandler<AddProgressEventArgs> addProgress = this.AddProgress;
            if (addProgress != null)
            {
                AddProgressEventArgs e = AddProgressEventArgs.Completed(this.ArchiveNameForEvent);
                addProgress(this, e);
            }
        }

        private void OnAddStarted()
        {
            EventHandler<AddProgressEventArgs> addProgress = this.AddProgress;
            if (addProgress != null)
            {
                AddProgressEventArgs e = AddProgressEventArgs.Started(this.ArchiveNameForEvent);
                addProgress(this, e);
                if (e.Cancel)
                {
                    this._addOperationCanceled = true;
                }
            }
        }

        private void OnExtractAllCompleted(string path)
        {
            EventHandler<ExtractProgressEventArgs> extractProgress = this.ExtractProgress;
            if (extractProgress != null)
            {
                ExtractProgressEventArgs e = ExtractProgressEventArgs.ExtractAllCompleted(this.ArchiveNameForEvent, path);
                extractProgress(this, e);
            }
        }

        private void OnExtractAllStarted(string path)
        {
            EventHandler<ExtractProgressEventArgs> extractProgress = this.ExtractProgress;
            if (extractProgress != null)
            {
                ExtractProgressEventArgs e = ExtractProgressEventArgs.ExtractAllStarted(this.ArchiveNameForEvent, path);
                extractProgress(this, e);
            }
        }

        internal bool OnExtractBlock(ZipEntry entry, long bytesWritten, long totalBytesToWrite)
        {
            EventHandler<ExtractProgressEventArgs> extractProgress = this.ExtractProgress;
            if (extractProgress != null)
            {
                ExtractProgressEventArgs e = ExtractProgressEventArgs.ByteUpdate(this.ArchiveNameForEvent, entry, bytesWritten, totalBytesToWrite);
                extractProgress(this, e);
                if (e.Cancel)
                {
                    this._extractOperationCanceled = true;
                }
            }
            return this._extractOperationCanceled;
        }

        private void OnExtractEntry(int current, bool before, ZipEntry currentEntry, string path)
        {
            EventHandler<ExtractProgressEventArgs> extractProgress = this.ExtractProgress;
            if (extractProgress != null)
            {
                ExtractProgressEventArgs e = new ExtractProgressEventArgs(this.ArchiveNameForEvent, before, this._entries.Count, current, currentEntry, path);
                extractProgress(this, e);
                if (e.Cancel)
                {
                    this._extractOperationCanceled = true;
                }
            }
        }

        internal bool OnExtractExisting(ZipEntry entry, string path)
        {
            EventHandler<ExtractProgressEventArgs> extractProgress = this.ExtractProgress;
            if (extractProgress != null)
            {
                ExtractProgressEventArgs e = ExtractProgressEventArgs.ExtractExisting(this.ArchiveNameForEvent, entry, path);
                extractProgress(this, e);
                if (e.Cancel)
                {
                    this._extractOperationCanceled = true;
                }
            }
            return this._extractOperationCanceled;
        }

        internal void OnReadBytes(ZipEntry entry)
        {
            EventHandler<ReadProgressEventArgs> readProgress = this.ReadProgress;
            if (readProgress != null)
            {
                ReadProgressEventArgs e = ReadProgressEventArgs.ByteUpdate(this.ArchiveNameForEvent, entry, this.ReadStream.Position, this.LengthOfReadStream);
                readProgress(this, e);
            }
        }

        private void OnReadCompleted()
        {
            EventHandler<ReadProgressEventArgs> readProgress = this.ReadProgress;
            if (readProgress != null)
            {
                ReadProgressEventArgs e = ReadProgressEventArgs.Completed(this.ArchiveNameForEvent);
                readProgress(this, e);
            }
        }

        internal void OnReadEntry(bool before, ZipEntry entry)
        {
            EventHandler<ReadProgressEventArgs> readProgress = this.ReadProgress;
            if (readProgress != null)
            {
                ReadProgressEventArgs e = before ? ReadProgressEventArgs.Before(this.ArchiveNameForEvent, this._entries.Count) : ReadProgressEventArgs.After(this.ArchiveNameForEvent, entry, this._entries.Count);
                readProgress(this, e);
            }
        }

        private void OnReadStarted()
        {
            EventHandler<ReadProgressEventArgs> readProgress = this.ReadProgress;
            if (readProgress != null)
            {
                ReadProgressEventArgs e = ReadProgressEventArgs.Started(this.ArchiveNameForEvent);
                readProgress(this, e);
            }
        }

        internal bool OnSaveBlock(ZipEntry entry, long bytesXferred, long totalBytesToXfer)
        {
            EventHandler<SaveProgressEventArgs> saveProgress = this.SaveProgress;
            if (saveProgress != null)
            {
                SaveProgressEventArgs e = SaveProgressEventArgs.ByteUpdate(this.ArchiveNameForEvent, entry, bytesXferred, totalBytesToXfer);
                saveProgress(this, e);
                if (e.Cancel)
                {
                    this._saveOperationCanceled = true;
                }
            }
            return this._saveOperationCanceled;
        }

        private void OnSaveCompleted()
        {
            EventHandler<SaveProgressEventArgs> saveProgress = this.SaveProgress;
            if (saveProgress != null)
            {
                SaveProgressEventArgs e = SaveProgressEventArgs.Completed(this.ArchiveNameForEvent);
                saveProgress(this, e);
            }
        }

        private void OnSaveEntry(int current, ZipEntry entry, bool before)
        {
            EventHandler<SaveProgressEventArgs> saveProgress = this.SaveProgress;
            if (saveProgress != null)
            {
                SaveProgressEventArgs e = new SaveProgressEventArgs(this.ArchiveNameForEvent, before, this._entries.Count, current, entry);
                saveProgress(this, e);
                if (e.Cancel)
                {
                    this._saveOperationCanceled = true;
                }
            }
        }

        private void OnSaveEvent(ZipProgressEventType eventFlavor)
        {
            EventHandler<SaveProgressEventArgs> saveProgress = this.SaveProgress;
            if (saveProgress != null)
            {
                SaveProgressEventArgs e = new SaveProgressEventArgs(this.ArchiveNameForEvent, eventFlavor);
                saveProgress(this, e);
                if (e.Cancel)
                {
                    this._saveOperationCanceled = true;
                }
            }
        }

        private void OnSaveStarted()
        {
            EventHandler<SaveProgressEventArgs> saveProgress = this.SaveProgress;
            if (saveProgress != null)
            {
                SaveProgressEventArgs e = SaveProgressEventArgs.Started(this.ArchiveNameForEvent);
                saveProgress(this, e);
                if (e.Cancel)
                {
                    this._saveOperationCanceled = true;
                }
            }
        }

        internal bool OnSingleEntryExtract(ZipEntry entry, string path, bool before)
        {
            EventHandler<ExtractProgressEventArgs> extractProgress = this.ExtractProgress;
            if (extractProgress != null)
            {
                ExtractProgressEventArgs e = before ? ExtractProgressEventArgs.BeforeExtractEntry(this.ArchiveNameForEvent, entry, path) : ExtractProgressEventArgs.AfterExtractEntry(this.ArchiveNameForEvent, entry, path);
                extractProgress(this, e);
                if (e.Cancel)
                {
                    this._extractOperationCanceled = true;
                }
            }
            return this._extractOperationCanceled;
        }

        internal bool OnZipErrorSaving(ZipEntry entry, Exception exc)
        {
            if (this.ZipError != null)
            {
                lock (this.LOCK)
                {
                    ZipErrorEventArgs e = ZipErrorEventArgs.Saving(this.Name, entry, exc);
                    this.ZipError(this, e);
                    if (e.Cancel)
                    {
                        this._saveOperationCanceled = true;
                    }
                }
            }
            return this._saveOperationCanceled;
        }

        /// <summary>
        /// Reads a zip archive from a stream.
        /// </summary>
        /// 
        /// <remarks>
        /// 
        /// <para>
        /// When reading from a file, it's probably easier to just use
        /// <see cref="M:DotNetZipAdditionalPlatforms.Zip.ZipFile.Read(System.String,DotNetZipAdditionalPlatforms.Zip.ReadOptions)">ZipFile.Read(String, ReadOptions)</see>.  This
        /// overload is useful when when the zip archive content is
        /// available from an already-open stream. The stream must be
        /// open and readable and seekable when calling this method.  The
        /// stream is left open when the reading is completed.
        /// </para>
        /// 
        /// <para>
        /// Using this overload, the stream is read using the default
        /// <c>System.Text.Encoding</c>, which is the <c>IBM437</c>
        /// codepage. If you want to specify the encoding to use when
        /// reading the zipfile content, see
        /// <see cref="M:DotNetZipAdditionalPlatforms.Zip.ZipFile.Read(System.IO.Stream,DotNetZipAdditionalPlatforms.Zip.ReadOptions)">ZipFile.Read(Stream, ReadOptions)</see>.  This
        /// </para>
        /// 
        /// <para>
        /// Reading of zip content begins at the current position in the
        /// stream.  This means if you have a stream that concatenates
        /// regular data and zip data, if you position the open, readable
        /// stream at the start of the zip data, you will be able to read
        /// the zip archive using this constructor, or any of the ZipFile
        /// constructors that accept a <see cref="T:System.IO.Stream" /> as
        /// input. Some examples of where this might be useful: the zip
        /// content is concatenated at the end of a regular EXE file, as
        /// some self-extracting archives do.  (Note: SFX files produced
        /// by DotNetZip do not work this way; they can be read as normal
        /// ZIP files). Another example might be a stream being read from
        /// a database, where the zip content is embedded within an
        /// aggregate stream of data.
        /// </para>
        /// 
        /// </remarks>
        /// 
        /// <example>
        /// <para>
        /// This example shows how to Read zip content from a stream, and
        /// extract one entry into a different stream. In this example,
        /// the filename "NameOfEntryInArchive.doc", refers only to the
        /// name of the entry within the zip archive.  A file by that
        /// name is not created in the filesystem.  The I/O is done
        /// strictly with the given streams.
        /// </para>
        /// 
        /// <code>
        /// using (ZipFile zip = ZipFile.Read(InputStream))
        /// {
        /// zip.Extract("NameOfEntryInArchive.doc", OutputStream);
        /// }
        /// </code>
        /// 
        /// <code lang="VB">
        /// Using zip as ZipFile = ZipFile.Read(InputStream)
        /// zip.Extract("NameOfEntryInArchive.doc", OutputStream)
        /// End Using
        /// </code>
        /// </example>
        /// 
        /// <param name="zipStream">the stream containing the zip data.</param>
        /// 
        /// <returns>The ZipFile instance read from the stream</returns>
        public static ZipFile Read(Stream zipStream)
        {
            return Read(zipStream, null, null, null);
        }

        /// <summary>
        /// Reads a zip file archive and returns the instance.
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// The stream is read using the default <c>System.Text.Encoding</c>, which is the
        /// <c>IBM437</c> codepage.
        /// </para>
        /// </remarks>
        /// 
        /// <exception cref="T:System.Exception">
        /// Thrown if the <c>ZipFile</c> cannot be read. The implementation of this method
        /// relies on <c>System.IO.File.OpenRead</c>, which can throw a variety of exceptions,
        /// including specific exceptions if a file is not found, an unauthorized access
        /// exception, exceptions for poorly formatted filenames, and so on.
        /// </exception>
        /// 
        /// <param name="fileName">
        /// The name of the zip archive to open.  This can be a fully-qualified or relative
        /// pathname.
        /// </param>
        /// 
        /// <seealso cref="M:DotNetZipAdditionalPlatforms.Zip.ZipFile.Read(System.String,DotNetZipAdditionalPlatforms.Zip.ReadOptions)" />.
        /// 
        /// <returns>The instance read from the zip archive.</returns>
        public static ZipFile Read(string fileName)
        {
            return Read(fileName, null, null, null);
        }

        /// <summary>
        /// Reads a zip file archive from the given stream using the
        /// specified options.
        /// </summary>
        /// 
        /// <remarks>
        /// 
        /// <para>
        /// When reading from a file, it's probably easier to just use
        /// <see cref="M:DotNetZipAdditionalPlatforms.Zip.ZipFile.Read(System.String,DotNetZipAdditionalPlatforms.Zip.ReadOptions)">ZipFile.Read(String, ReadOptions)</see>.  This
        /// overload is useful when when the zip archive content is
        /// available from an already-open stream. The stream must be
        /// open and readable and seekable when calling this method.  The
        /// stream is left open when the reading is completed.
        /// </para>
        /// 
        /// <para>
        /// Reading of zip content begins at the current position in the
        /// stream.  This means if you have a stream that concatenates
        /// regular data and zip data, if you position the open, readable
        /// stream at the start of the zip data, you will be able to read
        /// the zip archive using this constructor, or any of the ZipFile
        /// constructors that accept a <see cref="T:System.IO.Stream" /> as
        /// input. Some examples of where this might be useful: the zip
        /// content is concatenated at the end of a regular EXE file, as
        /// some self-extracting archives do.  (Note: SFX files produced
        /// by DotNetZip do not work this way; they can be read as normal
        /// ZIP files). Another example might be a stream being read from
        /// a database, where the zip content is embedded within an
        /// aggregate stream of data.
        /// </para>
        /// </remarks>
        /// 
        /// <param name="zipStream">the stream containing the zip data.</param>
        /// 
        /// <param name="options">
        /// The set of options to use when reading the zip file.
        /// </param>
        /// 
        /// <exception cref="T:System.Exception">
        /// Thrown if the zip archive cannot be read.
        /// </exception>
        /// 
        /// <returns>The ZipFile instance read from the stream.</returns>
        /// 
        /// <seealso cref="M:DotNetZipAdditionalPlatforms.Zip.ZipFile.Read(System.String,DotNetZipAdditionalPlatforms.Zip.ReadOptions)" />
        public static ZipFile Read(Stream zipStream, ReadOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException("options");
            }
            return Read(zipStream, options.StatusMessageWriter, options.Encoding, options.ReadProgress);
        }

        /// <summary>
        /// Reads a zip file archive from the named filesystem file using the
        /// specified options.
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// This version of the <c>Read()</c> method allows the caller to pass
        /// in a <c>TextWriter</c> an <c>Encoding</c>, via an instance of the
        /// <c>ReadOptions</c> class.  The <c>ZipFile</c> is read in using the
        /// specified encoding for entries where UTF-8 encoding is not
        /// explicitly specified.
        /// </para>
        /// </remarks>
        /// 
        /// <example>
        /// 
        /// <para>
        /// This example shows how to read a zip file using the Big-5 Chinese
        /// code page (950), and extract each entry in the zip file, while
        /// sending status messages out to the Console.
        /// </para>
        /// 
        /// <para>
        /// For this code to work as intended, the zipfile must have been
        /// created using the big5 code page (CP950). This is typical, for
        /// example, when using WinRar on a machine with CP950 set as the
        /// default code page.  In that case, the names of entries within the
        /// Zip archive will be stored in that code page, and reading the zip
        /// archive must be done using that code page.  If the application did
        /// not use the correct code page in ZipFile.Read(), then names of
        /// entries within the zip archive would not be correctly retrieved.
        /// </para>
        /// 
        /// <code lang="C#">
        /// string zipToExtract = "MyArchive.zip";
        /// string extractDirectory = "extract";
        /// var options = new ReadOptions
        /// {
        /// StatusMessageWriter = System.Console.Out,
        /// Encoding = System.Text.Encoding.GetEncoding(950)
        /// };
        /// using (ZipFile zip = ZipFile.Read(zipToExtract, options))
        /// {
        /// foreach (ZipEntry e in zip)
        /// {
        /// e.Extract(extractDirectory);
        /// }
        /// }
        /// </code>
        /// 
        /// 
        /// <code lang="VB">
        /// Dim zipToExtract as String = "MyArchive.zip"
        /// Dim extractDirectory as String = "extract"
        /// Dim options as New ReadOptions
        /// options.Encoding = System.Text.Encoding.GetEncoding(950)
        /// options.StatusMessageWriter = System.Console.Out
        /// Using zip As ZipFile = ZipFile.Read(zipToExtract, options)
        /// Dim e As ZipEntry
        /// For Each e In zip
        /// e.Extract(extractDirectory)
        /// Next
        /// End Using
        /// </code>
        /// </example>
        /// 
        /// 
        /// <example>
        /// 
        /// <para>
        /// This example shows how to read a zip file using the default
        /// code page, to remove entries that have a modified date before a given threshold,
        /// sending status messages out to a <c>StringWriter</c>.
        /// </para>
        /// 
        /// <code lang="C#">
        /// var options = new ReadOptions
        /// {
        /// StatusMessageWriter = new System.IO.StringWriter()
        /// };
        /// using (ZipFile zip =  ZipFile.Read("PackedDocuments.zip", options))
        /// {
        /// var Threshold = new DateTime(2007,7,4);
        /// // We cannot remove the entry from the list, within the context of
        /// // an enumeration of said list.
        /// // So we add the doomed entry to a list to be removed later.
        /// // pass 1: mark the entries for removal
        /// var MarkedEntries = new System.Collections.Generic.List&lt;ZipEntry&gt;();
        /// foreach (ZipEntry e in zip)
        /// {
        /// if (e.LastModified &lt; Threshold)
        /// MarkedEntries.Add(e);
        /// }
        /// // pass 2: actually remove the entry.
        /// foreach (ZipEntry zombie in MarkedEntries)
        /// zip.RemoveEntry(zombie);
        /// zip.Comment = "This archive has been updated.";
        /// zip.Save();
        /// }
        /// // can now use contents of sw, eg store in an audit log
        /// </code>
        /// 
        /// <code lang="VB">
        /// Dim options as New ReadOptions
        /// options.StatusMessageWriter = New System.IO.StringWriter
        /// Using zip As ZipFile = ZipFile.Read("PackedDocuments.zip", options)
        /// Dim Threshold As New DateTime(2007, 7, 4)
        /// ' We cannot remove the entry from the list, within the context of
        /// ' an enumeration of said list.
        /// ' So we add the doomed entry to a list to be removed later.
        /// ' pass 1: mark the entries for removal
        /// Dim MarkedEntries As New System.Collections.Generic.List(Of ZipEntry)
        /// Dim e As ZipEntry
        /// For Each e In zip
        /// If (e.LastModified &lt; Threshold) Then
        /// MarkedEntries.Add(e)
        /// End If
        /// Next
        /// ' pass 2: actually remove the entry.
        /// Dim zombie As ZipEntry
        /// For Each zombie In MarkedEntries
        /// zip.RemoveEntry(zombie)
        /// Next
        /// zip.Comment = "This archive has been updated."
        /// zip.Save
        /// End Using
        /// ' can now use contents of sw, eg store in an audit log
        /// </code>
        /// </example>
        /// 
        /// <exception cref="T:System.Exception">
        /// Thrown if the zipfile cannot be read. The implementation of
        /// this method relies on <c>System.IO.File.OpenRead</c>, which
        /// can throw a variety of exceptions, including specific
        /// exceptions if a file is not found, an unauthorized access
        /// exception, exceptions for poorly formatted filenames, and so
        /// on.
        /// </exception>
        /// 
        /// <param name="fileName">
        /// The name of the zip archive to open.
        /// This can be a fully-qualified or relative pathname.
        /// </param>
        /// 
        /// <param name="options">
        /// The set of options to use when reading the zip file.
        /// </param>
        /// 
        /// <returns>The ZipFile instance read from the zip archive.</returns>
        /// 
        /// <seealso cref="M:DotNetZipAdditionalPlatforms.Zip.ZipFile.Read(System.IO.Stream,DotNetZipAdditionalPlatforms.Zip.ReadOptions)" />
        public static ZipFile Read(string fileName, ReadOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException("options");
            }
            return Read(fileName, options.StatusMessageWriter, options.Encoding, options.ReadProgress);
        }

        /// <summary>
        /// Reads a zip archive from a stream, using the specified text Encoding, the
        /// specified TextWriter for status messages,
        /// and the specified ReadProgress event handler.
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// Reading of zip content begins at the current position in the stream.  This
        /// means if you have a stream that concatenates regular data and zip data, if
        /// you position the open, readable stream at the start of the zip data, you
        /// will be able to read the zip archive using this constructor, or any of the
        /// ZipFile constructors that accept a <see cref="T:System.IO.Stream" /> as
        /// input. Some examples of where this might be useful: the zip content is
        /// concatenated at the end of a regular EXE file, as some self-extracting
        /// archives do.  (Note: SFX files produced by DotNetZip do not work this
        /// way). Another example might be a stream being read from a database, where
        /// the zip content is embedded within an aggregate stream of data.
        /// </para>
        /// </remarks>
        /// 
        /// <param name="zipStream">the stream containing the zip data.</param>
        /// 
        /// <param name="statusMessageWriter">
        /// The <c>System.IO.TextWriter</c> to which verbose status messages are written
        /// during operations on the <c>ZipFile</c>.  For example, in a console
        /// application, System.Console.Out works, and will get a message for each entry
        /// added to the ZipFile.  If the TextWriter is <c>null</c>, no verbose messages
        /// are written.
        /// </param>
        /// 
        /// <param name="encoding">
        /// The text encoding to use when reading entries that do not have the UTF-8
        /// encoding bit set.  Be careful specifying the encoding.  If the value you use
        /// here is not the same as the Encoding used when the zip archive was created
        /// (possibly by a different archiver) you will get unexpected results and
        /// possibly exceptions.  See the <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.ProvisionalAlternateEncoding" />
        /// property for more information.
        /// </param>
        /// 
        /// <param name="readProgress">
        /// An event handler for Read operations.
        /// </param>
        /// 
        /// <returns>an instance of ZipFile</returns>
        private static ZipFile Read(Stream zipStream, TextWriter statusMessageWriter, Encoding encoding, EventHandler<ReadProgressEventArgs> readProgress)
        {
            if (zipStream == null)
            {
                throw new ArgumentNullException("zipStream");
            }
            ZipFile zf = new ZipFile();
            zf._StatusMessageTextWriter = statusMessageWriter;
            zf._alternateEncoding = encoding ?? DefaultEncoding;
            zf._alternateEncodingUsage = ZipOption.Always;
            if (readProgress != null)
            {
                zf.ReadProgress += readProgress;
            }
            zf._readstream = (zipStream.Position == 0L) ? zipStream : new OffsetStream(zipStream);
            zf._ReadStreamIsOurs = false;
            if (zf.Verbose)
            {
                zf._StatusMessageTextWriter.WriteLine("reading from stream...");
            }
            ReadIntoInstance(zf);
            return zf;
        }

        /// <summary>
        /// Reads a zip file archive using the specified text encoding,  the specified
        /// TextWriter for status messages, and the specified ReadProgress event handler,
        /// and returns the instance.
        /// </summary>
        /// 
        /// <param name="fileName">
        /// The name of the zip archive to open.
        /// This can be a fully-qualified or relative pathname.
        /// </param>
        /// 
        /// <param name="readProgress">
        /// An event handler for Read operations.
        /// </param>
        /// 
        /// <param name="statusMessageWriter">
        /// The <c>System.IO.TextWriter</c> to use for writing verbose status messages
        /// during operations on the zip archive.  A console application may wish to
        /// pass <c>System.Console.Out</c> to get messages on the Console. A graphical
        /// or headless application may wish to capture the messages in a different
        /// <c>TextWriter</c>, such as a <c>System.IO.StringWriter</c>.
        /// </param>
        /// 
        /// <param name="encoding">
        /// The <c>System.Text.Encoding</c> to use when reading in the zip archive. Be
        /// careful specifying the encoding.  If the value you use here is not the same
        /// as the Encoding used when the zip archive was created (possibly by a
        /// different archiver) you will get unexpected results and possibly exceptions.
        /// </param>
        /// 
        /// <returns>The instance read from the zip archive.</returns>
        private static ZipFile Read(string fileName, TextWriter statusMessageWriter, Encoding encoding, EventHandler<ReadProgressEventArgs> readProgress)
        {
            ZipFile zf = new ZipFile();
            zf.AlternateEncoding = encoding ?? DefaultEncoding;
            zf.AlternateEncodingUsage = ZipOption.Always;
            zf._StatusMessageTextWriter = statusMessageWriter;
            zf._name = fileName;
            if (readProgress != null)
            {
                zf.ReadProgress = readProgress;
            }
            if (zf.Verbose)
            {
                zf._StatusMessageTextWriter.WriteLine("reading from {0}...", fileName);
            }
            ReadIntoInstance(zf);
            zf._fileAlreadyExists = true;
            return zf;
        }

        private static void ReadCentralDirectory(ZipFile zf)
        {
            ZipEntry entry;
            bool flag = false;
            Dictionary<string, object> previouslySeen = new Dictionary<string, object>();
            while ((entry = ZipEntry.ReadDirEntry(zf, previouslySeen)) != null)
            {
                entry.ResetDirEntry();
                zf.OnReadEntry(true, null);
                if (zf.Verbose)
                {
                    zf.StatusMessageTextWriter.WriteLine("entry {0}", entry.FileName);
                }
                zf._entries.Add(entry.FileName, entry);
                if (entry._InputUsesZip64)
                {
                    flag = true;
                }
                previouslySeen.Add(entry.FileName, null);
            }
            if (flag)
            {
                zf.UseZip64WhenSaving = Zip64Option.Always;
            }
            if (zf._locEndOfCDS > 0L)
            {
                zf.ReadStream.Seek(zf._locEndOfCDS, SeekOrigin.Begin);
            }
            ReadCentralDirectoryFooter(zf);
            if (!(!zf.Verbose || string.IsNullOrEmpty(zf.Comment)))
            {
                zf.StatusMessageTextWriter.WriteLine("Zip file Comment: {0}", zf.Comment);
            }
            if (zf.Verbose)
            {
                zf.StatusMessageTextWriter.WriteLine("read in {0} entries.", zf._entries.Count);
            }
            zf.OnReadCompleted();
        }

        private static void ReadCentralDirectoryFooter(ZipFile zf)
        {
            Stream readStream = zf.ReadStream;
            int num = SharedUtilities.ReadSignature(readStream);
            byte[] buffer = null;
            int startIndex = 0;
            if (num == 0x6064b50L)
            {
                buffer = new byte[0x34];
                readStream.Read(buffer, 0, buffer.Length);
                long num3 = BitConverter.ToInt64(buffer, 0);
                if (num3 < 0x2cL)
                {
                    throw new ZipException("Bad size in the ZIP64 Central Directory.");
                }
                zf._versionMadeBy = BitConverter.ToUInt16(buffer, startIndex);
                startIndex += 2;
                zf._versionNeededToExtract = BitConverter.ToUInt16(buffer, startIndex);
                startIndex += 2;
                zf._diskNumberWithCd = BitConverter.ToUInt32(buffer, startIndex);
                startIndex += 2;
                buffer = new byte[num3 - 0x2cL];
                readStream.Read(buffer, 0, buffer.Length);
                if (SharedUtilities.ReadSignature(readStream) != 0x7064b50L)
                {
                    throw new ZipException("Inconsistent metadata in the ZIP64 Central Directory.");
                }
                buffer = new byte[0x10];
                readStream.Read(buffer, 0, buffer.Length);
                num = SharedUtilities.ReadSignature(readStream);
            }
            if (num != 0x6054b50L)
            {
                readStream.Seek(-4L, SeekOrigin.Current);
                throw new BadReadException(string.Format(CultureInfo.InvariantCulture, "Bad signature ({0:X8}) at position 0x{1:X8}", num, readStream.Position));
            }
            buffer = new byte[0x10];
            zf.ReadStream.Read(buffer, 0, buffer.Length);
            if (zf._diskNumberWithCd == 0)
            {
                zf._diskNumberWithCd = BitConverter.ToUInt16(buffer, 2);
            }
            ReadZipFileComment(zf);
        }

        private static uint ReadFirstFourBytes(Stream s)
        {
            return (uint) SharedUtilities.ReadInt(s);
        }

        private static void ReadIntoInstance(ZipFile zf)
        {
            Stream readStream = zf.ReadStream;
            try
            {
                zf._readName = zf._name;
                if (!readStream.CanSeek)
                {
                    ReadIntoInstance_Orig(zf);
                    return;
                }
                zf.OnReadStarted();
                if (ReadFirstFourBytes(readStream) == 0x6054b50)
                {
                    return;
                }
                int num2 = 0;
                bool flag = false;
                long offset = readStream.Length - 0x40L;
                long num4 = Math.Max((long) (readStream.Length - 0x4000L), (long) 10L);
                do
                {
                    if (offset < 0L)
                    {
                        offset = 0L;
                    }
                    readStream.Seek(offset, SeekOrigin.Begin);
                    if (SharedUtilities.FindSignature(readStream, 0x6054b50) != -1L)
                    {
                        flag = true;
                    }
                    else
                    {
                        if (offset == 0L)
                        {
                            break;
                        }
                        num2++;
                        offset -= (0x20 * (num2 + 1)) * num2;
                    }
                }
                while (!flag && (offset > num4));
                if (flag)
                {
                    zf._locEndOfCDS = readStream.Position - 4L;
                    byte[] buffer = new byte[0x10];
                    readStream.Read(buffer, 0, buffer.Length);
                    zf._diskNumberWithCd = BitConverter.ToUInt16(buffer, 2);
                    if (zf._diskNumberWithCd == 0xffff)
                    {
                        throw new ZipException("Spanned archives with more than 65534 segments are not supported at this time.");
                    }
                    zf._diskNumberWithCd++;
                    int startIndex = 12;
                    uint num7 = BitConverter.ToUInt32(buffer, startIndex);
                    if (num7 == uint.MaxValue)
                    {
                        Zip64SeekToCentralDirectory(zf);
                    }
                    else
                    {
                        zf._OffsetOfCentralDirectory = num7;
                        readStream.Seek((long) num7, SeekOrigin.Begin);
                    }
                    ReadCentralDirectory(zf);
                }
                else
                {
                    readStream.Seek(0L, SeekOrigin.Begin);
                    ReadIntoInstance_Orig(zf);
                }
            }
            catch (Exception exception)
            {
                if (zf._ReadStreamIsOurs && (zf._readstream != null))
                {
                    try
                    {
                        zf._readstream.Dispose();
                        zf._readstream = null;
                    }
                    finally
                    {
                    }
                }
                throw new ZipException("Cannot read that as a ZipFile", exception);
            }
            zf._contentsChanged = false;
        }

        private static void ReadIntoInstance_Orig(ZipFile zf)
        {
            ZipEntry entry;
            zf.OnReadStarted();
            zf._entries = new Dictionary<string, ZipEntry>();
            if (zf.Verbose)
            {
                if (zf.Name == null)
                {
                    zf.StatusMessageTextWriter.WriteLine("Reading zip from stream...");
                }
                else
                {
                    zf.StatusMessageTextWriter.WriteLine("Reading zip {0}...", zf.Name);
                }
            }
            bool first = true;
            ZipContainer zc = new ZipContainer(zf);
            while ((entry = ZipEntry.ReadEntry(zc, first)) != null)
            {
                if (zf.Verbose)
                {
                    zf.StatusMessageTextWriter.WriteLine("  {0}", entry.FileName);
                }
                zf._entries.Add(entry.FileName, entry);
                first = false;
            }
            try
            {
                ZipEntry entry2;
                Dictionary<string, object> previouslySeen = new Dictionary<string, object>();
                while ((entry2 = ZipEntry.ReadDirEntry(zf, previouslySeen)) != null)
                {
                    ZipEntry entry3 = zf._entries[entry2.FileName];
                    if (entry3 != null)
                    {
                        entry3._Comment = entry2.Comment;
                        if (entry2.IsDirectory)
                        {
                            entry3.MarkAsDirectory();
                        }
                    }
                    previouslySeen.Add(entry2.FileName, null);
                }
                if (zf._locEndOfCDS > 0L)
                {
                    zf.ReadStream.Seek(zf._locEndOfCDS, SeekOrigin.Begin);
                }
                ReadCentralDirectoryFooter(zf);
                if (!(!zf.Verbose || string.IsNullOrEmpty(zf.Comment)))
                {
                    zf.StatusMessageTextWriter.WriteLine("Zip file Comment: {0}", zf.Comment);
                }
            }
            catch (ZipException)
            {
            }
            catch (IOException)
            {
            }
            zf.OnReadCompleted();
        }

        private static void ReadZipFileComment(ZipFile zf)
        {
            byte[] buffer = new byte[2];
            zf.ReadStream.Read(buffer, 0, buffer.Length);
            short num = (short) (buffer[0] + (buffer[1] * 0x100));
            if (num > 0)
            {
                buffer = new byte[num];
                zf.ReadStream.Read(buffer, 0, buffer.Length);
                string str = zf.AlternateEncoding.GetString(buffer, 0, buffer.Length);
                zf.Comment = str;
            }
        }

        /// <summary>
        /// This method removes a collection of entries from the <c>ZipFile</c>.
        /// </summary>
        /// 
        /// <param name="entriesToRemove">
        /// A collection of ZipEntry instances from this zip file to be removed. For
        /// example, you can pass in an array of ZipEntry instances; or you can call
        /// SelectEntries(), and then add or remove entries from that
        /// ICollection&lt;ZipEntry&gt; (ICollection(Of ZipEntry) in VB), and pass
        /// that ICollection to this method.
        /// </param>
        /// 
        /// <seealso cref="M:DotNetZipAdditionalPlatforms.Zip.ZipFile.SelectEntries(System.String)" />
        /// <seealso cref="M:DotNetZipAdditionalPlatforms.Zip.ZipFile.RemoveSelectedEntries(System.String)" />
        public void RemoveEntries(ICollection<ZipEntry> entriesToRemove)
        {
            if (entriesToRemove == null)
            {
                throw new ArgumentNullException("entriesToRemove");
            }
            foreach (ZipEntry entry in entriesToRemove)
            {
                this.RemoveEntry(entry);
            }
        }

        /// <summary>
        /// This method removes a collection of entries from the <c>ZipFile</c>, by name.
        /// </summary>
        /// 
        /// <param name="entriesToRemove">
        /// A collection of strings that refer to names of entries to be removed
        /// from the <c>ZipFile</c>.  For example, you can pass in an array or a
        /// List of Strings that provide the names of entries to be removed.
        /// </param>
        /// 
        /// <seealso cref="M:DotNetZipAdditionalPlatforms.Zip.ZipFile.SelectEntries(System.String)" />
        /// <seealso cref="M:DotNetZipAdditionalPlatforms.Zip.ZipFile.RemoveSelectedEntries(System.String)" />
        public void RemoveEntries(ICollection<string> entriesToRemove)
        {
            if (entriesToRemove == null)
            {
                throw new ArgumentNullException("entriesToRemove");
            }
            foreach (string str in entriesToRemove)
            {
                this.RemoveEntry(str);
            }
        }

        /// <summary>
        /// Removes the given <c>ZipEntry</c> from the zip archive.
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// After calling <c>RemoveEntry</c>, the application must call <c>Save</c> to
        /// make the changes permanent.
        /// </para>
        /// </remarks>
        /// 
        /// <exception cref="T:System.ArgumentException">
        /// Thrown if the specified <c>ZipEntry</c> does not exist in the <c>ZipFile</c>.
        /// </exception>
        /// 
        /// <example>
        /// In this example, all entries in the zip archive dating from before
        /// December 31st, 2007, are removed from the archive.  This is actually much
        /// easier if you use the RemoveSelectedEntries method.  But I needed an
        /// example for RemoveEntry, so here it is.
        /// <code>
        /// String ZipFileToRead = "ArchiveToModify.zip";
        /// System.DateTime Threshold = new System.DateTime(2007,12,31);
        /// using (ZipFile zip = ZipFile.Read(ZipFileToRead))
        /// {
        /// var EntriesToRemove = new System.Collections.Generic.List&lt;ZipEntry&gt;();
        /// foreach (ZipEntry e in zip)
        /// {
        /// if (e.LastModified &lt; Threshold)
        /// {
        /// // We cannot remove the entry from the list, within the context of
        /// // an enumeration of said list.
        /// // So we add the doomed entry to a list to be removed later.
        /// EntriesToRemove.Add(e);
        /// }
        /// }
        /// 
        /// // actually remove the doomed entries.
        /// foreach (ZipEntry zombie in EntriesToRemove)
        /// zip.RemoveEntry(zombie);
        /// 
        /// zip.Comment= string.Format(CultureInfo.InvariantCulture, "This zip archive was updated at {0}.",
        /// System.DateTime.Now.ToString("G"));
        /// 
        /// // save with a different name
        /// zip.Save("Archive-Updated.zip");
        /// }
        /// </code>
        /// 
        /// <code lang="VB">
        /// Dim ZipFileToRead As String = "ArchiveToModify.zip"
        /// Dim Threshold As New DateTime(2007, 12, 31)
        /// Using zip As ZipFile = ZipFile.Read(ZipFileToRead)
        /// Dim EntriesToRemove As New System.Collections.Generic.List(Of ZipEntry)
        /// Dim e As ZipEntry
        /// For Each e In zip
        /// If (e.LastModified &lt; Threshold) Then
        /// ' We cannot remove the entry from the list, within the context of
        /// ' an enumeration of said list.
        /// ' So we add the doomed entry to a list to be removed later.
        /// EntriesToRemove.Add(e)
        /// End If
        /// Next
        /// 
        /// ' actually remove the doomed entries.
        /// Dim zombie As ZipEntry
        /// For Each zombie In EntriesToRemove
        /// zip.RemoveEntry(zombie)
        /// Next
        /// zip.Comment = string.Format(CultureInfo.InvariantCulture, "This zip archive was updated at {0}.", DateTime.Now.ToString("G"))
        /// 'save as a different name
        /// zip.Save("Archive-Updated.zip")
        /// End Using
        /// </code>
        /// </example>
        /// 
        /// <param name="entry">
        /// The <c>ZipEntry</c> to remove from the zip.
        /// </param>
        /// 
        /// <seealso cref="M:DotNetZipAdditionalPlatforms.Zip.ZipFile.RemoveSelectedEntries(System.String)" />
        public void RemoveEntry(ZipEntry entry)
        {
            if (entry == null)
            {
                throw new ArgumentNullException("entry");
            }
            this._entries.Remove(SharedUtilities.NormalizePathForUseInZipFile(entry.FileName));
            this._zipEntriesAsList = null;
            this._contentsChanged = true;
        }

        /// <summary>
        /// Removes the <c>ZipEntry</c> with the given filename from the zip archive.
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// After calling <c>RemoveEntry</c>, the application must call <c>Save</c> to
        /// make the changes permanent.
        /// </para>
        /// 
        /// </remarks>
        /// 
        /// <exception cref="T:System.InvalidOperationException">
        /// Thrown if the <c>ZipFile</c> is not updatable.
        /// </exception>
        /// 
        /// <exception cref="T:System.ArgumentException">
        /// Thrown if a <c>ZipEntry</c> with the specified filename does not exist in
        /// the <c>ZipFile</c>.
        /// </exception>
        /// 
        /// <example>
        /// 
        /// This example shows one way to remove an entry with a given filename from
        /// an existing zip archive.
        /// 
        /// <code>
        /// String zipFileToRead= "PackedDocuments.zip";
        /// string candidate = "DatedMaterial.xps";
        /// using (ZipFile zip = ZipFile.Read(zipFileToRead))
        /// {
        /// if (zip.EntryFilenames.Contains(candidate))
        /// {
        /// zip.RemoveEntry(candidate);
        /// zip.Comment= string.Format(CultureInfo.InvariantCulture, "The file '{0}' has been removed from this archive.",
        /// Candidate);
        /// zip.Save();
        /// }
        /// }
        /// </code>
        /// <code lang="VB">
        /// Dim zipFileToRead As String = "PackedDocuments.zip"
        /// Dim candidate As String = "DatedMaterial.xps"
        /// Using zip As ZipFile = ZipFile.Read(zipFileToRead)
        /// If zip.EntryFilenames.Contains(candidate) Then
        /// zip.RemoveEntry(candidate)
        /// zip.Comment = string.Format(CultureInfo.InvariantCulture, "The file '{0}' has been removed from this archive.", Candidate)
        /// zip.Save
        /// End If
        /// End Using
        /// </code>
        /// </example>
        /// 
        /// <param name="fileName">
        /// The name of the file, including any directory path, to remove from the zip.
        /// The filename match is not case-sensitive by default; you can use the
        /// <c>CaseSensitiveRetrieval</c> property to change this behavior. The
        /// pathname can use forward-slashes or backward slashes.
        /// </param>
        public void RemoveEntry(string fileName)
        {
            string str = ZipEntry.NameInArchive(fileName, null);
            ZipEntry entry = this[str];
            if (entry == null)
            {
                throw new ArgumentException("The entry you specified was not found in the zip archive.");
            }
            this.RemoveEntry(entry);
        }

        private void RemoveEntryForUpdate(string entryName)
        {
            if (string.IsNullOrEmpty(entryName))
            {
                throw new ArgumentNullException("entryName");
            }
            string directoryPathInArchive = null;
            if (entryName.IndexOf('\\') != -1)
            {
                directoryPathInArchive = Path.GetDirectoryName(entryName);
                entryName = Path.GetFileName(entryName);
            }
            string fileName = ZipEntry.NameInArchive(entryName, directoryPathInArchive);
            if (this[fileName] != null)
            {
                this.RemoveEntry(fileName);
            }
        }

        /// <summary>
        /// Remove entries from the zipfile by specified criteria.
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// This method allows callers to remove the collection of entries from the zipfile
        /// that fit the specified criteria.  The criteria are described in a string format, and
        /// can include patterns for the filename; constraints on the size of the entry;
        /// constraints on the last modified, created, or last accessed time for the file
        /// described by the entry; or the attributes of the entry.
        /// </para>
        /// 
        /// <para>
        /// For details on the syntax for the selectionCriteria parameter, see <see cref="M:DotNetZipAdditionalPlatforms.Zip.ZipFile.AddSelectedFiles(System.String)" />.
        /// </para>
        /// 
        /// <para>
        /// This method is intended for use with a ZipFile that has been read from storage.
        /// When creating a new ZipFile, this method will work only after the ZipArchive has
        /// been Saved to the disk (the ZipFile class subsequently and implicitly reads the Zip
        /// archive from storage.)  Calling SelectEntries on a ZipFile that has not yet been
        /// saved will deliver undefined results.
        /// </para>
        /// </remarks>
        /// 
        /// <exception cref="T:System.Exception">
        /// Thrown if selectionCriteria has an invalid syntax.
        /// </exception>
        /// 
        /// <example>
        /// This example removes all entries in a zip file that were modified prior to January 1st, 2008.
        /// <code>
        /// using (ZipFile zip1 = ZipFile.Read(ZipFileName))
        /// {
        /// // remove all entries from prior to Jan 1, 2008
        /// zip1.RemoveEntries("mtime &lt; 2008-01-01");
        /// // don't forget to save the archive!
        /// zip1.Save();
        /// }
        /// </code>
        /// <code lang="VB">
        /// Using zip As ZipFile = ZipFile.Read(ZipFileName)
        /// ' remove all entries from prior to Jan 1, 2008
        /// zip1.RemoveEntries("mtime &lt; 2008-01-01")
        /// ' do not forget to save the archive!
        /// zip1.Save
        /// End Using
        /// </code>
        /// </example>
        /// <param name="selectionCriteria">the string that specifies which entries to select</param>
        /// <returns>the number of entries removed</returns>
        public int RemoveSelectedEntries(string selectionCriteria)
        {
            ICollection<ZipEntry> entriesToRemove = this.SelectEntries(selectionCriteria);
            this.RemoveEntries(entriesToRemove);
            return entriesToRemove.Count;
        }

        /// <summary>
        /// Remove entries from the zipfile by specified criteria, and within the specified
        /// path in the archive.
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// This method allows callers to remove the collection of entries from the zipfile
        /// that fit the specified criteria.  The criteria are described in a string format, and
        /// can include patterns for the filename; constraints on the size of the entry;
        /// constraints on the last modified, created, or last accessed time for the file
        /// described by the entry; or the attributes of the entry.
        /// </para>
        /// 
        /// <para>
        /// For details on the syntax for the selectionCriteria parameter, see <see cref="M:DotNetZipAdditionalPlatforms.Zip.ZipFile.AddSelectedFiles(System.String)" />.
        /// </para>
        /// 
        /// <para>
        /// This method is intended for use with a ZipFile that has been read from storage.
        /// When creating a new ZipFile, this method will work only after the ZipArchive has
        /// been Saved to the disk (the ZipFile class subsequently and implicitly reads the Zip
        /// archive from storage.)  Calling SelectEntries on a ZipFile that has not yet been
        /// saved will deliver undefined results.
        /// </para>
        /// </remarks>
        /// 
        /// <exception cref="T:System.Exception">
        /// Thrown if selectionCriteria has an invalid syntax.
        /// </exception>
        /// 
        /// <example>
        /// <code>
        /// using (ZipFile zip1 = ZipFile.Read(ZipFileName))
        /// {
        /// // remove all entries from prior to Jan 1, 2008
        /// zip1.RemoveEntries("mtime &lt; 2008-01-01", "documents");
        /// // a call to ZipFile.Save will make the modifications permanent
        /// zip1.Save();
        /// }
        /// </code>
        /// <code lang="VB">
        /// Using zip As ZipFile = ZipFile.Read(ZipFileName)
        /// ' remove all entries from prior to Jan 1, 2008
        /// zip1.RemoveEntries("mtime &lt; 2008-01-01", "documents")
        /// ' a call to ZipFile.Save will make the modifications permanent
        /// zip1.Save
        /// End Using
        /// </code>
        /// </example>
        /// 
        /// <param name="selectionCriteria">the string that specifies which entries to select</param>
        /// <param name="directoryPathInArchive">
        /// the directory in the archive from which to select entries. If null, then
        /// all directories in the archive are used.
        /// </param>
        /// <returns>the number of entries removed</returns>
        public int RemoveSelectedEntries(string selectionCriteria, string directoryPathInArchive)
        {
            ICollection<ZipEntry> entriesToRemove = this.SelectEntries(selectionCriteria, directoryPathInArchive);
            this.RemoveEntries(entriesToRemove);
            return entriesToRemove.Count;
        }

        private void RemoveTempFile()
        {
            try
            {
                if (File.Exists(this._temporaryFileName))
                {
                    File.Delete(this._temporaryFileName);
                }
            }
            catch (IOException exception)
            {
                if (this.Verbose)
                {
                    this.StatusMessageTextWriter.WriteLine("ZipFile::Save: could not delete temp file: {0}.", exception.Message);
                }
            }
        }

        private static string ReplaceLeadingDirectory(string original, string pattern, string replacement)
        {
            string str = original.ToUpper();
            string str2 = pattern.ToUpper();
            if (str.IndexOf(str2) != 0)
            {
                return original;
            }
            return (replacement + original.Substring(str2.Length));
        }

        internal void Reset(bool whileSaving)
        {
            if (this._JustSaved)
            {
                using (ZipFile file = new ZipFile())
                {
                    file._readName = file._name = whileSaving ? (this._readName ?? this._name) : this._name;
                    file.AlternateEncoding = this.AlternateEncoding;
                    file.AlternateEncodingUsage = this.AlternateEncodingUsage;
                    ReadIntoInstance(file);
                    foreach (ZipEntry entry in file)
                    {
                        foreach (ZipEntry entry2 in this)
                        {
                            if (entry.FileName == entry2.FileName)
                            {
                                entry2.CopyMetaData(entry);
                                break;
                            }
                        }
                    }
                }
                this._JustSaved = false;
            }
        }

        /// <summary>
        /// Saves the Zip archive to a file, specified by the Name property of the
        /// <c>ZipFile</c>.
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// The <c>ZipFile</c> instance is written to storage, typically a zip file
        /// in a filesystem, only when the caller calls <c>Save</c>.  In the typical
        /// case, the Save operation writes the zip content to a temporary file, and
        /// then renames the temporary file to the desired name. If necessary, this
        /// method will delete a pre-existing file before the rename.
        /// </para>
        /// 
        /// <para>
        /// The <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.Name" /> property is specified either explicitly,
        /// or implicitly using one of the parameterized ZipFile constructors.  For
        /// COM Automation clients, the <c>Name</c> property must be set explicitly,
        /// because COM Automation clients cannot call parameterized constructors.
        /// </para>
        /// 
        /// <para>
        /// When using a filesystem file for the Zip output, it is possible to call
        /// <c>Save</c> multiple times on the <c>ZipFile</c> instance. With each
        /// call the zip content is re-written to the same output file.
        /// </para>
        /// 
        /// <para>
        /// Data for entries that have been added to the <c>ZipFile</c> instance is
        /// written to the output when the <c>Save</c> method is called. This means
        /// that the input streams for those entries must be available at the time
        /// the application calls <c>Save</c>.  If, for example, the application
        /// adds entries with <c>AddEntry</c> using a dynamically-allocated
        /// <c>MemoryStream</c>, the memory stream must not have been disposed
        /// before the call to <c>Save</c>. See the <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipEntry.InputStream" /> property for more discussion of the
        /// availability requirements of the input stream for an entry, and an
        /// approach for providing just-in-time stream lifecycle management.
        /// </para>
        /// 
        /// </remarks>
        /// 
        /// <seealso cref="M:DotNetZipAdditionalPlatforms.Zip.ZipFile.AddEntry(System.String,System.IO.Stream)" />
        /// 
        /// <exception cref="T:DotNetZipAdditionalPlatforms.Zip.BadStateException">
        /// Thrown if you haven't specified a location or stream for saving the zip,
        /// either in the constructor or by setting the Name property, or if you try
        /// to save a regular zip archive to a filename with a .exe extension.
        /// </exception>
        /// 
        /// <exception cref="T:System.OverflowException">
        /// Thrown if <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.MaxOutputSegmentSize" /> is non-zero, and the number
        /// of segments that would be generated for the spanned zip file during the
        /// save operation exceeds 99.  If this happens, you need to increase the
        /// segment size.
        /// </exception>
        public void Save()
        {
            try
            {
                bool flag = false;
                this._saveOperationCanceled = false;
                this._numberOfSegmentsForMostRecentSave = 0;
                this.OnSaveStarted();
                if (this.WriteStream == null)
                {
                    throw new BadStateException("You haven't specified where to save the zip.");
                }
                if (!(((this._name == null) || !this._name.EndsWith(".exe")) || this._SavingSfx))
                {
                    throw new BadStateException("You specified an EXE for a plain zip file.");
                }
                if (!this._contentsChanged)
                {
                    this.OnSaveCompleted();
                    if (this.Verbose)
                    {
                        this.StatusMessageTextWriter.WriteLine("No save is necessary....");
                    }
                }
                else
                {
                    this.Reset(true);
                    if (this.Verbose)
                    {
                        this.StatusMessageTextWriter.WriteLine("saving....");
                    }
                    if ((this._entries.Count >= 0xffff) && (this._zip64 == Zip64Option.Default))
                    {
                        throw new ZipException("The number of entries is 65535 or greater. Consider setting the UseZip64WhenSaving property on the ZipFile instance.");
                    }
                    int current = 0;
                    ICollection<ZipEntry> entries = this.SortEntriesBeforeSaving ? this.EntriesSorted : this.Entries;
                    foreach (ZipEntry entry in entries)
                    {
                        this.OnSaveEntry(current, entry, true);
                        entry.Write(this.WriteStream);
                        if (this._saveOperationCanceled)
                        {
                            break;
                        }
                        current++;
                        this.OnSaveEntry(current, entry, false);
                        if (this._saveOperationCanceled)
                        {
                            break;
                        }
                        if (entry.IncludedInMostRecentSave)
                        {
                            flag |= entry.OutputUsedZip64.Value;
                        }
                    }
                    if (!this._saveOperationCanceled)
                    {
                        ZipSegmentedStream writeStream = this.WriteStream as ZipSegmentedStream;
                        this._numberOfSegmentsForMostRecentSave = (writeStream != null) ? writeStream.CurrentSegment : 1;
                        bool flag2 = ZipOutput.WriteCentralDirectoryStructure(this.WriteStream, entries, this._numberOfSegmentsForMostRecentSave, this._zip64, this.Comment, new ZipContainer(this));
                        this.OnSaveEvent(ZipProgressEventType.Saving_AfterSaveTempArchive);
                        this._hasBeenSaved = true;
                        this._contentsChanged = false;
                        flag |= flag2;
                        this._OutputUsesZip64 = new bool?(flag);
                        if ((this._name != null) && ((this._temporaryFileName != null) || (writeStream != null)))
                        {
                            this.WriteStream.Dispose();
                            if (this._saveOperationCanceled)
                            {
                                return;
                            }
                            if (this._fileAlreadyExists && (this._readstream != null))
                            {
                                this._readstream.Close();
                                this._readstream = null;
                                foreach (ZipEntry entry in entries)
                                {
                                    ZipSegmentedStream stream2 = entry._archiveStream as ZipSegmentedStream;
                                    if (stream2 != null)
                                    {
                                        stream2.Dispose();
                                    }
                                    entry._archiveStream = null;
                                }
                            }
                            string path = null;
                            if (File.Exists(this._name))
                            {
                                path = this._name + "." + Path.GetRandomFileName();
                                if (File.Exists(path))
                                {
                                    this.DeleteFileWithRetry(path);
                                }
                                File.Move(this._name, path);
                            }
                            this.OnSaveEvent(ZipProgressEventType.Saving_BeforeRenameTempArchive);
                            File.Move((writeStream != null) ? writeStream.CurrentTempName : this._temporaryFileName, this._name);
                            this.OnSaveEvent(ZipProgressEventType.Saving_AfterRenameTempArchive);
                            if (path != null)
                            {
                                try
                                {
                                    if (File.Exists(path))
                                    {
                                        File.Delete(path);
                                    }
                                }
                                catch
                                {
                                }
                            }
                            this._fileAlreadyExists = true;
                        }
                        NotifyEntriesSaveComplete(entries);
                        this.OnSaveCompleted();
                        this._JustSaved = true;
                    }
                }
            }
            finally
            {
                this.CleanupAfterSaveOperation();
            }
        }

        /// <summary>
        /// Save the zip archive to the specified stream.
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// The <c>ZipFile</c> instance is written to storage - typically a zip file
        /// in a filesystem, but using this overload, the storage can be anything
        /// accessible via a writable stream - only when the caller calls <c>Save</c>.
        /// </para>
        /// 
        /// <para>
        /// Use this method to save the zip content to a stream directly.  A common
        /// scenario is an ASP.NET application that dynamically generates a zip file
        /// and allows the browser to download it. The application can call
        /// <c>Save(Response.OutputStream)</c> to write a zipfile directly to the
        /// output stream, without creating a zip file on the disk on the ASP.NET
        /// server.
        /// </para>
        /// 
        /// <para>
        /// Be careful when saving a file to a non-seekable stream, including
        /// <c>Response.OutputStream</c>. When DotNetZip writes to a non-seekable
        /// stream, the zip archive is formatted in such a way that may not be
        /// compatible with all zip tools on all platforms.  It's a perfectly legal
        /// and compliant zip file, but some people have reported problems opening
        /// files produced this way using the Mac OS archive utility.
        /// </para>
        /// 
        /// </remarks>
        /// 
        /// <example>
        /// 
        /// This example saves the zipfile content into a MemoryStream, and
        /// then gets the array of bytes from that MemoryStream.
        /// 
        /// <code lang="C#">
        /// using (var zip = new DotNetZipAdditionalPlatforms.Zip.ZipFile())
        /// {
        /// zip.CompressionLevel= DotNetZipAdditionalPlatforms.Zlib.CompressionLevel.BestCompression;
        /// zip.Password = "VerySecret.";
        /// zip.Encryption = EncryptionAlgorithm.WinZipAes128;
        /// zip.AddFile(sourceFileName);
        /// MemoryStream output = new MemoryStream();
        /// zip.Save(output);
        /// 
        /// byte[] zipbytes = output.ToArray();
        /// }
        /// </code>
        /// </example>
        /// 
        /// <example>
        /// <para>
        /// This example shows a pitfall you should avoid. DO NOT read
        /// from a stream, then try to save to the same stream.  DO
        /// NOT DO THIS:
        /// </para>
        /// 
        /// <code lang="C#">
        /// using (var fs = new FileSteeam(filename, FileMode.Open))
        /// {
        /// using (var zip = DotNetZipAdditionalPlatforms.Zip.ZipFile.Read(inputStream))
        /// {
        /// zip.AddEntry("Name1.txt", "this is the content");
        /// zip.Save(inputStream);  // NO NO NO!!
        /// }
        /// }
        /// </code>
        /// 
        /// <para>
        /// Better like this:
        /// </para>
        /// 
        /// <code lang="C#">
        /// using (var zip = DotNetZipAdditionalPlatforms.Zip.ZipFile.Read(filename))
        /// {
        /// zip.AddEntry("Name1.txt", "this is the content");
        /// zip.Save();  // YES!
        /// }
        /// </code>
        /// 
        /// </example>
        /// 
        /// <param name="outputStream">
        /// The <c>System.IO.Stream</c> to write to. It must be
        /// writable. If you created the ZipFile instanct by calling
        /// ZipFile.Read(), this stream must not be the same stream
        /// you passed to ZipFile.Read().
        /// </param>
        public void Save(Stream outputStream)
        {
            if (outputStream == null)
            {
                throw new ArgumentNullException("outputStream");
            }
            if (!outputStream.CanWrite)
            {
                throw new ArgumentException("Must be a writable stream.", "outputStream");
            }
            this._name = null;
            this._writestream = new CountingStream(outputStream);
            this._contentsChanged = true;
            this._fileAlreadyExists = false;
            this.Save();
        }

        /// <summary>
        /// Save the file to a new zipfile, with the given name.
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// This method allows the application to explicitly specify the name of the zip
        /// file when saving. Use this when creating a new zip file, or when
        /// updating a zip archive.
        /// </para>
        /// 
        /// <para>
        /// An application can also save a zip archive in several places by calling this
        /// method multiple times in succession, with different filenames.
        /// </para>
        /// 
        /// <para>
        /// The <c>ZipFile</c> instance is written to storage, typically a zip file in a
        /// filesystem, only when the caller calls <c>Save</c>.  The Save operation writes
        /// the zip content to a temporary file, and then renames the temporary file
        /// to the desired name. If necessary, this method will delete a pre-existing file
        /// before the rename.
        /// </para>
        /// 
        /// </remarks>
        /// 
        /// <exception cref="T:System.ArgumentException">
        /// Thrown if you specify a directory for the filename.
        /// </exception>
        /// 
        /// <param name="fileName">
        /// The name of the zip archive to save to. Existing files will
        /// be overwritten with great prejudice.
        /// </param>
        /// 
        /// <example>
        /// This example shows how to create and Save a zip file.
        /// <code>
        /// using (ZipFile zip = new ZipFile())
        /// {
        /// zip.AddDirectory(@"c:\reports\January");
        /// zip.Save("January.zip");
        /// }
        /// </code>
        /// 
        /// <code lang="VB">
        /// Using zip As New ZipFile()
        /// zip.AddDirectory("c:\reports\January")
        /// zip.Save("January.zip")
        /// End Using
        /// </code>
        /// 
        /// </example>
        /// 
        /// <example>
        /// This example shows how to update a zip file.
        /// <code>
        /// using (ZipFile zip = ZipFile.Read("ExistingArchive.zip"))
        /// {
        /// zip.AddFile("NewData.csv");
        /// zip.Save("UpdatedArchive.zip");
        /// }
        /// </code>
        /// 
        /// <code lang="VB">
        /// Using zip As ZipFile = ZipFile.Read("ExistingArchive.zip")
        /// zip.AddFile("NewData.csv")
        /// zip.Save("UpdatedArchive.zip")
        /// End Using
        /// </code>
        /// 
        /// </example>
        public void Save(string fileName)
        {
            if (this._name == null)
            {
                this._writestream = null;
            }
            else
            {
                this._readName = this._name;
            }
            this._name = fileName;
            if (Directory.Exists(this._name))
            {
                throw new ZipException("Bad Directory", new ArgumentException("That name specifies an existing directory. Please specify a filename.", "fileName"));
            }
            this._contentsChanged = true;
            this._fileAlreadyExists = File.Exists(this._name);
            this.Save();
        }

        /// <summary>
        /// Saves the ZipFile instance to a self-extracting zip archive.
        /// </summary>
        /// 
        /// <remarks>
        /// 
        /// <para>
        /// The generated exe image will execute on any machine that has the .NET
        /// Framework 2.0 installed on it.  The generated exe image is also a
        /// valid ZIP file, readable with DotNetZip or another Zip library or tool
        /// such as WinZip.
        /// </para>
        /// 
        /// <para>
        /// There are two "flavors" of self-extracting archive.  The
        /// <c>WinFormsApplication</c> version will pop up a GUI and allow the
        /// user to select a target directory into which to extract. There's also
        /// a checkbox allowing the user to specify to overwrite existing files,
        /// and another checkbox to allow the user to request that Explorer be
        /// opened to see the extracted files after extraction.  The other flavor
        /// is <c>ConsoleApplication</c>.  A self-extractor generated with that
        /// flavor setting will run from the command line. It accepts command-line
        /// options to set the overwrite behavior, and to specify the target
        /// extraction directory.
        /// </para>
        /// 
        /// <para>
        /// There are a few temporary files created during the saving to a
        /// self-extracting zip.  These files are created in the directory pointed
        /// to by <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.TempFileFolder" />, which defaults to <see cref="M:System.IO.Path.GetTempPath" />.  These temporary files are
        /// removed upon successful completion of this method.
        /// </para>
        /// 
        /// <para>
        /// When a user runs the WinForms SFX, the user's personal directory (<see cref="F:System.Environment.SpecialFolder.Personal">Environment.SpecialFolder.Personal</see>)
        /// will be used as the default extract location.  If you want to set the
        /// default extract location, you should use the other overload of
        /// <c>SaveSelfExtractor()</c>/ The user who runs the SFX will have the
        /// opportunity to change the extract directory before extracting. When
        /// the user runs the Command-Line SFX, the user must explicitly specify
        /// the directory to which to extract.  The .NET Framework 2.0 is required
        /// on the computer when the self-extracting archive is run.
        /// </para>
        /// 
        /// <para>
        /// NB: This method is not available in the version of DotNetZip build for
        /// the .NET Compact Framework, nor in the "Reduced" DotNetZip library.
        /// </para>
        /// 
        /// </remarks>
        /// 
        /// <example>
        /// <code>
        /// string DirectoryPath = "c:\\Documents\\Project7";
        /// using (ZipFile zip = new ZipFile())
        /// {
        /// zip.AddDirectory(DirectoryPath, System.IO.Path.GetFileName(DirectoryPath));
        /// zip.Comment = "This will be embedded into a self-extracting console-based exe";
        /// zip.SaveSelfExtractor("archive.exe", SelfExtractorFlavor.ConsoleApplication);
        /// }
        /// </code>
        /// <code lang="VB">
        /// Dim DirectoryPath As String = "c:\Documents\Project7"
        /// Using zip As New ZipFile()
        /// zip.AddDirectory(DirectoryPath, System.IO.Path.GetFileName(DirectoryPath))
        /// zip.Comment = "This will be embedded into a self-extracting console-based exe"
        /// zip.SaveSelfExtractor("archive.exe", SelfExtractorFlavor.ConsoleApplication)
        /// End Using
        /// </code>
        /// </example>
        /// 
        /// <param name="exeToGenerate">
        /// a pathname, possibly fully qualified, to be created. Typically it
        /// will end in an .exe extension.</param>
        /// <param name="flavor">
        /// Indicates whether a Winforms or Console self-extractor is
        /// desired. </param>
        public void SaveSelfExtractor(string exeToGenerate, SelfExtractorFlavor flavor)
        {
            SelfExtractorSaveOptions options = new SelfExtractorSaveOptions();
            options.Flavor = flavor;
            this.SaveSelfExtractor(exeToGenerate, options);
        }

        /// <summary>
        /// Saves the ZipFile instance to a self-extracting zip archive, using
        /// the specified save options.
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// This method saves a self extracting archive, using the specified save
        /// options. These options include the flavor of the SFX, the default extract
        /// directory, the icon file, and so on.  See the documentation
        /// for <see cref="M:DotNetZipAdditionalPlatforms.Zip.ZipFile.SaveSelfExtractor(System.String,DotNetZipAdditionalPlatforms.Zip.SelfExtractorFlavor)" /> for more
        /// details.
        /// </para>
        /// 
        /// <para>
        /// The user who runs the SFX will have the opportunity to change the extract
        /// directory before extracting. If at the time of extraction, the specified
        /// directory does not exist, the SFX will create the directory before
        /// extracting the files.
        /// </para>
        /// 
        /// </remarks>
        /// 
        /// <example>
        /// This example saves a WinForms-based self-extracting archive EXE that
        /// will use c:\ExtractHere as the default extract location. The C# code
        /// shows syntax for .NET 3.0, which uses an object initializer for
        /// the SelfExtractorOptions object.
        /// <code>
        /// string DirectoryPath = "c:\\Documents\\Project7";
        /// using (ZipFile zip = new ZipFile())
        /// {
        /// zip.AddDirectory(DirectoryPath, System.IO.Path.GetFileName(DirectoryPath));
        /// zip.Comment = "This will be embedded into a self-extracting WinForms-based exe";
        /// var options = new SelfExtractorOptions
        /// {
        /// Flavor = SelfExtractorFlavor.WinFormsApplication,
        /// DefaultExtractDirectory = "%USERPROFILE%\\ExtractHere",
        /// PostExtractCommandLine = ExeToRunAfterExtract,
        /// SfxExeWindowTitle = "My Custom Window Title",
        /// RemoveUnpackedFilesAfterExecute = true
        /// };
        /// zip.SaveSelfExtractor("archive.exe", options);
        /// }
        /// </code>
        /// <code lang="VB">
        /// Dim DirectoryPath As String = "c:\Documents\Project7"
        /// Using zip As New ZipFile()
        /// zip.AddDirectory(DirectoryPath, System.IO.Path.GetFileName(DirectoryPath))
        /// zip.Comment = "This will be embedded into a self-extracting console-based exe"
        /// Dim options As New SelfExtractorOptions()
        /// options.Flavor = SelfExtractorFlavor.WinFormsApplication
        /// options.DefaultExtractDirectory = "%USERPROFILE%\\ExtractHere"
        /// options.PostExtractCommandLine = ExeToRunAfterExtract
        /// options.SfxExeWindowTitle = "My Custom Window Title"
        /// options.RemoveUnpackedFilesAfterExecute = True
        /// zip.SaveSelfExtractor("archive.exe", options)
        /// End Using
        /// </code>
        /// </example>
        /// 
        /// <param name="exeToGenerate">The name of the EXE to generate.</param>
        /// <param name="options">provides the options for creating the
        /// Self-extracting archive.</param>
        public void SaveSelfExtractor(string exeToGenerate, SelfExtractorSaveOptions options)
        {
            if (this._name == null)
            {
                this._writestream = null;
            }
            this._SavingSfx = true;
            this._name = exeToGenerate;
            if (Directory.Exists(this._name))
            {
                throw new ZipException("Bad Directory", new ArgumentException("That name specifies an existing directory. Please specify a filename.", "exeToGenerate"));
            }
            this._contentsChanged = true;
            this._fileAlreadyExists = File.Exists(this._name);
            this._SaveSfxStub(exeToGenerate, options);
            this.Save();
            this._SavingSfx = false;
        }

        /// <summary>
        /// Retrieve entries from the zipfile by specified criteria.
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// This method allows callers to retrieve the collection of entries from the zipfile
        /// that fit the specified criteria.  The criteria are described in a string format, and
        /// can include patterns for the filename; constraints on the size of the entry;
        /// constraints on the last modified, created, or last accessed time for the file
        /// described by the entry; or the attributes of the entry.
        /// </para>
        /// 
        /// <para>
        /// For details on the syntax for the selectionCriteria parameter, see <see cref="M:DotNetZipAdditionalPlatforms.Zip.ZipFile.AddSelectedFiles(System.String)" />.
        /// </para>
        /// 
        /// <para>
        /// This method is intended for use with a ZipFile that has been read from storage.
        /// When creating a new ZipFile, this method will work only after the ZipArchive has
        /// been Saved to the disk (the ZipFile class subsequently and implicitly reads the Zip
        /// archive from storage.)  Calling SelectEntries on a ZipFile that has not yet been
        /// saved will deliver undefined results.
        /// </para>
        /// </remarks>
        /// 
        /// <exception cref="T:System.Exception">
        /// Thrown if selectionCriteria has an invalid syntax.
        /// </exception>
        /// 
        /// <example>
        /// This example selects all the PhotoShop files from within an archive, and extracts them
        /// to the current working directory.
        /// <code>
        /// using (ZipFile zip1 = ZipFile.Read(ZipFileName))
        /// {
        /// var PhotoShopFiles = zip1.SelectEntries("*.psd");
        /// foreach (ZipEntry psd in PhotoShopFiles)
        /// {
        /// psd.Extract();
        /// }
        /// }
        /// </code>
        /// <code lang="VB">
        /// Using zip1 As ZipFile = ZipFile.Read(ZipFileName)
        /// Dim PhotoShopFiles as ICollection(Of ZipEntry)
        /// PhotoShopFiles = zip1.SelectEntries("*.psd")
        /// Dim psd As ZipEntry
        /// For Each psd In PhotoShopFiles
        /// psd.Extract
        /// Next
        /// End Using
        /// </code>
        /// </example>
        /// <param name="selectionCriteria">the string that specifies which entries to select</param>
        /// <returns>a collection of ZipEntry objects that conform to the inclusion spec</returns>
        public ICollection<ZipEntry> SelectEntries(string selectionCriteria)
        {
            FileSelector selector = new FileSelector(selectionCriteria, this.AddDirectoryWillTraverseReparsePoints);
            return selector.SelectEntries(this);
        }

        /// <summary>
        /// Retrieve entries from the zipfile by specified criteria.
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// This method allows callers to retrieve the collection of entries from the zipfile
        /// that fit the specified criteria.  The criteria are described in a string format, and
        /// can include patterns for the filename; constraints on the size of the entry;
        /// constraints on the last modified, created, or last accessed time for the file
        /// described by the entry; or the attributes of the entry.
        /// </para>
        /// 
        /// <para>
        /// For details on the syntax for the selectionCriteria parameter, see <see cref="M:DotNetZipAdditionalPlatforms.Zip.ZipFile.AddSelectedFiles(System.String)" />.
        /// </para>
        /// 
        /// <para>
        /// This method is intended for use with a ZipFile that has been read from storage.
        /// When creating a new ZipFile, this method will work only after the ZipArchive has
        /// been Saved to the disk (the ZipFile class subsequently and implicitly reads the Zip
        /// archive from storage.)  Calling SelectEntries on a ZipFile that has not yet been
        /// saved will deliver undefined results.
        /// </para>
        /// </remarks>
        /// 
        /// <exception cref="T:System.Exception">
        /// Thrown if selectionCriteria has an invalid syntax.
        /// </exception>
        /// 
        /// <example>
        /// <code>
        /// using (ZipFile zip1 = ZipFile.Read(ZipFileName))
        /// {
        /// var UpdatedPhotoShopFiles = zip1.SelectEntries("*.psd", "UpdatedFiles");
        /// foreach (ZipEntry e in UpdatedPhotoShopFiles)
        /// {
        /// // prompt for extract here
        /// if (WantExtract(e.FileName))
        /// e.Extract();
        /// }
        /// }
        /// </code>
        /// <code lang="VB">
        /// Using zip1 As ZipFile = ZipFile.Read(ZipFileName)
        /// Dim UpdatedPhotoShopFiles As ICollection(Of ZipEntry) = zip1.SelectEntries("*.psd", "UpdatedFiles")
        /// Dim e As ZipEntry
        /// For Each e In UpdatedPhotoShopFiles
        /// ' prompt for extract here
        /// If Me.WantExtract(e.FileName) Then
        /// e.Extract
        /// End If
        /// Next
        /// End Using
        /// </code>
        /// </example>
        /// <param name="selectionCriteria">the string that specifies which entries to select</param>
        /// 
        /// <param name="directoryPathInArchive">
        /// the directory in the archive from which to select entries. If null, then
        /// all directories in the archive are used.
        /// </param>
        /// 
        /// <returns>a collection of ZipEntry objects that conform to the inclusion spec</returns>
        public ICollection<ZipEntry> SelectEntries(string selectionCriteria, string directoryPathInArchive)
        {
            FileSelector selector = new FileSelector(selectionCriteria, this.AddDirectoryWillTraverseReparsePoints);
            return selector.SelectEntries(this, directoryPathInArchive);
        }

        internal Stream StreamForDiskNumber(uint diskNumber)
        {
            if (((diskNumber + 1) == this._diskNumberWithCd) || ((diskNumber == 0) && (this._diskNumberWithCd == 0)))
            {
                return this.ReadStream;
            }
            return ZipSegmentedStream.ForReading(this._readName ?? this._name, diskNumber, this._diskNumberWithCd);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        /// <summary>Provides a string representation of the instance.</summary>
        /// <returns>a string representation of the instance.</returns>
        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture, "ZipFile::{0}", this.Name);
        }

        /// <summary>
        /// Add or update a directory in a zip archive.
        /// </summary>
        /// 
        /// <remarks>
        /// If the specified directory does not exist in the archive, then this method
        /// is equivalent to calling <c>AddDirectory()</c>.  If the specified
        /// directory already exists in the archive, then this method updates any
        /// existing entries, and adds any new entries. Any entries that are in the
        /// zip archive but not in the specified directory, are left alone.  In other
        /// words, the contents of the zip file will be a union of the previous
        /// contents and the new files.
        /// </remarks>
        /// 
        /// <seealso cref="M:DotNetZipAdditionalPlatforms.Zip.ZipFile.UpdateFile(System.String)" />
        /// <seealso cref="M:DotNetZipAdditionalPlatforms.Zip.ZipFile.AddDirectory(System.String)" />
        /// <seealso cref="M:DotNetZipAdditionalPlatforms.Zip.ZipFile.UpdateItem(System.String)" />
        /// 
        /// <param name="directoryName">
        /// The path to the directory to be added to the zip archive, or updated in
        /// the zip archive.
        /// </param>
        /// 
        /// <returns>
        /// The <c>ZipEntry</c> corresponding to the Directory that was added or updated.
        /// </returns>
        public ZipEntry UpdateDirectory(string directoryName)
        {
            return this.UpdateDirectory(directoryName, null);
        }

        /// <summary>
        /// Add or update a directory in the zip archive at the specified root
        /// directory in the archive.
        /// </summary>
        /// 
        /// <remarks>
        /// If the specified directory does not exist in the archive, then this method
        /// is equivalent to calling <c>AddDirectory()</c>.  If the specified
        /// directory already exists in the archive, then this method updates any
        /// existing entries, and adds any new entries. Any entries that are in the
        /// zip archive but not in the specified directory, are left alone.  In other
        /// words, the contents of the zip file will be a union of the previous
        /// contents and the new files.
        /// </remarks>
        /// 
        /// <seealso cref="M:DotNetZipAdditionalPlatforms.Zip.ZipFile.UpdateFile(System.String,System.String)" />
        /// <seealso cref="M:DotNetZipAdditionalPlatforms.Zip.ZipFile.AddDirectory(System.String,System.String)" />
        /// <seealso cref="M:DotNetZipAdditionalPlatforms.Zip.ZipFile.UpdateItem(System.String,System.String)" />
        /// 
        /// <param name="directoryName">
        /// The path to the directory to be added to the zip archive, or updated
        /// in the zip archive.
        /// </param>
        /// 
        /// <param name="directoryPathInArchive">
        /// Specifies a directory path to use to override any path in the
        /// <c>directoryName</c>.  This path may, or may not, correspond to a real
        /// directory in the current filesystem.  If the files within the zip are
        /// later extracted, this is the path used for the extracted file.  Passing
        /// <c>null</c> (<c>Nothing</c> in VB) will use the path on the
        /// <c>directoryName</c>, if any.  Passing the empty string ("") will insert
        /// the item at the root path within the archive.
        /// </param>
        /// 
        /// <returns>
        /// The <c>ZipEntry</c> corresponding to the Directory that was added or updated.
        /// </returns>
        public ZipEntry UpdateDirectory(string directoryName, string directoryPathInArchive)
        {
            return this.AddOrUpdateDirectoryImpl(directoryName, directoryPathInArchive, AddOrUpdateAction.AddOrUpdate);
        }

        /// <summary>
        /// Updates the given entry in the <c>ZipFile</c>, using the given delegate
        /// as the source for content for the <c>ZipEntry</c>.
        /// </summary>
        /// 
        /// <remarks>
        /// Calling this method is equivalent to removing the <c>ZipEntry</c> for the
        /// given file name and directory path, if it exists, and then calling <see cref="M:DotNetZipAdditionalPlatforms.Zip.ZipFile.AddEntry(System.String,DotNetZipAdditionalPlatforms.Zip.WriteDelegate)" />.  See the
        /// documentation for that method for further explanation.
        /// </remarks>
        /// 
        /// <param name="entryName">
        /// The name, including any path, to use within the archive for the entry.
        /// </param>
        /// 
        /// <param name="writer">the delegate which will write the entry content.</param>
        /// 
        /// <returns>The <c>ZipEntry</c> added.</returns>
        public ZipEntry UpdateEntry(string entryName, DotNetZipAdditionalPlatforms.Zip.WriteDelegate writer)
        {
            this.RemoveEntryForUpdate(entryName);
            return this.AddEntry(entryName, writer);
        }

        /// <summary>
        /// Updates the given entry in the <c>ZipFile</c>, using the given stream as
        /// input, and the given filename and given directory Path.
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// Calling the method is equivalent to calling <c>RemoveEntry()</c> if an
        /// entry by the same name already exists, and then calling <c>AddEntry()</c>
        /// with the given <c>fileName</c> and stream.
        /// </para>
        /// 
        /// <para>
        /// The stream must be open and readable during the call to
        /// <c>ZipFile.Save</c>.  You can dispense the stream on a just-in-time basis
        /// using the <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipEntry.InputStream" /> property. Check the
        /// documentation of that property for more information.
        /// </para>
        /// 
        /// <para>
        /// For <c>ZipFile</c> properties including <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.Encryption" />, <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.Password" />, <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.SetCompression" />, <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.ProvisionalAlternateEncoding" />, <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.ExtractExistingFile" />,
        /// <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.ZipErrorAction" />, and <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.CompressionLevel" />, their
        /// respective values at the time of this call will be applied to the
        /// <c>ZipEntry</c> added.
        /// </para>
        /// 
        /// </remarks>
        /// 
        /// <seealso cref="M:DotNetZipAdditionalPlatforms.Zip.ZipFile.AddEntry(System.String,System.IO.Stream)" />
        /// <seealso cref="P:DotNetZipAdditionalPlatforms.Zip.ZipEntry.InputStream" />
        /// 
        /// <param name="entryName">
        /// The name, including any path, to use within the archive for the entry.
        /// </param>
        /// 
        /// <param name="stream">The input stream from which to read file data.</param>
        /// <returns>The <c>ZipEntry</c> added.</returns>
        public ZipEntry UpdateEntry(string entryName, Stream stream)
        {
            this.RemoveEntryForUpdate(entryName);
            return this.AddEntry(entryName, stream);
        }

        /// <summary>
        /// Updates the given entry in the <c>ZipFile</c>, using the given byte
        /// array as content for the entry.
        /// </summary>
        /// 
        /// <remarks>
        /// Calling this method is equivalent to removing the <c>ZipEntry</c>
        /// for the given filename and directory path, if it exists, and then
        /// calling <see cref="M:DotNetZipAdditionalPlatforms.Zip.ZipFile.AddEntry(System.String,System.Byte[])" />.  See the
        /// documentation for that method for further explanation.
        /// </remarks>
        /// 
        /// <param name="entryName">
        /// The name, including any path, to use within the archive for the entry.
        /// </param>
        /// 
        /// <param name="byteContent">The content to use for the <c>ZipEntry</c>.</param>
        /// 
        /// <returns>The <c>ZipEntry</c> added.</returns>
        public ZipEntry UpdateEntry(string entryName, byte[] byteContent)
        {
            this.RemoveEntryForUpdate(entryName);
            return this.AddEntry(entryName, byteContent);
        }

        /// <summary>
        /// Updates the given entry in the <c>ZipFile</c>, using the given
        /// string as content for the <c>ZipEntry</c>.
        /// </summary>
        /// 
        /// <remarks>
        /// 
        /// <para>
        /// Calling this method is equivalent to removing the <c>ZipEntry</c> for
        /// the given file name and directory path, if it exists, and then calling
        /// <see cref="M:DotNetZipAdditionalPlatforms.Zip.ZipFile.AddEntry(System.String,System.String)" />.  See the documentation for
        /// that method for further explanation. The string content is encoded
        /// using the default encoding for the machine, or on Silverlight, using
        /// UTF-8. This encoding is distinct from the encoding used for the
        /// filename itself.  See <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.AlternateEncoding" />.
        /// </para>
        /// 
        /// </remarks>
        /// 
        /// <param name="entryName">
        /// The name, including any path, to use within the archive for the entry.
        /// </param>
        /// 
        /// <param name="content">
        /// The content of the file, should it be extracted from the zip.
        /// </param>
        /// 
        /// <returns>The <c>ZipEntry</c> added.</returns>
        public ZipEntry UpdateEntry(string entryName, string content)
        {
            return this.UpdateEntry(entryName, content, Encoding.Default);
        }

        /// <summary>
        /// Updates the given entry in the <c>ZipFile</c>, using the given delegates
        /// to open and close the stream that provides the content for the <c>ZipEntry</c>.
        /// </summary>
        /// 
        /// <remarks>
        /// Calling this method is equivalent to removing the <c>ZipEntry</c> for the
        /// given file name and directory path, if it exists, and then calling <see cref="M:DotNetZipAdditionalPlatforms.Zip.ZipFile.AddEntry(System.String,DotNetZipAdditionalPlatforms.Zip.OpenDelegate,DotNetZipAdditionalPlatforms.Zip.CloseDelegate)" />.  See the
        /// documentation for that method for further explanation.
        /// </remarks>
        /// 
        /// <param name="entryName">
        /// The name, including any path, to use within the archive for the entry.
        /// </param>
        /// 
        /// <param name="opener">
        /// the delegate that will be invoked to open the stream
        /// </param>
        /// <param name="closer">
        /// the delegate that will be invoked to close the stream
        /// </param>
        /// 
        /// <returns>The <c>ZipEntry</c> added or updated.</returns>
        public ZipEntry UpdateEntry(string entryName, OpenDelegate opener, CloseDelegate closer)
        {
            this.RemoveEntryForUpdate(entryName);
            return this.AddEntry(entryName, opener, closer);
        }

        /// <summary>
        /// Updates the given entry in the <c>ZipFile</c>, using the given string as
        /// content for the <c>ZipEntry</c>.
        /// </summary>
        /// 
        /// <remarks>
        /// Calling this method is equivalent to removing the <c>ZipEntry</c> for the
        /// given file name and directory path, if it exists, and then calling <see cref="M:DotNetZipAdditionalPlatforms.Zip.ZipFile.AddEntry(System.String,System.String,System.Text.Encoding)" />.  See the
        /// documentation for that method for further explanation.
        /// </remarks>
        /// 
        /// <param name="entryName">
        /// The name, including any path, to use within the archive for the entry.
        /// </param>
        /// 
        /// <param name="content">
        /// The content of the file, should it be extracted from the zip.
        /// </param>
        /// 
        /// <param name="encoding">
        /// The text encoding to use when encoding the string. Be aware: This is
        /// distinct from the text encoding used to encode the filename. See <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.AlternateEncoding" />.
        /// </param>
        /// 
        /// <returns>The <c>ZipEntry</c> added.</returns>
        public ZipEntry UpdateEntry(string entryName, string content, Encoding encoding)
        {
            this.RemoveEntryForUpdate(entryName);
            return this.AddEntry(entryName, content, encoding);
        }

        /// <summary>
        /// Adds or Updates a File in a Zip file archive.
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// This method adds a file to a zip archive, or, if the file already exists
        /// in the zip archive, this method Updates the content of that given filename
        /// in the zip archive.  The <c>UpdateFile</c> method might more accurately be
        /// called "AddOrUpdateFile".
        /// </para>
        /// 
        /// <para>
        /// Upon success, there is no way for the application to learn whether the file
        /// was added versus updated.
        /// </para>
        /// 
        /// <para>
        /// For <c>ZipFile</c> properties including <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.Encryption" />, <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.Password" />, <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.SetCompression" />, <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.ProvisionalAlternateEncoding" />, <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.ExtractExistingFile" />,
        /// <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.ZipErrorAction" />, and <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.CompressionLevel" />, their
        /// respective values at the time of this call will be applied to the
        /// <c>ZipEntry</c> added.
        /// </para>
        /// </remarks>
        /// 
        /// <example>
        /// 
        /// This example shows how to Update an existing entry in a zipfile. The first
        /// call to UpdateFile adds the file to the newly-created zip archive.  The
        /// second call to UpdateFile updates the content for that file in the zip
        /// archive.
        /// 
        /// <code>
        /// using (ZipFile zip1 = new ZipFile())
        /// {
        /// // UpdateFile might more accurately be called "AddOrUpdateFile"
        /// zip1.UpdateFile("MyDocuments\\Readme.txt");
        /// zip1.UpdateFile("CustomerList.csv");
        /// zip1.Comment = "This zip archive has been created.";
        /// zip1.Save("Content.zip");
        /// }
        /// 
        /// using (ZipFile zip2 = ZipFile.Read("Content.zip"))
        /// {
        /// zip2.UpdateFile("Updates\\Readme.txt");
        /// zip2.Comment = "This zip archive has been updated: The Readme.txt file has been changed.";
        /// zip2.Save();
        /// }
        /// 
        /// </code>
        /// <code lang="VB">
        /// Using zip1 As New ZipFile
        /// ' UpdateFile might more accurately be called "AddOrUpdateFile"
        /// zip1.UpdateFile("MyDocuments\Readme.txt")
        /// zip1.UpdateFile("CustomerList.csv")
        /// zip1.Comment = "This zip archive has been created."
        /// zip1.Save("Content.zip")
        /// End Using
        /// 
        /// Using zip2 As ZipFile = ZipFile.Read("Content.zip")
        /// zip2.UpdateFile("Updates\Readme.txt")
        /// zip2.Comment = "This zip archive has been updated: The Readme.txt file has been changed."
        /// zip2.Save
        /// End Using
        /// </code>
        /// </example>
        /// 
        /// <seealso cref="M:DotNetZipAdditionalPlatforms.Zip.ZipFile.AddFile(System.String)" />
        /// <seealso cref="M:DotNetZipAdditionalPlatforms.Zip.ZipFile.UpdateDirectory(System.String)" />
        /// <seealso cref="M:DotNetZipAdditionalPlatforms.Zip.ZipFile.UpdateItem(System.String)" />
        /// 
        /// <param name="fileName">
        /// The name of the file to add or update. It should refer to a file in the
        /// filesystem.  The name of the file may be a relative path or a
        /// fully-qualified path.
        /// </param>
        /// 
        /// <returns>
        /// The <c>ZipEntry</c> corresponding to the File that was added or updated.
        /// </returns>
        public ZipEntry UpdateFile(string fileName)
        {
            return this.UpdateFile(fileName, null);
        }

        /// <summary>
        /// Adds or Updates a File in a Zip file archive.
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// This method adds a file to a zip archive, or, if the file already exists
        /// in the zip archive, this method Updates the content of that given filename
        /// in the zip archive.
        /// </para>
        /// 
        /// <para>
        /// This version of the method allows the caller to explicitly specify the
        /// directory path to be used in the archive.  The entry to be added or
        /// updated is found by using the specified directory path, combined with the
        /// basename of the specified filename.
        /// </para>
        /// 
        /// <para>
        /// Upon success, there is no way for the application to learn if the file was
        /// added versus updated.
        /// </para>
        /// 
        /// <para>
        /// For <c>ZipFile</c> properties including <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.Encryption" />, <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.Password" />, <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.SetCompression" />, <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.ProvisionalAlternateEncoding" />, <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.ExtractExistingFile" />,
        /// <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.ZipErrorAction" />, and <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.CompressionLevel" />, their
        /// respective values at the time of this call will be applied to the
        /// <c>ZipEntry</c> added.
        /// </para>
        /// </remarks>
        /// 
        /// <seealso cref="M:DotNetZipAdditionalPlatforms.Zip.ZipFile.AddFile(System.String,System.String)" />
        /// <seealso cref="M:DotNetZipAdditionalPlatforms.Zip.ZipFile.UpdateDirectory(System.String,System.String)" />
        /// <seealso cref="M:DotNetZipAdditionalPlatforms.Zip.ZipFile.UpdateItem(System.String,System.String)" />
        /// 
        /// <param name="fileName">
        /// The name of the file to add or update. It should refer to a file in the
        /// filesystem.  The name of the file may be a relative path or a
        /// fully-qualified path.
        /// </param>
        /// 
        /// <param name="directoryPathInArchive">
        /// Specifies a directory path to use to override any path in the
        /// <c>fileName</c>.  This path may, or may not, correspond to a real
        /// directory in the current filesystem.  If the files within the zip are
        /// later extracted, this is the path used for the extracted file.  Passing
        /// <c>null</c> (<c>Nothing</c> in VB) will use the path on the
        /// <c>fileName</c>, if any.  Passing the empty string ("") will insert the
        /// item at the root path within the archive.
        /// </param>
        /// 
        /// <returns>
        /// The <c>ZipEntry</c> corresponding to the File that was added or updated.
        /// </returns>
        public ZipEntry UpdateFile(string fileName, string directoryPathInArchive)
        {
            string str = ZipEntry.NameInArchive(fileName, directoryPathInArchive);
            if (this[str] != null)
            {
                this.RemoveEntry(str);
            }
            return this.AddFile(fileName, directoryPathInArchive);
        }

        /// <summary>
        /// Adds or updates a set of files in the <c>ZipFile</c>.
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// Any files that already exist in the archive are updated. Any files that
        /// don't yet exist in the archive are added.
        /// </para>
        /// 
        /// <para>
        /// For <c>ZipFile</c> properties including <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.Encryption" />, <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.Password" />, <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.SetCompression" />, <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.ProvisionalAlternateEncoding" />, <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.ExtractExistingFile" />,
        /// <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.ZipErrorAction" />, and <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.CompressionLevel" />, their
        /// respective values at the time of this call will be applied to each
        /// ZipEntry added.
        /// </para>
        /// </remarks>
        /// 
        /// <param name="fileNames">
        /// The collection of names of the files to update. Each string should refer to a file in
        /// the filesystem. The name of the file may be a relative path or a fully-qualified path.
        /// </param>
        public void UpdateFiles(IEnumerable<string> fileNames)
        {
            this.UpdateFiles(fileNames, null);
        }

        /// <summary>
        /// Adds or updates a set of files to the <c>ZipFile</c>, using the specified
        /// directory path in the archive.
        /// </summary>
        /// 
        /// <remarks>
        /// 
        /// <para>
        /// Any files that already exist in the archive are updated. Any files that
        /// don't yet exist in the archive are added.
        /// </para>
        /// 
        /// <para>
        /// For <c>ZipFile</c> properties including <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.Encryption" />, <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.Password" />, <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.SetCompression" />, <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.ProvisionalAlternateEncoding" />, <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.ExtractExistingFile" />,
        /// <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.ZipErrorAction" />, and <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.CompressionLevel" />, their
        /// respective values at the time of this call will be applied to each
        /// ZipEntry added.
        /// </para>
        /// </remarks>
        /// 
        /// <param name="fileNames">
        /// The names of the files to add or update. Each string should refer to a
        /// file in the filesystem.  The name of the file may be a relative path or a
        /// fully-qualified path.
        /// </param>
        /// 
        /// <param name="directoryPathInArchive">
        /// Specifies a directory path to use to override any path in the file name.
        /// This path may, or may not, correspond to a real directory in the current
        /// filesystem.  If the files within the zip are later extracted, this is the
        /// path used for the extracted file.  Passing <c>null</c> (<c>Nothing</c> in
        /// VB) will use the path on each of the <c>fileNames</c>, if any.  Passing
        /// the empty string ("") will insert the item at the root path within the
        /// archive.
        /// </param>
        /// 
        /// <seealso cref="M:DotNetZipAdditionalPlatforms.Zip.ZipFile.AddSelectedFiles(System.String,System.String)" />
        public void UpdateFiles(IEnumerable<string> fileNames, string directoryPathInArchive)
        {
            if (fileNames == null)
            {
                throw new ArgumentNullException("fileNames");
            }
            this.OnAddStarted();
            foreach (string str in fileNames)
            {
                this.UpdateFile(str, directoryPathInArchive);
            }
            this.OnAddCompleted();
        }

        /// <summary>
        /// Add or update a file or directory in the zip archive.
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// This is useful when the application is not sure or does not care if the
        /// item to be added is a file or directory, and does not know or does not
        /// care if the item already exists in the <c>ZipFile</c>. Calling this method
        /// is equivalent to calling <c>RemoveEntry()</c> if an entry by the same name
        /// already exists, followed calling by <c>AddItem()</c>.
        /// </para>
        /// 
        /// <para>
        /// For <c>ZipFile</c> properties including <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.Encryption" />, <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.Password" />, <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.SetCompression" />, <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.ProvisionalAlternateEncoding" />, <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.ExtractExistingFile" />,
        /// <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.ZipErrorAction" />, and <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.CompressionLevel" />, their
        /// respective values at the time of this call will be applied to the
        /// <c>ZipEntry</c> added.
        /// </para>
        /// </remarks>
        /// 
        /// <seealso cref="M:DotNetZipAdditionalPlatforms.Zip.ZipFile.AddItem(System.String)" />
        /// <seealso cref="M:DotNetZipAdditionalPlatforms.Zip.ZipFile.UpdateFile(System.String)" />
        /// <seealso cref="M:DotNetZipAdditionalPlatforms.Zip.ZipFile.UpdateDirectory(System.String)" />
        /// 
        /// <param name="itemName">
        /// the path to the file or directory to be added or updated.
        /// </param>
        public void UpdateItem(string itemName)
        {
            this.UpdateItem(itemName, null);
        }

        /// <summary>
        /// Add or update a file or directory.
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// This method is useful when the application is not sure or does not care if
        /// the item to be added is a file or directory, and does not know or does not
        /// care if the item already exists in the <c>ZipFile</c>. Calling this method
        /// is equivalent to calling <c>RemoveEntry()</c>, if an entry by that name
        /// exists, and then calling <c>AddItem()</c>.
        /// </para>
        /// 
        /// <para>
        /// This version of the method allows the caller to explicitly specify the
        /// directory path to be used for the item being added to the archive.  The
        /// entry or entries that are added or updated will use the specified
        /// <c>DirectoryPathInArchive</c>. Extracting the entry from the archive will
        /// result in a file stored in that directory path.
        /// </para>
        /// 
        /// <para>
        /// For <c>ZipFile</c> properties including <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.Encryption" />, <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.Password" />, <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.SetCompression" />, <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.ProvisionalAlternateEncoding" />, <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.ExtractExistingFile" />,
        /// <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.ZipErrorAction" />, and <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.CompressionLevel" />, their
        /// respective values at the time of this call will be applied to the
        /// <c>ZipEntry</c> added.
        /// </para>
        /// </remarks>
        /// 
        /// <seealso cref="M:DotNetZipAdditionalPlatforms.Zip.ZipFile.AddItem(System.String,System.String)" />
        /// <seealso cref="M:DotNetZipAdditionalPlatforms.Zip.ZipFile.UpdateFile(System.String,System.String)" />
        /// <seealso cref="M:DotNetZipAdditionalPlatforms.Zip.ZipFile.UpdateDirectory(System.String,System.String)" />
        /// 
        /// <param name="itemName">
        /// The path for the File or Directory to be added or updated.
        /// </param>
        /// <param name="directoryPathInArchive">
        /// Specifies a directory path to use to override any path in the
        /// <c>itemName</c>.  This path may, or may not, correspond to a real
        /// directory in the current filesystem.  If the files within the zip are
        /// later extracted, this is the path used for the extracted file.  Passing
        /// <c>null</c> (<c>Nothing</c> in VB) will use the path on the
        /// <c>itemName</c>, if any.  Passing the empty string ("") will insert the
        /// item at the root path within the archive.
        /// </param>
        public void UpdateItem(string itemName, string directoryPathInArchive)
        {
            if (File.Exists(itemName))
            {
                this.UpdateFile(itemName, directoryPathInArchive);
            }
            else
            {
                if (!Directory.Exists(itemName))
                {
                    throw new FileNotFoundException(string.Format(CultureInfo.InvariantCulture, "That file or directory ({0}) does not exist!", itemName));
                }
                this.UpdateDirectory(itemName, directoryPathInArchive);
            }
        }

        /// <summary>
        /// Updates the ZipFile with a selection of files from the disk that conform
        /// to the specified criteria.
        /// </summary>
        /// 
        /// <remarks>
        /// This method selects files from the specified disk directory that match the
        /// specified selection criteria, and Updates the <c>ZipFile</c> with those
        /// files, using the specified directory path in the archive. If
        /// <c>recurseDirectories</c> is true, files are also selected from
        /// subdirectories, and the directory structure in the filesystem is
        /// reproduced in the zip archive, rooted at the directory specified by
        /// <c>directoryOnDisk</c>.  For details on the syntax for the
        /// selectionCriteria parameter, see <see cref="M:DotNetZipAdditionalPlatforms.Zip.ZipFile.AddSelectedFiles(System.String)" />.
        /// </remarks>
        /// 
        /// <param name="selectionCriteria">
        /// The criteria for selection of files to add to the <c>ZipFile</c>.
        /// </param>
        /// 
        /// <param name="directoryOnDisk">
        /// The path to the directory in the filesystem from which to select files.
        /// </param>
        /// 
        /// <param name="directoryPathInArchive">
        /// Specifies a directory path to use to in place of the
        /// <c>directoryOnDisk</c>. This path may, or may not, correspond to a
        /// real directory in the current filesystem. If the files within the zip
        /// are later extracted, this is the path used for the extracted file.
        /// Passing null (nothing in VB) will use the path on the file name, if
        /// any; in other words it would use <c>directoryOnDisk</c>, plus any
        /// subdirectory.  Passing the empty string ("") will insert the item at
        /// the root path within the archive.
        /// </param>
        /// 
        /// <param name="recurseDirectories">
        /// If true, the method also scans subdirectories for files matching the criteria.
        /// </param>
        /// 
        /// <seealso cref="M:DotNetZipAdditionalPlatforms.Zip.ZipFile.AddSelectedFiles(System.String,System.String,System.String,System.Boolean)" />
        public void UpdateSelectedFiles(string selectionCriteria, string directoryOnDisk, string directoryPathInArchive, bool recurseDirectories)
        {
            this._AddOrUpdateSelectedFiles(selectionCriteria, directoryOnDisk, directoryPathInArchive, recurseDirectories, true);
        }

        private static void Zip64SeekToCentralDirectory(ZipFile zf)
        {
            Stream readStream = zf.ReadStream;
            byte[] buffer = new byte[0x10];
            readStream.Seek(-40L, SeekOrigin.Current);
            readStream.Read(buffer, 0, 0x10);
            long offset = BitConverter.ToInt64(buffer, 8);
            zf._OffsetOfCentralDirectory = uint.MaxValue;
            zf._OffsetOfCentralDirectory64 = offset;
            readStream.Seek(offset, SeekOrigin.Begin);
            uint num2 = (uint) SharedUtilities.ReadInt(readStream);
            if (num2 != 0x6064b50)
            {
                throw new BadReadException(string.Format(CultureInfo.InvariantCulture, "  Bad signature (0x{0:X8}) looking for ZIP64 EoCD Record at position 0x{1:X8}", num2, readStream.Position));
            }
            readStream.Read(buffer, 0, 8);
            buffer = new byte[BitConverter.ToInt64(buffer, 0)];
            readStream.Read(buffer, 0, buffer.Length);
            offset = BitConverter.ToInt64(buffer, 0x24);
            readStream.Seek(offset, SeekOrigin.Begin);
        }

        /// <summary>
        /// Indicates whether NTFS Reparse Points, like junctions, should be
        /// traversed during calls to <c>AddDirectory()</c>.
        /// </summary>
        /// 
        /// <remarks>
        /// By default, calls to AddDirectory() will traverse NTFS reparse
        /// points, like mounted volumes, and directory junctions.  An example
        /// of a junction is the "My Music" directory in Windows Vista.  In some
        /// cases you may not want DotNetZip to traverse those directories.  In
        /// that case, set this property to false.
        /// </remarks>
        /// 
        /// <example>
        /// <code lang="C#">
        /// using (var zip = new ZipFile())
        /// {
        /// zip.AddDirectoryWillTraverseReparsePoints = false;
        /// zip.AddDirectory(dirToZip,"fodder");
        /// zip.Save(zipFileToCreate);
        /// }
        /// </code>
        /// </example>
        public bool AddDirectoryWillTraverseReparsePoints { get; set; }

        /// <summary>
        /// A Text Encoding to use when encoding the filenames and comments for
        /// all the ZipEntry items, during a ZipFile.Save() operation.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Whether the encoding specified here is used during the save depends
        /// on <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.AlternateEncodingUsage" />.
        /// </para>
        /// </remarks>
        public Encoding AlternateEncoding
        {
            get
            {
                return this._alternateEncoding;
            }
            set
            {
                this._alternateEncoding = value;
            }
        }

        /// <summary>
        /// A flag that tells if and when this instance should apply
        /// AlternateEncoding to encode the filenames and comments associated to
        /// of ZipEntry objects contained within this instance.
        /// </summary>
        public ZipOption AlternateEncodingUsage
        {
            get
            {
                return this._alternateEncodingUsage;
            }
            set
            {
                this._alternateEncodingUsage = value;
            }
        }

        private string ArchiveNameForEvent
        {
            get
            {
                return ((this._name != null) ? this._name : "(stream)");
            }
        }

        /// <summary>
        /// Size of the IO buffer used while saving.
        /// </summary>
        /// 
        /// <remarks>
        /// 
        /// <para>
        /// First, let me say that you really don't need to bother with this.  It is
        /// here to allow for optimizations that you probably won't make! It will work
        /// fine if you don't set or get this property at all. Ok?
        /// </para>
        /// 
        /// <para>
        /// Now that we have <em>that</em> out of the way, the fine print: This
        /// property affects the size of the buffer that is used for I/O for each
        /// entry contained in the zip file. When a file is read in to be compressed,
        /// it uses a buffer given by the size here.  When you update a zip file, the
        /// data for unmodified entries is copied from the first zip file to the
        /// other, through a buffer given by the size here.
        /// </para>
        /// 
        /// <para>
        /// Changing the buffer size affects a few things: first, for larger buffer
        /// sizes, the memory used by the <c>ZipFile</c>, obviously, will be larger
        /// during I/O operations.  This may make operations faster for very much
        /// larger files.  Last, for any given entry, when you use a larger buffer
        /// there will be fewer progress events during I/O operations, because there's
        /// one progress event generated for each time the buffer is filled and then
        /// emptied.
        /// </para>
        /// 
        /// <para>
        /// The default buffer size is 8k.  Increasing the buffer size may speed
        /// things up as you compress larger files.  But there are no hard-and-fast
        /// rules here, eh?  You won't know til you test it.  And there will be a
        /// limit where ever larger buffers actually slow things down.  So as I said
        /// in the beginning, it's probably best if you don't set or get this property
        /// at all.
        /// </para>
        /// 
        /// </remarks>
        /// 
        /// <example>
        /// This example shows how you might set a large buffer size for efficiency when
        /// dealing with zip entries that are larger than 1gb.
        /// <code lang="C#">
        /// using (ZipFile zip = new ZipFile())
        /// {
        /// zip.SaveProgress += this.zip1_SaveProgress;
        /// zip.AddDirectory(directoryToZip, "");
        /// zip.UseZip64WhenSaving = Zip64Option.Always;
        /// zip.BufferSize = 65536*8; // 65536 * 8 = 512k
        /// zip.Save(ZipFileToCreate);
        /// }
        /// </code>
        /// </example>
        public int BufferSize
        {
            get
            {
                return this._BufferSize;
            }
            set
            {
                this._BufferSize = value;
            }
        }

        /// <summary>
        /// Indicates whether to perform case-sensitive matching on the filename when
        /// retrieving entries in the zipfile via the string-based indexer.
        /// </summary>
        /// 
        /// <remarks>
        /// The default value is <c>false</c>, which means don't do case-sensitive
        /// matching. In other words, retrieving zip["ReadMe.Txt"] is the same as
        /// zip["readme.txt"].  It really makes sense to set this to <c>true</c> only
        /// if you are not running on Windows, which has case-insensitive
        /// filenames. But since this library is not built for non-Windows platforms,
        /// in most cases you should just leave this property alone.
        /// </remarks>
        public bool CaseSensitiveRetrieval
        {
            get
            {
                return this._CaseSensitiveRetrieval;
            }
            set
            {
                if (value != this._CaseSensitiveRetrieval)
                {
                    this._CaseSensitiveRetrieval = value;
                    this._initEntriesDictionary();
                }
            }
        }

        /// <summary>
        /// Size of the work buffer to use for the ZLIB codec during compression.
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// When doing ZLIB or Deflate compression, the library fills a buffer,
        /// then passes it to the compressor for compression. Then the library
        /// reads out the compressed bytes. This happens repeatedly until there
        /// is no more uncompressed data to compress. This property sets the
        /// size of the buffer that will be used for chunk-wise compression. In
        /// order for the setting to take effect, your application needs to set
        /// this property before calling one of the <c>ZipFile.Save()</c>
        /// overloads.
        /// </para>
        /// <para>
        /// Setting this affects the performance and memory efficiency of
        /// compression and decompression. For larger files, setting this to a
        /// larger size may improve compression performance, but the exact
        /// numbers vary depending on available memory, the size of the streams
        /// you are compressing, and a bunch of other variables. I don't have
        /// good firm recommendations on how to set it.  You'll have to test it
        /// yourself. Or just leave it alone and accept the default.
        /// </para>
        /// </remarks>
        public int CodecBufferSize { get; set; }

        /// <summary>
        /// A comment attached to the zip archive.
        /// </summary>
        /// 
        /// <remarks>
        /// 
        /// <para>
        /// This property is read/write. It allows the application to specify a
        /// comment for the <c>ZipFile</c>, or read the comment for the
        /// <c>ZipFile</c>.  After setting this property, changes are only made
        /// permanent when you call a <c>Save()</c> method.
        /// </para>
        /// 
        /// <para>
        /// According to <see href="http://www.pkware.com/documents/casestudies/APPNOTE.TXT">PKWARE's
        /// zip specification</see>, the comment is not encrypted, even if there is a
        /// password set on the zip file.
        /// </para>
        /// 
        /// <para>
        /// The specification does not describe how to indicate the encoding used
        /// on a comment string. Many "compliant" zip tools and libraries use
        /// IBM437 as the code page for comments; DotNetZip, too, follows that
        /// practice.  On the other hand, there are situations where you want a
        /// Comment to be encoded with something else, for example using code page
        /// 950 "Big-5 Chinese". To fill that need, DotNetZip will encode the
        /// comment following the same procedure it follows for encoding
        /// filenames: (a) if <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.AlternateEncodingUsage" /> is
        /// <c>Never</c>, it uses the default encoding (IBM437). (b) if <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.AlternateEncodingUsage" /> is <c>Always</c>, it always uses the
        /// alternate encoding (<see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.AlternateEncoding" />). (c) if <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.AlternateEncodingUsage" /> is <c>AsNecessary</c>, it uses the
        /// alternate encoding only if the default encoding is not sufficient for
        /// encoding the comment - in other words if decoding the result does not
        /// produce the original string.  This decision is taken at the time of
        /// the call to <c>ZipFile.Save()</c>.
        /// </para>
        /// 
        /// <para>
        /// When creating a zip archive using this library, it is possible to change
        /// the value of <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.AlternateEncoding" /> between each
        /// entry you add, and between adding entries and the call to
        /// <c>Save()</c>. Don't do this.  It will likely result in a zip file that is
        /// not readable by any tool or application.  For best interoperability, leave
        /// <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.AlternateEncoding" /> alone, or specify it only
        /// once, before adding any entries to the <c>ZipFile</c> instance.
        /// </para>
        /// 
        /// </remarks>
        public string Comment
        {
            get
            {
                return this._Comment;
            }
            set
            {
                this._Comment = value;
                this._contentsChanged = true;
            }
        }

        /// <summary>
        /// Sets the compression level to be used for entries subsequently added to
        /// the zip archive.
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// Varying the compression level used on entries can affect the
        /// size-vs-speed tradeoff when compression and decompressing data streams
        /// or files.
        /// </para>
        /// 
        /// <para>
        /// As with some other properties on the <c>ZipFile</c> class, like <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.Password" />, <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.Encryption" />, and <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.ZipErrorAction" />, setting this property on a <c>ZipFile</c>
        /// instance will cause the specified <c>CompressionLevel</c> to be used on all
        /// <see cref="T:DotNetZipAdditionalPlatforms.Zip.ZipEntry" /> items that are subsequently added to the
        /// <c>ZipFile</c> instance. If you set this property after you have added
        /// items to the <c>ZipFile</c>, but before you have called <c>Save()</c>,
        /// those items will not use the specified compression level.
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
        /// </remarks>
        public DotNetZipAdditionalPlatforms.Zlib.CompressionLevel CompressionLevel { get; set; }

        /// <summary>
        /// The compression method for the zipfile.
        /// </summary>
        /// <remarks>
        /// <para>
        /// By default, the compression method is <c>CompressionMethod.Deflate.</c>
        /// </para>
        /// </remarks>
        /// <seealso cref="T:DotNetZipAdditionalPlatforms.Zip.CompressionMethod" />
        public DotNetZipAdditionalPlatforms.Zip.CompressionMethod CompressionMethod
        {
            get
            {
                return this._compressionMethod;
            }
            set
            {
                this._compressionMethod = value;
            }
        }

        /// <summary>
        /// Returns the number of entries in the Zip archive.
        /// </summary>
        public int Count
        {
            get
            {
                return this._entries.Count;
            }
        }

        /// <summary>
        /// The default text encoding used in zip archives.  It is numeric 437, also
        /// known as IBM437.
        /// </summary>
        /// <seealso cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.ProvisionalAlternateEncoding" />
        public static Encoding DefaultEncoding
        {
            get
            {
                return _defaultEncoding;
            }
        }

        /// <summary>
        /// Specifies whether the Creation, Access, and Modified times
        /// for entries added to the zip file will be emitted in "Unix(tm)
        /// format" when the zip archive is saved.
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// An application creating a zip archive can use this flag to explicitly
        /// specify that the file times for the entries should or should not be stored
        /// in the zip archive in the format used by Unix. By default this flag is
        /// <c>false</c>, meaning the Unix-format times are not stored in the zip
        /// archive.
        /// </para>
        /// 
        /// <para>
        /// When adding an entry from a file or directory, the Creation (<see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipEntry.CreationTime" />), Access (<see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipEntry.AccessedTime" />), and Modified (<see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipEntry.ModifiedTime" />) times for the given entry are
        /// automatically set from the filesystem values. When adding an entry from a
        /// stream or string, all three values are implicitly set to DateTime.Now.
        /// Applications can also explicitly set those times by calling <see cref="M:DotNetZipAdditionalPlatforms.Zip.ZipEntry.SetEntryTimes(System.DateTime,System.DateTime,System.DateTime)" />.
        /// </para>
        /// 
        /// <para>
        /// <see href="http://www.pkware.com/documents/casestudies/APPNOTE.TXT">PKWARE's
        /// zip specification</see> describes multiple ways to format these times in a
        /// zip file. One is the format Windows applications normally use: 100ns ticks
        /// since January 1, 1601 UTC.  The other is a format Unix applications
        /// typically use: seconds since January 1, 1970 UTC.  Each format can be
        /// stored in an "extra field" in the zip entry when saving the zip
        /// archive. The former uses an extra field with a Header Id of 0x000A, while
        /// the latter uses a header ID of 0x5455, although you probably don't need to
        /// know that.
        /// </para>
        /// 
        /// <para>
        /// Not all tools and libraries can interpret these fields.  Windows
        /// compressed folders is one that can read the Windows Format timestamps,
        /// while I believe the <see href="http://www.info-zip.org/">Infozip</see>
        /// tools can read the Unix format timestamps. Some tools and libraries may be
        /// able to read only one or the other.  DotNetZip can read or write times in
        /// either or both formats.
        /// </para>
        /// 
        /// <para>
        /// The times stored are taken from <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipEntry.ModifiedTime" />, <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipEntry.AccessedTime" />, and <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipEntry.CreationTime" />.
        /// </para>
        /// 
        /// <para>
        /// This property is not mutually exclusive of the <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.EmitTimesInWindowsFormatWhenSaving" /> property. It is possible and
        /// legal and valid to produce a zip file that contains timestamps encoded in
        /// the Unix format as well as in the Windows format, in addition to the <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipEntry.LastModified">LastModified</see> time attached to each
        /// entry in the zip archive, a time that is always stored in "DOS
        /// format". And, notwithstanding the names PKWare uses for these time
        /// formats, any of them can be read and written by any computer, on any
        /// operating system.  But, there are no guarantees that a program running on
        /// Mac or Linux will gracefully handle a zip file with "Windows" formatted
        /// times, or that an application that does not use DotNetZip but runs on
        /// Windows will be able to handle file times in Unix format.
        /// </para>
        /// 
        /// <para>
        /// When in doubt, test.  Sorry, I haven't got a complete list of tools and
        /// which sort of timestamps they can use and will tolerate.  If you get any
        /// good information and would like to pass it on, please do so and I will
        /// include that information in this documentation.
        /// </para>
        /// </remarks>
        /// 
        /// <seealso cref="P:DotNetZipAdditionalPlatforms.Zip.ZipEntry.EmitTimesInUnixFormatWhenSaving" />
        /// <seealso cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.EmitTimesInWindowsFormatWhenSaving" />
        public bool EmitTimesInUnixFormatWhenSaving
        {
            get
            {
                return this._emitUnixTimes;
            }
            set
            {
                this._emitUnixTimes = value;
            }
        }

        /// <summary>
        /// Specifies whether the Creation, Access, and Modified times for entries
        /// added to the zip file will be emitted in Windows format
        /// when the zip archive is saved.
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// An application creating a zip archive can use this flag to explicitly
        /// specify that the file times for the entries should or should not be stored
        /// in the zip archive in the format used by Windows. By default this flag is
        /// <c>true</c>, meaning the Windows-format times are stored in the zip
        /// archive.
        /// </para>
        /// 
        /// <para>
        /// When adding an entry from a file or directory, the Creation (<see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipEntry.CreationTime" />), Access (<see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipEntry.AccessedTime" />), and Modified (<see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipEntry.ModifiedTime" />) times for the given entry are
        /// automatically set from the filesystem values. When adding an entry from a
        /// stream or string, all three values are implicitly set to
        /// <c>DateTime.Now</c>.  Applications can also explicitly set those times by
        /// calling <see cref="M:DotNetZipAdditionalPlatforms.Zip.ZipEntry.SetEntryTimes(System.DateTime,System.DateTime,System.DateTime)" />.
        /// </para>
        /// 
        /// <para>
        /// <see href="http://www.pkware.com/documents/casestudies/APPNOTE.TXT">PKWARE's
        /// zip specification</see> describes multiple ways to format these times in a
        /// zip file. One is the format Windows applications normally use: 100ns ticks
        /// since January 1, 1601 UTC.  The other is a format Unix applications typically
        /// use: seconds since January 1, 1970 UTC.  Each format can be stored in an
        /// "extra field" in the zip entry when saving the zip archive. The former
        /// uses an extra field with a Header Id of 0x000A, while the latter uses a
        /// header ID of 0x5455, although you probably don't need to know that.
        /// </para>
        /// 
        /// <para>
        /// Not all tools and libraries can interpret these fields.  Windows
        /// compressed folders is one that can read the Windows Format timestamps,
        /// while I believe <see href="http://www.info-zip.org/">the Infozip
        /// tools</see> can read the Unix format timestamps. Some tools and libraries
        /// may be able to read only one or the other. DotNetZip can read or write
        /// times in either or both formats.
        /// </para>
        /// 
        /// <para>
        /// The times stored are taken from <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipEntry.ModifiedTime" />, <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipEntry.AccessedTime" />, and <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipEntry.CreationTime" />.
        /// </para>
        /// 
        /// <para>
        /// The value set here applies to all entries subsequently added to the
        /// <c>ZipFile</c>.
        /// </para>
        /// 
        /// <para>
        /// This property is not mutually exclusive of the <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.EmitTimesInUnixFormatWhenSaving" /> property. It is possible and
        /// legal and valid to produce a zip file that contains timestamps encoded in
        /// the Unix format as well as in the Windows format, in addition to the <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipEntry.LastModified">LastModified</see> time attached to each
        /// entry in the archive, a time that is always stored in "DOS format". And,
        /// notwithstanding the names PKWare uses for these time formats, any of them
        /// can be read and written by any computer, on any operating system.  But,
        /// there are no guarantees that a program running on Mac or Linux will
        /// gracefully handle a zip file with "Windows" formatted times, or that an
        /// application that does not use DotNetZip but runs on Windows will be able to
        /// handle file times in Unix format.
        /// </para>
        /// 
        /// <para>
        /// When in doubt, test.  Sorry, I haven't got a complete list of tools and
        /// which sort of timestamps they can use and will tolerate.  If you get any
        /// good information and would like to pass it on, please do so and I will
        /// include that information in this documentation.
        /// </para>
        /// </remarks>
        /// 
        /// <example>
        /// This example shows how to save a zip file that contains file timestamps
        /// in a format normally used by Unix.
        /// <code lang="C#">
        /// using (var zip = new ZipFile())
        /// {
        /// // produce a zip file the Mac will like
        /// zip.EmitTimesInWindowsFormatWhenSaving = false;
        /// zip.EmitTimesInUnixFormatWhenSaving = true;
        /// zip.AddDirectory(directoryToZip, "files");
        /// zip.Save(outputFile);
        /// }
        /// </code>
        /// 
        /// <code lang="VB">
        /// Using zip As New ZipFile
        /// '' produce a zip file the Mac will like
        /// zip.EmitTimesInWindowsFormatWhenSaving = False
        /// zip.EmitTimesInUnixFormatWhenSaving = True
        /// zip.AddDirectory(directoryToZip, "files")
        /// zip.Save(outputFile)
        /// End Using
        /// </code>
        /// </example>
        /// 
        /// <seealso cref="P:DotNetZipAdditionalPlatforms.Zip.ZipEntry.EmitTimesInWindowsFormatWhenSaving" />
        /// <seealso cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.EmitTimesInUnixFormatWhenSaving" />
        public bool EmitTimesInWindowsFormatWhenSaving
        {
            get
            {
                return this._emitNtfsTimes;
            }
            set
            {
                this._emitNtfsTimes = value;
            }
        }

        /// <summary>
        /// The Encryption to use for entries added to the <c>ZipFile</c>.
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// Set this when creating a zip archive, or when updating a zip archive. The
        /// specified Encryption is applied to the entries subsequently added to the
        /// <c>ZipFile</c> instance.  Applications do not need to set the
        /// <c>Encryption</c> property when reading or extracting a zip archive.
        /// </para>
        /// 
        /// <para>
        /// If you set this to something other than EncryptionAlgorithm.None, you
        /// will also need to set the <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.Password" />.
        /// </para>
        /// 
        /// <para>
        /// As with some other properties on the <c>ZipFile</c> class, like <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.Password" /> and <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.CompressionLevel" />, setting this
        /// property on a <c>ZipFile</c> instance will cause the specified
        /// <c>EncryptionAlgorithm</c> to be used on all <see cref="T:DotNetZipAdditionalPlatforms.Zip.ZipEntry" /> items
        /// that are subsequently added to the <c>ZipFile</c> instance. In other
        /// words, if you set this property after you have added items to the
        /// <c>ZipFile</c>, but before you have called <c>Save()</c>, those items will
        /// not be encrypted or protected with a password in the resulting zip
        /// archive. To get a zip archive with encrypted entries, set this property,
        /// along with the <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.Password" /> property, before calling
        /// <c>AddFile</c>, <c>AddItem</c>, or <c>AddDirectory</c> (etc.) on the
        /// <c>ZipFile</c> instance.
        /// </para>
        /// 
        /// <para>
        /// If you read a <c>ZipFile</c>, you can modify the <c>Encryption</c> on an
        /// encrypted entry, only by setting the <c>Encryption</c> property on the
        /// <c>ZipEntry</c> itself.  Setting the <c>Encryption</c> property on the
        /// <c>ZipFile</c>, once it has been created via a call to
        /// <c>ZipFile.Read()</c>, does not affect entries that were previously read.
        /// </para>
        /// 
        /// <para>
        /// For example, suppose you read a <c>ZipFile</c>, and there is an encrypted
        /// entry.  Setting the <c>Encryption</c> property on that <c>ZipFile</c> and
        /// then calling <c>Save()</c> on the <c>ZipFile</c> does not update the
        /// <c>Encryption</c> used for the entries in the archive.  Neither is an
        /// exception thrown. Instead, what happens during the <c>Save()</c> is that
        /// all previously existing entries are copied through to the new zip archive,
        /// with whatever encryption and password that was used when originally
        /// creating the zip archive. Upon re-reading that archive, to extract
        /// entries, applications should use the original password or passwords, if
        /// any.
        /// </para>
        /// 
        /// <para>
        /// Suppose an application reads a <c>ZipFile</c>, and there is an encrypted
        /// entry.  Setting the <c>Encryption</c> property on that <c>ZipFile</c> and
        /// then adding new entries (via <c>AddFile()</c>, <c>AddEntry()</c>, etc)
        /// and then calling <c>Save()</c> on the <c>ZipFile</c> does not update the
        /// <c>Encryption</c> on any of the entries that had previously been in the
        /// <c>ZipFile</c>.  The <c>Encryption</c> property applies only to the
        /// newly-added entries.
        /// </para>
        /// 
        /// </remarks>
        /// 
        /// <example>
        /// <para>
        /// This example creates a zip archive that uses encryption, and then extracts
        /// entries from the archive.  When creating the zip archive, the ReadMe.txt
        /// file is zipped without using a password or encryption.  The other files
        /// use encryption.
        /// </para>
        /// 
        /// <code>
        /// // Create a zip archive with AES Encryption.
        /// using (ZipFile zip = new ZipFile())
        /// {
        /// zip.AddFile("ReadMe.txt");
        /// zip.Encryption= EncryptionAlgorithm.WinZipAes256;
        /// zip.Password= "Top.Secret.No.Peeking!";
        /// zip.AddFile("7440-N49th.png");
        /// zip.AddFile("2008-Regional-Sales-Report.pdf");
        /// zip.Save("EncryptedArchive.zip");
        /// }
        /// 
        /// // Extract a zip archive that uses AES Encryption.
        /// // You do not need to specify the algorithm during extraction.
        /// using (ZipFile zip = ZipFile.Read("EncryptedArchive.zip"))
        /// {
        /// zip.Password= "Top.Secret.No.Peeking!";
        /// zip.ExtractAll("extractDirectory");
        /// }
        /// </code>
        /// 
        /// <code lang="VB">
        /// ' Create a zip that uses Encryption.
        /// Using zip As New ZipFile()
        /// zip.Encryption= EncryptionAlgorithm.WinZipAes256
        /// zip.Password= "Top.Secret.No.Peeking!"
        /// zip.AddFile("ReadMe.txt")
        /// zip.AddFile("7440-N49th.png")
        /// zip.AddFile("2008-Regional-Sales-Report.pdf")
        /// zip.Save("EncryptedArchive.zip")
        /// End Using
        /// 
        /// ' Extract a zip archive that uses AES Encryption.
        /// ' You do not need to specify the algorithm during extraction.
        /// Using (zip as ZipFile = ZipFile.Read("EncryptedArchive.zip"))
        /// zip.Password= "Top.Secret.No.Peeking!"
        /// zip.ExtractAll("extractDirectory")
        /// End Using
        /// </code>
        /// 
        /// </example>
        /// 
        /// <seealso cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.Password">ZipFile.Password</seealso>
        /// <seealso cref="P:DotNetZipAdditionalPlatforms.Zip.ZipEntry.Encryption">ZipEntry.Encryption</seealso>
        public EncryptionAlgorithm Encryption
        {
            get
            {
                return this._Encryption;
            }
            set
            {
                if (value == EncryptionAlgorithm.Unsupported)
                {
                    throw new InvalidOperationException("You may not set Encryption to that value.");
                }
                this._Encryption = value;
            }
        }

        /// <summary>
        /// Returns the readonly collection of entries in the Zip archive.
        /// </summary>
        /// 
        /// <remarks>
        /// 
        /// <para>
        /// If there are no entries in the current <c>ZipFile</c>, the value returned is a
        /// non-null zero-element collection.  If there are entries in the zip file,
        /// the elements are returned in no particular order.
        /// </para>
        /// <para>
        /// This is the implied enumerator on the <c>ZipFile</c> class.  If you use a
        /// <c>ZipFile</c> instance in a context that expects an enumerator, you will
        /// get this collection.
        /// </para>
        /// </remarks>
        /// <seealso cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.EntriesSorted" />
        public ICollection<ZipEntry> Entries
        {
            get
            {
                return this._entries.Values;
            }
        }

        /// <summary>
        /// Returns a readonly collection of entries in the Zip archive, sorted by FileName.
        /// </summary>
        /// 
        /// <remarks>
        /// If there are no entries in the current <c>ZipFile</c>, the value returned
        /// is a non-null zero-element collection.  If there are entries in the zip
        /// file, the elements are returned sorted by the name of the entry.
        /// </remarks>
        /// 
        /// <example>
        /// 
        /// This example fills a Windows Forms ListView with the entries in a zip file.
        /// 
        /// <code lang="C#">
        /// using (ZipFile zip = ZipFile.Read(zipFile))
        /// {
        /// foreach (ZipEntry entry in zip.EntriesSorted)
        /// {
        /// ListViewItem item = new ListViewItem(n.ToString());
        /// n++;
        /// string[] subitems = new string[] {
        /// entry.FileName.Replace("/","\\"),
        /// entry.LastModified.ToString("yyyy-MM-dd HH:mm:ss"),
        /// entry.UncompressedSize.ToString(),
        /// string.Format(CultureInfo.InvariantCulture, "{0,5:F0}%", entry.CompressionRatio),
        /// entry.CompressedSize.ToString(),
        /// (entry.UsesEncryption) ? "Y" : "N",
        /// string.Format(CultureInfo.InvariantCulture, "{0:X8}", entry.Crc)};
        /// 
        /// foreach (String s in subitems)
        /// {
        /// ListViewItem.ListViewSubItem subitem = new ListViewItem.ListViewSubItem();
        /// subitem.Text = s;
        /// item.SubItems.Add(subitem);
        /// }
        /// 
        /// this.listView1.Items.Add(item);
        /// }
        /// }
        /// </code>
        /// </example>
        /// 
        /// <seealso cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.Entries" />
        public ICollection<ZipEntry> EntriesSorted
        {
            get
            {
                List<ZipEntry> list = new List<ZipEntry>();
                foreach (ZipEntry entry in this.Entries)
                {
                    list.Add(entry);
                }
                StringComparison sc = this.CaseSensitiveRetrieval ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
                list.Sort(delegate (ZipEntry x, ZipEntry y) {
                    return string.Compare(x.FileName, y.FileName, sc);
                });
                return list.AsReadOnly();
            }
        }

        /// <summary>
        /// The list of filenames for the entries contained within the zip archive.
        /// </summary>
        /// 
        /// <remarks>
        /// According to the ZIP specification, the names of the entries use forward
        /// slashes in pathnames.  If you are scanning through the list, you may have
        /// to swap forward slashes for backslashes.
        /// </remarks>
        /// 
        /// <seealso cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.Item(System.String)" />
        /// 
        /// <example>
        /// This example shows one way to test if a filename is already contained
        /// within a zip archive.
        /// <code>
        /// String zipFileToRead= "PackedDocuments.zip";
        /// string candidate = "DatedMaterial.xps";
        /// using (ZipFile zip = new ZipFile(zipFileToRead))
        /// {
        /// if (zip.EntryFilenames.Contains(candidate))
        /// Console.WriteLine("The file '{0}' exists in the zip archive '{1}'",
        /// candidate,
        /// zipFileName);
        /// else
        /// Console.WriteLine("The file, '{0}', does not exist in the zip archive '{1}'",
        /// candidate,
        /// zipFileName);
        /// Console.WriteLine();
        /// }
        /// </code>
        /// <code lang="VB">
        /// Dim zipFileToRead As String = "PackedDocuments.zip"
        /// Dim candidate As String = "DatedMaterial.xps"
        /// Using zip As ZipFile.Read(ZipFileToRead)
        /// If zip.EntryFilenames.Contains(candidate) Then
        /// Console.WriteLine("The file '{0}' exists in the zip archive '{1}'", _
        /// candidate, _
        /// zipFileName)
        /// Else
        /// Console.WriteLine("The file, '{0}', does not exist in the zip archive '{1}'", _
        /// candidate, _
        /// zipFileName)
        /// End If
        /// Console.WriteLine
        /// End Using
        /// </code>
        /// </example>
        /// 
        /// <returns>
        /// The list of strings for the filenames contained within the Zip archive.
        /// </returns>
        public ICollection<string> EntryFileNames
        {
            get
            {
                return this._entries.Keys;
            }
        }

        /// <summary>
        /// The action the library should take when extracting a file that already
        /// exists.
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// This property affects the behavior of the Extract methods (one of the
        /// <c>Extract()</c> or <c>ExtractWithPassword()</c> overloads), when
        /// extraction would would overwrite an existing filesystem file. If you do
        /// not set this property, the library throws an exception when extracting an
        /// entry would overwrite an existing file.
        /// </para>
        /// 
        /// <para>
        /// This property has no effect when extracting to a stream, or when the file
        /// to be extracted does not already exist.
        /// </para>
        /// </remarks>
        /// <seealso cref="P:DotNetZipAdditionalPlatforms.Zip.ZipEntry.ExtractExistingFile" />
        public ExtractExistingFileAction ExtractExistingFile { get; set; }

        /// <summary>
        /// Indicates whether extracted files should keep their paths as
        /// stored in the zip archive.
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// This property affects Extraction.  It is not used when creating zip
        /// archives.
        /// </para>
        /// 
        /// <para>
        /// With this property set to <c>false</c>, the default, extracting entries
        /// from a zip file will create files in the filesystem that have the full
        /// path associated to the entry within the zip file.  With this property set
        /// to <c>true</c>, extracting entries from the zip file results in files
        /// with no path: the folders are "flattened."
        /// </para>
        /// 
        /// <para>
        /// An example: suppose the zip file contains entries /directory1/file1.txt and
        /// /directory2/file2.txt.  With <c>FlattenFoldersOnExtract</c> set to false,
        /// the files created will be \directory1\file1.txt and \directory2\file2.txt.
        /// With the property set to true, the files created are file1.txt and file2.txt.
        /// </para>
        /// 
        /// </remarks>
        public bool FlattenFoldersOnExtract { get; set; }

        /// <summary>
        /// Indicates whether to perform a full scan of the zip file when reading it.
        /// </summary>
        /// 
        /// <remarks>
        /// 
        /// <para>
        /// You almost never want to use this property.
        /// </para>
        /// 
        /// <para>
        /// When reading a zip file, if this flag is <c>true</c> (<c>True</c> in
        /// VB), the entire zip archive will be scanned and searched for entries.
        /// For large archives, this can take a very, long time. The much more
        /// efficient default behavior is to read the zip directory, which is
        /// stored at the end of the zip file. But, in some cases the directory is
        /// corrupted and you need to perform a full scan of the zip file to
        /// determine the contents of the zip file. This property lets you do
        /// that, when necessary.
        /// </para>
        /// 
        /// <para>
        /// This flag is effective only when calling <see cref="M:DotNetZipAdditionalPlatforms.Zip.ZipFile.Initialize(System.String)" />. Normally you would read a ZipFile with the
        /// static <see cref="M:DotNetZipAdditionalPlatforms.Zip.ZipFile.Read(System.String)">ZipFile.Read</see>
        /// method. But you can't set the <c>FullScan</c> property on the
        /// <c>ZipFile</c> instance when you use a static factory method like
        /// <c>ZipFile.Read</c>.
        /// </para>
        /// 
        /// </remarks>
        /// 
        /// <example>
        /// 
        /// This example shows how to read a zip file using the full scan approach,
        /// and then save it, thereby producing a corrected zip file.
        /// 
        /// <code lang="C#">
        /// using (var zip = new ZipFile())
        /// {
        /// zip.FullScan = true;
        /// zip.Initialize(zipFileName);
        /// zip.Save(newName);
        /// }
        /// </code>
        /// 
        /// <code lang="VB">
        /// Using zip As New ZipFile
        /// zip.FullScan = True
        /// zip.Initialize(zipFileName)
        /// zip.Save(newName)
        /// End Using
        /// </code>
        /// </example>
        public bool FullScan { get; set; }

        /// <summary>
        /// Provides a human-readable string with information about the ZipFile.
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// The information string contains 10 lines or so, about each ZipEntry,
        /// describing whether encryption is in use, the compressed and uncompressed
        /// length of the entry, the offset of the entry, and so on. As a result the
        /// information string can be very long for zip files that contain many
        /// entries.
        /// </para>
        /// <para>
        /// This information is mostly useful for diagnostic purposes.
        /// </para>
        /// </remarks>
        public string Info
        {
            get
            {
                StringBuilder builder = new StringBuilder();
                builder.Append(string.Format(CultureInfo.InvariantCulture, "          ZipFile: {0}\n", this.Name));
                if (!string.IsNullOrEmpty(this._Comment))
                {
                    builder.Append(string.Format(CultureInfo.InvariantCulture, "          Comment: {0}\n", this._Comment));
                }
                if (this._versionMadeBy != 0)
                {
                    builder.Append(string.Format(CultureInfo.InvariantCulture, "  version made by: 0x{0:X4}\n", this._versionMadeBy));
                }
                if (this._versionNeededToExtract != 0)
                {
                    builder.Append(string.Format(CultureInfo.InvariantCulture, "needed to extract: 0x{0:X4}\n", this._versionNeededToExtract));
                }
                builder.Append(string.Format(CultureInfo.InvariantCulture, "       uses ZIP64: {0}\n", this.InputUsesZip64));
                builder.Append(string.Format(CultureInfo.InvariantCulture, "     disk with CD: {0}\n", this._diskNumberWithCd));
                if (this._OffsetOfCentralDirectory == uint.MaxValue)
                {
                    builder.Append(string.Format(CultureInfo.InvariantCulture, "      CD64 offset: 0x{0:X16}\n", this._OffsetOfCentralDirectory64));
                }
                else
                {
                    builder.Append(string.Format(CultureInfo.InvariantCulture, "        CD offset: 0x{0:X8}\n", this._OffsetOfCentralDirectory));
                }
                builder.Append("\n");
                foreach (ZipEntry entry in this._entries.Values)
                {
                    builder.Append(entry.Info);
                }
                return builder.ToString();
            }
        }

        /// <summary>
        /// Indicates whether the most recent <c>Read()</c> operation read a zip file that uses
        /// ZIP64 extensions.
        /// </summary>
        /// 
        /// <remarks>
        /// This property will return null (Nothing in VB) if you've added an entry after reading
        /// the zip file.
        /// </remarks>
        public bool? InputUsesZip64
        {
            get
            {
                if (this._entries.Count > 0xfffe)
                {
                    return true;
                }
                foreach (ZipEntry entry in this)
                {
                    if (entry.Source != ZipEntrySource.ZipFile)
                    {
                        return null;
                    }
                    if (entry._InputUsesZip64)
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        /// <summary>
        /// This is a name-based indexer into the Zip archive.
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// This property is read-only.
        /// </para>
        /// 
        /// <para>
        /// The <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.CaseSensitiveRetrieval" /> property on the <c>ZipFile</c>
        /// determines whether retrieval via this indexer is done via case-sensitive
        /// comparisons. By default, retrieval is not case sensitive.  This makes
        /// sense on Windows, in which filesystems are not case sensitive.
        /// </para>
        /// 
        /// <para>
        /// Regardless of case-sensitivity, it is not always the case that
        /// <c>this[value].FileName == value</c>. In other words, the <c>FileName</c>
        /// property of the <c>ZipEntry</c> retrieved with this indexer, may or may
        /// not be equal to the index value.
        /// </para>
        /// 
        /// <para>
        /// This is because DotNetZip performs a normalization of filenames passed to
        /// this indexer, before attempting to retrieve the item.  That normalization
        /// includes: removal of a volume letter and colon, swapping backward slashes
        /// for forward slashes.  So, <c>zip["dir1\\entry1.txt"].FileName ==
        /// "dir1/entry.txt"</c>.
        /// </para>
        /// 
        /// <para>
        /// Directory entries in the zip file may be retrieved via this indexer only
        /// with names that have a trailing slash. DotNetZip automatically appends a
        /// trailing slash to the names of any directory entries added to a zip.
        /// </para>
        /// 
        /// </remarks>
        /// 
        /// <example>
        /// This example extracts only the entries in a zip file that are .txt files.
        /// <code>
        /// using (ZipFile zip = ZipFile.Read("PackedDocuments.zip"))
        /// {
        /// foreach (string s1 in zip.EntryFilenames)
        /// {
        /// if (s1.EndsWith(".txt"))
        /// zip[s1].Extract("textfiles");
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
        /// <seealso cref="M:DotNetZipAdditionalPlatforms.Zip.ZipFile.RemoveEntry(System.String)" />
        /// 
        /// <exception cref="T:System.ArgumentException">
        /// Thrown if the caller attempts to assign a non-null value to the indexer.
        /// </exception>
        /// 
        /// <param name="fileName">
        /// The name of the file, including any directory path, to retrieve from the
        /// zip.  The filename match is not case-sensitive by default; you can use the
        /// <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.CaseSensitiveRetrieval" /> property to change this behavior. The
        /// pathname can use forward-slashes or backward slashes.
        /// </param>
        /// 
        /// <returns>
        /// The <c>ZipEntry</c> within the Zip archive, given by the specified
        /// filename. If the named entry does not exist in the archive, this indexer
        /// returns <c>null</c> (<c>Nothing</c> in VB).
        /// </returns>
        public ZipEntry this[string fileName]
        {
            get
            {
                string key = SharedUtilities.NormalizePathForUseInZipFile(fileName);
                if (this._entries.ContainsKey(key))
                {
                    return this._entries[key];
                }
                key = key.Replace("/", @"\");
                if (this._entries.ContainsKey(key))
                {
                    return this._entries[key];
                }
                return null;
            }
        }

        /// <summary>
        /// This is an integer indexer into the Zip archive.
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// This property is read-only.
        /// </para>
        /// 
        /// <para>
        /// Internally, the <c>ZipEntry</c> instances that belong to the
        /// <c>ZipFile</c> are stored in a Dictionary.  When you use this
        /// indexer the first time, it creates a read-only
        /// <c>List&lt;ZipEntry&gt;</c> from the Dictionary.Values Collection.
        /// If at any time you modify the set of entries in the <c>ZipFile</c>,
        /// either by adding an entry, removing an entry, or renaming an
        /// entry, a new List will be created, and the numeric indexes for the
        /// remaining entries may be different.
        /// </para>
        /// 
        /// <para>
        /// This means you cannot rename any ZipEntry from
        /// inside an enumeration of the zip file.
        /// </para>
        /// 
        /// <param name="ix">
        /// The index value.
        /// </param>
        /// 
        /// </remarks>
        /// 
        /// <returns>
        /// The <c>ZipEntry</c> within the Zip archive at the specified index. If the
        /// entry does not exist in the archive, this indexer throws.
        /// </returns>
        public ZipEntry this[int ix]
        {
            get
            {
                return this.ZipEntriesAsList[ix];
            }
        }

        private long LengthOfReadStream
        {
            get
            {
                if (this._lengthOfReadStream == -99L)
                {
                    this._lengthOfReadStream = this._ReadStreamIsOurs ? SharedUtilities.GetFileLength(this._name) : -1L;
                }
                return this._lengthOfReadStream;
            }
        }

        /// <summary>
        /// Returns the version number on the DotNetZip assembly.
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// This property is exposed as a convenience.  Callers could also get the
        /// version value by retrieving GetName().Version on the
        /// System.Reflection.Assembly object pointing to the DotNetZip
        /// assembly. But sometimes it is not clear which assembly is being loaded.
        /// This property makes it clear.
        /// </para>
        /// <para>
        /// This static property is primarily useful for diagnostic purposes.
        /// </para>
        /// </remarks>
        public static Version LibraryVersion
        {
            get
            {
                return Assembly.GetExecutingAssembly().GetName().Version;
            }
        }

        /// <summary>
        /// The maximum size of an output segment, when saving a split Zip file.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Set this to a non-zero value before calling <see cref="M:DotNetZipAdditionalPlatforms.Zip.ZipFile.Save" /> or <see cref="M:DotNetZipAdditionalPlatforms.Zip.ZipFile.Save(System.String)" /> to specify that the ZipFile should be saved as a
        /// split archive, also sometimes called a spanned archive. Some also
        /// call them multi-file archives.
        /// </para>
        /// 
        /// <para>
        /// A split zip archive is saved in a set of discrete filesystem files,
        /// rather than in a single file. This is handy when transmitting the
        /// archive in email or some other mechanism that has a limit to the size of
        /// each file.  The first file in a split archive will be named
        /// <c>basename.z01</c>, the second will be named <c>basename.z02</c>, and
        /// so on. The final file is named <c>basename.zip</c>. According to the zip
        /// specification from PKWare, the minimum value is 65536, for a 64k segment
        /// size. The maximum number of segments allows in a split archive is 99.
        /// </para>
        /// 
        /// <para>
        /// The value of this property determines the maximum size of a split
        /// segment when writing a split archive.  For example, suppose you have a
        /// <c>ZipFile</c> that would save to a single file of 200k. If you set the
        /// <c>MaxOutputSegmentSize</c> to 65536 before calling <c>Save()</c>, you
        /// will get four distinct output files. On the other hand if you set this
        /// property to 256k, then you will get a single-file archive for that
        /// <c>ZipFile</c>.
        /// </para>
        /// 
        /// <para>
        /// The size of each split output file will be as large as possible, up to
        /// the maximum size set here. The zip specification requires that some data
        /// fields in a zip archive may not span a split boundary, and an output
        /// segment may be smaller than the maximum if necessary to avoid that
        /// problem. Also, obviously the final segment of the archive may be smaller
        /// than the maximum segment size. Segments will never be larger than the
        /// value set with this property.
        /// </para>
        /// 
        /// <para>
        /// You can save a split Zip file only when saving to a regular filesystem
        /// file. It's not possible to save a split zip file as a self-extracting
        /// archive, nor is it possible to save a split zip file to a stream. When
        /// saving to a SFX or to a Stream, this property is ignored.
        /// </para>
        /// 
        /// <para>
        /// About interoperability: Split or spanned zip files produced by DotNetZip
        /// can be read by WinZip or PKZip, and vice-versa. Segmented zip files may
        /// not be readable by other tools, if those other tools don't support zip
        /// spanning or splitting.  When in doubt, test.  I don't believe Windows
        /// Explorer can extract a split archive.
        /// </para>
        /// 
        /// <para>
        /// This property has no effect when reading a split archive. You can read
        /// a split archive in the normal way with DotNetZip.
        /// </para>
        /// 
        /// <para>
        /// When saving a zip file, if you want a regular zip file rather than a
        /// split zip file, don't set this property, or set it to Zero.
        /// </para>
        /// 
        /// <para>
        /// If you read a split archive, with <see cref="M:DotNetZipAdditionalPlatforms.Zip.ZipFile.Read(System.String)" /> and
        /// then subsequently call <c>ZipFile.Save()</c>, unless you set this
        /// property before calling <c>Save()</c>, you will get a normal,
        /// single-file archive.
        /// </para>
        /// </remarks>
        /// 
        /// <seealso cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.NumberOfSegmentsForMostRecentSave" />
        public int MaxOutputSegmentSize
        {
            get
            {
                return this._maxOutputSegmentSize;
            }
            set
            {
                if ((value < 0x10000) && (value != 0))
                {
                    throw new ZipException("The minimum acceptable segment size is 65536.");
                }
                this._maxOutputSegmentSize = value;
            }
        }

        /// <summary>
        /// The name of the <c>ZipFile</c>, on disk.
        /// </summary>
        /// 
        /// <remarks>
        /// 
        /// <para>
        /// When the <c>ZipFile</c> instance was created by reading an archive using
        /// one of the <c>ZipFile.Read</c> methods, this property represents the name
        /// of the zip file that was read.  When the <c>ZipFile</c> instance was
        /// created by using the no-argument constructor, this value is <c>null</c>
        /// (<c>Nothing</c> in VB).
        /// </para>
        /// 
        /// <para>
        /// If you use the no-argument constructor, and you then explicitly set this
        /// property, when you call <see cref="M:DotNetZipAdditionalPlatforms.Zip.ZipFile.Save" />, this name will
        /// specify the name of the zip file created.  Doing so is equivalent to
        /// calling <see cref="M:DotNetZipAdditionalPlatforms.Zip.ZipFile.Save(System.String)" />.  When instantiating a
        /// <c>ZipFile</c> by reading from a stream or byte array, the <c>Name</c>
        /// property remains <c>null</c>.  When saving to a stream, the <c>Name</c>
        /// property is implicitly set to <c>null</c>.
        /// </para>
        /// </remarks>
        public string Name
        {
            get
            {
                return this._name;
            }
            set
            {
                this._name = value;
            }
        }

        /// <summary>
        /// Returns the number of segments used in the most recent Save() operation.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This is normally zero, unless you have set the <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.MaxOutputSegmentSize" /> property.  If you have set <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.MaxOutputSegmentSize" />, and then you save a file, after the call to
        /// Save() completes, you can read this value to learn the number of segments that
        /// were created.
        /// </para>
        /// <para>
        /// If you call Save("Archive.zip"), and it creates 5 segments, then you
        /// will have filesystem files named Archive.z01, Archive.z02, Archive.z03,
        /// Archive.z04, and Archive.zip, and the value of this property will be 5.
        /// </para>
        /// </remarks>
        /// <seealso cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.MaxOutputSegmentSize" />
        public int NumberOfSegmentsForMostRecentSave
        {
            get
            {
                return (((int) this._numberOfSegmentsForMostRecentSave) + 1);
            }
        }

        /// <summary>
        /// Indicates whether the most recent <c>Save()</c> operation used ZIP64 extensions.
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// The use of ZIP64 extensions within an archive is not always necessary, and
        /// for interoperability concerns, it may be desired to NOT use ZIP64 if
        /// possible.  The <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.UseZip64WhenSaving" /> property can be
        /// set to use ZIP64 extensions only when necessary.  In those cases,
        /// Sometimes applications want to know whether a Save() actually used ZIP64
        /// extensions.  Applications can query this read-only property to learn
        /// whether ZIP64 has been used in a just-saved <c>ZipFile</c>.
        /// </para>
        /// 
        /// <para>
        /// The value is <c>null</c> (or <c>Nothing</c> in VB) if the archive has not
        /// been saved.
        /// </para>
        /// 
        /// <para>
        /// Non-null values (<c>HasValue</c> is true) indicate whether ZIP64
        /// extensions were used during the most recent <c>Save()</c> operation.  The
        /// ZIP64 extensions may have been used as required by any particular entry
        /// because of its uncompressed or compressed size, or because the archive is
        /// larger than 4294967295 bytes, or because there are more than 65534 entries
        /// in the archive, or because the <c>UseZip64WhenSaving</c> property was set
        /// to <see cref="F:DotNetZipAdditionalPlatforms.Zip.Zip64Option.Always" />, or because the
        /// <c>UseZip64WhenSaving</c> property was set to <see cref="F:DotNetZipAdditionalPlatforms.Zip.Zip64Option.AsNecessary" /> and the output stream was not seekable.
        /// The value of this property does not indicate the reason the ZIP64
        /// extensions were used.
        /// </para>
        /// 
        /// </remarks>
        /// <seealso cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.UseZip64WhenSaving" />
        /// <seealso cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.RequiresZip64" />
        public bool? OutputUsedZip64
        {
            get
            {
                return this._OutputUsesZip64;
            }
        }

        /// <summary>
        /// The maximum number of buffer pairs to use when performing
        /// parallel compression.
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// This property sets an upper limit on the number of memory
        /// buffer pairs to create when performing parallel
        /// compression.  The implementation of the parallel
        /// compression stream allocates multiple buffers to
        /// facilitate parallel compression.  As each buffer fills up,
        /// the stream uses <see cref="M:System.Threading.ThreadPool.QueueUserWorkItem(System.Threading.WaitCallback)">
        /// ThreadPool.QueueUserWorkItem()</see> to compress those
        /// buffers in a background threadpool thread. After a buffer
        /// is compressed, it is re-ordered and written to the output
        /// stream.
        /// </para>
        /// 
        /// <para>
        /// A higher number of buffer pairs enables a higher degree of
        /// parallelism, which tends to increase the speed of compression on
        /// multi-cpu computers.  On the other hand, a higher number of buffer
        /// pairs also implies a larger memory consumption, more active worker
        /// threads, and a higher cpu utilization for any compression. This
        /// property enables the application to limit its memory consumption and
        /// CPU utilization behavior depending on requirements.
        /// </para>
        /// 
        /// <para>
        /// For each compression "task" that occurs in parallel, there are 2
        /// buffers allocated: one for input and one for output.  This property
        /// sets a limit for the number of pairs.  The total amount of storage
        /// space allocated for buffering will then be (N*S*2), where N is the
        /// number of buffer pairs, S is the size of each buffer (<see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.BufferSize" />).  By default, DotNetZip allocates 4 buffer
        /// pairs per CPU core, so if your machine has 4 cores, and you retain
        /// the default buffer size of 128k, then the
        /// ParallelDeflateOutputStream will use 4 * 4 * 2 * 128kb of buffer
        /// memory in total, or 4mb, in blocks of 128kb.  If you then set this
        /// property to 8, then the number will be 8 * 2 * 128kb of buffer
        /// memory, or 2mb.
        /// </para>
        /// 
        /// <para>
        /// CPU utilization will also go up with additional buffers, because a
        /// larger number of buffer pairs allows a larger number of background
        /// threads to compress in parallel. If you find that parallel
        /// compression is consuming too much memory or CPU, you can adjust this
        /// value downward.
        /// </para>
        /// 
        /// <para>
        /// The default value is 16. Different values may deliver better or
        /// worse results, depending on your priorities and the dynamic
        /// performance characteristics of your storage and compute resources.
        /// </para>
        /// 
        /// <para>
        /// This property is not the number of buffer pairs to use; it is an
        /// upper limit. An illustration: Suppose you have an application that
        /// uses the default value of this property (which is 16), and it runs
        /// on a machine with 2 CPU cores. In that case, DotNetZip will allocate
        /// 4 buffer pairs per CPU core, for a total of 8 pairs.  The upper
        /// limit specified by this property has no effect.
        /// </para>
        /// 
        /// <para>
        /// The application can set this value at any time
        /// before calling <c>ZipFile.Save()</c>.
        /// </para>
        /// </remarks>
        /// 
        /// <seealso cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.ParallelDeflateThreshold" />
        public int ParallelDeflateMaxBufferPairs
        {
            get
            {
                return this._maxBufferPairs;
            }
            set
            {
                if (value < 4)
                {
                    throw new ArgumentOutOfRangeException("ParallelDeflateMaxBufferPairs", "Value must be 4 or greater.");
                }
                this._maxBufferPairs = value;
            }
        }

        /// <summary>
        /// The size threshold for an entry, above which a parallel deflate is used.
        /// </summary>
        /// 
        /// <remarks>
        /// 
        /// <para>
        /// DotNetZip will use multiple threads to compress any ZipEntry,
        /// if the entry is larger than the given size.  Zero means "always
        /// use parallel deflate", while -1 means "never use parallel
        /// deflate". The default value for this property is 512k. Aside
        /// from the special values of 0 and 1, the minimum value is 65536.
        /// </para>
        /// 
        /// <para>
        /// If the entry size cannot be known before compression, as with a
        /// read-forward stream, then Parallel deflate will never be
        /// performed, unless the value of this property is zero.
        /// </para>
        /// 
        /// <para>
        /// A parallel deflate operations will speed up the compression of
        /// large files, on computers with multiple CPUs or multiple CPU
        /// cores.  For files above 1mb, on a dual core or dual-cpu (2p)
        /// machine, the time required to compress the file can be 70% of the
        /// single-threaded deflate.  For very large files on 4p machines the
        /// compression can be done in 30% of the normal time.  The downside
        /// is that parallel deflate consumes extra memory during the deflate,
        /// and the deflation is not as effective.
        /// </para>
        /// 
        /// <para>
        /// Parallel deflate tends to yield slightly less compression when
        /// compared to as single-threaded deflate; this is because the original
        /// data stream is split into multiple independent buffers, each of which
        /// is compressed in parallel.  But because they are treated
        /// independently, there is no opportunity to share compression
        /// dictionaries.  For that reason, a deflated stream may be slightly
        /// larger when compressed using parallel deflate, as compared to a
        /// traditional single-threaded deflate. Sometimes the increase over the
        /// normal deflate is as much as 5% of the total compressed size. For
        /// larger files it can be as small as 0.1%.
        /// </para>
        /// 
        /// <para>
        /// Multi-threaded compression does not give as much an advantage when
        /// using Encryption. This is primarily because encryption tends to slow
        /// down the entire pipeline. Also, multi-threaded compression gives less
        /// of an advantage when using lower compression levels, for example <see cref="F:DotNetZipAdditionalPlatforms.Zlib.CompressionLevel.BestSpeed" />.  You may have to
        /// perform some tests to determine the best approach for your situation.
        /// </para>
        /// 
        /// </remarks>
        /// 
        /// <seealso cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.ParallelDeflateMaxBufferPairs" />
        public long ParallelDeflateThreshold
        {
            get
            {
                return this._ParallelDeflateThreshold;
            }
            set
            {
                if (((value != 0L) && (value != -1L)) && (value < 0x10000L))
                {
                    throw new ArgumentOutOfRangeException("ParallelDeflateThreshold should be -1, 0, or > 65536");
                }
                this._ParallelDeflateThreshold = value;
            }
        }

        /// <summary>
        /// Sets the password to be used on the <c>ZipFile</c> instance.
        /// </summary>
        /// 
        /// <remarks>
        /// 
        /// <para>
        /// When writing a zip archive, this password is applied to the entries, not
        /// to the zip archive itself. It applies to any <c>ZipEntry</c> subsequently
        /// added to the <c>ZipFile</c>, using one of the <c>AddFile</c>,
        /// <c>AddDirectory</c>, <c>AddEntry</c>, or <c>AddItem</c> methods, etc.
        /// When reading a zip archive, this property applies to any entry
        /// subsequently extracted from the <c>ZipFile</c> using one of the Extract
        /// methods on the <c>ZipFile</c> class.
        /// </para>
        /// 
        /// <para>
        /// When writing a zip archive, keep this in mind: though the password is set
        /// on the ZipFile object, according to the Zip spec, the "directory" of the
        /// archive - in other words the list of entries or files contained in the archive - is
        /// not encrypted with the password, or protected in any way.  If you set the
        /// Password property, the password actually applies to individual entries
        /// that are added to the archive, subsequent to the setting of this property.
        /// The list of filenames in the archive that is eventually created will
        /// appear in clear text, but the contents of the individual files are
        /// encrypted.  This is how Zip encryption works.
        /// </para>
        /// 
        /// <para>
        /// One simple way around this limitation is to simply double-wrap sensitive
        /// filenames: Store the files in a zip file, and then store that zip file
        /// within a second, "outer" zip file.  If you apply a password to the outer
        /// zip file, then readers will be able to see that the outer zip file
        /// contains an inner zip file.  But readers will not be able to read the
        /// directory or file list of the inner zip file.
        /// </para>
        /// 
        /// <para>
        /// If you set the password on the <c>ZipFile</c>, and then add a set of files
        /// to the archive, then each entry is encrypted with that password.  You may
        /// also want to change the password between adding different entries. If you
        /// set the password, add an entry, then set the password to <c>null</c>
        /// (<c>Nothing</c> in VB), and add another entry, the first entry is
        /// encrypted and the second is not.  If you call <c>AddFile()</c>, then set
        /// the <c>Password</c> property, then call <c>ZipFile.Save</c>, the file
        /// added will not be password-protected, and no warning will be generated.
        /// </para>
        /// 
        /// <para>
        /// When setting the Password, you may also want to explicitly set the <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.Encryption" /> property, to specify how to encrypt the entries added
        /// to the ZipFile.  If you set the Password to a non-null value and do not
        /// set <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.Encryption" />, then PKZip 2.0 ("Weak") encryption is used.
        /// This encryption is relatively weak but is very interoperable. If you set
        /// the password to a <c>null</c> value (<c>Nothing</c> in VB), Encryption is
        /// reset to None.
        /// </para>
        /// 
        /// <para>
        /// All of the preceding applies to writing zip archives, in other words when
        /// you use one of the Save methods.  To use this property when reading or an
        /// existing ZipFile, do the following: set the Password property on the
        /// <c>ZipFile</c>, then call one of the Extract() overloads on the <see cref="T:DotNetZipAdditionalPlatforms.Zip.ZipEntry" />. In this case, the entry is extracted using the
        /// <c>Password</c> that is specified on the <c>ZipFile</c> instance. If you
        /// have not set the <c>Password</c> property, then the password is
        /// <c>null</c>, and the entry is extracted with no password.
        /// </para>
        /// 
        /// <para>
        /// If you set the Password property on the <c>ZipFile</c>, then call
        /// <c>Extract()</c> an entry that has not been encrypted with a password, the
        /// password is not used for that entry, and the <c>ZipEntry</c> is extracted
        /// as normal. In other words, the password is used only if necessary.
        /// </para>
        /// 
        /// <para>
        /// The <see cref="T:DotNetZipAdditionalPlatforms.Zip.ZipEntry" /> class also has a <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipEntry.Password">Password</see> property.  It takes precedence
        /// over this property on the <c>ZipFile</c>.  Typically, you would use the
        /// per-entry Password when most entries in the zip archive use one password,
        /// and a few entries use a different password.  If all entries in the zip
        /// file use the same password, then it is simpler to just set this property
        /// on the <c>ZipFile</c> itself, whether creating a zip archive or extracting
        /// a zip archive.
        /// </para>
        /// 
        /// </remarks>
        /// 
        /// <example>
        /// <para>
        /// This example creates a zip file, using password protection for the
        /// entries, and then extracts the entries from the zip file.  When creating
        /// the zip file, the Readme.txt file is not protected with a password, but
        /// the other two are password-protected as they are saved. During extraction,
        /// each file is extracted with the appropriate password.
        /// </para>
        /// <code>
        /// // create a file with encryption
        /// using (ZipFile zip = new ZipFile())
        /// {
        /// zip.AddFile("ReadMe.txt");
        /// zip.Password= "!Secret1";
        /// zip.AddFile("MapToTheSite-7440-N49th.png");
        /// zip.AddFile("2008-Regional-Sales-Report.pdf");
        /// zip.Save("EncryptedArchive.zip");
        /// }
        /// 
        /// // extract entries that use encryption
        /// using (ZipFile zip = ZipFile.Read("EncryptedArchive.zip"))
        /// {
        /// zip.Password= "!Secret1";
        /// zip.ExtractAll("extractDir");
        /// }
        /// 
        /// </code>
        /// 
        /// <code lang="VB">
        /// Using zip As New ZipFile
        /// zip.AddFile("ReadMe.txt")
        /// zip.Password = "123456!"
        /// zip.AddFile("MapToTheSite-7440-N49th.png")
        /// zip.Password= "!Secret1";
        /// zip.AddFile("2008-Regional-Sales-Report.pdf")
        /// zip.Save("EncryptedArchive.zip")
        /// End Using
        /// 
        /// 
        /// ' extract entries that use encryption
        /// Using (zip as ZipFile = ZipFile.Read("EncryptedArchive.zip"))
        /// zip.Password= "!Secret1"
        /// zip.ExtractAll("extractDir")
        /// End Using
        /// 
        /// </code>
        /// 
        /// </example>
        /// 
        /// <seealso cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.Encryption">ZipFile.Encryption</seealso>
        /// <seealso cref="P:DotNetZipAdditionalPlatforms.Zip.ZipEntry.Password">ZipEntry.Password</seealso>
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
                    this.Encryption = EncryptionAlgorithm.None;
                }
                else if (this.Encryption == EncryptionAlgorithm.None)
                {
                    this.Encryption = EncryptionAlgorithm.PkzipWeak;
                }
            }
        }

        internal Stream ReadStream
        {
            get
            {
                if ((this._readstream == null) && ((this._readName != null) || (this._name != null)))
                {
                    this._readstream = File.Open(this._readName ?? this._name, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    this._ReadStreamIsOurs = true;
                }
                return this._readstream;
            }
        }

        /// <summary>
        /// Indicates whether the archive requires ZIP64 extensions.
        /// </summary>
        /// 
        /// <remarks>
        /// 
        /// <para>
        /// This property is <c>null</c> (or <c>Nothing</c> in VB) if the archive has
        /// not been saved, and there are fewer than 65334 <c>ZipEntry</c> items
        /// contained in the archive.
        /// </para>
        /// 
        /// <para>
        /// The <c>Value</c> is true if any of the following four conditions holds:
        /// the uncompressed size of any entry is larger than 0xFFFFFFFF; the
        /// compressed size of any entry is larger than 0xFFFFFFFF; the relative
        /// offset of any entry within the zip archive is larger than 0xFFFFFFFF; or
        /// there are more than 65534 entries in the archive.  (0xFFFFFFFF =
        /// 4,294,967,295).  The result may not be known until a <c>Save()</c> is attempted
        /// on the zip archive.  The Value of this <see cref="T:System.Nullable" />
        /// property may be set only AFTER one of the Save() methods has been called.
        /// </para>
        /// 
        /// <para>
        /// If none of the four conditions holds, and the archive has been saved, then
        /// the <c>Value</c> is false.
        /// </para>
        /// 
        /// <para>
        /// A <c>Value</c> of false does not indicate that the zip archive, as saved,
        /// does not use ZIP64.  It merely indicates that ZIP64 is not required.  An
        /// archive may use ZIP64 even when not required if the <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.UseZip64WhenSaving" /> property is set to <see cref="F:DotNetZipAdditionalPlatforms.Zip.Zip64Option.Always" />, or if the <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.UseZip64WhenSaving" /> property is set to <see cref="F:DotNetZipAdditionalPlatforms.Zip.Zip64Option.AsNecessary" /> and the output stream was not
        /// seekable. Use the <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.OutputUsedZip64" /> property to determine if
        /// the most recent <c>Save()</c> method resulted in an archive that utilized
        /// the ZIP64 extensions.
        /// </para>
        /// 
        /// </remarks>
        /// <seealso cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.UseZip64WhenSaving" />
        /// <seealso cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.OutputUsedZip64" />
        public bool? RequiresZip64
        {
            get
            {
                if (this._entries.Count > 0xfffe)
                {
                    return true;
                }
                if (!(this._hasBeenSaved && !this._contentsChanged))
                {
                    return null;
                }
                foreach (ZipEntry entry in this._entries.Values)
                {
                    if (entry.RequiresZip64.Value)
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        /// <summary>
        /// A callback that allows the application to specify the compression level
        /// to use for entries subsequently added to the zip archive.
        /// </summary>
        /// 
        /// <remarks>
        /// 
        /// <para>
        /// With this callback, the DotNetZip library allows the application to
        /// determine whether compression will be used, at the time of the
        /// <c>Save</c>. This may be useful if the application wants to favor
        /// speed over size, and wants to defer the decision until the time of
        /// <c>Save</c>.
        /// </para>
        /// 
        /// <para>
        /// Typically applications set the <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.CompressionLevel" /> property on
        /// the <c>ZipFile</c> or on each <c>ZipEntry</c> to determine the level of
        /// compression used. This is done at the time the entry is added to the
        /// <c>ZipFile</c>. Setting the property to
        /// <c>DotNetZipAdditionalPlatforms.Zlib.CompressionLevel.None</c> means no compression will be used.
        /// </para>
        /// 
        /// <para>
        /// This callback allows the application to defer the decision on the
        /// <c>CompressionLevel</c> to use, until the time of the call to
        /// <c>ZipFile.Save()</c>. The callback is invoked once per <c>ZipEntry</c>,
        /// at the time the data for the entry is being written out as part of a
        /// <c>Save()</c> operation. The application can use whatever criteria it
        /// likes in determining the level to return.  For example, an application may
        /// wish that no .mp3 files should be compressed, because they are already
        /// compressed and the extra compression is not worth the CPU time incurred,
        /// and so can return <c>None</c> for all .mp3 entries.
        /// </para>
        /// 
        /// <para>
        /// The library determines whether compression will be attempted for an entry
        /// this way: If the entry is a zero length file, or a directory, no
        /// compression is used.  Otherwise, if this callback is set, it is invoked
        /// and the <c>CompressionLevel</c> is set to the return value. If this
        /// callback has not been set, then the previously set value for
        /// <c>CompressionLevel</c> is used.
        /// </para>
        /// 
        /// </remarks>
        public SetCompressionCallback SetCompression { get; set; }

        /// <summary>
        /// Whether to sort the ZipEntries before saving the file.
        /// </summary>
        /// 
        /// <remarks>
        /// The default is false.  If you have a large number of zip entries, the sort
        /// alone can consume significant time.
        /// </remarks>
        /// 
        /// <example>
        /// <code lang="C#">
        /// using (var zip = new ZipFile())
        /// {
        /// zip.AddFiles(filesToAdd);
        /// zip.SortEntriesBeforeSaving = true;
        /// zip.Save(name);
        /// }
        /// </code>
        /// 
        /// <code lang="VB">
        /// Using zip As New ZipFile
        /// zip.AddFiles(filesToAdd)
        /// zip.SortEntriesBeforeSaving = True
        /// zip.Save(name)
        /// End Using
        /// </code>
        /// </example>
        public bool SortEntriesBeforeSaving { get; set; }

        /// <summary>
        /// Gets or sets the <c>TextWriter</c> to which status messages are delivered
        /// for the instance.
        /// </summary>
        /// 
        /// <remarks>
        /// If the TextWriter is set to a non-null value, then verbose output is sent
        /// to the <c>TextWriter</c> during <c>Add</c><c>, Read</c><c>, Save</c> and
        /// <c>Extract</c> operations.  Typically, console applications might use
        /// <c>Console.Out</c> and graphical or headless applications might use a
        /// <c>System.IO.StringWriter</c>. The output of this is suitable for viewing
        /// by humans.
        /// </remarks>
        /// 
        /// <example>
        /// <para>
        /// In this example, a console application instantiates a <c>ZipFile</c>, then
        /// sets the <c>StatusMessageTextWriter</c> to <c>Console.Out</c>.  At that
        /// point, all verbose status messages for that <c>ZipFile</c> are sent to the
        /// console.
        /// </para>
        /// 
        /// <code lang="C#">
        /// using (ZipFile zip= ZipFile.Read(FilePath))
        /// {
        /// zip.StatusMessageTextWriter= System.Console.Out;
        /// // messages are sent to the console during extraction
        /// zip.ExtractAll();
        /// }
        /// </code>
        /// 
        /// <code lang="VB">
        /// Using zip As ZipFile = ZipFile.Read(FilePath)
        /// zip.StatusMessageTextWriter= System.Console.Out
        /// 'Status Messages will be sent to the console during extraction
        /// zip.ExtractAll()
        /// End Using
        /// </code>
        /// 
        /// <para>
        /// In this example, a Windows Forms application instantiates a
        /// <c>ZipFile</c>, then sets the <c>StatusMessageTextWriter</c> to a
        /// <c>StringWriter</c>.  At that point, all verbose status messages for that
        /// <c>ZipFile</c> are sent to the <c>StringWriter</c>.
        /// </para>
        /// 
        /// <code lang="C#">
        /// var sw = new System.IO.StringWriter();
        /// using (ZipFile zip= ZipFile.Read(FilePath))
        /// {
        /// zip.StatusMessageTextWriter= sw;
        /// zip.ExtractAll();
        /// }
        /// Console.WriteLine("{0}", sw.ToString());
        /// </code>
        /// 
        /// <code lang="VB">
        /// Dim sw as New System.IO.StringWriter
        /// Using zip As ZipFile = ZipFile.Read(FilePath)
        /// zip.StatusMessageTextWriter= sw
        /// zip.ExtractAll()
        /// End Using
        /// 'Status Messages are now available in sw
        /// 
        /// </code>
        /// </example>
        public TextWriter StatusMessageTextWriter
        {
            get
            {
                return this._StatusMessageTextWriter;
            }
            set
            {
                this._StatusMessageTextWriter = value;
            }
        }

        /// <summary>
        /// The compression strategy to use for all entries.
        /// </summary>
        /// 
        /// <remarks>
        /// Set the Strategy used by the ZLIB-compatible compressor, when
        /// compressing entries using the DEFLATE method. Different compression
        /// strategies work better on different sorts of data. The strategy
        /// parameter can affect the compression ratio and the speed of
        /// compression but not the correctness of the compresssion.  For more
        /// information see <see cref="T:DotNetZipAdditionalPlatforms.Zlib.CompressionStrategy">DotNetZipAdditionalPlatforms.Zlib.CompressionStrategy</see>.
        /// </remarks>
        public CompressionStrategy Strategy
        {
            get
            {
                return this._Strategy;
            }
            set
            {
                this._Strategy = value;
            }
        }

        /// <summary>
        /// Gets or sets the name for the folder to store the temporary file
        /// this library writes when saving a zip archive.
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// This library will create a temporary file when saving a Zip archive to a
        /// file.  This file is written when calling one of the <c>Save()</c> methods
        /// that does not save to a stream, or one of the <c>SaveSelfExtractor()</c>
        /// methods.
        /// </para>
        /// 
        /// <para>
        /// By default, the library will create the temporary file in the directory
        /// specified for the file itself, via the <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.Name" /> property or via
        /// the <see cref="M:DotNetZipAdditionalPlatforms.Zip.ZipFile.Save(System.String)" /> method.
        /// </para>
        /// 
        /// <para>
        /// Setting this property allows applications to override this default
        /// behavior, so that the library will create the temporary file in the
        /// specified folder. For example, to have the library create the temporary
        /// file in the current working directory, regardless where the <c>ZipFile</c>
        /// is saved, specfy ".".  To revert to the default behavior, set this
        /// property to <c>null</c> (<c>Nothing</c> in VB).
        /// </para>
        /// 
        /// <para>
        /// When setting the property to a non-null value, the folder specified must
        /// exist; if it does not an exception is thrown.  The application should have
        /// write and delete permissions on the folder.  The permissions are not
        /// explicitly checked ahead of time; if the application does not have the
        /// appropriate rights, an exception will be thrown at the time <c>Save()</c>
        /// is called.
        /// </para>
        /// 
        /// <para>
        /// There is no temporary file created when reading a zip archive.  When
        /// saving to a Stream, there is no temporary file created.  For example, if
        /// the application is an ASP.NET application and calls <c>Save()</c>
        /// specifying the <c>Response.OutputStream</c> as the output stream, there is
        /// no temporary file created.
        /// </para>
        /// </remarks>
        /// 
        /// <exception cref="T:System.IO.FileNotFoundException">
        /// Thrown when setting the property if the directory does not exist.
        /// </exception>
        public string TempFileFolder
        {
            get
            {
                return this._TempFileFolder;
            }
            set
            {
                this._TempFileFolder = value;
                if ((value != null) && !Directory.Exists(value))
                {
                    throw new FileNotFoundException(string.Format(CultureInfo.InvariantCulture, "That directory ({0}) does not exist.", value));
                }
            }
        }

        /// <summary>
        /// Specify whether to use ZIP64 extensions when saving a zip archive.
        /// </summary>
        /// 
        /// <remarks>
        /// 
        /// <para>
        /// When creating a zip file, the default value for the property is <see cref="F:DotNetZipAdditionalPlatforms.Zip.Zip64Option.Never" />. <see cref="F:DotNetZipAdditionalPlatforms.Zip.Zip64Option.AsNecessary" /> is
        /// safest, in the sense that you will not get an Exception if a pre-ZIP64
        /// limit is exceeded.
        /// </para>
        /// 
        /// <para>
        /// You may set the property at any time before calling Save().
        /// </para>
        /// 
        /// <para>
        /// When reading a zip file via the <c>Zipfile.Read()</c> method, DotNetZip
        /// will properly read ZIP64-endowed zip archives, regardless of the value of
        /// this property.  DotNetZip will always read ZIP64 archives.  This property
        /// governs only whether DotNetZip will write them. Therefore, when updating
        /// archives, be careful about setting this property after reading an archive
        /// that may use ZIP64 extensions.
        /// </para>
        /// 
        /// <para>
        /// An interesting question is, if you have set this property to
        /// <c>AsNecessary</c>, and then successfully saved, does the resulting
        /// archive use ZIP64 extensions or not?  To learn this, check the <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.OutputUsedZip64" /> property, after calling <c>Save()</c>.
        /// </para>
        /// 
        /// <para>
        /// Have you thought about
        /// <see href="http://cheeso.members.winisp.net/DotNetZipDonate.aspx">donating</see>?
        /// </para>
        /// 
        /// </remarks>
        /// <seealso cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.RequiresZip64" />
        public Zip64Option UseZip64WhenSaving
        {
            get
            {
                return this._zip64;
            }
            set
            {
                this._zip64 = value;
            }
        }

        /// <summary>
        /// Indicates whether verbose output is sent to the <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.StatusMessageTextWriter" /> during <c>AddXxx()</c> and
        /// <c>ReadXxx()</c> operations.
        /// </summary>
        /// 
        /// <remarks>
        /// This is a <em>synthetic</em> property.  It returns true if the <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.StatusMessageTextWriter" /> is non-null.
        /// </remarks>
        internal bool Verbose
        {
            get
            {
                return (this._StatusMessageTextWriter != null);
            }
        }

        private Stream WriteStream
        {
            get
            {
                if (this._writestream == null)
                {
                    if (this._name == null)
                    {
                        return this._writestream;
                    }
                    if (this._maxOutputSegmentSize != 0)
                    {
                        this._writestream = ZipSegmentedStream.ForWriting(this._name, this._maxOutputSegmentSize);
                        return this._writestream;
                    }
                    SharedUtilities.CreateAndOpenUniqueTempFile(this.TempFileFolder ?? Path.GetDirectoryName(this._name), out this._writestream, out this._temporaryFileName);
                }
                return this._writestream;
            }
            set
            {
                if (value != null)
                {
                    throw new ZipException("Cannot set the stream to a non-null value.");
                }
                this._writestream = null;
            }
        }

        private List<ZipEntry> ZipEntriesAsList
        {
            get
            {
                if (this._zipEntriesAsList == null)
                {
                    this._zipEntriesAsList = new List<ZipEntry>(this._entries.Values);
                }
                return this._zipEntriesAsList;
            }
        }

        /// <summary>
        /// The action the library should take when an error is encountered while
        /// opening or reading files as they are saved into a zip archive.
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// Errors can occur as a file is being saved to the zip archive.  For
        /// example, the File.Open may fail, or a File.Read may fail, because of
        /// lock conflicts or other reasons.
        /// </para>
        /// 
        /// <para>
        /// The first problem might occur after having called AddDirectory() on a
        /// directory that contains a Clipper .dbf file; the file is locked by
        /// Clipper and cannot be opened for read by another process. An example of
        /// the second problem might occur when trying to zip a .pst file that is in
        /// use by Microsoft Outlook. Outlook locks a range on the file, which allows
        /// other processes to open the file, but not read it in its entirety.
        /// </para>
        /// 
        /// <para>
        /// This property tells DotNetZip what you would like to do in the case of
        /// these errors.  The primary options are: <c>ZipErrorAction.Throw</c> to
        /// throw an exception (this is the default behavior if you don't set this
        /// property); <c>ZipErrorAction.Skip</c> to Skip the file for which there
        /// was an error and continue saving; <c>ZipErrorAction.Retry</c> to Retry
        /// the entry that caused the problem; or
        /// <c>ZipErrorAction.InvokeErrorEvent</c> to invoke an event handler.
        /// </para>
        /// 
        /// <para>
        /// This property is implicitly set to <c>ZipErrorAction.InvokeErrorEvent</c>
        /// if you add a handler to the <see cref="E:DotNetZipAdditionalPlatforms.Zip.ZipFile.ZipError" /> event.  If you set
        /// this property to something other than
        /// <c>ZipErrorAction.InvokeErrorEvent</c>, then the <c>ZipError</c>
        /// event is implicitly cleared.  What it means is you can set one or the
        /// other (or neither), depending on what you want, but you never need to set
        /// both.
        /// </para>
        /// 
        /// <para>
        /// As with some other properties on the <c>ZipFile</c> class, like <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.Password" />, <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.Encryption" />, and <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.CompressionLevel" />, setting this property on a <c>ZipFile</c>
        /// instance will cause the specified <c>ZipErrorAction</c> to be used on all
        /// <see cref="T:DotNetZipAdditionalPlatforms.Zip.ZipEntry" /> items that are subsequently added to the
        /// <c>ZipFile</c> instance. If you set this property after you have added
        /// items to the <c>ZipFile</c>, but before you have called <c>Save()</c>,
        /// those items will not use the specified error handling action.
        /// </para>
        /// 
        /// <para>
        /// If you want to handle any errors that occur with any entry in the zip
        /// file in the same way, then set this property once, before adding any
        /// entries to the zip archive.
        /// </para>
        /// 
        /// <para>
        /// If you set this property to <c>ZipErrorAction.Skip</c> and you'd like to
        /// learn which files may have been skipped after a <c>Save()</c>, you can
        /// set the <see cref="P:DotNetZipAdditionalPlatforms.Zip.ZipFile.StatusMessageTextWriter" /> on the ZipFile before
        /// calling <c>Save()</c>. A message will be emitted into that writer for
        /// each skipped file, if any.
        /// </para>
        /// 
        /// </remarks>
        /// 
        /// <example>
        /// This example shows how to tell DotNetZip to skip any files for which an
        /// error is generated during the Save().
        /// <code lang="VB">
        /// Public Sub SaveZipFile()
        /// Dim SourceFolder As String = "fodder"
        /// Dim DestFile As String =  "eHandler.zip"
        /// Dim sw as New StringWriter
        /// Using zipArchive As ZipFile = New ZipFile
        /// ' Tell DotNetZip to skip any files for which it encounters an error
        /// zipArchive.ZipErrorAction = ZipErrorAction.Skip
        /// zipArchive.StatusMessageTextWriter = sw
        /// zipArchive.AddDirectory(SourceFolder)
        /// zipArchive.Save(DestFile)
        /// End Using
        /// ' examine sw here to see any messages
        /// End Sub
        /// 
        /// </code>
        /// </example>
        /// 
        /// <seealso cref="P:DotNetZipAdditionalPlatforms.Zip.ZipEntry.ZipErrorAction" />
        /// <seealso cref="E:DotNetZipAdditionalPlatforms.Zip.ZipFile.ZipError" />
        public DotNetZipAdditionalPlatforms.Zip.ZipErrorAction ZipErrorAction
        {
            get
            {
                if (this.ZipError != null)
                {
                    this._zipErrorAction = DotNetZipAdditionalPlatforms.Zip.ZipErrorAction.InvokeErrorEvent;
                }
                return this._zipErrorAction;
            }
            set
            {
                this._zipErrorAction = value;
                if ((this._zipErrorAction != DotNetZipAdditionalPlatforms.Zip.ZipErrorAction.InvokeErrorEvent) && (this.ZipError != null))
                {
                    this.ZipError = null;
                }
            }
        }


        private class ExtractorSettings
        {
            public List<string> CopyThroughResources;
            public SelfExtractorFlavor Flavor;
            public List<string> ReferencedAssemblies;
            public List<string> ResourcesToCompile;
        }
    }
}

