using J2N;
using J2N.Collections.Generic.Extensions;
using Lucene.Net.Diagnostics;
using Lucene.Net.Index;
using Lucene.Net.Support;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using JCG = J2N.Collections.Generic;

namespace Lucene.Net.Search
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

    using Attribute = Lucene.Net.Util.Attribute;
    using AttributeSource = Lucene.Net.Util.AttributeSource;
    using Automaton = Lucene.Net.Util.Automaton.Automaton;
    using BasicAutomata = Lucene.Net.Util.Automaton.BasicAutomata;
    using BasicOperations = Lucene.Net.Util.Automaton.BasicOperations;
    using ByteRunAutomaton = Lucene.Net.Util.Automaton.ByteRunAutomaton;
    using BytesRef = Lucene.Net.Util.BytesRef;
    using CompiledAutomaton = Lucene.Net.Util.Automaton.CompiledAutomaton;
    using DocsAndPositionsEnum = Lucene.Net.Index.DocsAndPositionsEnum;
    using DocsEnum = Lucene.Net.Index.DocsEnum;
    using FilteredTermsEnum = Lucene.Net.Index.FilteredTermsEnum;
    using IBits = Lucene.Net.Util.IBits;
    using LevenshteinAutomata = Lucene.Net.Util.Automaton.LevenshteinAutomata;
    using Term = Lucene.Net.Index.Term;
    using Terms = Lucene.Net.Index.Terms;
    using TermsEnum = Lucene.Net.Index.TermsEnum;
    using TermState = Lucene.Net.Index.TermState;
    using UnicodeUtil = Lucene.Net.Util.UnicodeUtil;

    /// <summary>
    /// Subclass of <see cref="TermsEnum"/> for enumerating all terms that are similar
    /// to the specified filter term.
    ///
    /// <para>Term enumerations are always ordered by
    /// <see cref="Comparer"/>.  Each term in the enumeration is
    /// greater than all that precede it.</para>
    /// </summary>
    public class FuzzyTermsEnum : TermsEnum
    {
        private TermsEnum actualEnum;
        private IBoostAttribute actualBoostAtt;

        private readonly IBoostAttribute boostAtt;

        private readonly IMaxNonCompetitiveBoostAttribute maxBoostAtt;
        private readonly ILevenshteinAutomataAttribute dfaAtt;

        private float bottom;
        private BytesRef bottomTerm;

        // TODO: chicken-and-egg
        private readonly IComparer<BytesRef> termComparer = BytesRef.UTF8SortedAsUnicodeComparer;

        protected readonly float m_minSimilarity;
        protected readonly float m_scaleFactor;

        protected readonly int m_termLength;

        protected int m_maxEdits; 
        protected readonly bool m_raw;

        protected readonly Terms m_terms;
        private readonly Term term;
        protected readonly int[] m_termText;
        protected readonly int m_realPrefixLength;

        private readonly bool transpositions;

        /// <summary>
        /// Constructor for enumeration of all terms from specified <c>reader</c> which share a prefix of
        /// length <paramref name="prefixLength"/> with <paramref name="term"/> and which have a fuzzy similarity &gt;
        /// <paramref name="minSimilarity"/>.
        /// <para/>
        /// After calling the constructor the enumeration is already pointing to the first
        /// valid term if such a term exists.
        /// </summary>
        /// <param name="terms"> Delivers terms. </param>
        /// <param name="atts"> <see cref="AttributeSource"/> created by the rewrite method of <see cref="MultiTermQuery"/>
        /// thats contains information about competitive boosts during rewrite. It is also used
        /// to cache DFAs between segment transitions. </param>
        /// <param name="term"> Pattern term. </param>
        /// <param name="minSimilarity"> Minimum required similarity for terms from the reader. Pass an integer value
        ///        representing edit distance. Passing a fraction is deprecated. </param>
        /// <param name="prefixLength"> Length of required common prefix. Default value is 0. </param>
        /// <param name="transpositions"> Transpositions </param>
        /// <exception cref="IOException"> if there is a low-level IO error </exception>
        public FuzzyTermsEnum(Terms terms, AttributeSource atts, Term term, float minSimilarity, int prefixLength, bool transpositions)
        {
            boostAtt = Attributes.AddAttribute<IBoostAttribute>();
            if (minSimilarity >= 1.0f && minSimilarity != (int)minSimilarity)
            {
                throw new ArgumentException("fractional edit distances are not allowed");
            }
            if (minSimilarity < 0.0f)
            {
                throw new ArgumentOutOfRangeException(nameof(minSimilarity), "minimumSimilarity cannot be less than 0"); // LUCENENET specific - changed from IllegalArgumentException to ArgumentOutOfRangeException (.NET convention)
            }
            if (prefixLength < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(prefixLength), "prefixLength cannot be less than 0"); // LUCENENET specific - changed from IllegalArgumentException to ArgumentOutOfRangeException (.NET convention)
            }
            this.m_terms = terms;
            this.term = term;

            // convert the string into a utf32 int[] representation for fast comparisons
            string utf16 = term.Text;
            this.m_termText = new int[utf16.CodePointCount(0, utf16.Length)];
            for (int cp, i = 0, j = 0; i < utf16.Length; i += Character.CharCount(cp))
            {
                m_termText[j++] = cp = utf16.CodePointAt(i);
            }
            this.m_termLength = m_termText.Length;
            this.dfaAtt = atts.AddAttribute<ILevenshteinAutomataAttribute>();

            //The prefix could be longer than the word.
            //It's kind of silly though.  It means we must match the entire word.
            this.m_realPrefixLength = prefixLength > m_termLength ? m_termLength : prefixLength;
            // if minSimilarity >= 1, we treat it as number of edits
            if (minSimilarity >= 1f)
            {
                this.m_minSimilarity = 0; // just driven by number of edits
                m_maxEdits = (int)minSimilarity;
                m_raw = true;
            }
            else
            {
                this.m_minSimilarity = minSimilarity;
                // calculate the maximum k edits for this similarity
                m_maxEdits = InitialMaxDistance(this.m_minSimilarity, m_termLength);
                m_raw = false;
            }
            if (transpositions && m_maxEdits > LevenshteinAutomata.MAXIMUM_SUPPORTED_DISTANCE)
            {
                throw UnsupportedOperationException.Create("with transpositions enabled, distances > " + LevenshteinAutomata.MAXIMUM_SUPPORTED_DISTANCE + " are not supported ");
            }
            this.transpositions = transpositions;
            this.m_scaleFactor = 1.0f / (1.0f - this.m_minSimilarity);

            this.maxBoostAtt = atts.AddAttribute<IMaxNonCompetitiveBoostAttribute>();
            bottom = maxBoostAtt.MaxNonCompetitiveBoost;
            bottomTerm = maxBoostAtt.CompetitiveTerm;
            BottomChanged(null, true);
        }

        /// <summary>
        /// Return an automata-based enum for matching up to <paramref name="editDistance"/> from
        /// <paramref name="lastTerm"/>, if possible
        /// </summary>
        protected virtual TermsEnum GetAutomatonEnum(int editDistance, BytesRef lastTerm)
        {
            IList<CompiledAutomaton> runAutomata = InitAutomata(editDistance);
            if (editDistance < runAutomata.Count)
            {
                //if (BlockTreeTermsWriter.DEBUG) System.out.println("FuzzyTE.getAEnum: ed=" + editDistance + " lastTerm=" + (lastTerm==null ? "null" : lastTerm.utf8ToString()));
                CompiledAutomaton compiled = runAutomata[editDistance];
                return new AutomatonFuzzyTermsEnum(this, m_terms.Intersect(compiled, lastTerm is null ? null : compiled.Floor(lastTerm, new BytesRef())), runAutomata.GetView(0, editDistance + 1).ToArray(/*new CompiledAutomaton[editDistance + 1]*/)); // LUCENENET: Checked count parameter of GetView()
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Initialize levenshtein DFAs up to maxDistance, if possible </summary>
        private IList<CompiledAutomaton> InitAutomata(int maxDistance)
        {
            IList<CompiledAutomaton> runAutomata = dfaAtt.Automata;
            //System.out.println("cached automata size: " + runAutomata.size());
            if (runAutomata.Count <= maxDistance && maxDistance <= LevenshteinAutomata.MAXIMUM_SUPPORTED_DISTANCE)
            {
                LevenshteinAutomata builder = new LevenshteinAutomata(UnicodeUtil.NewString(m_termText, m_realPrefixLength, m_termText.Length - m_realPrefixLength), transpositions);

                for (int i = runAutomata.Count; i <= maxDistance; i++)
                {
                    Automaton a = builder.ToAutomaton(i);
                    //System.out.println("compute automaton n=" + i);
                    // constant prefix
                    if (m_realPrefixLength > 0)
                    {
                        Automaton prefix = BasicAutomata.MakeString(UnicodeUtil.NewString(m_termText, 0, m_realPrefixLength));
                        a = BasicOperations.Concatenate(prefix, a);
                    }
                    runAutomata.Add(new CompiledAutomaton(a, true, false));
                }
            }
            return runAutomata;
        }

        /// <summary>
        /// Swap in a new actual enum to proxy to </summary>
        protected virtual void SetEnum(TermsEnum actualEnum)
        {
            this.actualEnum = actualEnum;
            this.actualBoostAtt = actualEnum.Attributes.AddAttribute<IBoostAttribute>();
        }

        /// <summary>
        /// Fired when the max non-competitive boost has changed. This is the hook to
        /// swap in a smarter actualEnum
        /// </summary>

        private void BottomChanged(BytesRef lastTerm, bool init)
        {
            int oldMaxEdits = m_maxEdits;

            // true if the last term encountered is lexicographically equal or after the bottom term in the PQ
            bool termAfter = bottomTerm is null || (lastTerm != null && termComparer.Compare(lastTerm, bottomTerm) >= 0);

            // as long as the max non-competitive boost is >= the max boost
            // for some edit distance, keep dropping the max edit distance.

            // LUCENENET specific - compare bits rather than using equality operators to prevent these comparisons from failing in x86 in .NET Framework with optimizations enabled
            while (m_maxEdits > 0 && (termAfter ?
                NumericUtils.SingleToSortableInt32(bottom) >= NumericUtils.SingleToSortableInt32(CalculateMaxBoost(m_maxEdits)) :
                NumericUtils.SingleToSortableInt32(bottom) > NumericUtils.SingleToSortableInt32(CalculateMaxBoost(m_maxEdits))))
            {
                m_maxEdits--;
            }

            if (oldMaxEdits != m_maxEdits || init) // the maximum n has changed
            {
                MaxEditDistanceChanged(lastTerm, m_maxEdits, init);
            }
        }

        protected virtual void MaxEditDistanceChanged(BytesRef lastTerm, int maxEdits, bool init)
        {
            TermsEnum newEnum = GetAutomatonEnum(maxEdits, lastTerm);
            // instead of assert, we do a hard check in case someone uses our enum directly
            // assert newEnum != null;
            if (newEnum is null)
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(maxEdits > LevenshteinAutomata.MAXIMUM_SUPPORTED_DISTANCE);
                throw new ArgumentException("maxEdits cannot be > LevenshteinAutomata.MAXIMUM_SUPPORTED_DISTANCE");
            }
            SetEnum(newEnum);
        }

        // for some raw min similarity and input term length, the maximum # of edits
        private static int InitialMaxDistance(float minimumSimilarity, int termLen) // LUCENENET: CA1822: Mark members as static
        {
            return (int)((1D - minimumSimilarity) * termLen);
        }

        // for some number of edits, the maximum possible scaled boost
        private float CalculateMaxBoost(int nEdits)
        {
            float similarity = 1.0f - ((float)nEdits / (float)(m_termLength));
            return (similarity - m_minSimilarity) * m_scaleFactor;
        }

        private BytesRef queuedBottom = null;

        public override bool MoveNext()
        {
            if (queuedBottom != null)
            {
                BottomChanged(queuedBottom, false);
                queuedBottom = null;
            }

            bool moved = actualEnum.MoveNext();
            boostAtt.Boost = actualBoostAtt.Boost;

            float bottom = maxBoostAtt.MaxNonCompetitiveBoost;
            BytesRef bottomTerm = maxBoostAtt.CompetitiveTerm;
            if (moved && (bottom != this.bottom || bottomTerm != this.bottomTerm))
            {
                this.bottom = bottom;
                this.bottomTerm = bottomTerm;
                // clone the term before potentially doing something with it
                // this is a rare but wonderful occurrence anyway
                queuedBottom = BytesRef.DeepCopyOf(actualEnum.Term);
            }

            return moved && actualEnum.Term != null;
        }

        [Obsolete("Use MoveNext() and Term instead. This method will be removed in 4.8.0 release candidate."), System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public override BytesRef Next()
        {
            if (MoveNext())
                return actualEnum.Term;
            return null;
        }

        // proxy all other enum calls to the actual enum
        public override int DocFreq => actualEnum.DocFreq;

        public override long TotalTermFreq => actualEnum.TotalTermFreq;

        public override DocsEnum Docs(IBits liveDocs, DocsEnum reuse, DocsFlags flags)
        {
            return actualEnum.Docs(liveDocs, reuse, flags);
        }

        public override DocsAndPositionsEnum DocsAndPositions(IBits liveDocs, DocsAndPositionsEnum reuse, DocsAndPositionsFlags flags)
        {
            return actualEnum.DocsAndPositions(liveDocs, reuse, flags);
        }

        public override void SeekExact(BytesRef term, TermState state)
        {
            actualEnum.SeekExact(term, state);
        }

        public override TermState GetTermState()
        {
            return actualEnum.GetTermState();
        }

        public override IComparer<BytesRef> Comparer => actualEnum.Comparer;

        public override long Ord => actualEnum.Ord;

        public override bool SeekExact(BytesRef text)
        {
            return actualEnum.SeekExact(text);
        }

        public override SeekStatus SeekCeil(BytesRef text)
        {
            return actualEnum.SeekCeil(text);
        }

        public override void SeekExact(long ord)
        {
            actualEnum.SeekExact(ord);
        }

        public override BytesRef Term => actualEnum.Term;

        /// <summary>
        /// Implement fuzzy enumeration with <see cref="Terms.Intersect(CompiledAutomaton, BytesRef)"/>.
        /// <para/>
        /// This is the fastest method as opposed to LinearFuzzyTermsEnum:
        /// as enumeration is logarithmic to the number of terms (instead of linear)
        /// and comparison is linear to length of the term (rather than quadratic)
        /// </summary>
        private class AutomatonFuzzyTermsEnum : FilteredTermsEnum
        {
            private readonly FuzzyTermsEnum outerInstance;

            private readonly ByteRunAutomaton[] matchers;

            private readonly BytesRef termRef;

            private readonly IBoostAttribute boostAtt;

            public AutomatonFuzzyTermsEnum(FuzzyTermsEnum outerInstance, TermsEnum tenum, CompiledAutomaton[] compiled)
                : base(tenum, false)
            {
                this.outerInstance = outerInstance;

                boostAtt = Attributes.AddAttribute<IBoostAttribute>();
                this.matchers = new ByteRunAutomaton[compiled.Length];
                for (int i = 0; i < compiled.Length; i++)
                {
                    this.matchers[i] = compiled[i].RunAutomaton;
                }
                termRef = new BytesRef(outerInstance.term.Text);
            }

            /// <summary>
            /// Finds the smallest Lev(n) DFA that accepts the term. </summary>
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
                if (ed == 0) // exact match
                {
                    boostAtt.Boost = 1.0F;
                    //System.out.println("  yes");
                    return AcceptStatus.YES;
                }
                else
                {
                    int codePointCount = UnicodeUtil.CodePointCount(term);
                    float similarity = 1.0f - ((float)ed / (float)(Math.Min(codePointCount, outerInstance.m_termLength)));

                    // LUCENENET specific - compare bits rather than using equality operators to prevent these comparisons from failing in x86 in .NET Framework with optimizations enabled
                    if (NumericUtils.SingleToSortableInt32(similarity) > NumericUtils.SingleToSortableInt32(outerInstance.m_minSimilarity))
                    {
                        boostAtt.Boost = (similarity - outerInstance.m_minSimilarity) * outerInstance.m_scaleFactor;
                        //System.out.println("  yes");
                        return AcceptStatus.YES;
                    }
                    else
                    {
                        return AcceptStatus.NO;
                    }
                }
            }

            /// <summary>
            /// Returns <c>true</c> if <paramref name="term"/> is within <paramref name="k"/> edits of the query term </summary>
            internal bool Matches(BytesRef term, int k)
            {
                return k == 0 ? term.Equals(termRef) : matchers[k].Run(term.Bytes, term.Offset, term.Length);
            }
        }

        /// <summary>
        /// @lucene.internal </summary>
        public virtual float MinSimilarity => m_minSimilarity;

        /// <summary>
        /// @lucene.internal </summary>
        public virtual float ScaleFactor => m_scaleFactor;

        /// <summary>
        /// Reuses compiled automata across different segments,
        /// because they are independent of the index
        /// <para/>
        /// @lucene.internal
        /// </summary>
        public interface ILevenshteinAutomataAttribute : IAttribute
        {
            IList<CompiledAutomaton> Automata { get; }
        }

        /// <summary>
        /// Stores compiled automata as a list (indexed by edit distance)
        /// <para/>
        /// @lucene.internal
        /// </summary>
        public sealed class LevenshteinAutomataAttribute : Attribute, ILevenshteinAutomataAttribute
        {
            // LUCENENET NOTE: Must use JCG.List for Equals and GetHashCode()
            private readonly IList<CompiledAutomaton> automata = new JCG.List<CompiledAutomaton>();

            public IList<CompiledAutomaton> Automata => automata;

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
                {
                    return true;
                }
                if (!(other is LevenshteinAutomataAttribute))
                {
                    return false;
                }
                return automata.Equals(((LevenshteinAutomataAttribute)other).automata);
            }

            public override void CopyTo(IAttribute target) // LUCENENET specific - intentionally expanding target to use IAttribute rather than Attribute
            {
                // LUCENENET: Added guard clauses
                if (target is null)
                    throw new ArgumentNullException(nameof(target));
                if (target is not ILevenshteinAutomataAttribute t)
                    throw new ArgumentException($"Argument type {target.GetType().FullName} must implement {nameof(ILevenshteinAutomataAttribute)}", nameof(target));
                IList<CompiledAutomaton> targetAutomata = t.Automata;
                targetAutomata.Clear();
                targetAutomata.AddRange(automata);
            }
        }
    }
}