namespace Lucene.Net.Search.Spell
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
    /// Set of strategies for suggesting related terms
    /// @lucene.experimental
    /// </summary>
    public enum SuggestMode
    {
        /// <summary>
        /// Generate suggestions only for terms not in the index (default)
        /// </summary>
        SUGGEST_WHEN_NOT_IN_INDEX = 0, // LUCENENET NOTE: Zero is the default value for an uninitialized enum - http://stackoverflow.com/a/2409671/181087

        /// <summary>
        /// Return only suggested words that are as frequent or more frequent than the
        /// searched word
        /// </summary>
        SUGGEST_MORE_POPULAR,

        /// <summary>
        /// Always attempt to offer suggestions (however, other parameters may limit
        /// suggestions. For example, see
        /// <see cref="DirectSpellChecker.MaxQueryFrequency"/> ).
        /// </summary>
        SUGGEST_ALWAYS
    }
}