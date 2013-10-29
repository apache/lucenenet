using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WeightedPhraseInfo = Lucene.Net.Search.VectorHighlight.FieldPhraseList.WeightedPhraseInfo;

namespace Lucene.Net.Search.VectorHighlight
{
    public abstract class BaseFragListBuilder : IFragListBuilder
    {
        public static readonly int MARGIN_DEFAULT = 6;
        public static readonly int MIN_FRAG_CHAR_SIZE_FACTOR = 3;
        readonly int margin;
        readonly int minFragCharSize;

        public BaseFragListBuilder(int margin)
        {
            if (margin < 0)
                throw new ArgumentException(@"margin(" + margin + @") is too small. It must be 0 or higher.");
            this.margin = margin;
            this.minFragCharSize = Math.Max(1, margin * MIN_FRAG_CHAR_SIZE_FACTOR);
        }

        public BaseFragListBuilder()
            : this(MARGIN_DEFAULT)
        {
        }

        protected virtual FieldFragList CreateFieldFragList(FieldPhraseList fieldPhraseList, FieldFragList fieldFragList, int fragCharSize)
        {
            if (fragCharSize < minFragCharSize)
                throw new ArgumentException(@"fragCharSize(" + fragCharSize + @") is too small. It must be " + minFragCharSize + @" or higher.");
            List<WeightedPhraseInfo> wpil = new List<WeightedPhraseInfo>();
            IteratorQueue<WeightedPhraseInfo> queue = new IteratorQueue<WeightedPhraseInfo>(fieldPhraseList.PhraseList.GetEnumerator());
            WeightedPhraseInfo phraseInfo = null;
            int startOffset = 0;
            while ((phraseInfo = queue.Top()) != null)
            {
                if (phraseInfo.StartOffset < startOffset)
                {
                    queue.RemoveTop();
                    continue;
                }

                wpil.Clear();
                int currentPhraseStartOffset = phraseInfo.StartOffset;
                int currentPhraseEndOffset = phraseInfo.EndOffset;
                int spanStart = Math.Max(currentPhraseStartOffset - margin, startOffset);
                int spanEnd = Math.Max(currentPhraseEndOffset, spanStart + fragCharSize);
                if (AcceptPhrase(queue.RemoveTop(), currentPhraseEndOffset - currentPhraseStartOffset, fragCharSize))
                {
                    wpil.Add(phraseInfo);
                }

                while ((phraseInfo = queue.Top()) != null)
                {
                    if (phraseInfo.EndOffset <= spanEnd)
                    {
                        currentPhraseEndOffset = phraseInfo.EndOffset;
                        if (AcceptPhrase(queue.RemoveTop(), currentPhraseEndOffset - currentPhraseStartOffset, fragCharSize))
                        {
                            wpil.Add(phraseInfo);
                        }
                    }
                    else
                    {
                        break;
                    }
                }

                if (wpil.Count == 0)
                {
                    continue;
                }

                int matchLen = currentPhraseEndOffset - currentPhraseStartOffset;
                int newMargin = Math.Max(0, (fragCharSize - matchLen) / 2);
                spanStart = currentPhraseStartOffset - newMargin;
                if (spanStart < startOffset)
                {
                    spanStart = startOffset;
                }

                spanEnd = spanStart + Math.Max(matchLen, fragCharSize);
                startOffset = spanEnd;
                fieldFragList.Add(spanStart, spanEnd, wpil);
            }

            return fieldFragList;
        }

        protected virtual bool AcceptPhrase(WeightedPhraseInfo info, int matchLength, int fragCharSize)
        {
            return info.TermsOffsets.Count <= 1 || matchLength <= fragCharSize;
        }

        private sealed class IteratorQueue<T>
            where T : class
        {
            private readonly IEnumerator<T> iter;
            private T top;
            public IteratorQueue(IEnumerator<T> iter)
            {
                this.iter = iter;
                T removeTop = RemoveTop();
            }

            public T Top()
            {
                return top;
            }

            public T RemoveTop()
            {
                T currentTop = top;
                if (iter.MoveNext())
                {
                    top = iter.Current;
                }
                else
                {
                    top = null;
                }

                return currentTop;
            }
        }

        public abstract FieldFragList CreateFieldFragList(FieldPhraseList fieldPhraseList, int fragCharSize);
    }
}
