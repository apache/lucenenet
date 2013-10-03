using Lucene.Net.QueryParsers.Flexible.Core.Parser;
using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.QueryParsers.Flexible.Core.Nodes
{
    public interface IQueryNode
    {
        ICharSequence ToQueryString(IEscapeQuerySyntax escapeSyntaxParser);

        IList<IQueryNode> Children { get; }

        bool IsLeaf { get; }

        bool ContainsTag(string tagName);

        object GetTag(string tagName);

        IQueryNode Parent { get; }

        IQueryNode CloneTree();

        void Add(IQueryNode child);

        void Add(IList<IQueryNode> children);

        void Set(IList<IQueryNode> children);

        void SetTag(string tagName, object value);

        void UnsetTag(string tagName);

        IDictionary<string, object> TagMap { get; }
    }
}
