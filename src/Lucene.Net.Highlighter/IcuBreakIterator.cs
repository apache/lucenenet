using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Lucene.Net.Search
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
    /// A <see cref="BreakIterator"/> implementation that encapsulates the functionality
    /// of icu.net's <see cref="Icu.BreakIterator"/> static class. A <see cref="BreakIterator"/>
    /// provides methods to move forward, reverse, and randomly through a set of text breaks
    /// defined by the <see cref="Icu.BreakIterator.UBreakIteratorType"/> enumeration.
    /// </summary>
    // LUCENENET specific type
    internal class IcuBreakIterator : BreakIterator
    {
        private readonly Icu.Locale locale;
        private readonly Icu.BreakIterator.UBreakIteratorType type;

        private List<int> boundaries = new List<int>();
        private int currentBoundaryIndex; // Index (not the value) of the current boundary in boundaries
        private string text;

        public IcuBreakIterator(Icu.BreakIterator.UBreakIteratorType type)
            : this(type, CultureInfo.CurrentCulture)
        {
        }

        public IcuBreakIterator(Icu.BreakIterator.UBreakIteratorType type, CultureInfo locale)
        {
            if (locale == null)
                throw new ArgumentNullException("locale");
            this.locale = new Icu.Locale(locale.Name);
            this.type = type;
        }

        /// <summary>
        /// Sets the current iteration position to the beginning of the text.
        /// </summary>
        /// <returns>The offset of the beginning of the text.</returns>
        public override int First()
        {
            currentBoundaryIndex = 0;
            return ReturnCurrent();
        }

        /// <summary>
        /// Sets the current iteration position to the end of the text.
        /// </summary>
        /// <returns>The text's past-the-end offset.</returns>
        public override int Last()
        {
            currentBoundaryIndex = boundaries.Count - 1;
            return ReturnCurrent();
        }

        /// <summary>
        /// Advances the iterator either forward or backward the specified number of steps.
        /// Negative values move backward, and positive values move forward.  This is
        /// equivalent to repeatedly calling <see cref="Next()"/> or <see cref="Previous()"/>.
        /// </summary>
        /// <param name="n">The number of steps to move.  The sign indicates the direction
        /// (negative is backwards, and positive is forwards).</param>
        /// <returns>The character offset of the boundary position n boundaries away from
        /// the current one.</returns>
        public override int Next(int n)
        {
            int result = Current;
            while (n > 0)
            {
                result = Next();
                --n;
            }
            while (n < 0)
            {
                result = Previous();
                ++n;
            }
            return result;
        }

        /// <summary>
        /// Advances the iterator to the next boundary position.
        /// </summary>
        /// <returns>The position of the first boundary after this one.</returns>
        public override int Next()
        {
            if (currentBoundaryIndex >= boundaries.Count - 1 || boundaries.Count == 0)
            {
                return DONE;
            }
            currentBoundaryIndex++;
            return ReturnCurrent();
        }

        /// <summary>
        /// Advances the iterator backwards, to the last boundary preceding this one.
        /// </summary>
        /// <returns>The position of the last boundary position preceding this one.</returns>
        public override int Previous()
        {
            if (currentBoundaryIndex == 0 || boundaries.Count == 0)
            {
                return DONE;
            }
            currentBoundaryIndex--;
            return ReturnCurrent();
        }

        /// <summary>
        /// Throw <see cref="ArgumentException"/> unless begin &lt;= offset &lt; end.
        /// </summary>
        /// <param name="offset"></param>
        private void CheckOffset(int offset)
        {
            if (offset < start || offset > end)
            {
                throw new ArgumentException("offset out of bounds");
            }
        }

        /// <summary>
        /// Sets the iterator to refer to the first boundary position following
        /// the specified position.
        /// </summary>
        /// <param name="offset">The position from which to begin searching for a break position.</param>
        /// <returns>The position of the first break after the current position.</returns>
        public override int Following(int offset)
        {
            CheckOffset(offset);

            if (boundaries.Count == 0)
            {
                return DONE;
            }

            int following = GetLowestIndexGreaterThan(offset);
            if (following == -1)
            {
                currentBoundaryIndex = boundaries.Count - 1;
                return DONE;
            }
            else
            {
                currentBoundaryIndex = following;
            }
            return ReturnCurrent();
        }

        private int GetLowestIndexGreaterThan(int offset)
        {
            int index = boundaries.BinarySearch(offset);
            if (index < 0)
            {
                return ~index;
            }
            else if (index + 1 < boundaries.Count)
            {
                return index + 1;
            }

            return -1;
        }

        /// <summary>
        /// Sets the iterator to refer to the last boundary position before the
        /// specified position.
        /// </summary>
        /// <param name="offset">The position to begin searching for a break from.</param>
        /// <returns>The position of the last boundary before the starting position.</returns>
        public override int Preceding(int offset)
        {
            CheckOffset(offset);

            if (boundaries.Count == 0)
            {
                return DONE;
            }

            int preceeding = GetHighestIndexLessThan(offset);
            if (preceeding == -1)
            {
                currentBoundaryIndex = 0;
                return DONE;
            }
            else
            {
                currentBoundaryIndex = preceeding;
            }
            return ReturnCurrent();
        }

        private int GetHighestIndexLessThan(int offset)
        {
            int index = boundaries.BinarySearch(offset);
            if (index < 0)
            {
                return ~index - 1;
            }
            else
            {
                // NOTE: This is intentionally allowed to return -1 in the case
                // where index == 0. This state indicates we are before the first boundary.
                return index - 1;
            }
        }

        /// <summary>
        /// Returns the current iteration position.
        /// </summary>
        public override int Current
        {
            get { return ReturnCurrent(); }
        }

        /// <summary>
        /// Gets the text being analyzed.
        /// </summary>
        public override string Text
        {
            get
            {
                return text;
            }
        }

        /// <summary>
        /// Set the iterator to analyze a new piece of text.  This function resets
        /// the current iteration position to the beginning of the text.
        /// </summary>
        /// <param name="newText">The text to analyze.</param>
        public override void SetText(string newText)
        {
            text = newText;
            currentBoundaryIndex = 0;
            start = 0;
            end = newText.Length;

            LoadBoundaries(start, end);
        }

        public override void SetText(CharacterIterator newText)
        {
            text = newText.GetTextAsString();
            currentBoundaryIndex = 0;
            start = newText.BeginIndex;
            end = newText.EndIndex;

            LoadBoundaries(start, end);
        }

        private void LoadBoundaries(int start, int end)
        {
            //boundaries = new List<int>();

            IEnumerable<Icu.Boundary> icuBoundaries;
            string offsetText = text.Substring(start, end - start);


            if (type == Icu.BreakIterator.UBreakIteratorType.WORD)
            {
                // LUCENENET TODO: HACK - replacing hyphen with "a" so hyphenated words aren't broken
                icuBoundaries = Icu.BreakIterator.GetWordBoundaries(locale, offsetText.Replace("-", "a"), true);
            }
            else
            {
                if (type == Icu.BreakIterator.UBreakIteratorType.SENTENCE)
                {
                    // LUCENENET TODO: HACK - newline character causes incorrect sentence breaking.
                    offsetText = offsetText.Replace("\n", " ");
                    // LUCENENET TODO: HACK - the ICU sentence logic doesn't work (in English anyway) when sentences don't
                    // begin with capital letters.
                    offsetText = CapitalizeFirst(offsetText);
                }

                icuBoundaries = Icu.BreakIterator.GetBoundaries(type, locale, offsetText);
            }

            boundaries = icuBoundaries
                .Select(t => new[] { t.Start + start, t.End + start })
                .SelectMany(b => b)
                .Distinct()
                .ToList();
        }

        /// <summary>
        /// Returns true if the specified character offset is a text boundary.
        /// </summary>
        /// <param name="offset">the character offset to check.</param>
        /// <returns><c>true</c> if "offset" is a boundary position, <c>false</c> otherwise.</returns>
        public override bool IsBoundary(int offset)
        {
            CheckOffset(offset);
            return boundaries.Contains(offset);
        }

        private int ReturnCurrent()
        {
            if (boundaries.Count > 0)
            {
                return currentBoundaryIndex < boundaries.Count && currentBoundaryIndex > -1
                    ? boundaries[currentBoundaryIndex]
                    : DONE;
            }

            // If there are no boundaries, we must return the start offset
            return start;
        }

        /// <summary>
        /// LUCENENET TODO: This is a temporary workaround for an issue with icu-dotnet
        /// where it doesn't correctly break sentences unless they begin with a capital letter.
        /// If/when ICU is fixed, this method should be deleted and the IcuBreakIterator 
        /// code changed to remove calls to this method.
        /// </summary>
        public static string CapitalizeFirst(string s)
        {
            bool isNewSentence = true;
            var result = new StringBuilder(s.Length);
            for (int i = 0; i < s.Length; i++)
            {
                if (isNewSentence && char.IsLetter(s[i]))
                {
                    result.Append(char.ToUpper(s[i]));
                    isNewSentence = false;
                }
                else
                    result.Append(s[i]);

                if (s[i] == '!' || s[i] == '?' || s[i] == '.')
                {
                    isNewSentence = true;
                }
            }

            return result.ToString();
        }
    }
}
