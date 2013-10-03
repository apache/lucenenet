using Lucene.Net.Analysis;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers.Classic;
using Lucene.Net.Search;
using Lucene.Net.Search.Spans;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Version = Lucene.Net.Util.Version;

namespace Lucene.Net.QueryParsers.ComplexPhrase
{
    public class ComplexPhraseQueryParser : QueryParser
    {
        private List<ComplexPhraseQuery> complexPhrases = null;

        private bool isPass2ResolvingPhrases;

        private ComplexPhraseQuery currentPhraseQuery = null;

        public ComplexPhraseQueryParser(Version matchVersion, String f, Analyzer a)
            : base(matchVersion, f, a)
        {
        }

        protected override Query GetFieldQuery(string field, string queryText, int slop)
        {
            ComplexPhraseQuery cpq = new ComplexPhraseQuery(field, queryText, slop);
            complexPhrases.Add(cpq); // add to list of phrases to be parsed once
            // we
            // are through with this pass
            return cpq;
        }

        public override Query Parse(string query)
        {
            if (isPass2ResolvingPhrases)
            {
                MultiTermQuery.RewriteMethod oldMethod = this.MultiTermRewriteMethod;
                try
                {
                    // Temporarily force BooleanQuery rewrite so that Parser will
                    // generate visible
                    // collection of terms which we can convert into SpanQueries.
                    // ConstantScoreRewrite mode produces an
                    // opaque ConstantScoreQuery object which cannot be interrogated for
                    // terms in the same way a BooleanQuery can.
                    // QueryParser is not guaranteed threadsafe anyway so this temporary
                    // state change should not
                    // present an issue
                    this.MultiTermRewriteMethod = MultiTermQuery.SCORING_BOOLEAN_QUERY_REWRITE;
                    return base.Parse(query);
                }
                finally
                {
                    this.MultiTermRewriteMethod = oldMethod;
                }
            }

            // First pass - parse the top-level query recording any PhraseQuerys
            // which will need to be resolved
            complexPhrases = new List<ComplexPhraseQuery>();
            Query q = base.Parse(query);

            // Perform second pass, using this QueryParser to parse any nested
            // PhraseQueries with different
            // set of syntax restrictions (i.e. all fields must be same)
            isPass2ResolvingPhrases = true;
            try
            {
                foreach (ComplexPhraseQuery currentPhraseQuery in complexPhrases)
                {
                    // in each phrase, now parse the contents between quotes as a
                    // separate parse operation
                    currentPhraseQuery.ParsePhraseElements(this);
                }
            }
            finally
            {
                isPass2ResolvingPhrases = false;
            }
            return q;
        }

        protected override Query NewTermQuery(Term term)
        {
            if (isPass2ResolvingPhrases)
            {
                try
                {
                    CheckPhraseClauseIsForSameField(term.Field);
                }
                catch (ParseException pe)
                {
                    throw new SystemException("Error parsing complex phrase", pe);
                }
            }
            return base.NewTermQuery(term);
        }

        private void CheckPhraseClauseIsForSameField(string field)
        {
            if (!field.Equals(currentPhraseQuery.field))
            {
                throw new ParseException("Cannot have clause for field \"" + field
                    + "\" nested in phrase " + " for field \"" + currentPhraseQuery.field
                    + "\"");
            }
        }

        protected override Query GetWildcardQuery(string field, string termStr)
        {
            if (isPass2ResolvingPhrases)
            {
                CheckPhraseClauseIsForSameField(field);
            }
            return base.GetWildcardQuery(field, termStr);
        }

        protected override Query GetRangeQuery(string field, string part1, string part2, bool startInclusive, bool endInclusive)
        {
            if (isPass2ResolvingPhrases)
            {
                CheckPhraseClauseIsForSameField(field);
            }
            return base.GetRangeQuery(field, part1, part2, startInclusive, endInclusive);
        }

