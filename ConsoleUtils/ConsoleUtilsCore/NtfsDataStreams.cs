// This file is part of Managed NTFS Data Streams project
//
// Copyright 2020 Emzi0767
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//   http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace NtfsDataStreams
{
    /// <summary>
    /// Contains information about an existing NTFS Data Stream, as well as common IO operations.
    /// </summary>
    public sealed class FileDataStream
    {
        /// <summary>
        /// Gets the file this data stream is associated with.
        /// </summary>
        public FileInfo File { get; }

        /// <summary>
        /// Gets the name of the data stream.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the length of this stream.
        /// </summary>
        public long Length { get; }

        /// <summary>
        /// Gets the type of this stream.
        /// </summary>
        public FileDataStreamType Type { get; }

        internal FileDataStream(FileInfo file, string name, long length, FileDataStreamType type)
        {
            if (file == null)
                throw new ArgumentNullException(nameof(file));

            if (name == null)
                throw new ArgumentNullException(nameof(name));

            this.File = file;
            this.Name = name;
            this.Length = length;
            this.Type = type;
        }

        /// <summary>
        /// Opens the stream with specified mode.
        /// </summary>
        /// <param name="mode">Mode to open this stream with.</param>
        /// <returns><see cref="FileStream"/> instance to use for IO operations.</returns>
        public FileStream Open(FileMode mode)
            => this.Open(mode, mode == FileMode.Append ? FileAccess.Write : FileAccess.ReadWrite, FileShare.None);

        /// <summary>
        /// Opens the stream with specified mode and access.
        /// </summary>
        /// <param name="mode">Mode to open this stream with.</param>
        /// <param name="access">Access mode for the opened stream.</param>
        /// <returns><see cref="FileStream"/> instance to use for IO operations.</returns>
        public FileStream Open(FileMode mode, FileAccess access)
            => this.Open(mode, access, FileShare.None);

        /// <summary>
        /// Opens the stream with specified mode, access, and sharing mode.
        /// </summary>
        /// <param name="mode">Mode to open this stream with.</param>
        /// <param name="access">Access mode for the opened stream.</param>
        /// <param name="share">Sharing mode for the stream.</param>
        /// <returns><see cref="FileStream"/> instance to use for IO operations.</returns>
        public FileStream Open(FileMode mode, FileAccess access, FileShare share)
            => InteropWrapper.Open(this, mode, access, share);

        /// <summary>
        /// Opens the specified stream for reading.
        /// </summary>
        /// <returns><see cref="FileStream"/> instance to use for IO operations.</returns>
        public FileStream OpenRead()
            => this.Open(FileMode.Open, FileAccess.Read, FileShare.Read);

        /// <summary>
        /// Opens the specified stream for writing. Existing contents are preserved, however the stream position is set to beginning.
        /// </summary>
        /// <returns><see cref="FileStream"/> instance to use for IO operations.</returns>
        public FileStream OpenWrite()
            => this.Open(FileMode.OpenOrCreate, FileAccess.Write, FileShare.None);

        /// <summary>
        /// Opens the specified stream for writing. The stream position will be set to the end.
        /// </summary>
        /// <returns><see cref="FileStream"/> instance to use for IO operations.</returns>
        public FileStream OpenAppend()
            => this.Open(FileMode.Append, FileAccess.Write, FileShare.None).SeekToEnd();

        /// <summary>
        /// Opens the specified stream for writing. If the stream exists, it will be overwritten.
        /// </summary>
        /// <returns><see cref="FileStream"/> instance to use for IO operations.</returns>
        public FileStream Create()
            => this.Open(FileMode.Create, FileAccess.ReadWrite, FileShare.None);

        /// <summary>
        /// Opens the specified stream for writing, and truncates it.
        /// </summary>
        /// <returns><see cref="FileStream"/> instance to use for IO operations.</returns>
        public FileStream Truncate()
            => this.Open(FileMode.Truncate, FileAccess.Write, FileShare.None);

        /// <summary>
        /// Opens the specified stream for reading in text mode.
        /// </summary>
        /// <param name="encoding">Text encoding to use for the text.</param>
        /// <returns><see cref="StreamReader"/> instance for text reading operations.</returns>
        public StreamReader OpenText(Encoding encoding)
            => new StreamReader(this.OpenRead(), encoding);

        /// <summary>
        /// Opens the specified stream for writing in text mode. This will overwrite existsing contents.
        /// </summary>
        /// <param name="encoding">Text encoding to use for the text.</param>
        /// <returns><see cref="StreamWriter"/> instance for text writing operations.</returns>
        public StreamWriter CreateText(Encoding encoding)
            => new StreamWriter(this.Create(), encoding);

        /// <summary>
        /// Opens the specified stream for writing in text mode. The stream position will be set to the end of stream.
        /// </summary>
        /// <param name="encoding">Text encoding to use for the text.</param>
        /// <returns><see cref="StreamWriter"/> instance for text writing operations.</returns>
        public StreamWriter AppendText(Encoding encoding)
            => new StreamWriter(this.OpenAppend(), encoding);

        /// <summary>
        /// Opens the specified stream for writing in text mode. Stream contents will be preserved, but the stream position will be set to the beginning of the stream.
        /// </summary>
        /// <param name="encoding">Text encoding to use for the text.</param>
        /// <returns><see cref="StreamWriter"/> instance for text writing operations.</returns>
        public StreamWriter WriteText(Encoding encoding)
            => new StreamWriter(this.OpenWrite(), encoding);

        /// <summary>
        /// Deletes the specified stream.
        /// </summary>
        public void Delete()
            => InteropWrapper.Delete(this);
    }

    public enum FileDataStreamType : int
    {
        Unknown,

        [FileDataStreamTypeValue("$ATTRIBUTE_LIST")]
        AttributeList,

        [FileDataStreamTypeValue("$BITMAP")]
        Bitmap,

        [FileDataStreamTypeValue("$DATA")]
        Data,

        [FileDataStreamTypeValue("$EA")]
        ExtendedAttributes,

        [FileDataStreamTypeValue("$EA_INFORMATION")]
        ExtendedAttributeInformation,

        [FileDataStreamTypeValue("$FILE_NAME")]
        FileName,

        [FileDataStreamTypeValue("$INDEX_ALLOCATION")]
        IndexAllocation,

        [FileDataStreamTypeValue("$INDEX_ROOT")]
        IndexRoot,

        [FileDataStreamTypeValue("$LOGGED_UTILITY_STREAM")]
        LoggedUtilityStream,

        [FileDataStreamTypeValue("$OBJECT_ID")]
        ObjectId,

        [FileDataStreamTypeValue("$REPARSE_POINT")]
        ReparsePoint
    }

    public static class FileDataStreamTypeConverter
    {
        private static IReadOnlyDictionary<string, FileDataStreamType> TypeCache { get; } = GenerateTypeCache();

        public static FileDataStreamType GetStreamType(string typeName)
        {
            return TypeCache.TryGetValue(typeName, out var streamType) ? FileDataStreamType.Unknown : streamType;
        }

        private static IReadOnlyDictionary<string, FileDataStreamType> GenerateTypeCache()
            => typeof(FileDataStreamType)
                .GetFields(BindingFlags.Public | BindingFlags.Static)
                .Select(x => new { value = (FileDataStreamType)x.GetValue(null), name = x.GetCustomAttribute<FileDataStreamTypeValueAttribute>() })
                .Where(x => x.name != null)
                .ToDictionary(x => x.name.TypeNameString, x => x.value);
    }

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
    public sealed class FileDataStreamTypeValueAttribute : Attribute
    {
        public string TypeNameString { get; }

        public FileDataStreamTypeValueAttribute(string typeNameString)
        {
            this.TypeNameString = typeNameString;
        }
    }

    /// <summary>
    /// Contains NTFS-ADS extension methods for <see cref="FileInfo"/> class.
    /// </summary>
    public static class FileInfoExtensions
    {
        /// <summary>
        /// Enumerates all data streams contained within specified file.
        /// </summary>
        /// <param name="file">File to query data streams for.</param>
        /// <returns>An enumerator over data streams contained within the file.</returns>
        public static IEnumerable<FileDataStream> GetDataStreams(this FileInfo file)
            => InteropWrapper.EnumerateDataStreams(file);

        /// <summary>
        /// Creates a new alternate data stream within specified file. If a stream with this name exists, an exception is thrown.
        /// </summary>
        /// <param name="file">File to create the data stream for.</param>
        /// <param name="name">Name of the data stream to create.</param>
        /// <returns><see cref="FileStream"/> instance for IO operations.</returns>
        public static FileStream CreateDataStream(this FileInfo file, string name)
            => InteropWrapper.Open(new FileDataStream(file, name, 0, FileDataStreamType.Data), FileMode.CreateNew, FileAccess.Write, FileShare.None);

        /// <summary>
        /// Creates a new alternate data stream within specified file in text mode. If a stream with this name exists, an exception is thrown.
        /// </summary>
        /// <param name="file">File to create the data stream for.</param>
        /// <param name="name">Name of the data stream to create.</param>
        /// <param name="encoding">Encoding to use for text.</param>
        /// <returns><see cref="StreamWriter"/> instance for text writing operations.</returns>
        public static StreamWriter CreateTextDataStream(this FileInfo file, string name, Encoding encoding)
            => new StreamWriter(CreateDataStream(file, name), encoding);

        /// <summary>
        /// Gets an existing alternate data stream within specified file. Throws if specified stream does not exist.
        /// </summary>
        /// <param name="file">File to query data streams for.</param>
        /// <param name="name">Name of the data stream to retrieve.</param>
        /// <returns>Specified stream info.</returns>
        public static FileDataStream GetDataStream(this FileInfo file, string name)
            => GetDataStreams(file).FirstOrDefault(x => x.Name == name);
    }

    internal static class Interop
    {
        public const string KERNEL32 = "kernel32";

        #region Data Streams
        /// <summary>
        /// https://docs.microsoft.com/en-us/windows/win32/api/fileapi/nf-fileapi-findfirststreamw
        /// </summary>
        /// <param name="lpFileName">Path to file.</param>
        /// <param name="infoLevel">Must be <see cref="StreamInfoLevels.FindStreamInfoStandard"/>.</param>
        /// <param name="lpFindStreamData">Pointer to <see cref="FindStreamData"/>.</param>
        /// <param name="flags">Must be 0.</param>
        /// <returns>Handle or <see cref="INVALID_HANDLE_VALUE"/>.</returns>
        [DllImport(KERNEL32, CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern IntPtr FindFirstStreamW(
            string lpFileName,
            StreamInfoLevels infoLevel,
            IntPtr lpFindStreamData,
            int flags);

        [DllImport(KERNEL32, CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool FindNextStreamW(
            IntPtr hFindStream,
            IntPtr lpFindStreamData);

        [DllImport(KERNEL32, SetLastError = true)]
        public static extern bool FindClose(
            IntPtr hFindFile);

        public enum StreamInfoLevels : int
        {
            FindStreamInfoStandard = 0,
            FindStreamInfoMaxInfoLevel
        }

        [StructLayout(LayoutKind.Sequential)]
        public unsafe struct FindStreamData
        {
            public long StreamSize;
            // TODO: name
            public fixed char cStreamName[296];
        }

        public const int ERROR_HANDLE_EOF = 38;

        public static IntPtr INVALID_HANDLE_VALUE { get; } = new IntPtr(-1);
        #endregion

        #region Error Messages
        [DllImport(KERNEL32, CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern int FormatMessage(
            FormatMessageFlags dwFlags,
            IntPtr lpSource,
            int dwMessageId,
            int dwLanguageId,
            IntPtr buffer,
            int nSize,
            IntPtr arguments);

        [Flags]
        public enum FormatMessageFlags : int
        {
            FORMAT_MESSAGE_ALLOCATE_BUFFER = 0x100,
            FORMAT_MESSAGE_ARGUMENT_ARRAY = 0x2000,
            FORMAT_MESSAGE_FROM_HMODULE = 0x800,
            FORMAT_MESSAGE_FROM_STRING = 0x400,
            FORMAT_MESSAGE_FROM_SYSTEM = 0x1000,
            FORMAT_MESSAGE_IGNORE_INSERTS = 0x200,
            FORMAT_MESSAGE_MAX_WIDTH_MASK = 0xFF,

            Defaults = FORMAT_MESSAGE_IGNORE_INSERTS | FORMAT_MESSAGE_FROM_SYSTEM | FORMAT_MESSAGE_ARGUMENT_ARRAY
        }
        #endregion

        #region File IO
        [DllImport(KERNEL32, CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern IntPtr CreateFileW(
            string lpFileName,
            FileAccessMode dwDesiredAccess,
            FileShareMode dwShareMode,
            IntPtr lpSecurityAttributes, // always 0
            FileCreationDisposition dwCreationDisposition,
            FileFlagsAndAttributes dwFlagsAndAttributes, // typically just FILE_FLAG_OVERLAPPED
            IntPtr hTemplateFile);

        [DllImport(KERNEL32, CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool DeleteFileW(
            string lpFileName);

        [Flags]
        public enum FileAccessMode : uint
        {
            None = 0,
            GENERIC_READ = 0x80000000,
            GENERIC_WRITE = 0x40000000,
            GENERIC_EXECUTE = 0x20000000,
            GENERIC_ALL = 0x10000000
        }

        [Flags]
        public enum FileShareMode : int
        {
            None = 0,
            FILE_SHARE_DELETE = 0x00000004,
            FILE_SHARE_READ = 0x00000001,
            FILE_SHARE_WRITE = 0x00000002
        }

        public enum FileCreationDisposition : int
        {
            None = 0,
            CREATE_ALWAYS = 2,
            CREATE_NEW = 1,
            OPEN_ALWAYS = 4,
            OPEN_EXISTING = 3,
            TRUNCATE_EXISTING = 5
        }

        [Flags]
        public enum FileFlagsAndAttributes : uint
        {
            None = 0,

            FILE_ATTRIBUTE_ARCHIVE = 0x20,
            FILE_ATTRIBUTE_ENCRYPTED = 0x4000,
            FILE_ATTRIBUTE_HIDDEN = 0x2,
            FILE_ATTRIBUTE_NORMAL = 0x80,
            FILE_ATTRIBUTE_OFFLINE = 0x1000,
            FILE_ATTRIBUTE_READONLY = 0x1,
            FILE_ATTRIBUTE_SYSTEM = 0x4,
            FILE_ATTRIBUTE_TEMPORARY = 0x100,

            FILE_FLAG_BACKUP_SEMANTICS = 0x02000000,
            FILE_FLAG_DELETE_ON_CLOSE = 0x04000000,
            FILE_FLAG_NO_BUFFERING = 0x20000000,
            FILE_FLAG_OPEN_NO_RECALL = 0x00100000,
            FILE_FLAG_OPEN_REPARSE_POINT = 0x00200000,
            FILE_FLAG_OVERLAPPED = 0x40000000,
            FILE_FLAG_POSIX_SEMANTICS = 0x01000000,
            FILE_FLAG_RANDOM_ACCESS = 0x10000000,
            FILE_FLAG_SESSION_AWARE = 0x00800000,
            FILE_FLAG_SEQUENTIAL_SCAN = 0x08000000,
            FILE_FLAG_WRITE_THROUGH = 0x80000000
        }
        #endregion
    }

    internal static class InteropWrapper
    {
        #region Stream Enumerators
        public static IEnumerable<FileDataStream> EnumerateDataStreams(this FileInfo file)
        {
            if (file == null)
                throw new ArgumentNullException(nameof(file));

            // init locals
            var fpath = file.FullName;
            var fsd = new Interop.FindStreamData();
            AsIntPtr(ref fsd, out var fsdptr);
            var hFindStream = Interop.INVALID_HANDLE_VALUE;

            try
            {
                // check if we can get any stream
                if (!FindFirstStream(fpath, fsdptr, out hFindStream))
                    yield break;

                // Extract stream info
                ExtractStreamInfo(fsd.PtrToString(), out var streamName, out var streamType);
                yield return new FileDataStream(file, streamName, fsd.StreamSize, streamType);

                // Extract more streams until we run out
                while (FindNextStream(fsdptr, hFindStream))
                {
                    ExtractStreamInfo(fsd.PtrToString(), out streamName, out streamType);
                    yield return new FileDataStream(file, streamName, fsd.StreamSize, streamType);
                }
            }
            finally
            {
                if (hFindStream != Interop.INVALID_HANDLE_VALUE)
                    FindClose(hFindStream);
            }
        }

        // enumerator workarounds
        private static unsafe bool FindFirstStream(string lpFileName, IntPtr lpFindStreamData, out IntPtr hFindStream)
        {
            hFindStream = Interop.FindFirstStreamW(lpFileName, Interop.StreamInfoLevels.FindStreamInfoStandard, lpFindStreamData, 0);
            if (hFindStream == Interop.INVALID_HANDLE_VALUE)
            {
                var err = Marshal.GetLastWin32Error();
                if (err == Interop.ERROR_HANDLE_EOF)
                    return false;

                ThrowErrorAsException(err, lpFileName);
            }

            return true;
        }

        private static unsafe bool FindNextStream(IntPtr lpFindStreamData, IntPtr hFindStream)
        {
            if (!Interop.FindNextStreamW(hFindStream, lpFindStreamData))
            {
                var err = Marshal.GetLastWin32Error();
                if (err == Interop.ERROR_HANDLE_EOF)
                    return false;

                ThrowErrorAsException(err, null);
            }

            return true;
        }

        private static unsafe void FindClose(IntPtr hFindStream)
        {
            if (!Interop.FindClose(hFindStream))
                ThrowErrorAsException(Marshal.GetLastWin32Error(), null);
        }
        #endregion

        #region IO Helpers
        public static FileStream Open(FileDataStream ads, FileMode mode, FileAccess access, FileShare share)
        {
            //if (ads.Type != FileDataStreamType.Data) throw new InvalidOperationException("Only $DATA streams can be opened for reading or writing.");

            if (ads.Name.Length == 0) // default stream
                return ads.File.Open(mode, access, share);

            var (nmode, nflags) = ManagedToNative(mode);
            var naccess = ManagedToNative(access);
            var nshare = ManagedToNative(share);

            var lpFileName = $"{ads.File.FullName}:{ads.Name}";

            var hFile = Interop.CreateFileW(lpFileName, naccess, nshare, IntPtr.Zero, nmode, nflags, IntPtr.Zero);
            if (hFile == Interop.INVALID_HANDLE_VALUE)
            {
                ThrowErrorAsException(Marshal.GetLastWin32Error(), lpFileName);
                return null;
            }

            return new FileStream(new SafeFileHandle(hFile, true), access, 4096, true);
        }

        public static void Delete(FileDataStream ads)
        {
            if (ads.Type != FileDataStreamType.Data)
                throw new InvalidOperationException("Only $DATA streams can be deleted.");

            if (ads.Name.Length == 0) // default stream
            {
                ads.File.Delete();
                return;
            }

            var lpFileName = $"{ads.File.FullName}:{ads.Name}";
            if (!Interop.DeleteFileW(lpFileName))
                ThrowErrorAsException(Marshal.GetLastWin32Error(), lpFileName);
        }

        private static Interop.FileShareMode ManagedToNative(FileShare share)
        {
            var mode = Interop.FileShareMode.None;

            if ((share & FileShare.Delete) == FileShare.Delete) mode |= Interop.FileShareMode.FILE_SHARE_DELETE;
            if ((share & FileShare.Read) == FileShare.Read) mode |= Interop.FileShareMode.FILE_SHARE_READ;
            if ((share & FileShare.Write) == FileShare.Write) mode |= Interop.FileShareMode.FILE_SHARE_WRITE;

            return mode;
        }

        private static Interop.FileAccessMode ManagedToNative(FileAccess access)
        {
            var mode = Interop.FileAccessMode.None;

            if ((access & FileAccess.Read) == FileAccess.Read) mode |= Interop.FileAccessMode.GENERIC_READ;
            if ((access & FileAccess.Write) == FileAccess.Write) mode |= Interop.FileAccessMode.GENERIC_WRITE;

            return mode;
        }

        private static (Interop.FileCreationDisposition creation, Interop.FileFlagsAndAttributes attribs) ManagedToNative(FileMode mode)
        {
            switch (mode)
            {
                case FileMode.CreateNew: return (Interop.FileCreationDisposition.CREATE_NEW, Interop.FileFlagsAndAttributes.FILE_FLAG_OVERLAPPED);
                case FileMode.Create: return (Interop.FileCreationDisposition.CREATE_ALWAYS, Interop.FileFlagsAndAttributes.FILE_FLAG_OVERLAPPED);
                case FileMode.Open: return (Interop.FileCreationDisposition.OPEN_EXISTING, Interop.FileFlagsAndAttributes.FILE_FLAG_OVERLAPPED);
                case FileMode.OpenOrCreate: return (Interop.FileCreationDisposition.OPEN_ALWAYS, Interop.FileFlagsAndAttributes.FILE_FLAG_OVERLAPPED);
                case FileMode.Truncate: return (Interop.FileCreationDisposition.TRUNCATE_EXISTING, Interop.FileFlagsAndAttributes.FILE_FLAG_OVERLAPPED);
                case FileMode.Append: return (Interop.FileCreationDisposition.OPEN_ALWAYS, Interop.FileFlagsAndAttributes.FILE_FLAG_OVERLAPPED);
                default: throw new Exception("Unknown mode");
            }
        }
        #endregion

        #region Helpers
        private static unsafe void AsIntPtr<T>(ref T val, out IntPtr ptr)
            => ptr = new IntPtr(Unsafe.AsPointer(ref val));

        private static unsafe string PtrToString(this Interop.FindStreamData fsd)
            => new string(fsd.cStreamName);

        private static unsafe void ExtractStreamInfo(string s, out string name, out FileDataStreamType type)
        {
            var ss = s.AsSpan(1);
            var nl = ss.IndexOf(':');
            var ns = ss.Slice(0, nl);
            ss = ss.Slice(nl + 1);

#if NETCOREAPP2_1
            name = new string(ns);
            var ts = new string(ss);
#else
            fixed (char* sp = &ns.GetPinnableReference())
                name = new string(sp);

            string ts;
            fixed (char* sp = &ns.GetPinnableReference())
                ts = new string(sp);
#endif

            type = FileDataStreamTypeConverter.GetStreamType(ts);
        }

        private static void ThrowErrorAsException(int code, string fpath)
        {
            //https://github.com/RichardD2/NTFS-Streams/blob/master/ntfsstreams/Trinet.Core.IO.Ntfs/SafeNativeMethods.cs

            if (fpath == null)
                fpath = "<null>";

            switch (code)
            {
                case 2:
                    throw new FileNotFoundException("Specified file was not found.", fpath);
                case 3:
                    throw new DirectoryNotFoundException($"Could not find a part of the path \"{fpath}\".");
                case 5:
                    throw new UnauthorizedAccessException($"Access to the path \"{fpath}\" was denied.");
                case 15:
                    throw new DriveNotFoundException($"Access to the path \"{fpath}\" was denied.");
                case 32:
                    throw new IOException($"The process cannot access the file \"{fpath}\" because it is being used by another process.", HResultFromError(code));
                case 80:
                    throw new IOException($"The file \"{fpath}\" already exists.", HResultFromError(code));
                case 87:
                    throw new IOException(MessageFromError(code), HResultFromError(code));
                case 183:
                    throw new IOException($"Cannot create \"{fpath}\" because a file or directory with the same name already exists.");
                case 206:
                    throw new PathTooLongException();
                case 995:
                    throw new OperationCanceledException();
                default:
                    throw Marshal.GetExceptionForHR(HResultFromError(code));
            }

        }

        private static int HResultFromError(int code)
            => unchecked((int)0x80070000 | code);

        private unsafe static string MessageFromError(int code)
        {
            var lpBufferSize = 512;
            var lpBuffer = stackalloc char[lpBufferSize];
            lpBufferSize = Interop.FormatMessage(Interop.FormatMessageFlags.Defaults, IntPtr.Zero, code, 0, new IntPtr(lpBuffer), lpBufferSize, IntPtr.Zero);
            if (lpBufferSize != 0)
                return new string(lpBuffer, 0, lpBufferSize);

            return $"Unknown IO error.";
        }

        internal static FileStream SeekToEnd(this FileStream fs)
        {
            fs.Seek(0, SeekOrigin.End);
            return fs;
        }
        #endregion
    }
}
