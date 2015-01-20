/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Collections.Generic;
using System.Text;
using Lucene.Net.Search.Vectorhighlight;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Search.Vectorhighlight
{
	/// <summary>
	/// FieldPhraseList has a list of WeightedPhraseInfo that is used by FragListBuilder
	/// to create a FieldFragList object.
	/// </summary>
	/// <remarks>
	/// FieldPhraseList has a list of WeightedPhraseInfo that is used by FragListBuilder
	/// to create a FieldFragList object.
	/// </remarks>
	public class FieldPhraseList
	{
		/// <summary>List of non-overlapping WeightedPhraseInfo objects.</summary>
		/// <remarks>List of non-overlapping WeightedPhraseInfo objects.</remarks>
		internal List<FieldPhraseList.WeightedPhraseInfo> phraseList = new List<FieldPhraseList.WeightedPhraseInfo
			>();

		/// <summary>create a FieldPhraseList that has no limit on the number of phrases to analyze
		/// 	</summary>
		/// <param name="fieldTermStack">FieldTermStack object</param>
		/// <param name="fieldQuery">FieldQuery object</param>
		public FieldPhraseList(FieldTermStack fieldTermStack, FieldQuery fieldQuery) : this
			(fieldTermStack, fieldQuery, int.MaxValue)
		{
		}

		/// <summary>return the list of WeightedPhraseInfo.</summary>
		/// <remarks>return the list of WeightedPhraseInfo.</remarks>
		/// <returns>phraseList.</returns>
		public virtual IList<FieldPhraseList.WeightedPhraseInfo> GetPhraseList()
		{
			return phraseList;
		}

		/// <summary>a constructor.</summary>
		/// <remarks>a constructor.</remarks>
		/// <param name="fieldTermStack">FieldTermStack object</param>
		/// <param name="fieldQuery">FieldQuery object</param>
		/// <param name="phraseLimit">maximum size of phraseList</param>
		public FieldPhraseList(FieldTermStack fieldTermStack, FieldQuery fieldQuery, int 
			phraseLimit)
		{
			string field = fieldTermStack.GetFieldName();
			List<FieldTermStack.TermInfo> phraseCandidate = new List<FieldTermStack.TermInfo>
				();
			FieldQuery.QueryPhraseMap currMap = null;
			FieldQuery.QueryPhraseMap nextMap = null;
			while (!fieldTermStack.IsEmpty() && (phraseList.Count < phraseLimit))
			{
				phraseCandidate.Clear();
				FieldTermStack.TermInfo ti = null;
				FieldTermStack.TermInfo first = null;
				first = ti = fieldTermStack.Pop();
				currMap = fieldQuery.GetFieldTermMap(field, ti.GetText());
				while (currMap == null && ti.GetNext() != first)
				{
					ti = ti.GetNext();
					currMap = fieldQuery.GetFieldTermMap(field, ti.GetText());
				}
				// if not found, discard top TermInfo from stack, then try next element
				if (currMap == null)
				{
					continue;
				}
				// if found, search the longest phrase
				phraseCandidate.AddItem(ti);
				while (true)
				{
					first = ti = fieldTermStack.Pop();
					nextMap = null;
					if (ti != null)
					{
						nextMap = currMap.GetTermMap(ti.GetText());
						while (nextMap == null && ti.GetNext() != first)
						{
							ti = ti.GetNext();
							nextMap = currMap.GetTermMap(ti.GetText());
						}
					}
					if (ti == null || nextMap == null)
					{
						if (ti != null)
						{
							fieldTermStack.Push(ti);
						}
						if (currMap.IsValidTermOrPhrase(phraseCandidate))
						{
							AddIfNoOverlap(new FieldPhraseList.WeightedPhraseInfo(phraseCandidate, currMap.GetBoost
								(), currMap.GetTermOrPhraseNumber()));
						}
						else
						{
							while (phraseCandidate.Count > 1)
							{
								fieldTermStack.Push(phraseCandidate.RemoveLast());
								currMap = fieldQuery.SearchPhrase(field, phraseCandidate);
								if (currMap != null)
								{
									AddIfNoOverlap(new FieldPhraseList.WeightedPhraseInfo(phraseCandidate, currMap.GetBoost
										(), currMap.GetTermOrPhraseNumber()));
									break;
								}
							}
						}
						break;
					}
					else
					{
						phraseCandidate.AddItem(ti);
						currMap = nextMap;
					}
				}
			}
		}

		/// <summary>Merging constructor.</summary>
		/// <remarks>Merging constructor.</remarks>
		/// <param name="toMerge">FieldPhraseLists to merge to build this one</param>
		public FieldPhraseList(Lucene.Net.Search.Vectorhighlight.FieldPhraseList[]
			 toMerge)
		{
			// Merge all overlapping WeightedPhraseInfos
			// Step 1.  Sort by startOffset, endOffset, and boost, in that order.
			Iterator<FieldPhraseList.WeightedPhraseInfo>[] allInfos = new Iterator[toMerge.Length
				];
			int index = 0;
			foreach (Lucene.Net.Search.Vectorhighlight.FieldPhraseList fplToMerge in toMerge)
			{
				allInfos[index++] = fplToMerge.phraseList.Iterator();
			}
			MergedIterator<FieldPhraseList.WeightedPhraseInfo> itr = new MergedIterator<FieldPhraseList.WeightedPhraseInfo
				>(false, allInfos);
			// Step 2.  Walk the sorted list merging infos that overlap
			phraseList = new List<FieldPhraseList.WeightedPhraseInfo>();
			if (!itr.HasNext())
			{
				return;
			}
			IList<FieldPhraseList.WeightedPhraseInfo> work = new AList<FieldPhraseList.WeightedPhraseInfo
				>();
			FieldPhraseList.WeightedPhraseInfo first = itr.Next();
			work.AddItem(first);
			int workEndOffset = first.GetEndOffset();
			while (itr.HasNext())
			{
				FieldPhraseList.WeightedPhraseInfo current = itr.Next();
				if (current.GetStartOffset() <= workEndOffset)
				{
					workEndOffset = Math.Max(workEndOffset, current.GetEndOffset());
					work.AddItem(current);
				}
				else
				{
					if (work.Count == 1)
					{
						phraseList.AddItem(work[0]);
						work.Set(0, current);
					}
					else
					{
						phraseList.AddItem(new FieldPhraseList.WeightedPhraseInfo(work));
						work.Clear();
						work.AddItem(current);
					}
					workEndOffset = current.GetEndOffset();
				}
			}
			if (work.Count == 1)
			{
				phraseList.AddItem(work[0]);
			}
			else
			{
				phraseList.AddItem(new FieldPhraseList.WeightedPhraseInfo(work));
				work.Clear();
			}
		}

		public virtual void AddIfNoOverlap(FieldPhraseList.WeightedPhraseInfo wpi)
		{
			foreach (FieldPhraseList.WeightedPhraseInfo existWpi in GetPhraseList())
			{
				if (existWpi.IsOffsetOverlap(wpi))
				{
					// WeightedPhraseInfo.addIfNoOverlap() dumps the second part of, for example, hyphenated words (social-economics). 
					// The result is that all informations in TermInfo are lost and not available for further operations. 
					Sharpen.Collections.AddAll(existWpi.GetTermsInfos(), wpi.GetTermsInfos());
					return;
				}
			}
			GetPhraseList().AddItem(wpi);
		}

		/// <summary>Represents the list of term offsets and boost for some text</summary>
		public class WeightedPhraseInfo : Comparable<FieldPhraseList.WeightedPhraseInfo>
		{
			private IList<FieldPhraseList.WeightedPhraseInfo.Toffs> termsOffsets;

			private float boost;

			private int seqnum;

			private AList<FieldTermStack.TermInfo> termsInfos;

			// usually termsOffsets.size() == 1,
			// but if position-gap > 1 and slop > 0 then size() could be greater than 1
			// query boost
			/// <summary>Text of the match, calculated on the fly.</summary>
			/// <remarks>Text of the match, calculated on the fly.  Use for debugging only.</remarks>
			/// <returns>the text</returns>
			public virtual string GetText()
			{
				StringBuilder text = new StringBuilder();
				foreach (FieldTermStack.TermInfo ti in termsInfos)
				{
					text.Append(ti.GetText());
				}
				return text.ToString();
			}

			/// <returns>the termsOffsets</returns>
			public virtual IList<FieldPhraseList.WeightedPhraseInfo.Toffs> GetTermsOffsets()
			{
				return termsOffsets;
			}

			/// <returns>the boost</returns>
			public virtual float GetBoost()
			{
				return boost;
			}

			/// <returns>the termInfos</returns>
			public virtual IList<FieldTermStack.TermInfo> GetTermsInfos()
			{
				return termsInfos;
			}

			public WeightedPhraseInfo(List<FieldTermStack.TermInfo> terms, float boost) : this
				(terms, boost, 0)
			{
			}

			public WeightedPhraseInfo(List<FieldTermStack.TermInfo> terms, float boost, int seqnum
				)
			{
				this.boost = boost;
				this.seqnum = seqnum;
				// We keep TermInfos for further operations
				termsInfos = new AList<FieldTermStack.TermInfo>(terms);
				termsOffsets = new AList<FieldPhraseList.WeightedPhraseInfo.Toffs>(terms.Count);
				FieldTermStack.TermInfo ti = terms[0];
				termsOffsets.AddItem(new FieldPhraseList.WeightedPhraseInfo.Toffs(ti.GetStartOffset
					(), ti.GetEndOffset()));
				if (terms.Count == 1)
				{
					return;
				}
				int pos = ti.GetPosition();
				for (int i = 1; i < terms.Count; i++)
				{
					ti = terms[i];
					if (ti.GetPosition() - pos == 1)
					{
						FieldPhraseList.WeightedPhraseInfo.Toffs to = termsOffsets[termsOffsets.Count - 1
							];
						to.SetEndOffset(ti.GetEndOffset());
					}
					else
					{
						termsOffsets.AddItem(new FieldPhraseList.WeightedPhraseInfo.Toffs(ti.GetStartOffset
							(), ti.GetEndOffset()));
					}
					pos = ti.GetPosition();
				}
			}

			/// <summary>Merging constructor.</summary>
			/// <remarks>Merging constructor.  Note that this just grabs seqnum from the first info.
			/// 	</remarks>
			public WeightedPhraseInfo(ICollection<FieldPhraseList.WeightedPhraseInfo> toMerge
				)
			{
				// Pretty much the same idea as merging FieldPhraseLists:
				// Step 1.  Sort by startOffset, endOffset
				//          While we are here merge the boosts and termInfos
				Iterator<FieldPhraseList.WeightedPhraseInfo> toMergeItr = toMerge.Iterator();
				if (!toMergeItr.HasNext())
				{
					throw new ArgumentException("toMerge must contain at least one WeightedPhraseInfo."
						);
				}
				FieldPhraseList.WeightedPhraseInfo first = toMergeItr.Next();
				Iterator<FieldPhraseList.WeightedPhraseInfo.Toffs>[] allToffs = new Iterator[toMerge
					.Count];
				termsInfos = new AList<FieldTermStack.TermInfo>();
				seqnum = first.seqnum;
				boost = first.boost;
				allToffs[0] = first.termsOffsets.Iterator();
				int index = 1;
				while (toMergeItr.HasNext())
				{
					FieldPhraseList.WeightedPhraseInfo info = toMergeItr.Next();
					boost += info.boost;
					Sharpen.Collections.AddAll(termsInfos, info.termsInfos);
					allToffs[index++] = info.termsOffsets.Iterator();
				}
				// Step 2.  Walk the sorted list merging overlaps
				MergedIterator<FieldPhraseList.WeightedPhraseInfo.Toffs> itr = new MergedIterator
					<FieldPhraseList.WeightedPhraseInfo.Toffs>(false, allToffs);
				termsOffsets = new AList<FieldPhraseList.WeightedPhraseInfo.Toffs>();
				if (!itr.HasNext())
				{
					return;
				}
				FieldPhraseList.WeightedPhraseInfo.Toffs work = itr.Next();
				while (itr.HasNext())
				{
					FieldPhraseList.WeightedPhraseInfo.Toffs current = itr.Next();
					if (current.startOffset <= work.endOffset)
					{
						work.endOffset = Math.Max(work.endOffset, current.endOffset);
					}
					else
					{
						termsOffsets.AddItem(work);
						work = current;
					}
				}
				termsOffsets.AddItem(work);
			}

			public virtual int GetStartOffset()
			{
				return termsOffsets[0].startOffset;
			}

			public virtual int GetEndOffset()
			{
				return termsOffsets[termsOffsets.Count - 1].endOffset;
			}

			public virtual bool IsOffsetOverlap(FieldPhraseList.WeightedPhraseInfo other)
			{
				int so = GetStartOffset();
				int eo = GetEndOffset();
				int oso = other.GetStartOffset();
				int oeo = other.GetEndOffset();
				if (so <= oso && oso < eo)
				{
					return true;
				}
				if (so < oeo && oeo <= eo)
				{
					return true;
				}
				if (oso <= so && so < oeo)
				{
					return true;
				}
				if (oso < eo && eo <= oeo)
				{
					return true;
				}
				return false;
			}

			public override string ToString()
			{
				StringBuilder sb = new StringBuilder();
				sb.Append(GetText()).Append('(').Append(boost).Append(")(");
				foreach (FieldPhraseList.WeightedPhraseInfo.Toffs to in termsOffsets)
				{
					sb.Append(to);
				}
				sb.Append(')');
				return sb.ToString();
			}

			/// <returns>the seqnum</returns>
			public virtual int GetSeqnum()
			{
				return seqnum;
			}

			public virtual int CompareTo(FieldPhraseList.WeightedPhraseInfo other)
			{
				int diff = GetStartOffset() - other.GetStartOffset();
				if (diff != 0)
				{
					return diff;
				}
				diff = GetEndOffset() - other.GetEndOffset();
				if (diff != 0)
				{
					return diff;
				}
				return (int)Math.Signum(GetBoost() - other.GetBoost());
			}

			public override int GetHashCode()
			{
				int prime = 31;
				int result = 1;
				result = prime * result + GetStartOffset();
				result = prime * result + GetEndOffset();
				long b = double.DoubleToLongBits(GetBoost());
				result = prime * result + (int)(b ^ ((long)(((ulong)b) >> 32)));
				return result;
			}

			public override bool Equals(object obj)
			{
				if (this == obj)
				{
					return true;
				}
				if (obj == null)
				{
					return false;
				}
				if (GetType() != obj.GetType())
				{
					return false;
				}
				FieldPhraseList.WeightedPhraseInfo other = (FieldPhraseList.WeightedPhraseInfo)obj;
				if (GetStartOffset() != other.GetStartOffset())
				{
					return false;
				}
				if (GetEndOffset() != other.GetEndOffset())
				{
					return false;
				}
				if (GetBoost() != other.GetBoost())
				{
					return false;
				}
				return true;
			}

			/// <summary>Term offsets (start + end)</summary>
			public class Toffs : Comparable<FieldPhraseList.WeightedPhraseInfo.Toffs>
			{
				private int startOffset;

				private int endOffset;

				public Toffs(int startOffset, int endOffset)
				{
					this.startOffset = startOffset;
					this.endOffset = endOffset;
				}

				public virtual void SetEndOffset(int endOffset)
				{
					this.endOffset = endOffset;
				}

				public virtual int GetStartOffset()
				{
					return startOffset;
				}

				public virtual int GetEndOffset()
				{
					return endOffset;
				}

				public virtual int CompareTo(FieldPhraseList.WeightedPhraseInfo.Toffs other)
				{
					int diff = GetStartOffset() - other.GetStartOffset();
					if (diff != 0)
					{
						return diff;
					}
					return GetEndOffset() - other.GetEndOffset();
				}

				public override int GetHashCode()
				{
					int prime = 31;
					int result = 1;
					result = prime * result + GetStartOffset();
					result = prime * result + GetEndOffset();
					return result;
				}

				public override bool Equals(object obj)
				{
					if (this == obj)
					{
						return true;
					}
					if (obj == null)
					{
						return false;
					}
					if (GetType() != obj.GetType())
					{
						return false;
					}
					FieldPhraseList.WeightedPhraseInfo.Toffs other = (FieldPhraseList.WeightedPhraseInfo.Toffs
						)obj;
					if (GetStartOffset() != other.GetStartOffset())
					{
						return false;
					}
					if (GetEndOffset() != other.GetEndOffset())
					{
						return false;
					}
					return true;
				}

				public override string ToString()
				{
					StringBuilder sb = new StringBuilder();
					sb.Append('(').Append(startOffset).Append(',').Append(endOffset).Append(')');
					return sb.ToString();
				}
			}
		}
	}
}
