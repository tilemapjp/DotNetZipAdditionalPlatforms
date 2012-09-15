namespace DotNetZipAdditionalPlatforms.Zip
{
    using System;
    using System.IO;
    using System.Runtime.CompilerServices;

    /// <summary>
    /// Delegate in which the application writes the <c>ZipEntry</c> content for the named entry.
    /// </summary>
    /// 
    /// <param name="entryName">The name of the entry that must be written.</param>
    /// <param name="stream">The stream to which the entry data should be written.</param>
    /// 
    /// <remarks>
    /// When you add an entry and specify a <c>WriteDelegate</c>, via <see cref="M:DotNetZipAdditionalPlatforms.Zip.ZipFile.AddEntry(System.String,DotNetZipAdditionalPlatforms.Zip.WriteDelegate)" />, the application
    /// code provides the logic that writes the entry data directly into the zip file.
    /// </remarks>
    /// 
    /// <example>
    /// 
    /// This example shows how to define a WriteDelegate that obtains a DataSet, and then
    /// writes the XML for the DataSet into the zip archive.  There's no need to
    /// save the XML to a disk file first.
    /// 
    /// <code lang="C#">
    /// private void WriteEntry (String filename, Stream output)
    /// {
    /// DataSet ds1 = ObtainDataSet();
    /// ds1.WriteXml(output);
    /// }
    /// 
    /// private void Run()
    /// {
    /// using (var zip = new ZipFile())
    /// {
    /// zip.AddEntry(zipEntryName, WriteEntry);
    /// zip.Save(zipFileName);
    /// }
    /// }
    /// </code>
    /// 
    /// <code lang="vb">
    /// Private Sub WriteEntry (ByVal filename As String, ByVal output As Stream)
    /// DataSet ds1 = ObtainDataSet()
    /// ds1.WriteXml(stream)
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
    /// <seealso cref="M:DotNetZipAdditionalPlatforms.Zip.ZipFile.AddEntry(System.String,DotNetZipAdditionalPlatforms.Zip.WriteDelegate)" />
    public delegate void WriteDelegate(string entryName, Stream stream);
}

