using Lucene.Net.Analysis.Ja.Dict;
using Lucene.Net.Support;
using System.Diagnostics.CodeAnalysis;

namespace Lucene.Net.Analysis.Ja
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
    /// Analyzed token with morphological data from its dictionary.
    /// </summary>
    public class Token
    {
        private readonly IDictionary dictionary;

        private readonly int wordId;

        private readonly char[] surfaceForm;
        private readonly int offset;
        private readonly int length;

        private readonly int position;
        private int positionLength;

        private readonly JapaneseTokenizerType type;

        public Token(int wordId, char[] surfaceForm, int offset, int length, JapaneseTokenizerType type, int position, IDictionary dictionary)
        {
            this.wordId = wordId;
            this.surfaceForm = surfaceForm;
            this.offset = offset;
            this.length = length;
            this.type = type;
            this.position = position;
            this.dictionary = dictionary;
        }

        public override string ToString()
        {
            return "Token(\"" + new string(surfaceForm, offset, length) + "\" pos=" + position + " length=" + length +
                " posLen=" + positionLength + " type=" + type + " wordId=" + wordId +
                " leftID=" + dictionary.GetLeftId(wordId) + ")";
        }

        /// <summary>
        /// surfaceForm
        /// </summary>
        [WritableArray]
        [SuppressMessage("Microsoft.Performance", "CA1819", Justification = "Lucene's design requires some writable array properties")]
        public virtual char[] SurfaceForm => surfaceForm;

        /// <summary>
        /// offset into surfaceForm
        /// </summary>
        public virtual int Offset => offset;

        /// <summary>
        /// length of surfaceForm
        /// </summary>
        public virtual int Length => length;

        /// <summary>
        /// surfaceForm as a String
        /// </summary>
        /// <returns>surfaceForm as a String</returns>
        public virtual string GetSurfaceFormString()
        {
            return new string(surfaceForm, offset, length);
        }

        /// <summary>
        /// reading. <c>null</c> if token doesn't have reading.
        /// </summary>
        /// <returns>reading. <c>null</c> if token doesn't have reading.</returns>
        public virtual string GetReading()
        {
            return dictionary.GetReading(wordId, surfaceForm, offset, length);
        }

        /// <summary>
        /// pronunciation. <c>null</c> if token doesn't have pronunciation.
        /// </summary>
        /// <returns>pronunciation. <c>null</c> if token doesn't have pronunciation.</returns>
        public virtual string GetPronunciation()
        {
            return dictionary.GetPronunciation(wordId, surfaceForm, offset, length);
        }

        /// <summary>
        /// part of speech.
        /// </summary>
        /// <returns>part of speech.</returns>
        public virtual string GetPartOfSpeech()
        {
            return dictionary.GetPartOfSpeech(wordId);
        }

        /// <summary>
        /// inflection type or <c>null</c>
        /// </summary>
        /// <returns>inflection type or <c>null</c></returns>
        public virtual string GetInflectionType()
        {
            return dictionary.GetInflectionType(wordId);
        }

        /// <summary>
        /// inflection form or <c>null</c>
        /// </summary>
        /// <returns>inflection form or <c>null</c></returns>
        public virtual string GetInflectionForm()
        {
            return dictionary.GetInflectionForm(wordId);
        }

        /// <summary>
        /// base form or <c>null</c> if token is not inflected
        /// </summary>
        /// <returns>base form or <c>null</c> if token is not inflected</returns>
        public virtual string GetBaseForm()
        {
            return dictionary.GetBaseForm(wordId, surfaceForm, offset, length);
        }

        /// <summary>
        /// Returns <c>true</c> if this token is known word.
        /// </summary>
        /// <returns><c>true</c> if this token is in standard dictionary. <c>false</c> if not.</returns>
        public virtual bool IsKnown => type == JapaneseTokenizerType.KNOWN;

        /// <summary>
        /// Returns <c>true</c> if this token is unknown word.
        /// </summary>
        /// <returns><c>true</c> if this token is unknown word. <c>false</c> if not.</returns>
        public virtual bool IsUnknown => type == JapaneseTokenizerType.UNKNOWN;

        /// <summary>
        /// Returns <c>true</c> if this token is defined in user dictionary.
        /// </summary>
        /// <returns><c>true</c> if this token is in user dictionary. <c>false</c> if not.</returns>
        public virtual bool IsUser => type == JapaneseTokenizerType.USER;

        /// <summary>
        /// Get index of this token in input text. Returns position of token.
        /// </summary>
        public virtual int Position => position;

        /// <summary>
        /// Gets or Sets the length (in tokens) of this token.  For normal
        /// tokens this is 1; for compound tokens it's > 1.
        /// </summary>
        public virtual int PositionLength
        {
            get => positionLength;
            set => this.positionLength = value;
        }
    }
}
