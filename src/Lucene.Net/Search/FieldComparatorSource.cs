using System.IO;

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

    /// <summary>
    /// Provides a <see cref="FieldComparer"/> for custom field sorting.
    /// <para/>
    /// @lucene.experimental
    /// </summary>
    public abstract class FieldComparerSource
    {
        /// <summary>
        /// Creates a comparer for the field in the given index.
        /// </summary>
        /// <param name="fieldname">
        ///          Name of the field to create comparer for. </param>
        /// <returns> <see cref="FieldComparer"/>. </returns>
        /// <exception cref="IOException">
        ///           If an error occurs reading the index. </exception>
        public abstract FieldComparer NewComparer(string fieldname, int numHits, int sortPos, bool reversed);
    }
}