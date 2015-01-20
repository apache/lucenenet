/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Lucene.Net.Analysis;
using Lucene.Net.Queryparser.Flexible.Precedence.Processors;
using Lucene.Net.Queryparser.Flexible.Standard;
using Sharpen;

namespace Lucene.Net.Queryparser.Flexible.Precedence
{
	/// <summary>
	/// <p>
	/// This query parser works exactly as the standard query parser (
	/// <see cref="Lucene.Net.Queryparser.Flexible.Standard.StandardQueryParser">Lucene.Net.Queryparser.Flexible.Standard.StandardQueryParser
	/// 	</see>
	/// ),
	/// except that it respect the boolean precedence, so &lt;a AND b OR c AND d&gt; is parsed to &lt;(+a +b) (+c +d)&gt;
	/// instead of &lt;+a +b +c +d&gt;.
	/// </p>
	/// <p>
	/// EXPERT: This class extends
	/// <see cref="Lucene.Net.Queryparser.Flexible.Standard.StandardQueryParser">Lucene.Net.Queryparser.Flexible.Standard.StandardQueryParser
	/// 	</see>
	/// , but uses
	/// <see cref="Lucene.Net.Queryparser.Flexible.Precedence.Processors.PrecedenceQueryNodeProcessorPipeline
	/// 	">Lucene.Net.Queryparser.Flexible.Precedence.Processors.PrecedenceQueryNodeProcessorPipeline
	/// 	</see>
	/// instead of
	/// <see cref="Lucene.Net.Queryparser.Flexible.Standard.Processors.StandardQueryNodeProcessorPipeline
	/// 	">Lucene.Net.Queryparser.Flexible.Standard.Processors.StandardQueryNodeProcessorPipeline
	/// 	</see>
	/// to process the query tree.
	/// </p>
	/// </summary>
	/// <seealso cref="Lucene.Net.Queryparser.Flexible.Standard.StandardQueryParser
	/// 	">Lucene.Net.Queryparser.Flexible.Standard.StandardQueryParser</seealso>
	public class PrecedenceQueryParser : StandardQueryParser
	{
		/// <seealso cref="Lucene.Net.Queryparser.Flexible.Standard.StandardQueryParser.StandardQueryParser()
		/// 	">Lucene.Net.Queryparser.Flexible.Standard.StandardQueryParser.StandardQueryParser()
		/// 	</seealso>
		public PrecedenceQueryParser()
		{
			SetQueryNodeProcessor(new PrecedenceQueryNodeProcessorPipeline(GetQueryConfigHandler
				()));
		}

		/// <seealso cref="Lucene.Net.Queryparser.Flexible.Standard.StandardQueryParser.StandardQueryParser(Lucene.Net.Analysis.Analyzer)
		/// 	">Lucene.Net.Queryparser.Flexible.Standard.StandardQueryParser.StandardQueryParser(Lucene.Net.Analysis.Analyzer)
		/// 	</seealso>
		public PrecedenceQueryParser(Analyzer analyer) : base(analyer)
		{
			SetQueryNodeProcessor(new PrecedenceQueryNodeProcessorPipeline(GetQueryConfigHandler
				()));
		}
	}
}
