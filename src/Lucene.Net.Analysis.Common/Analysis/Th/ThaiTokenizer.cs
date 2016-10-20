using Icu;
using Lucene.Net.Analysis.Tokenattributes;
using Lucene.Net.Analysis.Util;
using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

    /// <summary>
    /// Tokenizer that use <seealso cref="BreakIterator"/> to tokenize Thai text.
    /// <para>WARNING: this tokenizer may not be supported by all JREs.
    ///    It is known to work with Sun/Oracle and Harmony JREs.
    ///    If your application needs to be fully portable, consider using ICUTokenizer instead,
    ///    which uses an ICU Thai BreakIterator that will always be available.
    /// </para>
    /// </summary>
    public class ThaiTokenizer : SegmentingTokenizerBase
    {
        /// <summary>
        /// True if the JRE supports a working dictionary-based breakiterator for Thai.
        /// If this is false, this tokenizer will not work at all!
        /// </summary>
        public static readonly bool DBBI_AVAILABLE;
        private static readonly IEnumerable<Boundary> proto;
        static ThaiTokenizer()
        {
            // check that we have a working dictionary-based break iterator for thai
            proto = BreakIterator.GetWordBoundaries(LocaleUS, "ภาษาไทย", includeSpacesAndPunctuation: false).ToArray();
            var first = proto.FirstOrDefault();
            DBBI_AVAILABLE = first != default(Boundary) && first.End == 4;
        }

        private readonly ThaiWordBreaker wordBreaker;
        private readonly CharArrayIterator wrapper = CharArrayIterator.NewWordInstance();

        internal int sentenceStart;
        internal int sentenceEnd;

        private readonly ICharTermAttribute termAtt;
        private readonly IOffsetAttribute offsetAtt;

        /// <summary>
        /// Creates a new ThaiTokenizer </summary>
        public ThaiTokenizer(TextReader reader)
              : this(AttributeFactory.DEFAULT_ATTRIBUTE_FACTORY, reader)
        {
        }

        /// <summary>
        /// Creates a new ThaiTokenizer, supplying the AttributeFactory </summary>
        public ThaiTokenizer(AttributeFactory factory, TextReader reader)
              : base(factory, reader, LocaleUS, BreakIterator.UBreakIteratorType.SENTENCE)
        {
            if (!DBBI_AVAILABLE)
            {
                throw new System.NotSupportedException("This JRE does not have support for Thai segmentation");
            }
            wordBreaker = new ThaiWordBreaker(LocaleUS);
            termAtt = AddAttribute<ICharTermAttribute>();
            offsetAtt = AddAttribute<IOffsetAttribute>();
        }

        protected internal override void SetNextSentence(int sentenceStart, int sentenceEnd)
        {
            this.sentenceStart = sentenceStart;
            this.sentenceEnd = sentenceEnd;
            wrapper.SetText(buffer, sentenceStart, sentenceEnd - sentenceStart);
            wordBreaker.SetText(new string(wrapper.Text, wrapper.Start, wrapper.Length));
        }

        protected internal override bool IncrementWord()
        {
            int start = wordBreaker.Current();
            if (start == BreakIterator.DONE)
            {
                return false; // BreakIterator exhausted
            }

            // find the next set of boundaries, skipping over non-tokens
            int end = wordBreaker.Next();
            while (end != BreakIterator.DONE && !char.IsLetterOrDigit((char)Support.Character.CodePointAt(buffer, sentenceStart + start, sentenceEnd)))
            {
                start = end;
                end = wordBreaker.Next();
            }

            if (end == BreakIterator.DONE)
            {
                return false; // BreakIterator exhausted
            }

            ClearAttributes();
            termAtt.CopyBuffer(buffer, sentenceStart + start, end - start);
            offsetAtt.SetOffset(CorrectOffset(offset + sentenceStart + start), CorrectOffset(offset + sentenceStart + end));
            return true;
        }
    }

    /// <summary>
    /// LUCENENET specific class to patch the behavior of the ICU BreakIterator.
    /// Corrects the breaking of words by finding transitions between Thai and non-Thai
    /// characters.
    /// 
    /// This logic assumes that the Java BreakIterator also breaks up Thai numerals from
    /// Arabic numerals (1, 2, 3, etc.). That is, it assumes the first test below passes
    /// and the second test fails in Lucene (not attempted).
    /// 
    /// ThaiAnalyzer analyzer = new ThaiAnalyzer(TEST_VERSION_CURRENT, CharArraySet.EMPTY_SET);
    /// AssertAnalyzesTo(analyzer, "๑๒๓456", new string[] { "๑๒๓", "456" });
    /// AssertAnalyzesTo(analyzer, "๑๒๓456", new string[] { "๑๒๓456" });
    /// </summary>
    internal class ThaiWordBreaker
    {
        private readonly Locale locale;
        private IEnumerator<Boundary> wordBreaker;
        private Boundary[] WordBoundaries;
        private int currentIndex;
        private string text;
        private readonly IList<int> transitions = new List<int>();
        private readonly static Regex thaiPattern = new Regex(@"\p{IsThai}", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        public ThaiWordBreaker(Locale locale)
        {
            if (locale == null)
            {
                throw new ArgumentNullException("locale");
            }

            this.locale = locale;
            currentIndex = int.MinValue;
        }

        public void SetText(string text)
        {
            this.text = text;
            wordBreaker = BreakIterator.GetWordBoundaries(locale, text, includeSpacesAndPunctuation: false).ToList().GetEnumerator();
            currentIndex = wordBreaker.MoveNext()
                ? wordBreaker.Current.Start : BreakIterator.DONE;
        }

        public int Current()
        {
            return currentIndex;
        }

        public int Next()
        {
            // Tracking whether a transition was returned last time
            // next was called. If that is the case, and there are no
            // transitions left, then we return the End index in the
            // wordbreaker.Current
            bool transitionReturned = false;

            if (transitions.Any())
            {
                transitionReturned = currentIndex == transitions[0];
                transitions.RemoveAt(0);
            }

            if (transitions.Any())
            {
                currentIndex = transitions.First();
                return currentIndex;
            }
            else if (transitionReturned)
            {
                currentIndex = wordBreaker.Current.End;
            }

            return GetNext();
        }

        private int GetNext()
        {
            bool isThai = false, isNonThai = false;
            bool prevWasThai = false, prevWasNonThai = false;

            int previous = currentIndex;
            int current;

            if (currentIndex == wordBreaker.Current.Start)
            {
                current = wordBreaker.Current.End;
            }
            else if (wordBreaker.MoveNext())
            {
                // The break iterator works by returning the start and end
                // boundary of each word it finds. Consider the two words,
                // 
                if (currentIndex == wordBreaker.Current.Start)
                {
                    current = wordBreaker.Current.End;
                }
                else
                {
                    current = wordBreaker.Current.Start;
                }
            }
            else
            {
                current = BreakIterator.DONE;
            }

            if (current != BreakIterator.DONE && current - previous > 0)
            {
                // Find all of the transitions between Thai and non-Thai characters and digits
                for (int i = previous; i < current; i++)
                {
                    char c = text[i];
                    isThai = thaiPattern.IsMatch(c.ToString());
                    isNonThai = char.IsLetterOrDigit(c) && !isThai;

                    if ((prevWasThai && isNonThai) ||
                        (prevWasNonThai && isThai))
                    {
                        transitions.Add(i);
                    }

                    // record the values for comparison with the next loop
                    prevWasThai = isThai;
                    prevWasNonThai = isNonThai;
                }

                if (transitions.Any())
                {
                    transitions.Add(current);
                    currentIndex = transitions.First();

                    return currentIndex;
                }
            }

            currentIndex = current;
            return currentIndex;
        }
    }
}