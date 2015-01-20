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
	/// <summary>
	/// A simple implementation of
	/// <see cref="FieldFragList">FieldFragList</see>
	/// .
	/// </summary>
	public class SimpleFieldFragList : FieldFragList
	{
		/// <summary>a constructor.</summary>
		/// <remarks>a constructor.</remarks>
		/// <param name="fragCharSize">the length (number of chars) of a fragment</param>
		public SimpleFieldFragList(int fragCharSize) : base(fragCharSize)
		{
		}

		public override void Add(int startOffset, int endOffset, IList<FieldPhraseList.WeightedPhraseInfo
			> phraseInfoList)
		{
			float totalBoost = 0;
			IList<FieldFragList.WeightedFragInfo.SubInfo> subInfos = new AList<FieldFragList.WeightedFragInfo.SubInfo
				>();
			foreach (FieldPhraseList.WeightedPhraseInfo phraseInfo in phraseInfoList)
			{
				subInfos.AddItem(new FieldFragList.WeightedFragInfo.SubInfo(phraseInfo.GetText(), 
					phraseInfo.GetTermsOffsets(), phraseInfo.GetSeqnum(), phraseInfo.GetBoost()));
				totalBoost += phraseInfo.GetBoost();
			}
			GetFragInfos().AddItem(new FieldFragList.WeightedFragInfo(startOffset, endOffset, 
				subInfos, totalBoost));
		}
	}
}
