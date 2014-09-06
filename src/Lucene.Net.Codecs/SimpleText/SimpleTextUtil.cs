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

namespace Lucene.Net.Codecs.SimpleText
{

    using System;
    using Lucene.Net.Index;
    using Lucene.Net.Store;
    using Lucene.Net.Util;

    public static class SimpleTextUtil
    {
        public const byte NEWLINE = 10;
        public const byte ESCAPE = 92;
        public static BytesRef CHECKSUM = new BytesRef("checksum ");

        public static void Write(DataOutput output, String s, BytesRef scratch)
        {
            UnicodeUtil.UTF16toUTF8(s, 0, s.Length, scratch);
            Write(output, scratch);
        }

        public static void Write(DataOutput output, BytesRef b)
        {
            for (int i = 0; i < b.Length; i++)
            {
                sbyte bx = b.Bytes[b.Offset + i];
                if (bx == NEWLINE || bx == ESCAPE)
                {
                    output.WriteByte(ESCAPE);
                }
                output.WriteByte(bx);
            }
        }

        public static void WriteNewline(DataOutput output)
        {
            output.WriteByte(NEWLINE);
        }

        public static void ReadLine(DataInput input, BytesRef scratch)
        {
            int upto = 0;
            while (true)
            {
                byte b = input.ReadByte();
                if (scratch.Bytes.Length == upto)
                {
                    scratch.Grow(1 + upto);
                }
                if (b == ESCAPE)
                {
                    scratch.Bytes[upto++] = input.ReadByte();
                }
                else
                {
                    if (b == NEWLINE)
                    {
                        break;
                    }
                    else
                    {
                        scratch.Bytes[upto++] = b;
                    }
                }
            }
            scratch.Offset = 0;
            scratch.Length = upto;
        }

        public static void WriteChecksum(IndexOutput output, BytesRef scratch)
        {
            // Pad with zeros so different checksum values use the
            // same number of bytes
            // (BaseIndexFileFormatTestCase.testMergeStability cares):
            String checksum = String.Format(Locale.ROOT, "%020d", output.Checksum);
            SimpleTextUtil.Write(output, CHECKSUM);
            SimpleTextUtil.Write(output, checksum, scratch);
            SimpleTextUtil.WriteNewline(output);
        }

        public static void CheckFooter(ChecksumIndexInput input)
        {
            BytesRef scratch = new BytesRef();
            String expectedChecksum = String.Format(Locale.ROOT, "%020d", input.Checksum);
            SimpleTextUtil.ReadLine(input, scratch);
            if (StringHelper.StartsWith(scratch, CHECKSUM) == false)
            {
                throw new CorruptIndexException("SimpleText failure: expected checksum line but got " +
                                                scratch.Utf8ToString() + " (resource=" + input + ")");
            }
            String actualChecksum =
                new BytesRef(scratch.Bytes, CHECKSUM.Length, scratch.Length - CHECKSUM.Length).Utf8ToString();
            if (!expectedChecksum.Equals(actualChecksum))
            {
                throw new CorruptIndexException("SimpleText checksum failure: " + actualChecksum + " != " +
                                                expectedChecksum +
                                                " (resource=" + input + ")");
            }
            if (input.Length() != input.FilePointer)
            {
                throw new CorruptIndexException(
                    "Unexpected stuff at the end of file, please be careful with your text editor! (resource=" + input +
                    ")");
            }
        }
    }
}