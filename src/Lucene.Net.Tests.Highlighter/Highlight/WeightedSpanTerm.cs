/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Collections.Generic;
using Org.Apache.Lucene.Search.Highlight;
using Sharpen;

namespace Org.Apache.Lucene.Search.Highlight
{
	/// <summary>
	/// Lightweight class to hold term, weight, and positions used for scoring this
	/// term.
	/// </summary>
	/// <remarks>
	/// Lightweight class to hold term, weight, and positions used for scoring this
	/// term.
	/// </remarks>
	public class WeightedSpanTerm : WeightedTerm
	{
		internal bool positionSensitive;

		private IList<PositionSpan> positionSpans = new AList<PositionSpan>();

		public WeightedSpanTerm(float weight, string term) : base(weight, term)
		{
			this.positionSpans = new AList<PositionSpan>();
		}

		public WeightedSpanTerm(float weight, string term, bool positionSensitive) : base
			(weight, term)
		{
			this.positionSensitive = positionSensitive;
		}

		/// <summary>Checks to see if this term is valid at <code>position</code>.</summary>
		/// <remarks>Checks to see if this term is valid at <code>position</code>.</remarks>
		/// <param name="position">to check against valid term positions</param>
		/// <returns>true iff this term is a hit at this position</returns>
		public virtual bool CheckPosition(int position)
		{
			// There would probably be a slight speed improvement if PositionSpans
			// where kept in some sort of priority queue - that way this method
			// could
			// bail early without checking each PositionSpan.
			Iterator<PositionSpan> positionSpanIt = positionSpans.Iterator();
			while (positionSpanIt.HasNext())
			{
				PositionSpan posSpan = positionSpanIt.Next();
				if (((position >= posSpan.start) && (position <= posSpan.end)))
				{
					return true;
				}
			}
			return false;
		}

		public virtual void AddPositionSpans(IList<PositionSpan> positionSpans)
		{
			Sharpen.Collections.AddAll(this.positionSpans, positionSpans);
		}

		public virtual bool IsPositionSensitive()
		{
			return positionSensitive;
		}

		public virtual void SetPositionSensitive(bool positionSensitive)
		{
			this.positionSensitive = positionSensitive;
		}

		public virtual IList<PositionSpan> GetPositionSpans()
		{
			return positionSpans;
		}
	}
}
