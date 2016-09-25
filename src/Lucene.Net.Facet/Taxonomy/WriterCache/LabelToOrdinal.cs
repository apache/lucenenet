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
    /// 
    /// @lucene.experimental
    /// </summary>
    public abstract class LabelToOrdinal
    {
        /// <summary>
        /// How many ordinals we've seen. </summary>
        protected internal int counter;

        /// <summary>
        /// Returned by <seealso cref="#getOrdinal"/> when the label isn't
        ///  recognized. 
        /// </summary>
        public const int INVALID_ORDINAL = -2;

        /// <summary>
        /// Default constructor. </summary>
        public LabelToOrdinal()
        {
        }

        /// <summary>
        /// return the maximal Ordinal assigned so far
        /// </summary>
        public virtual int MaxOrdinal
        {
            get
            {
                return this.counter;
            }
        }

        /// <summary>
        /// Returns the next unassigned ordinal. The default behavior of this method
        /// is to simply increment a counter.
        /// </summary>
        public virtual int GetNextOrdinal()
        {
            return this.counter++;
        }

        /// <summary>
        /// Adds a new label if its not yet in the table.
        /// Throws an <seealso cref="IllegalArgumentException"/> if the same label with
        /// a different ordinal was previoulsy added to this table.
        /// </summary>
        public abstract void AddLabel(FacetLabel label, int ordinal);

        /// <summary>
        /// Returns the ordinal assigned to the given label, 
        /// or <seealso cref="#INVALID_ORDINAL"/> if the label cannot be found in this table.
        /// </summary>
        public abstract int GetOrdinal(FacetLabel label);
    }
}