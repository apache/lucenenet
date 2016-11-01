using Lucene.Net.Util;
using System.IO;

namespace Lucene.Net.Analysis.Ngram
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
    /// Tokenizes the input from an edge into n-grams of given size(s).
    /// <para>
    /// This <seealso cref="Tokenizer"/> create n-grams from the beginning edge or ending edge of a input token.
    /// </para>
    /// <para><a name="version" /> As of Lucene 4.4, this tokenizer<ul>
    /// <li>can handle <code>maxGram</code> larger than 1024 chars, but beware that this will result in increased memory usage
    /// <li>doesn't trim the input,
    /// <li>sets position increments equal to 1 instead of 1 for the first token and 0 for all other ones
    /// <li>doesn't support backward n-grams anymore.
    /// <li>supports <seealso cref="#isTokenChar(int) pre-tokenization"/>,
    /// <li>correctly handles supplementary characters.
    /// </ul>
    /// </para>
    /// <para>Although <b style="color:red">highly</b> discouraged, it is still possible
    /// to use the old behavior through <seealso cref="Lucene43EdgeNGramTokenizer"/>.
    /// </para>
    /// </summary>
    public class EdgeNGramTokenizer : NGramTokenizer
    {
        public const int DEFAULT_MAX_GRAM_SIZE = 1;
        public const int DEFAULT_MIN_GRAM_SIZE = 1;

        /// <summary>
        /// Creates EdgeNGramTokenizer that can generate n-grams in the sizes of the given range
        /// </summary>
        /// <param name="version"> the <a href="#version">Lucene match version</a> </param>
        /// <param name="input"> <seealso cref="Reader"/> holding the input to be tokenized </param>
        /// <param name="minGram"> the smallest n-gram to generate </param>
        /// <param name="maxGram"> the largest n-gram to generate </param>
        public EdgeNGramTokenizer(LuceneVersion version, TextReader input, int minGram, int maxGram)
            : base(version, input, minGram, maxGram, true)
        {
        }

        /// <summary>
        /// Creates EdgeNGramTokenizer that can generate n-grams in the sizes of the given range
        /// </summary>
        /// <param name="version"> the <a href="#version">Lucene match version</a> </param>
        /// <param name="factory"> <seealso cref="org.apache.lucene.util.AttributeSource.AttributeFactory"/> to use </param>
        /// <param name="input"> <seealso cref="Reader"/> holding the input to be tokenized </param>
        /// <param name="minGram"> the smallest n-gram to generate </param>
        /// <param name="maxGram"> the largest n-gram to generate </param>
        public EdgeNGramTokenizer(LuceneVersion version, AttributeSource.AttributeFactory factory, TextReader input, int minGram, int maxGram)
            : base(version, factory, input, minGram, maxGram, true)
        {
        }
    }
}