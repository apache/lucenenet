using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Lucene.Net.Analysis;
using Lucene.Net.Index;
using Lucene.Net.Search.Spans;
using Lucene.Net.Store;
using Lucene.Net.Support;
using Lucene.Net.Util;

namespace Lucene.Net.Search.Highlight
{
    /**
     * Class used to extract {@link WeightedSpanTerm}s from a {@link Query} based on whether 
     * {@link Term}s from the {@link Query} are contained in a supplied {@link TokenStream}.
     */

    public class WeightedSpanTermExtractor
    {

        private String fieldName;
        private TokenStream tokenStream;
        private IDictionary<String, IndexReader> readers = new HashMap<String, IndexReader>(10);
        private String defaultField;
        private bool expandMultiTermQuery;
        private bool cachedTokenStream;
        private bool wrapToCaching = true;

        public WeightedSpanTermExtractor()
        {
        }

        public WeightedSpanTermExtractor(String defaultField)
        {
            if (defaultField != null)
            {
                this.defaultField = StringHelper.Intern(defaultField);
            }
        }

        private void closeReaders()
        {
            ICollection<IndexReader> readerSet = readers.Values;

            foreach (IndexReader reader in readerSet)
            {
                try
                {
                    reader.Close();
                }
                catch (IOException e)
                {
                    // alert?
                }
            }
        }

        /**
         * Fills a <code>Map</code> with <@link WeightedSpanTerm>s using the terms from the supplied <code>Query</code>.
         * 
         * @param query
         *          Query to extract Terms from
         * @param terms
         *          Map to place created WeightedSpanTerms in
         * @throws IOException
         */

        private void extract(Query query, IDictionary<String, WeightedSpanTerm> terms)
        {
            if (query is BooleanQuery)
            {
                BooleanClause[] queryClauses = ((BooleanQuery) query).GetClauses();

                for (int i = 0; i < queryClauses.Length; i++)
                {
                    if (!queryClauses[i].Prohibited)
                    {
                        extract(queryClauses[i].Query, terms);
                    }
                }
            }
            else if (query is PhraseQuery)
            {
                PhraseQuery phraseQuery = ((PhraseQuery) query);
                Term[] phraseQueryTerms = phraseQuery.GetTerms();
                SpanQuery[] clauses = new SpanQuery[phraseQueryTerms.Length];
                for (int i = 0; i < phraseQueryTerms.Length; i++)
                {
                    clauses[i] = new SpanTermQuery(phraseQueryTerms[i]);
                }
                int slop = phraseQuery.Slop;
                int[] positions = phraseQuery.GetPositions();
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
                extractWeightedSpanTerms(terms, sp);
            }
            else if (query is TermQuery)
            {
                extractWeightedTerms(terms, query);
            }
            else if (query is SpanQuery)
            {
                extractWeightedSpanTerms(terms, (SpanQuery) query);
            }
            else if (query is FilteredQuery)
            {
                extract(((FilteredQuery) query).Query, terms);
            }
            else if (query is DisjunctionMaxQuery)
            {
                foreach (var q in ((DisjunctionMaxQuery) query))
                {
                    extract(q, terms);
                }
            }
            else if (query is MultiTermQuery && expandMultiTermQuery)
            {
                MultiTermQuery mtq = ((MultiTermQuery) query);
                if (mtq.QueryRewriteMethod != MultiTermQuery.SCORING_BOOLEAN_QUERY_REWRITE)
                {
                    mtq = (MultiTermQuery) mtq.Clone();
                    mtq.QueryRewriteMethod = MultiTermQuery.SCORING_BOOLEAN_QUERY_REWRITE;
                    query = mtq;
                }
                FakeReader fReader = new FakeReader();
                MultiTermQuery.SCORING_BOOLEAN_QUERY_REWRITE.Rewrite(fReader, mtq);
                if (fReader.Field != null)
                {
                    IndexReader ir = getReaderForField(fReader.Field);
                    extract(query.Rewrite(ir), terms);
                }
            }
            else if (query is MultiPhraseQuery)
            {
                MultiPhraseQuery mpq = (MultiPhraseQuery) query;
                IList<Term[]> termArrays = mpq.GetTermArrays();
                int[] positions = mpq.GetPositions();
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

                    var disjunctLists = new IList<SpanQuery>[maxPosition + 1];
                    int distinctPositions = 0;

                    for (int i = 0; i < termArrays.Count; ++i)
                    {
                        Term[] termArray = termArrays[i];
                        IList<SpanQuery> disjuncts = disjunctLists[positions[i]];
                        if (disjuncts == null)
                        {
                            disjuncts = (disjunctLists[positions[i]] = new List<SpanQuery>(termArray.Length));
                            ++distinctPositions;
                        }
                        for (int j = 0; j < termArray.Length; ++j)
                        {
                            disjuncts.Add(new SpanTermQuery(termArray[j]));
                        }
                    }

                    int positionGaps = 0;
                    int position = 0;
                    var clauses = new SpanQuery[distinctPositions];
                    for (int i = 0; i < disjunctLists.Length; ++i)
                    {
                        IList<SpanQuery> disjuncts = disjunctLists[i];
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
                    extractWeightedSpanTerms(terms, sp);
                }
            }
        }

