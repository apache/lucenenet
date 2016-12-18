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

    using CharTermAttribute = Lucene.Net.Analysis.TokenAttributes.CharTermAttribute;
    using OffsetAttribute = Lucene.Net.Analysis.TokenAttributes.OffsetAttribute;
    using PositionIncrementAttribute = Lucene.Net.Analysis.TokenAttributes.PositionIncrementAttribute;
    using PositionLengthAttribute = Lucene.Net.Analysis.TokenAttributes.PositionLengthAttribute;

    // TODO: rename to OffsetsXXXTF?  ie we only validate
    // offsets (now anyway...)

    // TODO: also make a DebuggingTokenFilter, that just prints
    // all att values that come through it...

    // TODO: BTSTC should just append this to the chain
    // instead of checking itself:

    /// <summary>
    /// A TokenFilter that checks consistency of the tokens (eg
    ///  offsets are consistent with one another).
    /// </summary>
    public sealed class ValidatingTokenFilter : TokenFilter
    {
        private bool InstanceFieldsInitialized = false;

        private void InitializeInstanceFields()
        {
            PosIncAtt = getAttrIfExists<PositionIncrementAttribute>();
            PosLenAtt = getAttrIfExists<PositionLengthAttribute>();
            OffsetAtt = getAttrIfExists<OffsetAttribute>();
            TermAtt = getAttrIfExists<CharTermAttribute>();
        }

        private int Pos;
        private int LastStartOffset;

        // Maps position to the start/end offset:
        private readonly IDictionary<int, int> PosToStartOffset = new Dictionary<int, int>();

        private readonly IDictionary<int, int> PosToEndOffset = new Dictionary<int, int>();

        private PositionIncrementAttribute PosIncAtt;
        private PositionLengthAttribute PosLenAtt;
        private OffsetAttribute OffsetAtt;
        private CharTermAttribute TermAtt;
        private readonly bool OffsetsAreCorrect;

        private readonly string Name;

        // Returns null if the attr wasn't already added
        private A getAttrIfExists<A>() where A : Lucene.Net.Util.Attribute
        {
            var att = typeof(A);
            if (HasAttribute<A>())
            {
                return GetAttribute<A>();
            }
            else
            {
                return default(A);
            }
        }

        /// <summary>
        /// The name arg is used to identify this stage when
        ///  throwing exceptions (useful if you have more than one
        ///  instance in your chain).
        /// </summary>
        public ValidatingTokenFilter(TokenStream @in, string name, bool offsetsAreCorrect)
            : base(@in)
        {
            if (!InstanceFieldsInitialized)
            {
                InitializeInstanceFields();
                InstanceFieldsInitialized = true;
            }
            this.Name = name;
            this.OffsetsAreCorrect = offsetsAreCorrect;
        }

        public override bool IncrementToken()
        {
            if (!input.IncrementToken())
            {
                return false;
            }

            int startOffset = 0;
            int endOffset = 0;
            int posLen = 0;

            if (PosIncAtt != null)
            {
                Pos += PosIncAtt.PositionIncrement;
                if (Pos == -1)
                {
                    throw new Exception("first posInc must be > 0");
                }
            }

            // System.out.println("  got token=" + termAtt + " pos=" + pos);

            if (OffsetAtt != null)
            {
                startOffset = OffsetAtt.StartOffset;
                endOffset = OffsetAtt.EndOffset;

                if (OffsetsAreCorrect && OffsetAtt.StartOffset < LastStartOffset)
                {
                    throw new Exception(Name + ": offsets must not go backwards startOffset=" + startOffset + " is < lastStartOffset=" + LastStartOffset);
                }
                LastStartOffset = OffsetAtt.StartOffset;
            }

            posLen = PosLenAtt == null ? 1 : PosLenAtt.PositionLength;

            if (OffsetAtt != null && PosIncAtt != null && OffsetsAreCorrect)
            {
                if (!PosToStartOffset.ContainsKey(Pos))
                {
                    // First time we've seen a token leaving from this position:
                    PosToStartOffset[Pos] = startOffset;
                    //System.out.println("  + s " + pos + " -> " + startOffset);
                }
                else
                {
                    // We've seen a token leaving from this position
                    // before; verify the startOffset is the same:
                    //System.out.println("  + vs " + pos + " -> " + startOffset);
                    int oldStartOffset = PosToStartOffset[Pos];
                    if (oldStartOffset != startOffset)
                    {
                        throw new Exception(Name + ": inconsistent startOffset at pos=" + Pos + ": " + oldStartOffset + " vs " + startOffset + "; token=" + TermAtt);
                    }
                }

                int endPos = Pos + posLen;

                if (!PosToEndOffset.ContainsKey(endPos))
                {
                    // First time we've seen a token arriving to this position:
                    PosToEndOffset[endPos] = endOffset;
                    //System.out.println("  + e " + endPos + " -> " + endOffset);
                }
                else
                {
                    // We've seen a token arriving to this position
                    // before; verify the endOffset is the same:
                    //System.out.println("  + ve " + endPos + " -> " + endOffset);
                    int oldEndOffset = PosToEndOffset[endPos];
                    if (oldEndOffset != endOffset)
                    {
                        throw new Exception(Name + ": inconsistent endOffset at pos=" + endPos + ": " + oldEndOffset + " vs " + endOffset + "; token=" + TermAtt);
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
            Pos = -1;
            PosToStartOffset.Clear();
            PosToEndOffset.Clear();
            LastStartOffset = 0;
        }
    }
}