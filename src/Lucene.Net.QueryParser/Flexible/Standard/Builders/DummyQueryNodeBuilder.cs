using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lucene.Net.QueryParsers.Flexible.Core.Builders;
using Lucene.Net.QueryParsers.Flexible.Core.Nodes;
using Lucene.Net.Search;

namespace Lucene.Net.QueryParsers.Flexible.Standard.Builders
{
    /// <summary>
    /// This builder does nothing. Commonly used for {@link QueryNode} objects that
    /// are built by its parent's builder.
    /// </summary>
    /// <seealso cref="IStandardQueryBuilder"/>
    /// <seealso cref="QueryTreeBuilder"/>
    public class DummyQueryNodeBuilder : IStandardQueryBuilder
    {
        /// <summary>
        /// Constructs a {@link DummyQueryNodeBuilder} object.
        /// </summary>
        public DummyQueryNodeBuilder()
        {
            // empty constructor
        }

        /// <summary>
        /// Always return <c>null</c>.
        /// </summary>
        /// <param name="queryNode"></param>
        /// <returns><c>null</c></returns>
        public virtual Query Build(IQueryNode queryNode)
        {
            return null;
        }
    }
}
