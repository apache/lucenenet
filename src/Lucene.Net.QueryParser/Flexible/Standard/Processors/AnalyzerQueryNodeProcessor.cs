using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Tokenattributes;
using Lucene.Net.QueryParsers.Flexible.Core.Nodes;
using Lucene.Net.QueryParsers.Flexible.Core.Processors;
using Lucene.Net.QueryParsers.Flexible.Standard.Config;
using Lucene.Net.QueryParsers.Flexible.Standard.Nodes;
using Lucene.Net.Support;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Lucene.Net.QueryParsers.Classic.QueryParserBase;

namespace Lucene.Net.QueryParsers.Flexible.Standard.Processors
{
    /// <summary>
    /// This processor verifies if {@link ConfigurationKeys#ANALYZER}
    /// is defined in the {@link QueryConfigHandler}. If it is and the analyzer is
    /// not <code>null</code>, it looks for every {@link FieldQueryNode} that is not
    /// {@link WildcardQueryNode}, {@link FuzzyQueryNode} or
    /// {@link RangeQueryNode} contained in the query node tree, then it applies
    /// the analyzer to that {@link FieldQueryNode} object.
    /// <para/>
    /// If the analyzer return only one term, the returned term is set to the
    /// {@link FieldQueryNode} and it's returned.
    /// <para/>
    /// If the analyzer return more than one term, a {@link TokenizedPhraseQueryNode}
    /// or {@link MultiPhraseQueryNode} is created, whether there is one or more
    /// terms at the same position, and it's returned.
    /// <para/>
    /// If no term is returned by the analyzer a {@link NoTokenFoundQueryNode} object
    /// is returned.
    /// </summary>
    /// <seealso cref="ConfigurationKeys#ANALYZER"/>
    /// <seealso cref="Analyzer"/>
    /// <seealso cref="TokenStream"/>
    public class AnalyzerQueryNodeProcessor : QueryNodeProcessorImpl
    {
        private Analyzer analyzer;

        private bool positionIncrementsEnabled;

        private Config.Operator defaultOperator;

        public AnalyzerQueryNodeProcessor()
        {
            // empty constructor
        }


        public override IQueryNode Process(IQueryNode queryTree)
        {
            Analyzer analyzer = GetQueryConfigHandler().Get(ConfigurationKeys.ANALYZER);

            if (analyzer != null)
            {
                this.analyzer = analyzer;
                this.positionIncrementsEnabled = false;
                bool? positionIncrementsEnabled = GetQueryConfigHandler().Get(ConfigurationKeys.ENABLE_POSITION_INCREMENTS);
                var defaultOperator = GetQueryConfigHandler().Get(ConfigurationKeys.DEFAULT_OPERATOR);
                this.defaultOperator = defaultOperator != null ? defaultOperator.Value : Config.Operator.OR;

                if (positionIncrementsEnabled != null)
                {
                    this.positionIncrementsEnabled = positionIncrementsEnabled.Value;
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
                    source = this.analyzer.TokenStream(field, text);
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
                    catch (IOException e)
                    {
                        // ignore
                    }
                }
                catch (IOException e)
                {
                    throw new Exception(e.Message, e);
                }
                finally
                {
                    IOUtils.CloseWhileHandlingException(source);
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
                        Debug.Assert(hasNext == true);
                        term = termAtt.ToString();

                    }
                    catch (IOException e)
                    {
                        // safe to ignore, because we know the number of tokens
                    }

                    fieldNode.Text = term.ToCharSequence();

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
                            List<IQueryNode> children = new List<IQueryNode>();

                            for (int i = 0; i < numTokens; i++)
                            {
                                string term = null;
                                try
                                {
                                    bool hasNext = buffer.IncrementToken();
                                    Debug.Assert(hasNext == true);
                                    term = termAtt.ToString();

                                }
                                catch (IOException e)
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
                            IQueryNode q = new StandardBooleanQueryNode(new List<IQueryNode>(), false);
                            IQueryNode currentQuery = null;
                            for (int i = 0; i < numTokens; i++)
                            {
                                string term = null;
                                try
                                {
                                    bool hasNext = buffer.IncrementToken();
                                    Debug.Assert(hasNext == true);
                                    term = termAtt.ToString();
                                }
                                catch (IOException e)
                                {
                                    // safe to ignore, because we know the number of tokens
                                }
                                if (posIncrAtt != null && posIncrAtt.PositionIncrement == 0)
                                {
                                    if (!(currentQuery is BooleanQueryNode))
                                    {
                                        IQueryNode t = currentQuery;
                                        currentQuery = new StandardBooleanQueryNode(new List<IQueryNode>(), true);
                                        ((BooleanQueryNode)currentQuery).Add(t);
                                    }
                                  ((BooleanQueryNode)currentQuery).Add(new FieldQueryNode(field, term, -1, -1));
                                }
                                else
                                {
                                    if (currentQuery != null)
                                    {
                                        if (this.defaultOperator == Config.Operator.OR)
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
                            if (this.defaultOperator == Config.Operator.OR)
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

                        List<FieldQueryNode> multiTerms = new List<FieldQueryNode>();
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
                                Debug.Assert(hasNext == true);
                                term = termAtt.ToString();
                                if (posIncrAtt != null)
                                {
                                    positionIncrement = posIncrAtt.PositionIncrement;
                                }

                            }
                            catch (IOException e)
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
                            Debug.Assert(hasNext == true);
                            term = termAtt.ToString();

                            if (posIncrAtt != null)
                            {
                                positionIncrement = posIncrAtt.PositionIncrement;
                            }

                        }
                        catch (IOException e)
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
