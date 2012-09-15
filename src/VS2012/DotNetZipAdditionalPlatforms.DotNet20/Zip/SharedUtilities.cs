namespace DotNetZipAdditionalPlatforms.Zip
{
    using System;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Security.Permissions;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Threading;

    /// <summary>
    /// Collects general purpose utility methods.
    /// </summary>
    internal static class SharedUtilities
    {
        private static Regex doubleDotRegex1 = new Regex(@"^(.*/)?([^/\\.]+/\\.\\./)(.+)$");
        private static Encoding ibm437 = Encoding.GetEncoding("IBM437");
        private static Encoding utf8 = Encoding.GetEncoding("UTF-8");

        private static uint _HRForException(Exception ex1)
        {
            return (uint) Marshal.GetHRForException(ex1);
        }

        private static int _ReadFourBytes(Stream s, string message)
        {
            byte[] buffer = new byte[4];
            if (s.Read(buffer, 0, buffer.Length) != buffer.Length)
            {
                throw new BadReadException(string.Format(message, s.Position));
            }
            return ((((((buffer[3] * 0x100) + buffer[2]) * 0x100) + buffer[1]) * 0x100) + buffer[0]);
        }

        internal static DateTime AdjustTime_Reverse(DateTime time)
        {
            if (time.Kind == DateTimeKind.Utc)
            {
                return time;
            }
            DateTime time2 = time;
            if (!(!DateTime.Now.IsDaylightSavingTime() || time.IsDaylightSavingTime()))
            {
                time2 = time - new TimeSpan(1, 0, 0);
            }
            else if (!(DateTime.Now.IsDaylightSavingTime() || !time.IsDaylightSavingTime()))
            {
                time2 = time + new TimeSpan(1, 0, 0);
            }
            return time2;
        }

        /// <summary>
        /// Create a pseudo-random filename, suitable for use as a temporary
        /// file, and open it.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The System.IO.Path.GetRandomFileName() method is not available on
        /// the Compact Framework, so this library provides its own substitute
        /// on NETCF.
        /// </para>
        /// <para>
        /// This method produces a filename of the form
        /// DotNetZip-xxxxxxxx.tmp, where xxxxxxxx is replaced by randomly
        /// chosen characters, and creates that file.
        /// </para>
        /// </remarks>
        public static void CreateAndOpenUniqueTempFile(string dir, out Stream fs, out string filename)
        {
            for (int i = 0; i < 3; i++)
            {
                try
                {
                    filename = Path.Combine(dir, InternalGetTempFileName());
                    fs = new FileStream(filename, FileMode.CreateNew);
                    return;
                }
                catch (IOException)
                {
                    if (i == 2)
                    {
                        throw;
                    }
                }
            }
            throw new IOException();
        }

        internal static int DateTimeToPacked(DateTime time)
        {
            time = time.ToLocalTime();
            ushort num = (ushort) (((time.Day & 0x1f) | ((time.Month << 5) & 480)) | (((time.Year - 0x7bc) << 9) & 0xfe00));
            ushort num2 = (ushort) ((((time.Second / 2) & 0x1f) | ((time.Minute << 5) & 0x7e0)) | ((time.Hour << 11) & 0xf800));
            return ((num << 0x10) | num2);
        }

        /// <summary>
        /// Finds a signature in the zip stream. This is useful for finding
        /// the end of a zip entry, for example, or the beginning of the next ZipEntry.
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// Scans through 64k at a time.
        /// </para>
        /// 
        /// <para>
        /// If the method fails to find the requested signature, the stream Position
        /// after completion of this method is unchanged. If the method succeeds in
        /// finding the requested signature, the stream position after completion is
        /// direct AFTER the signature found in the stream.
        /// </para>
        /// </remarks>
        /// 
        /// <param name="stream">The stream to search</param>
        /// <param name="SignatureToFind">The 4-byte signature to find</param>
        /// <returns>The number of bytes read</returns>
        internal static long FindSignature(Stream stream, int SignatureToFind)
        {
            long position = stream.Position;
            int num2 = 0x10000;
            byte[] buffer = new byte[] { (byte) (SignatureToFind >> 0x18), (byte) ((SignatureToFind & 0xff0000) >> 0x10), (byte) ((SignatureToFind & 0xff00) >> 8), (byte) (SignatureToFind & 0xff) };
            byte[] buffer2 = new byte[num2];
            int num3 = 0;
            bool flag = false;
        Label_0050:
            num3 = stream.Read(buffer2, 0, buffer2.Length);
            if (num3 != 0)
            {
                for (int i = 0; i < num3; i++)
                {
                    if (buffer2[i] == buffer[3])
                    {
                        long offset = stream.Position;
                        stream.Seek((long) (i - num3), SeekOrigin.Current);
                        flag = ReadSignature(stream) == SignatureToFind;
                        if (flag)
                        {
                            break;
                        }
                        stream.Seek(offset, SeekOrigin.Begin);
                    }
                }
                if (!flag)
                {
                    goto Label_0050;
                }
            }
            if (!flag)
            {
                stream.Seek(position, SeekOrigin.Begin);
                return -1L;
            }
            return ((stream.Position - position) - 4L);
        }

        /// private null constructor
        public static long GetFileLength(string fileName)
        {
            if (!File.Exists(fileName))
            {
                throw new FileNotFoundException(fileName);
            }
            FileShare readWrite = FileShare.ReadWrite;
            readWrite |= FileShare.Delete;
            using (FileStream stream = File.Open(fileName, FileMode.Open, FileAccess.Read, readWrite))
            {
                return stream.Length;
            }
        }

        public static string InternalGetTempFileName()
        {
            return ("DotNetZip-" + Path.GetRandomFileName().Substring(0, 8) + ".tmp");
        }

        /// <summary>
        /// Utility routine for transforming path names from filesystem format (on Windows that means backslashes) to
        /// a format suitable for use within zipfiles. This means trimming the volume letter and colon (if any) And
        /// swapping backslashes for forward slashes.
        /// </summary>
        /// <param name="pathName">source path.</param>
        /// <returns>transformed path</returns>
        public static string NormalizePathForUseInZipFile(string pathName)
        {
            if (string.IsNullOrEmpty(pathName))
            {
                return pathName;
            }
            if ((pathName.Length >= 2) && ((pathName[1] == ':') && (pathName[2] == '\\')))
            {
                pathName = pathName.Substring(3);
            }
            pathName = pathName.Replace('\\', '/');
            while (pathName.StartsWith("/"))
            {
                pathName = pathName.Substring(1);
            }
            return SimplifyFwdSlashPath(pathName);
        }

        internal static DateTime PackedToDateTime(int packedDateTime)
        {
            if ((packedDateTime == 0xffff) || (packedDateTime == 0))
            {
                return new DateTime(0x7cb, 1, 1, 0, 0, 0, 0);
            }
            short num = (short) (packedDateTime & 0xffff);
            short num2 = (short) ((packedDateTime & 0xffff0000L) >> 0x10);
            int year = 0x7bc + ((num2 & 0xfe00) >> 9);
            int month = (num2 & 480) >> 5;
            int day = num2 & 0x1f;
            int hour = (num & 0xf800) >> 11;
            int minute = (num & 0x7e0) >> 5;
            int second = (num & 0x1f) * 2;
            if (second >= 60)
            {
                minute++;
                second = 0;
            }
            if (minute >= 60)
            {
                hour++;
                minute = 0;
            }
            if (hour >= 0x18)
            {
                day++;
                hour = 0;
            }
            DateTime now = DateTime.Now;
            bool flag = false;
            try
            {
                now = new DateTime(year, month, day, hour, minute, second, 0);
                flag = true;
            }
            catch (ArgumentOutOfRangeException)
            {
                if ((year == 0x7bc) && ((month == 0) || (day == 0)))
                {
                    try
                    {
                        now = new DateTime(0x7bc, 1, 1, hour, minute, second, 0);
                        flag = true;
                    }
                    catch (ArgumentOutOfRangeException)
                    {
                        try
                        {
                            now = new DateTime(0x7bc, 1, 1, 0, 0, 0, 0);
                            flag = true;
                        }
                        catch (ArgumentOutOfRangeException)
                        {
                        }
                    }
                }
                else
                {
                    try
                    {
                        while (year < 0x7bc)
                        {
                            year++;
                        }
                        while (year > 0x7ee)
                        {
                            year--;
                        }
                        while (month < 1)
                        {
                            month++;
                        }
                        while (month > 12)
                        {
                            month--;
                        }
                        while (day < 1)
                        {
                            day++;
                        }
                        while (day > 0x1c)
                        {
                            day--;
                        }
                        while (minute < 0)
                        {
                            minute++;
                        }
                        while (minute > 0x3b)
                        {
                            minute--;
                        }
                        while (second < 0)
                        {
                            second++;
                        }
                        while (second > 0x3b)
                        {
                            second--;
                        }
                        now = new DateTime(year, month, day, hour, minute, second, 0);
                        flag = true;
                    }
                    catch (ArgumentOutOfRangeException)
                    {
                    }
                }
            }
            if (!flag)
            {
                string str = string.Format(CultureInfo.InvariantCulture, "y({0}) m({1}) d({2}) h({3}) m({4}) s({5})", new object[] { year, month, day, hour, minute, second });
                throw new ZipException(string.Format(CultureInfo.InvariantCulture, "Bad date/time format in the zip file. ({0})", str));
            }
            return DateTime.SpecifyKind(now, DateTimeKind.Local);
        }

        internal static int ReadEntrySignature(Stream s)
        {
            int num = 0;
            try
            {
                num = _ReadFourBytes(s, "n/a");
                if (num != 0x8074b50)
                {
                    return num;
                }
                s.Seek(12L, SeekOrigin.Current);
                num = _ReadFourBytes(s, "n/a");
                if (num == 0x4034b50)
                {
                    return num;
                }
                s.Seek(8L, SeekOrigin.Current);
                num = _ReadFourBytes(s, "n/a");
                if (num != 0x4034b50)
                {
                    s.Seek(-24L, SeekOrigin.Current);
                    num = _ReadFourBytes(s, "n/a");
                }
            }
            catch (BadReadException)
            {
            }
            return num;
        }

        internal static int ReadInt(Stream s)
        {
            return _ReadFourBytes(s, "Could not read block - no data!  (position 0x{0:X8})");
        }

        internal static int ReadSignature(Stream s)
        {
            int num = 0;
            try
            {
                num = _ReadFourBytes(s, "n/a");
            }
            catch (BadReadException)
            {
            }
            return num;
        }

        /// <summary>
        /// Workitem 7889: handle ERROR_LOCK_VIOLATION during read
        /// </summary>
        /// <remarks>
        /// This could be gracefully handled with an extension attribute, but
        /// This assembly is built for .NET 2.0, so I cannot use them.
        /// </remarks>
        internal static int ReadWithRetry(Stream s, byte[] buffer, int offset, int count, string FileName)
        {
            int num = 0;
            bool flag = false;
            int num2 = 0;
            do
            {
                try
                {
                    num = s.Read(buffer, offset, count);
                    flag = true;
                }
                catch (IOException exception)
                {
                    SecurityPermission permission = new SecurityPermission(SecurityPermissionFlag.UnmanagedCode);
                    if (!permission.IsUnrestricted())
                    {
                        throw;
                    }
                    if (_HRForException(exception) != 0x80070021)
                    {
                        throw new IOException(string.Format(CultureInfo.InvariantCulture, "Cannot read file {0}", FileName), exception);
                    }
                    num2++;
                    if (num2 > 10)
                    {
                        throw new IOException(string.Format(CultureInfo.InvariantCulture, "Cannot read file {0}, at offset 0x{1:X8} after 10 retries", FileName, offset), exception);
                    }
                    Thread.Sleep((int) (250 + (num2 * 550)));
                }
            }
            while (!flag);
            return num;
        }

        private static string SimplifyFwdSlashPath(string path)
        {
            if (path.StartsWith("./"))
            {
                path = path.Substring(2);
            }
            path = path.Replace("/./", "/");
            path = doubleDotRegex1.Replace(path, "$1$3");
            return path;
        }

        internal static string StringFromBuffer(byte[] buf, Encoding encoding)
        {
            return encoding.GetString(buf, 0, buf.Length);
        }

        internal static byte[] StringToByteArray(string value)
        {
            return StringToByteArray(value, ibm437);
        }

        internal static byte[] StringToByteArray(string value, Encoding encoding)
        {
            return encoding.GetBytes(value);
        }

        internal static string Utf8StringFromBuffer(byte[] buf)
        {
            return StringFromBuffer(buf, utf8);
        }

        [Conditional("NETCF")]
        public static void Workaround_Ladybug318918(Stream s)
        {
            s.Flush();
        }
    }
}

