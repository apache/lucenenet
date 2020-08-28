using System;
using System.Collections.Generic;
using System.IO;

namespace Lucene.Net.Search.Spans
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
    /// Expert: an enumeration of span matches.  Used to implement span searching.
    /// Each span represents a range of term positions within a document.  Matches
    /// are enumerated in order, by increasing document number, within that by
    /// increasing start position and finally by increasing end position.
    /// </summary>
    public abstract class Spans
    {
        /// <summary>
        /// Move to the next match, returning true if any such exists. </summary>
        public abstract bool MoveNext();

        /// <summary>
        /// Move to the next match, returning true if any such exists. </summary>
        [Obsolete("Use MoveNext() instead. This method will be removed in 4.8.0 release candidate."), System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public virtual bool Next() => MoveNext();

        /// <summary>
        /// Skips to the first match beyond the current, whose document number is
        /// greater than or equal to <i>target</i>.
        /// <para/>The behavior of this method is <b>undefined</b> when called with
        /// <c> target &lt;= current</c>, or after the iterator has exhausted.
        /// Both cases may result in unpredicted behavior.
        /// <para/>Returns <c>true</c> if there is such
        /// a match.  
        /// <para/>Behaves as if written: 
        /// <code>
        ///     bool SkipTo(int target) 
        ///     {
        ///         do 
        ///         {
        ///             if (!Next())
        ///                 return false;
        ///         } while (target > Doc);
        ///         return true;
        ///     }
        /// </code>
        /// Most implementations are considerably more efficient than that.
        /// </summary>
        public abstract bool SkipTo(int target);

        /// <summary>
        /// Returns the document number of the current match.  Initially invalid. </summary>
        public abstract int Doc { get; }

        /// <summary>
        /// Returns the start position of the current match.  Initially invalid. </summary>
        public abstract int Start { get; }

        /// <summary>
        /// Returns the end position of the current match.  Initially invalid. </summary>
        public abstract int End { get; }

        /// <summary>
        /// Returns the payload data for the current span.
        /// this is invalid until <see cref="MoveNext()"/> is called for
        /// the first time.
        /// This method must not be called more than once after each call
        /// of <see cref="MoveNext()"/>. However, most payloads are loaded lazily,
        /// so if the payload data for the current position is not needed,
        /// this method may not be called at all for performance reasons. An ordered
        /// SpanQuery does not lazy load, so if you have payloads in your index and
        /// you do not want ordered SpanNearQuerys to collect payloads, you can
        /// disable collection with a constructor option.
        /// <para/>
        /// Note that the return type is a collection, thus the ordering should not be relied upon.
        /// <para/>
        /// @lucene.experimental
        /// </summary>
        /// <returns> A <see cref="T:ICollection{byte[]}"/> of byte arrays containing the data of this payload, 
        /// otherwise <c>null</c> if <see cref="IsPayloadAvailable"/> is <c>false</c> </returns>
        /// <exception cref="IOException"> if there is a low-level I/O error </exception>
        // TODO: Remove warning after API has been finalized
        public abstract ICollection<byte[]> GetPayload();

        /// <summary>
        /// Checks if a payload can be loaded at this position.
        /// <para/>
        /// Payloads can only be loaded once per call to
        /// <see cref="MoveNext()"/>.
        /// </summary>
        /// <returns> <c>true</c> if there is a payload available at this position that can be loaded </returns>
        public abstract bool IsPayloadAvailable { get; }

        /// <summary>
        /// Returns the estimated cost of this spans.
        /// <para/>
        /// This is generally an upper bound of the number of documents this iterator
        /// might match, but may be a rough heuristic, hardcoded value, or otherwise
        /// completely inaccurate.
        /// </summary>
        public abstract long GetCost();
    }
}