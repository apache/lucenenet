// Lucene version compatibility level 7.1.0
using ICU4N;
using ICU4N.Support.Text;
using ICU4N.Text;

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
    /// Contain all the issues surrounding BreakIterators in ICU in one place.
    /// Basically this boils down to the fact that they aren't very friendly to any
    /// sort of OO design.
    /// <para/>
    /// http://bugs.icu-project.org/trac/ticket/5901: RBBI.getRuleStatus(), hoist to
    /// BreakIterator from <see cref="RuleBasedBreakIterator"/>
    /// <para/>
    /// DictionaryBasedBreakIterator is a subclass of <see cref="RuleBasedBreakIterator"/>, but
    /// doesn't actually behave as a subclass: it always returns 0 for
    /// getRuleStatus(): 
    /// http://bugs.icu-project.org/trac/ticket/4730: Thai RBBI, no boundary type
    /// tags
    /// <para/>
    /// @lucene.experimental
    /// </summary>
    internal abstract class BreakIteratorWrapper
    {
        protected readonly CharArrayIterator m_textIterator = new CharArrayIterator();
        protected char[] m_text;
        protected int m_start;
        protected int m_length;

        public abstract int Next();
        public abstract int Current { get; }
        public abstract int RuleStatus { get; }
        public abstract void SetText(CharacterIterator text);

        public void SetText(char[] text, int start, int length)
        {
            this.m_text = text;
            this.m_start = start;
            this.m_length = length;
            m_textIterator.SetText(text, start, length);
            SetText(m_textIterator);
        }

        /// <summary>
        /// If its a <see cref="RuleBasedBreakIterator"/>, the rule status can be used for token type. If it's
        /// any other <see cref="BreakIterator"/>, the rulestatus method is not available, so treat
        /// it like a generic <see cref="BreakIterator"/>.
        /// </summary>
        /// <param name="breakIterator"></param>
        /// <returns></returns>
        public static BreakIteratorWrapper Wrap(BreakIterator breakIterator)
        {
            if (breakIterator is RuleBasedBreakIterator)
                return new RBBIWrapper((RuleBasedBreakIterator)breakIterator);
            else
                return new BIWrapper(breakIterator);
        }

        /// <summary>
        /// <see cref="RuleBasedBreakIterator"/> wrapper: <see cref="RuleBasedBreakIterator"/> (as long as it's not
        /// a DictionaryBasedBreakIterator) behaves correctly.
        /// </summary>
        private sealed class RBBIWrapper : BreakIteratorWrapper
        {
            private readonly RuleBasedBreakIterator rbbi;

            internal RBBIWrapper(RuleBasedBreakIterator rbbi)
            {
                this.rbbi = rbbi;
            }

            public override int Current => rbbi.Current;

            public override int RuleStatus => rbbi.RuleStatus;

            public override int Next()
            {
                return rbbi.Next();
            }

            public override void SetText(CharacterIterator text)
            {
                rbbi.SetText(text);
            }
        }

        /// <summary>
        /// Generic <see cref="BreakIterator"/> wrapper: Either the rulestatus method is not
        /// available or always returns 0. Calculate a rulestatus here so it behaves
        /// like <see cref="RuleBasedBreakIterator"/>.
        /// </summary>
        /// <remarks>
        /// Note: This is slower than <see cref="RuleBasedBreakIterator"/>.
        /// </remarks>
        private sealed class BIWrapper : BreakIteratorWrapper
        {
            private readonly BreakIterator bi;
            private int status;

            internal BIWrapper(BreakIterator bi)
            {
                this.bi = bi;
            }

            public override int Current => bi.Current;

            public override int RuleStatus => status;

            public override int Next()
            {
                int current = bi.Current;
                int next = bi.Next();
                status = CalcStatus(current, next);
                return next;
            }

            private int CalcStatus(int current, int next)
            {
                if (current == BreakIterator.Done || next == BreakIterator.Done)
                    return BreakIterator.WordNone;

                int begin = m_start + current;
                int end = m_start + next;

                int codepoint;
                for (int i = begin; i < end; i += UTF16.GetCharCount(codepoint))
                {
                    codepoint = UTF16.CharAt(m_text, 0, end, begin);

                    if (UChar.IsDigit(codepoint))
                        return BreakIterator.WordNumber;
                    else if (UChar.IsLetter(codepoint))
                    {
                        // TODO: try to separately specify ideographic, kana? 
                        // [currently all bundled as letter for this case]
                        return BreakIterator.WordLetter;
                    }
                }

                return BreakIterator.WordNone;
            }

            public override void SetText(CharacterIterator text)
            {
                bi.SetText(text);
                status = BreakIterator.WordNone;
            }
        }
    }
}
