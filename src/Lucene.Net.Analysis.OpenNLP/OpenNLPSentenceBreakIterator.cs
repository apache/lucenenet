// Lucene version compatibility level 8.2.0
using ICU4N.Support.Text;
using ICU4N.Text;
using Lucene.Net.Analysis.OpenNlp.Tools;
using Lucene.Net.Analysis.Util;
using opennlp.tools.util;
using System;
using System.Diagnostics;
using System.Text;

namespace Lucene.Net.Analysis.OpenNlp
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
    /// A <see cref="BreakIterator"/> that splits sentences using an OpenNLP sentence chunking model.
    /// </summary>
    public sealed class OpenNLPSentenceBreakIterator : BreakIterator
    {
        private CharacterIterator text;
        private int currentSentence;
        private int[] sentenceStarts;
        private readonly NLPSentenceDetectorOp sentenceOp; // LUCENENET: marked readonly

        public OpenNLPSentenceBreakIterator(NLPSentenceDetectorOp sentenceOp)
        {
            this.sentenceOp = sentenceOp;
        }

        public override int Current => text.Index;

        public override int First()
        {
            currentSentence = 0;
            text.SetIndex(text.BeginIndex);
            return Current;
        }

        public override int Last()
        {
            if (sentenceStarts.Length > 0)
            {
                currentSentence = sentenceStarts.Length - 1;
                text.SetIndex(text.EndIndex);
            }
            else
            { // there are no sentences; both the first and last positions are the begin index
                currentSentence = 0;
                text.SetIndex(text.BeginIndex);
            }
            return Current;
        }

        public override int Next()
        {
            if (text.Index == text.EndIndex || 0 == sentenceStarts.Length)
            {
                return Done;
            }
            else if (currentSentence < sentenceStarts.Length - 1)
            {
                text.SetIndex(sentenceStarts[++currentSentence]);
                return Current;
            }
            else
            {
                return Last();
            }
        }

        public override int Following(int pos)
        {
            if (pos < text.BeginIndex || pos > text.EndIndex)
            {
                throw new ArgumentOutOfRangeException(nameof(pos), "offset out of bounds: must be >= text.BeginIndex and <= text.EndIndex"); // LUCENENET specific - changed from IllegalArgumentException to ArgumentOutOfRangeException (.NET convention)
            }
            else if (0 == sentenceStarts.Length)
            {
                text.SetIndex(text.BeginIndex);
                return Done;
            }
            else if (pos >= sentenceStarts[sentenceStarts.Length - 1])
            {
                // this conflicts with the javadocs, but matches actual behavior (Oracle has a bug in something)
                // https://bugs.openjdk.java.net/browse/JDK-8015110
                text.SetIndex(text.EndIndex);
                currentSentence = sentenceStarts.Length - 1;
                return Done;
            }
            else
            { // there are at least two sentences
                currentSentence = (sentenceStarts.Length - 1) / 2; // start search from the middle
                MoveToSentenceAt(pos, 0, sentenceStarts.Length - 2);
                text.SetIndex(sentenceStarts[++currentSentence]);
                return Current;
            }
        }

        /// <summary>Binary search over sentences</summary>
        private void MoveToSentenceAt(int pos, int minSentence, int maxSentence)
        {
            if (minSentence != maxSentence)
            {
                if (pos < sentenceStarts[currentSentence])
                {
                    int newMaxSentence = currentSentence - 1;
                    currentSentence = minSentence + (currentSentence - minSentence) / 2;
                    MoveToSentenceAt(pos, minSentence, newMaxSentence);
                }
                else if (pos >= sentenceStarts[currentSentence + 1])
                {
                    int newMinSentence = currentSentence + 1;
                    currentSentence = maxSentence - (maxSentence - currentSentence) / 2;
                    MoveToSentenceAt(pos, newMinSentence, maxSentence);
                }
            }
            else
            {
                Debug.Assert(currentSentence == minSentence);
                Debug.Assert(pos >= sentenceStarts[currentSentence]);
                Debug.Assert((currentSentence == sentenceStarts.Length - 1 && pos <= text.EndIndex)
                    || pos < sentenceStarts[currentSentence + 1]);
            }
            // we have arrived - nothing to do
        }

        public override int Previous()
        {
            if (text.Index == text.BeginIndex)
            {
                return Done;
            }
            else
            {
                if (0 == sentenceStarts.Length)
                {
                    text.SetIndex(text.BeginIndex);
                    return Done;
                }
                if (text.Index == text.EndIndex)
                {
                    text.SetIndex(sentenceStarts[currentSentence]);
                }
                else
                {
                    text.SetIndex(sentenceStarts[--currentSentence]);
                }
                return Current;
            }
        }

        public override int Preceding(int pos)
        {
            if (pos < text.BeginIndex || pos > text.EndIndex)
            {
                throw new ArgumentOutOfRangeException(nameof(pos), "offset out of bounds: must be >= text.BeginIndex and <= text.EndIndex"); // LUCENENET specific - changed from IllegalArgumentException to ArgumentOutOfRangeException (.NET convention)
            }
            else if (0 == sentenceStarts.Length)
            {
                text.SetIndex(text.BeginIndex);
                currentSentence = 0;
                return Done;
            }
            else if (pos < sentenceStarts[0])
            {
                // this conflicts with the javadocs, but matches actual behavior (Oracle has a bug in something)
                // https://bugs.openjdk.java.net/browse/JDK-8015110
                text.SetIndex(text.BeginIndex);
                currentSentence = 0;
                return Done;
            }
            else
            {
                currentSentence = sentenceStarts.Length / 2; // start search from the middle
                MoveToSentenceAt(pos, 0, sentenceStarts.Length - 1);
                if (0 == currentSentence)
                {
                    text.SetIndex(text.BeginIndex);
                    return Done;
                }
                else
                {
                    text.SetIndex(sentenceStarts[--currentSentence]);
                    return Current;
                }
            }
        }

        public override int Next(int n)
        {
            currentSentence += n;
            if (n < 0)
            {
                if (text.Index == text.EndIndex)
                {
                    ++currentSentence;
                }
                if (currentSentence < 0)
                {
                    currentSentence = 0;
                    text.SetIndex(text.BeginIndex);
                    return Done;
                }
                else
                {
                    text.SetIndex(sentenceStarts[currentSentence]);
                }
            }
            else if (n > 0)
            {
                if (currentSentence >= sentenceStarts.Length)
                {
                    currentSentence = sentenceStarts.Length - 1;
                    text.SetIndex(text.EndIndex);
                    return Done;
                }
                else
                {
                    text.SetIndex(sentenceStarts[currentSentence]);
                }
            }
            return Current;
        }

        public override CharacterIterator Text => text;

        public override void SetText(CharacterIterator newText)
        {
            text = newText;
            text.SetIndex(text.BeginIndex);
            currentSentence = 0;
            Span[] spans = sentenceOp.SplitSentences(CharacterIteratorToString());
            sentenceStarts = new int[spans.Length];
            for (int i = 0; i < spans.Length; ++i)
            {
                // Adjust start positions to match those of the passed-in CharacterIterator
                sentenceStarts[i] = spans[i].getStart() + text.BeginIndex;
            }
        }

        private string CharacterIteratorToString()
        {
            string fullText;
            if (text is CharArrayIterator charArrayIterator)
            {
                fullText = new string(charArrayIterator.Text, charArrayIterator.Start, charArrayIterator.Length);
            }
            else
            {
                // TODO: is there a better way to extract full text from arbitrary CharacterIterators?
                StringBuilder builder = new StringBuilder();
                for (char ch = text.First(); ch != CharacterIterator.Done; ch = text.Next())
                {
                    builder.Append(ch);
                }
                fullText = builder.ToString();
                text.SetIndex(text.BeginIndex);
            }
            return fullText;
        }
    }
}
