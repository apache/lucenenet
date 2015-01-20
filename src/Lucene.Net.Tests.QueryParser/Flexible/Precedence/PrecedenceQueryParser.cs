/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Org.Apache.Lucene.Analysis;
using Org.Apache.Lucene.Queryparser.Flexible.Precedence.Processors;
using Org.Apache.Lucene.Queryparser.Flexible.Standard;
using Sharpen;

namespace Org.Apache.Lucene.Queryparser.Flexible.Precedence
{
	/// <summary>
	/// <p>
	/// This query parser works exactly as the standard query parser (
	/// <see cref="Org.Apache.Lucene.Queryparser.Flexible.Standard.StandardQueryParser">Org.Apache.Lucene.Queryparser.Flexible.Standard.StandardQueryParser
	/// 	</see>
	/// ),
	/// except that it respect the boolean precedence, so &lt;a AND b OR c AND d&gt; is parsed to &lt;(+a +b) (+c +d)&gt;
	/// instead of &lt;+a +b +c +d&gt;.
	/// </p>
	/// <p>
	/// EXPERT: This class extends
	/// <see cref="Org.Apache.Lucene.Queryparser.Flexible.Standard.StandardQueryParser">Org.Apache.Lucene.Queryparser.Flexible.Standard.StandardQueryParser
	/// 	</see>
	/// , but uses
	/// <see cref="Org.Apache.Lucene.Queryparser.Flexible.Precedence.Processors.PrecedenceQueryNodeProcessorPipeline
	/// 	">Org.Apache.Lucene.Queryparser.Flexible.Precedence.Processors.PrecedenceQueryNodeProcessorPipeline
	/// 	</see>
	/// instead of
	/// <see cref="Org.Apache.Lucene.Queryparser.Flexible.Standard.Processors.StandardQueryNodeProcessorPipeline
	/// 	">Org.Apache.Lucene.Queryparser.Flexible.Standard.Processors.StandardQueryNodeProcessorPipeline
	/// 	</see>
	/// to process the query tree.
	/// </p>
	/// </summary>
	/// <seealso cref="Org.Apache.Lucene.Queryparser.Flexible.Standard.StandardQueryParser
	/// 	">Org.Apache.Lucene.Queryparser.Flexible.Standard.StandardQueryParser</seealso>
	public class PrecedenceQueryParser : StandardQueryParser
	{
		/// <seealso cref="Org.Apache.Lucene.Queryparser.Flexible.Standard.StandardQueryParser.StandardQueryParser()
		/// 	">Org.Apache.Lucene.Queryparser.Flexible.Standard.StandardQueryParser.StandardQueryParser()
		/// 	</seealso>
		public PrecedenceQueryParser()
		{
			SetQueryNodeProcessor(new PrecedenceQueryNodeProcessorPipeline(GetQueryConfigHandler
				()));
		}

		/// <seealso cref="Org.Apache.Lucene.Queryparser.Flexible.Standard.StandardQueryParser.StandardQueryParser(Org.Apache.Lucene.Analysis.Analyzer)
		/// 	">Org.Apache.Lucene.Queryparser.Flexible.Standard.StandardQueryParser.StandardQueryParser(Org.Apache.Lucene.Analysis.Analyzer)
		/// 	</seealso>
		public PrecedenceQueryParser(Analyzer analyer) : base(analyer)
		{
			SetQueryNodeProcessor(new PrecedenceQueryNodeProcessorPipeline(GetQueryConfigHandler
				()));
		}
	}
}
