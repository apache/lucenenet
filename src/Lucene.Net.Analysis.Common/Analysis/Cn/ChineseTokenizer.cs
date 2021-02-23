// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.TokenAttributes;
using System;
using System.Globalization;
using System.IO;

namespace Lucene.Net.Analysis.Cn
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

    /// <summary>
    /// Tokenize Chinese text as individual chinese characters.
    /// 
    /// <para>
    /// The difference between <see cref="ChineseTokenizer"/> and
    /// <see cref="Cjk.CJKTokenizer"/> is that they have different
    /// token parsing logic.
    /// </para>
    /// <para>
    /// For example, if the Chinese text
    /// "C1C2C3C4" is to be indexed:
    /// <list type="bullet">
    ///     <item><description>The tokens returned from ChineseTokenizer are C1, C2, C3, C4.</description></item>
    ///     <item><description>The tokens returned from the CJKTokenizer are C1C2, C2C3, C3C4.</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// Therefore the index created by <see cref="Cjk.CJKTokenizer"/> is much larger.
    /// </para>
    /// <para>
    /// The problem is that when searching for C1, C1C2, C1C3,
    /// C4C2, C1C2C3 ... the <see cref="ChineseTokenizer"/> works, but the
    /// <see cref="Cjk.CJKTokenizer"/> will not work.
    /// </para> 
    /// </summary>
    /// @deprecated (3.1) Use <see cref="Standard.StandardTokenizer"/> instead, which has the same functionality.
    /// This filter will be removed in Lucene 5.0 
    [Obsolete("(3.1) Use StandardTokenizer instead, which has the same functionality.")]
    public sealed class ChineseTokenizer : Tokenizer
    {
        public ChineseTokenizer(TextReader @in)
            : base(@in)
        {
            Init();
        }

        public ChineseTokenizer(AttributeFactory factory, TextReader @in)
            : base(factory, @in)
        {
            Init();
        }

        private void Init()
        {
            termAtt = AddAttribute<ICharTermAttribute>();
            offsetAtt = AddAttribute<IOffsetAttribute>();
        }

        private int offset = 0, bufferIndex = 0, dataLen = 0;
        private const int MAX_WORD_LEN = 255;
        private const int IO_BUFFER_SIZE = 1024;
        private readonly char[] buffer = new char[MAX_WORD_LEN];
        private readonly char[] ioBuffer = new char[IO_BUFFER_SIZE];


        private int length;
        private int start;

        private ICharTermAttribute termAtt;
        private IOffsetAttribute offsetAtt;

        private void Push(char c)
        {
            if (length == 0) // start of token
            {
                start = offset - 1;
            }
            buffer[length++] = char.ToLowerInvariant(c); // buffer it

        }

        private bool Flush()
        {
            if (length > 0)
            {
                //System.out.println(new String(buffer, 0,
                //length));
                termAtt.CopyBuffer(buffer, 0, length);
                offsetAtt.SetOffset(CorrectOffset(start), CorrectOffset(start + length));
                return true;
            }
            else
            {
                return false;
            }
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
                    dataLen = m_input.Read(ioBuffer, 0, ioBuffer.Length);
                    bufferIndex = 0;
                }

                if (dataLen <= 0)
                {
                    offset--;
                    return Flush();
                }
                else
                {
                    c = ioBuffer[bufferIndex++];
                }

                switch (CharUnicodeInfo.GetUnicodeCategory(c))
                {

                    case UnicodeCategory.DecimalDigitNumber:
                    case UnicodeCategory.LowercaseLetter:
                    case UnicodeCategory.UppercaseLetter:
                        Push(c);
                        if (length == MAX_WORD_LEN)
                        {
                            return Flush();
                        }
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
                        if (length > 0)
                        {
                            return Flush();
                        }
                        break;
                }
            }
        }

        public override sealed void End()
        {
            base.End();
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