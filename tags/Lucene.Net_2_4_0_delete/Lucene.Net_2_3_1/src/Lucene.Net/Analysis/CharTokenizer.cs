/*
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 * 
 * http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;

namespace Lucene.Net.Analysis
{
	
    /// <summary>An abstract base class for simple, character-oriented tokenizers.</summary>
    public abstract class CharTokenizer : Tokenizer
    {
        public CharTokenizer(System.IO.TextReader input) : base(input)
        {
        }
		
        private int offset = 0, bufferIndex = 0, dataLen = 0;
        private const int MAX_WORD_LEN = 255;
        private const int IO_BUFFER_SIZE = 1024;
        private char[] ioBuffer = new char[IO_BUFFER_SIZE];
		
        /// <summary>Returns true iff a character should be included in a token.  This
        /// tokenizer generates as tokens adjacent sequences of characters which
        /// satisfy this predicate.  Characters for which this is false are used to
        /// define token boundaries and are not included in tokens. 
        /// </summary>
        protected internal abstract bool IsTokenChar(char c);
		
        /// <summary>Called on each token character to normalize it before it is added to the
        /// token.  The default implementation does nothing.  Subclasses may use this
        /// to, e.g., lowercase tokens. 
        /// </summary>
        protected internal virtual char Normalize(char c)
        {
            return c;
        }

        public override Token Next(Token token)
        {
            token.Clear();
            int length = 0;
            int start = bufferIndex;
            char[] buffer = token.TermBuffer();
            while (true)
            {

                if (bufferIndex >= dataLen)
                {
                    offset += dataLen;
                    dataLen = input is Lucene.Net.Index.DocumentsWriter.ReusableStringReader ? ((Lucene.Net.Index.DocumentsWriter.ReusableStringReader) input).Read(ioBuffer) : input.Read((System.Char[]) ioBuffer, 0, ioBuffer.Length);
                    if (dataLen <= 0)
                    {
                        if (length > 0)
                            break;
                        else
                            return null;
                    }
                    bufferIndex = 0;
                }

                char c = ioBuffer[bufferIndex++];

                if (IsTokenChar(c))
                {
                    // if it's a token char

                    if (length == 0)
                        // start of token
                        start = offset + bufferIndex - 1;
                    else if (length == buffer.Length)
                        buffer = token.ResizeTermBuffer(1 + length);

                    buffer[length++] = Normalize(c); // buffer it, normalized

                    if (length == MAX_WORD_LEN)
                        // buffer overflow!
                        break;
                }
                else if (length > 0)
                    // at non-Letter w/ chars
                    break; // return 'em
            }

            token.termLength = length;
            token.startOffset = start;
            token.endOffset = start + length;
            return token;
        }

        public override void Reset(System.IO.TextReader input)
        {
            base.Reset(input);
            bufferIndex = 0;
            offset = 0;
            dataLen = 0;
        }
    }
}