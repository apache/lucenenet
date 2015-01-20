/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Org.Apache.Lucene.Index;
using Org.Apache.Lucene.Search;
using Sharpen;

namespace Org.Apache.Lucene.Queries
{
	/// <summary>
	/// An instance of this subclass should be returned by
	/// <see cref="CustomScoreQuery.GetCustomScoreProvider(Org.Apache.Lucene.Index.AtomicReaderContext)
	/// 	">CustomScoreQuery.GetCustomScoreProvider(Org.Apache.Lucene.Index.AtomicReaderContext)
	/// 	</see>
	/// , if you want
	/// to modify the custom score calculation of a
	/// <see cref="CustomScoreQuery">CustomScoreQuery</see>
	/// .
	/// <p>Since Lucene 2.9, queries operate on each segment of an index separately,
	/// so the protected
	/// <see cref="context">context</see>
	/// field can be used to resolve doc IDs,
	/// as the supplied <code>doc</code> ID is per-segment and without knowledge
	/// of the IndexReader you cannot access the document or
	/// <see cref="Org.Apache.Lucene.Search.FieldCache">Org.Apache.Lucene.Search.FieldCache
	/// 	</see>
	/// .
	/// </summary>
	/// <lucene.experimental></lucene.experimental>
	/// <since>2.9.2</since>
	public class CustomScoreProvider
	{
		protected internal readonly AtomicReaderContext context;

		/// <summary>
		/// Creates a new instance of the provider class for the given
		/// <see cref="Org.Apache.Lucene.Index.IndexReader">Org.Apache.Lucene.Index.IndexReader
		/// 	</see>
		/// .
		/// </summary>
		public CustomScoreProvider(AtomicReaderContext context)
		{
			// for javadocs
			// for javadocs
			this.context = context;
		}

		/// <summary>
		/// Compute a custom score by the subQuery score and a number of
		/// <see cref="Org.Apache.Lucene.Queries.Function.FunctionQuery">Org.Apache.Lucene.Queries.Function.FunctionQuery
		/// 	</see>
		/// scores.
		/// <p>
		/// Subclasses can override this method to modify the custom score.
		/// <p>
		/// If your custom scoring is different than the default herein you
		/// should override at least one of the two customScore() methods.
		/// If the number of
		/// <see cref="Org.Apache.Lucene.Queries.Function.FunctionQuery">function queries</see>
		/// is always &lt; 2 it is
		/// sufficient to override the other
		/// <see cref="CustomScore(int, float, float)">customScore()</see>
		/// 
		/// method, which is simpler.
		/// <p>
		/// The default computation herein is a multiplication of given scores:
		/// <pre>
		/// ModifiedScore = valSrcScore * valSrcScores[0] * valSrcScores[1] * ...
		/// </pre>
		/// </summary>
		/// <param name="doc">id of scored doc.</param>
		/// <param name="subQueryScore">score of that doc by the subQuery.</param>
		/// <param name="valSrcScores">
		/// scores of that doc by the
		/// <see cref="Org.Apache.Lucene.Queries.Function.FunctionQuery">Org.Apache.Lucene.Queries.Function.FunctionQuery
		/// 	</see>
		/// .
		/// </param>
		/// <returns>custom score.</returns>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual float CustomScore(int doc, float subQueryScore, float[] valSrcScores
			)
		{
			if (valSrcScores.Length == 1)
			{
				return CustomScore(doc, subQueryScore, valSrcScores[0]);
			}
			if (valSrcScores.Length == 0)
			{
				return CustomScore(doc, subQueryScore, 1);
			}
			float score = subQueryScore;
			foreach (float valSrcScore in valSrcScores)
			{
				score *= valSrcScore;
			}
			return score;
		}

		/// <summary>
		/// Compute a custom score by the subQuery score and the
		/// <see cref="Org.Apache.Lucene.Queries.Function.FunctionQuery">Org.Apache.Lucene.Queries.Function.FunctionQuery
		/// 	</see>
		/// score.
		/// <p>
		/// Subclasses can override this method to modify the custom score.
		/// <p>
		/// If your custom scoring is different than the default herein you
		/// should override at least one of the two customScore() methods.
		/// If the number of
		/// <see cref="Org.Apache.Lucene.Queries.Function.FunctionQuery">function queries</see>
		/// is always &lt; 2 it is
		/// sufficient to override this customScore() method, which is simpler.
		/// <p>
		/// The default computation herein is a multiplication of the two scores:
		/// <pre>
		/// ModifiedScore = subQueryScore * valSrcScore
		/// </pre>
		/// </summary>
		/// <param name="doc">id of scored doc.</param>
		/// <param name="subQueryScore">score of that doc by the subQuery.</param>
		/// <param name="valSrcScore">
		/// score of that doc by the
		/// <see cref="Org.Apache.Lucene.Queries.Function.FunctionQuery">Org.Apache.Lucene.Queries.Function.FunctionQuery
		/// 	</see>
		/// .
		/// </param>
		/// <returns>custom score.</returns>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual float CustomScore(int doc, float subQueryScore, float valSrcScore)
		{
			return subQueryScore * valSrcScore;
		}

		/// <summary>Explain the custom score.</summary>
		/// <remarks>
		/// Explain the custom score.
		/// Whenever overriding
		/// <see cref="CustomScore(int, float, float[])">CustomScore(int, float, float[])</see>
		/// ,
		/// this method should also be overridden to provide the correct explanation
		/// for the part of the custom scoring.
		/// </remarks>
		/// <param name="doc">doc being explained.</param>
		/// <param name="subQueryExpl">explanation for the sub-query part.</param>
		/// <param name="valSrcExpls">explanation for the value source part.</param>
		/// <returns>an explanation for the custom score</returns>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual Explanation CustomExplain(int doc, Explanation subQueryExpl, Explanation
			[] valSrcExpls)
		{
			if (valSrcExpls.Length == 1)
			{
				return CustomExplain(doc, subQueryExpl, valSrcExpls[0]);
			}
			if (valSrcExpls.Length == 0)
			{
				return subQueryExpl;
			}
			float valSrcScore = 1;
			foreach (Explanation valSrcExpl in valSrcExpls)
			{
				valSrcScore *= valSrcExpl.GetValue();
			}
			Explanation exp = new Explanation(valSrcScore * subQueryExpl.GetValue(), "custom score: product of:"
				);
			exp.AddDetail(subQueryExpl);
			foreach (Explanation valSrcExpl_1 in valSrcExpls)
			{
				exp.AddDetail(valSrcExpl_1);
			}
			return exp;
		}

		/// <summary>Explain the custom score.</summary>
		/// <remarks>
		/// Explain the custom score.
		/// Whenever overriding
		/// <see cref="CustomScore(int, float, float)">CustomScore(int, float, float)</see>
		/// ,
		/// this method should also be overridden to provide the correct explanation
		/// for the part of the custom scoring.
		/// </remarks>
		/// <param name="doc">doc being explained.</param>
		/// <param name="subQueryExpl">explanation for the sub-query part.</param>
		/// <param name="valSrcExpl">explanation for the value source part.</param>
		/// <returns>an explanation for the custom score</returns>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual Explanation CustomExplain(int doc, Explanation subQueryExpl, Explanation
			 valSrcExpl)
		{
			float valSrcScore = 1;
			if (valSrcExpl != null)
			{
				valSrcScore *= valSrcExpl.GetValue();
			}
			Explanation exp = new Explanation(valSrcScore * subQueryExpl.GetValue(), "custom score: product of:"
				);
			exp.AddDetail(subQueryExpl);
			exp.AddDetail(valSrcExpl);
			return exp;
		}
	}
}
