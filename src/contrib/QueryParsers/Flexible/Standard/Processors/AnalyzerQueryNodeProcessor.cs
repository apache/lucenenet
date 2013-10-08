using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Tokenattributes;
using Lucene.Net.QueryParsers.Flexible.Core.Nodes;
using Lucene.Net.QueryParsers.Flexible.Core.Processors;
using Lucene.Net.QueryParsers.Flexible.Standard.Config;
using Lucene.Net.QueryParsers.Flexible.Standard.Nodes;
using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.QueryParsers.Flexible.Standard.Processors
{
    public class AnalyzerQueryNodeProcessor : QueryNodeProcessor
    {
        private Analyzer analyzer;

        private bool positionIncrementsEnabled;

        public AnalyzerQueryNodeProcessor()
        {
            // empty constructor
        }

        public override IQueryNode Process(IQueryNode queryTree)
        {
            Analyzer analyzer = QueryConfigHandler.Get(StandardQueryConfigHandler.ConfigurationKeys.ANALYZER);

            if (analyzer != null)
            {
                this.analyzer = analyzer;
                this.positionIncrementsEnabled = false;
                bool? positionIncrementsEnabled = QueryConfigHandler.Get(StandardQueryConfigHandler.ConfigurationKeys.ENABLE_POSITION_INCREMENTS);

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
                String text = fieldNode.TextAsString;
                String field = fieldNode.FieldAsString;

                TokenStream source;
                try
                {
                    source = this.analyzer.TokenStream(field, new StringReader(text));
                    source.Reset();
                }
                catch (IOException)
                {
                    throw;
                }
                CachingTokenFilter buffer = new CachingTokenFilter(source);

                IPositionIncrementAttribute posIncrAtt = null;
                int numTokens = 0;
                int positionCount = 0;
                bool severalTokensAtSamePosition = false;

                if (buffer.HasAttribute<IPositionIncrementAttribute>())
                {
                    posIncrAtt = buffer.GetAttribute<IPositionIncrementAttribute>();
                }

                try
                {

                    while (buffer.IncrementToken())
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

                    }

                }
                catch (IOException)
                {
                    // ignore
                }

                try
                {
                    // rewind the buffer stream
                    buffer.Reset();

                    // close original stream - all tokens buffered
                    source.Dispose();
                }
                catch (IOException)
                {
                    // ignore
                }

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
                    String term = null;
                    try
                    {
                        bool hasNext;
                        hasNext = buffer.IncrementToken();
                        //assert hasNext == true;
                        term = termAtt.ToString();

                    }
                    catch (IOException)
                    {
                        // safe to ignore, because we know the number of tokens
                    }

                    fieldNode.Text = new StringCharSequenceWrapper(term);

                    return fieldNode;

                }
                else if (severalTokensAtSamePosition || !(node is QuotedFieldQueryNode))
                {
                    if (positionCount == 1 || !(node is QuotedFieldQueryNode))
                    {
                        // no phrase query:
                        List<IQueryNode> children = new List<IQueryNode>();

                        for (int i = 0; i < numTokens; i++)
                        {
                            String term = null;
                            try
                            {
                                bool hasNext = buffer.IncrementToken();
                                //assert hasNext == true;
                                term = termAtt.ToString();

                            }
                            catch (IOException)
                            {
                                // safe to ignore, because we know the number of tokens
                            }

                            children.Add(new FieldQueryNode(new StringCharSequenceWrapper(field), new StringCharSequenceWrapper(term), -1, -1));

                        }
                        return new GroupQueryNode(
                          new StandardBooleanQueryNode(children, positionCount == 1));
                    }
                    else
                    {
                        // phrase query:
                        MultiPhraseQueryNode mpq = new MultiPhraseQueryNode();

                        IList<FieldQueryNode> multiTerms = new List<FieldQueryNode>();
                        int position = -1;
                        int i = 0;
                        int termGroupCount = 0;
                        for (; i < numTokens; i++)
                        {
                            String term = null;
                            int positionIncrement = 1;
                            try
                            {
                                bool hasNext = buffer.IncrementToken();
                                //assert hasNext == true;
                                term = termAtt.ToString();
                                if (posIncrAtt != null)
                                {
                                    positionIncrement = posIncrAtt.PositionIncrement;
                                }

                            }
                            catch (IOException)
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
                            multiTerms.Add(new FieldQueryNode(new StringCharSequenceWrapper(field), new StringCharSequenceWrapper(term), -1, -1));

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
                        String term = null;
                        int positionIncrement = 1;

                        try
                        {
                            bool hasNext = buffer.IncrementToken();
                            //assert hasNext == true;
                            term = termAtt.ToString();

                            if (posIncrAtt != null)
                            {
                                positionIncrement = posIncrAtt.PositionIncrement;
                            }

                        }
                        catch (IOException)
                        {
                            // safe to ignore, because we know the number of tokens
                        }

                        FieldQueryNode newFieldNode = new FieldQueryNode(new StringCharSequenceWrapper(field), new StringCharSequenceWrapper(term), -1, -1);

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
