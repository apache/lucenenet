/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Org.Apache.Lucene.Queryparser.Flexible.Core.Config;
using Org.Apache.Lucene.Queryparser.Flexible.Precedence.Processors;
using Org.Apache.Lucene.Queryparser.Flexible.Standard.Processors;
using Sharpen;

namespace Org.Apache.Lucene.Queryparser.Flexible.Precedence.Processors
{
	/// <summary>
	/// <p>
	/// This processor pipeline extends
	/// <see cref="Org.Apache.Lucene.Queryparser.Flexible.Standard.Processors.StandardQueryNodeProcessorPipeline
	/// 	">Org.Apache.Lucene.Queryparser.Flexible.Standard.Processors.StandardQueryNodeProcessorPipeline
	/// 	</see>
	/// and enables
	/// boolean precedence on it.
	/// </p>
	/// <p>
	/// EXPERT: the precedence is enabled by removing
	/// <see cref="Org.Apache.Lucene.Queryparser.Flexible.Standard.Processors.GroupQueryNodeProcessor
	/// 	">Org.Apache.Lucene.Queryparser.Flexible.Standard.Processors.GroupQueryNodeProcessor
	/// 	</see>
	/// from the
	/// <see cref="Org.Apache.Lucene.Queryparser.Flexible.Standard.Processors.StandardQueryNodeProcessorPipeline
	/// 	">Org.Apache.Lucene.Queryparser.Flexible.Standard.Processors.StandardQueryNodeProcessorPipeline
	/// 	</see>
	/// and appending
	/// <see cref="BooleanModifiersQueryNodeProcessor">BooleanModifiersQueryNodeProcessor
	/// 	</see>
	/// to the pipeline.
	/// </p>
	/// </summary>
	/// <seealso cref="Org.Apache.Lucene.Queryparser.Flexible.Precedence.PrecedenceQueryParser
	/// 	">Org.Apache.Lucene.Queryparser.Flexible.Precedence.PrecedenceQueryParser</seealso>
	/// <seealso cref="Org.Apache.Lucene.Queryparser.Flexible.Standard.Processors.StandardQueryNodeProcessorPipeline
	/// 	">Org.Apache.Lucene.Queryparser.Flexible.Standard.Processors.StandardQueryNodeProcessorPipeline
	/// 	</seealso>
	public class PrecedenceQueryNodeProcessorPipeline : StandardQueryNodeProcessorPipeline
	{
		/// <seealso cref="Org.Apache.Lucene.Queryparser.Flexible.Standard.Processors.StandardQueryNodeProcessorPipeline.StandardQueryNodeProcessorPipeline(Org.Apache.Lucene.Queryparser.Flexible.Core.Config.QueryConfigHandler)
		/// 	">Org.Apache.Lucene.Queryparser.Flexible.Standard.Processors.StandardQueryNodeProcessorPipeline.StandardQueryNodeProcessorPipeline(Org.Apache.Lucene.Queryparser.Flexible.Core.Config.QueryConfigHandler)
		/// 	</seealso>
		public PrecedenceQueryNodeProcessorPipeline(QueryConfigHandler queryConfig) : base
			(queryConfig)
		{
			for (int i = 0; i < Count; i++)
			{
				if (this[i].GetType().Equals(typeof(BooleanQuery2ModifierNodeProcessor)))
				{
					Remove(i--);
				}
			}
			AddItem(new BooleanModifiersQueryNodeProcessor());
		}
	}
}
