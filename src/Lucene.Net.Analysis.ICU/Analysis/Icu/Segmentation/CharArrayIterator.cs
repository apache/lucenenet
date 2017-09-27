#if FEATURE_BREAKITERATOR
using Lucene.Net.Support;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Lucene.Net.Analysis.Icu.Segmentation
{
    /// <summary>
    /// Wraps a char[] as CharacterIterator for processing with a BreakIterator
    /// <para/>
    /// @lucene.experimental
    /// </summary>
    internal sealed class CharArrayIterator : CharacterIterator
    {
        private char[] array;
        private int start;
        private int index;
        private int length;
        private int limit;

        [WritableArray]
        [SuppressMessage("Microsoft.Performance", "CA1819", Justification = "Lucene's design requires some writable array properties")]
        public char[] Text
        {
            get
            {
                return array;
            }
        }

        public int Start
        {
            get { return start; }
        }

        public int Length
        {
            get { return length; }
        }

        /// <summary>
        /// Set a new region of text to be examined by this iterator
        /// </summary>
        /// <param name="array">text buffer to examine</param>
        /// <param name="start">offset into buffer</param>
        /// <param name="length"> maximum length to examine</param>
        public void SetText(char[] array, int start, int length)
        {
            this.array = array;
            this.start = start;
            this.index = start;
            this.length = length;
            this.limit = start + length;
        }

        public override char Current
        {
            get { return (index == limit) ? DONE : array[index]; }
        }

        public override char First()
        {
            index = start;
            return Current;
        }

        public override int BeginIndex
        {
            get { return 0; }
        }

        public override int EndIndex
        {
            get { return length; }
        }

        public override int Index
        {
            get { return index - start; }
        }

        public override char Last()
        {
            index = (limit == start) ? limit : limit - 1;
            return Current;
        }

        public override char Next()
        {
            if (++index >= limit)
            {
                index = limit;
                return DONE;
            }
            else
            {
                return Current;
            }
        }

        public override char Previous()
        {
            if (--index < start)
            {
                index = start;
                return DONE;
            }
            else
            {
                return Current;
            }
        }

        public override char SetIndex(int position)
        {
            if (position < BeginIndex || position > EndIndex)
                throw new ArgumentException("Illegal Position: " + position);
            index = start + position;
            return Current;
        }

        public override string GetTextAsString()
        {
            return new string(array);
        }

        public override object Clone()
        {
            CharArrayIterator clone = new CharArrayIterator();
            clone.SetText(array, start, length);
            clone.index = index;
            return clone;
        }
    }
}
#endif