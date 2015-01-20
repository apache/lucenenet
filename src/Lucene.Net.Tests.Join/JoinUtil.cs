/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Globalization;
using Org.Apache.Lucene.Search;
using Org.Apache.Lucene.Search.Join;
using Sharpen;

namespace Org.Apache.Lucene.Search.Join
{
	/// <summary>Utility for query time joining using TermsQuery and TermsCollector.</summary>
	/// <remarks>Utility for query time joining using TermsQuery and TermsCollector.</remarks>
	/// <lucene.experimental></lucene.experimental>
	public sealed class JoinUtil
	{
		public JoinUtil()
		{
		}

		// No instances allowed
		/// <summary>Method for query time joining.</summary>
		/// <remarks>
		/// Method for query time joining.
		/// <p/>
		/// Execute the returned query with a
		/// <see cref="Org.Apache.Lucene.Search.IndexSearcher">Org.Apache.Lucene.Search.IndexSearcher
		/// 	</see>
		/// to retrieve all documents that have the same terms in the
		/// to field that match with documents matching the specified fromQuery and have the same terms in the from field.
		/// <p/>
		/// In the case a single document relates to more than one document the <code>multipleValuesPerDocument</code> option
		/// should be set to true. When the <code>multipleValuesPerDocument</code> is set to <code>true</code> only the
		/// the score from the first encountered join value originating from the 'from' side is mapped into the 'to' side.
		/// Even in the case when a second join value related to a specific document yields a higher score. Obviously this
		/// doesn't apply in the case that
		/// <see cref="ScoreMode.None">ScoreMode.None</see>
		/// is used, since no scores are computed at all.
		/// </p>
		/// Memory considerations: During joining all unique join values are kept in memory. On top of that when the scoreMode
		/// isn't set to
		/// <see cref="ScoreMode.None">ScoreMode.None</see>
		/// a float value per unique join value is kept in memory for computing scores.
		/// When scoreMode is set to
		/// <see cref="ScoreMode.Avg">ScoreMode.Avg</see>
		/// also an additional integer value is kept in memory per unique
		/// join value.
		/// </remarks>
		/// <param name="fromField">The from field to join from</param>
		/// <param name="multipleValuesPerDocument">Whether the from field has multiple terms per document
		/// 	</param>
		/// <param name="toField">The to field to join to</param>
		/// <param name="fromQuery">The query to match documents on the from side</param>
		/// <param name="fromSearcher">The searcher that executed the specified fromQuery</param>
		/// <param name="scoreMode">Instructs how scores from the fromQuery are mapped to the returned query
		/// 	</param>
		/// <returns>
		/// a
		/// <see cref="Org.Apache.Lucene.Search.Query">Org.Apache.Lucene.Search.Query</see>
		/// instance that can be used to join documents based on the
		/// terms in the from and to field
		/// </returns>
		/// <exception cref="System.IO.IOException">If I/O related errors occur</exception>
		public static Query CreateJoinQuery(string fromField, bool multipleValuesPerDocument
			, string toField, Query fromQuery, IndexSearcher fromSearcher, ScoreMode scoreMode
			)
		{
			switch (scoreMode)
			{
				case ScoreMode.None:
				{
					TermsCollector termsCollector = TermsCollector.Create(fromField, multipleValuesPerDocument
						);
					fromSearcher.Search(fromQuery, termsCollector);
					return new TermsQuery(toField, fromQuery, termsCollector.GetCollectorTerms());
				}

				case ScoreMode.Total:
				case ScoreMode.Max:
				case ScoreMode.Avg:
				{
					TermsWithScoreCollector termsWithScoreCollector = TermsWithScoreCollector.Create(
						fromField, multipleValuesPerDocument, scoreMode);
					fromSearcher.Search(fromQuery, termsWithScoreCollector);
					return new TermsIncludingScoreQuery(toField, multipleValuesPerDocument, termsWithScoreCollector
						.GetCollectedTerms(), termsWithScoreCollector.GetScoresPerTerm(), fromQuery);
				}

				default:
				{
					throw new ArgumentException(string.Format(CultureInfo.ROOT, "Score mode %s isn't supported."
						, scoreMode));
				}
			}
		}
	}
}
