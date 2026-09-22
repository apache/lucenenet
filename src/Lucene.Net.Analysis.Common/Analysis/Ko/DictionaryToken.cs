using IDictionary = Lucene.Net.Analysis.Ko.Dict.IDictionary;

namespace Lucene.Net.Analysis.Ko
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
    /// A token stored in a Dictionary.
    /// </summary>
    public class DictionaryToken : Token
    {
        private readonly int wordId;
        private readonly KoreanTokenizer.KoreanTokenizerType type;
        private readonly IDictionary dictionary;

        public DictionaryToken(KoreanTokenizer.KoreanTokenizerType type, IDictionary dictionary, int wordId, char[] surfaceForm,
                             int offset, int length, int startOffset, int endOffset)
            : base(surfaceForm, offset, length, startOffset, endOffset) {
            this.type = type;
            this.dictionary = dictionary;
            this.wordId = wordId;
        }


        public override string ToString() {
            return "DictionaryToken(\"" + GetSurfaceFormString() + "\" pos=" + GetStartOffset() + " length=" + GetLength +
                " posLen=" + GetPositionLength() + " type=" + type + " wordId=" + wordId +
                " leftID=" + dictionary.GetLeftId(wordId) + ")";
        }

        /// <summary>
        /// Returns the type of this token
        /// </summary>
        /// <returns> @return token type, not null </returns>
        public KoreanTokenizer.KoreanTokenizerType GetType() {
            return type;
        }

        /// <summary>
        /// Returns true if this token is known word
        /// </summary>
        /// <returns> @return true if this token is in standard dictionary. false if not. </returns>
        public bool IsKnown() {
            return type == KoreanTokenizer.KoreanTokenizerType.KNOWN;
        }

        /// <summary>
        /// Returns true if this token is unknown word
        /// </summary>
        /// <returns> @return true if this token is unknown word. false if not. </returns>
        public bool IsUnknown() {
            return type == KoreanTokenizer.KoreanTokenizerType.UNKNOWN;
        }

        /// <summary>
        /// Returns true if this token is defined in user dictionary
        /// </summary>
        /// <returns> @return true if this token is in user dictionary. false if not. </returns>
        public bool IsUser() {
            return type == KoreanTokenizer.KoreanTokenizerType.USER;
        }

        public override POS.Type GetPOSType()
        {
            return dictionary.GetPOSType(wordId);
        }

        public override POS.Tag GetLeftPOS()
        {
            return dictionary.GetLeftPOS(wordId);
        }
        public override POS.Tag GetRightPOS()
        {
            return dictionary.GetRightPOS(wordId);
        }
        public override string GetReading()
        {
            return dictionary.GetReading(wordId);
        }

        public override IDictionary.Morpheme[] GetMorphemes()
        {
            return dictionary.GetMorphemes(wordId, GetSurfaceForm, GetOffset, GetLength);
        }
}
}