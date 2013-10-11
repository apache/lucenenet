using Lucene.Net.Analysis;
using Lucene.Net.QueryParsers.Flexible.Precedence.Processors;
using Lucene.Net.QueryParsers.Flexible.Standard;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.QueryParsers.Flexible.Precedence
{
    public class PrecedenceQueryParser : StandardQueryParser
    {
        public PrecedenceQueryParser()
        {
            QueryNodeProcessor = new PrecedenceQueryNodeProcessorPipeline(QueryConfigHandler);
        }

        public PrecedenceQueryParser(Analyzer analyer)
            : base(analyer)
        {
            QueryNodeProcessor = new PrecedenceQueryNodeProcessorPipeline(QueryConfigHandler);
        }
    }
}
