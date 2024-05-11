using System;
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

    public static class WindowsFsyncSupport
    {
        // https://learn.microsoft.com/en-us/windows/win32/api/fileapi/nf-fileapi-createfilew
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr CreateFileW(
            string lpFileName,
            uint dwDesiredAccess,
            uint dwShareMode,
            IntPtr lpSecurityAttributes,
            uint dwCreationDisposition,
            uint dwFlagsAndAttributes,
            IntPtr hTemplateFile
        );

        // https://learn.microsoft.com/en-us/windows/win32/api/fileapi/nf-fileapi-flushfilebuffers
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool FlushFileBuffers(IntPtr hFile);

        // https://learn.microsoft.com/en-us/windows/win32/api/handleapi/nf-handleapi-closehandle
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hObject);

        private static readonly IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);

        // https://learn.microsoft.com/en-us/windows/win32/debug/system-error-codes--0-499-
        private const int ERROR_FILE_NOT_FOUND = 2;
        private const int ERROR_PATH_NOT_FOUND = 3;
        private const int ERROR_ACCESS_DENIED = 5;

        // https://learn.microsoft.com/en-us/windows/win32/secauthz/generic-access-rights
        private const int GENERIC_WRITE = 0x40000000;

        private const int FILE_SHARE_READ = 0x00000001;
        private const int FILE_SHARE_WRITE = 0x00000002;
        private const int FILE_SHARE_DELETE = 0x00000004;
        private const int FILE_FLAG_BACKUP_SEMANTICS = 0x02000000;
        private const int OPEN_EXISTING = 3;

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

                    throw error switch {
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
