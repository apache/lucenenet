using Lucene.Net.Util;
using System;
using System.IO;
using System.Runtime.InteropServices;
using static Lucene.Net.Native.Interop.Posix;
using static Lucene.Net.Native.Interop.MacOS;

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

                    throw error switch
                    {
                        ENOENT when isDir => new DirectoryNotFoundException($"Directory/path not found: {path}"),
                        ENOENT => new FileNotFoundException($"File not found: {path}"),
                        EACCES => new UnauthorizedAccessException($"Access denied to {(isDir ? "directory" : "file")}: {path}"),
                        _ => new IOException($"Unable to open path, error: 0x{error:x8}", error)
                    };
                }
            }

            public void Flush()
            {
                // if macOS, use F_FULLFSYNC
                if (Constants.MAC_OS_X)
                {
                    if (fcntl(fd, F_FULLFSYNC, 0) == -1)
                    {
                        int error = Marshal.GetLastWin32Error();
                        throw new IOException($"fcntl failed, error: 0x{error:x8}", error);
                    }
                }
                else if (fsync(fd) == -1)
                {
                    int error = Marshal.GetLastWin32Error();
                    throw new IOException($"fsync failed, error: 0x{error:x8}", error);
                }
            }

            public void Dispose()
            {
                if (close(fd) == -1)
                {
                    int error = Marshal.GetLastWin32Error();
                    throw new IOException($"close failed, error: 0x{error:x8}", error);
                }
            }
        }
    }
}
