using System;

namespace Lucene.Net.Support.Native
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

    internal static partial class Interop
    {
        internal static partial class Win32
        {
            internal static readonly IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);

            // https://learn.microsoft.com/en-us/windows/win32/debug/system-error-codes--0-499-
            internal const int ERROR_FILE_NOT_FOUND = 2;
            internal const int ERROR_PATH_NOT_FOUND = 3;
            internal const int ERROR_ACCESS_DENIED = 5;

            // https://learn.microsoft.com/en-us/windows/win32/secauthz/generic-access-rights
            internal const int GENERIC_WRITE = 0x40000000;

            internal const int FILE_SHARE_READ = 0x00000001;
            internal const int FILE_SHARE_WRITE = 0x00000002;
            internal const int FILE_SHARE_DELETE = 0x00000004;
            internal const int FILE_FLAG_BACKUP_SEMANTICS = 0x02000000;
            internal const int OPEN_EXISTING = 3;
        }
    }
}
