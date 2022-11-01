using Lucene.Net.Diagnostics;
using System;
using System.Collections.Generic;
using JCG = J2N.Collections.Generic;
using WeightedPhraseInfo = Lucene.Net.Search.VectorHighlight.FieldPhraseList.WeightedPhraseInfo;

namespace Lucene.Net.Search.VectorHighlight
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
    /// A abstract implementation of <see cref="IFragListBuilder"/>.
    /// </summary>
    public abstract class BaseFragListBuilder : IFragListBuilder
    {
        public static readonly int MARGIN_DEFAULT = 6;
        public static readonly int MIN_FRAG_CHAR_SIZE_FACTOR = 3;

        internal readonly int margin;
        internal readonly int minFragCharSize;

        protected BaseFragListBuilder(int margin) // LUCENENET: CA1012: Abstract types should not have constructors (marked protected)
        {
            if (margin < 0)
                throw new ArgumentOutOfRangeException(nameof(margin), "margin(" + margin + ") is too small. It must be 0 or higher."); // LUCENENET specific - changed from IllegalArgumentException to ArgumentOutOfRangeException (.NET convention)

            this.margin = margin;
            this.minFragCharSize = Math.Max(1, margin * MIN_FRAG_CHAR_SIZE_FACTOR);
        }

        protected BaseFragListBuilder() // LUCENENET: CA1012: Abstract types should not have constructors (marked protected)
            : this(MARGIN_DEFAULT)
        {
        }

        // LUCENENET specific - need to make this overload of CreateFieldFragList abstract so it satisfies
        // the interface contract.
        public abstract FieldFragList CreateFieldFragList(FieldPhraseList fieldPhraseList, int fragCharSize);

        protected virtual FieldFragList CreateFieldFragList(FieldPhraseList fieldPhraseList, FieldFragList fieldFragList, int fragCharSize)
        {
            // LUCENENET specific - added guard clauses to check for nulls
            if (fieldPhraseList is null)
                throw new ArgumentNullException(nameof(fieldPhraseList));
            if (fieldFragList is null)
                throw new ArgumentNullException(nameof(fieldFragList));
            if (fragCharSize < minFragCharSize)
                throw new ArgumentOutOfRangeException(nameof(fragCharSize), "fragCharSize(" + fragCharSize + ") is too small. It must be " + minFragCharSize + " or higher."); // LUCENENET specific - changed from IllegalArgumentException to ArgumentOutOfRangeException (.NET convention)

            JCG.List<WeightedPhraseInfo> wpil = new JCG.List<WeightedPhraseInfo>();
            using (EnumeratorQueue<WeightedPhraseInfo> queue = new EnumeratorQueue<WeightedPhraseInfo>(fieldPhraseList.PhraseList.GetEnumerator()))
            {
                WeightedPhraseInfo phraseInfo = null;
                int startOffset = 0;
                while ((phraseInfo = queue.Top()) != null)
                {
                    // if the phrase violates the border of previous fragment, discard it and try next phrase
                    if (phraseInfo.StartOffset < startOffset)
                    {
                        queue.RemoveTop();
                        continue;
                    }

                    wpil.Clear();
                    int currentPhraseStartOffset = phraseInfo.StartOffset;
                    int currentPhraseEndOffset = phraseInfo.EndOffset;
                    int spanStart = Math.Max(currentPhraseStartOffset - margin, startOffset);
                    int spanEnd = Math.Max(currentPhraseEndOffset, spanStart + fragCharSize);
                    if (AcceptPhrase(queue.RemoveTop(), currentPhraseEndOffset - currentPhraseStartOffset, fragCharSize))
                    {
                        wpil.Add(phraseInfo);
                    }
                    while ((phraseInfo = queue.Top()) != null)
                    { // pull until we crossed the current spanEnd
                        if (phraseInfo.EndOffset <= spanEnd)
                        {
                            currentPhraseEndOffset = phraseInfo.EndOffset;
                            if (AcceptPhrase(queue.RemoveTop(), currentPhraseEndOffset - currentPhraseStartOffset, fragCharSize))
                            {
                                wpil.Add(phraseInfo);
                            }
                        }
                        else
                        {
                            break;
                        }
                    }
                    if (wpil.Count == 0)
                    {
                        continue;
                    }

                    int matchLen = currentPhraseEndOffset - currentPhraseStartOffset;
                    // now recalculate the start and end position to "center" the result
                    int newMargin = Math.Max(0, (fragCharSize - matchLen) / 2); // matchLen can be > fragCharSize prevent IAOOB here
                    spanStart = currentPhraseStartOffset - newMargin;
                    if (spanStart < startOffset)
                    {
                        spanStart = startOffset;
                    }
                    // whatever is bigger here we grow this out
                    spanEnd = spanStart + Math.Max(matchLen, fragCharSize);
                    startOffset = spanEnd;
                    fieldFragList.Add(spanStart, spanEnd, wpil);
                }
            }
            return fieldFragList;
        }

        /// <summary>
        /// A predicate to decide if the given <see cref="WeightedPhraseInfo"/> should be
        /// accepted as a highlighted phrase or if it should be discarded.
        /// <para/>
        /// The default implementation discards phrases that are composed of more than one term
        /// and where the matchLength exceeds the fragment character size.
        /// </summary>
        /// <param name="info">the phrase info to accept</param>
        /// <param name="matchLength">the match length of the current phrase</param>
        /// <param name="fragCharSize">the configured fragment character size</param>
        /// <returns><c>true</c> if this phrase info should be accepted as a highligh phrase</returns>
        protected virtual bool AcceptPhrase(WeightedPhraseInfo info, int matchLength, int fragCharSize)
        {
            // LUCENENET specific - added guard clause to check for null
            if (info is null)
                throw new ArgumentNullException(nameof(WeightedPhraseInfo));

            return info.TermsOffsets.Count <= 1 || matchLength <= fragCharSize;
        }

        private sealed class EnumeratorQueue<T> : IDisposable // LUCENENET specific - implemented IDisposable to dispose the IEnumerator<T>
        {
            private readonly IEnumerator<T> iter;
            private T top;

            public EnumeratorQueue(IEnumerator<T> iter)
            {
                this.iter = iter;
                T removeTop = RemoveTop();
                if (Debugging.AssertsEnabled) Debugging.Assert(removeTop is null);
            }

            public T Top()
            {
                return top;
            }

            public T RemoveTop()
            {
                T currentTop = top;
                if (iter.MoveNext())
                {
                    top = iter.Current;
                }
                else
                {
                    top = default;
                }
                return currentTop;
            }

            public void Dispose()
            {
                iter.Dispose();
            }
        }
    }
}
