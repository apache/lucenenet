// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Util;
using System;
using System.IO;

namespace Lucene.Net.Analysis.Core
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
    /// Emits the entire input as a single token.
    /// </summary>
    public sealed class KeywordTokenizer : Tokenizer
    {
        /// <summary>
        /// Default read buffer size </summary>
        public const int DEFAULT_BUFFER_SIZE = 256;

        private bool done = false;
        private int finalOffset;
        private readonly ICharTermAttribute termAtt;
        private readonly IOffsetAttribute offsetAtt;

        public KeywordTokenizer(TextReader input)
            : this(input, DEFAULT_BUFFER_SIZE)
        {
        }

        public KeywordTokenizer(TextReader input, int bufferSize)
            : base(input)
        {
            termAtt = AddAttribute<ICharTermAttribute>();
            offsetAtt = AddAttribute<IOffsetAttribute>();

            if (bufferSize <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(bufferSize), "bufferSize must be > 0"); // LUCENENET specific - changed from IllegalArgumentException to ArgumentOutOfRangeException (.NET convention)
            }
            termAtt.ResizeBuffer(bufferSize);
        }

        public KeywordTokenizer(AttributeSource.AttributeFactory factory, TextReader input, int bufferSize)
            : base(factory, input)
        {
            termAtt = AddAttribute<ICharTermAttribute>();
            offsetAtt = AddAttribute<IOffsetAttribute>();

            if (bufferSize <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(bufferSize), "bufferSize must be > 0"); // LUCENENET specific - changed from IllegalArgumentException to ArgumentOutOfRangeException (.NET convention)
            }
            termAtt.ResizeBuffer(bufferSize);
        }

        public override sealed bool IncrementToken()
        {
            if (!done)
            {
                ClearAttributes();
                done = true;
                int upto = 0;
                char[] buffer = termAtt.Buffer;
                while (true)
                {
                    int length = m_input.Read(buffer, upto, buffer.Length - upto);
                    if (length <= 0)
                    {
                        break;
                    }
                    upto += length;
                    if (upto == buffer.Length)
                    {
                        buffer = termAtt.ResizeBuffer(1 + buffer.Length);
                    }
                }
                termAtt.Length = upto;
                finalOffset = CorrectOffset(upto);
                offsetAtt.SetOffset(CorrectOffset(0), finalOffset);
                return true;
            }
            return false;
        }

        public override sealed void End()
        {
            base.End();
            // set final offset 
            offsetAtt.SetOffset(finalOffset, finalOffset);
        }

        public override void Reset()
        {
            base.Reset();
            this.done = false;
        }
    }
}