// Lucene version compatibility level 4.8.1
#if FEATURE_BREAKITERATOR
using ICU4N.Support.Text;
using Lucene.Net.Support;
﻿using System;
using System.Diagnostics.CodeAnalysis;

namespace Lucene.Net.Analysis.Util
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
    /// A CharacterIterator used internally for use with <see cref="ICU4N.Text.BreakIterator"/>
    /// <para/>
    /// @lucene.internal
    /// </summary>
    public abstract class CharArrayIterator : CharacterIterator
    {
        private char[] array;
        private int start;
        private int index;
        private int length;
        private int limit;

        [WritableArray]
        [SuppressMessage("Microsoft.Performance", "CA1819", Justification = "Lucene's design requires some writable array properties")]
        public virtual char[] Text => array;

        public virtual int Start => start;

        public virtual int Length => length;

        /// <summary>
        /// Set a new region of text to be examined by this iterator
        /// </summary>
        /// <param name="array"> text buffer to examine </param>
        /// <param name="start"> offset into buffer </param>
        /// <param name="length"> maximum length to examine </param>
        public virtual void SetText(char[] array, int start, int length)
        {
            this.array = array;
            this.start = start;
            this.index = start;
            this.length = length;
            this.limit = start + length;
        }

        public override char Current => (index == limit) ? Done : array[index];

        protected abstract char JreBugWorkaround(char ch);
 

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
            {
                throw new ArgumentException("Illegal Position: " + position);
            }
            index = start + position;
            return Current;
        }

        public override object Clone()
        {
            return this.MemberwiseClone();
        }

        /// <summary>
        /// Create a new <see cref="CharArrayIterator"/> that works around JRE bugs
        /// in a manner suitable for <see cref="ICU4N.Text.BreakIterator.GetSentenceInstance()"/>.
        /// </summary>
        public static CharArrayIterator NewSentenceInstance()
        {
            return new CharArrayIteratorAnonymousClass2();
        }

        private sealed class CharArrayIteratorAnonymousClass2 : CharArrayIterator
        {
            // no bugs
            protected override char JreBugWorkaround(char ch)
            {
                return ch;
            }
        }

        /// <summary>
        /// Create a new <see cref="CharArrayIterator"/> that works around JRE bugs
        /// in a manner suitable for <see cref="ICU4N.Text.BreakIterator.GetWordInstance()"/>.
        /// </summary>
        public static CharArrayIterator NewWordInstance()
        {
            return new CharArrayIteratorAnonymousClass4();
        }

        private sealed class CharArrayIteratorAnonymousClass4 : CharArrayIterator
        {
            // no bugs
            protected override char JreBugWorkaround(char ch)
            {
                return ch;
            }
        }
    }
}
#endif