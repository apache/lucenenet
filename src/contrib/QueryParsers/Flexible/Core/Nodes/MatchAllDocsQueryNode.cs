using Lucene.Net.QueryParsers.Flexible.Core.Parser;
using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.QueryParsers.Flexible.Core.Nodes
{
    public class MatchAllDocsQueryNode : QueryNode
    {
        public MatchAllDocsQueryNode()
        {
            // empty constructor
        }

        public override string ToString()
        {
            return "<matchAllDocs field='*' term='*'/>";
        }

        public override ICharSequence ToQueryString(IEscapeQuerySyntax escapeSyntaxParser)
        {
            return new StringCharSequenceWrapper("*:*");
        }

        public override IQueryNode CloneTree()
        {
            MatchAllDocsQueryNode clone = (MatchAllDocsQueryNode)base.CloneTree();

            // nothing to clone

            return clone;
        }
    }
}
