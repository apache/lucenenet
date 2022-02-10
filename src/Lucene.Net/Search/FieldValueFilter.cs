using Lucene.Net.Index;
using System;

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

    using AtomicReaderContext = Lucene.Net.Index.AtomicReaderContext;
    using IBits = Lucene.Net.Util.IBits;
    using MatchAllBits = Lucene.Net.Util.Bits.MatchAllBits;
    using MatchNoBits = Lucene.Net.Util.Bits.MatchNoBits;

    /// <summary>
    /// A <see cref="Filter"/> that accepts all documents that have one or more values in a
    /// given field. this <see cref="Filter"/> request <see cref="IBits"/> from the
    /// <see cref="IFieldCache"/> and build the bits if not present.
    /// </summary>
    public class FieldValueFilter : Filter
    {
        private readonly string field;
        private readonly bool negate;

        /// <summary>
        /// Creates a new <see cref="FieldValueFilter"/>
        /// </summary>
        /// <param name="field">
        ///          The field to filter </param>
        public FieldValueFilter(string field)
            : this(field, false)
        {
        }

        /// <summary>
        /// Creates a new <see cref="FieldValueFilter"/>
        /// </summary>
        /// <param name="field">
        ///          The field to filter </param>
        /// <param name="negate">
        ///          If <c>true</c> all documents with no value in the given
        ///          field are accepted.
        ///  </param>
        public FieldValueFilter(string field, bool negate)
        {
            this.field = field;
            this.negate = negate;
        }

        /// <summary>
        /// Returns the field this filter is applied on. </summary>
        /// <returns> The field this filter is applied on. </returns>
        public virtual string Field => field;

        /// <summary>
        /// Returns <c>true</c> if this filter is negated, otherwise <c>false</c> </summary>
        /// <returns> <c>true</c> if this filter is negated, otherwise <c>false</c> </returns>
        public virtual bool Negate => negate;

        public override DocIdSet GetDocIdSet(AtomicReaderContext context, IBits acceptDocs)
        {
            IBits docsWithField = FieldCache.DEFAULT.GetDocsWithField(((AtomicReader)context.Reader), field);
            if (negate)
            {
                if (docsWithField is MatchAllBits)
                {
                    return null;
                }
                return new FieldCacheDocIdSet(context.AtomicReader.MaxDoc, acceptDocs, (doc) => !docsWithField.Get(doc));
            }
            else
            {
                if (docsWithField is MatchNoBits)
                {
                    return null;
                }
                if (docsWithField is DocIdSet docIdSetWithField)
                {
                    // UweSays: this is always the case for our current impl - but who knows
                    // :-)
                    return BitsFilteredDocIdSet.Wrap(docIdSetWithField, acceptDocs);
                }
                return new FieldCacheDocIdSet(context.AtomicReader.MaxDoc, acceptDocs, (doc) => docsWithField.Get(doc));
            }
        }

        public override int GetHashCode()
        {
            const int prime = 31;
            int result = 1;
            result = prime * result + ((field is null) ? 0 : field.GetHashCode());
            result = prime * result + (negate ? 1231 : 1237);
            return result;
        }

        public override bool Equals(object obj)
        {
            if (this == obj)
            {
                return true;
            }
            if (obj is null)
            {
                return false;
            }
            if (this.GetType() != obj.GetType())
            {
                return false;
            }
            FieldValueFilter other = (FieldValueFilter)obj;
            if (field is null)
            {
                if (other.field != null)
                {
                    return false;
                }
            }
            else if (!field.Equals(other.field, StringComparison.Ordinal))
            {
                return false;
            }
            if (negate != other.negate)
            {
                return false;
            }
            return true;
        }

        public override string ToString()
        {
            return "FieldValueFilter [field=" + field + ", negate=" + negate + "]";
        }
    }
}