namespace Lucene.Net.Search
{
    using Lucene.Net.Index;

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
    using Bits = Lucene.Net.Util.Bits;
    using Bits_MatchAllBits = Lucene.Net.Util.Bits_MatchAllBits;
    using Bits_MatchNoBits = Lucene.Net.Util.Bits_MatchNoBits;

    /// <summary>
    /// A <seealso cref="Filter"/> that accepts all documents that have one or more values in a
    /// given field. this <seealso cref="Filter"/> request <seealso cref="Bits"/> from the
    /// <seealso cref="IFieldCache"/> and build the bits if not present.
    /// </summary>
    public class FieldValueFilter : Filter
    {
        private readonly string Field_Renamed; // LUCENENET TODO: rename (private)
        private readonly bool Negate_Renamed; // LUCENENET TODO: rename (private)

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
            this.Field_Renamed = field;
            this.Negate_Renamed = negate;
        }

        /// <summary>
        /// Returns the field this filter is applied on. </summary>
        /// <returns> the field this filter is applied on. </returns>
        public virtual string Field() // LUCENENET TODO: make property
        {
            return Field_Renamed;
        }

        /// <summary>
        /// Returns <code>true</code> iff this filter is negated, otherwise <code>false</code> </summary>
        /// <returns> <code>true</code> iff this filter is negated, otherwise <code>false</code> </returns>
        public virtual bool Negate() // LUCENENET TODO: make property (confusing)
        {
            return Negate_Renamed;
        }

        public override DocIdSet GetDocIdSet(AtomicReaderContext context, Bits acceptDocs)
        {
            Bits docsWithField = FieldCache.DEFAULT.GetDocsWithField(((AtomicReader)context.Reader), Field_Renamed);
            if (Negate_Renamed)
            {
                if (docsWithField is Bits_MatchAllBits)
                {
                    return null;
                }
                return new FieldCacheDocIdSetAnonymousInnerClassHelper(this, context.AtomicReader.MaxDoc, acceptDocs, docsWithField);
            }
            else
            {
                if (docsWithField is Bits_MatchNoBits)
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
            private readonly FieldValueFilter OuterInstance; // LUCENENET TODO: rename (private)

            private Bits DocsWithField; // LUCENENET TODO: rename (private)

            public FieldCacheDocIdSetAnonymousInnerClassHelper(FieldValueFilter outerInstance, int maxDoc, Bits acceptDocs, Bits docsWithField)
                : base(maxDoc, acceptDocs)
            {
                this.OuterInstance = outerInstance;
                this.DocsWithField = docsWithField;
            }

            protected internal override sealed bool MatchDoc(int doc)
            {
                return !DocsWithField.Get(doc);
            }
        }

        private class FieldCacheDocIdSetAnonymousInnerClassHelper2 : FieldCacheDocIdSet
        {
            private readonly FieldValueFilter OuterInstance; // LUCENENET TODO: rename (private)

            private readonly Bits DocsWithField; // LUCENENET TODO: rename (private)

            public FieldCacheDocIdSetAnonymousInnerClassHelper2(FieldValueFilter outerInstance, int maxDoc, Bits acceptDocs, Bits docsWithField)
                : base(maxDoc, acceptDocs)
            {
                this.OuterInstance = outerInstance;
                this.DocsWithField = docsWithField;
            }

            protected internal override sealed bool MatchDoc(int doc)
            {
                return DocsWithField.Get(doc);
            }
        }

        public override int GetHashCode()
        {
            const int prime = 31;
            int result = 1;
            result = prime * result + ((Field_Renamed == null) ? 0 : Field_Renamed.GetHashCode());
            result = prime * result + (Negate_Renamed ? 1231 : 1237);
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
            if (Field_Renamed == null)
            {
                if (other.Field_Renamed != null)
                {
                    return false;
                }
            }
            else if (!Field_Renamed.Equals(other.Field_Renamed))
            {
                return false;
            }
            if (Negate_Renamed != other.Negate_Renamed)
            {
                return false;
            }
            return true;
        }

        public override string ToString()
        {
            return "FieldValueFilter [field=" + Field_Renamed + ", negate=" + Negate_Renamed + "]";
        }
    }
}