/*
 *
 * Licensed to the Apache Software Foundation (ASF) under one
 * or more contributor license agreements.  See the NOTICE file
 * distributed with this work for additional information
 * regarding copyright ownership.  The ASF licenses this file
 * to you under the Apache License, Version 2.0 (the
 * "License"); you may not use this file except in compliance
 * with the License.  You may obtain a copy of the License at
 *
 *   http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing,
 * software distributed under the License is distributed on an
 * "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
 * KIND, either express or implied.  See the License for the
 * specific language governing permissions and limitations
 * under the License.
 *
*/

using System;
using System.Collections.Generic;
using System.IO;
using Lucene.Net.Analysis;
using Lucene.Net.Index;
using Lucene.Net.Index.Memory;
using Lucene.Net.Queries;
using Lucene.Net.Search.Spans;
using Lucene.Net.Support;
using Lucene.Net.Util;

namespace Lucene.Net.Search.Highlight
{
    /// <summary>
    /// Class used to extract <see cref="WeightedSpanTerm"/>s from a <see cref="Query"/> based on whether 
    /// <see cref="Term"/>s from the <see cref="Query"/> are contained in a supplied <see cref="Analysis.TokenStream"/>.
    /// </summary>
    public class WeightedSpanTermExtractor
    {
        private string fieldName;
        private TokenStream tokenStream;
        private string defaultField;
        private bool expandMultiTermQuery;
        private bool cachedTokenStream;
        private bool wrapToCaching = true;
        private int maxDocCharsToAnalyze;
        private AtomicReader internalReader = null;

        public WeightedSpanTermExtractor()
        {
        }

        public WeightedSpanTermExtractor(string defaultField)
        {
            if (defaultField != null)
            {
                this.defaultField = StringHelper.Intern(defaultField);
            }
        }

