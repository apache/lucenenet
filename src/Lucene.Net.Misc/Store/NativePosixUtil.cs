using Lucene.Net.Util;
using Microsoft.Win32.SafeHandles;
using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
#if FEATURE_SUPPORTEDOSPLATFORMATTRIBUTE
using System.Runtime.Versioning;
#endif

namespace Lucene.Net.Store
{
    /*
     * Licensed to the Apache Software Foundation (ASF) under one or more
     * contributor license agreements.  See the NOTICE file distributed with
     * this work for additional information regarding copyright ownership.
     * The ASF licenses this file to You under the Apache License, Version 2.0
     * (the "License"); you may not use this file except in compliance with
     * the License.  You may obtain a copy of the License at
     *
     *     http://www.apache.org/licenses/LICENSE-2.0
     *
     * Unless required by applicable law or agreed to in writing, software
     * distributed under the License is distributed on an "AS IS" BASIS,
     * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
     * See the License for the specific language governing permissions and
     * limitations under the License.
     */

    /// <summary>
    /// Provides access to native POSIX methods such as <c>madvise()</c> for
    /// <see cref="NativeUnixDirectory"/>.
    /// <para/>
    /// LUCENENET specific: the original Lucene implementation called these through a JNI
    /// shim compiled from <c>NativePosixUtil.cpp</c>. This port replaces that native build
    /// step with direct P/Invoke into <c>libc</c>, so it can only be used on Unix-like
    /// platforms (Linux and macOS); it is not supported on Microsoft Windows.
    /// </summary>
#if FEATURE_SUPPORTEDOSPLATFORMATTRIBUTE
    [UnsupportedOSPlatform("windows")]
#endif
    public static class NativePosixUtil
    {
        // These constants mirror the Java NativePosixUtil ordering. Note this is NOT the same
        // ordering as the OS POSIX_FADV_*/POSIX_MADV_* values (SEQUENTIAL/RANDOM are swapped);
        // MapAdvice() translates to the OS values.
        public const int NORMAL = 0;
        public const int SEQUENTIAL = 1;
        public const int RANDOM = 2;
        public const int WILLNEED = 3;
        public const int DONTNEED = 4;
        public const int NOREUSE = 5;

        /// <summary>
        /// Opens a file for direct (un-cached) I/O.
        /// <para/>
        /// On Linux this uses <c>O_DIRECT | O_NOATIME</c>; on macOS it opens normally and then
        /// applies <c>fcntl(F_NOCACHE)</c>, matching the original native implementation.
        /// </summary>
        /// <param name="filename"> the file to open </param>
        /// <param name="read"> <c>true</c> to open read-only; <c>false</c> to open read-write (creating if needed) </param>
        /// <returns> a <see cref="SafeFileHandle"/> wrapping the open file descriptor </returns>
        /// <exception cref="IOException"> If the file could not be opened </exception>
        /// <exception cref="PlatformNotSupportedException"> If running on Microsoft Windows </exception>
        public static SafeFileHandle OpenDirect(string filename, bool read)
        {
            EnsureUnix();

            // Upstream's C++ called open(fname, O_RDWR | O_CREAT | DIRECT_FLAG, 0666) for the write path.
            // We cannot create-with-mode through P/Invoke: open() is variadic (int open(const char*, int, ...))
            // and the mode argument rides the varargs ABI. On platforms where varargs are not passed in the
            // same registers as fixed args (notably macOS, including Apple Silicon arm64), a fixed-signature
            // P/Invoke delivers garbage for mode, producing a file without owner-read permission, so the
            // subsequent O_RDONLY open of the same file fails with EACCES. To stay correct and ABI-agnostic
            // we never pass mode: the file is created up front by the BCL (which applies the normal mode and
            // umask), and we then open it with the plain two-argument open() - no O_CREAT, no varargs.
            if (!read)
            {
                // mirrors O_CREAT: ensure the file exists with a sane mode before opening it
                using (System.IO.File.Open(filename, System.IO.FileMode.OpenOrCreate, System.IO.FileAccess.ReadWrite)) { }
            }

            int fd;
            if (Constants.MAC_OS_X)
            {
                fd = read
                    ? NativeMethods.open(filename, NativeMethods.O_RDONLY)
                    : NativeMethods.open(filename, NativeMethods.O_RDWR);
                if (fd < 0)
                {
                    throw NewIOException("open", filename);
                }

                // macOS has no O_DIRECT; disable the page cache for this descriptor instead.
                if (NativeMethods.fcntl(fd, NativeMethods.F_NOCACHE, 1) < 0)
                {
                    int err = Marshal.GetLastWin32Error();
                    NativeMethods.close(fd);
                    throw NewIOException("fcntl(F_NOCACHE)", filename, err);
                }
            }
            else // Linux (and other Unixes, best-effort)
            {
                fd = read
                    ? NativeMethods.open(filename, NativeMethods.O_RDONLY | NativeMethods.O_DIRECT | NativeMethods.O_NOATIME)
                    : NativeMethods.open(filename, NativeMethods.O_RDWR | NativeMethods.O_DIRECT | NativeMethods.O_NOATIME);
                if (fd < 0)
                {
                    throw NewIOException("open", filename);
                }
            }

            return new SafeFileHandle((IntPtr)fd, ownsHandle: true);
        }

        /// <summary>
        /// Positioned read of up to <paramref name="length"/> bytes from <paramref name="fd"/> at
        /// absolute offset <paramref name="pos"/> into the native buffer at <paramref name="buffer"/>,
        /// without changing the file's current offset.
        /// </summary>
        /// <returns> the number of bytes read (may be less than <paramref name="length"/> near EOF) </returns>
        /// <exception cref="IOException"> If the read failed </exception>
        public static long Pread(SafeFileHandle fd, long pos, IntPtr buffer, int length)
        {
            EnsureUnix();
            long n = (long)NativeMethods.pread(Fd(fd), buffer, (nuint)length, pos);
            if (n < 0)
            {
                throw NewIOException("pread", null);
            }
            return n;
        }

