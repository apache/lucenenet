/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Collections.Generic;
using Org.Apache.Lucene.Search.Vectorhighlight;
using Sharpen;

namespace Org.Apache.Lucene.Search.Vectorhighlight
{
	/// <summary>
	/// A weighted implementation of
	/// <see cref="FieldFragList">FieldFragList</see>
	/// .
	/// </summary>
	public class WeightedFieldFragList : FieldFragList
	{
		/// <summary>a constructor.</summary>
		/// <remarks>a constructor.</remarks>
		/// <param name="fragCharSize">the length (number of chars) of a fragment</param>
		public WeightedFieldFragList(int fragCharSize) : base(fragCharSize)
		{
		}

		public override void Add(int startOffset, int endOffset, IList<FieldPhraseList.WeightedPhraseInfo
			> phraseInfoList)
		{
			IList<FieldFragList.WeightedFragInfo.SubInfo> tempSubInfos = new AList<FieldFragList.WeightedFragInfo.SubInfo
				>();
			IList<FieldFragList.WeightedFragInfo.SubInfo> realSubInfos = new AList<FieldFragList.WeightedFragInfo.SubInfo
				>();
			HashSet<string> distinctTerms = new HashSet<string>();
			int length = 0;
			foreach (FieldPhraseList.WeightedPhraseInfo phraseInfo in phraseInfoList)
			{
				float phraseTotalBoost = 0;
				foreach (FieldTermStack.TermInfo ti in phraseInfo.GetTermsInfos())
				{
					if (distinctTerms.AddItem(ti.GetText()))
					{
						phraseTotalBoost += ti.GetWeight() * phraseInfo.GetBoost();
					}
					length++;
				}
				tempSubInfos.AddItem(new FieldFragList.WeightedFragInfo.SubInfo(phraseInfo.GetText
					(), phraseInfo.GetTermsOffsets(), phraseInfo.GetSeqnum(), phraseTotalBoost));
			}
			// We want that terms per fragment (length) is included into the weight. Otherwise a one-word-query
			// would cause an equal weight for all fragments regardless of how much words they contain.  
			// To avoid that fragments containing a high number of words possibly "outrank" more relevant fragments
			// we "bend" the length with a standard-normalization a little bit.
			float norm = length * (1 / (float)Math.Sqrt(length));
			float totalBoost = 0;
			foreach (FieldFragList.WeightedFragInfo.SubInfo tempSubInfo in tempSubInfos)
			{
				float subInfoBoost = tempSubInfo.GetBoost() * norm;
				realSubInfos.AddItem(new FieldFragList.WeightedFragInfo.SubInfo(tempSubInfo.GetText
					(), tempSubInfo.GetTermsOffsets(), tempSubInfo.GetSeqnum(), subInfoBoost));
				totalBoost += subInfoBoost;
			}
			GetFragInfos().AddItem(new FieldFragList.WeightedFragInfo(startOffset, endOffset, 
				realSubInfos, totalBoost));
		}
	}
}
