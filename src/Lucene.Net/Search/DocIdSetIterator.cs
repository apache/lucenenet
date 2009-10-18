/**
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

namespace Lucene.Net.Search
{
    public abstract class DocIdSetIterator
    {
        /// <summary>
        /// Returns the current document number.  This is invalid until Next() is called for the first time.
        /// </summary>
        /// <returns>the current doc number</returns> 
        public abstract int Doc();

        /// <summary>
        /// Moves to the next docId in the set.  Returns true, iff there is such a docId.
        /// </summary>
        /// <returns>true if there is a next docId</returns>
        public abstract bool Next();

        /// <summary>
        /// Skips entries to the first beyond the current whose document number is
        /// greater than or equal to <i>target</i>.  Returns true iff there is such
        /// an entry.
        /// <p>
        /// Behaves as if written:
        /// <pre>
        ///   boolean skipTo(int target) {
        ///     do {
        ///       if (!next())
        ///         return false;
        ///     } while (target > doc());
        ///     return true;
        ///   }
        /// </pre>
        /// Some implementations are considerably more efficient than that.
        /// </p>
        /// </summary>
        /// <returns>true if there is a docId greater than or equal to target</returns>
        public abstract bool SkipTo(int target);
    }
}
