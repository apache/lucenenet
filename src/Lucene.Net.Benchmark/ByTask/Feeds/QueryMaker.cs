using Lucene.Net.Benchmarks.ByTask.Utils;
using Lucene.Net.Search;
using System;

namespace Lucene.Net.Benchmarks.ByTask.Feeds
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
    /// Create queries for the test.
    /// </summary>
    public interface IQueryMaker
    {
        /// <summary>
        /// Create the next query, of the given size.
        /// </summary>
        /// <param name="size">The size of the query - number of terms, etc.</param>
        /// <returns></returns>
        /// <exception cref="Exception">If cannot make the query, or if size > 0 was specified but this feature is not supported.</exception>
        Query MakeQuery(int size);

        /// <summary>Create the next query</summary>
        Query MakeQuery();

        /// <summary>Set the properties</summary>
        void SetConfig(Config config);

        /// <summary>Reset inputs so that the test run would behave, input wise, as if it just started.</summary>
        void ResetInputs();

        /// <summary>Print the queries</summary>
        string PrintQueries();
    }
}
