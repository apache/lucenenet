using Lucene.Net.Util;
using System.Collections.Generic;
using System.Linq;
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

    /// <summary>
    /// A <see cref="FilterAtomicReader"/> that exposes only a subset
    /// of fields from the underlying wrapped reader.
    /// </summary>
    public sealed class FieldFilterAtomicReader : FilterAtomicReader
    {
        private readonly ISet<string> fields;
        private readonly bool negate;
        private readonly FieldInfos fieldInfos;

        public FieldFilterAtomicReader(AtomicReader @in, ISet<string> fields, bool negate)
            : base(@in)
        {
            this.fields = fields;
            this.negate = negate;
            List<FieldInfo> filteredInfos = new List<FieldInfo>();
            foreach (FieldInfo fi in @in.FieldInfos)
            {
                if (HasField(fi.Name))
                {
                    filteredInfos.Add(fi);
                }
            }
            fieldInfos = new FieldInfos(filteredInfos.ToArray());
        }

        internal bool HasField(string field)
        {
            return negate ^ fields.Contains(field);
        }

        public override FieldInfos FieldInfos => fieldInfos;

        public override Fields GetTermVectors(int docID)
        {
            Fields f = base.GetTermVectors(docID);
            if (f is null)
            {
                return null;
            }
            f = new FieldFilterFields(this, f);
            // we need to check for emptyness, so we can return
            // null:
            using var iter = f.GetEnumerator();
            return iter.MoveNext() ? f : null;
        }

        public override void Document(int docID, StoredFieldVisitor visitor)
        {
            base.Document(docID, new StoredFieldVisitorAnonymousClass(this, visitor));
        }

        private sealed class StoredFieldVisitorAnonymousClass : StoredFieldVisitor
        {
            private readonly FieldFilterAtomicReader outerInstance;

            private readonly StoredFieldVisitor visitor;

            public StoredFieldVisitorAnonymousClass(FieldFilterAtomicReader outerInstance, StoredFieldVisitor visitor)
            {
                this.outerInstance = outerInstance;
                this.visitor = visitor;
            }

            public override void BinaryField(FieldInfo fieldInfo, byte[] value)
            {
                visitor.BinaryField(fieldInfo, value);
            }

            public override void StringField(FieldInfo fieldInfo, string value)
            {
                visitor.StringField(fieldInfo, value);
            }

            public override void Int32Field(FieldInfo fieldInfo, int value)
            {
                visitor.Int32Field(fieldInfo, value);
            }

            public override void Int64Field(FieldInfo fieldInfo, long value)
            {
                visitor.Int64Field(fieldInfo, value);
            }

            public override void SingleField(FieldInfo fieldInfo, float value)
            {
                visitor.SingleField(fieldInfo, value);
            }

            public override void DoubleField(FieldInfo fieldInfo, double value)
            {
                visitor.DoubleField(fieldInfo, value);
            }

            public override Status NeedsField(FieldInfo fieldInfo)
            {
                return outerInstance.HasField(fieldInfo.Name) ? visitor.NeedsField(fieldInfo) : Status.NO;
            }
        }

        public override Fields Fields
        {
            get
            {
                Fields f = base.Fields;
                return (f is null) ? null : new FieldFilterFields(this, f);
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

        public override IBits GetDocsWithField(string field)
        {
            return HasField(field) ? base.GetDocsWithField(field) : null;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder("FieldFilterAtomicReader(reader=");
            sb.Append(m_input).Append(", fields=");
            if (negate)
            {
                sb.Append('!');
            }
            return sb.Append(fields).Append(')').ToString();
        }

        private class FieldFilterFields : FilterFields
        {
            private readonly FieldFilterAtomicReader outerInstance;

            public FieldFilterFields(FieldFilterAtomicReader outerInstance, Fields @in)
                : base(@in)
            {
                this.outerInstance = outerInstance;
            }

            // this information is not cheap, return -1 like MultiFields does:
            public override int Count => -1;

            public override IEnumerator<string> GetEnumerator()
            {
                // LUCENENET: Performance is better and code simpler with simple where clause
                // and yield return.
                foreach (var field in m_input.Where((f) => outerInstance.HasField(f)))
                    yield return field;
            }

            public override Terms GetTerms(string field)
            {
                return outerInstance.HasField(field) ? base.GetTerms(field) : null;
            }
        }
    }
}