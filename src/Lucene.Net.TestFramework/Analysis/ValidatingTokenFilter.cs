using Lucene.Net.Analysis.TokenAttributes;
using System;
using System.Collections.Generic;

namespace Lucene.Net.Analysis
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

    // TODO: rename to OffsetsXXXTF?  ie we only validate
    // offsets (now anyway...)

    // TODO: also make a DebuggingTokenFilter, that just prints
    // all att values that come through it...

    // TODO: BTSTC should just append this to the chain
    // instead of checking itself:

    /// <summary>
    /// A <see cref="TokenFilter"/> that checks consistency of the tokens (eg
    /// offsets are consistent with one another).
    /// </summary>
    public sealed class ValidatingTokenFilter : TokenFilter
    {
        private int pos;
        private int lastStartOffset;

        // Maps position to the start/end offset:
        private readonly IDictionary<int, int> posToStartOffset = new Dictionary<int, int>();

        private readonly IDictionary<int, int> posToEndOffset = new Dictionary<int, int>();

        private readonly IPositionIncrementAttribute posIncAtt;
        private readonly IPositionLengthAttribute posLenAtt;
        private readonly IOffsetAttribute offsetAtt;
        private readonly ICharTermAttribute termAtt;
        private readonly bool offsetsAreCorrect;

        private readonly string name;

        // Returns null if the attr wasn't already added
        private A GetAttrIfExists<A>() where A : Lucene.Net.Util.IAttribute
        {
            if (HasAttribute<A>())
            {
                return GetAttribute<A>();
            }
            else
            {
                return default;
            }
        }

        /// <summary>
        /// The <paramref name="name"/> is used to identify this stage when
        /// throwing exceptions (useful if you have more than one
        /// instance in your chain).
        /// </summary>
        public ValidatingTokenFilter(TokenStream @in, string name, bool offsetsAreCorrect)
            : base(@in)
        {
            posIncAtt = GetAttrIfExists<IPositionIncrementAttribute>();
            posLenAtt = GetAttrIfExists<IPositionLengthAttribute>();
            offsetAtt = GetAttrIfExists<IOffsetAttribute>();
            termAtt = GetAttrIfExists<ICharTermAttribute>();
            this.name = name;
            this.offsetsAreCorrect = offsetsAreCorrect;
        }

        public override bool IncrementToken()
        {
            if (!m_input.IncrementToken())
            {
                return false;
            }

            int startOffset = 0;
            int endOffset = 0;
            int posLen; // LUCENENET: IDE0059: Remove unnecessary value assignment

            if (posIncAtt != null)
            {
                pos += posIncAtt.PositionIncrement;
                if (pos == -1)
                {
                    throw IllegalStateException.Create("first posInc must be > 0");
                }
            }

            // System.out.println("  got token=" + termAtt + " pos=" + pos);

            if (offsetAtt != null)
            {
                startOffset = offsetAtt.StartOffset;
                endOffset = offsetAtt.EndOffset;

                if (offsetsAreCorrect && offsetAtt.StartOffset < lastStartOffset)
                {
                    throw IllegalStateException.Create(name + ": offsets must not go backwards startOffset=" + startOffset + " is < lastStartOffset=" + lastStartOffset);
                }
                lastStartOffset = offsetAtt.StartOffset;
            }

            posLen = posLenAtt is null ? 1 : posLenAtt.PositionLength;

            if (offsetAtt != null && posIncAtt != null && offsetsAreCorrect)
            {
                if (!posToStartOffset.TryGetValue(pos, out int oldStartOffset))
                {
                    // First time we've seen a token leaving from this position:
                    posToStartOffset[pos] = startOffset;
                    //System.out.println("  + s " + pos + " -> " + startOffset);
                }
                else
                {
                    // We've seen a token leaving from this position
                    // before; verify the startOffset is the same:
                    //System.out.println("  + vs " + pos + " -> " + startOffset);
                    if (oldStartOffset != startOffset)
                    {
                        throw IllegalStateException.Create(name + ": inconsistent startOffset at pos=" + pos + ": " + oldStartOffset + " vs " + startOffset + "; token=" + termAtt);
                    }
                }

                int endPos = pos + posLen;

                if (!posToEndOffset.TryGetValue(endPos, out int oldEndOffset))
                {
                    // First time we've seen a token arriving to this position:
                    posToEndOffset[endPos] = endOffset;
                    //System.out.println("  + e " + endPos + " -> " + endOffset);
                }
                else
                {
                    // We've seen a token arriving to this position
                    // before; verify the endOffset is the same:
                    //System.out.println("  + ve " + endPos + " -> " + endOffset);
                    if (oldEndOffset != endOffset)
                    {
                        throw IllegalStateException.Create(name + ": inconsistent endOffset at pos=" + endPos + ": " + oldEndOffset + " vs " + endOffset + "; token=" + termAtt);
                    }
                }
            }

            return true;
        }

        public override void End()
        {
            base.End();

            // TODO: what else to validate

            // TODO: check that endOffset is >= max(endOffset)
            // we've seen
        }

        public override void Reset()
        {
            base.Reset();
            pos = -1;
            posToStartOffset.Clear();
            posToEndOffset.Clear();
            lastStartOffset = 0;
        }
    }
}