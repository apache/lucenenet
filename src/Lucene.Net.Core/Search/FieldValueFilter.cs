using Lucene.Net.Index;

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
    /// A <seealso cref="Filter"/> that accepts all documents that have one or more values in a
    /// given field. this <seealso cref="Filter"/> request <seealso cref="IBits"/> from the
    /// <seealso cref="IFieldCache"/> and build the bits if not present.
    /// </summary>
    public class FieldValueFilter : Filter
    {
        private readonly string field;
        private readonly bool negate;

        /// <summary>
        /// Creates a new <seealso cref="FieldValueFilter"/>
        /// </summary>
        /// <param name="field">
        ///          the field to filter </param>
        public FieldValueFilter(string field)
            : this(field, false)
        {
        }

        /// <summary>
        /// Creates a new <seealso cref="FieldValueFilter"/>
        /// </summary>
        /// <param name="field">
        ///          the field to filter </param>
        /// <param name="negate">
        ///          iff <code>true</code> all documents with no value in the given
        ///          field are accepted.
        ///  </param>
        public FieldValueFilter(string field, bool negate)
        {
            this.field = field;
            this.negate = negate;
        }

        /// <summary>
        /// Returns the field this filter is applied on. </summary>
        /// <returns> the field this filter is applied on. </returns>
        public virtual string Field
        {
            get { return field; }
        }

        /// <summary>
        /// Returns <code>true</code> iff this filter is negated, otherwise <code>false</code> </summary>
        /// <returns> <code>true</code> iff this filter is negated, otherwise <code>false</code> </returns>
        public virtual bool Negate
        {
            get { return negate; }
        }

        public override DocIdSet GetDocIdSet(AtomicReaderContext context, IBits acceptDocs)
        {
            IBits docsWithField = FieldCache.DEFAULT.GetDocsWithField(((AtomicReader)context.Reader), field);
            if (negate)
            {
                if (docsWithField is MatchAllBits)
                {
                    return null;
                }
                return new FieldCacheDocIdSetAnonymousInnerClassHelper(this, context.AtomicReader.MaxDoc, acceptDocs, docsWithField);
            }
            else
            {
                if (docsWithField is MatchNoBits)
                {
                    return null;
                }
                if (docsWithField is DocIdSet)
                {
                    // UweSays: this is always the case for our current impl - but who knows
                    // :-)
                    return BitsFilteredDocIdSet.Wrap((DocIdSet)docsWithField, acceptDocs);
                }
                return new FieldCacheDocIdSetAnonymousInnerClassHelper2(this, context.AtomicReader.MaxDoc, acceptDocs, docsWithField);
            }
        }

        private class FieldCacheDocIdSetAnonymousInnerClassHelper : FieldCacheDocIdSet
        {
            private readonly FieldValueFilter outerInstance;

            private IBits docsWithField;

            public FieldCacheDocIdSetAnonymousInnerClassHelper(FieldValueFilter outerInstance, int maxDoc, IBits acceptDocs, IBits docsWithField)
                : base(maxDoc, acceptDocs)
            {
                this.outerInstance = outerInstance;
                this.docsWithField = docsWithField;
            }

            protected internal override sealed bool MatchDoc(int doc)
            {
                return !docsWithField.Get(doc);
            }
        }

        private class FieldCacheDocIdSetAnonymousInnerClassHelper2 : FieldCacheDocIdSet
        {
            private readonly FieldValueFilter outerInstance;

            private readonly IBits docsWithField;

            public FieldCacheDocIdSetAnonymousInnerClassHelper2(FieldValueFilter outerInstance, int maxDoc, IBits acceptDocs, IBits docsWithField)
                : base(maxDoc, acceptDocs)
            {
                this.outerInstance = outerInstance;
                this.docsWithField = docsWithField;
            }

            protected internal override sealed bool MatchDoc(int doc)
            {
                return docsWithField.Get(doc);
            }
        }

        public override int GetHashCode()
        {
            const int prime = 31;
            int result = 1;
            result = prime * result + ((field == null) ? 0 : field.GetHashCode());
            result = prime * result + (negate ? 1231 : 1237);
            return result;
        }

        public override bool Equals(object obj)
        {
            if (this == obj)
            {
                return true;
            }
            if (obj == null)
            {
                return false;
            }
            if (this.GetType() != obj.GetType())
            {
                return false;
            }
            FieldValueFilter other = (FieldValueFilter)obj;
            if (field == null)
            {
                if (other.field != null)
                {
                    return false;
                }
            }
            else if (!field.Equals(other.field))
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