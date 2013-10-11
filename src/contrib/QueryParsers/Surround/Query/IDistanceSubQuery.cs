using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.QueryParsers.Surround.Query
{
    public interface IDistanceSubQuery
    {
        string DistanceSubQueryNotAllowed { get; }

        void AddSpanQueries(SpanNearClauseFactory sncf);
    }
}
