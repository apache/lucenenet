/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Collections.Generic;
using Lucene.Net.Search.Vectorhighlight;
using Sharpen;

namespace Lucene.Net.Search.Vectorhighlight
{
	/// <summary>
	/// A abstract implementation of
	/// <see cref="FragListBuilder">FragListBuilder</see>
	/// .
	/// </summary>
	public abstract class BaseFragListBuilder : FragListBuilder
	{
		public const int MARGIN_DEFAULT = 6;

		public const int MIN_FRAG_CHAR_SIZE_FACTOR = 3;

		internal readonly int margin;

		internal readonly int minFragCharSize;

		public BaseFragListBuilder(int margin)
		{
			if (margin < 0)
			{
				throw new ArgumentException("margin(" + margin + ") is too small. It must be 0 or higher."
					);
			}
			this.margin = margin;
			this.minFragCharSize = Math.Max(1, margin * MIN_FRAG_CHAR_SIZE_FACTOR);
		}

		public BaseFragListBuilder() : this(MARGIN_DEFAULT)
		{
		}

		protected internal virtual FieldFragList CreateFieldFragList(FieldPhraseList fieldPhraseList
			, FieldFragList fieldFragList, int fragCharSize)
		{
			if (fragCharSize < minFragCharSize)
			{
				throw new ArgumentException("fragCharSize(" + fragCharSize + ") is too small. It must be "
					 + minFragCharSize + " or higher.");
			}
			IList<FieldPhraseList.WeightedPhraseInfo> wpil = new AList<FieldPhraseList.WeightedPhraseInfo
				>();
			BaseFragListBuilder.IteratorQueue<FieldPhraseList.WeightedPhraseInfo> queue = new 
				BaseFragListBuilder.IteratorQueue<FieldPhraseList.WeightedPhraseInfo>(fieldPhraseList
				.GetPhraseList().Iterator());
			FieldPhraseList.WeightedPhraseInfo phraseInfo = null;
			int startOffset = 0;
			while ((phraseInfo = queue.Top()) != null)
			{
				// if the phrase violates the border of previous fragment, discard it and try next phrase
				if (phraseInfo.GetStartOffset() < startOffset)
				{
					queue.RemoveTop();
					continue;
				}
				wpil.Clear();
				int currentPhraseStartOffset = phraseInfo.GetStartOffset();
				int currentPhraseEndOffset = phraseInfo.GetEndOffset();
				int spanStart = Math.Max(currentPhraseStartOffset - margin, startOffset);
				int spanEnd = Math.Max(currentPhraseEndOffset, spanStart + fragCharSize);
				if (AcceptPhrase(queue.RemoveTop(), currentPhraseEndOffset - currentPhraseStartOffset
					, fragCharSize))
				{
					wpil.AddItem(phraseInfo);
				}
				while ((phraseInfo = queue.Top()) != null)
				{
					// pull until we crossed the current spanEnd
					if (phraseInfo.GetEndOffset() <= spanEnd)
					{
						currentPhraseEndOffset = phraseInfo.GetEndOffset();
						if (AcceptPhrase(queue.RemoveTop(), currentPhraseEndOffset - currentPhraseStartOffset
							, fragCharSize))
						{
							wpil.AddItem(phraseInfo);
						}
					}
					else
					{
						break;
					}
				}
				if (wpil.IsEmpty())
				{
					continue;
				}
				int matchLen = currentPhraseEndOffset - currentPhraseStartOffset;
				// now recalculate the start and end position to "center" the result
				int newMargin = Math.Max(0, (fragCharSize - matchLen) / 2);
				// matchLen can be > fragCharSize prevent IAOOB here
				spanStart = currentPhraseStartOffset - newMargin;
				if (spanStart < startOffset)
				{
					spanStart = startOffset;
				}
				// whatever is bigger here we grow this out
				spanEnd = spanStart + Math.Max(matchLen, fragCharSize);
				startOffset = spanEnd;
				fieldFragList.Add(spanStart, spanEnd, wpil);
			}
			return fieldFragList;
		}

		/// <summary>
		/// A predicate to decide if the given
		/// <see cref="WeightedPhraseInfo">WeightedPhraseInfo</see>
		/// should be
		/// accepted as a highlighted phrase or if it should be discarded.
		/// <p>
		/// The default implementation discards phrases that are composed of more than one term
		/// and where the matchLength exceeds the fragment character size.
		/// </summary>
		/// <param name="info">the phrase info to accept</param>
		/// <param name="matchLength">the match length of the current phrase</param>
		/// <param name="fragCharSize">the configured fragment character size</param>
		/// <returns><code>true</code> if this phrase info should be accepted as a highligh phrase
		/// 	</returns>
		protected internal virtual bool AcceptPhrase(FieldPhraseList.WeightedPhraseInfo info
			, int matchLength, int fragCharSize)
		{
			return info.GetTermsOffsets().Count <= 1 || matchLength <= fragCharSize;
		}

		private sealed class IteratorQueue<T>
		{
			private readonly Iterator<T> iter;

			private T top;

			public IteratorQueue(Iterator<T> iter)
			{
				this.iter = iter;
				T removeTop = RemoveTop();
			}

			public T Top()
			{
				return removeTop == null;
			}

			public T RemoveTop()
			{
				T currentTop = top;
				if (iter.HasNext())
				{
					top = iter.Next();
				}
				else
				{
					top = null;
				}
				return currentTop;
			}
		}

		public abstract FieldFragList CreateFieldFragList(FieldPhraseList arg1, int arg2);
	}
}
