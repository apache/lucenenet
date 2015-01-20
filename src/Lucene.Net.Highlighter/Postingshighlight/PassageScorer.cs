/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using Sharpen;

namespace Lucene.Net.Search.Postingshighlight
{
	/// <summary>
	/// Ranks passages found by
	/// <see cref="PostingsHighlighter">PostingsHighlighter</see>
	/// .
	/// <p>
	/// Each passage is scored as a miniature document within the document.
	/// The final score is computed as
	/// <see cref="Norm(int)">Norm(int)</see>
	/// * &sum; (
	/// <see cref="Weight(int, int)">Weight(int, int)</see>
	/// *
	/// <see cref="Tf(int, int)">Tf(int, int)</see>
	/// ).
	/// The default implementation is
	/// <see cref="Norm(int)">Norm(int)</see>
	/// * BM25.
	/// </summary>
	/// <lucene.experimental></lucene.experimental>
	public class PassageScorer
	{
		/// <summary>BM25 k1 parameter, controls term frequency normalization</summary>
		internal readonly float k1;

		/// <summary>BM25 b parameter, controls length normalization.</summary>
		/// <remarks>BM25 b parameter, controls length normalization.</remarks>
		internal readonly float b;

		/// <summary>A pivot used for length normalization.</summary>
		/// <remarks>A pivot used for length normalization.</remarks>
		internal readonly float pivot;

		/// <summary>
		/// Creates PassageScorer with these default values:
		/// <ul>
		/// <li>
		/// <code>k1 = 1.2</code>
		/// ,
		/// <li>
		/// <code>b = 0.75</code>
		/// .
		/// <li>
		/// <code>pivot = 87</code>
		/// </ul>
		/// </summary>
		public PassageScorer() : this(1.2f, 0.75f, 87f)
		{
		}

		/// <summary>Creates PassageScorer with specified scoring parameters</summary>
		/// <param name="k1">Controls non-linear term frequency normalization (saturation).</param>
		/// <param name="b">Controls to what degree passage length normalizes tf values.</param>
		/// <param name="pivot">Pivot value for length normalization (some rough idea of average sentence length in characters).
		/// 	</param>
		public PassageScorer(float k1, float b, float pivot)
		{
			// TODO: this formula is completely made up. It might not provide relevant snippets!
			// 1.2 and 0.75 are well-known bm25 defaults (but maybe not the best here) ?
			// 87 is typical average english sentence length.
			this.k1 = k1;
			this.b = b;
			this.pivot = pivot;
		}

		/// <summary>Computes term importance, given its in-document statistics.</summary>
		/// <remarks>Computes term importance, given its in-document statistics.</remarks>
		/// <param name="contentLength">length of document in characters</param>
		/// <param name="totalTermFreq">number of time term occurs in document</param>
		/// <returns>term importance</returns>
		public virtual float Weight(int contentLength, int totalTermFreq)
		{
			// approximate #docs from content length
			float numDocs = 1 + contentLength / pivot;
			// numDocs not numDocs - docFreq (ala DFR), since we approximate numDocs
			return (k1 + 1) * (float)Math.Log(1 + (numDocs + 0.5D) / (totalTermFreq + 0.5D));
		}

		/// <summary>
		/// Computes term weight, given the frequency within the passage
		/// and the passage's length.
		/// </summary>
		/// <remarks>
		/// Computes term weight, given the frequency within the passage
		/// and the passage's length.
		/// </remarks>
		/// <param name="freq">number of occurrences of within this passage</param>
		/// <param name="passageLen">length of the passage in characters.</param>
		/// <returns>term weight</returns>
		public virtual float Tf(int freq, int passageLen)
		{
			float norm = k1 * ((1 - b) + b * (passageLen / pivot));
			return freq / (freq + norm);
		}

		/// <summary>Normalize a passage according to its position in the document.</summary>
		/// <remarks>
		/// Normalize a passage according to its position in the document.
		/// <p>
		/// Typically passages towards the beginning of the document are
		/// more useful for summarizing the contents.
		/// <p>
		/// The default implementation is <code>1 + 1/log(pivot + passageStart)</code>
		/// </remarks>
		/// <param name="passageStart">start offset of the passage</param>
		/// <returns>a boost value multiplied into the passage's core.</returns>
		public virtual float Norm(int passageStart)
		{
			return 1 + 1 / (float)Math.Log(pivot + passageStart);
		}
	}
}
