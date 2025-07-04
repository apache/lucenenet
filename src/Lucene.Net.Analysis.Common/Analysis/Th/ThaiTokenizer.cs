// Lucene version compatibility level 4.8.1
#if FEATURE_BREAKITERATOR
using ICU4N.Support.Text;
using ICU4N.Text;
using J2N;
using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Analysis.Util;
using Lucene.Net.Support;
using Lucene.Net.Support.Threading;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;

namespace Lucene.Net.Analysis.Th
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

    // LUCENENET NOTE: Removing this notice from the doc comment because it is not relevant for our purposes.

    // <para>WARNING: this tokenizer may not be supported by all JREs.
    //    It is known to work with Sun/Oracle and Harmony JREs.
    //    If your application needs to be fully portable, consider using ICUTokenizer instead,
    //    which uses an ICU Thai BreakIterator that will always be available.
    // </para>

    /// <summary>
    /// Tokenizer that use <see cref="BreakIterator"/> to tokenize Thai text.
    /// </summary>
    public class ThaiTokenizer : SegmentingTokenizerBase
    {
        // LUCENENET specific - DBBI_AVAILABLE removed because ICU always has a dictionary-based BreakIterator
        private static readonly BreakIterator proto = BreakIterator.GetWordInstance(new CultureInfo("th"));

        /// <summary>
        /// used for breaking the text into sentences
        /// </summary>
        private static readonly BreakIterator sentenceProto = BreakIterator.GetSentenceInstance(CultureInfo.InvariantCulture);

        private readonly ThaiWordBreaker wordBreaker;
        private readonly CharArrayIterator wrapper = CharArrayIterator.NewWordInstance();

        private int sentenceStart;
        private int sentenceEnd;

        private readonly ICharTermAttribute termAtt;
        private readonly IOffsetAttribute offsetAtt;

        /// <summary>
        /// Creates a new <see cref="ThaiTokenizer"/> </summary>
        public ThaiTokenizer(TextReader reader)
            : this(AttributeFactory.DEFAULT_ATTRIBUTE_FACTORY, reader)
        {
        }

        /// <summary>
        /// Creates a new <see cref="ThaiTokenizer"/>, supplying the <see cref="AttributeFactory"/> </summary>
        public ThaiTokenizer(AttributeFactory factory, TextReader reader)
            : base(factory, reader, (BreakIterator)sentenceProto.Clone())
        {
            // LUCENENET specific - DBBI_AVAILABLE removed because ICU always has a dictionary-based BreakIterator
            wordBreaker = new ThaiWordBreaker((BreakIterator)proto.Clone());
            termAtt = AddAttribute<ICharTermAttribute>();
            offsetAtt = AddAttribute<IOffsetAttribute>();
        }

        protected override void SetNextSentence(int sentenceStart, int sentenceEnd)
        {
            this.sentenceStart = sentenceStart;
            this.sentenceEnd = sentenceEnd;
            wrapper.SetText(m_buffer, sentenceStart, sentenceEnd - sentenceStart);
            wordBreaker.SetText(wrapper);
        }

        protected override bool IncrementWord()
        {
            int start, end;
            start = wordBreaker.Current;
            if (start == BreakIterator.Done)
            {
                return false; // BreakIterator exhausted
            }

            // find the next set of boundaries, skipping over non-tokens
            end = wordBreaker.Next();
            while (end != BreakIterator.Done && !Character.IsLetterOrDigit(Character.CodePointAt(m_buffer, sentenceStart + start, sentenceEnd)))
            {
                start = end;
                end = wordBreaker.Next();
            }

            if (end == BreakIterator.Done)
            {
                return false; // BreakIterator exhausted
            }

            ClearAttributes();
            termAtt.CopyBuffer(m_buffer, sentenceStart + start, end - start);
            offsetAtt.SetOffset(CorrectOffset(m_offset + sentenceStart + start), CorrectOffset(m_offset + sentenceStart + end));
            return true;
        }
    }

    /// <summary>
    /// LUCENENET specific class to patch the behavior of the ICU BreakIterator to match the behavior of the JDK.
    /// Corrects the breaking of words by finding transitions between Thai and non-Thai
    /// characters.
    /// </summary>
    internal class ThaiWordBreaker
    {
        private readonly BreakIterator wordBreaker;
        private readonly CharsRef text = new CharsRef();
        private readonly Queue<int> transitions = new Queue<int>();
        private static readonly UnicodeSet thai = new UnicodeSet("[:Thai:]").Freeze();

        public ThaiWordBreaker(BreakIterator wordBreaker)
        {
            this.wordBreaker = wordBreaker ?? throw new ArgumentNullException(nameof(wordBreaker));
        }

        public void SetText(CharArrayIterator text)
        {
            this.text.CopyChars(text.Text, text.Start, text.Length);
            wordBreaker.SetText(text);
            transitions.Clear();
        }

        public int Current
        {
            get
            {
                if (transitions.TryPeek(out int current))
                {
                    return current;
                }

                return wordBreaker.Current;
            }
        }

        public int Next()
        {
            if (transitions.TryDequeue(out _) && transitions.TryPeek(out int next))
            {
                return next;
            }

            return GetNext();
        }

        private int GetNext()
        {
            bool isThai, isNonThai;
            bool prevWasThai = false, prevWasNonThai = false;
            int prev = wordBreaker.Current;
            int current = wordBreaker.Next();

            if (current != BreakIterator.Done && current - prev > 0)
            {
                int length = text.Length;
                int codePoint;
                // Find all of the transitions between Thai and non-Thai characters and digits
                for (int i = prev; i < current; i++)
                {
                    char high = text[i];
                    // Account for surrogate pairs
                    if (char.IsHighSurrogate(high) && i < length && i + 1 < current && char.IsLowSurrogate(text[i + 1]))
                        codePoint = Character.ToCodePoint(high, text[++i]);
                    else
                        codePoint = high;

                    if (Character.IsLetter(codePoint)) // Always break letters apart from digits to match the JDK
                    {
                        isThai = thai.Contains(codePoint);
                        isNonThai = !isThai;
                    }
                    else
                    {
                        isThai = false;
                        isNonThai = false;
                    }

                    if ((prevWasThai && isNonThai) ||
                        (prevWasNonThai && isThai))
                    {
                        transitions.Enqueue(i);
                    }

                    // record the values for comparison with the next loop
                    prevWasThai = isThai;
                    prevWasNonThai = isNonThai;
                }

                if (transitions.TryPeek(out int transition))
                {
                    transitions.Enqueue(current);
                    return transition;
                }
            }

            return current;
        }
    }
}
#endif