        protected override Query NewRangeQuery(string field, string part1, string part2, bool startInclusive, bool endInclusive)
        {
            if (isPass2ResolvingPhrases)
            {
                // Must use old-style RangeQuery in order to produce a BooleanQuery
                // that can be turned into SpanOr clause
                TermRangeQuery rangeQuery = TermRangeQuery.NewStringRange(field, part1, part2, startInclusive, endInclusive);
                rangeQuery.SetRewriteMethod(MultiTermQuery.SCORING_BOOLEAN_QUERY_REWRITE);
                return rangeQuery;
            }
            return base.NewRangeQuery(field, part1, part2, startInclusive, endInclusive);
        }

        protected override Query GetFuzzyQuery(string field, string termStr, float minSimilarity)
        {
            if (isPass2ResolvingPhrases)
            {
                CheckPhraseClauseIsForSameField(field);
            }
            return base.GetFuzzyQuery(field, termStr, minSimilarity);
        }

        public class ComplexPhraseQuery : Query
        {
            protected internal string field;

            protected internal string phrasedQueryStringContents;

            protected internal int slopFactor;

            private Query contents;

            public ComplexPhraseQuery(string field, string phrasedQueryStringContents, int slopFactor)
                : base()
            {
                this.field = field;
                this.phrasedQueryStringContents = phrasedQueryStringContents;
                this.slopFactor = slopFactor;
            }

            // Called by ComplexPhraseQueryParser for each phrase after the main
            // parse
            // thread is through
            protected internal void ParsePhraseElements(QueryParser qp)
            {
                // TODO ensure that field-sensitivity is preserved ie the query
                // string below is parsed as
                // field+":("+phrasedQueryStringContents+")"
                // but this will need code in rewrite to unwrap the first layer of
                // boolean query
                contents = qp.Parse(phrasedQueryStringContents);
            }

            public override Query Rewrite(IndexReader reader)
            {
                // ArrayList spanClauses = new ArrayList();
                if (contents is TermQuery)
                {
                    return contents;
                }
                // Build a sequence of Span clauses arranged in a SpanNear - child
                // clauses can be complex
                // Booleans e.g. nots and ors etc
                int numNegatives = 0;
                if (!(contents is BooleanQuery))
                {
                    throw new ArgumentException("Unknown query type \""
                        + contents.GetType().Name
                        + "\" found in phrase query string \"" + phrasedQueryStringContents
                        + "\"");
                }
                BooleanQuery bq = (BooleanQuery)contents;
                BooleanClause[] bclauses = bq.Clauses;
                SpanQuery[] allSpanClauses = new SpanQuery[bclauses.Length];
                // For all clauses e.g. one* two~
                for (int i = 0; i < bclauses.Length; i++)
                {
                    // HashSet bclauseterms=new HashSet();
                    Query qc = bclauses[i].Query;
                    // Rewrite this clause e.g one* becomes (one OR onerous)
                    qc = qc.Rewrite(reader);
                    if (bclauses[i].Occur.Equals(Occur.MUST_NOT))
                    {
                        numNegatives++;
                    }

                    if (qc is BooleanQuery)
                    {
                        List<SpanQuery> sc = new List<SpanQuery>();
                        AddComplexPhraseClause(sc, (BooleanQuery)qc);
                        if (sc.Count > 0)
                        {
                            allSpanClauses[i] = sc[0];
                        }
                        else
                        {
                            // Insert fake term e.g. phrase query was for "Fred Smithe*" and
                            // there were no "Smithe*" terms - need to
                            // prevent match on just "Fred".
                            allSpanClauses[i] = new SpanTermQuery(new Term(field,
                                "Dummy clause because no terms found - must match nothing"));
                        }
                    }
                    else
                    {
                        if (qc is TermQuery)
                        {
                            TermQuery tq = (TermQuery)qc;
                            allSpanClauses[i] = new SpanTermQuery(tq.Term);
                        }
                        else
                        {
                            throw new ArgumentException("Unknown query type \""
                                + qc.GetType().Name
                                + "\" found in phrase query string \""
                                + phrasedQueryStringContents + "\"");
                        }

                    }
                }
                if (numNegatives == 0)
                {
                    // The simple case - no negative elements in phrase
                    return new SpanNearQuery(allSpanClauses, slopFactor, true);
                }
                // Complex case - we have mixed positives and negatives in the
                // sequence.
                // Need to return a SpanNotQuery
                List<SpanQuery> positiveClauses = new List<SpanQuery>();
                for (int j = 0; j < allSpanClauses.Length; j++)
                {
                    if (!bclauses[j].Occur.Equals(Occur.MUST_NOT))
                    {
                        positiveClauses.Add(allSpanClauses[j]);
                    }
                }

                SpanQuery[] includeClauses = positiveClauses.ToArray();

                SpanQuery include = null;
                if (includeClauses.Length == 1)
                {
                    include = includeClauses[0]; // only one positive clause
                }
                else
                {
                    // need to increase slop factor based on gaps introduced by
                    // negatives
                    include = new SpanNearQuery(includeClauses, slopFactor + numNegatives,
                        true);
                }
                // Use sequence of positive and negative values as the exclude.
                SpanNearQuery exclude = new SpanNearQuery(allSpanClauses, slopFactor,
                    true);
                SpanNotQuery snot = new SpanNotQuery(include, exclude);
                return snot;
            }

