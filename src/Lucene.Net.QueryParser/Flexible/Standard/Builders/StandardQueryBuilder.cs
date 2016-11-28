using Lucene.Net.QueryParsers.Flexible.Core.Builders;
using Lucene.Net.QueryParsers.Flexible.Core.Nodes;
using Lucene.Net.Search;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.QueryParsers.Flexible.Standard.Builders
{
    /// <summary>
    /// This interface should be implemented by every class that wants to build
    /// {@link Query} objects from {@link QueryNode} objects. 
    /// </summary>
    /// <seealso cref="IQueryBuilder"/>
    /// <seealso cref="QueryTreeBuilder"/>
    public interface IStandardQueryBuilder : IQueryBuilder
    {
        new Query Build(IQueryNode queryNode);
    }
}
