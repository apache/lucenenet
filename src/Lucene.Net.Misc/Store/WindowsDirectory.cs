using Lucene.Net.Util;
using Microsoft.Win32.SafeHandles;
using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
#if FEATURE_SUPPORTEDOSPLATFORMATTRIBUTE
using System.Runtime.Versioning;
#endif
using System.Threading;

namespace Lucene.Net.Store
{
    /*
     * Licensed to the Apache Software Foundation (ASF) under one or more
     * contributor license agreements. See the NOTICE file distributed with this
     * work for additional information regarding copyright ownership. The ASF
     * licenses this file to You under the Apache License, Version 2.0 (the
     * "License"); you may not use this file except in compliance with the License.
     * You may obtain a copy of the License at
     *
     * http://www.apache.org/licenses/LICENSE-2.0
     *
     * Unless required by applicable law or agreed to in writing, software
     * distributed under the License is distributed on an "AS IS" BASIS, WITHOUT
     * WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the
     * License for the specific language governing permissions and limitations under
     * the License.
     */

    /// <summary>
    /// Native <see cref="Directory"/> implementation for Microsoft Windows.
    /// <para/>
    /// This uses <c>CreateFile</c> with the <c>FILE_FLAG_RANDOM_ACCESS</c> cache hint via
    /// P/Invoke, so that the operating system can optimize its caching for the random-access
    /// pattern that searching produces. Each read supplies its own file offset through the
    /// <c>OVERLAPPED</c> structure; because the handle is synchronous (not opened with
    /// <c>FILE_FLAG_OVERLAPPED</c>), these positioned reads are serialized by the OS on the
    /// shared handle rather than running in parallel, but no explicit seek state is needed and
    /// clones can read independently.
    /// <para/>
    /// <font color="red"><b>NOTE:</b> Unlike the original Lucene implementation, which
    /// required compiling a native <c>WindowsDirectory.dll</c> with the JNI sources,
    /// this implementation calls the Win32 APIs directly through P/Invoke. No native
    /// build step is required, but it can only be used on Microsoft Windows.</font>
    /// <para/>
    /// @lucene.experimental
    /// </summary>
#if FEATURE_SUPPORTEDOSPLATFORMATTRIBUTE
    [SupportedOSPlatform("windows")]
#endif
    public class WindowsDirectory : FSDirectory
    {
        private const int DEFAULT_BUFFERSIZE = 4096; // default pgsize on ia32/amd64

        /// <summary>
        /// Create a new <see cref="WindowsDirectory"/> for the named location.
        /// </summary>
        /// <param name="path"> the path of the directory </param>
        /// <param name="lockFactory"> the lock factory to use, or null for the default
        /// (<see cref="NativeFSLockFactory"/>); </param>
        /// <exception cref="IOException"> If there is a low-level I/O error </exception>
        /// <exception cref="PlatformNotSupportedException"> If not running on Microsoft Windows </exception>
        public WindowsDirectory(DirectoryInfo path, LockFactory lockFactory)
            : base(path, lockFactory)
        {
            EnsureWindows();
        }

        /// <summary>
        /// Create a new <see cref="WindowsDirectory"/> for the named location and <see cref="NativeFSLockFactory"/>.
        /// </summary>
        /// <param name="path"> the path of the directory </param>
        /// <exception cref="IOException"> If there is a low-level I/O error </exception>
        /// <exception cref="PlatformNotSupportedException"> If not running on Microsoft Windows </exception>
        public WindowsDirectory(DirectoryInfo path)
            : base(path, null)
        {
            EnsureWindows();
        }

        /// <summary>
        /// Create a new <see cref="WindowsDirectory"/> for the named location.
        /// <para/>
        /// LUCENENET specific overload for convenience using string instead of <see cref="DirectoryInfo"/>.
        /// </summary>
        /// <param name="path"> the path of the directory </param>
        /// <param name="lockFactory"> the lock factory to use, or null for the default
        /// (<see cref="NativeFSLockFactory"/>); </param>
        /// <exception cref="IOException"> If there is a low-level I/O error </exception>
        /// <exception cref="PlatformNotSupportedException"> If not running on Microsoft Windows </exception>
        public WindowsDirectory(string path, LockFactory lockFactory)
            : this(new DirectoryInfo(path), lockFactory)
        {
        }

        /// <summary>
        /// Create a new <see cref="WindowsDirectory"/> for the named location and <see cref="NativeFSLockFactory"/>.
        /// <para/>
        /// LUCENENET specific overload for convenience using string instead of <see cref="DirectoryInfo"/>.
        /// </summary>
        /// <param name="path"> the path of the directory </param>
        /// <exception cref="IOException"> If there is a low-level I/O error </exception>
        /// <exception cref="PlatformNotSupportedException"> If not running on Microsoft Windows </exception>
        public WindowsDirectory(string path)
            : this(path, null)
        {
        }