        /// <summary>
        /// Fills a <c>Map</c> with <see cref="WeightedSpanTerm"/>s using the terms from the supplied <c>Query</c>.
        /// </summary>
        /// <param name="query">Query to extract Terms from</param>
        /// <param name="terms">Map to place created WeightedSpanTerms in</param>
        private void Extract(Query query, IDictionary<string, WeightedSpanTerm> terms)
        {
            if (query is BooleanQuery)
            {
                IList<BooleanClause> queryClauses = ((BooleanQuery)query).GetClauses();

                for (int i = 0; i < queryClauses.Count; i++)
                {
                    if (!queryClauses[i].Prohibited)
                    {
                        Extract(queryClauses[i].Query, terms);
                    }
                }
            }
            else if (query is PhraseQuery)
            {
                PhraseQuery phraseQuery = (PhraseQuery)query;
                Term[] phraseQueryTerms = phraseQuery.Terms;
                SpanQuery[] clauses = new SpanQuery[phraseQueryTerms.Length];
                for (int i = 0; i < phraseQueryTerms.Length; i++)
                {
                    clauses[i] = new SpanTermQuery(phraseQueryTerms[i]);
                }
                int slop = phraseQuery.Slop;
                int[] positions = phraseQuery.Positions;
                // add largest position increment to slop
                if (positions.Length > 0)
                {
                    int lastPos = positions[0];
                    int largestInc = 0;
                    int sz = positions.Length;
                    for (int i = 1; i < sz; i++)
                    {
                        int pos = positions[i];
                        int inc = pos - lastPos;
                        if (inc > largestInc)
                        {
                            largestInc = inc;
                        }
                        lastPos = pos;
                    }
                    if (largestInc > 1)
                    {
                        slop += largestInc;
                    }
                }

                bool inorder = slop == 0;

                SpanNearQuery sp = new SpanNearQuery(clauses, slop, inorder);
                sp.Boost = query.Boost;
                ExtractWeightedSpanTerms(terms, sp);
            }
            else if (query is TermQuery)
            {
                ExtractWeightedTerms(terms, query);
            }
            else if (query is SpanQuery)
            {
                ExtractWeightedSpanTerms(terms, (SpanQuery)query);
            }
            else if (query is FilteredQuery)
            {
                Extract(((FilteredQuery)query).Query, terms);
            }
            else if (query is ConstantScoreQuery)
            {
                Query q = ((ConstantScoreQuery)query).Query;
                if (q != null)
                {
                    Extract(q, terms);
                }
            }
            else if (query is CommonTermsQuery)
            {
                // specialized since rewriting would change the result query 
                // this query is TermContext sensitive.
                ExtractWeightedTerms(terms, query);
            } 
            else if (query is DisjunctionMaxQuery)
            {
                foreach (var q in ((DisjunctionMaxQuery)query))
                {
                    Extract(q, terms);
                }
            }
            else if (query is MultiPhraseQuery)
            {
                MultiPhraseQuery mpq = (MultiPhraseQuery) query;
                IList<Term[]> termArrays = mpq.TermArrays;
                int[] positions = mpq.Positions;
                if (positions.Length > 0)
                {

                    int maxPosition = positions[positions.Length - 1];
                    for (int i = 0; i < positions.Length - 1; ++i)
                    {
                        if (positions[i] > maxPosition)
                        {
                            maxPosition = positions[i];
                        }
                    }

                    var disjunctLists = new List<SpanQuery>[maxPosition + 1];
                    int distinctPositions = 0;

                    for (int i = 0; i < termArrays.Count; ++i)
                    {
                        Term[] termArray = termArrays[i];
                        List<SpanQuery> disjuncts = disjunctLists[positions[i]];
                        if (disjuncts == null)
                        {
                            disjuncts = (disjunctLists[positions[i]] = new List<SpanQuery>(termArray.Length));
                            ++distinctPositions;
                        }
                        foreach (var term in termArray)
                        {
                            disjuncts.Add(new SpanTermQuery(term));
                        }
                    }

                    int positionGaps = 0;
                    int position = 0;
                    SpanQuery[] clauses = new SpanQuery[distinctPositions];
                    foreach (var disjuncts in disjunctLists)
                    {
                        if (disjuncts != null)
                        {
                            clauses[position++] = new SpanOrQuery(disjuncts.ToArray());
                        }
                        else
                        {
                            ++positionGaps;
                        }
                    }

                    int slop = mpq.Slop;
                    bool inorder = (slop == 0);

                    SpanNearQuery sp = new SpanNearQuery(clauses, slop + positionGaps, inorder);
                    sp.Boost = query.Boost;
                    ExtractWeightedSpanTerms(terms, sp);
                }
            }
            else
            {
                Query origQuery = query;
                if (query is MultiTermQuery)
                {
                    if (!expandMultiTermQuery)
                    {
                        return;
                    }
                    MultiTermQuery copy = (MultiTermQuery) query.Clone();
                    copy.SetRewriteMethod(MultiTermQuery.SCORING_BOOLEAN_QUERY_REWRITE);
                    origQuery = copy;
                }
                IndexReader reader = GetLeafContext().Reader;
                Query rewritten = origQuery.Rewrite(reader);
                if (rewritten != origQuery)
                {
                    // only rewrite once and then flatten again - the rewritten query could have a speacial treatment
                    // if this method is overwritten in a subclass or above in the next recursion
                    Extract(rewritten, terms);
                }
            }
            ExtractUnknownQuery(query, terms);
        }

        protected void ExtractUnknownQuery(Query query,
            IDictionary<string, WeightedSpanTerm> terms)
        {
            // for sub-classing to extract custom queries
        }

