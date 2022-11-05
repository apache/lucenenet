// Lucene version compatibility level 4.8.1
using J2N;
using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Diagnostics;
using Lucene.Net.Util;
using System.IO;

namespace Lucene.Net.Analysis.Util
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
    /// An abstract base class for simple, character-oriented tokenizers. 
    /// <para>
    /// You must specify the required <see cref="LuceneVersion"/> compatibility
    /// when creating <see cref="CharTokenizer"/>:
    /// <list type="bullet">
    ///     <item><description>As of 3.1, <see cref="CharTokenizer"/> uses an int based API to normalize and
    ///         detect token codepoints. See <see cref="IsTokenChar(int)"/> and
    ///         <see cref="Normalize(int)"/> for details.</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// A new <see cref="CharTokenizer"/> API has been introduced with Lucene 3.1. This API
    /// moved from UTF-16 code units to UTF-32 codepoints to eventually add support
    /// for <a href=
    /// "http://java.sun.com/j2se/1.5.0/docs/api/java/lang/Character.html#supplementary"
    /// >supplementary characters</a>. The old <i>char</i> based API has been
    /// deprecated and should be replaced with the <i>int</i> based methods
    /// <see cref="IsTokenChar(int)"/> and <see cref="Normalize(int)"/>.
    /// </para>
    /// <para>
    /// As of Lucene 3.1 each <see cref="CharTokenizer"/> - constructor expects a
    /// <see cref="LuceneVersion"/> argument. Based on the given <see cref="LuceneVersion"/> either the new
    /// API or a backwards compatibility layer is used at runtime. For
    /// <see cref="LuceneVersion"/> &lt; 3.1 the backwards compatibility layer ensures correct
    /// behavior even for indexes build with previous versions of Lucene. If a
    /// <see cref="LuceneVersion"/> >= 3.1 is used <see cref="CharTokenizer"/> requires the new API to
    /// be implemented by the instantiated class. Yet, the old <i>char</i> based API
    /// is not required anymore even if backwards compatibility must be preserved.
    /// <see cref="CharTokenizer"/> subclasses implementing the new API are fully backwards
    /// compatible if instantiated with <see cref="LuceneVersion"/> &lt; 3.1.
    /// </para>
    /// <para>
    /// <strong>Note:</strong> If you use a subclass of <see cref="CharTokenizer"/> with <see cref="LuceneVersion"/> >=
    /// 3.1 on an index build with a version &lt; 3.1, created tokens might not be
    /// compatible with the terms in your index.
    /// </para>
    /// </summary>
    public abstract class CharTokenizer : Tokenizer
    {
        /// <summary>
        /// Creates a new <see cref="CharTokenizer"/> instance
        /// </summary>
        /// <param name="matchVersion">
        ///          Lucene version to match </param>
        /// <param name="input">
        ///          the input to split up into tokens </param>
        protected CharTokenizer(LuceneVersion matchVersion, TextReader input) // LUCENENET: CA1012: Abstract types should not have constructors (marked protected)
            : base(input)
        {
            Init(matchVersion);
        }

        /// <summary>
        /// Creates a new <see cref="CharTokenizer"/> instance
        /// </summary>
        /// <param name="matchVersion">
        ///          Lucene version to match </param>
        /// <param name="factory">
        ///          the attribute factory to use for this <see cref="Tokenizer"/> </param>
        /// <param name="input">
        ///          the input to split up into tokens </param>
        protected CharTokenizer(LuceneVersion matchVersion, AttributeFactory factory, TextReader input) // LUCENENET: CA1012: Abstract types should not have constructors (marked protected)
            : base(factory, input)
        {
            Init(matchVersion);
        }

        /// <summary>
        /// LUCENENET specific - Added in the .NET version to assist with setting the attributes
        /// from multiple constructors.
        /// </summary>
        /// <param name="matchVersion"></param>
        private void Init(LuceneVersion matchVersion)
        {
            charUtils = CharacterUtils.GetInstance(matchVersion);
            termAtt = AddAttribute<ICharTermAttribute>();
            offsetAtt = AddAttribute<IOffsetAttribute>();
        }

        private int offset = 0, bufferIndex = 0, dataLen = 0, finalOffset = 0;
        private const int MAX_WORD_LEN = 255;
        private const int IO_BUFFER_SIZE = 4096;

        private ICharTermAttribute termAtt;
        private IOffsetAttribute offsetAtt;

        private CharacterUtils charUtils;
        private readonly CharacterUtils.CharacterBuffer ioBuffer = CharacterUtils.NewCharacterBuffer(IO_BUFFER_SIZE);

        /// <summary>
        /// Returns true iff a codepoint should be included in a token. This tokenizer
        /// generates as tokens adjacent sequences of codepoints which satisfy this
        /// predicate. Codepoints for which this is false are used to define token
        /// boundaries and are not included in tokens.
        /// </summary>
        protected abstract bool IsTokenChar(int c);

        /// <summary>
        /// Called on each token character to normalize it before it is added to the
        /// token. The default implementation does nothing. Subclasses may use this to,
        /// e.g., lowercase tokens.
        /// </summary>
        protected virtual int Normalize(int c)
        {
            return c;
        }

        public override sealed bool IncrementToken()
        {
            ClearAttributes();
            int length = 0;
            int start = -1; // this variable is always initialized
            int end = -1;
            char[] buffer = termAtt.Buffer;
            while (true)
            {
                if (bufferIndex >= dataLen)
                {
                    offset += dataLen;
                    charUtils.Fill(ioBuffer, m_input); // read supplementary char aware with CharacterUtils
                    if (ioBuffer.Length == 0)
                    {
                        dataLen = 0; // so next offset += dataLen won't decrement offset
                        if (length > 0)
                        {
                            break;
                        }
                        else
                        {
                            finalOffset = CorrectOffset(offset);
                            return false;
                        }
                    }
                    dataLen = ioBuffer.Length;
                    bufferIndex = 0;
                }
                // use CharacterUtils here to support < 3.1 UTF-16 code unit behavior if the char based methods are gone
                int c = charUtils.CodePointAt(ioBuffer.Buffer, bufferIndex, ioBuffer.Length);
                int charCount = Character.CharCount(c);
                bufferIndex += charCount;

                if (IsTokenChar(c)) // if it's a token char
                {
                    if (length == 0) // start of token
                    {
                        if (Debugging.AssertsEnabled) Debugging.Assert(start == -1);
                        start = offset + bufferIndex - charCount;
                        end = start;
                    } // check if a supplementary could run out of bounds
                    else if (length >= buffer.Length - 1)
                    {
                        buffer = termAtt.ResizeBuffer(2 + length); // make sure a supplementary fits in the buffer
                    }
                    end += charCount;
                    length += Character.ToChars(Normalize(c), buffer, length); // buffer it, normalized
                    if (length >= MAX_WORD_LEN) // buffer overflow! make sure to check for >= surrogate pair could break == test
                    {
                        break;
                    }
                } // at non-Letter w/ chars
                else if (length > 0)
                {
                    break; // return 'em
                }
            }

            termAtt.Length = length;
            if (Debugging.AssertsEnabled) Debugging.Assert(start != -1);
            offsetAtt.SetOffset(CorrectOffset(start), finalOffset = CorrectOffset(end));
            return true;
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
            bufferIndex = 0;
            offset = 0;
            dataLen = 0;
            finalOffset = 0;
            ioBuffer.Reset(); // make sure to reset the IO buffer!!
        }
    }
}