namespace DotNetZipAdditionalPlatforms.Zip
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Text;

    internal static class ZipOutput
    {
        private static int CountEntries(ICollection<ZipEntry> _entries)
        {
            int num = 0;
            foreach (ZipEntry entry in _entries)
            {
                if (entry.IncludedInMostRecentSave)
                {
                    num++;
                }
            }
            return num;
        }

        private static byte[] GenCentralDirectoryFooter(long StartOfCentralDirectory, long EndOfCentralDirectory, Zip64Option zip64, int entryCount, string comment, ZipContainer container)
        {
            Encoding encoding = GetEncoding(container, comment);
            int index = 0;
            int num2 = 0x16;
            byte[] bytes = null;
            short length = 0;
            if ((comment != null) && (comment.Length != 0))
            {
                bytes = encoding.GetBytes(comment);
                length = (short) bytes.Length;
            }
            num2 += length;
            byte[] destinationArray = new byte[num2];
            int destinationIndex = 0;
            Array.Copy(BitConverter.GetBytes((uint) 0x6054b50), 0, destinationArray, destinationIndex, 4);
            destinationIndex += 4;
            destinationArray[destinationIndex++] = 0;
            destinationArray[destinationIndex++] = 0;
            destinationArray[destinationIndex++] = 0;
            destinationArray[destinationIndex++] = 0;
            if ((entryCount >= 0xffff) || (zip64 == Zip64Option.Always))
            {
                for (index = 0; index < 4; index++)
                {
                    destinationArray[destinationIndex++] = 0xff;
                }
            }
            else
            {
                destinationArray[destinationIndex++] = (byte) (entryCount & 0xff);
                destinationArray[destinationIndex++] = (byte) ((entryCount & 0xff00) >> 8);
                destinationArray[destinationIndex++] = (byte) (entryCount & 0xff);
                destinationArray[destinationIndex++] = (byte) ((entryCount & 0xff00) >> 8);
            }
            long num5 = EndOfCentralDirectory - StartOfCentralDirectory;
            if ((num5 >= 0xffffffffL) || (StartOfCentralDirectory >= 0xffffffffL))
            {
                for (index = 0; index < 8; index++)
                {
                    destinationArray[destinationIndex++] = 0xff;
                }
            }
            else
            {
                destinationArray[destinationIndex++] = (byte) (num5 & 0xffL);
                destinationArray[destinationIndex++] = (byte) ((num5 & 0xff00L) >> 8);
                destinationArray[destinationIndex++] = (byte) ((num5 & 0xff0000L) >> 0x10);
                destinationArray[destinationIndex++] = (byte) ((num5 & 0xff000000L) >> 0x18);
                destinationArray[destinationIndex++] = (byte) (StartOfCentralDirectory & 0xffL);
                destinationArray[destinationIndex++] = (byte) ((StartOfCentralDirectory & 0xff00L) >> 8);
                destinationArray[destinationIndex++] = (byte) ((StartOfCentralDirectory & 0xff0000L) >> 0x10);
                destinationArray[destinationIndex++] = (byte) ((StartOfCentralDirectory & 0xff000000L) >> 0x18);
            }
            if ((comment == null) || (comment.Length == 0))
            {
                destinationArray[destinationIndex++] = 0;
                destinationArray[destinationIndex++] = 0;
                return destinationArray;
            }
            if (((length + destinationIndex) + 2) > destinationArray.Length)
            {
                length = (short) ((destinationArray.Length - destinationIndex) - 2);
            }
            destinationArray[destinationIndex++] = (byte) (length & 0xff);
            destinationArray[destinationIndex++] = (byte) ((length & 0xff00) >> 8);
            if (length != 0)
            {
                index = 0;
                while ((index < length) && ((destinationIndex + index) < destinationArray.Length))
                {
                    destinationArray[destinationIndex + index] = bytes[index];
                    index++;
                }
                destinationIndex += index;
            }
            return destinationArray;
        }

        private static byte[] GenZip64EndOfCentralDirectory(long StartOfCentralDirectory, long EndOfCentralDirectory, int entryCount, uint numSegments)
        {
            byte[] destinationArray = new byte[0x4c];
            int destinationIndex = 0;
            Array.Copy(BitConverter.GetBytes((uint) 0x6064b50), 0, destinationArray, destinationIndex, 4);
            destinationIndex += 4;
            long num2 = 0x2cL;
            Array.Copy(BitConverter.GetBytes(num2), 0, destinationArray, destinationIndex, 8);
            destinationIndex += 8;
            destinationArray[destinationIndex++] = 0x2d;
            destinationArray[destinationIndex++] = 0;
            destinationArray[destinationIndex++] = 0x2d;
            destinationArray[destinationIndex++] = 0;
            for (int i = 0; i < 8; i++)
            {
                destinationArray[destinationIndex++] = 0;
            }
            long num4 = entryCount;
            Array.Copy(BitConverter.GetBytes(num4), 0, destinationArray, destinationIndex, 8);
            destinationIndex += 8;
            Array.Copy(BitConverter.GetBytes(num4), 0, destinationArray, destinationIndex, 8);
            destinationIndex += 8;
            long num5 = EndOfCentralDirectory - StartOfCentralDirectory;
            Array.Copy(BitConverter.GetBytes(num5), 0, destinationArray, destinationIndex, 8);
            destinationIndex += 8;
            Array.Copy(BitConverter.GetBytes(StartOfCentralDirectory), 0, destinationArray, destinationIndex, 8);
            destinationIndex += 8;
            Array.Copy(BitConverter.GetBytes((uint) 0x7064b50), 0, destinationArray, destinationIndex, 4);
            destinationIndex += 4;
            uint num6 = (numSegments == 0) ? 0 : (numSegments - 1);
            Array.Copy(BitConverter.GetBytes(num6), 0, destinationArray, destinationIndex, 4);
            destinationIndex += 4;
            Array.Copy(BitConverter.GetBytes(EndOfCentralDirectory), 0, destinationArray, destinationIndex, 8);
            destinationIndex += 8;
            Array.Copy(BitConverter.GetBytes(numSegments), 0, destinationArray, destinationIndex, 4);
            destinationIndex += 4;
            return destinationArray;
        }

        private static Encoding GetEncoding(ZipContainer container, string t)
        {
            switch (container.AlternateEncodingUsage)
            {
                case ZipOption.Default:
                    return container.DefaultEncoding;

                case ZipOption.Always:
                    return container.AlternateEncoding;
            }
            Encoding defaultEncoding = container.DefaultEncoding;
            if (t == null)
            {
                return defaultEncoding;
            }
            byte[] bytes = defaultEncoding.GetBytes(t);
            if (defaultEncoding.GetString(bytes, 0, bytes.Length).Equals(t))
            {
                return defaultEncoding;
            }
            return container.AlternateEncoding;
        }

        public static bool WriteCentralDirectoryStructure(Stream s, ICollection<ZipEntry> entries, uint numSegments, Zip64Option zip64, string comment, ZipContainer container)
        {
            byte[] buffer;
            int num8;
            ZipSegmentedStream stream = s as ZipSegmentedStream;
            if (stream != null)
            {
                stream.ContiguousWrite = true;
            }
            long length = 0L;
            using (MemoryStream stream2 = new MemoryStream())
            {
                foreach (ZipEntry entry in entries)
                {
                    if (entry.IncludedInMostRecentSave)
                    {
                        entry.WriteCentralDirectoryEntry(stream2);
                    }
                }
                buffer = stream2.ToArray();
                s.Write(buffer, 0, buffer.Length);
                length = buffer.Length;
            }
            CountingStream stream3 = s as CountingStream;
            long endOfCentralDirectory = (stream3 != null) ? stream3.ComputedPosition : s.Position;
            long startOfCentralDirectory = endOfCentralDirectory - length;
            uint num4 = (stream != null) ? stream.CurrentSegment : 0;
            long num5 = endOfCentralDirectory - startOfCentralDirectory;
            int entryCount = CountEntries(entries);
            bool flag = (((zip64 == Zip64Option.Always) || (entryCount >= 0xffff)) || (num5 > 0xffffffffL)) || (startOfCentralDirectory > 0xffffffffL);
            byte[] destinationArray = null;
            if (flag)
            {
                if (zip64 == Zip64Option.Default)
                {
                    StackFrame frame = new StackFrame(1);
                    if (frame.GetMethod().DeclaringType == typeof(ZipFile))
                    {
                        throw new ZipException("The archive requires a ZIP64 Central Directory. Consider setting the ZipFile.UseZip64WhenSaving property.");
                    }
                    throw new ZipException("The archive requires a ZIP64 Central Directory. Consider setting the ZipOutputStream.EnableZip64 property.");
                }
                buffer = GenZip64EndOfCentralDirectory(startOfCentralDirectory, endOfCentralDirectory, entryCount, numSegments);
                destinationArray = GenCentralDirectoryFooter(startOfCentralDirectory, endOfCentralDirectory, zip64, entryCount, comment, container);
                if (num4 != 0)
                {
                    uint num7 = stream.ComputeSegment(buffer.Length + destinationArray.Length);
                    num8 = 0x10;
                    Array.Copy(BitConverter.GetBytes(num7), 0, buffer, num8, 4);
                    num8 += 4;
                    Array.Copy(BitConverter.GetBytes(num7), 0, buffer, num8, 4);
                    num8 = 60;
                    Array.Copy(BitConverter.GetBytes(num7), 0, buffer, num8, 4);
                    num8 += 4;
                    num8 += 8;
                    Array.Copy(BitConverter.GetBytes(num7), 0, buffer, num8, 4);
                }
                s.Write(buffer, 0, buffer.Length);
            }
            else
            {
                destinationArray = GenCentralDirectoryFooter(startOfCentralDirectory, endOfCentralDirectory, zip64, entryCount, comment, container);
            }
            if (num4 != 0)
            {
                ushort num9 = (ushort) stream.ComputeSegment(destinationArray.Length);
                num8 = 4;
                Array.Copy(BitConverter.GetBytes(num9), 0, destinationArray, num8, 2);
                num8 += 2;
                Array.Copy(BitConverter.GetBytes(num9), 0, destinationArray, num8, 2);
                num8 += 2;
            }
            s.Write(destinationArray, 0, destinationArray.Length);
            if (stream != null)
            {
                stream.ContiguousWrite = false;
            }
            return flag;
        }
    }
}

