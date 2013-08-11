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
using System.IO;
using System.Text;
using System.Collections;
using System.Globalization;

using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Tokenattributes;
using Lucene.Net.Util;

namespace Lucene.Net.Analysis.Cn
{
    /// <summary>
    /// <para>
    /// Tokenize Chinese text as individual chinese chars.
    /// </para>
    /// <para>
    /// The difference between ChineseTokenizer and
    /// CJKTokenizer is that they have different
    /// token parsing logic.
    /// </para>
    /// <para>
    /// For example, if the Chinese text
    /// "C1C2C3C4" is to be indexed:
    /// <list type="bullet">
    /// <item><description>The tokens returned from ChineseTokenizer are C1, C2, C3, C4</description></item>
    /// <item><description>The tokens returned from the CJKTokenizer are C1C2, C2C3, C3C4.</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// Therefore the index created by CJKTokenizer is much larger.
    /// </para>
    /// <para>
    /// The problem is that when searching for C1, C1C2, C1C3,
    /// C4C2, C1C2C3 ... the ChineseTokenizer works, but the
    /// CJKTokenizer will not work.
    /// </para>
    /// </summary>
    [Obsolete("(3.1) Use {Lucene.Net.Analysis.Standard.StandardTokenizer} instead, which has the same functionality. This filter will be removed in Lucene 5.0")]
    public sealed class ChineseTokenizer : Tokenizer
    {
        public ChineseTokenizer(TextReader _in)
            : base(_in)
        {
            termAtt = AddAttribute<ICharTermAttribute>();
            offsetAtt = AddAttribute<IOffsetAttribute>();
        }

        public ChineseTokenizer(AttributeFactory factory, TextReader _in)
            : base(factory, _in)
        {
            termAtt = AddAttribute<ICharTermAttribute>();
            offsetAtt = AddAttribute<IOffsetAttribute>();
        }

        private int offset = 0, bufferIndex = 0, dataLen = 0;
        private static readonly int MAX_WORD_LEN = 255;
        private static readonly int IO_BUFFER_SIZE = 1024;
        private readonly char[] buffer = new char[MAX_WORD_LEN];
        private readonly char[] ioBuffer = new char[IO_BUFFER_SIZE];

        private int length;
        private int start;

        private readonly ICharTermAttribute termAtt;
        private readonly IOffsetAttribute offsetAtt;

        private void Push(char c)
        {
            if (length == 0) start = offset - 1; // start of token
            buffer[length++] = Char.ToLower(c); // buffer it
        }

        private bool Flush()
        {
            if (length > 0)
            {
                termAtt.CopyBuffer(buffer, 0, length);
                offsetAtt.SetOffset(CorrectOffset(start), CorrectOffset(start + length));
                return true;
            }
            else
                return false;
        }

        public override bool IncrementToken()
        {
            ClearAttributes();

            length = 0;
            start = offset;


            while (true)
            {
                char c;
                offset++;

                if (bufferIndex >= dataLen)
                {
                    dataLen = input.Read(ioBuffer, 0, ioBuffer.Length);
                    bufferIndex = 0;
                }

                if (dataLen == 0)
                {
                    offset--;
                    return Flush();
                }
                else
                    c = ioBuffer[bufferIndex++];


                switch (char.GetUnicodeCategory(c))
                {

                    case UnicodeCategory.DecimalDigitNumber:
                    case UnicodeCategory.LowercaseLetter:
                    case UnicodeCategory.UppercaseLetter:
                        Push(c);
                        if (length == MAX_WORD_LEN) return Flush();
                        break;

                    case UnicodeCategory.OtherLetter:
                        if (length > 0)
                        {
                            bufferIndex--;
                            offset--;
                            return Flush();
                        }
                        Push(c);
                        return Flush();

                    default:
                        if (length > 0) return Flush();
                        break;
                }
            }
        }

        public override sealed void End()
        {
            // set final offset
            int finalOffset = CorrectOffset(offset);
            this.offsetAtt.SetOffset(finalOffset, finalOffset);
        }

        public override void Reset()
        {
            base.Reset();
            offset = bufferIndex = dataLen = 0;
        }
    }
}
