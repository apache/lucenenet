/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Collections.Generic;
using Org.Apache.Lucene.Analysis;
using Org.Apache.Lucene.Analysis.Tokenattributes;
using Org.Apache.Lucene.Search.Highlight;
using Sharpen;

namespace Org.Apache.Lucene.Search.Highlight
{
	/// <summary>
	/// <see cref="Fragmenter">Fragmenter</see>
	/// implementation which breaks text up into same-size
	/// fragments but does not split up
	/// <see cref="Org.Apache.Lucene.Search.Spans.Spans">Org.Apache.Lucene.Search.Spans.Spans
	/// 	</see>
	/// . This is a simple sample class.
	/// </summary>
	public class SimpleSpanFragmenter : Fragmenter
	{
		private const int DEFAULT_FRAGMENT_SIZE = 100;

		private int fragmentSize;

		private int currentNumFrags;

		private int position = -1;

		private QueryScorer queryScorer;

		private int waitForPos = -1;

		private int textSize;

		private CharTermAttribute termAtt;

		private PositionIncrementAttribute posIncAtt;

		private OffsetAttribute offsetAtt;

		/// <param name="queryScorer">QueryScorer that was used to score hits</param>
		public SimpleSpanFragmenter(QueryScorer queryScorer) : this(queryScorer, DEFAULT_FRAGMENT_SIZE
			)
		{
		}

		/// <param name="queryScorer">QueryScorer that was used to score hits</param>
		/// <param name="fragmentSize">size in bytes of each fragment</param>
		public SimpleSpanFragmenter(QueryScorer queryScorer, int fragmentSize)
		{
			this.fragmentSize = fragmentSize;
			this.queryScorer = queryScorer;
		}

		public virtual bool IsNewFragment()
		{
			position += posIncAtt.GetPositionIncrement();
			if (waitForPos == position)
			{
				waitForPos = -1;
			}
			else
			{
				if (waitForPos != -1)
				{
					return false;
				}
			}
			WeightedSpanTerm wSpanTerm = queryScorer.GetWeightedSpanTerm(termAtt.ToString());
			if (wSpanTerm != null)
			{
				IList<PositionSpan> positionSpans = wSpanTerm.GetPositionSpans();
				for (int i = 0; i < positionSpans.Count; i++)
				{
					if (positionSpans[i].start == position)
					{
						waitForPos = positionSpans[i].end + 1;
						break;
					}
				}
			}
			bool isNewFrag = offsetAtt.EndOffset() >= (fragmentSize * currentNumFrags) && (textSize
				 - offsetAtt.EndOffset()) >= ((int)(((uint)fragmentSize) >> 1));
			if (isNewFrag)
			{
				currentNumFrags++;
			}
			return isNewFrag;
		}

		public virtual void Start(string originalText, TokenStream tokenStream)
		{
			position = -1;
			currentNumFrags = 1;
			textSize = originalText.Length;
			termAtt = tokenStream.AddAttribute<CharTermAttribute>();
			posIncAtt = tokenStream.AddAttribute<PositionIncrementAttribute>();
			offsetAtt = tokenStream.AddAttribute<OffsetAttribute>();
		}
	}
}