        /// <summary>
        /// Issues a <c>posix_madvise()</c> hint over the native buffer at <paramref name="buffer"/>. </summary>
        public static int PosixMAdvise(IntPtr buffer, int length, int advise)
        {
            EnsureUnix();
            return NativeMethods.posix_madvise(buffer, (nuint)length, MapAdvice(advise));
        }

        /// <summary>
        /// Issues a <c>madvise()</c> hint over the native buffer at <paramref name="buffer"/>. </summary>
        public static int MAdvise(IntPtr buffer, int length, int advise)
        {
            EnsureUnix();
            return NativeMethods.madvise(buffer, (nuint)length, MapAdvice(advise));
        }

        /// <summary>
        /// Issues a <c>posix_fadvise()</c> hint for the given range of <paramref name="fd"/>,
        /// throwing if the call reports a non-zero error code.
        /// </summary>
        /// <exception cref="IOException"> If <c>posix_fadvise</c> returned a non-zero code </exception>
        public static void Advise(SafeFileHandle fd, long offset, long len, int advise)
        {
            EnsureUnix();
            int code = NativeMethods.posix_fadvise(Fd(fd), offset, len, MapAdvice(advise));
            if (code != 0)
            {
                // LUCENENET: upstream throws RuntimeException; we use IOException as this is an I/O failure.
                // posix_fadvise returns the error number directly (it does not set errno); surface it as
                // the HResult to match the errno-as-HResult convention used elsewhere here.
                throw new IOException("posix_fadvise failed code=" + code, code);
            }
        }

        /// <summary>
        /// Translates the Java-style advice ordinal (see the public constants) to the OS
        /// <c>POSIX_FADV_*</c>/<c>POSIX_MADV_*</c> value. Only <c>SEQUENTIAL</c> and <c>RANDOM</c>
        /// differ in ordering between the two.
        /// </summary>
        private static int MapAdvice(int advise)
        {
            switch (advise)
            {
                case SEQUENTIAL: return 2; // POSIX_*_SEQUENTIAL
                case RANDOM: return 1;     // POSIX_*_RANDOM
                default: return advise;    // NORMAL=0, WILLNEED=3, DONTNEED=4, NOREUSE=5 are identical
            }
        }

        private static int Fd(SafeFileHandle handle) => (int)handle.DangerousGetHandle();

        private static IOException NewIOException(string operation, string filename)
            => NewIOException(operation, filename, Marshal.GetLastWin32Error());

        private static IOException NewIOException(string operation, string filename, int errno)
        {
            string where = filename is null ? operation : $"{operation} {filename}";
            // On Unix with SetLastError, GetLastWin32Error() returns errno. Win32Exception's message
            // is not meaningful on Unix, so include the raw errno for diagnosis.
            // LUCENENET: surface the errno as the IOException's HResult. On Unix the BCL sets HResult to
            // the raw errno (e.g. a FileStream share violation surfaces HResult == EWOULDBLOCK), and
            // NativeFSLock inspects IOException.HResult, so we match that convention here.
            return new IOException($"{where} failed (errno {errno})", errno);
        }

        internal static void EnsureUnix()
        {
            if (Constants.WINDOWS)
            {
                throw new PlatformNotSupportedException(
                    $"{nameof(NativePosixUtil)} requires Linux or macOS direct I/O and is not supported on Microsoft Windows.");
            }
        }

        /// <summary>
        /// P/Invoke declarations for the <c>libc</c> functions used here. These replace the
        /// JNI/C++ native methods of the original implementation.
        /// </summary>
        private static class NativeMethods
        {
            private const string LIBC = "libc";

            // open() flags. O_DIRECT/O_NOATIME are Linux-only. We never pass O_CREAT/mode: see OpenDirect.
            internal const int O_RDONLY = 0x0;
            internal const int O_RDWR = 0x2;

            // O_DIRECT is architecture-dependent on Linux: most arches (x86/x86-64) use the asm-generic
            // value 0x4000, but Arm/Arm64 (and a few others) swap it with O_DIRECTORY and use 0x10000.
            // Using the wrong value silently means "O_DIRECTORY", which makes open() of a regular file
            // fail with ENOTDIR. O_NOATIME (0x40000) is the same across these arches.
            internal static readonly int O_DIRECT =
                RuntimeInformation.OSArchitecture is Architecture.Arm or Architecture.Arm64
                    ? 0x10000
                    : 0x4000;
            internal const int O_NOATIME = 0x40000; // Linux
            internal const int F_NOCACHE = 48;      // macOS

            [DllImport(LIBC, SetLastError = true, EntryPoint = "open")]
            internal static extern int open([MarshalAs(UnmanagedType.LPStr)] string pathname, int flags);

            [DllImport(LIBC, SetLastError = true)]
            internal static extern int close(int fd);

            [DllImport(LIBC, SetLastError = true)]
            internal static extern int fcntl(int fd, int cmd, int arg);

            [DllImport(LIBC, SetLastError = true)]
            internal static extern nint pread(int fd, IntPtr buf, nuint count, long offset);

            [DllImport(LIBC, SetLastError = true)]
            internal static extern int posix_fadvise(int fd, long offset, long len, int advice);

            [DllImport(LIBC, SetLastError = true)]
            internal static extern int posix_madvise(IntPtr addr, nuint length, int advice);

            [DllImport(LIBC, SetLastError = true)]
            internal static extern int madvise(IntPtr addr, nuint length, int advice);
        }
    }
}
