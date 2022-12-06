using System;
using System.Globalization;

namespace Lucene.Net.Codecs.SimpleText
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

    using CorruptIndexException = Index.CorruptIndexException;
    using ChecksumIndexInput = Store.ChecksumIndexInput;
    using DataInput = Store.DataInput;
    using DataOutput = Store.DataOutput;
    using IndexOutput = Store.IndexOutput;
    using BytesRef = Util.BytesRef;
    using StringHelper = Util.StringHelper;
    using UnicodeUtil = Util.UnicodeUtil;

    internal class SimpleTextUtil
    {
        public const byte NEWLINE = 10;
        public const byte ESCAPE = 92;
        internal static readonly BytesRef CHECKSUM = new BytesRef("checksum ");

        public static void Write(DataOutput output, string s, BytesRef scratch)
        {
            UnicodeUtil.UTF16toUTF8(s, 0, s.Length, scratch);
            Write(output, scratch);
        }

        public static void Write(DataOutput output, BytesRef b)
        {
            for (var i = 0; i < b.Length; i++)
            {
                var bx = b.Bytes[b.Offset + i];
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
            var upto = 0;
            while (true)
            {
                var b = input.ReadByte();
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
                    
                    scratch.Bytes[upto++] = b;
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
            var checksum = string.Format(CultureInfo.InvariantCulture, "{0:D20}", output.Checksum);
            Write(output, CHECKSUM);
            Write(output, checksum, scratch);
            WriteNewline(output);
        }

        public static void CheckFooter(ChecksumIndexInput input)
        {
            var scratch = new BytesRef();
            var expectedChecksum = string.Format(CultureInfo.InvariantCulture, "{0:D20}", input.Checksum);
            ReadLine(input, scratch);

            if (StringHelper.StartsWith(scratch, CHECKSUM) == false)
            {
                throw new CorruptIndexException("SimpleText failure: expected checksum line but got " +
                                                scratch.Utf8ToString() + " (resource=" + input + ")");
            }
            var actualChecksum =
                (new BytesRef(scratch.Bytes, CHECKSUM.Length, scratch.Length - CHECKSUM.Length)).Utf8ToString();
            if (!expectedChecksum.Equals(actualChecksum, StringComparison.Ordinal))
            {
                throw new CorruptIndexException("SimpleText checksum failure: " + actualChecksum + " != " +
                                                expectedChecksum + " (resource=" + input + ")");
            }
            if (input.Length != input.Position) // LUCENENET specific: Renamed from getFilePointer() to match FileStream
            {
                throw new CorruptIndexException(
                    "Unexpected stuff at the end of file, please be careful with your text editor! (resource=" + input +
                    ")");
            }
        }
    }
}