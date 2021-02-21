// Lucene version compatibility level 4.8.1
using System;

namespace Lucene.Net.Facet.Taxonomy.WriterCache
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
    /// Abstract class for storing Label->Ordinal mappings in a taxonomy. 
    /// <para/>
    /// @lucene.experimental
    /// </summary>
    public abstract class LabelToOrdinal
    {
        /// <summary>
        /// How many ordinals we've seen. </summary>
        protected int m_counter;

        /// <summary>
        /// Returned by <see cref="GetOrdinal"/> when the label isn't
        /// recognized. 
        /// </summary>
        public const int INVALID_ORDINAL = -2;

        /// <summary>
        /// Default constructor.
        /// </summary>
        protected LabelToOrdinal() // LUCENENET: CA1012: Abstract types should not have constructors (marked protected)
        {
        }

        /// <summary>
        /// return the maximal Ordinal assigned so far
        /// </summary>
        public virtual int MaxOrdinal => this.m_counter;

        /// <summary>
        /// Returns the next unassigned ordinal. The default behavior of this method
        /// is to simply increment a counter.
        /// </summary>
        public virtual int GetNextOrdinal()
        {
            return this.m_counter++;
        }

        /// <summary>
        /// Adds a new label if its not yet in the table.
        /// Throws an <see cref="ArgumentException"/> if the same label with
        /// a different ordinal was previoulsy added to this table.
        /// </summary>
        public abstract void AddLabel(FacetLabel label, int ordinal);

        /// <summary>
        /// Returns the ordinal assigned to the given label, 
        /// or <see cref="INVALID_ORDINAL"/> if the label cannot be found in this table.
        /// </summary>
        public abstract int GetOrdinal(FacetLabel label);
    }
}