        /**
         * Fills a <code>Map</code> with <@link WeightedSpanTerm>s using the terms from the supplied <code>SpanQuery</code>.
         * 
         * @param terms
         *          Map to place created WeightedSpanTerms in
         * @param spanQuery
         *          SpanQuery to extract Terms from
         * @throws IOException
         */

        private void extractWeightedSpanTerms(IDictionary<String, WeightedSpanTerm> terms, SpanQuery spanQuery)
        {
            ISet<String> fieldNames;

            if (fieldName == null)
            {
                fieldNames = new HashSet<String>();
                collectSpanQueryFields(spanQuery, fieldNames);
            }
            else
            {
                fieldNames = new HashSet<String> {fieldName};
            }
            // To support the use of the default field name
            if (defaultField != null)
            {
                fieldNames.Add(defaultField);
            }

            HashMap<String, SpanQuery> queries = new HashMap<String, SpanQuery>();

            ISet<Term> nonWeightedTerms = new HashSet<Term>();
            bool mrq = mustRewriteQuery(spanQuery);
            if (mrq)
            {
                foreach (var field in fieldNames)
                {
                    SpanQuery rewrittenQuery = (SpanQuery) spanQuery.Rewrite(getReaderForField(field));
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

                IndexReader reader = getReaderForField(field);
                Spans.Spans spans = mrq ? queries[field].GetSpans(reader) : spanQuery.GetSpans(reader);


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

                if (fieldNameComparator(queryTerm.Field))
                {
                    WeightedSpanTerm weightedSpanTerm = terms[queryTerm.Text];

                    if (weightedSpanTerm == null)
                    {
                        weightedSpanTerm = new WeightedSpanTerm(spanQuery.Boost, queryTerm.Text);
                        weightedSpanTerm.addPositionSpans(spanPositions);
                        weightedSpanTerm.setPositionSensitive(true);
                        terms[queryTerm.Text] = weightedSpanTerm;
                    }
                    else
                    {
                        if (spanPositions.Count > 0)
                        {
                            weightedSpanTerm.addPositionSpans(spanPositions);
                        }
                    }
                }
            }
        }

        /**
         * Fills a <code>Map</code> with <@link WeightedSpanTerm>s using the terms from the supplied <code>Query</code>.
         * 
         * @param terms
         *          Map to place created WeightedSpanTerms in
         * @param query
         *          Query to extract Terms from
         * @throws IOException
         */

        private void extractWeightedTerms(IDictionary<String, WeightedSpanTerm> terms, Query query)
        {
            ISet<Term> nonWeightedTerms = new HashSet<Term>();
            query.ExtractTerms(nonWeightedTerms);

            foreach (Term queryTerm in nonWeightedTerms)
            {

                if (fieldNameComparator(queryTerm.Field))
                {
                    WeightedSpanTerm weightedSpanTerm = new WeightedSpanTerm(query.Boost, queryTerm.Text);
                    terms[queryTerm.Text] = weightedSpanTerm;
                }
            }
        }

        /**
         * Necessary to implement matches for queries against <code>defaultField</code>
         */

        private bool fieldNameComparator(String fieldNameToCheck)
        {
            bool rv = fieldName == null || fieldNameToCheck == fieldName
                      || fieldNameToCheck == defaultField;
            return rv;
        }

        private IndexReader getReaderForField(String field)
        {
            if (wrapToCaching && !cachedTokenStream && !(tokenStream is CachingTokenFilter))
            {
                tokenStream = new CachingTokenFilter(tokenStream);
                cachedTokenStream = true;
            }
            IndexReader reader = readers[field];
            if (reader == null)
            {
                //MemoryIndex indexer = new MemoryIndex();
                //indexer.AddField(field, tokenStream);
                //tokenStream.Reset();
                //IndexSearcher searcher = indexer.CreateSearcher();
                //reader = searcher.IndexReader;
                //readers[field] = reader;
            }

            return reader;
        }

        /**
         * Creates a Map of <code>WeightedSpanTerms</code> from the given <code>Query</code> and <code>TokenStream</code>.
         * 
         * <p>
         * 
         * @param query
         *          that caused hit
         * @param tokenStream
         *          of text to be highlighted
         * @return Map containing WeightedSpanTerms
         * @throws IOException
         */

        public HashMap<String, WeightedSpanTerm> getWeightedSpanTerms(Query query, TokenStream tokenStream)
        {
            return getWeightedSpanTerms(query, tokenStream, null);
        }

        /**
         * Creates a Map of <code>WeightedSpanTerms</code> from the given <code>Query</code> and <code>TokenStream</code>.
         * 
         * <p>
         * 
         * @param query
         *          that caused hit
         * @param tokenStream
         *          of text to be highlighted
         * @param fieldName
         *          restricts Term's used based on field name
         * @return Map containing WeightedSpanTerms
         * @throws IOException
         */

        public HashMap<String, WeightedSpanTerm> getWeightedSpanTerms(Query query, TokenStream tokenStream,
                                                                      String fieldName)
        {
            if (fieldName != null)
            {
                this.fieldName = StringHelper.Intern(fieldName);
            }
            else
            {
                this.fieldName = null;
            }

            HashMap<String, WeightedSpanTerm> terms = new PositionCheckingMap<String>();
            this.tokenStream = tokenStream;
            try
            {
                extract(query, terms);
            }
            finally
            {
                closeReaders();
            }

            return terms;
        }

        /**
         * Creates a Map of <code>WeightedSpanTerms</code> from the given <code>Query</code> and <code>TokenStream</code>. Uses a supplied
         * <code>IndexReader</code> to properly weight terms (for gradient highlighting).
         * 
         * <p>
         * 
         * @param query
         *          that caused hit
         * @param tokenStream
         *          of text to be highlighted
         * @param fieldName
         *          restricts Term's used based on field name
         * @param reader
         *          to use for scoring
         * @return Map of WeightedSpanTerms with quasi tf/idf scores
         * @throws IOException
         */

        public HashMap<String, WeightedSpanTerm> getWeightedSpanTermsWithScores(Query query, TokenStream tokenStream,
                                                                                String fieldName,
                                                                                IndexReader reader)
        {
            if (fieldName != null)
            {
                this.fieldName = StringHelper.Intern(fieldName);
            }
            else
            {
                this.fieldName = null;
            }
            this.tokenStream = tokenStream;

            HashMap<String, WeightedSpanTerm> terms = new PositionCheckingMap<String>();
            extract(query, terms);

            int totalNumDocs = reader.NumDocs();
            var weightedTerms = terms.Keys;

            try
            {
                foreach (var term in weightedTerms)
                {
                    WeightedSpanTerm weightedSpanTerm = terms[term];
                    int docFreq = reader.DocFreq(new Term(fieldName, weightedSpanTerm.term));
                    // docFreq counts deletes
                    if (totalNumDocs < docFreq)
                    {
                        docFreq = totalNumDocs;
                    }
                    // IDF algorithm taken from DefaultSimilarity class
                    float idf = (float) (Math.Log((float) totalNumDocs/(double) (docFreq + 1)) + 1.0);
                    weightedSpanTerm.weight *= idf;
                }

            }
            finally
            {

                closeReaders();
            }

            return terms;
        }

        private void collectSpanQueryFields(SpanQuery spanQuery, ISet<String> fieldNames)
        {
            if (spanQuery is FieldMaskingSpanQuery)
            {
                collectSpanQueryFields(((FieldMaskingSpanQuery) spanQuery).MaskedQuery, fieldNames);
            }
            else if (spanQuery is SpanFirstQuery)
            {
                collectSpanQueryFields(((SpanFirstQuery) spanQuery).Match, fieldNames);
            }
            else if (spanQuery is SpanNearQuery)
            {
                foreach (SpanQuery clause in ((SpanNearQuery) spanQuery).GetClauses())
                {
                    collectSpanQueryFields(clause, fieldNames);
                }
            }
            else if (spanQuery is SpanNotQuery)
            {
                collectSpanQueryFields(((SpanNotQuery) spanQuery).Include, fieldNames);
            }
            else if (spanQuery is SpanOrQuery)
            {
                foreach (SpanQuery clause in ((SpanOrQuery) spanQuery).GetClauses())
                {
                    collectSpanQueryFields(clause, fieldNames);
                }
            }
            else
            {
                fieldNames.Add(spanQuery.Field);
            }
        }

        private bool mustRewriteQuery(SpanQuery spanQuery)
        {
            if (!expandMultiTermQuery)
            {
                return false; // Will throw UnsupportedOperationException in case of a SpanRegexQuery.
            }
            else if (spanQuery is FieldMaskingSpanQuery)
            {
                return mustRewriteQuery(((FieldMaskingSpanQuery) spanQuery).MaskedQuery);
            }
            else if (spanQuery is SpanFirstQuery)
            {
                return mustRewriteQuery(((SpanFirstQuery) spanQuery).Match);
            }
            else if (spanQuery is SpanNearQuery)
            {
                foreach (SpanQuery clause in ((SpanNearQuery) spanQuery).GetClauses())
                {
                    if (mustRewriteQuery(clause))
                    {
                        return true;
                    }
                }
                return false;
            }
            else if (spanQuery is SpanNotQuery)
            {
                SpanNotQuery spanNotQuery = (SpanNotQuery) spanQuery;
                return mustRewriteQuery(spanNotQuery.Include) || mustRewriteQuery(spanNotQuery.Exclude);
            }
            else if (spanQuery is SpanOrQuery)
            {
                foreach (SpanQuery clause in ((SpanOrQuery) spanQuery).GetClauses())
                {
                    if (mustRewriteQuery(clause))
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

        /**
         * This class makes sure that if both position sensitive and insensitive
         * versions of the same term are added, the position insensitive one wins.
         */

        private class PositionCheckingMap<K> : HashMap<K, WeightedSpanTerm>
        {

            public void PutAll(IDictionary<K, WeightedSpanTerm> m)
            {
                foreach (var entry in m)
                {
                    this[entry.Key] = entry.Value;
                }
            }

            public override void Add(K key, WeightedSpanTerm value)
            {
                WeightedSpanTerm prev = base[key] = value;

                WeightedSpanTerm prevTerm = prev;
                WeightedSpanTerm newTerm = value;
                if (!prevTerm.isPositionSensitive())
                {
                    newTerm.setPositionSensitive(false);
                }
            }

        }

        public bool getExpandMultiTermQuery()
        {
            return expandMultiTermQuery;
        }

        public void setExpandMultiTermQuery(bool expandMultiTermQuery)
        {
            this.expandMultiTermQuery = expandMultiTermQuery;
        }

        public bool isCachedTokenStream()
        {
            return cachedTokenStream;
        }

        public TokenStream getTokenStream()
        {
            return tokenStream;
        }

        /**
         * By default, {@link TokenStream}s that are not of the type
         * {@link CachingTokenFilter} are wrapped in a {@link CachingTokenFilter} to
         * ensure an efficient reset - if you are already using a different caching
         * {@link TokenStream} impl and you don't want it to be wrapped, set this to
         * false.
         * 
         * @param wrap
         */

        public void setWrapIfNotCachingTokenFilter(bool wrap)
        {
            this.wrapToCaching = wrap;
        }

        /**
         * 
         * A fake IndexReader class to extract the field from a MultiTermQuery
         * 
         */
        private class FakeReader : FilterIndexReader
        {
            //See if this will work.
            private static IndexReader EMPTY_MEMORY_INDEX_READER = IndexReader.Open(new RAMDirectory());
            //private static IndexReader EMPTY_MEMORY_INDEX_READER = new MemoryIndex().createSearcher().getIndexReader();

            public FakeReader()
                : base(EMPTY_MEMORY_INDEX_READER)
            {
            }

            public string Field { get; set; }

            public override TermEnum Terms(Term t)
            {
                // only set first fieldname, maybe use a Set?
                if (t != null && Field == null)
                    Field = t.Field;
                return base.Terms(t);
            }
        }
    }
}