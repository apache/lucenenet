using Lucene.Net.Util;
using System.IO;
using System.Runtime.InteropServices;

namespace Lucene.Net.Support.IO
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

    internal static class PosixFsyncSupport
    {
        // https://pubs.opengroup.org/onlinepubs/009695399/functions/fsync.html
        [DllImport("libc", SetLastError = true)]
        private static extern int fsync(int fd);

        // https://pubs.opengroup.org/onlinepubs/007904875/functions/open.html
        [DllImport("libc", SetLastError = true)]
        private static extern int open([MarshalAs(UnmanagedType.LPStr)] string pathname, int flags);

        // https://pubs.opengroup.org/onlinepubs/009604499/functions/close.html
        [DllImport("libc", SetLastError = true)]
        private static extern int close(int fd);

        // https://pubs.opengroup.org/onlinepubs/007904975/functions/fcntl.html
        // and https://developer.apple.com/library/archive/documentation/System/Conceptual/ManPages_iPhoneOS/man2/fcntl.2.html
        [DllImport("libc", SetLastError = true)]
        private static extern int fcntl(int fd, int cmd, int arg);

        private const int O_RDONLY = 0;
        private const int O_WRONLY = 1;

        // https://opensource.apple.com/source/xnu/xnu-6153.81.5/bsd/sys/fcntl.h.auto.html
        private const int F_FULLFSYNC = 51;

        public static void Fsync(string path, bool isDir)
        {
            using DescriptorWrapper handle = new DescriptorWrapper(path, isDir);
            handle.Flush();
        }

        private readonly ref struct DescriptorWrapper
        {
            private readonly int fd;

            public DescriptorWrapper(string path, bool isDir)
            {
                fd = open(path, isDir ? O_RDONLY : O_WRONLY);

                if (fd == -1)
                {
                    int error = Marshal.GetLastWin32Error();
                    throw new IOException($"Unable to open path, error: 0x{error:x8}", error);
                }
            }

            public void Flush()
            {
                // if macOS, use F_FULLFSYNC
                if (Constants.MAC_OS_X)
                {
                    if (fcntl(fd, F_FULLFSYNC, 0) == -1)
                    {
                        throw new IOException("fcntl failed", Marshal.GetLastWin32Error());
                    }
                }
                else if (fsync(fd) == -1)
                {
                    throw new IOException("fsync failed", Marshal.GetLastWin32Error());
                }
            }

            public void Dispose()
            {
                if (close(fd) == -1)
                {
                    throw new IOException("close failed", Marshal.GetLastWin32Error());
                }
            }
        }
    }
}
