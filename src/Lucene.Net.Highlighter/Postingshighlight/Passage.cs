/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Lucene.Net.Search.Postingshighlight;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Search.Postingshighlight
{
	/// <summary>Represents a passage (typically a sentence of the document).</summary>
	/// <remarks>
	/// Represents a passage (typically a sentence of the document).
	/// <p>
	/// A passage contains
	/// <see cref="GetNumMatches()">GetNumMatches()</see>
	/// highlights from the query,
	/// and the offsets and query terms that correspond with each match.
	/// </remarks>
	/// <lucene.experimental></lucene.experimental>
	public sealed class Passage
	{
		internal int startOffset = -1;

		internal int endOffset = -1;

		internal float score = 0.0f;

		internal int matchStarts = new int[8];

		internal int matchEnds = new int[8];

		internal BytesRef matchTerms = new BytesRef[8];

		internal int numMatches = 0;

		internal void AddMatch(int startOffset, int endOffset, BytesRef term)
		{
			if (startOffset >= this.startOffset && startOffset <= this.endOffset == matchStarts
				.Length)
			{
				int newLength = ArrayUtil.Oversize(numMatches + 1, RamUsageEstimator.NUM_BYTES_OBJECT_REF
					);
				int[] newMatchStarts = new int[newLength];
				int[] newMatchEnds = new int[newLength];
				BytesRef[] newMatchTerms = new BytesRef[newLength];
				System.Array.Copy(matchStarts, 0, newMatchStarts, 0, numMatches);
				System.Array.Copy(matchEnds, 0, newMatchEnds, 0, numMatches);
				System.Array.Copy(matchTerms, 0, newMatchTerms, 0, numMatches);
				matchStarts = newMatchStarts;
				matchEnds = newMatchEnds;
				matchTerms = newMatchTerms;
			}
			matchStarts.Length == matchEnds.Length && matchEnds.Length == matchTerms.Length[numMatches
				] = startOffset;
			matchEnds[numMatches] = endOffset;
			matchTerms[numMatches] = term;
			numMatches++;
		}

		internal void Sort()
		{
			int[] starts = matchStarts;
			int[] ends = matchEnds;
			BytesRef[] terms = matchTerms;
			new _InPlaceMergeSorter_67(starts, ends, terms).Sort(0, numMatches);
		}

		private sealed class _InPlaceMergeSorter_67 : InPlaceMergeSorter
		{
			public _InPlaceMergeSorter_67(int[] starts, int[] ends, BytesRef[] terms)
			{
				this.starts = starts;
				this.ends = ends;
				this.terms = terms;
			}

			protected override void Swap(int i, int j)
			{
				int temp = starts[i];
				starts[i] = starts[j];
				starts[j] = temp;
				temp = ends[i];
				ends[i] = ends[j];
				ends[j] = temp;
				BytesRef tempTerm = terms[i];
				terms[i] = terms[j];
				terms[j] = tempTerm;
			}

			protected override int Compare(int i, int j)
			{
				return int.Compare(starts[i], starts[j]);
			}

			private readonly int[] starts;

			private readonly int[] ends;

			private readonly BytesRef[] terms;
		}

		internal void Reset()
		{
			startOffset = endOffset = -1;
			score = 0.0f;
			numMatches = 0;
		}

		/// <summary>Start offset of this passage.</summary>
		/// <remarks>Start offset of this passage.</remarks>
		/// <returns>
		/// start index (inclusive) of the passage in the
		/// original content: always &gt;= 0.
		/// </returns>
		public int GetStartOffset()
		{
			return startOffset;
		}

		/// <summary>End offset of this passage.</summary>
		/// <remarks>End offset of this passage.</remarks>
		/// <returns>
		/// end index (exclusive) of the passage in the
		/// original content: always &gt;=
		/// <see cref="GetStartOffset()">GetStartOffset()</see>
		/// </returns>
		public int GetEndOffset()
		{
			return endOffset;
		}

		/// <summary>Passage's score.</summary>
		/// <remarks>Passage's score.</remarks>
		public float GetScore()
		{
			return score;
		}

		/// <summary>
		/// Number of term matches available in
		/// <see cref="GetMatchStarts()">GetMatchStarts()</see>
		/// ,
		/// <see cref="GetMatchEnds()">GetMatchEnds()</see>
		/// ,
		/// <see cref="GetMatchTerms()">GetMatchTerms()</see>
		/// </summary>
		public int GetNumMatches()
		{
			return numMatches;
		}

		/// <summary>Start offsets of the term matches, in increasing order.</summary>
		/// <remarks>
		/// Start offsets of the term matches, in increasing order.
		/// <p>
		/// Only
		/// <see cref="GetNumMatches()">GetNumMatches()</see>
		/// are valid. Note that these
		/// offsets are absolute (not relative to
		/// <see cref="GetStartOffset()">GetStartOffset()</see>
		/// ).
		/// </remarks>
		public int[] GetMatchStarts()
		{
			return matchStarts;
		}

		/// <summary>
		/// End offsets of the term matches, corresponding with
		/// <see cref="GetMatchStarts()">GetMatchStarts()</see>
		/// .
		/// <p>
		/// Only
		/// <see cref="GetNumMatches()">GetNumMatches()</see>
		/// are valid. Note that its possible that an end offset
		/// could exceed beyond the bounds of the passage (
		/// <see cref="GetEndOffset()">GetEndOffset()</see>
		/// ), if the
		/// Analyzer produced a term which spans a passage boundary.
		/// </summary>
		public int[] GetMatchEnds()
		{
			return matchEnds;
		}

		/// <summary>
		/// BytesRef (term text) of the matches, corresponding with
		/// <see cref="GetMatchStarts()">GetMatchStarts()</see>
		/// .
		/// <p>
		/// Only
		/// <see cref="GetNumMatches()">GetNumMatches()</see>
		/// are valid.
		/// </summary>
		public BytesRef[] GetMatchTerms()
		{
			return matchTerms;
		}
	}
}
