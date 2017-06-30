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
    /// Internal <see cref="SmartChineseAnalyzer"/> token type constants
    /// <para/>
    /// @lucene.experimental
    /// </summary>
    public enum WordType
    {
        /// <summary>
        /// Start of a Sentence
        /// </summary>
        SENTENCE_BEGIN = 0,

        /// <summary>
        /// End of a Sentence
        /// </summary>
        SENTENCE_END = 1,

        /// <summary>
        /// Chinese Word 
        /// </summary>
        CHINESE_WORD = 2,

        /// <summary>
        /// ASCII String
        /// </summary>
        STRING = 3,

        /// <summary>
        /// ASCII Alphanumeric
        /// </summary>
        NUMBER = 4,

        /// <summary>
        /// Punctuation Symbol
        /// </summary>
        DELIMITER = 5,

        /// <summary>
        /// Full-Width String
        /// </summary>
        FULLWIDTH_STRING = 6,

        /// <summary>
        /// Full-Width Alphanumeric
        /// </summary>
        FULLWIDTH_NUMBER = 7
    }
}
