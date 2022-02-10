using Lucene.Net.Index;
using Lucene.Net.Util;
using System.IO;

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
    /// Concrete implementation of <see cref="AbstractFirstPassGroupingCollector{BytesRef}"/> that groups based on
    /// field values and more specifically uses <see cref="SortedDocValues"/>
    /// to collect groups.
    /// 
    /// @lucene.experimental
    /// </summary>
    public class TermFirstPassGroupingCollector : AbstractFirstPassGroupingCollector<BytesRef>
    {
        private readonly BytesRef scratchBytesRef = new BytesRef();
        private SortedDocValues index;

        private readonly string groupField; // LUCENENET: marked readonly

        /// <summary>
        /// Create the first pass collector.
        /// </summary>
        /// <param name="groupField">
        /// The field used to group
        /// documents. This field must be single-valued and
        /// indexed (<see cref="FieldCache"/> is used to access its value
        /// per-document).
        /// </param>
        /// <param name="groupSort">
        /// The <see cref="Sort"/> used to sort the
        /// groups.  The top sorted document within each group
        /// according to groupSort, determines how that group
        /// sorts against other groups.  This must be non-null,
        /// ie, if you want to groupSort by relevance use
        /// <see cref="Sort.RELEVANCE"/>.
        /// </param>
        /// <param name="topNGroups">
        /// How many top groups to keep.
        /// </param>
        /// <exception cref="IOException">When I/O related errors occur</exception>
        public TermFirstPassGroupingCollector(string groupField, Sort groupSort, int topNGroups)
            : base(groupSort, topNGroups)
        {
            this.groupField = groupField;
        }

        protected override BytesRef GetDocGroupValue(int doc)
        {
            int ord = index.GetOrd(doc);
            if (ord == -1)
            {
                return null;
            }
            else
            {
                index.LookupOrd(ord, scratchBytesRef);
                return scratchBytesRef;
            }
        }

        protected override BytesRef CopyDocGroupValue(BytesRef groupValue, BytesRef reuse)
        {
            if (groupValue is null)
            {
                return null;
            }
            else if (reuse != null)
            {
                reuse.CopyBytes(groupValue);
                return reuse;
            }
            else
            {
                return BytesRef.DeepCopyOf(groupValue);
            }
        }

        public override void SetNextReader(AtomicReaderContext context)
        {
            base.SetNextReader(context);
            index = FieldCache.DEFAULT.GetTermsIndex(context.AtomicReader, groupField);
        }
    }
}
