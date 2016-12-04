using Lucene.Net.QueryParsers.Flexible.Core.Nodes;
using System.Collections.Generic;

namespace Lucene.Net.QueryParsers.Flexible.Standard.Nodes
{
    /// <summary>
    /// A {@link StandardBooleanQueryNode} has the same behavior as
    /// {@link BooleanQueryNode}. It only indicates if the coord should be enabled or
    /// not for this boolean query. 
    /// </summary>
    /// <seealso cref="Similarity#coord(int, int)"/>
    /// <seealso cref="BooleanQuery"/>
    public class StandardBooleanQueryNode : BooleanQueryNode
    {
        private bool disableCoord;

        public StandardBooleanQueryNode(IList<IQueryNode> clauses, bool disableCoord)
            : base(clauses)
        {
            this.disableCoord = disableCoord;
        }

        public virtual bool DisableCoord
        {
            get { return this.disableCoord; }
        }
    }
}
