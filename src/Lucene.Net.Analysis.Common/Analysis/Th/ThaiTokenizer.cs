#if FEATURE_BREAKITERATOR
using ICU4N.Text;
using J2N;
using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Analysis.Util;
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
        private readonly CharArrayIterator wrapper = Analysis.Util.CharArrayIterator.NewWordInstance();

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
        /// Creates a new <see cref="ThaiTokenizer"/>, supplying the <see cref="Lucene.Net.Util.AttributeSource.AttributeFactory"/> </summary>
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
            wordBreaker.SetText(new string(wrapper.Text, wrapper.Start, wrapper.Length));
        }

        protected override bool IncrementWord()
        {
            int start = wordBreaker.Current;
            if (start == BreakIterator.Done)
            {
                return false; // BreakIterator exhausted
            }

            // find the next set of boundaries, skipping over non-tokens
            int end = wordBreaker.Next();
            while (end != BreakIterator.Done && !char.IsLetterOrDigit((char)Character.CodePointAt(m_buffer, sentenceStart + start, sentenceEnd)))
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
    /// LUCENENET specific class to patch the behavior of the ICU BreakIterator.
    /// Corrects the breaking of words by finding transitions between Thai and non-Thai
    /// characters.
    /// </summary>
    internal class ThaiWordBreaker
    {
        private readonly BreakIterator wordBreaker;
        private string text;
        private readonly IList<int> transitions = new List<int>();
        private readonly static Regex thaiPattern = new Regex(@"\p{IsThai}", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        public ThaiWordBreaker(BreakIterator wordBreaker)
        {
            if (wordBreaker == null)
            {
                throw new ArgumentNullException("wordBreaker");
            }
            this.wordBreaker = wordBreaker;
        }

        public void SetText(string text)
        {
            this.text = text;
            wordBreaker.SetText(text);
        }

        public int Current
        {
            get
            {
                if (transitions.Count > 0)
                {
                    return transitions[0];
                }
                return wordBreaker.Current;
            }
        }

        public int Next()
        {
            if (transitions.Count > 0)
            {
                transitions.RemoveAt(0);
            }
            if (transitions.Count > 0)
            {
                return transitions[0];
            }
            return GetNext();
        }

        private int GetNext()
        {
            bool isThai = false, isNonThai = false;
            bool prevWasThai = false, prevWasNonThai = false;
            int prev = wordBreaker.Current;
            int current = wordBreaker.Next();

            if (current != BreakIterator.Done && current - prev > 0)
            {
                // Find all of the transitions between Thai and non-Thai characters and digits
                for (int i = prev; i < current; i++)
                {
                    char c = text[i];
                    isThai = char.IsLetter(c) && thaiPattern.IsMatch(c.ToString());
                    isNonThai = char.IsLetter(c) && !isThai;

                    if ((prevWasThai && isNonThai) ||
                        (prevWasNonThai && isThai))
                    {
                        transitions.Add(i);
                    }

                    // record the values for comparison with the next loop
                    prevWasThai = isThai;
                    prevWasNonThai = isNonThai;
                }

                if (transitions.Count > 0)
                {
                    transitions.Add(current);
                    return transitions[0];
                }
            }

            return current;
        }
    }
}
#endif