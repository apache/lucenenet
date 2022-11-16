using J2N;
using Lucene.Net.Analysis;
using Lucene.Net.Support;
using Lucene.Net.Util;
using Lucene.Net.Util.Automaton;
using Lucene.Net.Util.Fst;
using System;
using System.Collections.Generic;
using Int64 = J2N.Numerics.Int64;

namespace Lucene.Net.Search.Suggest.Analyzing
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
    /// Implements a fuzzy <see cref="AnalyzingSuggester"/>. The similarity measurement is
    /// based on the Damerau-Levenshtein (optimal string alignment) algorithm, though
    /// you can explicitly choose classic Levenshtein by passing <c>false</c>
    /// for the <see cref="transpositions"/> parameter.
    /// <para>
    /// At most, this query will match terms up to <see cref="LevenshteinAutomata.MAXIMUM_SUPPORTED_DISTANCE"/>
    /// edits. Higher distances are not supported.  Note that the
    /// fuzzy distance is measured in "byte space" on the bytes
    /// returned by the <see cref="TokenStream"/>'s <see cref="Analysis.TokenAttributes.ITermToBytesRefAttribute"/>, 
    /// usually UTF8.  By default
    /// the analyzed bytes must be at least 3 <see cref="DEFAULT_MIN_FUZZY_LENGTH"/>
    /// bytes before any edits are
    /// considered.  Furthermore, the first 1 <see cref="DEFAULT_NON_FUZZY_PREFIX"/>
    /// byte is not allowed to be
    /// edited.  We allow up to 1 <see cref="DEFAULT_MAX_EDITS"/>
    /// edit.
    /// If <see cref="unicodeAware"/> parameter in the constructor is set to true, maxEdits,
    /// minFuzzyLength, transpositions and nonFuzzyPrefix are measured in Unicode code 
    /// points (actual letters) instead of bytes. 
    /// 
    /// </para>
    /// <para>
    /// NOTE: This suggester does not boost suggestions that
    /// required no edits over suggestions that did require
    /// edits.  This is a known limitation.
    /// 
    /// </para>
    /// <para>
    /// Note: complex query analyzers can have a significant impact on the lookup
    /// performance. It's recommended to not use analyzers that drop or inject terms
    /// like synonyms to keep the complexity of the prefix intersection low for good
    /// lookup performance. At index time, complex analyzers can safely be used.
    /// </para>
    /// 
    /// @lucene.experimental
    /// </summary>
    public sealed class FuzzySuggester : AnalyzingSuggester
    {
        private readonly int maxEdits;
        private readonly bool transpositions;
        private readonly int nonFuzzyPrefix;
        private readonly int minFuzzyLength;
        private readonly bool unicodeAware;

        /// <summary>
        /// Measure <see cref="maxEdits"/>, <see cref="minFuzzyLength"/>, 
        /// <see cref="transpositions"/>, and <see cref="nonFuzzyPrefix"/> 
        /// parameters in Unicode code points (actual letters)
        /// instead of bytes.
        /// </summary>
        public const bool DEFAULT_UNICODE_AWARE = false;

        /// <summary>
        /// The default minimum length of the key passed to <see cref="Lookup"/>
        /// before any edits are allowed.
        /// </summary>
        public const int DEFAULT_MIN_FUZZY_LENGTH = 3;

        /// <summary>
        /// The default prefix length where edits are not allowed.
        /// </summary>
        public const int DEFAULT_NON_FUZZY_PREFIX = 1;

        /// <summary>
        /// The default maximum number of edits for fuzzy
        /// suggestions.
        /// </summary>
        public const int DEFAULT_MAX_EDITS = 1;

        /// <summary>
        /// The default transposition value passed to <see cref="LevenshteinAutomata"/>
        /// </summary>
        public const bool DEFAULT_TRANSPOSITIONS = true;

        /// <summary>
        /// Creates a <see cref="FuzzySuggester"/> instance initialized with default values.
        /// </summary>
        /// <param name="analyzer"> The <see cref="Analyzer"/> used for this suggester. </param>
        public FuzzySuggester(Analyzer analyzer)
            : this(analyzer, analyzer)
        {
        }

        /// <summary>
        /// Creates a <see cref="FuzzySuggester"/> instance with an index &amp; a query analyzer initialized with default values.
        /// </summary>
        /// <param name="indexAnalyzer">
        ///           <see cref="Analyzer"/> that will be used for analyzing suggestions while building the index. </param>
        /// <param name="queryAnalyzer">
        ///           <see cref="Analyzer"/> that will be used for analyzing query text during lookup </param>
        public FuzzySuggester(Analyzer indexAnalyzer, Analyzer queryAnalyzer)
            : this(indexAnalyzer, queryAnalyzer, SuggesterOptions.EXACT_FIRST | SuggesterOptions.PRESERVE_SEP, 256, -1, true, 
                  DEFAULT_MAX_EDITS, DEFAULT_TRANSPOSITIONS, DEFAULT_NON_FUZZY_PREFIX, 
                  DEFAULT_MIN_FUZZY_LENGTH, DEFAULT_UNICODE_AWARE)
        {
        }

        /// <summary>
        /// Creates a <see cref="FuzzySuggester"/> instance.
        /// </summary>
        /// <param name="indexAnalyzer"> The <see cref="Analyzer"/> that will be used for
        ///        analyzing suggestions while building the index. </param>
        /// <param name="queryAnalyzer"> The <see cref="Analyzer"/> that will be used for
        ///        analyzing query text during lookup </param>
        /// <param name="options"> see <see cref="SuggesterOptions.EXACT_FIRST"/>, <see cref="SuggesterOptions.PRESERVE_SEP"/> </param>
        /// <param name="maxSurfaceFormsPerAnalyzedForm"> Maximum number of
        ///        surface forms to keep for a single analyzed form.
        ///        When there are too many surface forms we discard the
        ///        lowest weighted ones. </param>
        /// <param name="maxGraphExpansions"> Maximum number of graph paths
        ///        to expand from the analyzed form.  Set this to -1 for
        ///        no limit. </param>
        /// <param name="preservePositionIncrements"> Whether position holes should appear in the automaton </param>
        /// <param name="maxEdits"> must be &gt;= 0 and &lt;= <see cref="LevenshteinAutomata.MAXIMUM_SUPPORTED_DISTANCE"/>. </param>
        /// <param name="transpositions"> <c>true</c> if transpositions should be treated as a primitive 
        ///        edit operation. If this is false, comparisons will implement the classic
        ///        Levenshtein algorithm. </param>
        /// <param name="nonFuzzyPrefix"> length of common (non-fuzzy) prefix (see default <see cref="DEFAULT_NON_FUZZY_PREFIX"/> </param>
        /// <param name="minFuzzyLength"> minimum length of lookup key before any edits are allowed (see default <see cref="DEFAULT_MIN_FUZZY_LENGTH"/>) </param>
        /// <param name="unicodeAware"> operate Unicode code points instead of bytes. </param>
        public FuzzySuggester(Analyzer indexAnalyzer, Analyzer queryAnalyzer, SuggesterOptions options, 
            int maxSurfaceFormsPerAnalyzedForm, int maxGraphExpansions, bool preservePositionIncrements, 
            int maxEdits, bool transpositions, int nonFuzzyPrefix, int minFuzzyLength, bool unicodeAware)
            : base(indexAnalyzer, queryAnalyzer, options, maxSurfaceFormsPerAnalyzedForm, maxGraphExpansions, preservePositionIncrements)
        {
            if (maxEdits < 0 || maxEdits > LevenshteinAutomata.MAXIMUM_SUPPORTED_DISTANCE)
            {
                throw new ArgumentOutOfRangeException(nameof(maxEdits), "maxEdits must be between 0 and " + LevenshteinAutomata.MAXIMUM_SUPPORTED_DISTANCE); // LUCENENET specific - changed from IllegalArgumentException to ArgumentOutOfRangeException (.NET convention)
            }
            if (nonFuzzyPrefix < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(nonFuzzyPrefix), "nonFuzzyPrefix must not be >= 0 (got " + nonFuzzyPrefix + ")"); // LUCENENET specific - changed from IllegalArgumentException to ArgumentOutOfRangeException (.NET convention)
            }
            if (minFuzzyLength < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(minFuzzyLength), "minFuzzyLength must not be >= 0 (got " + minFuzzyLength + ")"); // LUCENENET specific - changed from IllegalArgumentException to ArgumentOutOfRangeException (.NET convention)
            }

            this.maxEdits = maxEdits;
            this.transpositions = transpositions;
            this.nonFuzzyPrefix = nonFuzzyPrefix;
            this.minFuzzyLength = minFuzzyLength;
            this.unicodeAware = unicodeAware;
        }

        protected override IList<FSTUtil.Path<PairOutputs<Int64, BytesRef>.Pair>> GetFullPrefixPaths(
            IList<FSTUtil.Path<PairOutputs<Int64, BytesRef>.Pair>> prefixPaths, 
            Automaton lookupAutomaton, 
            FST<PairOutputs<Int64, BytesRef>.Pair> fst)
        {

            // TODO: right now there's no penalty for fuzzy/edits,
            // ie a completion whose prefix matched exactly what the
            // user typed gets no boost over completions that
            // required an edit, which get no boost over completions
            // requiring two edits.  I suspect a multiplicative
            // factor is appropriate (eg, say a fuzzy match must be at
            // least 2X better weight than the non-fuzzy match to
            // "compete") ... in which case I think the wFST needs
            // to be log weights or something ...

            Automaton levA = ConvertAutomaton(ToLevenshteinAutomata(lookupAutomaton));
            /*
              Writer w = new OutputStreamWriter(new FileOutputStream("out.dot"), StandardCharsets.UTF_8);
              w.write(levA.toDot());
              w.close();
              System.out.println("Wrote LevA to out.dot");
            */
            return FSTUtil.IntersectPrefixPaths(levA, fst);
        }

        protected internal override Automaton ConvertAutomaton(Automaton a)
        {
            if (unicodeAware)
            {
                Automaton utf8automaton = (new UTF32ToUTF8()).Convert(a);
                BasicOperations.Determinize(utf8automaton);
                return utf8automaton;
            }
            else
            {
                return a;
            }
        }

        internal override TokenStreamToAutomaton GetTokenStreamToAutomaton()
        {
            var tsta = base.GetTokenStreamToAutomaton();
            tsta.UnicodeArcs = unicodeAware;
            return tsta;
        }

        internal Automaton ToLevenshteinAutomata(Automaton automaton)
        {
            var @ref = SpecialOperations.GetFiniteStrings(automaton, -1);
            Automaton[] subs = new Automaton[@ref.Count];
            int upto = 0;
            foreach (Int32sRef path in @ref)
            {
                if (path.Length <= nonFuzzyPrefix || path.Length < minFuzzyLength)
                {
                    subs[upto] = BasicAutomata.MakeString(path.Int32s, path.Offset, path.Length);
                    upto++;
                }
                else
                {
                    Automaton prefix = BasicAutomata.MakeString(path.Int32s, path.Offset, nonFuzzyPrefix);
                    int[] ints = new int[path.Length - nonFuzzyPrefix];
                    Arrays.Copy(path.Int32s, path.Offset + nonFuzzyPrefix, ints, 0, ints.Length);
                    // TODO: maybe add alphaMin to LevenshteinAutomata,
                    // and pass 1 instead of 0?  We probably don't want
                    // to allow the trailing dedup bytes to be
                    // edited... but then 0 byte is "in general" allowed
                    // on input (but not in UTF8).
                    LevenshteinAutomata lev = new LevenshteinAutomata(ints, unicodeAware ? Character.MaxCodePoint : 255, transpositions);
                    Automaton levAutomaton = lev.ToAutomaton(maxEdits);
                    Automaton combined = BasicOperations.Concatenate(prefix, levAutomaton);
                    combined.IsDeterministic = true; // its like the special case in concatenate itself, except we cloneExpanded already
                    subs[upto] = combined;
                    upto++;
                }
            }

            if (subs.Length == 0)
            {
                // automaton is empty, there is no accepted paths through it
                return BasicAutomata.MakeEmpty(); // matches nothing
            }
            else if (subs.Length == 1)
            {
                // no synonyms or anything: just a single path through the tokenstream
                return subs[0];
            }
            else
            {
                // multiple paths: this is really scary! is it slow?
                // maybe we should not do this and throw UOE?
                Automaton a = BasicOperations.Union(subs);
                // TODO: we could call toLevenshteinAutomata() before det? 
                // this only happens if you have multiple paths anyway (e.g. synonyms)
                BasicOperations.Determinize(a);

                return a;
            }
        }
    }
}