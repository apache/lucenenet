// Lucene version compatibility level 7.1.0
using ICU4N.Text;
using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Support;

namespace Lucene.Net.Analysis.Icu
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
    /// A <see cref="TokenFilter"/> that transforms text with ICU.
    /// </summary>
    /// <remarks>
    /// ICU provides text-transformation functionality via its Transliteration API.
    /// Although script conversion is its most common use, a Transliterator can
    /// actually perform a more general class of tasks. In fact, Transliterator
    /// defines a very general API which specifies only that a segment of the input
    /// text is replaced by new text. The particulars of this conversion are
    /// determined entirely by subclasses of Transliterator.
    /// <para/>
    /// Some useful transformations for search are built-in:
    /// <list type="bullet">
    ///     <item><description>Conversion from Traditional to Simplified Chinese characters</description></item>
    ///     <item><description>Conversion from Hiragana to Katakana</description></item>
    ///     <item><description>Conversion from Fullwidth to Halfwidth forms.</description></item>
    ///     <item><description>Script conversions, for example Serbian Cyrillic to Latin</description></item>
    /// </list>
    /// <para/>
    /// Example usage: 
    /// <code>
    ///     stream = new ICUTransformFilter(stream, Transliterator.GetInstance("Traditional-Simplified"));
    /// </code>
    /// <para/>
    /// For more details, see the <a href="http://userguide.icu-project.org/transforms/general">ICU User Guide</a>.
    /// </remarks>
    [ExceptionToClassNameConvention]
    public sealed class ICUTransformFilter : TokenFilter
    {
        // Transliterator to transform the text
        private readonly Transliterator transform;

        // Reusable position object
        private readonly TransliterationPosition position = new TransliterationPosition();

        // term attribute, will be updated with transformed text.
        private readonly ICharTermAttribute termAtt;

        // Wraps a termAttribute around the replaceable interface.
        private readonly ReplaceableTermAttribute replaceableAttribute = new ReplaceableTermAttribute();

        /// <summary>
        /// Create a new <see cref="ICUTransformFilter"/> that transforms text on the given stream.
        /// </summary>
        /// <param name="input"><see cref="TokenStream"/> to filter.</param>
        /// <param name="transform">Transliterator to transform the text.</param>
        public ICUTransformFilter(TokenStream input, Transliterator transform)
            : base(input)
        {
            this.transform = transform;
            this.termAtt = AddAttribute<ICharTermAttribute>();

            /* 
             * This is cheating, but speeds things up a lot.
             * If we wanted to use pkg-private APIs we could probably do better.
             */
#pragma warning disable 612, 618
            if (transform.Filter is null && transform is RuleBasedTransliterator)
#pragma warning restore 612, 618
            {
                UnicodeSet sourceSet = transform.GetSourceSet();
                if (sourceSet != null && sourceSet.Any())
                    transform.Filter=sourceSet;
            }
        }

        public override bool IncrementToken()
        {
            /*
             * Wrap around replaceable. clear the positions, and transliterate.
             */
            if (m_input.IncrementToken())
            {
                replaceableAttribute.SetText(termAtt);

                int length = termAtt.Length;
                position.Start = 0;
                position.Limit = length;
                position.ContextStart = 0;
                position.ContextLimit = length;

                transform.FilteredTransliterate(replaceableAttribute, position, false);
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Wrap a <see cref="ICharTermAttribute"/> with the <see cref="IReplaceable"/> API.
        /// </summary>
        private sealed class ReplaceableTermAttribute : IReplaceable
        {
            private char[] buffer;
            private int length;
            private ICharTermAttribute token;

            public void SetText(ICharTermAttribute token)
            {
                this.token = token;
                this.buffer = token.Buffer;
                this.length = token.Length;
            }

            public int Char32At(int pos) => UTF16.CharAt(buffer, 0, length, pos);

            public char this[int pos] => buffer[pos];

            public void Copy(int startIndex, int length, int destinationIndex) // LUCENENET: Changed 2nd parameter from limit to length
            {
                char[] text = new char[length]; // LUCENENET: Corrected length
                CopyTo(startIndex, text, 0, length); // LUCENENET: Corrected length
                Replace(destinationIndex, destinationIndex - destinationIndex, text, 0, length); // LUCENENET: Corrected length & charsLen
            }

            public void CopyTo(int sourceIndex, char[] destination, int destinationIndex, int count)
            {
                Arrays.Copy(buffer, sourceIndex, destination, destinationIndex, count);
            }

            public bool HasMetaData => false;

            public int Length => length;

            public void Replace(int start, int length, string text) // LUCENENET: Changed 2nd parameter from limit to length
            {
                int charsLen = text.Length;
                int newLength = ShiftForReplace(start, length + start, charsLen); // LUCENENET: Changed 2nd parameter to calculate limit
                // insert the replacement text
                text.CopyTo(0, buffer, start, charsLen);
                token.Length = (this.length = newLength);
            }

            public void Replace(int start, int length, char[] text, int charsStart,
                int charsLen)
            {
                // shift text if necessary for the replacement
                int newLength = ShiftForReplace(start, length + start, charsLen); // LUCENENET: Changed 2nd parameter to calculate limit
                // insert the replacement text
                Arrays.Copy(text, charsStart, buffer, start, charsLen);
                token.Length = (this.length = newLength);
            }

            /// <summary>shift text (if necessary) for a replacement operation</summary>
            private int ShiftForReplace(int start, int limit, int charsLen)
            {
                int replacementLength = limit - start;
                int newLength = length - replacementLength + charsLen;
                // resize if necessary
                if (newLength > length)
                    buffer = token.ResizeBuffer(newLength);
                // if the substring being replaced is longer or shorter than the
                // replacement, need to shift things around
                if (replacementLength != charsLen && limit < length)
                    Arrays.Copy(buffer, limit, buffer, start + charsLen, length - limit);
                return newLength;
            }
        }
    }
}
