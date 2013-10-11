using Lucene.Net.QueryParsers.Flexible.Core.Config;
using Lucene.Net.QueryParsers.Flexible.Standard.Processors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.QueryParsers.Flexible.Precedence.Processors
{
    public class PrecedenceQueryNodeProcessorPipeline : StandardQueryNodeProcessorPipeline
    {
        public PrecedenceQueryNodeProcessorPipeline(QueryConfigHandler queryConfig)
            : base(queryConfig)
        {
            for (int i = 0; i < Count; i++)
            {
                if (this[i].GetType().Equals(typeof(BooleanQuery2ModifierNodeProcessor)))
                {
                    RemoveAt(i--);
                }
            }

            Add(new BooleanModifiersQueryNodeProcessor());
        }
    }
}
