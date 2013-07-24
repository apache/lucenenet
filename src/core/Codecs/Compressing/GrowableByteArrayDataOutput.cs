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

/**
 * A {@link DataOutput} that can be used to build a byte[].
 */
using Lucene.Net.Store;
using Lucene.Net.Util;
using System;
namespace Lucene.Net.Codecs.Compressing
{
    internal sealed class GrowableByteArrayDataOutput : DataOutput
    {
        private sbyte[] bytes;
        private int length;

        public GrowableByteArrayDataOutput(int cp)
        {
            Bytes = new sbyte[ArrayUtil.Oversize(cp, 1)];
            Length = 0;
        }

        public sbyte[] Bytes
        {
            get
            {
                return bytes;
            }
            set
            {
                bytes = value;
            }
        }

        public int Length
        {
            get
            {
                return length;
            }
            set
            {
                length = value;
            }
        }

        public override void WriteByte(byte b)
        {
            if (length >= bytes.Length)
            {
                bytes = ArrayUtil.Grow(bytes);
            }
            bytes[length++] = (sbyte)b;
        }

        public override void WriteBytes(byte[] b, int off, int len)
        {
            int newLength = length + len;
            if (newLength > bytes.Length)
            {
                bytes = ArrayUtil.Grow(bytes, newLength);
            }

            Array.Copy(b, off, bytes, length, len);
            length = newLength;
        }

    }
}