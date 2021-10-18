using J2N.Collections.Generic.Extensions;
using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Diagnostics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using JCG = J2N.Collections.Generic;

namespace Lucene.Net.Util
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

    using Analyzer = Lucene.Net.Analysis.Analyzer;
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
    /// Creates queries from the <see cref="Analyzer"/> chain.
    /// <para/>
    /// Example usage:
    /// <code>
    ///     QueryBuilder builder = new QueryBuilder(analyzer);
    ///     Query a = builder.CreateBooleanQuery("body", "just a test");
    ///     Query b = builder.CreatePhraseQuery("body", "another test");
    ///     Query c = builder.CreateMinShouldMatchQuery("body", "another test", 0.5f);
    /// </code>
    /// <para/>
    /// This can also be used as a subclass for query parsers to make it easier
    /// to interact with the analysis chain. Factory methods such as <see cref="NewTermQuery(Term)"/>
    /// are provided so that the generated queries can be customized.
    /// </summary>
    public class QueryBuilder
    {
        private Analyzer analyzer;
        private bool enablePositionIncrements = true;

        /// <summary>
        /// Creates a new <see cref="QueryBuilder"/> using the given analyzer. </summary>
        public QueryBuilder(Analyzer analyzer)
        {
            this.analyzer = analyzer;
        }

        /// <summary>
        /// Creates a boolean query from the query text.
        /// <para/>
        /// This is equivalent to <c>CreateBooleanQuery(field, queryText, Occur.SHOULD)</c> </summary>
        /// <param name="field"> Field name. </param>
        /// <param name="queryText"> Text to be passed to the analyzer. </param>
        /// <returns> <see cref="TermQuery"/> or <see cref="BooleanQuery"/>, based on the analysis
        ///         of <paramref name="queryText"/>. </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual Query CreateBooleanQuery(string field, string queryText)
        {
            return CreateBooleanQuery(field, queryText, Occur.SHOULD);
        }

        /// <summary>
        /// Creates a boolean query from the query text.
        /// </summary>
        /// <param name="field"> Field name </param>
        /// <param name="queryText"> Text to be passed to the analyzer. </param>
        /// <param name="operator"> Operator used for clauses between analyzer tokens. </param>
        /// <returns> <see cref="TermQuery"/> or <see cref="BooleanQuery"/>, based on the analysis
        ///         of <paramref name="queryText"/>. </returns>
        public virtual Query CreateBooleanQuery(string field, string queryText, Occur @operator)
        {
            if (@operator != Occur.SHOULD && @operator != Occur.MUST)
            {
                throw new ArgumentException("invalid operator: only SHOULD or MUST are allowed");
            }
            return CreateFieldQuery(analyzer, @operator, field, queryText, false, 0);
        }

        /// <summary>
        /// Creates a phrase query from the query text.
        /// <para/>
        /// This is equivalent to <c>CreatePhraseQuery(field, queryText, 0)</c> </summary>
        /// <param name="field"> Field name. </param>
        /// <param name="queryText"> Text to be passed to the analyzer. </param>
        /// <returns> <see cref="TermQuery"/>, <see cref="BooleanQuery"/>, <see cref="PhraseQuery"/>, or
        ///         <see cref="MultiPhraseQuery"/>, based on the analysis of <paramref name="queryText"/>. </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual Query CreatePhraseQuery(string field, string queryText)
        {
            return CreatePhraseQuery(field, queryText, 0);
        }

        /// <summary>
        /// Creates a phrase query from the query text.
        /// </summary>
        /// <param name="field"> Field name. </param>
        /// <param name="queryText"> Text to be passed to the analyzer. </param>
        /// <param name="phraseSlop"> number of other words permitted between words in query phrase </param>
        /// <returns> <see cref="TermQuery"/>, <see cref="BooleanQuery"/>, <see cref="PhraseQuery"/>, or
        ///         <see cref="MultiPhraseQuery"/>, based on the analysis of <paramref name="queryText"/>. </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual Query CreatePhraseQuery(string field, string queryText, int phraseSlop)
        {
            return CreateFieldQuery(analyzer, Occur.MUST, field, queryText, true, phraseSlop);
        }

        /// <summary>
        /// Creates a minimum-should-match query from the query text.
        /// </summary>
        /// <param name="field"> Field name. </param>
        /// <param name="queryText"> Text to be passed to the analyzer. </param>
        /// <param name="fraction"> of query terms <c>[0..1]</c> that should match </param>
        /// <returns> <see cref="TermQuery"/> or <see cref="BooleanQuery"/>, based on the analysis
        ///         of <paramref name="queryText"/>. </returns>
        public virtual Query CreateMinShouldMatchQuery(string field, string queryText, float fraction)
        {
            if (float.IsNaN(fraction) || fraction < 0 || fraction > 1)
            {
                throw new ArgumentException("fraction should be >= 0 and <= 1");
            }

            // TODO: wierd that BQ equals/rewrite/scorer doesn't handle this?
            if (fraction == 1)
            {
                return CreateBooleanQuery(field, queryText, Occur.MUST);
            }

            Query query = CreateFieldQuery(analyzer, Occur.SHOULD, field, queryText, false, 0);
            if (query is BooleanQuery bq)
            {
                bq.MinimumNumberShouldMatch = (int)(fraction * bq.Clauses.Count);
            }
            return query;
        }

        /// <summary>
        /// Gets or Sets the analyzer. </summary>
        public virtual Analyzer Analyzer
        {
            get => analyzer;
            set => this.analyzer = value;
        }

        /// <summary>
        /// Gets or Sets whether position increments are enabled.
        /// <para/>
        /// When <c>true</c>, result phrase and multi-phrase queries will
        /// be aware of position increments.
        /// Useful when e.g. a StopFilter increases the position increment of
        /// the token that follows an omitted token.
        /// <para/>
        /// Default: true.
        /// </summary>
        public virtual bool EnablePositionIncrements
        {
            get => enablePositionIncrements;
            set => this.enablePositionIncrements = value;
        }

        /// <summary>
        /// Creates a query from the analysis chain.
        /// <para/>
        /// Expert: this is more useful for subclasses such as queryparsers.
        /// If using this class directly, just use <see cref="CreateBooleanQuery(string, string)"/>
        /// and <see cref="CreatePhraseQuery(string, string)"/>. </summary>
        /// <param name="analyzer"> Analyzer used for this query. </param>
        /// <param name="operator"> Default boolean operator used for this query. </param>
        /// <param name="field"> Field to create queries against. </param>
        /// <param name="queryText"> Text to be passed to the analysis chain. </param>
        /// <param name="quoted"> <c>true</c> if phrases should be generated when terms occur at more than one position. </param>
        /// <param name="phraseSlop"> Slop factor for phrase/multiphrase queries. </param>
        protected Query CreateFieldQuery(Analyzer analyzer, Occur @operator, string field, string queryText, bool quoted, int phraseSlop)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(@operator == Occur.SHOULD || @operator == Occur.MUST);
            // Use the analyzer to get all the tokens, and then build a TermQuery,
            // PhraseQuery, or nothing based on the term count
            CachingTokenFilter buffer = null;
            ITermToBytesRefAttribute termAtt = null;
            IPositionIncrementAttribute posIncrAtt = null;
            int numTokens = 0;
            int positionCount = 0;
            bool severalTokensAtSamePosition = false;
            bool hasMoreTokens/* = false*/; // LUCENENET: IDE0059: Remove unnecessary value assignment

            TokenStream source = null;
            try
            {
                source = analyzer.GetTokenStream(field, new StringReader(queryText));
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
                    catch (Exception e) when (e.IsIOException())
                    {
                        // ignore
                    }
                }
            }
            catch (Exception e) when (e.IsIOException())
            {
                throw RuntimeException.Create("Error analyzing query text", e);
            }
            finally
            {
                IOUtils.DisposeWhileHandlingException(source);
            }

            // rewind the buffer stream
            buffer.Reset();

            BytesRef bytes = termAtt?.BytesRef;

            if (numTokens == 0)
            {
                return null;
            }
            else if (numTokens == 1)
            {
                try
                {
                    bool hasNext = buffer.IncrementToken();
                    if (Debugging.AssertsEnabled) Debugging.Assert(hasNext == true);
                    termAtt.FillBytesRef();
                }
                catch (Exception e) when (e.IsIOException())
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
                                    if (Debugging.AssertsEnabled) Debugging.Assert(hasNext == true);
                                    termAtt.FillBytesRef();
                                }
                                catch (Exception e) when (e.IsIOException())
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
                                    if (Debugging.AssertsEnabled) Debugging.Assert(hasNext == true);
                                    termAtt.FillBytesRef();
                                }
                                catch (Exception e) when (e.IsIOException())
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
                        IList<Term> multiTerms = new JCG.List<Term>();
                        int position = -1;
                        for (int i = 0; i < numTokens; i++)
                        {
                            int positionIncrement = 1;
                            try
                            {
                                bool hasNext = buffer.IncrementToken();
                                if (Debugging.AssertsEnabled) Debugging.Assert(hasNext == true);
                                termAtt.FillBytesRef();
                                if (posIncrAtt != null)
                                {
                                    positionIncrement = posIncrAtt.PositionIncrement;
                                }
                            }
                            catch (Exception e) when (e.IsIOException())
                            {
                                // safe to ignore, because we know the number of tokens
                            }

                            if (positionIncrement > 0 && multiTerms.Count > 0)
                            {
                                if (enablePositionIncrements)
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
                        if (enablePositionIncrements)
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
                            if (Debugging.AssertsEnabled) Debugging.Assert(hasNext == true);
                            termAtt.FillBytesRef();
                            if (posIncrAtt != null)
                            {
                                positionIncrement = posIncrAtt.PositionIncrement;
                            }
                        }
                        catch (Exception e) when (e.IsIOException())
                        {
                            // safe to ignore, because we know the number of tokens
                        }

                        if (enablePositionIncrements)
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
        /// Builds a new <see cref="BooleanQuery"/> instance.
        /// <para/>
        /// This is intended for subclasses that wish to customize the generated queries. 
        /// </summary>
        /// <param name="disableCoord"> Disable coord. </param>
        /// <returns> New <see cref="BooleanQuery"/> instance. </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected virtual BooleanQuery NewBooleanQuery(bool disableCoord)
        {
            return new BooleanQuery(disableCoord);
        }

        /// <summary>
        /// Builds a new <see cref="TermQuery"/> instance.
        /// <para/>
        /// This is intended for subclasses that wish to customize the generated queries. 
        /// </summary>
        /// <param name="term"> Term. </param>
        /// <returns> New <see cref="TermQuery"/> instance. </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected virtual Query NewTermQuery(Term term)
        {
            return new TermQuery(term);
        }

        /// <summary>
        /// Builds a new <see cref="PhraseQuery"/> instance.
        /// <para/>
        /// This is intended for subclasses that wish to customize the generated queries. 
        /// </summary>
        /// <returns> New <see cref="PhraseQuery"/> instance. </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected virtual PhraseQuery NewPhraseQuery()
        {
            return new PhraseQuery();
        }

        /// <summary>
        /// Builds a new <see cref="MultiPhraseQuery"/> instance.
        /// <para/>
        /// This is intended for subclasses that wish to customize the generated queries. 
        /// </summary>
        /// <returns> New <see cref="MultiPhraseQuery"/> instance. </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected virtual MultiPhraseQuery NewMultiPhraseQuery()
        {
            return new MultiPhraseQuery();
        }
    }
}