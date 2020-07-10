#if FEATURE_BREAKITERATOR
namespace Lucene.Net.Search.PostingsHighlight
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
    /// Creates a formatted snippet from the top passages.
    /// 
    /// @lucene.experimental
    /// </summary>
    public abstract class PassageFormatter
    {
        /// <summary>
        /// Formats the top <paramref name="passages"/> from <paramref name="content"/>
        /// into a human-readable text snippet.
        /// </summary>
        /// <param name="passages">
        /// top-N passages for the field. Note these are sorted in
        /// the order that they appear in the document for convenience.
        /// </param>
        /// <param name="content">content for the field.</param>
        /// <returns>
        /// formatted highlight.  Note that for the
        /// non-expert APIs in <see cref="ICUPostingsHighlighter"/> that
        /// return <see cref="string"/>, the <see cref="object.ToString()"/> method on the <see cref="object"/>
        /// returned by this method is used to compute the string.
        /// </returns>
        public abstract object Format(Passage[] passages, string content); // LUCENENET TODO: API Make return type generic?
    }
}
#endif
