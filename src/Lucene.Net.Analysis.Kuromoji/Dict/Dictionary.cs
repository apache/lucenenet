namespace Lucene.Net.Analysis.Ja.Dict
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
        /// Get Part-Of-Speech of tokens
        /// </summary>
        /// <param name="wordId">Word ID of token.</param>
        /// <returns>Part-Of-Speech of the token.</returns>
        string GetPartOfSpeech(int wordId);

        /// <summary>
        /// Get reading of tokens.
        /// </summary>
        /// <param name="wordId">Word ID of token.</param>
        /// <param name="surface"></param>
        /// <param name="off"></param>
        /// <param name="len"></param>
        /// <returns>Reading of the token.</returns>
        string GetReading(int wordId, char[] surface, int off, int len);

        /// <summary>
        /// Get base form of word.
        /// </summary>
        /// <param name="wordId">Word ID of token.</param>
        /// <param name="surface"></param>
        /// <param name="off"></param>
        /// <param name="len"></param>
        /// <returns>Base form (only different for inflected words, otherwise null).</returns>
        string GetBaseForm(int wordId, char[] surface, int off, int len);

        /// <summary>
        /// Get pronunciation of tokens
        /// </summary>
        /// <param name="wordId">Word ID of token.</param>
        /// <param name="surface"></param>
        /// <param name="off"></param>
        /// <param name="len"></param>
        /// <returns>Pronunciation of the token.</returns>
        string GetPronunciation(int wordId, char[] surface, int off, int len);

        /// <summary>
        /// Get inflection type of tokens.
        /// </summary>
        /// <param name="wordId">Word ID of token.</param>
        /// <returns>Inflection type, or null.</returns>
        string GetInflectionType(int wordId);

        /// <summary>
        /// Get inflection form of tokens.
        /// </summary>
        /// <param name="wordId">Word ID of token.</param>
        /// <returns>Inflection form, or null.</returns>
        string GetInflectionForm(int wordId);
        // TODO: maybe we should have a optimal method, a non-typesafe
        // 'getAdditionalData' if other dictionaries like unidic have additional data
    }

    // LUCENENT TODO: Make this whole thing into an abstact class??
    public static class Dictionary // LUCENENET specific: CA1052 Static holder types should be Static or NotInheritable
    {
        public static readonly string INTERNAL_SEPARATOR = "\u0000";
    }
}
