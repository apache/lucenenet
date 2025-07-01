using System;
using System.IO;
using System.Runtime.InteropServices;
using static Lucene.Net.Native.Interop.Win32;

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

    public static class WindowsFsyncSupport
    {
        public static void Fsync(string path, bool isDir)
        {
            using HandleWrapper handle = new HandleWrapper(path, isDir);
            handle.Flush();
        }

        private readonly ref struct HandleWrapper
        {
            private readonly IntPtr handle;

            public HandleWrapper(string path, bool isDir)
            {
                handle = CreateFileW(path,
                    GENERIC_WRITE,
                    FILE_SHARE_READ | FILE_SHARE_WRITE | FILE_SHARE_DELETE,
                    IntPtr.Zero,
                    OPEN_EXISTING,
                    (uint)(isDir ? FILE_FLAG_BACKUP_SEMANTICS : 0), // FILE_FLAG_BACKUP_SEMANTICS required to open a directory
                    IntPtr.Zero);

                if (handle == INVALID_HANDLE_VALUE)
                {
                    int error = Marshal.GetLastWin32Error();

                    throw error switch
                    {
                        ERROR_FILE_NOT_FOUND => new FileNotFoundException($"File not found: {path}"),
                        ERROR_PATH_NOT_FOUND => new DirectoryNotFoundException($"Directory/path not found: {path}"),
                        ERROR_ACCESS_DENIED => new UnauthorizedAccessException($"Access denied to {(isDir ? "directory" : "file")}: {path}"),
                        _ => new IOException($"Unable to open {(isDir ? "directory" : "file")}, error: 0x{error:x8}", error)
                    };
                }
            }

            public void Flush()
            {
                if (!FlushFileBuffers(handle))
                {
                    int error = Marshal.GetLastWin32Error();

                    if (error != ERROR_ACCESS_DENIED)
                    {
                        // swallow ERROR_ACCESS_DENIED like in OpenJDK
                        throw new IOException($"FlushFileBuffers failed, error: 0x{error:x8}", error);
                    }
                }
            }

            public void Dispose()
            {
                if (!CloseHandle(handle))
                {
                    int error = Marshal.GetLastWin32Error();
                    throw new IOException($"CloseHandle failed, error: 0x{error:x8}", error);
                }
            }
        }
    }
}
