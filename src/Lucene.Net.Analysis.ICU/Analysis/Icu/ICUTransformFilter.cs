// LUCENENET TODO: Port issues - missing Transliterator dependency from icu.net

//using Lucene.Net.Analysis.TokenAttributes;

//namespace Lucene.Net.Analysis.ICU
//{
//    /*
//     * Licensed to the Apache Software Foundation (ASF) under one or more
//     * contributor license agreements.  See the NOTICE file distributed with
//     * this work for additional information regarding copyright ownership.
//     * The ASF licenses this file to You under the Apache License, Version 2.0
//     * (the "License"); you may not use this file except in compliance with
//     * the License.  You may obtain a copy of the License at
//     *
//     *     http://www.apache.org/licenses/LICENSE-2.0
//     *
//     * Unless required by applicable law or agreed to in writing, software
//     * distributed under the License is distributed on an "AS IS" BASIS,
//     * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//     * See the License for the specific language governing permissions and
//     * limitations under the License.
//     */

//    public sealed class ICUTransformFilter : TokenFilter
//    {
//        // Transliterator to transform the text
//        private readonly Transliterator transform;

//        // Reusable position object
//        private readonly Transliterator.Position position = new Transliterator.Position();

//        // term attribute, will be updated with transformed text.
//        private readonly ICharTermAttribute termAtt;

//        // Wraps a termAttribute around the replaceable interface.
//        private readonly ReplaceableTermAttribute replaceableAttribute = new ReplaceableTermAttribute();

//        /// <summary>
//        /// Create a new ICUTransformFilter that transforms text on the given stream.
//        /// </summary>
//        /// <param name="input"><see cref="TokenStream"/> to filter.</param>
//        /// <param name="transform">Transliterator to transform the text.</param>
//        public ICUTransformFilter(TokenStream input, Transliterator transform)
//            : base(input)
//        {
//            this.transform = transform;
//            this.termAtt = AddAttribute<ICharTermAttribute>();

//            /* 
//             * This is cheating, but speeds things up a lot.
//             * If we wanted to use pkg-private APIs we could probably do better.
//             */
//            if (transform.getFilter() == null && transform is com.ibm.icu.text.RuleBasedTransliterator)
//            {
//                UnicodeSet sourceSet = transform.getSourceSet();
//                if (sourceSet != null && !sourceSet.isEmpty())
//                    transform.setFilter(sourceSet);
//            }
//        }

//        public override bool IncrementToken()
//        {
//            /*
//             * Wrap around replaceable. clear the positions, and transliterate.
//             */
//            if (m_input.IncrementToken())
//            {
//                replaceableAttribute.SetText(termAtt);

//                int length = termAtt.Length;
//                position.start = 0;
//                position.limit = length;
//                position.contextStart = 0;
//                position.contextLimit = length;

//                transform.FilteredTransliterate(replaceableAttribute, position, false);
//                return true;
//            }
//            else
//            {
//                return false;
//            }
//        }

//        /// <summary>
//        /// Wrap a <see cref="ICharTermAttribute"/> with the Replaceable API.
//        /// </summary>
//        private sealed class ReplaceableTermAttribute //: IReplaceable
//        {
//            private char[] buffer;
//            private int length;
//            private ICharTermAttribute token;

//            public void SetText(ICharTermAttribute token)
//            {
//                this.token = token;
//                this.buffer = token.Buffer;
//                this.length = token.Length;
//            }

//            public int Char32At(int pos)
//            {
//                return UTF16.charAt(buffer, 0, length, pos);
//            }

//            public char CharAt(int pos)
//            {
//                return buffer[pos];
//            }

//            public void Copy(int start, int limit, int dest)
//            {
//                char[] text = new char[limit - start];
//                GetChars(start, limit, text, 0);
//                Replace(dest, dest, text, 0, limit - start);
//            }

//            public void GetChars(int srcStart, int srcLimit, char[] dst, int dstStart)
//            {
//                System.Array.Copy(buffer, srcStart, dst, dstStart, srcLimit - srcStart);
//            }

//            public bool HasMetaData
//            {
//                get { return false; }
//            }

//            public int Length
//            {
//                get { return length; }
//            }

//            public void Replace(int start, int limit, string text)
//            {
//                int charsLen = text.Length;
//                int newLength = ShiftForReplace(start, limit, charsLen);
//                // insert the replacement text
//                //text.getChars(0, charsLen, buffer, start);
//                text.CopyTo(0, buffer, start, charsLen);
//                token.Length = (length = newLength);
//            }

//            public void Replace(int start, int limit, char[] text, int charsStart,
//                int charsLen)
//            {
//                // shift text if necessary for the replacement
//                int newLength = ShiftForReplace(start, limit, charsLen);
//                // insert the replacement text
//                System.Array.Copy(text, charsStart, buffer, start, charsLen);
//                token.Length = (length = newLength);
//            }

//            /// <summary>shift text (if necessary) for a replacement operation</summary>
//            private int ShiftForReplace(int start, int limit, int charsLen)
//            {
//                int replacementLength = limit - start;
//                int newLength = length - replacementLength + charsLen;
//                // resize if necessary
//                if (newLength > length)
//                    buffer = token.ResizeBuffer(newLength);
//                // if the substring being replaced is longer or shorter than the
//                // replacement, need to shift things around
//                if (replacementLength != charsLen && limit < length)
//                    System.Array.Copy(buffer, limit, buffer, start + charsLen, length - limit);
//                return newLength;
//            }
//        }
//    }
//}
