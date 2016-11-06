using Lucene.Net.Index;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;

namespace Lucene.Net.Search.Grouping.Terms
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
    /// A collector that collects all groups that match the
    /// query. Only the group value is collected, and the order
    /// is undefined.  This collector does not determine
    /// the most relevant document of a group.
    /// 
    /// <para>
    /// Implementation detail: an int hash set (SentinelIntSet)
    /// is used to detect if a group is already added to the
    /// total count.  For each segment the int set is cleared and filled
    /// with previous counted groups that occur in the new
    /// segment.
    /// </para>
    /// @lucene.experimental
    /// </summary>
    public class TermAllGroupsCollector : AbstractAllGroupsCollector<BytesRef>
    {
        private static readonly int DEFAULT_INITIAL_SIZE = 128;

        private readonly String groupField;
        private readonly SentinelIntSet ordSet;
        private readonly IList<BytesRef> groups;

        private SortedDocValues index;

        /// <summary>
        /// Expert: Constructs a <see cref="AbstractAllGroupsCollector{BytesRef}"/>
        /// </summary>
        /// <param name="groupField">The field to group by</param>
        /// <param name="initialSize">
        /// The initial allocation size of the
        /// internal int set and group list
        /// which should roughly match the total
        /// number of expected unique groups. Be aware that the
        /// heap usage is 4 bytes * initialSize.
        /// </param>
        public TermAllGroupsCollector(string groupField, int initialSize)
        {
            ordSet = new SentinelIntSet(initialSize, -2);
            groups = new List<BytesRef>(initialSize);
            this.groupField = groupField;
        }

        /// <summary>
        /// Constructs a <see cref="AbstractAllGroupsCollector{BytesRef}"/>. This sets the
        /// initial allocation size for the internal int set and group
        /// list to 128.
        /// </summary>
        /// <param name="groupField">The field to group by</param>
        public TermAllGroupsCollector(string groupField)
            : this(groupField, DEFAULT_INITIAL_SIZE)
        {
        }

        public override void Collect(int doc)
        {
            int key = index.GetOrd(doc);
            if (!ordSet.Exists(key))
            {
                ordSet.Put(key);
                BytesRef term;
                if (key == -1)
                {
                    term = null;
                }
                else
                {
                    term = new BytesRef();
                    index.LookupOrd(key, term);
                }
                groups.Add(term);
            }
        }

        public override IEnumerable<BytesRef> Groups
        {
            get
            {
                return groups;
            }
        }

        public override AtomicReaderContext NextReader
        {
            set
            {
                index = FieldCache.DEFAULT.GetTermsIndex(value.AtomicReader, groupField);

                // Clear ordSet and fill it with previous encountered groups that can occur in the current segment.
                ordSet.Clear();
                foreach (BytesRef countedGroup in groups)
                {
                    if (countedGroup == null)
                    {
                        ordSet.Put(-1);
                    }
                    else
                    {
                        int ord = index.LookupTerm(countedGroup);
                        if (ord >= 0)
                        {
                            ordSet.Put(ord);
                        }
                    }
                }
            }
        }
    }
}
