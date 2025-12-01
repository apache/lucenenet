namespace Lucene.Net.Analysis.Ko.Dict
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
    /// Dictionary interface for retrieving morphological data
    /// by id.
    /// </summary>
    public interface IDictionary
    {
        class Morpheme {
            public POS.Tag posTag;
            public char[] surfaceForm;

            public Morpheme(POS.Tag posTag, char[] surfaceForm) {
                this.posTag = posTag;
                this.surfaceForm = surfaceForm;
            }
        }

        /// <summary>
        /// Get left id of specified word.
        /// </summary>
        /// <param name="wordId">Word ID of token.</param>
        /// <returns>Left id.</returns>
        int GetLeftId(int wordId);

        /// <summary>
        /// Get right id of specified word.
        /// </summary>
        /// <param name="wordId">Word ID of token.</param>
        /// <returns>Right id.</returns>
        int GetRightId(int wordId);

        /// <summary>
        /// Get word cost of specified word
        /// </summary>
        /// <param name="wordId">Word ID of token.</param>
        /// <returns>Word's cost.</returns>
        int GetWordCost(int wordId);

        /// <summary>
        /// Get the {@link Type} of specified word (morpheme, compound, inflect or pre-analysis)
        /// </summary>
        /// <param name="wordId">Word ID of token.</param>
        /// <returns>POS.Type.</returns>
        POS.Type GetPOSType(int wordId);

        /// <summary>
        /// Get the left {@link Tag} of specfied word.
        ///
        /// For {@link Type#MORPHEME} and {@link Type#COMPOUND} the left and right POS are the same.
        /// </summary>
        /// <param name="wordId">Word ID of token.</param>
        /// <returns>POS.Tag.</returns>
        POS.Tag GetLeftPOS(int wordId);

        /// <summary>
        /// Get the right {@link Tag} of specfied word.
        ///
        /// For {@link Type#MORPHEME} and {@link Type#COMPOUND} the left and right POS are the same.
        /// </summary>
        /// <param name="wordId">Word ID of token.</param>
        /// <returns>POS.Tag.</returns>
        POS.Tag GetRightPOS(int wordId);

        /// <summary>
        ///Get the reading of specified word (mainly used for Hanja to Hangul conversion).
        /// </summary>
        /// <param name="wordId">Word ID of token.</param>
        /// <returns>POS.Tag.</returns>
        string GetReading(int wordId);

        /// <summary>
        /// Get the morphemes of specified word (e.g. 가깝으나: 가깝 + 으나).
        /// </summary>
        /// <param name="wordId">Word ID of token.</param>
        /// /// <param name="surface"></param>
        /// <param name="off"></param>
        /// <param name="len"></param>
        /// <returns>POS.Tag.</returns>
        Morpheme[] GetMorphemes(int wordId, char[] surfaceForm, int off, int len);
    }
}