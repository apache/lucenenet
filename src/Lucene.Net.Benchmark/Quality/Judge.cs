using System.IO;

namespace Lucene.Net.Benchmarks.Quality
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
    /// Judge if a document is relevant for a quality query.
    /// </summary>
    public interface IJudge
    {
        /// <summary>
        /// Judge if document <paramref name="docName"/> is relevant for the given quality query.
        /// </summary>
        /// <param name="docName">Name of doc tested for relevancy.</param>
        /// <param name="query">Tested quality query.</param>
        /// <returns><c>true</c> if relevant, <c>false</c> if not.</returns>
        bool IsRelevant(string docName, QualityQuery query);

        /// <summary>
        /// Validate that queries and this <see cref="IJudge"/> match each other.
        /// To be perfectly valid, this Judge must have some data for each and every 
        /// input quality query, and must not have any data on any other quality query. 
        /// <b>Note</b>: the quality benchmark run would not fail in case of imperfect
        /// validity, just a warning message would be logged.  
        /// </summary>
        /// <param name="qq">Quality queries to be validated.</param>
        /// <param name="logger">If not <c>null</c>, validation issues are logged.</param>
        /// <returns><c>true</c> if perfectly valid, <c>false</c> if not.</returns>
        bool ValidateData(QualityQuery[] qq, TextWriter logger);

        /// <summary>
        /// Return the maximal recall for the input quality query. 
        /// It is the number of relevant docs this <see cref="IJudge"/> "knows" for the query.
        /// </summary>
        /// <param name="query">The query whose maximal recall is needed.</param>
        /// <returns></returns>
        int MaxRecall(QualityQuery query);
    }
}
