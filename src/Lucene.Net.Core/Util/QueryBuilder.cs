using Lucene.Net.Analysis.TokenAttributes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Lucene.Net.Util
{
    using System.IO;

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

    using Analyzer = Lucene.Net.Analysis.Analyzer;
    using BooleanClause = Lucene.Net.Search.BooleanClause;
    using BooleanQuery = Lucene.Net.Search.BooleanQuery;
    using CachingTokenFilter = Lucene.Net.Analysis.CachingTokenFilter;
    using MultiPhraseQuery = Lucene.Net.Search.MultiPhraseQuery;
    using Occur = Lucene.Net.Search.Occur;
    using PhraseQuery = Lucene.Net.Search.PhraseQuery;
    using Query = Lucene.Net.Search.Query;
    using Term = Lucene.Net.Index.Term;
    using TermQuery = Lucene.Net.Search.TermQuery;
    using TokenStream = Lucene.Net.Analysis.TokenStream;

    /// <summary>
    /// Creates queries from the <seealso cref="Analyzer"/> chain.
    /// <p>
    /// Example usage:
    /// <pre class="prettyprint">
    ///   QueryBuilder builder = new QueryBuilder(analyzer);
    ///   Query a = builder.createBooleanQuery("body", "just a test");
    ///   Query b = builder.createPhraseQuery("body", "another test");
    ///   Query c = builder.createMinShouldMatchQuery("body", "another test", 0.5f);
    /// </pre>
    /// <p>
    /// this can also be used as a subclass for query parsers to make it easier
    /// to interact with the analysis chain. Factory methods such as {@code newTermQuery}
    /// are provided so that the generated queries can be customized.
    /// </summary>
    public class QueryBuilder
    {
        private Analyzer Analyzer_Renamed;
        private bool EnablePositionIncrements_Renamed = true;

        /// <summary>
        /// Creates a new QueryBuilder using the given analyzer. </summary>
        public QueryBuilder(Analyzer analyzer)
        {
            this.Analyzer_Renamed = analyzer;
        }

        /// <summary>
        /// Creates a boolean query from the query text.
        /// <p>
        /// this is equivalent to {@code createBooleanQuery(field, queryText, Occur.SHOULD)} </summary>
        /// <param name="field"> field name </param>
        /// <param name="queryText"> text to be passed to the analyzer </param>
        /// <returns> {@code TermQuery} or {@code BooleanQuery}, based on the analysis
        ///         of {@code queryText} </returns>
        public virtual Query CreateBooleanQuery(string field, string queryText)
        {
            return CreateBooleanQuery(field, queryText, Occur.SHOULD);
        }

        /// <summary>
        /// Creates a boolean query from the query text.
        /// <p> </summary>
        /// <param name="field"> field name </param>
        /// <param name="queryText"> text to be passed to the analyzer </param>
        /// <param name="operator"> operator used for clauses between analyzer tokens. </param>
        /// <returns> {@code TermQuery} or {@code BooleanQuery}, based on the analysis
        ///         of {@code queryText} </returns>
        public virtual Query CreateBooleanQuery(string field, string queryText, Occur @operator)
        {
            if (@operator != Occur.SHOULD && @operator != Occur.MUST)
            {
                throw new System.ArgumentException("invalid operator: only SHOULD or MUST are allowed");
            }
            return CreateFieldQuery(Analyzer_Renamed, @operator, field, queryText, false, 0);
        }

        /// <summary>
        /// Creates a phrase query from the query text.
        /// <p>
        /// this is equivalent to {@code createPhraseQuery(field, queryText, 0)} </summary>
        /// <param name="field"> field name </param>
        /// <param name="queryText"> text to be passed to the analyzer </param>
        /// <returns> {@code TermQuery}, {@code BooleanQuery}, {@code PhraseQuery}, or
        ///         {@code MultiPhraseQuery}, based on the analysis of {@code queryText} </returns>
        public virtual Query CreatePhraseQuery(string field, string queryText)
        {
            return CreatePhraseQuery(field, queryText, 0);
        }

        /// <summary>
        /// Creates a phrase query from the query text.
        /// <p> </summary>
        /// <param name="field"> field name </param>
        /// <param name="queryText"> text to be passed to the analyzer </param>
        /// <param name="phraseSlop"> number of other words permitted between words in query phrase </param>
        /// <returns> {@code TermQuery}, {@code BooleanQuery}, {@code PhraseQuery}, or
        ///         {@code MultiPhraseQuery}, based on the analysis of {@code queryText} </returns>
        public virtual Query CreatePhraseQuery(string field, string queryText, int phraseSlop)
        {
            return CreateFieldQuery(Analyzer_Renamed, Occur.MUST, field, queryText, true, phraseSlop);
        }

        /// <summary>
        /// Creates a minimum-should-match query from the query text.
        /// <p> </summary>
        /// <param name="field"> field name </param>
        /// <param name="queryText"> text to be passed to the analyzer </param>
        /// <param name="fraction"> of query terms {@code [0..1]} that should match </param>
        /// <returns> {@code TermQuery} or {@code BooleanQuery}, based on the analysis
        ///         of {@code queryText} </returns>
        public virtual Query CreateMinShouldMatchQuery(string field, string queryText, float fraction)
        {
            if (float.IsNaN(fraction) || fraction < 0 || fraction > 1)
            {
                throw new System.ArgumentException("fraction should be >= 0 and <= 1");
            }

            // TODO: wierd that BQ equals/rewrite/scorer doesn't handle this?
            if (fraction == 1)
            {
                return CreateBooleanQuery(field, queryText, Occur.MUST);
            }

            Query query = CreateFieldQuery(Analyzer_Renamed, Occur.SHOULD, field, queryText, false, 0);
            if (query is BooleanQuery)
            {
                BooleanQuery bq = (BooleanQuery)query;
                bq.MinimumNumberShouldMatch = (int)(fraction * bq.GetClauses().Count);
            }
            return query;
        }

        /// <summary>
        /// Returns the analyzer. </summary>
        /// <seealso cref= #setAnalyzer(Analyzer) </seealso>
        public virtual Analyzer Analyzer
        {
            get
            {
                return Analyzer_Renamed;
            }
            set
            {
                this.Analyzer_Renamed = value;
            }
        }

        /// <summary>
        /// Returns true if position increments are enabled. </summary>
        /// <seealso cref= #setEnablePositionIncrements(boolean) </seealso>
        public virtual bool EnablePositionIncrements
        {
            get
            {
                return EnablePositionIncrements_Renamed;
            }
            set
            {
                this.EnablePositionIncrements_Renamed = value;
            }
        }

        /// <summary>
        /// Creates a query from the analysis chain.
        /// <p>
        /// Expert: this is more useful for subclasses such as queryparsers.
        /// If using this class directly, just use <seealso cref="#createBooleanQuery(String, String)"/>
        /// and <seealso cref="#createPhraseQuery(String, String)"/> </summary>
        /// <param name="analyzer"> analyzer used for this query </param>
        /// <param name="operator"> default boolean operator used for this query </param>
        /// <param name="field"> field to create queries against </param>
        /// <param name="queryText"> text to be passed to the analysis chain </param>
        /// <param name="quoted"> true if phrases should be generated when terms occur at more than one position </param>
        /// <param name="phraseSlop"> slop factor for phrase/multiphrase queries </param>
        protected internal Query CreateFieldQuery(Analyzer analyzer, Occur @operator, string field, string queryText, bool quoted, int phraseSlop)
        {
            Debug.Assert(@operator == Occur.SHOULD || @operator == Occur.MUST);
            // Use the analyzer to get all the tokens, and then build a TermQuery,
            // PhraseQuery, or nothing based on the term count
            CachingTokenFilter buffer = null;
            ITermToBytesRefAttribute termAtt = null;
            IPositionIncrementAttribute posIncrAtt = null;
            int numTokens = 0;
            int positionCount = 0;
            bool severalTokensAtSamePosition = false;
            bool hasMoreTokens = false;

            TokenStream source = null;
            try
            {
                source = analyzer.TokenStream(field, new StringReader(queryText));
                source.Reset();
                buffer = new CachingTokenFilter(source);
                buffer.Reset();

                if (buffer.HasAttribute<ITermToBytesRefAttribute>())
                {
                    termAtt = buffer.GetAttribute<ITermToBytesRefAttribute>();
                }
                if (buffer.HasAttribute<IPositionIncrementAttribute>())
                {
                    posIncrAtt = buffer.GetAttribute<IPositionIncrementAttribute>();
                }

                if (termAtt != null)
                {
                    try
                    {
                        hasMoreTokens = buffer.IncrementToken();
                        while (hasMoreTokens)
                        {
                            numTokens++;
                            int positionIncrement = (posIncrAtt != null) ? posIncrAtt.PositionIncrement : 1;
                            if (positionIncrement != 0)
                            {
                                positionCount += positionIncrement;
                            }
                            else
                            {
                                severalTokensAtSamePosition = true;
                            }
                            hasMoreTokens = buffer.IncrementToken();
                        }
                    }
                    catch (System.IO.IOException)
                    {
                        // ignore
                    }
                }
            }
            catch (System.IO.IOException e)
            {
                throw new Exception("Error analyzing query text", e);
            }
            finally
            {
                IOUtils.CloseWhileHandlingException(source);
            }

            // rewind the buffer stream
            buffer.Reset();

            BytesRef bytes = termAtt == null ? null : termAtt.BytesRef;

            if (numTokens == 0)
            {
                return null;
            }
            else if (numTokens == 1)
            {
                try
                {
                    bool hasNext = buffer.IncrementToken();
                    Debug.Assert(hasNext == true);
                    termAtt.FillBytesRef();
                }
                catch (System.IO.IOException)
                {
                    // safe to ignore, because we know the number of tokens
                }
                return NewTermQuery(new Term(field, BytesRef.DeepCopyOf(bytes)));
            }
            else
            {
                if (severalTokensAtSamePosition || (!quoted))
                {
                    if (positionCount == 1 || (!quoted))
                    {
                        // no phrase query:

                        if (positionCount == 1)
                        {
                            // simple case: only one position, with synonyms
                            BooleanQuery q = NewBooleanQuery(true);
                            for (int i = 0; i < numTokens; i++)
                            {
                                try
                                {
                                    bool hasNext = buffer.IncrementToken();
                                    Debug.Assert(hasNext == true);
                                    termAtt.FillBytesRef();
                                }
                                catch (System.IO.IOException)
                                {
                                    // safe to ignore, because we know the number of tokens
                                }
                                Query currentQuery = NewTermQuery(new Term(field, BytesRef.DeepCopyOf(bytes)));
                                q.Add(currentQuery, Occur.SHOULD);
                            }
                            return q;
                        }
                        else
                        {
                            // multiple positions
                            BooleanQuery q = NewBooleanQuery(false);
                            Query currentQuery = null;
                            for (int i = 0; i < numTokens; i++)
                            {
                                try
                                {
                                    bool hasNext = buffer.IncrementToken();
                                    Debug.Assert(hasNext == true);
                                    termAtt.FillBytesRef();
                                }
                                catch (System.IO.IOException)
                                {
                                    // safe to ignore, because we know the number of tokens
                                }
                                if (posIncrAtt != null && posIncrAtt.PositionIncrement == 0)
                                {
                                    if (!(currentQuery is BooleanQuery))
                                    {
                                        Query t = currentQuery;
                                        currentQuery = NewBooleanQuery(true);
                                        ((BooleanQuery)currentQuery).Add(t, Occur.SHOULD);
                                    }
                                    ((BooleanQuery)currentQuery).Add(NewTermQuery(new Term(field, BytesRef.DeepCopyOf(bytes))), Occur.SHOULD);
                                }
                                else
                                {
                                    if (currentQuery != null)
                                    {
                                        q.Add(currentQuery, @operator);
                                    }
                                    currentQuery = NewTermQuery(new Term(field, BytesRef.DeepCopyOf(bytes)));
                                }
                            }
                            q.Add(currentQuery, @operator);
                            return q;
                        }
                    }
                    else
                    {
                        // phrase query:
                        MultiPhraseQuery mpq = NewMultiPhraseQuery();
                        mpq.Slop = phraseSlop;
                        IList<Term> multiTerms = new List<Term>();
                        int position = -1;
                        for (int i = 0; i < numTokens; i++)
                        {
                            int positionIncrement = 1;
                            try
                            {
                                bool hasNext = buffer.IncrementToken();
                                Debug.Assert(hasNext == true);
                                termAtt.FillBytesRef();
                                if (posIncrAtt != null)
                                {
                                    positionIncrement = posIncrAtt.PositionIncrement;
                                }
                            }
                            catch (System.IO.IOException)
                            {
                                // safe to ignore, because we know the number of tokens
                            }

                            if (positionIncrement > 0 && multiTerms.Count > 0)
                            {
                                if (EnablePositionIncrements_Renamed)
                                {
                                    mpq.Add(multiTerms.ToArray(), position);
                                }
                                else
                                {
                                    mpq.Add(multiTerms.ToArray());
                                }
                                multiTerms.Clear();
                            }
                            position += positionIncrement;
                            multiTerms.Add(new Term(field, BytesRef.DeepCopyOf(bytes)));
                        }
                        if (EnablePositionIncrements_Renamed)
                        {
                            mpq.Add(multiTerms.ToArray(), position);
                        }
                        else
                        {
                            mpq.Add(multiTerms.ToArray());
                        }
                        return mpq;
                    }
                }
                else
                {
                    PhraseQuery pq = NewPhraseQuery();
                    pq.Slop = phraseSlop;
                    int position = -1;

                    for (int i = 0; i < numTokens; i++)
                    {
                        int positionIncrement = 1;

                        try
                        {
                            bool hasNext = buffer.IncrementToken();
                            Debug.Assert(hasNext == true);
                            termAtt.FillBytesRef();
                            if (posIncrAtt != null)
                            {
                                positionIncrement = posIncrAtt.PositionIncrement;
                            }
                        }
                        catch (System.IO.IOException)
                        {
                            // safe to ignore, because we know the number of tokens
                        }

                        if (EnablePositionIncrements_Renamed)
                        {
                            position += positionIncrement;
                            pq.Add(new Term(field, BytesRef.DeepCopyOf(bytes)), position);
                        }
                        else
                        {
                            pq.Add(new Term(field, BytesRef.DeepCopyOf(bytes)));
                        }
                    }
                    return pq;
                }
            }
        }

        /// <summary>
        /// Builds a new BooleanQuery instance.
        /// <p>
        /// this is intended for subclasses that wish to customize the generated queries. </summary>
        /// <param name="disableCoord"> disable coord </param>
        /// <returns> new BooleanQuery instance </returns>
        protected internal virtual BooleanQuery NewBooleanQuery(bool disableCoord)
        {
            return new BooleanQuery(disableCoord);
        }

        /// <summary>
        /// Builds a new TermQuery instance.
        /// <p>
        /// this is intended for subclasses that wish to customize the generated queries. </summary>
        /// <param name="term"> term </param>
        /// <returns> new TermQuery instance </returns>
        protected internal virtual Query NewTermQuery(Term term)
        {
            return new TermQuery(term);
        }

        /// <summary>
        /// Builds a new PhraseQuery instance.
        /// <p>
        /// this is intended for subclasses that wish to customize the generated queries. </summary>
        /// <returns> new PhraseQuery instance </returns>
        protected internal virtual PhraseQuery NewPhraseQuery()
        {
            return new PhraseQuery();
        }

        /// <summary>
        /// Builds a new MultiPhraseQuery instance.
        /// <p>
        /// this is intended for subclasses that wish to customize the generated queries. </summary>
        /// <returns> new MultiPhraseQuery instance </returns>
        protected internal virtual MultiPhraseQuery NewMultiPhraseQuery()
        {
            return new MultiPhraseQuery();
        }
    }
}