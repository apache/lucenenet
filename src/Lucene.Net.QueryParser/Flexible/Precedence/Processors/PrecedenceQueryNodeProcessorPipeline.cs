using Lucene.Net.QueryParsers.Flexible.Core.Config;
using Lucene.Net.QueryParsers.Flexible.Standard.Processors;

namespace Lucene.Net.QueryParsers.Flexible.Precedence.Processors
{
    /// <summary>
    /// This processor pipeline extends {@link StandardQueryNodeProcessorPipeline} and enables
    /// boolean precedence on it.
    /// <para>
    /// EXPERT: the precedence is enabled by removing {@link GroupQueryNodeProcessor} from the
    /// {@link StandardQueryNodeProcessorPipeline} and appending {@link BooleanModifiersQueryNodeProcessor}
    /// to the pipeline.
    /// </para>
    /// </summary>
    /// <seealso cref="PrecedenceQueryParser"/>
    /// <seealso cref="StandardQueryNodeProcessorPipeline"/>
    public class PrecedenceQueryNodeProcessorPipeline : StandardQueryNodeProcessorPipeline
    {
        /**
   * @see StandardQueryNodeProcessorPipeline#StandardQueryNodeProcessorPipeline(QueryConfigHandler)
   */
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
