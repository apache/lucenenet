using Lucene.Net.Analysis;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers.Classic;
using Lucene.Net.Search;
using Lucene.Net.Search.Spans;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using JCG = J2N.Collections.Generic;

namespace Lucene.Net.QueryParsers.ComplexPhrase
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

    // LUCENENET: Special case exception - QueryParser has its own ParseException types that are generated.
    // We won't know until we start generating QueryParser how to handle this scenario, but for now we are
    // mapping this explicitly INSIDE of the namespace declaration to prevent our Lucene.ParseException from being
    // used instead.
    using ParseException = Lucene.Net.QueryParsers.Classic.ParseException;

    /// <summary>
    /// <see cref="QueryParser"/> which permits complex phrase query syntax eg "(john jon
    /// jonathan~) peters*".
    /// <para>
    /// Performs potentially multiple passes over Query text to parse any nested
    /// logic in PhraseQueries. - First pass takes any PhraseQuery content between
    /// quotes and stores for subsequent pass. All other query content is parsed as
    /// normal - Second pass parses any stored PhraseQuery content, checking all
    /// embedded clauses are referring to the same field and therefore can be
    /// rewritten as Span queries. All PhraseQuery clauses are expressed as
    /// ComplexPhraseQuery objects
    /// </para>
    /// <para>
    /// This could arguably be done in one pass using a new <see cref="QueryParser"/> but here I am
    /// working within the constraints of the existing parser as a base class. This
    /// currently simply feeds all phrase content through an analyzer to select
    /// phrase terms - any "special" syntax such as * ~ * etc are not given special
    /// status
    /// </para>
    /// </summary>
    public class ComplexPhraseQueryParser : QueryParser
    {
        private IList<ComplexPhraseQuery> complexPhrases = null;

        private bool isPass2ResolvingPhrases;

        /// <summary>
        /// When <see cref="InOrder"/> is true, the search terms must
        /// exists in the documents as the same order as in query.
        /// Choose between ordered (true) or un-ordered (false) proximity search.
        /// </summary>
        public virtual bool InOrder { get; internal set; }

        private ComplexPhraseQuery currentPhraseQuery = null;

        public ComplexPhraseQueryParser(LuceneVersion matchVersion, string f, Analyzer a)
            : base(matchVersion, f, a)
        {
            // set property defaults
            this.InOrder = true;
        }

        protected internal override Query GetFieldQuery(string field, string queryText, int slop)
        {
            ComplexPhraseQuery cpq = new ComplexPhraseQuery(field, queryText, slop, InOrder);
            complexPhrases.Add(cpq); // add to list of phrases to be parsed once
            // we
            // are through with this pass
            return cpq;
        }

        public override Query Parse(string query)
        {
            if (isPass2ResolvingPhrases)
            {
                MultiTermQuery.RewriteMethod oldMethod = MultiTermRewriteMethod;
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
                    MultiTermRewriteMethod = MultiTermQuery.SCORING_BOOLEAN_QUERY_REWRITE;
                    return base.Parse(query);
                }
                finally
                {
                    MultiTermRewriteMethod = oldMethod;
                }
            }

            // First pass - parse the top-level query recording any PhraseQuerys
            // which will need to be resolved
            complexPhrases = new JCG.List<ComplexPhraseQuery>();
            Query q = base.Parse(query);

            // Perform second pass, using this QueryParser to parse any nested
            // PhraseQueries with different
            // set of syntax restrictions (i.e. all fields must be same)
            isPass2ResolvingPhrases = true;
            try
            {
                foreach (var currentPhraseQuery in complexPhrases)
                {
                    this.currentPhraseQuery = currentPhraseQuery;
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

        // There is No "getTermQuery throws ParseException" method to override so
        // unfortunately need
        // to throw a runtime exception here if a term for another field is embedded
        // in phrase query
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
                    throw RuntimeException.Create("Error parsing complex phrase", pe);
                }
            }
            return base.NewTermQuery(term);
        }

        // Helper method used to report on any clauses that appear in query syntax
        private void CheckPhraseClauseIsForSameField(string field)
        {
            if (!field.Equals(currentPhraseQuery.Field, StringComparison.Ordinal))
            {
                throw new ParseException("Cannot have clause for field \"" + field
                    + "\" nested in phrase " + " for field \"" + currentPhraseQuery.Field
                    + "\"");
            }
        }

        protected internal override Query GetWildcardQuery(string field, string termStr)
        {
            if (isPass2ResolvingPhrases)
            {
                CheckPhraseClauseIsForSameField(field);
            }
            return base.GetWildcardQuery(field, termStr);
        }

        protected internal override Query GetRangeQuery(string field, string part1, string part2, bool startInclusive, bool endInclusive)
        {
            if (isPass2ResolvingPhrases)
            {
                CheckPhraseClauseIsForSameField(field);
            }
            return base.GetRangeQuery(field, part1, part2, startInclusive, endInclusive);
        }

        protected internal override Query NewRangeQuery(string field, string part1, string part2, bool startInclusive, bool endInclusive)
        {
            if (isPass2ResolvingPhrases)
            {
                // Must use old-style RangeQuery in order to produce a BooleanQuery
                // that can be turned into SpanOr clause
                TermRangeQuery rangeQuery = TermRangeQuery.NewStringRange(field, part1, part2, startInclusive, endInclusive);
                rangeQuery.MultiTermRewriteMethod = MultiTermQuery.SCORING_BOOLEAN_QUERY_REWRITE;
                return rangeQuery;
            }
            return base.NewRangeQuery(field, part1, part2, startInclusive, endInclusive);
        }

        protected internal override Query GetFuzzyQuery(string field, string termStr, float minSimilarity)
        {
            if (isPass2ResolvingPhrases)
            {
                CheckPhraseClauseIsForSameField(field);
            }
            return base.GetFuzzyQuery(field, termStr, minSimilarity);
        }

        /// <summary>
        /// Used to handle the query content in between quotes and produced Span-based
        /// interpretations of the clauses.
        /// </summary>
        public class ComplexPhraseQuery : Query
        {
            private readonly string field;
            private readonly string phrasedQueryStringContents;
            private readonly int slopFactor;
            private readonly bool inOrder;
            private Query contents;

            public ComplexPhraseQuery(string field, string phrasedQueryStringContents,
                int slopFactor, bool inOrder)
            {
                this.field = field;
                this.phrasedQueryStringContents = phrasedQueryStringContents;
                this.slopFactor = slopFactor;
                this.inOrder = inOrder;
            }

            public string Field => field;

            // Called by ComplexPhraseQueryParser for each phrase after the main
            // parse
            // thread is through
            protected internal void ParsePhraseElements(ComplexPhraseQueryParser qp)
            {
                // TODO ensure that field-sensitivity is preserved ie the query
                // string below is parsed as
                // field+":("+phrasedQueryStringContents+")"
                // but this will need code in rewrite to unwrap the first layer of
                // boolean query

                string oldDefaultParserField = qp.Field;
                try
                {
                    //temporarily set the QueryParser to be parsing the default field for this phrase e.g author:"fred* smith"
                    qp.m_field = this.field;
                    contents = qp.Parse(phrasedQueryStringContents);
                }
                finally
                {
                    qp.m_field = oldDefaultParserField;
                }
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
                BooleanClause[] bclauses = bq.GetClauses();
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

                    if (qc is BooleanQuery booleanQuery)
                    {
                        IList<SpanQuery> sc = new JCG.List<SpanQuery>();
                        AddComplexPhraseClause(sc, booleanQuery);
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
                        if (qc is TermQuery tq)
                        {
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
                    return new SpanNearQuery(allSpanClauses, slopFactor, inOrder);
                }
                // Complex case - we have mixed positives and negatives in the
                // sequence.
                // Need to return a SpanNotQuery
                JCG.List<SpanQuery> positiveClauses = new JCG.List<SpanQuery>();
                for (int j = 0; j < allSpanClauses.Length; j++)
                {
                    if (!bclauses[j].Occur.Equals(Occur.MUST_NOT))
                    {
                        positiveClauses.Add(allSpanClauses[j]);
                    }
                }

                SpanQuery[] includeClauses = positiveClauses
                    .ToArray();

                SpanQuery include; // LUCENENET: IDE0059: Remove unnecessary value assignment
                if (includeClauses.Length == 1)
                {
                    include = includeClauses[0]; // only one positive clause
                }
                else
                {
                    // need to increase slop factor based on gaps introduced by
                    // negatives
                    include = new SpanNearQuery(includeClauses, slopFactor + numNegatives,
                        inOrder);
                }
                // Use sequence of positive and negative values as the exclude.
                SpanNearQuery exclude = new SpanNearQuery(allSpanClauses, slopFactor,
                    inOrder);
                SpanNotQuery snot = new SpanNotQuery(include, exclude);
                return snot;
            }

            private void AddComplexPhraseClause(IList<SpanQuery> spanClauses, BooleanQuery qc)
            {
                JCG.List<SpanQuery> ors = new JCG.List<SpanQuery>();
                JCG.List<SpanQuery> nots = new JCG.List<SpanQuery>();
                BooleanClause[] bclauses = qc.GetClauses();

                // For all clauses e.g. one* two~
                for (int i = 0; i < bclauses.Length; i++)
                {
                    Query childQuery = bclauses[i].Query;

                    // select the list to which we will add these options
                    IList<SpanQuery> chosenList = ors;
                    if (bclauses[i].Occur == Occur.MUST_NOT)
                    {
                        chosenList = nots;
                    }

                    if (childQuery is TermQuery tq)
                    {
                        SpanTermQuery stq = new SpanTermQuery(tq.Term);
                        stq.Boost = tq.Boost;
                        chosenList.Add(stq);
                    }
                    else if (childQuery is BooleanQuery cbq)
                    {
                        AddComplexPhraseClause(chosenList, cbq);
                    }
                    else
                    {
                        // LUCENETODO alternatively could call extract terms here?
                        throw new ArgumentException("Unknown query type:"
                            + childQuery.GetType().Name);
                    }
                }
                if (ors.Count == 0)
                {
                    return;
                }
                SpanOrQuery soq = new SpanOrQuery(ors
                    .ToArray());
                if (nots.Count == 0)
                {
                    spanClauses.Add(soq);
                }
                else
                {
                    SpanOrQuery snqs = new SpanOrQuery(nots
                        .ToArray());
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
                result = prime * result + ((field is null) ? 0 : field.GetHashCode());
                result = prime
                    * result
                    + ((phrasedQueryStringContents is null) ? 0
                        : phrasedQueryStringContents.GetHashCode());
                result = prime * result + slopFactor;
                result = prime * result + (inOrder ? 1 : 0);
                return result;
            }

            public override bool Equals(object obj)
            {
                if (this == obj)
                    return true;
                if (obj is null)
                    return false;
                if (GetType() != obj.GetType())
                    return false;
                if (!base.Equals(obj))
                {
                    return false;
                }
                ComplexPhraseQuery other = (ComplexPhraseQuery)obj;
                if (field is null)
                {
                    if (other.field != null)
                        return false;
                }
                else if (!field.Equals(other.field, StringComparison.Ordinal))
                    return false;
                if (phrasedQueryStringContents is null)
                {
                    if (other.phrasedQueryStringContents != null)
                        return false;
                }
                else if (!phrasedQueryStringContents
                  .Equals(other.phrasedQueryStringContents, StringComparison.Ordinal))
                    return false;
                if (slopFactor != other.slopFactor)
                    return false;
                return inOrder == other.inOrder;
            }
        }
    }
}
