using Lucene.Net.QueryParsers.Flexible.Core.Parser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lucene.Net.Search;

namespace Lucene.Net.QueryParsers.Flexible.Core.Nodes
{
    /// <summary>
    /// A {@link QueryNode} is a interface implemented by all nodes on a QueryNode
    /// tree.
    /// </summary>
    public interface IQueryNode
    {
        /** convert to a query string understood by the query parser */
        // TODO: this interface might be changed in the future
        string ToQueryString(IEscapeQuerySyntax escapeSyntaxParser);

        /** for printing */
        
  //public override string ToString();

        /** get Children nodes */
        IList<IQueryNode> GetChildren();

        /** verify if a node is a Leaf node */
        bool IsLeaf();

        /** verify if a node contains a tag */
        bool ContainsTag(string tagName);

        /**
         * Returns object stored under that tag name
         */
        object GetTag(string tagName);

        IQueryNode GetParent();

        /**
         * Recursive clone the QueryNode tree The tags are not copied to the new tree
         * when you call the cloneTree() method
         * 
         * @return the cloned tree
         */
        IQueryNode CloneTree();

        // Below are the methods that can change state of a QueryNode
        // Write Operations (not Thread Safe)

        // add a new child to a non Leaf node
        void Add(IQueryNode child);

        void Add(IList<IQueryNode> children);

        // reset the children of a node
        void Set(IList<IQueryNode> children);

        /**
         * Associate the specified value with the specified tagName. If the tagName
         * already exists, the old value is replaced. The tagName and value cannot be
         * null. tagName will be converted to lowercase.
         */
        void SetTag(string tagName, object value);

        /**
         * Unset a tag. tagName will be converted to lowercase.
         */
        void UnsetTag(string tagName);

        /**
         * Returns a map containing all tags attached to this query node. 
         * 
         * @return a map containing all tags attached to this query node
         */
        IDictionary<string, object> GetTagMap();

        /**
         * Removes this query node from its parent.
         */
        void RemoveFromParent();
    }
}
