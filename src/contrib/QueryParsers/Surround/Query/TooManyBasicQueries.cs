using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Lucene.Net.QueryParsers.Surround.Query
{
    public class TooManyBasicQueries : IOException
    {
        public TooManyBasicQueries(int maxBasicQueries)
            : base("Exceeded maximum of " + maxBasicQueries + " basic queries.")
        {
        }
    }
}
