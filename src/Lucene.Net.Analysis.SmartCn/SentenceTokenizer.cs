// lucene version compatibility level: 4.8.1
using Lucene.Net.Analysis.TokenAttributes;
using System;
using System.IO;
using System.Text;

namespace Lucene.Net.Analysis.Cn.Smart
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
    /// Tokenizes input text into sentences.
    /// <para>
    /// The output tokens can then be broken into words with <see cref="WordTokenFilter"/>
    /// </para>
    /// @lucene.experimental
    /// </summary>
    [Obsolete("Use HMMChineseTokenizer instead")]
    public sealed class SentenceTokenizer : Tokenizer
    {
        /// <summary>
        /// End of sentence punctuation: 。，！？；,!?;
        /// </summary>
        private const string PUNCTION = "。，！？；,!?;";

        private readonly StringBuilder buffer = new StringBuilder();

        private int tokenStart = 0, tokenEnd = 0;

        private ICharTermAttribute termAtt;
        private IOffsetAttribute offsetAtt;
        private ITypeAttribute typeAtt;

        public SentenceTokenizer(TextReader reader)
                  : base(reader)
        {
            Init();
        }

        public SentenceTokenizer(AttributeFactory factory, TextReader reader)
            : base(factory, reader)
        {
            Init();
        }

        private void Init()
        {
            termAtt = AddAttribute<ICharTermAttribute>();
            offsetAtt = AddAttribute<IOffsetAttribute>();
            typeAtt = AddAttribute<ITypeAttribute>();
        }


        public override bool IncrementToken()
        {
            ClearAttributes();
            buffer.Length = 0;
            int ci;
            char ch, pch;
            bool atBegin = true;
            tokenStart = tokenEnd;
            ci = m_input.Read();
            ch = (char)ci;

            while (true)
            {
                if (ci == -1)
                {
                    break;
                }
                else if (PUNCTION.IndexOf(ch) != -1)
                {
                    // End of a sentence
                    buffer.Append(ch);
                    tokenEnd++;
                    break;
                }
                else if (atBegin && Utility.SPACES.IndexOf(ch) != -1)
                {
                    tokenStart++;
                    tokenEnd++;
                    ci = m_input.Read();
                    ch = (char)ci;
                }
                else
                {
                    buffer.Append(ch);
                    atBegin = false;
                    tokenEnd++;
                    pch = ch;
                    ci = m_input.Read();
                    ch = (char)ci;
                    // Two spaces, such as CR, LF
                    if (Utility.SPACES.IndexOf(ch) != -1
                        && Utility.SPACES.IndexOf(pch) != -1)
                    {
                        // buffer.append(ch);
                        tokenEnd++;
                        break;
                    }
                }
            }
            if (buffer.Length == 0)
                return false;
            else
            {
                termAtt.SetEmpty().Append(buffer);
                offsetAtt.SetOffset(CorrectOffset(tokenStart), CorrectOffset(tokenEnd));
                typeAtt.Type = "sentence";
                return true;
            }
        }

        public override void Reset()
        {
            base.Reset();
            tokenStart = tokenEnd = 0;
        }

        public override void End()
        {
            base.End();
            // set final offset
            int finalOffset = CorrectOffset(tokenEnd);
            offsetAtt.SetOffset(finalOffset, finalOffset);
        }
    }
}
