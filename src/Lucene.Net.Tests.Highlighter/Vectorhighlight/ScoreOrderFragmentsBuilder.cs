/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Collections.Generic;
using Org.Apache.Lucene.Search.Vectorhighlight;
using Sharpen;

namespace Org.Apache.Lucene.Search.Vectorhighlight
{
	/// <summary>An implementation of FragmentsBuilder that outputs score-order fragments.
	/// 	</summary>
	/// <remarks>An implementation of FragmentsBuilder that outputs score-order fragments.
	/// 	</remarks>
	public class ScoreOrderFragmentsBuilder : BaseFragmentsBuilder
	{
		/// <summary>a constructor.</summary>
		/// <remarks>a constructor.</remarks>
		public ScoreOrderFragmentsBuilder() : base()
		{
		}

		/// <summary>a constructor.</summary>
		/// <remarks>a constructor.</remarks>
		/// <param name="preTags">array of pre-tags for markup terms.</param>
		/// <param name="postTags">array of post-tags for markup terms.</param>
		protected internal ScoreOrderFragmentsBuilder(string[] preTags, string[] postTags
			) : base(preTags, postTags)
		{
		}

		protected internal ScoreOrderFragmentsBuilder(BoundaryScanner bs) : base(bs)
		{
		}

		protected internal ScoreOrderFragmentsBuilder(string[] preTags, string[] postTags
			, BoundaryScanner bs) : base(preTags, postTags, bs)
		{
		}

		/// <summary>Sort by score the list of WeightedFragInfo</summary>
		public override IList<FieldFragList.WeightedFragInfo> GetWeightedFragInfoList(IList
			<FieldFragList.WeightedFragInfo> src)
		{
			src.Sort(new ScoreOrderFragmentsBuilder.ScoreComparator());
			return src;
		}

		/// <summary>
		/// Comparator for
		/// <see cref="WeightedFragInfo">WeightedFragInfo</see>
		/// by boost, breaking ties
		/// by offset.
		/// </summary>
		public class ScoreComparator : IComparer<FieldFragList.WeightedFragInfo>
		{
			public virtual int Compare(FieldFragList.WeightedFragInfo o1, FieldFragList.WeightedFragInfo
				 o2)
			{
				if (o1.GetTotalBoost() > o2.GetTotalBoost())
				{
					return -1;
				}
				else
				{
					if (o1.GetTotalBoost() < o2.GetTotalBoost())
					{
						return 1;
					}
					else
					{
						// if same score then check startOffset
						if (o1.GetStartOffset() < o2.GetStartOffset())
						{
							return -1;
						}
						else
						{
							if (o1.GetStartOffset() > o2.GetStartOffset())
							{
								return 1;
							}
						}
					}
				}
				return 0;
			}
		}
	}
}
