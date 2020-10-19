using Lucene.Net.Diagnostics;

namespace Lucene.Net.Search
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

    using BytesRef = Lucene.Net.Util.BytesRef;

    /// <summary>
    /// Contains statistics for a specific term
    /// <para/>
    /// @lucene.experimental
    /// </summary>
    public class TermStatistics
    {
        private readonly BytesRef term;
        private readonly long docFreq;
        private readonly long totalTermFreq;

        /// <summary>
        /// Sole constructor.
        /// </summary>
        public TermStatistics(BytesRef term, long docFreq, long totalTermFreq)
        {
            if (Debugging.AssertsEnabled)
            {
                Debugging.Assert(docFreq >= 0);
                Debugging.Assert(totalTermFreq == -1 || totalTermFreq >= docFreq); // #positions must be >= #postings
            }
            this.term = term;
            this.docFreq = docFreq;
            this.totalTermFreq = totalTermFreq;
        }

        /// <summary>
        /// Returns the term text </summary>
        public BytesRef Term => term;

        /// <summary>
        /// Returns the number of documents this term occurs in </summary>
        /// <seealso cref="Index.TermsEnum.DocFreq"/>
        public long DocFreq => docFreq;

        /// <summary>
        /// Returns the total number of occurrences of this term </summary>
        /// <seealso cref="Index.TermsEnum.TotalTermFreq"/>
        public long TotalTermFreq => totalTermFreq;
    }
}