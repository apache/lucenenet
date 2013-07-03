/* 
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 * 
 * http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using Lucene.Net.Index;
using Lucene.Net.Util;
using Lucene.Net.Util.Automaton;
using System;
using System.Collections.Generic;
using IndexReader = Lucene.Net.Index.IndexReader;
using Term = Lucene.Net.Index.Term;

namespace Lucene.Net.Search
{

    /// <summary>Subclass of FilteredTermEnum for enumerating all terms that are similiar
    /// to the specified filter term.
    /// 
    /// <p/>Term enumerations are always ordered by Term.compareTo().  Each term in
    /// the enumeration is greater than all that precede it.
    /// </summary>
    public sealed class FuzzyTermsEnum : TermsEnum
    {
        private TermsEnum actualEnum;
        private IBoostAttribute actualBoostAtt;

        private readonly IBoostAttribute boostAtt; // = Attributes.AddAttribute<IBoostAttribute>();

        private readonly IMaxNonCompetitiveBoostAttribute maxBoostAtt;
        private readonly ILevenshteinAutomataAttribute dfaAtt;

        private float bottom;
        private BytesRef bottomTerm;

        // TODO: chicken-and-egg
        private readonly IComparer<BytesRef> termComparator = BytesRef.UTF8SortedAsUnicodeComparer;

        protected readonly float minSimilarity;
        protected readonly float scale_factor;

        protected readonly int termLength;

        protected int maxEdits;
        protected readonly bool raw;

        protected readonly Terms terms;
        private readonly Term term;
        protected readonly int[] termText;
        protected readonly int realPrefixLength;

        private readonly bool transpositions;

        public FuzzyTermsEnum(Terms terms, AttributeSource atts, Term term, float minSimilarity, int prefixLength, bool transpositions)
        {
            // .NET Port: couldn't inline this like in java
            boostAtt = Attributes.AddAttribute<IBoostAttribute>();

            if (minSimilarity >= 1.0f && minSimilarity != (int)minSimilarity)
                throw new ArgumentException("fractional edit distances are not allowed");
            if (minSimilarity < 0.0f)
                throw new ArgumentException("minimumSimilarity cannot be less than 0");
            if (prefixLength < 0)
                throw new ArgumentException("prefixLength cannot be less than 0");
            this.terms = terms;
            this.term = term;

            // convert the string into a utf32 int[] representation for fast comparisons
            string utf16 = term.Text;
            this.termText = new int[utf16.Length];
            for (int cp, i = 0, j = 0; i < utf16.Length; i += 1)
                termText[j++] = cp = utf16[i];
            this.termLength = termText.Length;
            this.dfaAtt = atts.AddAttribute<ILevenshteinAutomataAttribute>();

            //The prefix could be longer than the word.
            //It's kind of silly though.  It means we must match the entire word.
            this.realPrefixLength = prefixLength > termLength ? termLength : prefixLength;
            // if minSimilarity >= 1, we treat it as number of edits
            if (minSimilarity >= 1f)
            {
                this.minSimilarity = 0; // just driven by number of edits
                maxEdits = (int)minSimilarity;
                raw = true;
            }
            else
            {
                this.minSimilarity = minSimilarity;
                // calculate the maximum k edits for this similarity
                maxEdits = InitialMaxDistance(this.minSimilarity, termLength);
                raw = false;
            }
            if (transpositions && maxEdits > LevenshteinAutomata.MAXIMUM_SUPPORTED_DISTANCE)
            {
                throw new NotSupportedException("with transpositions enabled, distances > "
                  + LevenshteinAutomata.MAXIMUM_SUPPORTED_DISTANCE + " are not supported ");
            }
            this.transpositions = transpositions;
            this.scale_factor = 1.0f / (1.0f - this.minSimilarity);

            this.maxBoostAtt = atts.AddAttribute<IMaxNonCompetitiveBoostAttribute>();
            bottom = maxBoostAtt.MaxNonCompetitiveBoost;
            bottomTerm = maxBoostAtt.CompetitiveTerm;
            BottomChanged(null, true);
        }

        protected TermsEnum GetAutomatonEnum(int editDistance, BytesRef lastTerm)
        {
            List<CompiledAutomaton> runAutomata = InitAutomata(editDistance);
            if (editDistance < runAutomata.Count)
            {
                //if (BlockTreeTermsWriter.DEBUG) System.out.println("FuzzyTE.getAEnum: ed=" + editDistance + " lastTerm=" + (lastTerm==null ? "null" : lastTerm.utf8ToString()));
                CompiledAutomaton compiled = runAutomata[editDistance];
                return new AutomatonFuzzyTermsEnum(this, terms.Intersect(compiled, lastTerm == null ? null : compiled.Floor(lastTerm, new BytesRef())),
                                                   runAutomata.GetRange(0, editDistance + 1).ToArray());
            }
            else
            {
                return null;
            }
        }

        private List<CompiledAutomaton> InitAutomata(int maxDistance)
        {
            List<CompiledAutomaton> runAutomata = dfaAtt.Automata;
            //System.out.println("cached automata size: " + runAutomata.size());
            if (runAutomata.Count <= maxDistance &&
                maxDistance <= LevenshteinAutomata.MAXIMUM_SUPPORTED_DISTANCE)
            {
                LevenshteinAutomata builder =
                  new LevenshteinAutomata(UnicodeUtil.NewString(termText, realPrefixLength, termText.Length - realPrefixLength), transpositions);

                for (int i = runAutomata.Count; i <= maxDistance; i++)
                {
                    Automaton a = builder.ToAutomaton(i);
                    //System.out.println("compute automaton n=" + i);
                    // constant prefix
                    if (realPrefixLength > 0)
                    {
                        Automaton prefix = BasicAutomata.MakeString(
                          UnicodeUtil.NewString(termText, 0, realPrefixLength));
                        a = BasicOperations.Concatenate(prefix, a);
                    }
                    runAutomata.Add(new CompiledAutomaton(a, true, false));
                }
            }
            return runAutomata;
        }

        protected void SetEnum(TermsEnum actualEnum)
        {
            this.actualEnum = actualEnum;
            this.actualBoostAtt = actualEnum.Attributes.AddAttribute<IBoostAttribute>();
        }

        private void BottomChanged(BytesRef lastTerm, bool init)
        {
            int oldMaxEdits = maxEdits;

            // true if the last term encountered is lexicographically equal or after the bottom term in the PQ
            bool termAfter = bottomTerm == null || (lastTerm != null && termComparator.Compare(lastTerm, bottomTerm) >= 0);

            // as long as the max non-competitive boost is >= the max boost
            // for some edit distance, keep dropping the max edit distance.
            while (maxEdits > 0 && (termAfter ? bottom >= CalculateMaxBoost(maxEdits) : bottom > CalculateMaxBoost(maxEdits)))
                maxEdits--;

            if (oldMaxEdits != maxEdits || init)
            { // the maximum n has changed
                MaxEditDistanceChanged(lastTerm, maxEdits, init);
            }
        }

        protected void MaxEditDistanceChanged(BytesRef lastTerm, int maxEdits, bool init)
        {
            TermsEnum newEnum = GetAutomatonEnum(maxEdits, lastTerm);
            // instead of assert, we do a hard check in case someone uses our enum directly
            // assert newEnum != null;
            if (newEnum == null)
            {
                //assert maxEdits > LevenshteinAutomata.MAXIMUM_SUPPORTED_DISTANCE;
                throw new ArgumentException("maxEdits cannot be > LevenshteinAutomata.MAXIMUM_SUPPORTED_DISTANCE");
            }
            SetEnum(newEnum);
        }

        private int InitialMaxDistance(float minimumSimilarity, int termLen)
        {
            return (int)((1D - minimumSimilarity) * termLen);
        }

        private float CalculateMaxBoost(int nEdits)
        {
            float similarity = 1.0f - ((float)nEdits / (float)(termLength));
            return (similarity - minSimilarity) * scale_factor;
        }

        private BytesRef queuedBottom = null;

        public override BytesRef Next()
        {
            if (queuedBottom != null)
            {
                BottomChanged(queuedBottom, false);
                queuedBottom = null;
            }

            BytesRef term = actualEnum.Next();
            boostAtt.Boost = actualBoostAtt.Boost;

            float bottom = maxBoostAtt.MaxNonCompetitiveBoost;
            BytesRef bottomTerm = maxBoostAtt.CompetitiveTerm;
            if (term != null && (bottom != this.bottom || bottomTerm != this.bottomTerm))
            {
                this.bottom = bottom;
                this.bottomTerm = bottomTerm;
                // clone the term before potentially doing something with it
                // this is a rare but wonderful occurrence anyway
                queuedBottom = BytesRef.DeepCopyOf(term);
            }

            return term;
        }

        public override int DocFreq
        {
            get { return actualEnum.DocFreq; }
        }

        public override long TotalTermFreq
        {
            get { return actualEnum.TotalTermFreq; }
        }

        public override DocsEnum Docs(IBits liveDocs, DocsEnum reuse, int flags)
        {
            return actualEnum.Docs(liveDocs, reuse, flags);
        }

        public override DocsAndPositionsEnum DocsAndPositions(IBits liveDocs, DocsAndPositionsEnum reuse, int flags)
        {
            return actualEnum.DocsAndPositions(liveDocs, reuse, flags);
        }

        public override void SeekExact(BytesRef term, TermState state)
        {
            actualEnum.SeekExact(term, state);
        }

        public override TermState TermState
        {
            get
            {
                return actualEnum.TermState;
            }
        }

        public override IComparer<BytesRef> Comparator
        {
            get { return actualEnum.Comparator; }
        }

        public override long Ord
        {
            get { return actualEnum.Ord; }
        }

        public override bool SeekExact(BytesRef text, bool useCache)
        {
            return actualEnum.SeekExact(text, useCache);
        }

        public override SeekStatus SeekCeil(BytesRef text, bool useCache)
        {
            return actualEnum.SeekCeil(text, useCache);
        }

        public override void SeekExact(long ord)
        {
            actualEnum.SeekExact(ord);
        }

        public override BytesRef Term
        {
            get { return actualEnum.Term; }
        }

        private class AutomatonFuzzyTermsEnum : FilteredTermsEnum
        {
            private readonly FuzzyTermsEnum parent;

            private readonly ByteRunAutomaton[] matchers;

            private readonly BytesRef termRef;

            private readonly IBoostAttribute boostAtt; // = Attributes.AddAttribute<IBoostAttribute>();

            public AutomatonFuzzyTermsEnum(FuzzyTermsEnum parent, TermsEnum tenum, CompiledAutomaton[] compiled)
                : base(tenum, false)
            {
                // .NET Port: couldn't inline this
                this.boostAtt = Attributes.AddAttribute<IBoostAttribute>();
                this.parent = parent;

                this.matchers = new ByteRunAutomaton[compiled.Length];
                for (int i = 0; i < compiled.Length; i++)
                    this.matchers[i] = compiled[i].runAutomaton;
                termRef = new BytesRef(parent.term.Text);
            }

            protected override AcceptStatus Accept(BytesRef term)
            {
                //System.out.println("AFTE.accept term=" + term);
                int ed = matchers.Length - 1;

                // we are wrapping either an intersect() TermsEnum or an AutomatonTermsENum,
                // so we know the outer DFA always matches.
                // now compute exact edit distance
                while (ed > 0)
                {
                    if (Matches(term, ed - 1))
                    {
                        ed--;
                    }
                    else
                    {
                        break;
                    }
                }
                //System.out.println("CHECK term=" + term.utf8ToString() + " ed=" + ed);

                // scale to a boost and return (if similarity > minSimilarity)
                if (ed == 0)
                { // exact match
                    boostAtt.Boost = 1.0F;
                    //System.out.println("  yes");
                    return AcceptStatus.YES;
                }
                else
                {
                    int codePointCount = UnicodeUtil.CodePointCount(term);
                    float similarity = 1.0f - ((float)ed / (float)
                        (Math.Min(codePointCount, parent.termLength)));
                    if (similarity > parent.minSimilarity)
                    {
                        boostAtt.Boost = (similarity - parent.minSimilarity) * parent.scale_factor;
                        //System.out.println("  yes");
                        return AcceptStatus.YES;
                    }
                    else
                    {
                        return AcceptStatus.NO;
                    }
                }
            }

            internal bool Matches(BytesRef term, int k)
            {
                return k == 0 ? term.Equals(termRef) : matchers[k].Run(term.bytes, term.offset, term.length);
            }
        }

        public float MinSimilarity
        {
            get { return minSimilarity; }
        }

        public float ScaleFactor
        {
            get { return scale_factor; }
        }

        public interface ILevenshteinAutomataAttribute : Lucene.Net.Util.IAttribute
        {
            // .NET Port: using List<T> instead of IList<T> for GetRange support.
            List<CompiledAutomaton> Automata { get; }
        }

        public sealed class LevenshteinAutomataAttributeImpl : Lucene.Net.Util.Attribute, ILevenshteinAutomataAttribute
        {
            private readonly List<CompiledAutomaton> automata = new List<CompiledAutomaton>();

            public List<CompiledAutomaton> Automata
            {
                get { return automata; }
            }

            public override void Clear()
            {
                automata.Clear();
            }

            public override int GetHashCode()
            {
                return automata.GetHashCode();
            }

            public override bool Equals(object other)
            {
                if (this == other)
                    return true;
                if (!(other is LevenshteinAutomataAttributeImpl))
                    return false;
                return automata.Equals(((LevenshteinAutomataAttributeImpl)other).automata);
            }

            public override void CopyTo(Util.Attribute target)
            {
                List<CompiledAutomaton> targetAutomata =
                    ((ILevenshteinAutomataAttribute)target).Automata;
                targetAutomata.Clear();
                targetAutomata.AddRange(automata);
            }
        }
    }
}