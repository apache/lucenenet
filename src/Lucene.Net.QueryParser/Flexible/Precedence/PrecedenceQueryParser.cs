using Lucene.Net.Analysis;
using Lucene.Net.QueryParsers.Flexible.Precedence.Processors;
using Lucene.Net.QueryParsers.Flexible.Standard;

namespace Lucene.Net.QueryParsers.Flexible.Precedence
{
    /// <summary>
    /// This query parser works exactly as the standard query parser ( {@link StandardQueryParser} ), 
    /// except that it respect the boolean precedence, so &lt;a AND b OR c AND d&gt; is parsed to &lt;(+a +b) (+c +d)&gt;
    /// instead of &lt;+a +b +c +d&gt;.
    /// <para>
    /// EXPERT: This class extends {@link StandardQueryParser}, but uses {@link PrecedenceQueryNodeProcessorPipeline}
    /// instead of {@link StandardQueryNodeProcessorPipeline} to process the query tree.
    /// </para>
    /// </summary>
    /// <seealso cref="StandardQueryParser"/>
    public class PrecedenceQueryParser : StandardQueryParser
    {
        /**
   * @see StandardQueryParser#StandardQueryParser()
   */
        public PrecedenceQueryParser()
        {
            SetQueryNodeProcessor(new PrecedenceQueryNodeProcessorPipeline(QueryConfigHandler));
        }

        /**
         * @see StandardQueryParser#StandardQueryParser(Analyzer)
         */
        public PrecedenceQueryParser(Analyzer analyer)
            : base(analyer)
        {
            SetQueryNodeProcessor(new PrecedenceQueryNodeProcessorPipeline(QueryConfigHandler));
        }
    }
}
