/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Collections.Generic;
using System.Text;
using Org.Apache.Lucene.Search.Vectorhighlight;
using Sharpen;

namespace Org.Apache.Lucene.Search.Vectorhighlight
{
	/// <summary>
	/// FieldFragList has a list of "frag info" that is used by FragmentsBuilder class
	/// to create fragments (snippets).
	/// </summary>
	/// <remarks>
	/// FieldFragList has a list of "frag info" that is used by FragmentsBuilder class
	/// to create fragments (snippets).
	/// </remarks>
	public abstract class FieldFragList
	{
		private IList<FieldFragList.WeightedFragInfo> fragInfos = new AList<FieldFragList.WeightedFragInfo
			>();

		/// <summary>a constructor.</summary>
		/// <remarks>a constructor.</remarks>
		/// <param name="fragCharSize">the length (number of chars) of a fragment</param>
		public FieldFragList(int fragCharSize)
		{
		}

		/// <summary>convert the list of WeightedPhraseInfo to WeightedFragInfo, then add it to the fragInfos
		/// 	</summary>
		/// <param name="startOffset">start offset of the fragment</param>
		/// <param name="endOffset">end offset of the fragment</param>
		/// <param name="phraseInfoList">list of WeightedPhraseInfo objects</param>
		public abstract void Add(int startOffset, int endOffset, IList<FieldPhraseList.WeightedPhraseInfo
			> phraseInfoList);

		/// <summary>return the list of WeightedFragInfos.</summary>
		/// <remarks>return the list of WeightedFragInfos.</remarks>
		/// <returns>fragInfos.</returns>
		public virtual IList<FieldFragList.WeightedFragInfo> GetFragInfos()
		{
			return fragInfos;
		}

		/// <summary>List of term offsets + weight for a frag info</summary>
		public class WeightedFragInfo
		{
			private IList<FieldFragList.WeightedFragInfo.SubInfo> subInfos;

			private float totalBoost;

			private int startOffset;

			private int endOffset;

			public WeightedFragInfo(int startOffset, int endOffset, IList<FieldFragList.WeightedFragInfo.SubInfo
				> subInfos, float totalBoost)
			{
				this.startOffset = startOffset;
				this.endOffset = endOffset;
				this.totalBoost = totalBoost;
				this.subInfos = subInfos;
			}

			public virtual IList<FieldFragList.WeightedFragInfo.SubInfo> GetSubInfos()
			{
				return subInfos;
			}

			public virtual float GetTotalBoost()
			{
				return totalBoost;
			}

			public virtual int GetStartOffset()
			{
				return startOffset;
			}

			public virtual int GetEndOffset()
			{
				return endOffset;
			}

			public override string ToString()
			{
				StringBuilder sb = new StringBuilder();
				sb.Append("subInfos=(");
				foreach (FieldFragList.WeightedFragInfo.SubInfo si in subInfos)
				{
					sb.Append(si.ToString());
				}
				sb.Append(")/").Append(totalBoost).Append('(').Append(startOffset).Append(',').Append
					(endOffset).Append(')');
				return sb.ToString();
			}

			/// <summary>Represents the list of term offsets for some text</summary>
			public class SubInfo
			{
				private readonly string text;

				private readonly IList<FieldPhraseList.WeightedPhraseInfo.Toffs> termsOffsets;

				private readonly int seqnum;

				private readonly float boost;

				public SubInfo(string text, IList<FieldPhraseList.WeightedPhraseInfo.Toffs> termsOffsets
					, int seqnum, float boost)
				{
					// unnecessary member, just exists for debugging purpose
					// usually termsOffsets.size() == 1,
					// but if position-gap > 1 and slop > 0 then size() could be greater than 1
					// used for scoring split WeightedPhraseInfos.
					this.text = text;
					this.termsOffsets = termsOffsets;
					this.seqnum = seqnum;
					this.boost = boost;
				}

				public virtual IList<FieldPhraseList.WeightedPhraseInfo.Toffs> GetTermsOffsets()
				{
					return termsOffsets;
				}

				public virtual int GetSeqnum()
				{
					return seqnum;
				}

				public virtual string GetText()
				{
					return text;
				}

				public virtual float GetBoost()
				{
					return boost;
				}

				public override string ToString()
				{
					StringBuilder sb = new StringBuilder();
					sb.Append(text).Append('(');
					foreach (FieldPhraseList.WeightedPhraseInfo.Toffs to in termsOffsets)
					{
						sb.Append(to.ToString());
					}
					sb.Append(')');
					return sb.ToString();
				}
			}
		}
	}
}
