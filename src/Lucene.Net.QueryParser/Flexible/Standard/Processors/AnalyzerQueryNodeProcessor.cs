using J2N.Text;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Diagnostics;
using Lucene.Net.QueryParsers.Flexible.Core.Nodes;
using Lucene.Net.QueryParsers.Flexible.Core.Processors;
using Lucene.Net.QueryParsers.Flexible.Standard.Config;
using Lucene.Net.QueryParsers.Flexible.Standard.Nodes;
using Lucene.Net.Support;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using JCG = J2N.Collections.Generic;
using Operator = Lucene.Net.QueryParsers.Flexible.Standard.Config.StandardQueryConfigHandler.Operator;

namespace Lucene.Net.QueryParsers.Flexible.Standard.Processors
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
    /// This processor verifies if <see cref="ConfigurationKeys.ANALYZER"/>
    /// is defined in the <see cref="Core.Config.QueryConfigHandler"/>. If it is and the analyzer is
    /// not <c>null</c>, it looks for every <see cref="FieldQueryNode"/> that is not
    /// <see cref="WildcardQueryNode"/>, <see cref="FuzzyQueryNode"/> or
    /// <see cref="IRangeQueryNode"/> contained in the query node tree, then it applies
    /// the analyzer to that <see cref="FieldQueryNode"/> object.
    /// <para/>
    /// If the analyzer return only one term, the returned term is set to the
    /// <see cref="FieldQueryNode"/> and it's returned.
    /// <para/>
    /// If the analyzer return more than one term, a <see cref="TokenizedPhraseQueryNode"/>
    /// or <see cref="MultiPhraseQueryNode"/> is created, whether there is one or more
    /// terms at the same position, and it's returned.
    /// <para/>
    /// If no term is returned by the analyzer a <see cref="NoTokenFoundQueryNode"/> object
    /// is returned.
    /// </summary>
    /// <seealso cref="ConfigurationKeys.ANALYZER"/>
    /// <seealso cref="Analyzer"/>
    /// <seealso cref="TokenStream"/>
    public class AnalyzerQueryNodeProcessor : QueryNodeProcessor
    {
        private Analyzer analyzer;

        private bool positionIncrementsEnabled;

        private Operator defaultOperator;

        public AnalyzerQueryNodeProcessor()
        {
            // empty constructor
        }

        public override IQueryNode Process(IQueryNode queryTree)
        {
            var queryConfigHandler = GetQueryConfigHandler();
            Analyzer analyzer = queryConfigHandler.Get(ConfigurationKeys.ANALYZER);

            if (analyzer != null)
            {
                this.analyzer = analyzer;
                this.positionIncrementsEnabled = false;

                // LUCENENET specific - rather than using null, we are relying on the behavior that the default
                // value for an enum is 0 (OR in this case).
                this.defaultOperator = GetQueryConfigHandler().Get(ConfigurationKeys.DEFAULT_OPERATOR);

                // LUCENENET: Use TryGetValue() to determine if the value exists
                if (GetQueryConfigHandler().TryGetValue(ConfigurationKeys.ENABLE_POSITION_INCREMENTS, out bool positionIncrementsEnabled))
                {
                    this.positionIncrementsEnabled = positionIncrementsEnabled;
                }

                if (this.analyzer != null)
                {
                    return base.Process(queryTree);
                }
            }

            return queryTree;
        }

        protected override IQueryNode PostProcessNode(IQueryNode node)
        {
            if (node is ITextableQueryNode
                && !(node is WildcardQueryNode)
                && !(node is FuzzyQueryNode)
                && !(node is RegexpQueryNode)
                && !(node.Parent is IRangeQueryNode))
            {
                FieldQueryNode fieldNode = ((FieldQueryNode)node);
                string text = fieldNode.GetTextAsString();
                string field = fieldNode.GetFieldAsString();

                CachingTokenFilter buffer = null;
                IPositionIncrementAttribute posIncrAtt = null;
                int numTokens = 0;
                int positionCount = 0;
                bool severalTokensAtSamePosition = false;

                TokenStream source = null;
                try
                {
                    source = this.analyzer.GetTokenStream(field, text);
                    source.Reset();
                    buffer = new CachingTokenFilter(source);

                    if (buffer.HasAttribute<IPositionIncrementAttribute>())
                    {
                        posIncrAtt = buffer.GetAttribute<IPositionIncrementAttribute>();
                    }

                    try
                    {
                        while (buffer.IncrementToken())
                        {
                            numTokens++;
                            int positionIncrement = (posIncrAtt != null) ? posIncrAtt
                                .PositionIncrement : 1;
                            if (positionIncrement != 0)
                            {
                                positionCount += positionIncrement;
                            }
                            else
                            {
                                severalTokensAtSamePosition = true;
                            }
                        }
                    }
                    catch (Exception e) when (e.IsIOException())
                    {
                        // ignore
                    }
                }
                catch (Exception e) when (e.IsIOException())
                {
                    throw RuntimeException.Create(e);
                }
                finally
                {
                    IOUtils.DisposeWhileHandlingException(source);
                }

                // rewind the buffer stream
                buffer.Reset();

                if (!buffer.HasAttribute<ICharTermAttribute>())
                {
                    return new NoTokenFoundQueryNode();
                }

                ICharTermAttribute termAtt = buffer.GetAttribute<ICharTermAttribute>();

                if (numTokens == 0)
                {
                    return new NoTokenFoundQueryNode();

                }
                else if (numTokens == 1)
                {
                    string term = null;
                    try
                    {
                        bool hasNext;
                        hasNext = buffer.IncrementToken();
                        if (Debugging.AssertsEnabled) Debugging.Assert(hasNext == true);
                        term = termAtt.ToString();
                    }
                    catch (Exception e) when (e.IsIOException())
                    {
                        // safe to ignore, because we know the number of tokens
                    }

                    fieldNode.Text = term.AsCharSequence();

                    return fieldNode;
                }
                else if (severalTokensAtSamePosition || !(node is QuotedFieldQueryNode))
                {
                    if (positionCount == 1 || !(node is QuotedFieldQueryNode))
                    {
                        // no phrase query:

                        if (positionCount == 1)
                        {
                            // simple case: only one position, with synonyms
                            IList<IQueryNode> children = new JCG.List<IQueryNode>();

                            for (int i = 0; i < numTokens; i++)
                            {
                                string term = null;
                                try
                                {
                                    bool hasNext = buffer.IncrementToken();
                                    if (Debugging.AssertsEnabled) Debugging.Assert(hasNext == true);
                                    term = termAtt.ToString();
                                }
                                catch (Exception e) when (e.IsIOException())
                                {
                                    // safe to ignore, because we know the number of tokens
                                }

                                children.Add(new FieldQueryNode(field, term, -1, -1));

                            }
                            return new GroupQueryNode(
                                new StandardBooleanQueryNode(children, positionCount == 1));
                        }
                        else
                        {
                            // multiple positions
                            IQueryNode q = new StandardBooleanQueryNode(Collections.EmptyList<IQueryNode>(), false);
                            IQueryNode currentQuery = null;
                            for (int i = 0; i < numTokens; i++)
                            {
                                string term = null;
                                try
                                {
                                    bool hasNext = buffer.IncrementToken();
                                    if (Debugging.AssertsEnabled) Debugging.Assert(hasNext == true);
                                    term = termAtt.ToString();
                                }
                                catch (Exception e) when (e.IsIOException())
                                {
                                    // safe to ignore, because we know the number of tokens
                                }
                                if (posIncrAtt != null && posIncrAtt.PositionIncrement == 0)
                                {
                                    if (!(currentQuery is BooleanQueryNode))
                                    {
                                        IQueryNode t = currentQuery;
                                        currentQuery = new StandardBooleanQueryNode(Collections.EmptyList<IQueryNode>(), true);
                                        ((BooleanQueryNode)currentQuery).Add(t);
                                    }
                                  ((BooleanQueryNode)currentQuery).Add(new FieldQueryNode(field, term, -1, -1));
                                }
                                else
                                {
                                    if (currentQuery != null)
                                    {
                                        if (this.defaultOperator == Operator.OR)
                                        {
                                            q.Add(currentQuery);
                                        }
                                        else
                                        {
                                            q.Add(new ModifierQueryNode(currentQuery, Modifier.MOD_REQ));
                                        }
                                    }
                                    currentQuery = new FieldQueryNode(field, term, -1, -1);
                                }
                            }
                            if (this.defaultOperator == Operator.OR)
                            {
                                q.Add(currentQuery);
                            }
                            else
                            {
                                q.Add(new ModifierQueryNode(currentQuery, Modifier.MOD_REQ));
                            }

                            if (q is BooleanQueryNode)
                            {
                                q = new GroupQueryNode(q);
                            }
                            return q;
                        }
                    }
                    else
                    {
                        // phrase query:
                        MultiPhraseQueryNode mpq = new MultiPhraseQueryNode();

                        IList<FieldQueryNode> multiTerms = new JCG.List<FieldQueryNode>();
                        int position = -1;
                        int i = 0;
                        int termGroupCount = 0;
                        for (; i < numTokens; i++)
                        {
                            string term = null;
                            int positionIncrement = 1;
                            try
                            {
                                bool hasNext = buffer.IncrementToken();
                                if (Debugging.AssertsEnabled) Debugging.Assert(hasNext == true);
                                term = termAtt.ToString();
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
                                foreach (FieldQueryNode termNode in multiTerms)
                                {
                                    if (this.positionIncrementsEnabled)
                                    {
                                        termNode.PositionIncrement = position;
                                    }
                                    else
                                    {
                                        termNode.PositionIncrement = termGroupCount;
                                    }

                                    mpq.Add(termNode);
                                }

                                // Only increment once for each "group" of
                                // terms that were in the same position:
                                termGroupCount++;

                                multiTerms.Clear();
                            }

                            position += positionIncrement;
                            multiTerms.Add(new FieldQueryNode(field, term, -1, -1));
                        }

                        foreach (FieldQueryNode termNode in multiTerms)
                        {
                            if (this.positionIncrementsEnabled)
                            {
                                termNode.PositionIncrement = position;
                            }
                            else
                            {
                                termNode.PositionIncrement = termGroupCount;
                            }

                            mpq.Add(termNode);
                        }

                        return mpq;
                    }
                }
                else
                {
                    TokenizedPhraseQueryNode pq = new TokenizedPhraseQueryNode();

                    int position = -1;

                    for (int i = 0; i < numTokens; i++)
                    {
                        string term = null;
                        int positionIncrement = 1;

                        try
                        {
                            bool hasNext = buffer.IncrementToken();
                            if (Debugging.AssertsEnabled) Debugging.Assert(hasNext == true);
                            term = termAtt.ToString();

                            if (posIncrAtt != null)
                            {
                                positionIncrement = posIncrAtt.PositionIncrement;
                            }
                        }
                        catch (Exception e) when (e.IsIOException())
                        {
                            // safe to ignore, because we know the number of tokens
                        }

                        FieldQueryNode newFieldNode = new FieldQueryNode(field, term, -1, -1);

                        if (this.positionIncrementsEnabled)
                        {
                            position += positionIncrement;
                            newFieldNode.PositionIncrement = position;
                        }
                        else
                        {
                            newFieldNode.PositionIncrement = i;
                        }

                        pq.Add(newFieldNode);
                    }

                    return pq;
                }
            }

            return node;
        }

        protected override IQueryNode PreProcessNode(IQueryNode node)
        {
            return node;
        }

        protected override IList<IQueryNode> SetChildrenOrder(IList<IQueryNode> children)
        {
            return children;
        }
    }
}
