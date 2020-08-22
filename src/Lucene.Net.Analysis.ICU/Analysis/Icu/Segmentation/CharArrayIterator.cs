// Lucene version compatibility level 8.6.1
#if FEATURE_BREAKITERATOR
using ICU4N.Support.Text;
using Lucene.Net.Support;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Lucene.Net.Analysis.Icu.Segmentation
{
    /*
     * Licensed to the Apache Software Foundation (ASF) under one or more
     * contributor license agreements.  See the NOTICE file distributed with
     * this work for additional information regarding copyright ownership.
     * The ASF licenses this file to You under the Apache License, Version 2.0
     * (the "License"); you may not use this file except in compliance with
     * the License.  You may obtain a copy of the License at
     *
     *     http://www.apache.org/licenses/LICENSE-2.0
     *
     * Unless required by applicable law or agreed to in writing, software
     * distributed under the License is distributed on an "AS IS" BASIS,
     * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
     * See the License for the specific language governing permissions and
     * limitations under the License.
     */

    /// <summary>
    /// Wraps a <see cref="T:char[]"/> as <see cref="CharacterIterator"/> for processing with a <see cref="ICU4N.Text.BreakIterator"/>
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
        public char[] Text => array;

        public int Start => start;

        public int Length => length;

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

        public override char Current => (index == limit) ? Done : array[index];

        public override char First()
        {
            index = start;
            return Current;
        }

        public override int BeginIndex => 0;

        public override int EndIndex => length;

        public override int Index => index - start;

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
                return Done;
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
                return Done;
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