            private void AddComplexPhraseClause(IList<SpanQuery> spanClauses, BooleanQuery qc)
            {
                List<SpanQuery> ors = new List<SpanQuery>();
                List<SpanQuery> nots = new List<SpanQuery>();
                BooleanClause[] bclauses = qc.Clauses;

                // For all clauses e.g. one* two~
                for (int i = 0; i < bclauses.Length; i++)
                {
                    Query childQuery = bclauses[i].Query;

                    // select the list to which we will add these options
                    List<SpanQuery> chosenList = ors;
                    if (bclauses[i].Occur == Occur.MUST_NOT)
                    {
                        chosenList = nots;
                    }

                    if (childQuery is TermQuery)
                    {
                        TermQuery tq = (TermQuery)childQuery;
                        SpanTermQuery stq = new SpanTermQuery(tq.Term);
                        stq.Boost = tq.Boost;
                        chosenList.Add(stq);
                    }
                    else if (childQuery is BooleanQuery)
                    {
                        BooleanQuery cbq = (BooleanQuery)childQuery;
                        AddComplexPhraseClause(chosenList, cbq);
                    }
                    else
                    {
                        // TODO alternatively could call extract terms here?
                        throw new ArgumentException("Unknown query type:"
                            + childQuery.GetType().Name);
                    }
                }
                if (ors.Count == 0)
                {
                    return;
                }
                SpanOrQuery soq = new SpanOrQuery(ors.ToArray());
                if (nots.Count == 0)
                {
                    spanClauses.Add(soq);
                }
                else
                {
                    SpanOrQuery snqs = new SpanOrQuery(nots.ToArray());
                    SpanNotQuery snq = new SpanNotQuery(soq, snqs);
                    spanClauses.Add(snq);
                }
            }

            public override string ToString(string field)
            {
                return "\"" + phrasedQueryStringContents + "\"";
            }

            public override int GetHashCode()
            {
                int prime = 31;
                int result = base.GetHashCode();
                result = prime * result + ((field == null) ? 0 : field.GetHashCode());
                result = prime
                    * result
                    + ((phrasedQueryStringContents == null) ? 0
                        : phrasedQueryStringContents.GetHashCode());
                result = prime * result + slopFactor;
                return result;
            }

            public override bool Equals(object obj)
            {
                if (this == obj)
                    return true;
                if (obj == null)
                    return false;
                if (GetType() != obj.GetType())
                    return false;
                if (!base.Equals(obj))
                {
                    return false;
                }
                ComplexPhraseQuery other = (ComplexPhraseQuery)obj;
                if (field == null)
                {
                    if (other.field != null)
                        return false;
                }
                else if (!field.Equals(other.field))
                    return false;
                if (phrasedQueryStringContents == null)
                {
                    if (other.phrasedQueryStringContents != null)
                        return false;
                }
                else if (!phrasedQueryStringContents
                  .Equals(other.phrasedQueryStringContents))
                    return false;
                if (slopFactor != other.slopFactor)
                    return false;
                return true;
            }
        }
    }
}
