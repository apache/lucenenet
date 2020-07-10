using System.Text;

namespace Lucene.Net.Search.VectorHighlight
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
    /// Finds fragment boundaries: pluggable into <see cref="BaseFragmentsBuilder"/>
    /// </summary>
    public interface IBoundaryScanner
    {
        /// <summary>
        /// Scan backward to find end offset.
        /// </summary>
        /// <param name="buffer">scanned object</param>
        /// <param name="start">offset to begin</param>
        /// <returns>the found start offset</returns>
        int FindStartOffset(StringBuilder buffer, int start);

        /// <summary>
        /// Scan forward to find start offset.
        /// </summary>
        /// <param name="buffer">scanned object</param>
        /// <param name="start">start offset to begin</param>
        /// <returns>the found end offset</returns>
        int FindEndOffset(StringBuilder buffer, int start);
    }
}
