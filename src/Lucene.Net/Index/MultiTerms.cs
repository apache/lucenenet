using J2N.Collections.Generic.Extensions;
using Lucene.Net.Diagnostics;
using System.Collections.Generic;
using JCG = J2N.Collections.Generic;

namespace Lucene.Net.Index
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

    using BytesRef = Lucene.Net.Util.BytesRef;
    using CompiledAutomaton = Lucene.Net.Util.Automaton.CompiledAutomaton;

    /// <summary>
    /// Exposes flex API, merged from flex API of
    /// sub-segments.
    /// <para/>
    /// @lucene.experimental
    /// </summary>

    public sealed class MultiTerms : Terms
    {
        private readonly Terms[] subs;
        private readonly ReaderSlice[] subSlices;
        private readonly IComparer<BytesRef> termComp;
        private readonly bool hasFreqs;
        private readonly bool hasOffsets;
        private readonly bool hasPositions;
        private readonly bool hasPayloads;

        /// <summary>
        /// Sole constructor.
        /// </summary>
        /// <param name="subs"> The <see cref="Terms"/> instances of all sub-readers. </param>
        /// <param name="subSlices"> A parallel array (matching 
        ///        <paramref name="subs"/>) describing the sub-reader slices. </param>
        public MultiTerms(Terms[] subs, ReaderSlice[] subSlices)
        {
            this.subs = subs;
            this.subSlices = subSlices;

            IComparer<BytesRef> _termComp = null;
            if (Debugging.AssertsEnabled) Debugging.Assert(subs.Length > 0, "inefficient: don't use MultiTerms over one sub");
            bool _hasFreqs = true;
            bool _hasOffsets = true;
            bool _hasPositions = true;
            bool _hasPayloads = false;
            for (int i = 0; i < subs.Length; i++)
            {
                if (_termComp is null)
                {
                    _termComp = subs[i].Comparer;
                }
                else
                {
                    // We cannot merge sub-readers that have
                    // different TermComps
                    IComparer<BytesRef> subTermComp = subs[i].Comparer;
                    if (subTermComp != null && !subTermComp.Equals(_termComp))
                    {
                        throw IllegalStateException.Create("sub-readers have different BytesRef.Comparers; cannot merge");
                    }
                }
                _hasFreqs &= subs[i].HasFreqs;
                _hasOffsets &= subs[i].HasOffsets;
                _hasPositions &= subs[i].HasPositions;
                _hasPayloads |= subs[i].HasPayloads;
            }

            termComp = _termComp;
            hasFreqs = _hasFreqs;
            hasOffsets = _hasOffsets;
            hasPositions = _hasPositions;
            hasPayloads = hasPositions && _hasPayloads; // if all subs have pos, and at least one has payloads.
        }

        public override TermsEnum Intersect(CompiledAutomaton compiled, BytesRef startTerm)
        {
            IList<MultiTermsEnum.TermsEnumIndex> termsEnums = new JCG.List<MultiTermsEnum.TermsEnumIndex>();
            for (int i = 0; i < subs.Length; i++)
            {
                TermsEnum termsEnum = subs[i].Intersect(compiled, startTerm);
                if (termsEnum != null)
                {
                    termsEnums.Add(new MultiTermsEnum.TermsEnumIndex(termsEnum, i));
                }
            }

            if (termsEnums.Count > 0)
            {
                return (new MultiTermsEnum(subSlices)).Reset(termsEnums.ToArray(/*MultiTermsEnum.TermsEnumIndex.EMPTY_ARRAY*/));
            }
            else
            {
                return TermsEnum.EMPTY;
            }
        }

        public override TermsEnum GetEnumerator()
        {
            IList<MultiTermsEnum.TermsEnumIndex> termsEnums = new JCG.List<MultiTermsEnum.TermsEnumIndex>();
            for (int i = 0; i < subs.Length; i++)
            {
                TermsEnum termsEnum = subs[i].GetEnumerator();
                if (termsEnum != null)
                {
                    termsEnums.Add(new MultiTermsEnum.TermsEnumIndex(termsEnum, i));
                }
            }

            if (termsEnums.Count > 0)
            {
                return (new MultiTermsEnum(subSlices)).Reset(termsEnums.ToArray(/*MultiTermsEnum.TermsEnumIndex.EMPTY_ARRAY*/));
            }
            else
            {
                return TermsEnum.EMPTY;
            }
        }

        public override long Count => -1;

        public override long SumTotalTermFreq
        {
            get
            {
                long sum = 0;
                foreach (Terms terms in subs)
                {
                    long v = terms.SumTotalTermFreq;
                    if (v == -1)
                    {
                        return -1;
                    }
                    sum += v;
                }
                return sum;
            }
        }

        public override long SumDocFreq
        {
            get
            {
                long sum = 0;
                foreach (Terms terms in subs)
                {
                    long v = terms.SumDocFreq;
                    if (v == -1)
                    {
                        return -1;
                    }
                    sum += v;
                }
                return sum;
            }
        }

        public override int DocCount
        {
            get
            {
                int sum = 0;
                foreach (Terms terms in subs)
                {
                    int v = terms.DocCount;
                    if (v == -1)
                    {
                        return -1;
                    }
                    sum += v;
                }
                return sum;
            }
        }

        public override IComparer<BytesRef> Comparer => termComp;

        public override bool HasFreqs => hasFreqs;

        public override bool HasOffsets => hasOffsets;

        public override bool HasPositions => hasPositions;

        public override bool HasPayloads => hasPayloads;
    }
}