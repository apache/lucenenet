using System.Runtime.InteropServices;

namespace Lucene.Net.Native
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
        internal static partial class Posix
        {
            // https://pubs.opengroup.org/onlinepubs/9699919799/functions/readlink.html
            // Takes pointers rather than string/byte[] so that no marshaling buffers are allocated per call.
            // path must be null-terminated; buf is not null-terminated by readlink().
            [DllImport("libc", SetLastError = true)]
            internal static extern unsafe nint readlink(byte* path, byte* buf, nuint bufsize);
        }
    }
}
