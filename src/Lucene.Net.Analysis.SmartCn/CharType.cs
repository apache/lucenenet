// lucene version compatibility level: 4.8.1
namespace Lucene.Net.Analysis.Cn.Smart
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
    /// Internal <see cref="SmartChineseAnalyzer"/> character type constants.
    /// <para/>
    /// @lucene.experimental
    /// </summary>
    public enum CharType
    {
        /// <summary>
        /// Punctuation Characters
        /// </summary>
        DELIMITER = 0,

        /// <summary>
        /// Letters
        /// </summary>
        LETTER = 1,

        /// <summary>
        /// Numeric Digits
        /// </summary>
        DIGIT = 2,

        /// <summary>
        /// Han Ideographs
        /// </summary>
        HANZI = 3,

        /// <summary>
        /// Characters that act as a space
        /// </summary>
        SPACE_LIKE = 4,

        /// <summary>
        /// Full-Width letters
        /// </summary>
        FULLWIDTH_LETTER = 5,

        /// <summary>
        /// Full-Width alphanumeric characters
        /// </summary>
        FULLWIDTH_DIGIT = 6,

        /// <summary>
        /// Other (not fitting any of the other categories)
        /// </summary>
        OTHER = 7
    }
}
