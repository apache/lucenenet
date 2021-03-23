// Lucene version compatibility level 4.8.1
using System.Collections.Generic;

namespace Lucene.Net.Analysis.Compound.Hyphenation
{
    /*
     * Licensed to the Apache Software Foundation (ASF) under one or more
     * contributor license agreements.  See the NOTICE file distributed with
     * this work for additional information regarding copyright ownership.
     * The ASF licenses this file to You under the Apache License, Version 2.0
     * (the "License"); you may not use this file except in compliance with
     * the License.  You may obtain a copy of the License at
     * 
     *      http://www.apache.org/licenses/LICENSE-2.0
     * 
     * Unless required by applicable law or agreed to in writing, software
     * distributed under the License is distributed on an "AS IS" BASIS,
     * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
     * See the License for the specific language governing permissions and
     * limitations under the License.
     */

    /// <summary>
    /// This interface is used to connect the XML pattern file parser to the
    /// hyphenation tree.
    /// <para/>
    /// This interface has been taken from the Apache FOP project (http://xmlgraphics.apache.org/fop/). They have been slightly modified.
    /// </summary>
    public interface IPatternConsumer
    {
        /// <summary>
        /// Add a character class. A character class defines characters that are
        /// considered equivalent for the purpose of hyphenation (e.g. "aA"). It
        /// usually means to ignore case.
        /// </summary>
        /// <param name="chargroup"> character group </param>
        void AddClass(string chargroup);

        /// <summary>
        /// Add a hyphenation exception. An exception replaces the result obtained by
        /// the algorithm for cases for which this fails or the user wants to provide
        /// his own hyphenation. A hyphenatedword is a vector of alternating String's
        /// and <see cref="Hyphen"/> instances
        /// </summary>
        void AddException(string word, IList<object> hyphenatedword);

        /// <summary>
        /// Add hyphenation patterns.
        /// </summary>
        /// <param name="pattern"> the pattern </param>
        /// <param name="values"> interletter values expressed as a string of digit characters. </param>
        void AddPattern(string pattern, string values);
    }
}