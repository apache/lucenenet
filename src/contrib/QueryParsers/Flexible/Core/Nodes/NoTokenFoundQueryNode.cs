using Lucene.Net.QueryParsers.Flexible.Core.Parser;
using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.QueryParsers.Flexible.Core.Nodes
{
    public class NoTokenFoundQueryNode : DeletedQueryNode
    {
        public NoTokenFoundQueryNode()
            : base()
        {
        }

        public override ICharSequence ToQueryString(IEscapeQuerySyntax escapeSyntaxParser)
        {
            return new StringCharSequenceWrapper("[NTF]");
        }

        public override string ToString()
        {
            return "<notokenfound/>";
        }

        public override IQueryNode CloneTree()
        {
            NoTokenFoundQueryNode clone = (NoTokenFoundQueryNode)base.CloneTree();

            // nothing to do here

            return clone;
        }
    }
}
