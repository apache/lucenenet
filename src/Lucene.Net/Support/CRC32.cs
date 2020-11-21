/*
 *
 * Licensed to the Apache Software Foundation (ASF) under one
 * or more contributor license agreements.  See the NOTICE file
 * distributed with this work for additional information
 * regarding copyright ownership.  The ASF licenses this file
 * to you under the Apache License, Version 2.0 (the
 * "License"); you may not use this file except in compliance
 * with the License.  You may obtain a copy of the License at
 *
 *   http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing,
 * software distributed under the License is distributed on an
 * "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
 * KIND, either express or implied.  See the License for the
 * specific language governing permissions and limitations
 * under the License.
 *
*/

using System;

namespace Lucene.Net.Support
{
    internal class CRC32 : IChecksum
    {
        private static readonly uint[] crcTable = InitializeCRCTable();

        private static uint[] InitializeCRCTable()
        {
            uint[] crcTable = new uint[256];
            for (uint n = 0; n < 256; n++)
            {
                uint c = n;
                for (int k = 8; --k >= 0; )
                {
                    if ((c & 1) != 0)
                        c = 0xedb88320 ^ (c >> 1);
                    else
                        c = c >> 1;
                }
                crcTable[n] = c;
            }
            return crcTable;
        }

        private uint crc = 0;

        public long Value => crc & 0xffffffffL;

        public void Reset()
        {
            crc = 0;
        }

        public void Update(int bval)
        {
            uint c = ~crc;
            c = crcTable[(c ^ bval) & 0xff] ^ (c >> 8);
            crc = ~c;
        }

        public void Update(byte[] buf, int off, int len)
        {
            uint c = ~crc;
            while (--len >= 0)
                c = crcTable[(c ^ buf[off++]) & 0xff] ^ (c >> 8);
            crc = ~c;
        }

        public void Update(byte[] buf)
        {
            Update(buf, 0, buf.Length);
        }
    }
}