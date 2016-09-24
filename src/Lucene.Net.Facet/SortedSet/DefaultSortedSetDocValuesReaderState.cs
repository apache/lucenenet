using Lucene.Net.Index;
using Lucene.Net.Support;
using Lucene.Net.Util;
using System.Collections.Generic;

namespace Lucene.Net.Facet.SortedSet
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
    /// Default implementation of <seealso cref="SortedSetDocValuesFacetCounts"/>
    /// </summary>
    public class DefaultSortedSetDocValuesReaderState : SortedSetDocValuesReaderState
    {
        private readonly string field;
        private readonly AtomicReader topReader;
        private readonly int valueCount;

        /// <summary>
        /// <seealso cref="IndexReader"/> passed to the constructor. </summary>
        public readonly IndexReader origReader;

        private readonly IDictionary<string, OrdRange> prefixToOrdRange = new Dictionary<string, OrdRange>();

        /// <summary>
        /// Creates this, pulling doc values from the specified
        ///  field. 
        /// </summary>
        public DefaultSortedSetDocValuesReaderState(IndexReader reader, string field = FacetsConfig.DEFAULT_INDEX_FIELD_NAME)
        {
            this.field = field;
            this.origReader = reader;

            // We need this to create thread-safe MultiSortedSetDV
            // per collector:
            topReader = SlowCompositeReaderWrapper.Wrap(reader);
            SortedSetDocValues dv = topReader.GetSortedSetDocValues(field);
            if (dv == null)
            {
                throw new System.ArgumentException("field \"" + field + "\" was not indexed with SortedSetDocValues");
            }
            if (dv.ValueCount > int.MaxValue)
            {
                throw new System.ArgumentException("can only handle valueCount < Integer.MAX_VALUE; got " + dv.ValueCount);
            }
            valueCount = (int)dv.ValueCount;

            // TODO: we can make this more efficient if eg we can be
            // "involved" when OrdinalMap is being created?  Ie see
            // each term/ord it's assigning as it goes...
            string lastDim = null;
            int startOrd = -1;

            // TODO: this approach can work for full hierarchy?;
            // TaxoReader can't do this since ords are not in
            // "sorted order" ... but we should generalize this to
            // support arbitrary hierarchy:
            for (int ord = 0; ord < valueCount; ord++)
            {
                BytesRef term = new BytesRef();
                dv.LookupOrd(ord, term);
                string[] components = FacetsConfig.StringToPath(term.Utf8ToString());
                if (components.Length != 2)
                {
                    throw new System.ArgumentException("this class can only handle 2 level hierarchy (dim/value); got: " + Arrays.ToString(components) + " " + term.Utf8ToString());
                }
                if (!components[0].Equals(lastDim))
                {
                    if (lastDim != null)
                    {
                        prefixToOrdRange[lastDim] = new OrdRange(startOrd, ord - 1);
                    }
                    startOrd = ord;
                    lastDim = components[0];
                }
            }

            if (lastDim != null)
            {
                prefixToOrdRange[lastDim] = new OrdRange(startOrd, valueCount - 1);
            }
        }

        /// <summary>
        /// Return top-level doc values. </summary>
        public override SortedSetDocValues DocValues
        {
            get
            {
                return topReader.GetSortedSetDocValues(field);
            }
        }

        /// <summary>
        /// Returns mapping from prefix to <seealso cref="SortedSetDocValuesReaderState.OrdRange"/>. </summary>
        public override IDictionary<string, OrdRange> PrefixToOrdRange
        {
            get
            {
                return prefixToOrdRange;
            }
        }

        /// <summary>
        /// Returns the <seealso cref="SortedSetDocValuesReaderState.OrdRange"/> for this dimension. </summary>
        public override OrdRange GetOrdRange(string dim)
        {
            return prefixToOrdRange[dim];
        }

        /// <summary>
        /// Indexed field we are reading. </summary>
        public override string Field
        {
            get
            {
                return field;
            }
        }

        public override IndexReader OrigReader
        {
            get
            {
                return origReader;
            }
        }

        /// <summary>
        /// Number of unique labels. </summary>
        public override int Size
        {
            get
            {
                return valueCount;
            }
        }
    }
}