        private static void EnsureWindows()
        {
            if (!Constants.WINDOWS)
            {
                throw new PlatformNotSupportedException($"{nameof(WindowsDirectory)} is only supported on Microsoft Windows.");
            }
        }

        public override IndexInput OpenInput(string name, IOContext context)
        {
            EnsureOpen();
            var path = Path.Combine(Directory.FullName, name);
            return new WindowsIndexInput(path, Math.Max(BufferedIndexInput.GetBufferSize(context), DEFAULT_BUFFERSIZE));
        }

        internal class WindowsIndexInput : BufferedIndexInput
        {
            private readonly SafeFileHandle fd;
            private readonly long length;
            private bool isClone;
            private bool isOpen;
            private int disposed = 0; // LUCENENET specific - allow double-dispose

            public WindowsIndexInput(string path, int bufferSize)
                : base("WindowsIndexInput(path=\"" + path + "\")", bufferSize)
            {
                fd = WindowsDirectory.OpenFile(path);
                try
                {
                    length = WindowsDirectory.Length(fd);
                }
                catch
                {
                    // LUCENENET specific: avoid leaking the handle if reading the length fails;
                    // the ctor throws so Dispose() will never be called on this instance.
                    fd.Dispose();
                    throw;
                }
                isOpen = true;
            }

            protected override void ReadInternal(Span<byte> b)
            {
                int bytesRead;
                try
                {
                    bytesRead = WindowsDirectory.Read(fd, b, Position); // LUCENENET: Position is the file pointer (renamed from getFilePointer())
                }
                catch (Exception ioe) when (ioe.IsIOException())
                {
                    // LUCENENET: surface the original HResult (via the ctor that accepts it, which is
                    // public on all target frameworks) so callers such as NativeFSLock, which inspect
                    // IOException.HResult, still see the underlying error. The inner detail is retained
                    // in the message.
                    throw new IOException(ioe.Message + ": " + this, ioe.HResult);
                }

                if (bytesRead != b.Length)
                {
                    throw EOFException.Create("read past EOF: " + this);
                }
            }

            protected override void SeekInternal(long pos)
            {
            }

            protected override void Dispose(bool disposing)
            {
                // NOTE: we track "isOpen" because Lucene sometimes closes IndexInputs twice!
                if (0 != Interlocked.CompareExchange(ref this.disposed, 1, 0)) return; // LUCENENET specific - allow double-dispose

                if (disposing && !isClone && isOpen)
                {
                    fd.Dispose(); // closes the underlying Win32 handle (CloseHandle)
                    isOpen = false;
                }
            }

            public override sealed long Length => length;

            public override object Clone()
            {
                WindowsIndexInput clone = (WindowsIndexInput)base.Clone();
                clone.isClone = true;
                return clone;
            }
        }

        /// <summary>
        /// Opens a handle to a file. </summary>
        private static SafeFileHandle OpenFile(string filename)
        {
            // LUCENENET specific: include FILE_SHARE_DELETE (upstream used only READ|WRITE) so that,
            // like SimpleFSDirectory's read path (#1283), an open read handle does not block deletion
            // of the underlying file on Windows.
            IntPtr handle = NativeMethods.CreateFileW(
                filename,
                NativeMethods.GENERIC_READ,
                NativeMethods.FILE_SHARE_READ | NativeMethods.FILE_SHARE_WRITE | NativeMethods.FILE_SHARE_DELETE,
                IntPtr.Zero,
                NativeMethods.OPEN_EXISTING,
                NativeMethods.FILE_FLAG_RANDOM_ACCESS,
                IntPtr.Zero);
            int lastError = Marshal.GetLastWin32Error();
            // LUCENENET: surface the Win32 error as the IOException's HResult (matching what the BCL
            // sets on a FileStream IOException) so callers such as NativeFSLock can recognize it.
            int hresult = Marshal.GetHRForLastWin32Error();

            if (handle == NativeMethods.INVALID_HANDLE_VALUE)
            {
                throw new IOException("Could not open file " + filename + ": " + new Win32Exception(lastError).Message, hresult);
            }

            return new SafeFileHandle(handle, ownsHandle: true);
        }

