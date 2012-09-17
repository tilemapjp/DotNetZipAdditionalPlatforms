namespace DotNetZipAdditionalPlatforms.Zip
{
    using DotNetZipAdditionalPlatforms.Crc;
    using DotNetZipAdditionalPlatforms.Zlib;
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Runtime.CompilerServices;
    using System.Text;

    /// <summary>
    /// Provides a stream metaphor for generating zip files.
    /// </summary>
    /// 
    /// <remarks>
    /// <para>
    /// This class writes zip files, as defined in the <see href="http://www.pkware.com/documents/casestudies/APPNOTE.TXT">specification
    /// for zip files described by PKWare</see>.  The compression for this
    /// implementation is provided by a managed-code version of Zlib, included with
    /// DotNetZip in the classes in the DotNetZipAdditionalPlatforms.Zlib namespace.
    /// </para>
    /// 
    /// <para>
    /// This class provides an alternative programming model to the one enabled by the
    /// <see cref="T:ZipFile" /> class. Use this when creating zip files, as an
    /// alternative to the <see cref="T:ZipFile" /> class, when you would like to use a
    /// <c>Stream</c> type to write the zip file.
    /// </para>
    /// 
    /// <para>
    /// Both the <c>ZipOutputStream</c> class and the <c>ZipFile</c> class can be used
    /// to create zip files. Both of them support many of the common zip features,
    /// including Unicode, different compression levels, and ZIP64.   They provide
    /// very similar performance when creating zip files.
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
    /// call to <see cref="M:ZipFile.AddFile(System.String,System.String)">ZipFile.AddFile()</see>.
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
    /// to use a stream to read zip files, check out the <see cref="T:ZipInputStream" /> class.
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
    /// Be aware that the <c>ZipOutputStream</c> class implements the <see cref="T:System.IDisposable" /> interface.  In order for
    /// <c>ZipOutputStream</c> to produce a valid zip file, you use use it within
    /// a using clause (<c>Using</c> in VB), or call the <c>Dispose()</c> method
    /// explicitly.  See the examples for how to employ a using clause.
    /// </para>
    /// 
    /// <para>
    /// Also, a note regarding compression performance: On the desktop .NET
    /// Framework, DotNetZip can use a multi-threaded compression implementation
    /// that provides significant speed increases on large files, over 300k or so,
    /// at the cost of increased memory use at runtime.  (The output of the
    /// compression is almost exactly the same size).  But, the multi-threaded
    /// approach incurs a performance hit on smaller files. There's no way for the
    /// ZipOutputStream to know whether parallel compression will be beneficial,
    /// because the ZipOutputStream does not know how much data you will write
    /// through the stream.  You may wish to set the <see cref="P:ZipOutputStream.ParallelDeflateThreshold" /> property to zero, if you are compressing
    /// large files through <c>ZipOutputStream</c>.  This will cause parallel
    /// compression to be used, always.
    /// </para>
    /// </remarks>
    public class ZipOutputStream : Stream
    {
        private Encoding alternateEncodingField;
        private ZipOption alternateEncodingUsageField;
        private bool anyEntriesUsedZip64Field;
        private string commentField;
        private ZipEntry currentEntryField;
        private Stream deflaterField;
        private bool directoryNeededZip64Field;
        private bool disposedField;
        private bool dontIgnoreCaseField;
        private EncryptionAlgorithm encryptionField;
        private Stream encryptorField;
        private Dictionary<string, ZipEntry> entriesWrittenField;
        private int entryCountField;
        private CrcCalculatorStream entryOutputStreamField;
        private bool exceptionPendingField;
        private bool leaveUnderlyingStreamOpenField;
        private int maxBufferPairsField;
        private string nameField;
        private bool needToWriteEntryHeaderField;
        private CountingStream outputCounterField;
        private Stream outputStreamField;
        private long parallelDeflateThresholdField;
        internal string passwordField;
        private ZipEntryTimestamp timestampField;
        internal Zip64Option zip64Field;
        internal ParallelDeflateOutputStream parallelDeflaterField;

        /// <summary>
        /// Create a ZipOutputStream, wrapping an existing stream.
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// The <see cref="T:ZipFile" /> class is generally easier to use when creating
        /// zip files. The ZipOutputStream offers a different metaphor for creating a
        /// zip file, based on the <see cref="T:System.IO.Stream" /> class.
        /// </para>
        /// 
        /// </remarks>
        /// 
        /// <param name="stream">
        /// The stream to wrap. It must be writable. This stream will be closed at
        /// the time the ZipOutputStream is closed.
        /// </param>
        /// 
        /// <example>
        /// 
        /// This example shows how to create a zip file, using the
        /// ZipOutputStream class.
        /// 
        /// <code lang="C#">
        /// private void Zipup()
        /// {
        /// if (filesToZip.Count == 0)
        /// {
        /// System.Console.WriteLine("Nothing to do.");
        /// return;
        /// }
        /// 
        /// using (var raw = File.Open(_outputFileName, FileMode.Create, FileAccess.ReadWrite ))
        /// {
        /// using (var output= new ZipOutputStream(raw))
        /// {
        /// output.Password = "VerySecret!";
        /// output.Encryption = EncryptionAlgorithm.WinZipAes256;
        /// 
        /// foreach (string inputFileName in filesToZip)
        /// {
        /// System.Console.WriteLine("file: {0}", inputFileName);
        /// 
        /// output.PutNextEntry(inputFileName);
        /// using (var input = File.Open(inputFileName, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Write ))
        /// {
        /// byte[] buffer= new byte[2048];
        /// int n;
        /// while ((n= input.Read(buffer,0,buffer.Length)) &gt; 0)
        /// {
        /// output.Write(buffer,0,n);
        /// }
        /// }
        /// }
        /// }
        /// }
        /// }
        /// </code>
        /// 
        /// <code lang="VB">
        /// Private Sub Zipup()
        /// Dim outputFileName As String = "XmlData.zip"
        /// Dim filesToZip As String() = Directory.GetFiles(".", "*.xml")
        /// If (filesToZip.Length = 0) Then
        /// Console.WriteLine("Nothing to do.")
        /// Else
        /// Using raw As FileStream = File.Open(outputFileName, FileMode.Create, FileAccess.ReadWrite)
        /// Using output As ZipOutputStream = New ZipOutputStream(raw)
        /// output.Password = "VerySecret!"
        /// output.Encryption = EncryptionAlgorithm.WinZipAes256
        /// Dim inputFileName As String
        /// For Each inputFileName In filesToZip
        /// Console.WriteLine("file: {0}", inputFileName)
        /// output.PutNextEntry(inputFileName)
        /// Using input As FileStream = File.Open(inputFileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)
        /// Dim n As Integer
        /// Dim buffer As Byte() = New Byte(2048) {}
        /// Do While (n = input.Read(buffer, 0, buffer.Length) &gt; 0)
        /// output.Write(buffer, 0, n)
        /// Loop
        /// End Using
        /// Next
        /// End Using
        /// End Using
        /// End If
        /// End Sub
        /// </code>
        /// </example>
        public ZipOutputStream(Stream stream) : this(stream, false)
        {
        }

        /// <summary>
        /// Create a ZipOutputStream that writes to a filesystem file.
        /// </summary>
        /// 
        /// <remarks>
        /// The <see cref="T:ZipFile" /> class is generally easier to use when creating
        /// zip files. The ZipOutputStream offers a different metaphor for creating a
        /// zip file, based on the <see cref="T:System.IO.Stream" /> class.
        /// </remarks>
        /// 
        /// <param name="fileName">
        /// The name of the zip file to create.
        /// </param>
        /// 
        /// <example>
        /// 
        /// This example shows how to create a zip file, using the
        /// ZipOutputStream class.
        /// 
        /// <code lang="C#">
        /// private void Zipup()
        /// {
        /// if (filesToZip.Count == 0)
        /// {
        /// System.Console.WriteLine("Nothing to do.");
        /// return;
        /// }
        /// 
        /// using (var output= new ZipOutputStream(outputFileName))
        /// {
        /// output.Password = "VerySecret!";
        /// output.Encryption = EncryptionAlgorithm.WinZipAes256;
        /// 
        /// foreach (string inputFileName in filesToZip)
        /// {
        /// System.Console.WriteLine("file: {0}", inputFileName);
        /// 
        /// output.PutNextEntry(inputFileName);
        /// using (var input = File.Open(inputFileName, FileMode.Open, FileAccess.Read,
        /// FileShare.Read | FileShare.Write ))
        /// {
        /// byte[] buffer= new byte[2048];
        /// int n;
        /// while ((n= input.Read(buffer,0,buffer.Length)) &gt; 0)
        /// {
        /// output.Write(buffer,0,n);
        /// }
        /// }
        /// }
        /// }
        /// }
        /// </code>
        /// 
        /// <code lang="VB">
        /// Private Sub Zipup()
        /// Dim outputFileName As String = "XmlData.zip"
        /// Dim filesToZip As String() = Directory.GetFiles(".", "*.xml")
        /// If (filesToZip.Length = 0) Then
        /// Console.WriteLine("Nothing to do.")
        /// Else
        /// Using output As ZipOutputStream = New ZipOutputStream(outputFileName)
        /// output.Password = "VerySecret!"
        /// output.Encryption = EncryptionAlgorithm.WinZipAes256
        /// Dim inputFileName As String
        /// For Each inputFileName In filesToZip
        /// Console.WriteLine("file: {0}", inputFileName)
        /// output.PutNextEntry(inputFileName)
        /// Using input As FileStream = File.Open(inputFileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)
        /// Dim n As Integer
        /// Dim buffer As Byte() = New Byte(2048) {}
        /// Do While (n = input.Read(buffer, 0, buffer.Length) &gt; 0)
        /// output.Write(buffer, 0, n)
        /// Loop
        /// End Using
        /// Next
        /// End Using
        /// End If
        /// End Sub
        /// </code>
        /// </example>
        public ZipOutputStream(string fileName)
        {
            this.alternateEncodingUsageField = ZipOption.Default;
            this.alternateEncodingField = Encoding.GetEncoding("IBM437");
            this.maxBufferPairsField = 0x10;
            Stream stream = File.Open(fileName, FileMode.Create, FileAccess.ReadWrite, FileShare.None);
            this._Init(stream, false, fileName);
        }

        /// <summary>
        /// Create a ZipOutputStream.
        /// </summary>
        /// 
        /// <remarks>
        /// See the documentation for the <see cref="M:ZipOutputStream.#ctor(System.IO.Stream)">ZipOutputStream(Stream)</see>
        /// constructor for an example.
        /// </remarks>
        /// 
        /// <param name="stream">
        /// The stream to wrap. It must be writable.
        /// </param>
        /// 
        /// <param name="leaveOpen">
        /// true if the application would like the stream
        /// to remain open after the <c>ZipOutputStream</c> has been closed.
        /// </param>
        public ZipOutputStream(Stream stream, bool leaveOpen)
        {
            this.alternateEncodingUsageField = ZipOption.Default;
            this.alternateEncodingField = Encoding.GetEncoding("IBM437");
            this.maxBufferPairsField = 0x10;
            this._Init(stream, leaveOpen, null);
        }

        private void _FinishCurrentEntry()
        {
            if (this.currentEntryField != null)
            {
                if (this.needToWriteEntryHeaderField)
                {
                    this._InitiateCurrentEntry(true);
                }
                this.currentEntryField.FinishOutputStream(this.outputStreamField, this.outputCounterField, this.encryptorField, this.deflaterField, this.entryOutputStreamField);
                this.currentEntryField.PostProcessOutput(this.outputStreamField);
                if (this.currentEntryField.OutputUsedZip64.HasValue)
                {
                    this.anyEntriesUsedZip64Field |= this.currentEntryField.OutputUsedZip64.Value;
                }
                this.outputCounterField = null;
                this.encryptorField = (Stream) (this.deflaterField = null);
                this.entryOutputStreamField = null;
            }
        }

        private void _Init(Stream stream, bool leaveOpen, string name)
        {
            this.outputStreamField = stream.CanRead ? stream : new CountingStream(stream);
            this.CompressionLevel = CompressionLevel.Default;
            this.CompressionMethod = CompressionMethod.Deflate;
            this.encryptionField = EncryptionAlgorithm.None;
            this.entriesWrittenField = new Dictionary<string, ZipEntry>(StringComparer.Ordinal);
            this.zip64Field = Zip64Option.Default;
            this.leaveUnderlyingStreamOpenField = leaveOpen;
            this.Strategy = CompressionStrategy.Default;
            this.nameField = name ?? "(stream)";
            this.ParallelDeflateThreshold = -1L;
        }

        private void _InitiateCurrentEntry(bool finishing)
        {
            this.entriesWrittenField.Add(this.currentEntryField.FileName, this.currentEntryField);
            this.entryCountField++;
            if ((this.entryCountField > 0xfffe) && (this.zip64Field == Zip64Option.Default))
            {
                this.exceptionPendingField = true;
                throw new InvalidOperationException("Too many entries. Consider setting ZipOutputStream.EnableZip64.");
            }
            this.currentEntryField.WriteHeader(this.outputStreamField, finishing ? 0x63 : 0);
            this.currentEntryField.StoreRelativeOffset();
            if (!this.currentEntryField.IsDirectory)
            {
                this.currentEntryField.WriteSecurityMetadata(this.outputStreamField);
                this.currentEntryField.PrepOutputStream(this.outputStreamField, finishing ? ((long) 0) : ((long) (-1)), out this.outputCounterField, out this.encryptorField, out this.deflaterField, out this.entryOutputStreamField);
            }
            this.needToWriteEntryHeaderField = false;
        }

        /// <summary>
        /// Returns true if an entry by the given name has already been written
        /// to the ZipOutputStream.
        /// </summary>
        /// 
        /// <param name="name">
        /// The name of the entry to scan for.
        /// </param>
        /// 
        /// <returns>
        /// true if an entry by the given name has already been written.
        /// </returns>
        public bool ContainsEntry(string name)
        {
            return this.entriesWrittenField.ContainsKey(SharedUtilities.NormalizePathForUseInZipFile(name));
        }

        /// <summary>
        /// Dispose the stream
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// This method writes the Zip Central directory, then closes the stream.  The
        /// application must call Dispose() (or Close) in order to produce a valid zip file.
        /// </para>
        /// 
        /// <para>
        /// Typically the application will call <c>Dispose()</c> implicitly, via a <c>using</c>
        /// statement in C#, or a <c>Using</c> statement in VB.
        /// </para>
        /// 
        /// </remarks>
        /// 
        /// <param name="disposing">set this to true, always.</param>
        protected override void Dispose(bool disposing)
        {
            if (!this.disposedField)
            {
                if (disposing && !this.exceptionPendingField)
                {
                    this._FinishCurrentEntry();
                    this.directoryNeededZip64Field = ZipOutput.WriteCentralDirectoryStructure(this.outputStreamField, this.entriesWrittenField.Values, 1, this.zip64Field, this.Comment, new ZipContainer(this));
                    Stream wrappedStream = null;
                    CountingStream stream2 = this.outputStreamField as CountingStream;
                    if (stream2 != null)
                    {
                        wrappedStream = stream2.WrappedStream;
                        stream2.Dispose();
                    }
                    else
                    {
                        wrappedStream = this.outputStreamField;
                    }
                    if (!this.leaveUnderlyingStreamOpenField)
                    {
                        wrappedStream.Dispose();
                    }
                    this.outputStreamField = null;
                }
                this.disposedField = true;
            }
        }

        /// <summary>
        /// This is a no-op.
        /// </summary>
        public override void Flush()
        {
        }

        private void InsureUniqueEntry(ZipEntry ze1)
        {
            if (this.entriesWrittenField.ContainsKey(ze1.FileName))
            {
                this.exceptionPendingField = true;
                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, "The entry '{0}' already exists in the zip archive.", ze1.FileName));
            }
        }

        /// <summary>
        /// Specify the name of the next entry that will be written to the zip file.
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// Call this method just before calling <see cref="M:ZipOutputStream.Write(System.Byte[],System.Int32,System.Int32)" />, to
        /// specify the name of the entry that the next set of bytes written to
        /// the <c>ZipOutputStream</c> belongs to. All subsequent calls to <c>Write</c>,
        /// until the next call to <c>PutNextEntry</c>,
        /// will be inserted into the named entry in the zip file.
        /// </para>
        /// 
        /// <para>
        /// If the <paramref name="entryName" /> used in <c>PutNextEntry()</c> ends in
        /// a slash, then the entry added is marked as a directory. Because directory
        /// entries do not contain data, a call to <c>Write()</c>, before an
        /// intervening additional call to <c>PutNextEntry()</c>, will throw an
        /// exception.
        /// </para>
        /// 
        /// <para>
        /// If you don't call <c>Write()</c> between two calls to
        /// <c>PutNextEntry()</c>, the first entry is inserted into the zip file as a
        /// file of zero size.  This may be what you want.
        /// </para>
        /// 
        /// <para>
        /// Because <c>PutNextEntry()</c> closes out the prior entry, if any, this
        /// method may throw if there is a problem with the prior entry.
        /// </para>
        /// 
        /// <para>
        /// This method returns the <c>ZipEntry</c>.  You can modify public properties
        /// on the <c>ZipEntry</c>, such as <see cref="P:ZipEntry.Encryption" />, <see cref="P:ZipEntry.Password" />, and so on, until the first call to
        /// <c>ZipOutputStream.Write()</c>, or until the next call to
        /// <c>PutNextEntry()</c>.  If you modify the <c>ZipEntry</c> <em>after</em>
        /// having called <c>Write()</c>, you may get a runtime exception, or you may
        /// silently get an invalid zip archive.
        /// </para>
        /// 
        /// </remarks>
        /// 
        /// <example>
        /// 
        /// This example shows how to create a zip file, using the
        /// <c>ZipOutputStream</c> class.
        /// 
        /// <code>
        /// private void Zipup()
        /// {
        /// using (FileStream fs raw = File.Open(_outputFileName, FileMode.Create, FileAccess.ReadWrite ))
        /// {
        /// using (var output= new ZipOutputStream(fs))
        /// {
        /// output.Password = "VerySecret!";
        /// output.Encryption = EncryptionAlgorithm.WinZipAes256;
        /// output.PutNextEntry("entry1.txt");
        /// byte[] buffer= System.Text.Encoding.ASCII.GetBytes("This is the content for entry #1.");
        /// output.Write(buffer,0,buffer.Length);
        /// output.PutNextEntry("entry2.txt");  // this will be zero length
        /// output.PutNextEntry("entry3.txt");
        /// buffer= System.Text.Encoding.ASCII.GetBytes("This is the content for entry #3.");
        /// output.Write(buffer,0,buffer.Length);
        /// }
        /// }
        /// }
        /// </code>
        /// </example>
        /// 
        /// <param name="entryName">
        /// The name of the entry to be added, including any path to be used
        /// within the zip file.
        /// </param>
        /// 
        /// <returns>
        /// The ZipEntry created.
        /// </returns>
        public ZipEntry PutNextEntry(string entryName)
        {
            if (string.IsNullOrEmpty(entryName))
            {
                throw new ArgumentNullException("entryName");
            }
            if (this.disposedField)
            {
                this.exceptionPendingField = true;
                throw new InvalidOperationException("The stream has been closed.");
            }
            this._FinishCurrentEntry();
            this.currentEntryField = ZipEntry.CreateForZipOutputStream(entryName);
            this.currentEntryField.containerField = new ZipContainer(this);
            this.currentEntryField.bitFieldField = (short) (this.currentEntryField.bitFieldField | 8);
            this.currentEntryField.SetEntryTimes(DateTime.Now, DateTime.Now, DateTime.Now);
            this.currentEntryField.CompressionLevel = this.CompressionLevel;
            this.currentEntryField.CompressionMethod = this.CompressionMethod;
            this.currentEntryField.Password = this.passwordField;
            this.currentEntryField.Encryption = this.Encryption;
            this.currentEntryField.AlternateEncoding = this.AlternateEncoding;
            this.currentEntryField.AlternateEncodingUsage = this.AlternateEncodingUsage;
            if (entryName.EndsWith("/"))
            {
                this.currentEntryField.MarkAsDirectory();
            }
            this.currentEntryField.EmitTimesInWindowsFormatWhenSaving = (this.timestampField & ZipEntryTimestamp.Windows) != ZipEntryTimestamp.None;
            this.currentEntryField.EmitTimesInUnixFormatWhenSaving = (this.timestampField & ZipEntryTimestamp.Unix) != ZipEntryTimestamp.None;
            this.InsureUniqueEntry(this.currentEntryField);
            this.needToWriteEntryHeaderField = true;
            return this.currentEntryField;
        }

        /// <summary>
        /// This method always throws a NotSupportedException.
        /// </summary>
        /// <param name="buffer">ignored</param>
        /// <param name="offset">ignored</param>
        /// <param name="count">ignored</param>
        /// <returns>nothing</returns>
        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException("Read");
        }

        /// <summary>
        /// This method always throws a NotSupportedException.
        /// </summary>
        /// <param name="offset">ignored</param>
        /// <param name="origin">ignored</param>
        /// <returns>nothing</returns>
        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException("Seek");
        }

        /// <summary>
        /// This method always throws a NotSupportedException.
        /// </summary>
        /// <param name="value">ignored</param>
        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        /// <summary>Provides a string representation of the instance.</summary>
        /// <remarks>
        /// <para>
        /// This can be useful for debugging purposes.
        /// </para>
        /// </remarks>
        /// <returns>a string representation of the instance.</returns>
        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture, "ZipOutputStream::{0}(leaveOpen({1})))", this.nameField, this.leaveUnderlyingStreamOpenField);
        }

        /// <summary>
        /// Write the data from the buffer to the stream.
        /// </summary>
        /// 
        /// <remarks>
        /// As the application writes data into this stream, the data may be
        /// compressed and encrypted before being written out to the underlying
        /// stream, depending on the settings of the <see cref="P:ZipOutputStream.CompressionLevel" />
        /// and the <see cref="P:ZipOutputStream.Encryption" /> properties.
        /// </remarks>
        /// 
        /// <param name="buffer">The buffer holding data to write to the stream.</param>
        /// <param name="offset">the offset within that data array to find the first byte to write.</param>
        /// <param name="count">the number of bytes to write.</param>
        public override void Write(byte[] buffer, int offset, int count)
        {
            if (this.disposedField)
            {
                this.exceptionPendingField = true;
                throw new InvalidOperationException("The stream has been closed.");
            }
            if (buffer == null)
            {
                this.exceptionPendingField = true;
                throw new ArgumentNullException("buffer");
            }
            if (this.currentEntryField == null)
            {
                this.exceptionPendingField = true;
                throw new InvalidOperationException("You must call PutNextEntry() before calling Write().");
            }
            if (this.currentEntryField.IsDirectory)
            {
                this.exceptionPendingField = true;
                throw new InvalidOperationException("You cannot Write() data for an entry that is a directory.");
            }
            if (this.needToWriteEntryHeaderField)
            {
                this._InitiateCurrentEntry(false);
            }
            if (count != 0)
            {
                this.entryOutputStreamField.Write(buffer, offset, count);
            }
        }

        /// <summary>
        /// A Text Encoding to use when encoding the filenames and comments for
        /// all the ZipEntry items, during a ZipFile.Save() operation.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Whether the encoding specified here is used during the save depends
        /// on <see cref="P:ZipOutputStream.AlternateEncodingUsage" />.
        /// </para>
        /// </remarks>
        public Encoding AlternateEncoding
        {
            get
            {
                return this.alternateEncodingField;
            }
            set
            {
                this.alternateEncodingField = value;
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
                return this.alternateEncodingUsageField;
            }
            set
            {
                this.alternateEncodingUsageField = value;
            }
        }

        /// <summary>
        /// Always returns false.
        /// </summary>
        public override bool CanRead
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Always returns false.
        /// </summary>
        public override bool CanSeek
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Always returns true.
        /// </summary>
        public override bool CanWrite
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// Size of the work buffer to use for the ZLIB codec during compression.
        /// </summary>
        /// 
        /// <remarks>
        /// Setting this may affect performance.  For larger files, setting this to a
        /// larger size may improve performance, but I'm not sure.  Sorry, I don't
        /// currently have good recommendations on how to set it.  You can test it if
        /// you like.
        /// </remarks>
        public int CodecBufferSize { get; set; }

        /// <summary>
        /// A comment attached to the zip archive.
        /// </summary>
        /// 
        /// <remarks>
        /// 
        /// <para>
        /// The application sets this property to specify a comment to be embedded
        /// into the generated zip archive.
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
        /// filenames: (a) if <see cref="P:ZipOutputStream.AlternateEncodingUsage" /> is
        /// <c>Never</c>, it uses the default encoding (IBM437). (b) if <see cref="P:ZipOutputStream.AlternateEncodingUsage" /> is <c>Always</c>, it always uses the
        /// alternate encoding (<see cref="P:ZipOutputStream.AlternateEncoding" />). (c) if <see cref="P:ZipOutputStream.AlternateEncodingUsage" /> is <c>AsNecessary</c>, it uses the
        /// alternate encoding only if the default encoding is not sufficient for
        /// encoding the comment - in other words if decoding the result does not
        /// produce the original string.  This decision is taken at the time of
        /// the call to <c>ZipFile.Save()</c>.
        /// </para>
        /// 
        /// </remarks>
        public string Comment
        {
            get
            {
                return this.commentField;
            }
            set
            {
                if (this.disposedField)
                {
                    this.exceptionPendingField = true;
                    throw new InvalidOperationException("The stream has been closed.");
                }
                this.commentField = value;
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
        /// As with some other properties on the <c>ZipOutputStream</c> class, like <see cref="P:ZipOutputStream.Password" />, and <see cref="P:ZipOutputStream.Encryption" />,
        /// setting this property on a <c>ZipOutputStream</c>
        /// instance will cause the specified <c>CompressionLevel</c> to be used on all
        /// <see cref="T:ZipEntry" /> items that are subsequently added to the
        /// <c>ZipOutputStream</c> instance.
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
        public CompressionLevel CompressionLevel { get; set; }

        /// <summary>
        /// The compression method used on each entry added to the ZipOutputStream.
        /// </summary>
        public CompressionMethod CompressionMethod { get; set; }

        /// <summary>
        /// The default text encoding used in zip archives.  It is numeric 437, also
        /// known as IBM437.
        /// </summary>
        /// <seealso cref="P:ZipFile.ProvisionalAlternateEncoding" />
        public static Encoding DefaultEncoding
        {
            get
            {
                return Encoding.GetEncoding("IBM437");
            }
        }

        /// <summary>
        /// Specify whether to use ZIP64 extensions when saving a zip archive.
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// The default value for the property is <see cref="F:Zip64Option.Never" />. <see cref="F:Zip64Option.AsNecessary" /> is
        /// safest, in the sense that you will not get an Exception if a
        /// pre-ZIP64 limit is exceeded.
        /// </para>
        /// 
        /// <para>
        /// You must set this property before calling <c>Write()</c>.
        /// </para>
        /// 
        /// </remarks>
        public Zip64Option EnableZip64
        {
            get
            {
                return this.zip64Field;
            }
            set
            {
                if (this.disposedField)
                {
                    this.exceptionPendingField = true;
                    throw new InvalidOperationException("The stream has been closed.");
                }
                this.zip64Field = value;
            }
        }

        /// <summary>
        /// The Encryption to use for entries added to the <c>ZipOutputStream</c>.
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// The specified Encryption is applied to the entries subsequently
        /// written to the <c>ZipOutputStream</c> instance.
        /// </para>
        /// 
        /// <para>
        /// If you set this to something other than
        /// EncryptionAlgorithm.None, you will also need to set the
        /// <see cref="P:ZipOutputStream.Password" /> to a non-null, non-empty value in
        /// order to actually get encryption on the entry.
        /// </para>
        /// 
        /// </remarks>
        /// 
        /// <seealso cref="P:ZipOutputStream.Password">ZipOutputStream.Password</seealso>
        /// <seealso cref="P:ZipEntry.Encryption">ZipEntry.Encryption</seealso>
        public EncryptionAlgorithm Encryption
        {
            get
            {
                return this.encryptionField;
            }
            set
            {
                if (this.disposedField)
                {
                    this.exceptionPendingField = true;
                    throw new InvalidOperationException("The stream has been closed.");
                }
                if (value == EncryptionAlgorithm.Unsupported)
                {
                    this.exceptionPendingField = true;
                    throw new InvalidOperationException("You may not set Encryption to that value.");
                }
                this.encryptionField = value;
            }
        }

        /// <summary>
        /// Whether the ZipOutputStream should use case-insensitive comparisons when
        /// checking for uniqueness of zip entries.
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// Though the zip specification doesn't prohibit zipfiles with duplicate
        /// entries, Sane zip files have no duplicates, and the DotNetZip library
        /// cannot create zip files with duplicate entries. If an application attempts
        /// to call <see cref="M:ZipOutputStream.PutNextEntry(System.String)" /> with a name that duplicates one
        /// already used within the archive, the library will throw an Exception.
        /// </para>
        /// <para>
        /// This property allows the application to specify whether the
        /// ZipOutputStream instance considers ordinal case when checking for
        /// uniqueness of zip entries.
        /// </para>
        /// </remarks>
        public bool IgnoreCase
        {
            get
            {
                return !this.dontIgnoreCaseField;
            }
            set
            {
                this.dontIgnoreCaseField = !value;
            }
        }

        /// <summary>
        /// Always returns a NotSupportedException.
        /// </summary>
        public override long Length
        {
            get
            {
                throw new NotSupportedException();
            }
        }

        internal string Name
        {
            get
            {
                return this.nameField;
            }
        }

        internal Stream OutputStream
        {
            get
            {
                return this.outputStreamField;
            }
        }

        /// <summary>
        /// Indicates whether ZIP64 extensions were used when saving the zip archive.
        /// </summary>
        /// 
        /// <remarks>
        /// The value is defined only after the <c>ZipOutputStream</c> has been closed.
        /// </remarks>
        public bool OutputUsedZip64
        {
            get
            {
                return (this.anyEntriesUsedZip64Field || this.directoryNeededZip64Field);
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
        /// number of buffer pairs, S is the size of each buffer (<see cref="P:ZipOutputStream.CodecBufferSize" />).  By default, DotNetZip allocates 4 buffer
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
        /// The application can set this value at any time, but it is
        /// effective only if set before calling
        /// <c>ZipOutputStream.Write()</c> for the first time.
        /// </para>
        /// </remarks>
        /// 
        /// <seealso cref="P:ZipOutputStream.ParallelDeflateThreshold" />
        public int ParallelDeflateMaxBufferPairs
        {
            get
            {
                return this.maxBufferPairsField;
            }
            set
            {
                if (value < 4)
                {
                    throw new ArgumentOutOfRangeException("ParallelDeflateMaxBufferPairs", "Value must be 4 or greater.");
                }
                this.maxBufferPairsField = value;
            }
        }

        /// <summary>
        /// The size threshold for an entry, above which a parallel deflate is used.
        /// </summary>
        /// 
        /// <remarks>
        /// 
        /// <para>
        /// DotNetZip will use multiple threads to compress any ZipEntry, when
        /// the <c>CompressionMethod</c> is Deflate, and if the entry is
        /// larger than the given size.  Zero means "always use parallel
        /// deflate", while -1 means "never use parallel deflate".
        /// </para>
        /// 
        /// <para>
        /// If the entry size cannot be known before compression, as with any entry
        /// added via a ZipOutputStream, then Parallel deflate will never be
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
        /// and the deflation is slightly less effective.
        /// </para>
        /// 
        /// <para>
        /// Parallel deflate tends to not be as effective as single-threaded deflate
        /// because the original data stream is split into multiple independent
        /// buffers, each of which is compressed in parallel.  But because they are
        /// treated independently, there is no opportunity to share compression
        /// dictionaries, and additional framing bytes must be added to the output
        /// stream.  For that reason, a deflated stream may be slightly larger when
        /// compressed using parallel deflate, as compared to a traditional
        /// single-threaded deflate. For files of about 512k, the increase over the
        /// normal deflate is as much as 5% of the total compressed size. For larger
        /// files, the difference can be as small as 0.1%.
        /// </para>
        /// 
        /// <para>
        /// Multi-threaded compression does not give as much an advantage when using
        /// Encryption. This is primarily because encryption tends to slow down
        /// the entire pipeline. Also, multi-threaded compression gives less of an
        /// advantage when using lower compression levels, for example <see cref="F:CompressionLevel.BestSpeed" />.  You may have to perform
        /// some tests to determine the best approach for your situation.
        /// </para>
        /// 
        /// <para>
        /// The default value for this property is -1, which means parallel
        /// compression will not be performed unless you set it to zero.
        /// </para>
        /// 
        /// </remarks>
        public long ParallelDeflateThreshold
        {
            get
            {
                return this.parallelDeflateThresholdField;
            }
            set
            {
                if (((value != 0L) && (value != -1L)) && (value < 0x10000L))
                {
                    throw new ArgumentOutOfRangeException("value must be greater than 64k, or 0, or -1");
                }
                this.parallelDeflateThresholdField = value;
            }
        }

        /// <summary>
        /// Sets the password to be used on the <c>ZipOutputStream</c> instance.
        /// </summary>
        /// 
        /// <remarks>
        /// 
        /// <para>
        /// When writing a zip archive, this password is applied to the entries, not
        /// to the zip archive itself. It applies to any <c>ZipEntry</c> subsequently
        /// written to the <c>ZipOutputStream</c>.
        /// </para>
        /// 
        /// <para>
        /// Using a password does not encrypt or protect the "directory" of the
        /// archive - the list of entries contained in the archive.  If you set the
        /// <c>Password</c> property, the password actually applies to individual
        /// entries that are added to the archive, subsequent to the setting of this
        /// property.  The list of filenames in the archive that is eventually created
        /// will appear in clear text, but the contents of the individual files are
        /// encrypted.  This is how Zip encryption works.
        /// </para>
        /// 
        /// <para>
        /// If you set this property, and then add a set of entries to the archive via
        /// calls to <c>PutNextEntry</c>, then each entry is encrypted with that
        /// password.  You may also want to change the password between adding
        /// different entries. If you set the password, add an entry, then set the
        /// password to <c>null</c> (<c>Nothing</c> in VB), and add another entry, the
        /// first entry is encrypted and the second is not.
        /// </para>
        /// 
        /// <para>
        /// When setting the <c>Password</c>, you may also want to explicitly set the <see cref="P:ZipOutputStream.Encryption" /> property, to specify how to encrypt the entries added
        /// to the ZipFile.  If you set the <c>Password</c> to a non-null value and do not
        /// set <see cref="P:ZipOutputStream.Encryption" />, then PKZip 2.0 ("Weak") encryption is used.
        /// This encryption is relatively weak but is very interoperable. If
        /// you set the password to a <c>null</c> value (<c>Nothing</c> in VB),
        /// <c>Encryption</c> is reset to None.
        /// </para>
        /// 
        /// <para>
        /// Special case: if you wrap a ZipOutputStream around a non-seekable stream,
        /// and use encryption, and emit an entry of zero bytes, the <c>Close()</c> or
        /// <c>PutNextEntry()</c> following the entry will throw an exception.
        /// </para>
        /// 
        /// </remarks>
        public string Password
        {
            set
            {
                if (this.disposedField)
                {
                    this.exceptionPendingField = true;
                    throw new InvalidOperationException("The stream has been closed.");
                }
                this.passwordField = value;
                if (this.passwordField == null)
                {
                    this.encryptionField = EncryptionAlgorithm.None;
                }
                else if (this.encryptionField == EncryptionAlgorithm.None)
                {
                    this.encryptionField = EncryptionAlgorithm.PkzipWeak;
                }
            }
        }

        /// <summary>
        /// Setting this property always returns a NotSupportedException. Getting it
        /// returns the value of the Position on the underlying stream.
        /// </summary>
        public override long Position
        {
            get
            {
                return this.outputStreamField.Position;
            }
            set
            {
                throw new NotSupportedException();
            }
        }

        /// <summary>
        /// The compression strategy to use for all entries.
        /// </summary>
        /// 
        /// <remarks>
        /// Set the Strategy used by the ZLIB-compatible compressor, when compressing
        /// data for the entries in the zip archive. Different compression strategies
        /// work better on different sorts of data. The strategy parameter can affect
        /// the compression ratio and the speed of compression but not the correctness
        /// of the compresssion.  For more information see <see cref="T:CompressionStrategy" />.
        /// </remarks>
        public CompressionStrategy Strategy { get; set; }

        /// <summary>
        /// The type of timestamp attached to the ZipEntry.
        /// </summary>
        /// 
        /// <remarks>
        /// Set this in order to specify the kind of timestamp that should be emitted
        /// into the zip file for each entry.
        /// </remarks>
        public ZipEntryTimestamp Timestamp
        {
            get
            {
                return this.timestampField;
            }
            set
            {
                if (this.disposedField)
                {
                    this.exceptionPendingField = true;
                    throw new InvalidOperationException("The stream has been closed.");
                }
                this.timestampField = value;
            }
        }
    }
}

