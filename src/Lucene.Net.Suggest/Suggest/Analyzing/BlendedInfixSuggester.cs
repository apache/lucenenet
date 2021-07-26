using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using JCG = J2N.Collections.Generic;
using Directory = Lucene.Net.Store.Directory;
using Lucene.Net.Diagnostics;

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

    // TODO:
    // - allow to use the search score

    /// <summary>
    /// Extension of the <see cref="AnalyzingInfixSuggester"/> which transforms the weight
    /// after search to take into account the position of the searched term into
    /// the indexed text.
    /// Please note that it increases the number of elements searched and applies the
    /// ponderation after. It might be costly for long suggestions.
    ///
    /// @lucene.experimental
    /// </summary>
    public class BlendedInfixSuggester : AnalyzingInfixSuggester
    {

        /// <summary>
        /// Coefficient used for linear blending
        /// </summary>
        protected internal static double LINEAR_COEF = 0.10;

        /// <summary>
        /// Default factor
        /// </summary>
        public static int DEFAULT_NUM_FACTOR = 10;

        /// <summary>
        /// Factor to multiply the number of searched elements
        /// </summary>
        private readonly int numFactor;

        /// <summary>
        /// Type of blender used by the suggester
        /// </summary>
        private readonly BlenderType blenderType;

        /// <summary>
        /// The different types of blender.
        /// </summary>
        public enum BlenderType
        {
            /// <summary>
            /// Application dependent; override <see cref="CalculateCoefficient(int)"/>
            /// to compute it.
            /// </summary>
            CUSTOM,
            /// <summary>
            /// weight*(1 - 0.10*position)
            /// </summary>
            POSITION_LINEAR,
            /// <summary>
            /// weight/(1+position)
            /// </summary>
            POSITION_RECIPROCAL,
            // TODO:
            //SCORE
        }

        /// <summary>
        /// Create a new instance, loading from a previously built
        /// directory, if it exists.
        /// </summary>
        public BlendedInfixSuggester(LuceneVersion matchVersion, Directory dir, Analyzer analyzer)
            : base(matchVersion, dir, analyzer)
        {
            this.blenderType = BlenderType.POSITION_LINEAR;
            this.numFactor = DEFAULT_NUM_FACTOR;
        }

        /// <summary>
        /// Create a new instance, loading from a previously built
        /// directory, if it exists.
        /// </summary>
        /// <param name="blenderType"> Type of blending strategy, see BlenderType for more precisions </param>
        /// <param name="numFactor">   Factor to multiply the number of searched elements before ponderate </param>
        /// <exception cref="IOException"> If there are problems opening the underlying Lucene index. </exception>
        // LUCENENET specific - LUCENE-5889, a 4.11.0 feature. calls new constructor with extra param.
        // LUCENENET TODO: Remove method at version 4.11.0. Was retained for perfect 4.8 compatibility
        public BlendedInfixSuggester(LuceneVersion matchVersion, Directory dir, Analyzer indexAnalyzer, Analyzer queryAnalyzer, int minPrefixChars,
                                     BlenderType blenderType, int numFactor)
            : this(matchVersion, dir, indexAnalyzer, queryAnalyzer, minPrefixChars, blenderType, numFactor, commitOnBuild: false)
        {
        }

        /// <summary>
        /// Create a new instance, loading from a previously built
        /// directory, if it exists.
        /// </summary>
        /// <param name="blenderType"> Type of blending strategy, see BlenderType for more precisions </param>
        /// <param name="numFactor">   Factor to multiply the number of searched elements before ponderate </param>
        ///  <param name="commitOnBuild"> Call commit after the index has finished building. This
        ///  would persist the suggester index to disk and future instances of this suggester can
        ///  use this pre-built dictionary. </param>
        /// <exception cref="IOException"> If there are problems opening the underlying Lucene index. </exception>
        // LUCENENET specific - LUCENE-5889, a 4.11.0 feature. (Code moved from other constructor to here.)
        public BlendedInfixSuggester(LuceneVersion matchVersion, Directory dir, Analyzer indexAnalyzer, Analyzer queryAnalyzer, int minPrefixChars,
                                     BlenderType blenderType, int numFactor, bool commitOnBuild)
            : base(matchVersion, dir, indexAnalyzer, queryAnalyzer, minPrefixChars, commitOnBuild)
        {
            this.blenderType = blenderType;
            this.numFactor = numFactor;
        }

        public override IList<Lookup.LookupResult> DoLookup(string key, IEnumerable<BytesRef> contexts, bool onlyMorePopular, int num)
        {
            // here we multiply the number of searched element by the defined factor
            return base.DoLookup(key, contexts, onlyMorePopular, num * numFactor);
        }

        public override IList<Lookup.LookupResult> DoLookup(string key, IEnumerable<BytesRef> contexts, int num, bool allTermsRequired, bool doHighlight)
        {
            // here we multiply the number of searched element by the defined factor
            return base.DoLookup(key, contexts, num * numFactor, allTermsRequired, doHighlight);
        }

        protected override FieldType GetTextFieldType()
        {
            FieldType ft = new FieldType(TextField.TYPE_NOT_STORED);
            ft.IndexOptions = IndexOptions.DOCS_AND_FREQS_AND_POSITIONS;
            ft.StoreTermVectors = true;
            ft.StoreTermVectorPositions = true;
            ft.OmitNorms = true;

            return ft;
        }

        protected internal override IList<Lookup.LookupResult> CreateResults(IndexSearcher searcher, TopFieldDocs hits,
            int num, string key, bool doHighlight, ICollection<string> matchedTokens, string prefixToken)
        {

            BinaryDocValues textDV = MultiDocValues.GetBinaryValues(searcher.IndexReader, TEXT_FIELD_NAME);
            if (Debugging.AssertsEnabled) Debugging.Assert(textDV != null);

            // This will just be null if app didn't pass payloads to build():
            // TODO: maybe just stored fields?  they compress...
            BinaryDocValues payloadsDV = MultiDocValues.GetBinaryValues(searcher.IndexReader, "payloads");

            JCG.SortedSet<Lookup.LookupResult> results = new JCG.SortedSet<Lookup.LookupResult>(LOOKUP_COMP);

            // we reduce the num to the one initially requested
            int actualNum = num / numFactor;

            BytesRef scratch = new BytesRef();
            for (int i = 0; i < hits.ScoreDocs.Length; i++)
            {
                FieldDoc fd = (FieldDoc)hits.ScoreDocs[i];

                textDV.Get(fd.Doc, scratch);
                string text = scratch.Utf8ToString();
                long weight = (J2N.Numerics.Int64)fd.Fields[0];

                BytesRef payload;
                if (payloadsDV != null)
                {
                    payload = new BytesRef();
                    payloadsDV.Get(fd.Doc, payload);
                }
                else
                {
                    payload = null;
                }

                double coefficient;
                if (text.StartsWith(key.ToString(), StringComparison.Ordinal))
                {
                    // if hit starts with the key, we don't change the score
                    coefficient = 1;
                }
                else
                {
                    coefficient = CreateCoefficient(searcher, fd.Doc, matchedTokens, prefixToken);
                }

                long score = (long)(weight * coefficient);

                LookupResult result;
                if (doHighlight)
                {
                    object highlightKey = Highlight(text, matchedTokens, prefixToken);
                    result = new LookupResult(highlightKey.ToString(), highlightKey, score, payload);
                }
                else
                {
                    result = new LookupResult(text, score, payload);
                }

                BoundedTreeAdd(results, result, actualNum);
            }

            return new JCG.List<LookupResult>(results.Reverse());
        }

        /// <summary>
        /// Add an element to the tree respecting a size limit
        /// </summary>
        /// <param name="results"> the tree to add in </param>
        /// <param name="result"> the result we try to add </param>
        /// <param name="num"> size limit </param>
        private static void BoundedTreeAdd(JCG.SortedSet<Lookup.LookupResult> results, Lookup.LookupResult result, int num)
        {
            if (results.Count >= num)
            {
                var first = results.Min; // "get" our first object so we don't cross threads
                if (first.Value < result.Value)
                    // Code similar to the java TreeMap class
                    results.Remove(first);
                else
                    return;
            }

            results.Add(result);
        }

        /// <summary>
        /// Create the coefficient to transform the weight.
        /// </summary>
        /// <param name="doc"> id of the document </param>
        /// <param name="matchedTokens"> tokens found in the query </param>
        /// <param name="prefixToken"> unfinished token in the query </param>
        /// <returns> the coefficient </returns>
        /// <exception cref="IOException"> If there are problems reading term vectors from the underlying Lucene index. </exception>
        private double CreateCoefficient(IndexSearcher searcher, int doc, ICollection<string> matchedTokens, string prefixToken)
        {
            Terms tv = searcher.IndexReader.GetTermVector(doc, TEXT_FIELD_NAME);
            TermsEnum it = tv.GetEnumerator(TermsEnum.EMPTY);

            int position = int.MaxValue;
            // find the closest token position
            while (it.MoveNext())
            {
                string docTerm = it.Term.Utf8ToString();

                if (matchedTokens.Contains(docTerm) || prefixToken != null &&  docTerm.StartsWith(prefixToken, StringComparison.Ordinal))
                {
                    DocsAndPositionsEnum docPosEnum = it.DocsAndPositions(null, null, DocsAndPositionsFlags.OFFSETS);
                    docPosEnum.NextDoc();

                    // use the first occurrence of the term
                    int p = docPosEnum.NextPosition();
                    if (p < position)
                    {
                        position = p;
                    }
                }
            }

            // create corresponding coefficient based on position
            return CalculateCoefficient(position);
        }

        /// <summary>
        /// Calculate the weight coefficient based on the position of the first matching word.
        /// Subclass should override it to adapt it to particular needs </summary>
        /// <param name="position"> of the first matching word in text </param>
        /// <returns> the coefficient </returns>
        protected internal virtual double CalculateCoefficient(int position)
        {
            double coefficient;
            switch (blenderType)
            {
                case BlendedInfixSuggester.BlenderType.POSITION_LINEAR:
                    coefficient = 1 - LINEAR_COEF * position;
                    break;

                case BlendedInfixSuggester.BlenderType.POSITION_RECIPROCAL:
                    coefficient = 1.0 / (position + 1);
                    break;

                default:
                    coefficient = 1;
                    break;
            }

            return coefficient;
        }

        private static readonly IComparer<Lookup.LookupResult> LOOKUP_COMP = new LookUpComparer(); // LUCENENET: marked readonly

        private class LookUpComparer : IComparer<Lookup.LookupResult>
        {
            public virtual int Compare(Lookup.LookupResult o1, Lookup.LookupResult o2)
            {
                // order on weight
                if (o1.Value > o2.Value)
                {
                    return 1;
                }
                else if (o1.Value < o2.Value)
                {
                    return -1;
                }

                // otherwise on alphabetic order
                return CHARSEQUENCE_COMPARER.Compare(o1.Key, o2.Key);
            }
        }
    }
}