        /// <summary>
        /// Fills a <c>Map</c> with <see cref="WeightedSpanTerm"/>s using the terms from the supplied <c>SpanQuery</c>.
        /// </summary>
        /// <param name="terms">Map to place created WeightedSpanTerms in</param>
        /// <param name="spanQuery">SpanQuery to extract Terms from</param>
        private void ExtractWeightedSpanTerms(IDictionary<string, WeightedSpanTerm> terms, SpanQuery spanQuery)
        {
            HashSet<string> fieldNames;

            if (fieldName == null)
            {
                fieldNames = new HashSet<String>();
                CollectSpanQueryFields(spanQuery, fieldNames);
            }
            else
            {
                fieldNames = new HashSet<String>();
                fieldNames.Add(fieldName);
            }
            // To support the use of the default field name
            if (defaultField != null)
            {
                fieldNames.Add(defaultField);
            }

            IDictionary<String, SpanQuery> queries = new HashMap<String, SpanQuery>();

            var nonWeightedTerms = Support.Compatibility.SetFactory.CreateHashSet<Term>();
            bool mustRewriteQuery = MustRewriteQuery(spanQuery);
            if (mustRewriteQuery)
            {
                foreach (String field in fieldNames)
                {
                    SpanQuery rewrittenQuery = (SpanQuery)spanQuery.Rewrite(GetLeafContext().Reader);
                    queries[field] = rewrittenQuery;
                    rewrittenQuery.ExtractTerms(nonWeightedTerms);
                }
            }
            else
            {
                spanQuery.ExtractTerms(nonWeightedTerms);
            }

            List<PositionSpan> spanPositions = new List<PositionSpan>();

            foreach (String field in fieldNames)
            {
                SpanQuery q;
                q = mustRewriteQuery ? queries[field] : spanQuery;

                AtomicReaderContext context = GetLeafContext();
                var termContexts = new HashMap<Term, TermContext>();
                TreeSet<Term> extractedTerms = new TreeSet<Term>();
                q.ExtractTerms(extractedTerms);
                foreach (Term term in extractedTerms)
                {
                    termContexts[term] = TermContext.Build(context, term);
                }
                Bits acceptDocs = ((AtomicReader)context.Reader).LiveDocs;
                Spans.Spans spans = q.GetSpans(context, acceptDocs, termContexts);

                // collect span positions
                while (spans.Next())
                {
                    spanPositions.Add(new PositionSpan(spans.Start(), spans.End() - 1));
                }

            }

            if (spanPositions.Count == 0)
            {
                // no spans found
                return;
            }

            foreach (Term queryTerm in nonWeightedTerms)
            {

                if (FieldNameComparator(queryTerm.Field))
                {
                    WeightedSpanTerm weightedSpanTerm = terms[queryTerm.Text()];

                    if (weightedSpanTerm == null)
                    {
                        weightedSpanTerm = new WeightedSpanTerm(spanQuery.Boost, queryTerm.Text());
                        weightedSpanTerm.AddPositionSpans(spanPositions);
                        weightedSpanTerm.SetPositionSensitive(true);
                        terms[queryTerm.Text()] = weightedSpanTerm;
                    }
                    else
                    {
                        if (spanPositions.Count > 0)
                        {
                            weightedSpanTerm.AddPositionSpans(spanPositions);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Fills a <c>Map</c> with <see cref="WeightedSpanTerm"/>s using the terms from the supplied <c>Query</c>.
        /// </summary>
        /// <param name="terms"></param>
        /// <param name="query"></param>
        private void ExtractWeightedTerms(IDictionary<String, WeightedSpanTerm> terms, Query query)
        {
            var nonWeightedTerms = Support.Compatibility.SetFactory.CreateHashSet<Term>();
            query.ExtractTerms(nonWeightedTerms);

            foreach (Term queryTerm in nonWeightedTerms)
            {

                if (FieldNameComparator(queryTerm.Field))
                {
                    WeightedSpanTerm weightedSpanTerm = new WeightedSpanTerm(query.Boost, queryTerm.Text());
                    terms[queryTerm.Text()] = weightedSpanTerm;
                }
            }
        }

        /// <summary>
        /// Necessary to implement matches for queries against <c>defaultField</c>
        /// </summary>
        private bool FieldNameComparator(string fieldNameToCheck)
        {
            bool rv = fieldName == null || fieldNameToCheck == fieldName
                      || fieldNameToCheck == defaultField;
            return rv;
        }

        protected AtomicReaderContext GetLeafContext()
        {
            if (internalReader == null)
            {
                if (wrapToCaching && !(tokenStream is CachingTokenFilter))
                {
                    tokenStream = new CachingTokenFilter(new OffsetLimitTokenFilter(tokenStream, maxDocCharsToAnalyze));
                    cachedTokenStream = true;
                }
                MemoryIndex indexer = new MemoryIndex(true);
                indexer.AddField(DelegatingAtomicReader.FIELD_NAME, tokenStream);
                tokenStream.Reset();
                IndexSearcher searcher = indexer.CreateSearcher();
                // MEM index has only atomic ctx
                var reader = ((AtomicReaderContext) searcher.TopReaderContext).Reader;
                internalReader = new DelegatingAtomicReader((AtomicReader)reader);
            }
            return (AtomicReaderContext)internalReader.Context;
        }

        private sealed class DelegatingAtomicReader : FilterAtomicReader
        {
            public static string FIELD_NAME = "shadowed_field";

            public DelegatingAtomicReader(AtomicReader reader) : base(reader) { }

            public override NumericDocValues GetNumericDocValues(string field)
            {
                return base.GetNumericDocValues(FIELD_NAME);
            }

            public override BinaryDocValues GetBinaryDocValues(string field)
            {
                return base.GetBinaryDocValues(FIELD_NAME);
            }
           
            public override SortedDocValues GetSortedDocValues(string field)
            {
                return base.GetSortedDocValues(FIELD_NAME);
            }

            public override NumericDocValues GetNormValues(string field)
            {
                return base.GetNormValues(FIELD_NAME);
            }

            public override Bits GetDocsWithField(string field)
            {
                return base.GetDocsWithField(FIELD_NAME);
            }

            public override Fields Fields
            {
                get
                {
                    return new DelegatingFilterFields(base.Fields);
                }               
            }

            private class DelegatingFilterFields : FilterFields
            {
                public DelegatingFilterFields(Fields fields) : base(fields) { }

                public override Terms Terms(string field)
                {
                    return base.Terms(DelegatingAtomicReader.FIELD_NAME);
                }

                public override IEnumerator<string> GetEnumerator()
                {
                    var list = new List<string> {DelegatingAtomicReader.FIELD_NAME};
                    return list.GetEnumerator();
                }

                public override int Size
                {
                    get { return 1; }
                }
            }
        }

        /// <summary>
        /// Creates a Map of <c>WeightedSpanTerms</c> from the given <c>Query</c> and <c>TokenStream</c>.
        /// </summary>
        /// <param name="query">query that caused hit</param>
        /// <param name="tokenStream">TokenStream of text to be highlighted</param>
        /// <returns>Map containing WeightedSpanTerms</returns>
        /// <exception cref="IOException">If there is a low-level I/O error</exception>
        public IDictionary<string, WeightedSpanTerm> GetWeightedSpanTerms(Query query, TokenStream tokenStream)
        {
            return GetWeightedSpanTerms(query, tokenStream, null);
        }


        /// <summary>
        /// Creates a Map of <c>WeightedSpanTerms</c> from the given <c>Query</c> and <c>TokenStream</c>.
        /// </summary>
        /// <param name="query">query that caused hit</param>
        /// <param name="tokenStream">tokenStream of text to be highlighted</param>
        /// <param name="fieldName">restricts Term's used based on field name</param>
        /// <returns>Map containing WeightedSpanTerms</returns>
        /// <exception cref="IOException">If there is a low-level I/O error</exception>
        public IDictionary<string, WeightedSpanTerm> GetWeightedSpanTerms(Query query, TokenStream tokenStream,
                                                                          string fieldName)
        {
            if (fieldName != null)
            {
                this.fieldName = StringHelper.Intern(fieldName);
            }
            else
            {
                this.fieldName = null;
            }

            IDictionary<string, WeightedSpanTerm> terms = new PositionCheckingMap<string>();
            this.tokenStream = tokenStream;
            try
            {
                Extract(query, terms);
            }
            finally
            {
                IOUtils.Close(internalReader);
            }

            return terms;
        }

        /// <summary>
        /// Creates a Map of <c>WeightedSpanTerms</c> from the given <c>Query</c> and <c>TokenStream</c>. Uses a supplied
        /// <c>IndexReader</c> to properly Weight terms (for gradient highlighting).
        /// </summary>
        /// <param name="query">Query that caused hit</param>
        /// <param name="tokenStream">Tokenstream of text to be highlighted</param>
        /// <param name="fieldName">restricts Term's used based on field name</param>
        /// <param name="reader">to use for scoring</param>
        /// <returns>Map of WeightedSpanTerms with quasi tf/idf scores</returns>
        /// <exception cref="IOException">If there is a low-level I/O error</exception>
        public IDictionary<string, WeightedSpanTerm> GetWeightedSpanTermsWithScores(
            Query query, TokenStream tokenStream, string fieldName, IndexReader reader)
        {
            this.fieldName = fieldName == null ? null : StringHelper.Intern(fieldName);

            this.tokenStream = tokenStream;

            IDictionary<string, WeightedSpanTerm> terms = new PositionCheckingMap<string>();
            Extract(query, terms);

            int totalNumDocs = reader.MaxDoc;
            var weightedTerms = terms.Keys;

            try
            {
                foreach (var wt in weightedTerms)
                {
                    WeightedSpanTerm weightedSpanTerm = terms[wt];
                    int docFreq = reader.DocFreq(new Term(fieldName, weightedSpanTerm.Term));
                    // IDF algorithm taken from DefaultSimilarity class
                    float idf = (float)(Math.Log((float)totalNumDocs / (double)(docFreq + 1)) + 1.0);
                    weightedSpanTerm.Weight *= idf;
                }
            }
            finally
            {
                IOUtils.Close(internalReader);
            }

            return terms;
        }

        private void CollectSpanQueryFields(SpanQuery spanQuery, HashSet<string> fieldNames)
        {
            if (spanQuery is FieldMaskingSpanQuery)
            {
                CollectSpanQueryFields(((FieldMaskingSpanQuery)spanQuery).MaskedQuery, fieldNames);
            }
            else if (spanQuery is SpanFirstQuery)
            {
                CollectSpanQueryFields(((SpanFirstQuery)spanQuery).Match, fieldNames);
            }
            else if (spanQuery is SpanNearQuery)
            {
                foreach (SpanQuery clause in ((SpanNearQuery)spanQuery).Clauses)
                {
                    CollectSpanQueryFields(clause, fieldNames);
                }
            }
            else if (spanQuery is SpanNotQuery)
            {
                CollectSpanQueryFields(((SpanNotQuery)spanQuery).Include, fieldNames);
            }
            else if (spanQuery is SpanOrQuery)
            {
                foreach (SpanQuery clause in ((SpanOrQuery)spanQuery).Clauses)
                {
                    CollectSpanQueryFields(clause, fieldNames);
                }
            }
            else
            {
                fieldNames.Add(spanQuery.Field);
            }
        }

        private bool MustRewriteQuery(SpanQuery spanQuery)
        {
            if (!expandMultiTermQuery)
            {
                return false; // Will throw NotImplementedException in case of a SpanRegexQuery.
            }
            else if (spanQuery is FieldMaskingSpanQuery)
            {
                return MustRewriteQuery(((FieldMaskingSpanQuery)spanQuery).MaskedQuery);
            }
            else if (spanQuery is SpanFirstQuery)
            {
                return MustRewriteQuery(((SpanFirstQuery)spanQuery).Match);
            }
            else if (spanQuery is SpanNearQuery)
            {
                foreach (SpanQuery clause in ((SpanNearQuery)spanQuery).Clauses)
                {
                    if (MustRewriteQuery(clause))
                    {
                        return true;
                    }
                }
                return false;
            }
            else if (spanQuery is SpanNotQuery)
            {
                SpanNotQuery spanNotQuery = (SpanNotQuery)spanQuery;
                return MustRewriteQuery(spanNotQuery.Include) || MustRewriteQuery(spanNotQuery.Exclude);
            }
            else if (spanQuery is SpanOrQuery)
            {
                foreach (SpanQuery clause in ((SpanOrQuery)spanQuery).Clauses)
                {
                    if (MustRewriteQuery(clause))
                    {
                        return true;
                    }
                }
                return false;
            }
            else if (spanQuery is SpanTermQuery)
            {
                return false;
            }
            else
            {
                return true;
            }
        }


        /// <summary>
        /// This class makes sure that if both position sensitive and insensitive
        /// versions of the same term are added, the position insensitive one wins.
        /// </summary>
        /// <typeparam name="K"></typeparam>
        private class PositionCheckingMap<K> : HashMap<K, WeightedSpanTerm>
        {
            public PositionCheckingMap()
            {

            }

            public PositionCheckingMap(IEnumerable<KeyValuePair<K, WeightedSpanTerm>> m)
            {
                PutAll(m);
            }

            public void PutAll(IEnumerable<KeyValuePair<K, WeightedSpanTerm>> m)
            {
                foreach (var entry in m)
                {
                    Add(entry.Key, entry.Value);
                }
            }

            public override void Add(K key, WeightedSpanTerm value)
            {
                base.Add(key, value);
                WeightedSpanTerm prev = this[key];

                if (prev == null) return;

                WeightedSpanTerm prevTerm = prev;
                WeightedSpanTerm newTerm = value;
                if (!prevTerm.IsPositionSensitive())
                {
                    newTerm.SetPositionSensitive(false);
                }
            }

        }

        public bool ExpandMultiTermQuery
        {
            set { expandMultiTermQuery = value; }
            get { return expandMultiTermQuery; }
        }

        public bool IsCachedTokenStream
        {
            get { return cachedTokenStream; }
        }

        public TokenStream TokenStream
        {
            get { return tokenStream; }
        }


        /// <summary>
        /// By default, <see cref="Analysis.TokenStream"/>s that are not of the type
        /// <see cref="CachingTokenFilter"/> are wrapped in a <see cref="CachingTokenFilter"/> to
        /// <see cref="Analysis.TokenStream"/> impl and you don't want it to be wrapped, set this to
        /// false.
        /// </summary>
        public void SetWrapIfNotCachingTokenFilter(bool wrap)
        {
            this.wrapToCaching = wrap;
        }

        protected void SetMaxDocCharsToAnalyze(int maxDocCharsToAnalyze)
        {
            this.maxDocCharsToAnalyze = maxDocCharsToAnalyze;
        }
    }
}