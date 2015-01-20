/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Lucene.Net.Search;
using Sharpen;

namespace Lucene.Net.Search.Grouping
{
	/// <summary>Represents one group in the results.</summary>
	/// <remarks>Represents one group in the results.</remarks>
	/// <lucene.experimental></lucene.experimental>
	public class GroupDocs<GROUP_VALUE_TYPE>
	{
		/// <summary>
		/// The groupField value for all docs in this group; this
		/// may be null if hits did not have the groupField.
		/// </summary>
		/// <remarks>
		/// The groupField value for all docs in this group; this
		/// may be null if hits did not have the groupField.
		/// </remarks>
		public readonly GROUP_VALUE_TYPE groupValue;

		/// <summary>Max score in this group</summary>
		public readonly float maxScore;

		/// <summary>
		/// Overall aggregated score of this group (currently only
		/// set by join queries).
		/// </summary>
		/// <remarks>
		/// Overall aggregated score of this group (currently only
		/// set by join queries).
		/// </remarks>
		public readonly float score;

		/// <summary>
		/// Hits; this may be
		/// <see cref="Lucene.Net.Search.FieldDoc">Lucene.Net.Search.FieldDoc</see>
		/// instances if the
		/// withinGroupSort sorted by fields.
		/// </summary>
		public readonly ScoreDoc[] scoreDocs;

		/// <summary>Total hits within this group</summary>
		public readonly int totalHits;

		/// <summary>
		/// Matches the groupSort passed to
		/// <see cref="AbstractFirstPassGroupingCollector{GROUP_VALUE_TYPE}">AbstractFirstPassGroupingCollector&lt;GROUP_VALUE_TYPE&gt;
		/// 	</see>
		/// .
		/// </summary>
		public readonly object[] groupSortValues;

		public GroupDocs(float score, float maxScore, int totalHits, ScoreDoc[] scoreDocs
			, GROUP_VALUE_TYPE groupValue, object[] groupSortValues)
		{
			this.score = score;
			this.maxScore = maxScore;
			this.totalHits = totalHits;
			this.scoreDocs = scoreDocs;
			this.groupValue = groupValue;
			this.groupSortValues = groupSortValues;
		}
	}
}
