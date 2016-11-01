using Lucene.Net.Util;
using System.Collections.Generic;
using System.Text;

namespace Lucene.Net.Index
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

    using Bits = Lucene.Net.Util.Bits;

    //using FilterIterator = Lucene.Net.Util.FilterIterator;

    /// <summary>
    /// A <seealso cref="FilterAtomicReader"/> that exposes only a subset
    /// of fields from the underlying wrapped reader.
    /// </summary>
    public sealed class FieldFilterAtomicReader : FilterAtomicReader
    {
        private readonly ISet<string> Fields_Renamed;
        private readonly bool Negate;
        private readonly FieldInfos FieldInfos_Renamed;

        public FieldFilterAtomicReader(AtomicReader @in, ISet<string> fields, bool negate)
            : base(@in)
        {
            this.Fields_Renamed = fields;
            this.Negate = negate;
            List<FieldInfo> filteredInfos = new List<FieldInfo>();
            foreach (FieldInfo fi in @in.FieldInfos)
            {
                if (HasField(fi.Name))
                {
                    filteredInfos.Add(fi);
                }
            }
            FieldInfos_Renamed = new FieldInfos(filteredInfos.ToArray());
        }

        internal bool HasField(string field)
        {
            return Negate ^ Fields_Renamed.Contains(field);
        }

        public override FieldInfos FieldInfos
        {
            get
            {
                return FieldInfos_Renamed;
            }
        }

        public override Fields GetTermVectors(int docID)
        {
            Fields f = base.GetTermVectors(docID);
            if (f == null)
            {
                return null;
            }
            f = new FieldFilterFields(this, f);
            // we need to check for emptyness, so we can return
            // null:
            return f.GetEnumerator().MoveNext() ? f : null;
        }

        public override void Document(int docID, StoredFieldVisitor visitor)
        {
            base.Document(docID, new StoredFieldVisitorAnonymousInnerClassHelper(this, visitor));
        }

        private class StoredFieldVisitorAnonymousInnerClassHelper : StoredFieldVisitor
        {
            private readonly FieldFilterAtomicReader OuterInstance;

            private readonly StoredFieldVisitor Visitor;

            public StoredFieldVisitorAnonymousInnerClassHelper(FieldFilterAtomicReader outerInstance, StoredFieldVisitor visitor)
            {
                this.OuterInstance = outerInstance;
                this.Visitor = visitor;
            }

            public override void BinaryField(FieldInfo fieldInfo, byte[] value)
            {
                Visitor.BinaryField(fieldInfo, value);
            }

            public override void StringField(FieldInfo fieldInfo, string value)
            {
                Visitor.StringField(fieldInfo, value);
            }

            public override void IntField(FieldInfo fieldInfo, int value)
            {
                Visitor.IntField(fieldInfo, value);
            }

            public override void LongField(FieldInfo fieldInfo, long value)
            {
                Visitor.LongField(fieldInfo, value);
            }

            public override void FloatField(FieldInfo fieldInfo, float value)
            {
                Visitor.FloatField(fieldInfo, value);
            }

            public override void DoubleField(FieldInfo fieldInfo, double value)
            {
                Visitor.DoubleField(fieldInfo, value);
            }

            public override Status NeedsField(FieldInfo fieldInfo)
            {
                return OuterInstance.HasField(fieldInfo.Name) ? Visitor.NeedsField(fieldInfo) : Status.NO;
            }
        }

        public override Fields Fields
        {
            get
            {
                Fields f = base.Fields;
                return (f == null) ? null : new FieldFilterFields(this, f);
            }
        }

        public override NumericDocValues GetNumericDocValues(string field)
        {
            return HasField(field) ? base.GetNumericDocValues(field) : null;
        }

        public override BinaryDocValues GetBinaryDocValues(string field)
        {
            return HasField(field) ? base.GetBinaryDocValues(field) : null;
        }

        public override SortedDocValues GetSortedDocValues(string field)
        {
            return HasField(field) ? base.GetSortedDocValues(field) : null;
        }

        public override NumericDocValues GetNormValues(string field)
        {
            return HasField(field) ? base.GetNormValues(field) : null;
        }

        public override Bits GetDocsWithField(string field)
        {
            return HasField(field) ? base.GetDocsWithField(field) : null;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder("FieldFilterAtomicReader(reader=");
            sb.Append(@in).Append(", fields=");
            if (Negate)
            {
                sb.Append('!');
            }
            return sb.Append(Fields_Renamed).Append(')').ToString();
        }

        private class FieldFilterFields : FilterFields
        {
            private readonly FieldFilterAtomicReader OuterInstance;

            public FieldFilterFields(FieldFilterAtomicReader outerInstance, Fields @in)
                : base(@in)
            {
                this.OuterInstance = outerInstance;
            }

            public override int Size
            {
                get
                {
                    // this information is not cheap, return -1 like MultiFields does:
                    return -1;
                }
            }

            public override IEnumerator<string> GetEnumerator()
            {
                return new FilterIteratorAnonymousInnerClassHelper(this, base.GetEnumerator());
            }

            private class FilterIteratorAnonymousInnerClassHelper : FilterIterator<string>
            {
                private readonly FieldFilterFields OuterInstance;

                public FilterIteratorAnonymousInnerClassHelper(FieldFilterFields outerInstance, IEnumerator<string> iterator)
                    : base(iterator)
                {
                    this.OuterInstance = outerInstance;
                }

                protected internal override bool PredicateFunction(string field)
                {
                    return OuterInstance.OuterInstance.HasField(field);
                }
            }

            public override Terms Terms(string field)
            {
                return OuterInstance.HasField(field) ? base.Terms(field) : null;
            }
        }
    }
}