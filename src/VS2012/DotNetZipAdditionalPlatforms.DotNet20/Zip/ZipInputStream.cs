namespace DotNetZipAdditionalPlatforms.Zip
{
    using DotNetZipAdditionalPlatforms.Crc;
    using System;
    using System.Globalization;
    using System.IO;
    using System.Runtime.CompilerServices;
    using System.Text;

    /// <summary>
    /// Provides a stream metaphor for reading zip files.
    /// </summary>
    /// 
    /// <remarks>
    /// <para>
    /// This class provides an alternative programming model for reading zip files to
    /// the one enabled by the <see cref="T:ZipFile" /> class.  Use this when reading zip
    /// files, as an alternative to the <see cref="T:ZipFile" /> class, when you would
    /// like to use a Stream class to read the file.
    /// </para>
    /// 
    /// <para>
    /// Some application designs require a readable stream for input. This stream can
    /// be used to read a zip file, and extract entries.
    /// </para>
    /// 
    /// <para>
    /// Both the <c>ZipInputStream</c> class and the <c>ZipFile</c> class can be used
    /// to read and extract zip files.  Both of them support many of the common zip
    /// features, including Unicode, different compression levels, and ZIP64.  The
    /// programming models differ. For example, when extracting entries via calls to
    /// the <c>GetNextEntry()</c> and <c>Read()</c> methods on the
    /// <c>ZipInputStream</c> class, the caller is responsible for creating the file,
    /// writing the bytes into the file, setting the attributes on the file, and
    /// setting the created, last modified, and last accessed timestamps on the
    /// file. All of these things are done automatically by a call to <see cref="M:ZipEntry.Extract">ZipEntry.Extract()</see>.  For this reason, the
    /// <c>ZipInputStream</c> is generally recommended for when your application wants
    /// to extract the data, without storing that data into a file.
    /// </para>
    /// 
    /// <para>
    /// Aside from the obvious differences in programming model, there are some
    /// differences in capability between the <c>ZipFile</c> class and the
    /// <c>ZipInputStream</c> class.
    /// </para>
    /// 
    /// <list type="bullet">
    /// <item>
    /// <c>ZipFile</c> can be used to create or update zip files, or read and
    /// extract zip files. <c>ZipInputStream</c> can be used only to read and
    /// extract zip files. If you want to use a stream to create zip files, check
    /// out the <see cref="T:ZipOutputStream" />.
    /// </item>
    /// 
    /// <item>
    /// <c>ZipInputStream</c> cannot read segmented or spanned
    /// zip files.
    /// </item>
    /// 
    /// <item>
    /// <c>ZipInputStream</c> will not read Zip file comments.
    /// </item>
    /// 
    /// <item>
    /// When reading larger files, <c>ZipInputStream</c> will always underperform
    /// <c>ZipFile</c>. This is because the <c>ZipInputStream</c> does a full scan on the
    /// zip file, while the <c>ZipFile</c> class reads the central directory of the
    /// zip file.
    /// </item>
    /// 
    /// </list>
    /// 
    /// </remarks>
    public class ZipInputStream : Stream
    {
        private bool closedField;
        private ZipContainer containerField;
        private CrcCalculatorStream crcStreamField;
        private ZipEntry currentEntryField;
        private long endOfEntryField;
        private bool exceptionPendingField;
        private bool findRequiredField;
        private bool firstEntryField;
        private Stream inputStreamField;
        private bool leaveUnderlyingStreamOpenField;
        private long leftToReadField;
        private string nameField;
        private bool needSetupField;
        internal string passwordField;
        private Encoding provisionalAlternateEncodingField;

        /// <summary>
        /// Create a <c>ZipInputStream</c>, wrapping it around an existing stream.
        /// </summary>
        /// 
        /// <remarks>
        /// 
        /// <para>
        /// While the <see cref="T:ZipFile" /> class is generally easier
        /// to use, this class provides an alternative to those
        /// applications that want to read from a zipfile directly,
        /// using a <see cref="T:System.IO.Stream" />.
        /// </para>
        /// 
        /// <para>
        /// Both the <c>ZipInputStream</c> class and the <c>ZipFile</c> class can be used
        /// to read and extract zip files.  Both of them support many of the common zip
        /// features, including Unicode, different compression levels, and ZIP64.  The
        /// programming models differ. For example, when extracting entries via calls to
        /// the <c>GetNextEntry()</c> and <c>Read()</c> methods on the
        /// <c>ZipInputStream</c> class, the caller is responsible for creating the file,
        /// writing the bytes into the file, setting the attributes on the file, and
        /// setting the created, last modified, and last accessed timestamps on the
        /// file. All of these things are done automatically by a call to <see cref="M:ZipEntry.Extract">ZipEntry.Extract()</see>.  For this reason, the
        /// <c>ZipInputStream</c> is generally recommended for when your application wants
        /// to extract the data, without storing that data into a file.
        /// </para>
        /// 
        /// <para>
        /// Aside from the obvious differences in programming model, there are some
        /// differences in capability between the <c>ZipFile</c> class and the
        /// <c>ZipInputStream</c> class.
        /// </para>
        /// 
        /// <list type="bullet">
        /// <item>
        /// <c>ZipFile</c> can be used to create or update zip files, or read and extract
        /// zip files. <c>ZipInputStream</c> can be used only to read and extract zip
        /// files. If you want to use a stream to create zip files, check out the <see cref="T:ZipOutputStream" />.
        /// </item>
        /// 
        /// <item>
        /// <c>ZipInputStream</c> cannot read segmented or spanned
        /// zip files.
        /// </item>
        /// 
        /// <item>
        /// <c>ZipInputStream</c> will not read Zip file comments.
        /// </item>
        /// 
        /// <item>
        /// When reading larger files, <c>ZipInputStream</c> will always underperform
        /// <c>ZipFile</c>. This is because the <c>ZipInputStream</c> does a full scan on the
        /// zip file, while the <c>ZipFile</c> class reads the central directory of the
        /// zip file.
        /// </item>
        /// 
        /// </list>
        /// 
        /// </remarks>
        /// 
        /// <param name="stream">
        /// The stream to read. It must be readable. This stream will be closed at
        /// the time the <c>ZipInputStream</c> is closed.
        /// </param>
        /// 
        /// <example>
        /// 
        /// This example shows how to read a zip file, and extract entries, using the
        /// <c>ZipInputStream</c> class.
        /// 
        /// <code lang="C#">
        /// private void Unzip()
        /// {
        /// byte[] buffer= new byte[2048];
        /// int n;
        /// using (var raw = File.Open(inputFileName, FileMode.Open, FileAccess.Read))
        /// {
        /// using (var input= new ZipInputStream(raw))
        /// {
        /// ZipEntry e;
        /// while (( e = input.GetNextEntry()) != null)
        /// {
        /// if (e.IsDirectory) continue;
        /// string outputPath = Path.Combine(extractDir, e.FileName);
        /// using (var output = File.Open(outputPath, FileMode.Create, FileAccess.ReadWrite))
        /// {
        /// while ((n= input.Read(buffer, 0, buffer.Length)) &gt; 0)
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
        /// Private Sub UnZip()
        /// Dim inputFileName As String = "MyArchive.zip"
        /// Dim extractDir As String = "extract"
        /// Dim buffer As Byte() = New Byte(2048) {}
        /// Using raw As FileStream = File.Open(inputFileName, FileMode.Open, FileAccess.Read)
        /// Using input As ZipInputStream = New ZipInputStream(raw)
        /// Dim e As ZipEntry
        /// Do While (Not e = input.GetNextEntry Is Nothing)
        /// If Not e.IsDirectory Then
        /// Using output As FileStream = File.Open(Path.Combine(extractDir, e.FileName), _
        /// FileMode.Create, FileAccess.ReadWrite)
        /// Dim n As Integer
        /// Do While (n = input.Read(buffer, 0, buffer.Length) &gt; 0)
        /// output.Write(buffer, 0, n)
        /// Loop
        /// End Using
        /// End If
        /// Loop
        /// End Using
        /// End Using
        /// End Sub
        /// </code>
        /// </example>
        public ZipInputStream(Stream stream) : this(stream, false)
        {
        }

        /// <summary>
        /// Create a <c>ZipInputStream</c>, given the name of an existing zip file.
        /// </summary>
        /// 
        /// <remarks>
        /// 
        /// <para>
        /// This constructor opens a <c>FileStream</c> for the given zipfile, and
        /// wraps a <c>ZipInputStream</c> around that.  See the documentation for the
        /// <see cref="M:ZipInputStream.#ctor(System.IO.Stream)" /> constructor for full details.
        /// </para>
        /// 
        /// <para>
        /// While the <see cref="T:ZipFile" /> class is generally easier
        /// to use, this class provides an alternative to those
        /// applications that want to read from a zipfile directly,
        /// using a <see cref="T:System.IO.Stream" />.
        /// </para>
        /// 
        /// </remarks>
        /// 
        /// <param name="fileName">
        /// The name of the filesystem file to read.
        /// </param>
        /// 
        /// <example>
        /// 
        /// This example shows how to read a zip file, and extract entries, using the
        /// <c>ZipInputStream</c> class.
        /// 
        /// <code lang="C#">
        /// private void Unzip()
        /// {
        /// byte[] buffer= new byte[2048];
        /// int n;
        /// using (var input= new ZipInputStream(inputFileName))
        /// {
        /// ZipEntry e;
        /// while (( e = input.GetNextEntry()) != null)
        /// {
        /// if (e.IsDirectory) continue;
        /// string outputPath = Path.Combine(extractDir, e.FileName);
        /// using (var output = File.Open(outputPath, FileMode.Create, FileAccess.ReadWrite))
        /// {
        /// while ((n= input.Read(buffer, 0, buffer.Length)) &gt; 0)
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
        /// Private Sub UnZip()
        /// Dim inputFileName As String = "MyArchive.zip"
        /// Dim extractDir As String = "extract"
        /// Dim buffer As Byte() = New Byte(2048) {}
        /// Using input As ZipInputStream = New ZipInputStream(inputFileName)
        /// Dim e As ZipEntry
        /// Do While (Not e = input.GetNextEntry Is Nothing)
        /// If Not e.IsDirectory Then
        /// Using output As FileStream = File.Open(Path.Combine(extractDir, e.FileName), _
        /// FileMode.Create, FileAccess.ReadWrite)
        /// Dim n As Integer
        /// Do While (n = input.Read(buffer, 0, buffer.Length) &gt; 0)
        /// output.Write(buffer, 0, n)
        /// Loop
        /// End Using
        /// End If
        /// Loop
        /// End Using
        /// End Sub
        /// </code>
        /// </example>
        public ZipInputStream(string fileName)
        {
            Stream stream = File.Open(fileName, FileMode.Open, FileAccess.Read, FileShare.Read);
            this._Init(stream, false, fileName);
        }

        /// <summary>
        /// Create a <c>ZipInputStream</c>, explicitly specifying whether to
        /// keep the underlying stream open.
        /// </summary>
        /// 
        /// <remarks>
        /// See the documentation for the <see cref="M:ZipInputStream.#ctor(System.IO.Stream)">ZipInputStream(Stream)</see>
        /// constructor for a discussion of the class, and an example of how to use the class.
        /// </remarks>
        /// 
        /// <param name="stream">
        /// The stream to read from. It must be readable.
        /// </param>
        /// 
        /// <param name="leaveOpen">
        /// true if the application would like the stream
        /// to remain open after the <c>ZipInputStream</c> has been closed.
        /// </param>
        public ZipInputStream(Stream stream, bool leaveOpen)
        {
            this._Init(stream, leaveOpen, null);
        }

        private void _Init(Stream stream, bool leaveOpen, string name)
        {
            this.inputStreamField = stream;
            if (!this.inputStreamField.CanRead)
            {
                throw new ZipException("The stream must be readable.");
            }
            this.containerField = new ZipContainer(this);
            this.provisionalAlternateEncodingField = Encoding.GetEncoding("IBM437");
            this.leaveUnderlyingStreamOpenField = leaveOpen;
            this.findRequiredField = true;
            this.nameField = name ?? "(stream)";
        }

        /// <summary>
        /// Dispose the stream.
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// This method disposes the ZipInputStream.  It may also close the
        /// underlying stream, depending on which constructor was used.
        /// </para>
        /// 
        /// <para>
        /// Typically the application will call <c>Dispose()</c> implicitly, via
        /// a <c>using</c> statement in C#, or a <c>Using</c> statement in VB.
        /// </para>
        /// 
        /// <para>
        /// Application code won't call this code directly.  This method may
        /// be invoked in two distinct scenarios.  If disposing == true, the
        /// method has been called directly or indirectly by a user's code,
        /// for example via the public Dispose() method. In this case, both
        /// managed and unmanaged resources can be referenced and disposed.
        /// If disposing == false, the method has been called by the runtime
        /// from inside the object finalizer and this method should not
        /// reference other objects; in that case only unmanaged resources
        /// must be referenced or disposed.
        /// </para>
        /// </remarks>
        /// 
        /// <param name="disposing">
        /// true if the Dispose method was invoked by user code.
        /// </param>
        protected override void Dispose(bool disposing)
        {
            if (!this.closedField)
            {
                if (disposing)
                {
                    if (this.exceptionPendingField)
                    {
                        return;
                    }
                    if (!this.leaveUnderlyingStreamOpenField)
                    {
                        this.inputStreamField.Dispose();
                    }
                }
                this.closedField = true;
            }
        }

        /// <summary>
        /// This is a no-op.
        /// </summary>
        public override void Flush()
        {
            throw new NotSupportedException("Flush");
        }

        /// <summary>
        /// Read the next entry from the zip file.
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// Call this method just before calling <see cref="M:ZipInputStream.Read(System.Byte[],System.Int32,System.Int32)" />,
        /// to position the pointer in the zip file to the next entry that can be
        /// read.  Subsequent calls to <c>Read()</c>, will decrypt and decompress the
        /// data in the zip file, until <c>Read()</c> returns 0.
        /// </para>
        /// 
        /// <para>
        /// Each time you call <c>GetNextEntry()</c>, the pointer in the wrapped
        /// stream is moved to the next entry in the zip file.  If you call <see cref="M:ZipInputStream.Seek(System.Int64,System.IO.SeekOrigin)" />, and thus re-position the pointer within
        /// the file, you will need to call <c>GetNextEntry()</c> again, to insure
        /// that the file pointer is positioned at the beginning of a zip entry.
        /// </para>
        /// 
        /// <para>
        /// This method returns the <c>ZipEntry</c>. Using a stream approach, you will
        /// read the raw bytes for an entry in a zip file via calls to <c>Read()</c>.
        /// Alternatively, you can extract an entry into a file, or a stream, by
        /// calling <see cref="M:ZipEntry.Extract" />, or one of its siblings.
        /// </para>
        /// 
        /// </remarks>
        /// 
        /// <returns>
        /// The <c>ZipEntry</c> read. Returns null (or Nothing in VB) if there are no more
        /// entries in the zip file.
        /// </returns>
        public ZipEntry GetNextEntry()
        {
            if (this.findRequiredField)
            {
                if (SharedUtilities.FindSignature(this.inputStreamField, 0x4034b50) == -1L)
                {
                    return null;
                }
                this.inputStreamField.Seek(-4L, SeekOrigin.Current);
            }
            else if (this.firstEntryField)
            {
                this.inputStreamField.Seek(this.endOfEntryField, SeekOrigin.Begin);
            }
            this.currentEntryField = ZipEntry.ReadEntry(this.containerField, !this.firstEntryField);
            this.endOfEntryField = this.inputStreamField.Position;
            this.firstEntryField = true;
            this.needSetupField = true;
            this.findRequiredField = false;
            return this.currentEntryField;
        }

        /// <summary>
        /// Read the data from the stream into the buffer.
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// The data for the zipentry will be decrypted and uncompressed, as
        /// necessary, before being copied into the buffer.
        /// </para>
        /// 
        /// <para>
        /// You must set the <see cref="P:ZipInputStream.Password" /> property before calling
        /// <c>Read()</c> the first time for an encrypted entry.  To determine if an
        /// entry is encrypted and requires a password, check the <see cref="P:ZipEntry.Encryption">ZipEntry.Encryption</see> property.
        /// </para>
        /// </remarks>
        /// 
        /// <param name="buffer">The buffer to hold the data read from the stream.</param>
        /// <param name="offset">the offset within the buffer to copy the first byte read.</param>
        /// <param name="count">the number of bytes to read.</param>
        /// <returns>the number of bytes read, after decryption and decompression.</returns>
        public override int Read(byte[] buffer, int offset, int count)
        {
            if (this.closedField)
            {
                this.exceptionPendingField = true;
                throw new InvalidOperationException("The stream has been closed.");
            }
            if (this.needSetupField)
            {
                this.SetupStream();
            }
            if (this.leftToReadField == 0L)
            {
                return 0;
            }
            int num = (this.leftToReadField > count) ? count : ((int) this.leftToReadField);
            int num2 = this.crcStreamField.Read(buffer, offset, num);
            this.leftToReadField -= num2;
            if (this.leftToReadField == 0L)
            {
                int crc = this.crcStreamField.Crc;
                this.currentEntryField.VerifyCrcAfterExtract(crc);
                this.inputStreamField.Seek(this.endOfEntryField, SeekOrigin.Begin);
            }
            return num2;
        }

        /// <summary>
        /// This method seeks in the underlying stream.
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// Call this method if you want to seek around within the zip file for random access.
        /// </para>
        /// 
        /// <para>
        /// Applications can intermix calls to <c>Seek()</c> with calls to <see cref="M:ZipInputStream.GetNextEntry" />.  After a call to <c>Seek()</c>,
        /// <c>GetNextEntry()</c> will get the next <c>ZipEntry</c> that falls after
        /// the current position in the input stream. You're on your own for finding
        /// out just where to seek in the stream, to get to the various entries.
        /// </para>
        /// 
        /// </remarks>
        /// 
        /// <param name="offset">the offset point to seek to</param>
        /// <param name="origin">the reference point from which to seek</param>
        /// <returns>The new position</returns>
        public override long Seek(long offset, SeekOrigin origin)
        {
            this.findRequiredField = true;
            return this.inputStreamField.Seek(offset, origin);
        }

        /// <summary>
        /// This method always throws a NotSupportedException.
        /// </summary>
        /// <param name="value">ignored</param>
        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        private void SetupStream()
        {
            this.crcStreamField = this.currentEntryField.InternalOpenReader(this.passwordField);
            this.leftToReadField = this.crcStreamField.Length;
            this.needSetupField = false;
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
            return string.Format(CultureInfo.InvariantCulture, "ZipInputStream::{0}(leaveOpen({1})))", this.nameField, this.leaveUnderlyingStreamOpenField);
        }

        /// <summary>
        /// This method always throws a NotSupportedException.
        /// </summary>
        /// <param name="buffer">ignored</param>
        /// <param name="offset">ignored</param>
        /// <param name="count">ignored</param>
        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException("Write");
        }

        /// <summary>
        /// Always returns true.
        /// </summary>
        public override bool CanRead
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// Returns the value of <c>CanSeek</c> for the underlying (wrapped) stream.
        /// </summary>
        public override bool CanSeek
        {
            get
            {
                return this.inputStreamField.CanSeek;
            }
        }

        /// <summary>
        /// Always returns false.
        /// </summary>
        public override bool CanWrite
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Size of the work buffer to use for the ZLIB codec during decompression.
        /// </summary>
        /// 
        /// <remarks>
        /// Setting this affects the performance and memory efficiency of compression
        /// and decompression.  For larger files, setting this to a larger size may
        /// improve performance, but the exact numbers vary depending on available
        /// memory, and a bunch of other variables. I don't have good firm
        /// recommendations on how to set it.  You'll have to test it yourself. Or
        /// just leave it alone and accept the default.
        /// </remarks>
        public int CodecBufferSize { get; set; }

        /// <summary>
        /// Returns the length of the underlying stream.
        /// </summary>
        public override long Length
        {
            get
            {
                return this.inputStreamField.Length;
            }
        }

        /// <summary>
        /// Sets the password to be used on the <c>ZipInputStream</c> instance.
        /// </summary>
        /// 
        /// <remarks>
        /// 
        /// <para>
        /// When reading a zip archive, this password is used to read and decrypt the
        /// entries that are encrypted within the zip file. When entries within a zip
        /// file use different passwords, set the appropriate password for the entry
        /// before the first call to <c>Read()</c> for each entry.
        /// </para>
        /// 
        /// <para>
        /// When reading an entry that is not encrypted, the value of this property is
        /// ignored.
        /// </para>
        /// 
        /// </remarks>
        /// 
        /// <example>
        /// 
        /// This example uses the ZipInputStream to read and extract entries from a
        /// zip file, using a potentially different password for each entry.
        /// 
        /// <code lang="C#">
        /// byte[] buffer= new byte[2048];
        /// int n;
        /// using (var raw = File.Open(_inputFileName, FileMode.Open, FileAccess.Read ))
        /// {
        /// using (var input= new ZipInputStream(raw))
        /// {
        /// ZipEntry e;
        /// while (( e = input.GetNextEntry()) != null)
        /// {
        /// input.Password = PasswordForEntry(e.FileName);
        /// if (e.IsDirectory) continue;
        /// string outputPath = Path.Combine(_extractDir, e.FileName);
        /// using (var output = File.Open(outputPath, FileMode.Create, FileAccess.ReadWrite))
        /// {
        /// while ((n= input.Read(buffer,0,buffer.Length)) &gt; 0)
        /// {
        /// output.Write(buffer,0,n);
        /// }
        /// }
        /// }
        /// }
        /// }
        /// 
        /// </code>
        /// </example>
        public string Password
        {
            set
            {
                if (this.closedField)
                {
                    this.exceptionPendingField = true;
                    throw new InvalidOperationException("The stream has been closed.");
                }
                this.passwordField = value;
            }
        }

        /// <summary>
        /// Gets or sets the position of the underlying stream.
        /// </summary>
        /// <remarks>
        /// Setting the position is equivalent to calling <c>Seek(value, SeekOrigin.Begin)</c>.
        /// </remarks>
        public override long Position
        {
            get
            {
                return this.inputStreamField.Position;
            }
            set
            {
                this.Seek(value, SeekOrigin.Begin);
            }
        }

        /// <summary>
        /// The text encoding to use when reading entries into the zip archive, for
        /// those entries whose filenames or comments cannot be encoded with the
        /// default (IBM437) encoding.
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// In <see href="http://www.pkware.com/documents/casestudies/APPNOTE.TXT">its
        /// zip specification</see>, PKWare describes two options for encoding
        /// filenames and comments: using IBM437 or UTF-8.  But, some archiving tools
        /// or libraries do not follow the specification, and instead encode
        /// characters using the system default code page.  For example, WinRAR when
        /// run on a machine in Shanghai may encode filenames with the Big-5 Chinese
        /// (950) code page.  This behavior is contrary to the Zip specification, but
        /// it occurs anyway.
        /// </para>
        /// 
        /// <para>
        /// When using DotNetZip to read zip archives that use something other than
        /// UTF-8 or IBM437, set this property to specify the code page to use when
        /// reading encoded filenames and comments for each <c>ZipEntry</c> in the zip
        /// file.
        /// </para>
        /// 
        /// <para>
        /// This property is "provisional". When the entry in the zip archive is not
        /// explicitly marked as using UTF-8, then IBM437 is used to decode filenames
        /// and comments. If a loss of data would result from using IBM436 -
        /// specifically when encoding and decoding is not reflexive - the codepage
        /// specified here is used. It is possible, therefore, to have a given entry
        /// with a <c>Comment</c> encoded in IBM437 and a <c>FileName</c> encoded with
        /// the specified "provisional" codepage.
        /// </para>
        /// 
        /// <para>
        /// When a zip file uses an arbitrary, non-UTF8 code page for encoding, there
        /// is no standard way for the reader application - whether DotNetZip, WinZip,
        /// WinRar, or something else - to know which codepage has been used for the
        /// entries. Readers of zip files are not able to inspect the zip file and
        /// determine the codepage that was used for the entries contained within it.
        /// It is left to the application or user to determine the necessary codepage
        /// when reading zip files encoded this way.  If you use an incorrect codepage
        /// when reading a zipfile, you will get entries with filenames that are
        /// incorrect, and the incorrect filenames may even contain characters that
        /// are not legal for use within filenames in Windows. Extracting entries with
        /// illegal characters in the filenames will lead to exceptions. It's too bad,
        /// but this is just the way things are with code pages in zip files. Caveat
        /// Emptor.
        /// </para>
        /// 
        /// </remarks>
        public Encoding ProvisionalAlternateEncoding
        {
            get
            {
                return this.provisionalAlternateEncodingField;
            }
            set
            {
                this.provisionalAlternateEncodingField = value;
            }
        }

        internal Stream ReadStream
        {
            get
            {
                return this.inputStreamField;
            }
        }
    }
}