        /// <summary>
        /// Reads data from a file at <paramref name="pos"/> into <paramref name="bytes"/>.
        /// <para/>
        /// The position is supplied via the <c>OVERLAPPED</c> structure. The handle is
        /// synchronous (no <c>FILE_FLAG_OVERLAPPED</c>), so the read completes synchronously and
        /// is serialized by the OS on the handle; the explicit offset is what lets clones share
        /// one handle without seeking. </summary>
        private static unsafe int Read(SafeFileHandle fd, Span<byte> bytes, long pos)
        {
            NativeOverlapped overlapped = default;
            overlapped.OffsetLow = (int)(pos & 0xFFFFFFFFL);
            overlapped.OffsetHigh = (int)((pos >> 0x20) & 0x7FFFFFFFL);

            int numRead;
            bool success;
            fixed (byte* p = bytes)
            {
                success = NativeMethods.ReadFile(fd, p, bytes.Length, out numRead, &overlapped);
            }

            if (!success)
            {
                int lastError = Marshal.GetLastWin32Error();
                // LUCENENET: surface the Win32 error as the IOException's HResult (see OpenFile).
                throw new IOException(new Win32Exception(lastError).Message, Marshal.GetHRForLastWin32Error());
            }

            return numRead;
        }

        /// <summary>
        /// Returns the length of a file. </summary>
        private static long Length(SafeFileHandle fd)
        {
            if (!NativeMethods.GetFileInformationByHandle(fd, out NativeMethods.BY_HANDLE_FILE_INFORMATION info))
            {
                int lastError = Marshal.GetLastWin32Error();
                // LUCENENET: surface the Win32 error as the IOException's HResult (see OpenFile).
                throw new IOException(new Win32Exception(lastError).Message, Marshal.GetHRForLastWin32Error());
            }

            return ((long)info.nFileSizeHigh << 0x20) | info.nFileSizeLow;
        }

        /// <summary>
        /// P/Invoke declarations for the Win32 APIs used by <see cref="WindowsDirectory"/>.
        /// <para/>
        /// These replace the JNI/C++ native methods (<c>open</c>, <c>read</c>, <c>close</c>,
        /// <c>length</c>) in the original Lucene implementation. Closing is handled by
        /// <see cref="SafeFileHandle"/>, which calls <c>CloseHandle</c>.
        /// </summary>
        private static class NativeMethods
        {
            internal const uint GENERIC_READ = 0x80000000;
            internal const uint FILE_SHARE_READ = 0x00000001;
            internal const uint FILE_SHARE_WRITE = 0x00000002;
            internal const uint FILE_SHARE_DELETE = 0x00000004;
            internal const uint OPEN_EXISTING = 3;
            internal const uint FILE_FLAG_RANDOM_ACCESS = 0x10000000;

            internal static readonly IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);

            // https://learn.microsoft.com/en-us/windows/win32/api/fileapi/nf-fileapi-createfilew
            [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
            internal static extern IntPtr CreateFileW(
                string lpFileName,
                uint dwDesiredAccess,
                uint dwShareMode,
                IntPtr lpSecurityAttributes,
                uint dwCreationDisposition,
                uint dwFlagsAndAttributes,
                IntPtr hTemplateFile);

            // https://learn.microsoft.com/en-us/windows/win32/api/fileapi/nf-fileapi-readfile
            [DllImport("kernel32.dll", SetLastError = true)]
            internal static extern unsafe bool ReadFile(
                SafeFileHandle hFile,
                byte* lpBuffer,
                int nNumberOfBytesToRead,
                out int lpNumberOfBytesRead,
                NativeOverlapped* lpOverlapped);

            // https://learn.microsoft.com/en-us/windows/win32/api/fileapi/nf-fileapi-getfileinformationbyhandle
            [DllImport("kernel32.dll", SetLastError = true)]
            internal static extern bool GetFileInformationByHandle(
                SafeFileHandle hFile,
                out BY_HANDLE_FILE_INFORMATION lpFileInformation);

            // https://learn.microsoft.com/en-us/windows/win32/api/fileapi/ns-fileapi-by_handle_file_information
            [StructLayout(LayoutKind.Sequential)]
            internal struct BY_HANDLE_FILE_INFORMATION
            {
                public uint dwFileAttributes;
                public System.Runtime.InteropServices.ComTypes.FILETIME ftCreationTime;
                public System.Runtime.InteropServices.ComTypes.FILETIME ftLastAccessTime;
                public System.Runtime.InteropServices.ComTypes.FILETIME ftLastWriteTime;
                public uint dwVolumeSerialNumber;
                public uint nFileSizeHigh;
                public uint nFileSizeLow;
                public uint nNumberOfLinks;
                public uint nFileIndexHigh;
                public uint nFileIndexLow;
            }
        }
    }
}
