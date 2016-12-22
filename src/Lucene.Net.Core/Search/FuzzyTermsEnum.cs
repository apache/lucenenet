using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Lucene.Net.Search
{
    using Lucene.Net.Support;
    using Lucene.Net.Util;
    using Attribute = Lucene.Net.Util.Attribute;
    using AttributeSource = Lucene.Net.Util.AttributeSource;
    using Automaton = Lucene.Net.Util.Automaton.Automaton;
    using BasicAutomata = Lucene.Net.Util.Automaton.BasicAutomata;
    using BasicOperations = Lucene.Net.Util.Automaton.BasicOperations;
    using Bits = Lucene.Net.Util.Bits;
    using ByteRunAutomaton = Lucene.Net.Util.Automaton.ByteRunAutomaton;
    using BytesRef = Lucene.Net.Util.BytesRef;
    using CompiledAutomaton = Lucene.Net.Util.Automaton.CompiledAutomaton;

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

    using DocsAndPositionsEnum = Lucene.Net.Index.DocsAndPositionsEnum;
    using DocsEnum = Lucene.Net.Index.DocsEnum;
    using FilteredTermsEnum = Lucene.Net.Index.FilteredTermsEnum;
    using LevenshteinAutomata = Lucene.Net.Util.Automaton.LevenshteinAutomata;
    using Term = Lucene.Net.Index.Term;
    using Terms = Lucene.Net.Index.Terms;
    using TermsEnum = Lucene.Net.Index.TermsEnum;
    using TermState = Lucene.Net.Index.TermState;
    using UnicodeUtil = Lucene.Net.Util.UnicodeUtil;

    /// <summary>
    /// Subclass of TermsEnum for enumerating all terms that are similar
    /// to the specified filter term.
    ///
    /// <p>Term enumerations are always ordered by
    /// <seealso cref="#getComparator"/>.  Each term in the enumeration is
    /// greater than all that precede it.</p>
    /// </summary>
    public class FuzzyTermsEnum : TermsEnum
    {
        private void InitializeInstanceFields()
        {
            BoostAtt = Attributes.AddAttribute<IBoostAttribute>();
        }

        private TermsEnum ActualEnum;
        private IBoostAttribute ActualBoostAtt;

        private IBoostAttribute BoostAtt;

        private readonly IMaxNonCompetitiveBoostAttribute MaxBoostAtt;
        private readonly ILevenshteinAutomataAttribute DfaAtt;

        private float Bottom;
        private BytesRef BottomTerm;

        // TODO: chicken-and-egg
        private readonly IComparer<BytesRef> TermComparator = BytesRef.UTF8SortedAsUnicodeComparer;

        protected internal readonly float MinSimilarity_Renamed;
        protected internal readonly float Scale_factor;

        protected internal readonly int TermLength;

        protected internal int MaxEdits;
        protected internal readonly bool Raw;

        protected internal readonly Terms Terms;
        private readonly Term Term_Renamed;
        protected internal readonly int[] TermText;
        protected internal readonly int RealPrefixLength;

        private readonly bool Transpositions;

        /// <summary>
        /// Constructor for enumeration of all terms from specified <code>reader</code> which share a prefix of
        /// length <code>prefixLength</code> with <code>term</code> and which have a fuzzy similarity &gt;
        /// <code>minSimilarity</code>.
        /// <p>
        /// After calling the constructor the enumeration is already pointing to the first
        /// valid term if such a term exists.
        /// </summary>
        /// <param name="terms"> Delivers terms. </param>
        /// <param name="atts"> <seealso cref="AttributeSource"/> created by the rewrite method of <seealso cref="MultiTermQuery"/>
        /// thats contains information about competitive boosts during rewrite. It is also used
        /// to cache DFAs between segment transitions. </param>
        /// <param name="term"> Pattern term. </param>
        /// <param name="minSimilarity"> Minimum required similarity for terms from the reader. Pass an integer value
        ///        representing edit distance. Passing a fraction is deprecated. </param>
        /// <param name="prefixLength"> Length of required common prefix. Default value is 0. </param>
        /// <exception cref="IOException"> if there is a low-level IO error </exception>
        public FuzzyTermsEnum(Terms terms, AttributeSource atts, Term term, float minSimilarity, int prefixLength, bool transpositions)
        {
            InitializeInstanceFields();
            if (minSimilarity >= 1.0f && minSimilarity != (int)minSimilarity)
            {
                throw new System.ArgumentException("fractional edit distances are not allowed");
            }
            if (minSimilarity < 0.0f)
            {
                throw new System.ArgumentException("minimumSimilarity cannot be less than 0");
            }
            if (prefixLength < 0)
            {
                throw new System.ArgumentException("prefixLength cannot be less than 0");
            }
            this.Terms = terms;
            this.Term_Renamed = term;

            // convert the string into a utf32 int[] representation for fast comparisons
            string utf16 = term.Text();
            //this.TermText = new int[utf16.codePointCount(0, utf16.Length)];
            this.TermText = new int[Character.CodePointCount(utf16, 0, utf16.Length)];
            for (int cp, i = 0, j = 0; i < utf16.Length; i += Character.CharCount(cp))
            {
                TermText[j++] = cp = Character.CodePointAt(utf16, i);
            }
            this.TermLength = TermText.Length;
            this.DfaAtt = atts.AddAttribute<ILevenshteinAutomataAttribute>();

            //The prefix could be longer than the word.
            //It's kind of silly though.  It means we must match the entire word.
            this.RealPrefixLength = prefixLength > TermLength ? TermLength : prefixLength;
            // if minSimilarity >= 1, we treat it as number of edits
            if (minSimilarity >= 1f)
            {
                this.MinSimilarity_Renamed = 0; // just driven by number of edits
                MaxEdits = (int)minSimilarity;
                Raw = true;
            }
            else
            {
                this.MinSimilarity_Renamed = minSimilarity;
                // calculate the maximum k edits for this similarity
                MaxEdits = InitialMaxDistance(this.MinSimilarity_Renamed, TermLength);
                Raw = false;
            }
            if (transpositions && MaxEdits > LevenshteinAutomata.MAXIMUM_SUPPORTED_DISTANCE)
            {
                throw new System.NotSupportedException("with transpositions enabled, distances > " + LevenshteinAutomata.MAXIMUM_SUPPORTED_DISTANCE + " are not supported ");
            }
            this.Transpositions = transpositions;
            this.Scale_factor = 1.0f / (1.0f - this.MinSimilarity_Renamed);

            this.MaxBoostAtt = atts.AddAttribute<IMaxNonCompetitiveBoostAttribute>();
            Bottom = MaxBoostAtt.MaxNonCompetitiveBoost;
            BottomTerm = MaxBoostAtt.CompetitiveTerm;
            BottomChanged(null, true);
        }

        /// <summary>
        /// return an automata-based enum for matching up to editDistance from
        /// lastTerm, if possible
        /// </summary>
        protected internal virtual TermsEnum GetAutomatonEnum(int editDistance, BytesRef lastTerm)
        {
            IList<CompiledAutomaton> runAutomata = InitAutomata(editDistance);
            if (editDistance < runAutomata.Count)
            {
                //if (BlockTreeTermsWriter.DEBUG) System.out.println("FuzzyTE.getAEnum: ed=" + editDistance + " lastTerm=" + (lastTerm==null ? "null" : lastTerm.utf8ToString()));
                CompiledAutomaton compiled = runAutomata[editDistance];
                return new AutomatonFuzzyTermsEnum(this, Terms.Intersect(compiled, lastTerm == null ? null : compiled.Floor(lastTerm, new BytesRef())), runAutomata.SubList(0, editDistance + 1).ToArray(/*new CompiledAutomaton[editDistance + 1]*/));
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// initialize levenshtein DFAs up to maxDistance, if possible </summary>
        private IList<CompiledAutomaton> InitAutomata(int maxDistance)
        {
            IList<CompiledAutomaton> runAutomata = DfaAtt.Automata();
            //System.out.println("cached automata size: " + runAutomata.size());
            if (runAutomata.Count <= maxDistance && maxDistance <= LevenshteinAutomata.MAXIMUM_SUPPORTED_DISTANCE)
            {
                LevenshteinAutomata builder = new LevenshteinAutomata(UnicodeUtil.NewString(TermText, RealPrefixLength, TermText.Length - RealPrefixLength), Transpositions);

                for (int i = runAutomata.Count; i <= maxDistance; i++)
                {
                    Automaton a = builder.ToAutomaton(i);
                    //System.out.println("compute automaton n=" + i);
                    // constant prefix
                    if (RealPrefixLength > 0)
                    {
                        Automaton prefix = BasicAutomata.MakeString(UnicodeUtil.NewString(TermText, 0, RealPrefixLength));
                        a = BasicOperations.Concatenate(prefix, a);
                    }
                    runAutomata.Add(new CompiledAutomaton(a, true, false));
                }
            }
            return runAutomata;
        }

        /// <summary>
        /// swap in a new actual enum to proxy to </summary>
        protected internal virtual TermsEnum Enum
        {
            set
            {
                this.ActualEnum = value;
                this.ActualBoostAtt = value.Attributes.AddAttribute<IBoostAttribute>();
            }
        }

        /// <summary>
        /// fired when the max non-competitive boost has changed. this is the hook to
        /// swap in a smarter actualEnum
        /// </summary>
        private void BottomChanged(BytesRef lastTerm, bool init)
        {
            int oldMaxEdits = MaxEdits;

            // true if the last term encountered is lexicographically equal or after the bottom term in the PQ
            bool termAfter = BottomTerm == null || (lastTerm != null && TermComparator.Compare(lastTerm, BottomTerm) >= 0);

            // as long as the max non-competitive boost is >= the max boost
            // for some edit distance, keep dropping the max edit distance.
            while (MaxEdits > 0 && (termAfter ? Bottom >= CalculateMaxBoost(MaxEdits) : Bottom > CalculateMaxBoost(MaxEdits)))
            {
                MaxEdits--;
            }

            if (oldMaxEdits != MaxEdits || init) // the maximum n has changed
            {
                MaxEditDistanceChanged(lastTerm, MaxEdits, init);
            }
        }

        protected internal virtual void MaxEditDistanceChanged(BytesRef lastTerm, int maxEdits, bool init)
        {
            TermsEnum newEnum = GetAutomatonEnum(maxEdits, lastTerm);
            // instead of assert, we do a hard check in case someone uses our enum directly
            // assert newEnum != null;
            if (newEnum == null)
            {
                Debug.Assert(maxEdits > LevenshteinAutomata.MAXIMUM_SUPPORTED_DISTANCE);
                throw new System.ArgumentException("maxEdits cannot be > LevenshteinAutomata.MAXIMUM_SUPPORTED_DISTANCE");
            }
            Enum = newEnum;
        }

        // for some raw min similarity and input term length, the maximum # of edits
        private int InitialMaxDistance(float minimumSimilarity, int termLen)
        {
            return (int)((1D - minimumSimilarity) * termLen);
        }

        // for some number of edits, the maximum possible scaled boost
        private float CalculateMaxBoost(int nEdits)
        {
            float similarity = 1.0f - ((float)nEdits / (float)(TermLength));
            return (similarity - MinSimilarity_Renamed) * Scale_factor;
        }

        private BytesRef QueuedBottom = null;

        public override BytesRef Next()
        {
            if (QueuedBottom != null)
            {
                BottomChanged(QueuedBottom, false);
                QueuedBottom = null;
            }

            BytesRef term = ActualEnum.Next();
            BoostAtt.Boost = ActualBoostAtt.Boost;

            float bottom = MaxBoostAtt.MaxNonCompetitiveBoost;
            BytesRef bottomTerm = MaxBoostAtt.CompetitiveTerm;
            if (term != null && (bottom != this.Bottom || bottomTerm != this.BottomTerm))
            {
                this.Bottom = bottom;
                this.BottomTerm = bottomTerm;
                // clone the term before potentially doing something with it
                // this is a rare but wonderful occurrence anyway
                QueuedBottom = BytesRef.DeepCopyOf(term);
            }

            return term;
        }

        // proxy all other enum calls to the actual enum
        public override int DocFreq()
        {
            return ActualEnum.DocFreq();
        }

        public override long TotalTermFreq()
        {
            return ActualEnum.TotalTermFreq();
        }

        public override DocsEnum Docs(Bits liveDocs, DocsEnum reuse, int flags)
        {
            return ActualEnum.Docs(liveDocs, reuse, flags);
        }

        public override DocsAndPositionsEnum DocsAndPositions(Bits liveDocs, DocsAndPositionsEnum reuse, int flags)
        {
            return ActualEnum.DocsAndPositions(liveDocs, reuse, flags);
        }

        public override void SeekExact(BytesRef term, TermState state)
        {
            ActualEnum.SeekExact(term, state);
        }

        public override TermState TermState()
        {
            return ActualEnum.TermState();
        }

        public override IComparer<BytesRef> Comparator
        {
            get
            {
                return ActualEnum.Comparator;
            }
        }

        public override long Ord()
        {
            return ActualEnum.Ord();
        }

        public override bool SeekExact(BytesRef text)
        {
            return ActualEnum.SeekExact(text);
        }

        public override SeekStatus SeekCeil(BytesRef text)
        {
            return ActualEnum.SeekCeil(text);
        }

        public override void SeekExact(long ord)
        {
            ActualEnum.SeekExact(ord);
        }

        public override BytesRef Term
        {
            get { return ActualEnum.Term; }
        }

        /// <summary>
        /// Implement fuzzy enumeration with Terms.intersect.
        /// <p>
        /// this is the fastest method as opposed to LinearFuzzyTermsEnum:
        /// as enumeration is logarithmic to the number of terms (instead of linear)
        /// and comparison is linear to length of the term (rather than quadratic)
        /// </summary>
        private class AutomatonFuzzyTermsEnum : FilteredTermsEnum
        {
            internal virtual void InitializeInstanceFields()
            {
                BoostAtt = Attributes.AddAttribute<IBoostAttribute>();
            }

            private readonly FuzzyTermsEnum OuterInstance;

            internal readonly ByteRunAutomaton[] Matchers;

            internal readonly BytesRef TermRef;

            internal IBoostAttribute BoostAtt;

            public AutomatonFuzzyTermsEnum(FuzzyTermsEnum outerInstance, TermsEnum tenum, CompiledAutomaton[] compiled)
                : base(tenum, false)
            {
                this.OuterInstance = outerInstance;

                InitializeInstanceFields();
                this.Matchers = new ByteRunAutomaton[compiled.Length];
                for (int i = 0; i < compiled.Length; i++)
                {
                    this.Matchers[i] = compiled[i].RunAutomaton;
                }
                TermRef = new BytesRef(outerInstance.Term_Renamed.Text());
            }

            /// <summary>
            /// finds the smallest Lev(n) DFA that accepts the term. </summary>
            protected override AcceptStatus Accept(BytesRef term)
            {
                //System.out.println("AFTE.accept term=" + term);
                int ed = Matchers.Length - 1;

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
                    BoostAtt.Boost = 1.0F;
                    //System.out.println("  yes");
                    return AcceptStatus.YES;
                }
                else
                {
                    int codePointCount = UnicodeUtil.CodePointCount(term);
                    float similarity = 1.0f - ((float)ed / (float)(Math.Min(codePointCount, OuterInstance.TermLength)));
                    if (similarity > OuterInstance.MinSimilarity_Renamed)
                    {
                        BoostAtt.Boost = (similarity - OuterInstance.MinSimilarity_Renamed) * OuterInstance.Scale_factor;
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
            /// returns true if term is within k edits of the query term </summary>
            internal bool Matches(BytesRef term, int k)
            {
                return k == 0 ? term.Equals(TermRef) : Matchers[k].Run(term.Bytes, term.Offset, term.Length);
            }
        }

        /// <summary>
        /// @lucene.internal </summary>
        public virtual float MinSimilarity
        {
            get
            {
                return MinSimilarity_Renamed;
            }
        }

        /// <summary>
        /// @lucene.internal </summary>
        public virtual float ScaleFactor
        {
            get
            {
                return Scale_factor;
            }
        }

        /// <summary>
        /// reuses compiled automata across different segments,
        /// because they are independent of the index
        /// @lucene.internal
        /// </summary>
        public interface ILevenshteinAutomataAttribute : IAttribute
        {
            IList<CompiledAutomaton> Automata();
        }

        /// <summary>
        /// Stores compiled automata as a list (indexed by edit distance)
        /// @lucene.internal
        /// </summary>
        public sealed class LevenshteinAutomataAttribute : Attribute, ILevenshteinAutomataAttribute
        {
            internal readonly IList<CompiledAutomaton> automata = new List<CompiledAutomaton>();

            public IList<CompiledAutomaton> Automata()
            {
                return automata;
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
                {
                    return true;
                }
                if (!(other is LevenshteinAutomataAttribute))
                {
                    return false;
                }
                return automata.Equals(((LevenshteinAutomataAttribute)other).automata);
            }

            public override void CopyTo(Attribute target)
            {
                IList<CompiledAutomaton> targetAutomata = ((LevenshteinAutomataAttribute)target).Automata();
                targetAutomata.Clear();
                targetAutomata.AddRange(automata);
            }
        }
    }
}