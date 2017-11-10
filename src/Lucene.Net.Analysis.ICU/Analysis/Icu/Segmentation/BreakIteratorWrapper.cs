// LUCENENET TODO: Port issues - missing dependencies

//using Icu;
//using Lucene.Net.Analysis.Util;
//using Lucene.Net.Support;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace Lucene.Net.Analysis.ICU.Segmentation
//{
//    /*
//     * Licensed to the Apache Software Foundation (ASF) under one or more
//     * contributor license agreements.  See the NOTICE file distributed with
//     * this work for additional information regarding copyright ownership.
//     * The ASF licenses this file to You under the Apache License, Version 2.0
//     * (the "License"); you may not use this file except in compliance with
//     * the License.  You may obtain a copy of the License at
//     *
//     *     http://www.apache.org/licenses/LICENSE-2.0
//     *
//     * Unless required by applicable law or agreed to in writing, software
//     * distributed under the License is distributed on an "AS IS" BASIS,
//     * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//     * See the License for the specific language governing permissions and
//     * limitations under the License.
//     */

//    /// <summary>
//    /// Contain all the issues surrounding BreakIterators in ICU in one place.
//    /// Basically this boils down to the fact that they aren't very friendly to any
//    /// sort of OO design.
//    /// <para/>
//    /// http://bugs.icu-project.org/trac/ticket/5901: RBBI.getRuleStatus(), hoist to
//    /// BreakIterator from RuleBasedBreakIterator
//    /// <para/>
//    /// DictionaryBasedBreakIterator is a subclass of RuleBasedBreakIterator, but
//    /// doesn't actually behave as a subclass: it always returns 0 for
//    /// getRuleStatus(): 
//    /// http://bugs.icu-project.org/trac/ticket/4730: Thai RBBI, no boundary type
//    /// tags
//    /// <para/>
//    /// @lucene.experimental
//    /// </summary>
//    internal abstract class BreakIteratorWrapper
//    {
//        protected readonly CharArrayIterator textIterator = new CharArrayIterator();
//        protected char[] text;
//        protected int start;
//        protected int length;

//        public abstract int Next();
//        public abstract int Current { get; }
//        public abstract int GetRuleStatus();
//        public abstract void SetText(CharacterIterator text);

//        public void SetText(char[] text, int start, int length)
//        {
//            this.text = text;
//            this.start = start;
//            this.length = length;
//            textIterator.SetText(text, start, length);
//            SetText(textIterator);
//        }

//        /**
//         * If its a RuleBasedBreakIterator, the rule status can be used for token type. If its
//         * any other BreakIterator, the rulestatus method is not available, so treat
//         * it like a generic BreakIterator.
//         */
//        public static BreakIteratorWrapper Wrap(Icu.BreakIterator breakIterator)
//        {
//            if (breakIterator is Icu.RuleBasedBreakIterator)
//                return new RBBIWrapper((Icu.RuleBasedBreakIterator)breakIterator);
//            else
//                return new BIWrapper(breakIterator);
//        }

//        /**
//         * RuleBasedBreakIterator wrapper: RuleBasedBreakIterator (as long as its not
//         * a DictionaryBasedBreakIterator) behaves correctly.
//         */
//        private sealed class RBBIWrapper : BreakIteratorWrapper
//        {
//            private readonly Icu.RuleBasedBreakIterator rbbi;

//            internal RBBIWrapper(Icu.RuleBasedBreakIterator rbbi)
//            {
//                this.rbbi = rbbi;
//            }

//            public override int Current
//            {
//                get { return rbbi.Current; }
//            }

//            public override int GetRuleStatus()
//            {
//                return rbbi.GetRuleStatus();
//            }

//            public override int Next()
//            {
//                return rbbi.Next();
//            }

//            public override void SetText(CharacterIterator text)
//            {
//                rbbi.SetText(text);
//            }
//        }

//        /**
//         * Generic BreakIterator wrapper: Either the rulestatus method is not
//         * available or always returns 0. Calculate a rulestatus here so it behaves
//         * like RuleBasedBreakIterator.
//         * 
//         * Note: This is slower than RuleBasedBreakIterator.
//         */
//        private sealed class BIWrapper : BreakIteratorWrapper
//        {
//            private readonly Support.BreakIterator bi;
//            private int status;

//            internal BIWrapper(Support.BreakIterator bi)
//            {
//                this.bi = bi;
//            }

//            public override int Current
//            {
//                get { return bi.Current; }
//            }

//            public override int GetRuleStatus()
//            {
//                return status;
//            }

//            public override int Next()
//            {
//                int current = bi.Current;
//                int next = bi.Next();
//                status = CalcStatus(current, next);
//                return next;
//            }

//            private int CalcStatus(int current, int next)
//            {
//                if (current == Support.BreakIterator.DONE || next == Support.BreakIterator.DONE)
//                    return RuleBasedBreakIterator.WORD_NONE;

//                int begin = start + current;
//                int end = start + next;

//                int codepoint;
//                for (int i = begin; i < end; i += UTF16.getCharCount(codepoint))
//                {
//                    codepoint = UTF16.charAt(text, 0, end, begin);

//                    if (UCharacter.isDigit(codepoint))
//                        return RuleBasedBreakIterator.WORD_NUMBER;
//                    else if (UCharacter.isLetter(codepoint))
//                    {
//                        // TODO: try to separately specify ideographic, kana? 
//                        // [currently all bundled as letter for this case]
//                        return RuleBasedBreakIterator.WORD_LETTER;
//                    }
//                }

//                return RuleBasedBreakIterator.WORD_NONE;
//            }

//            public override void SetText(CharacterIterator text)
//            {
//                bi.SetText(text);
//                status = RuleBasedBreakIterator.WORD_NONE;
//            }
//        }
//    }
//}
