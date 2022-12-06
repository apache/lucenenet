// Lucene version compatibility level 8.6.1
using ICU4N;
using ICU4N.Text;
using Lucene.Net.Analysis.Icu.TokenAttributes;
using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Diagnostics;
using Lucene.Net.Support;
using Lucene.Net.Support.Threading;
using System;
using System.Diagnostics;
using System.IO;

namespace Lucene.Net.Analysis.Icu.Segmentation
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
    /// Breaks text into words according to UAX #29: Unicode Text Segmentation
    /// (http://www.unicode.org/reports/tr29/)
    /// <para/>
    /// Words are broken across script boundaries, then segmented according to
    /// the BreakIterator and typing provided by the <see cref="ICUTokenizerConfig"/>
    /// <para/>
    /// @lucene.experimental
    /// </summary>
    /// <seealso cref="ICUTokenizerConfig"/>
    [ExceptionToClassNameConvention]
    public sealed class ICUTokenizer : Tokenizer
    {
        private const int IOBUFFER = 4096;
        private readonly char[] buffer = new char[IOBUFFER];
        /// <summary>true length of text in the buffer</summary>
        private int length = 0;
        /// <summary>length in buffer that can be evaluated safely, up to a safe end point</summary>
        private int usableLength = 0;
        /// <summary>accumulated offset of previous buffers for this reader, for offsetAtt</summary>
        private int offset = 0;

        private readonly CompositeBreakIterator breaker; /* tokenizes a char[] of text */
        private readonly ICUTokenizerConfig config;
        private readonly IOffsetAttribute offsetAtt;
        private readonly ICharTermAttribute termAtt;
        private readonly ITypeAttribute typeAtt;
        private readonly IScriptAttribute scriptAtt;

        private static readonly object syncLock = new object(); // LUCENENET specific - workaround until BreakIterator is made thread safe (LUCENENET TODO: TO REVERT)

        /// <summary>
        /// Construct a new <see cref="ICUTokenizer"/> that breaks text into words from the given
        /// <see cref="TextReader"/>.
        /// </summary>
        /// <remarks>
        /// The default script-specific handling is used.
        /// <para/>
        /// The default attribute factory is used.
        /// </remarks>
        /// <param name="input"><see cref="TextReader"/> containing text to tokenize.</param>
        /// <seealso cref="DefaultICUTokenizerConfig"/>
        public ICUTokenizer(TextReader input)
            : this(input, new DefaultICUTokenizerConfig(true, true))
        {
        }

        /// <summary>
        /// Construct a new <see cref="ICUTokenizer"/> that breaks text into words from the given
        /// <see cref="TextReader"/>, using a tailored <see cref="BreakIterator"/> configuration.
        /// </summary>
        /// <remarks>
        /// The default attribute factory is used.
        /// </remarks>
        /// <param name="input"><see cref="TextReader"/> containing text to tokenize.</param>
        /// <param name="config">Tailored <see cref="BreakIterator"/> configuration.</param>
        public ICUTokenizer(TextReader input, ICUTokenizerConfig config)
            : this(AttributeFactory.DEFAULT_ATTRIBUTE_FACTORY, input, config)
        {
        }

        /// <summary>
        /// Construct a new <see cref="ICUTokenizer"/> that breaks text into words from the given
        /// <see cref="TextReader"/>, using a tailored <see cref="BreakIterator"/> configuration.
        /// </summary>
        /// <param name="factory"><see cref="Lucene.Net.Util.AttributeSource.AttributeFactory"/> to use.</param>
        /// <param name="input"><see cref="TextReader"/> containing text to tokenize.</param>
        /// <param name="config">Tailored <see cref="BreakIterator"/> configuration.</param>
        public ICUTokenizer(AttributeFactory factory, TextReader input, ICUTokenizerConfig config)
            : base(factory, input)
        {
            this.offsetAtt = AddAttribute<IOffsetAttribute>();
            this.termAtt = AddAttribute<ICharTermAttribute>();
            this.typeAtt = AddAttribute<ITypeAttribute>();
            this.scriptAtt = AddAttribute<IScriptAttribute>();

            this.config = config;
            breaker = new CompositeBreakIterator(config);
        }


        public override bool IncrementToken()
        {
            UninterruptableMonitor.Enter(syncLock);
            try
            {
                ClearAttributes();
                if (length == 0)
                    Refill();
                while (!IncrementTokenBuffer())
                {
                    Refill();
                    if (length <= 0) // no more bytes to read;
                        return false;
                }
                return true;
            }
            finally
            {
                UninterruptableMonitor.Exit(syncLock);
            }
        }


        public override void Reset()
        {
            base.Reset();
            UninterruptableMonitor.Enter(syncLock);
            try
            {
                breaker.SetText(buffer, 0, 0);
            }
            finally
            {
                UninterruptableMonitor.Exit(syncLock);
            }
            length = usableLength = offset = 0;
        }

        public override void End()
        {
            base.End();
            int finalOffset = (length < 0) ? offset : offset + length;
            offsetAtt.SetOffset(CorrectOffset(finalOffset), CorrectOffset(finalOffset));
        }

        /*
         * This tokenizes text based upon the longest matching rule, and because of 
         * this, isn't friendly to a Reader.
         * 
         * Text is read from the input stream in 4kB chunks. Within a 4kB chunk of
         * text, the last unambiguous break point is found (in this implementation:
         * white space character) Any remaining characters represent possible partial
         * words, so are appended to the front of the next chunk.
         * 
         * There is the possibility that there are no unambiguous break points within
         * an entire 4kB chunk of text (binary data). So there is a maximum word limit
         * of 4kB since it will not try to grow the buffer in this case.
         */

        /// <summary>
        /// Returns the last unambiguous break position in the text.
        /// </summary>
        /// <returns>Position of character, or -1 if one does not exist.</returns>
        private int FindSafeEnd()
        {
            for (int i = length - 1; i >= 0; i--)
                if (UChar.IsWhiteSpace(buffer[i]))
                    return i + 1;
            return -1;
        }

        /// <summary>
        /// Refill the buffer, accumulating the offset and setting usableLength to the
        /// last unambiguous break position.
        /// </summary>
        /// <exception cref="IOException">If there is a low-level I/O error.</exception>
        private void Refill()
        {
            offset += usableLength;
            int leftover = length - usableLength;
            Arrays.Copy(buffer, usableLength, buffer, 0, leftover);
            int requested = buffer.Length - leftover;
            int returned = Read(m_input, buffer, leftover, requested);
            length = returned + leftover;
            if (returned < requested) /* reader has been emptied, process the rest */
                usableLength = length;
            else
            { /* still more data to be read, find a safe-stopping place */
                usableLength = FindSafeEnd();
                if (usableLength < 0)
                    usableLength = length; /*
                                * more than IOBUFFER of text without space,
                                * gonna possibly truncate tokens
                                */
            }

            UninterruptableMonitor.Enter(syncLock);
            try
            {
                breaker.SetText(buffer, 0, Math.Max(0, usableLength));
            }
            finally
            {
                UninterruptableMonitor.Exit(syncLock);
            }
        }

        // TODO: refactor to a shared readFully somewhere
        // (NGramTokenizer does this too):
        /// <summary>commons-io's readFully, but without bugs if offset != 0</summary>
        private static int Read(TextReader input, char[] buffer, int offset, int length)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(length >= 0, "length must not be negative: {0}", length);

            int remaining = length;
            while (remaining > 0)
            {
                int location = length - remaining;
                int count = input.Read(buffer, offset + location, remaining);
                if (count <= 0)
                { // EOF
                    break;
                }
                remaining -= count;
            }
            return length - remaining;
        }

        /// <summary>
        /// Returns <c>true</c> if there is a token from the buffer, or <c>false</c> if it is exhausted.
        /// </summary>
        /// <returns><c>true</c> if there is a token from the buffer, or <c>false</c> if it is exhausted.</returns>
        private bool IncrementTokenBuffer()
        {
            int start = breaker.Current;
            if (start == BreakIterator.Done)
                return false; // BreakIterator exhausted

            // find the next set of boundaries, skipping over non-tokens (rule status 0)
            int end = breaker.Next();
            while (end != BreakIterator.Done && breaker.RuleStatus == 0)
            {
                start = end;
                end = breaker.Next();
            }

            if (end == BreakIterator.Done)
                return false; // BreakIterator exhausted

            termAtt.CopyBuffer(buffer, start, end - start);
            offsetAtt.SetOffset(CorrectOffset(offset + start), CorrectOffset(offset + end));
            typeAtt.Type = config.GetType(breaker.ScriptCode, breaker.RuleStatus);
            scriptAtt.Code = breaker.ScriptCode;
            
            return true;
        }
    }
}
