namespace Lucene.Net.Facet.Taxonomy
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

    using ArrayUtil = Lucene.Net.Util.ArrayUtil;
    using AtomicReaderContext = Lucene.Net.Index.AtomicReaderContext;
    using BinaryDocValues = Lucene.Net.Index.BinaryDocValues;
    using BytesRef = Lucene.Net.Util.BytesRef;
    using DocValues = Lucene.Net.Index.DocValues;
    using IntsRef = Lucene.Net.Util.IntsRef;

    /// <summary>
    /// Decodes ordinals previously indexed into a BinaryDocValues field </summary>

    public class DocValuesOrdinalsReader : OrdinalsReader
    {
        private readonly string field;

        /// <summary>
        /// Default constructor. </summary>
        public DocValuesOrdinalsReader()
            : this(FacetsConfig.DEFAULT_INDEX_FIELD_NAME)
        {
        }

        /// <summary>
        /// Create this, with the specified indexed field name. </summary>
        public DocValuesOrdinalsReader(string field)
        {
            this.field = field;
        }

        public override OrdinalsSegmentReader GetReader(AtomicReaderContext context)
        {
            BinaryDocValues values0 = context.AtomicReader.GetBinaryDocValues(field);
            if (values0 == null)
            {
                values0 = DocValues.EMPTY_BINARY;
            }

            BinaryDocValues values = values0;

            return new OrdinalsSegmentReaderAnonymousInnerClassHelper(this, values);
        }

        private class OrdinalsSegmentReaderAnonymousInnerClassHelper : OrdinalsSegmentReader
        {
            private readonly DocValuesOrdinalsReader outerInstance;

            private BinaryDocValues values;

            public OrdinalsSegmentReaderAnonymousInnerClassHelper(DocValuesOrdinalsReader outerInstance, BinaryDocValues values)
            {
                this.outerInstance = outerInstance;
                this.values = values;
            }

            public override void Get(int docID, IntsRef ordinals)
            {
                BytesRef bytes = new BytesRef();
                values.Get(docID, bytes);
                outerInstance.Decode(bytes, ordinals);
            }
        }

        public override string IndexFieldName
        {
            get
            {
                return field;
            }
        }

        /// <summary>
        /// Subclass & override if you change the encoding. </summary>
        protected virtual void Decode(BytesRef buf, IntsRef ordinals)
        {

            // grow the buffer up front, even if by a large number of values (buf.length)
            // that saves the need to check inside the loop for every decoded value if
            // the buffer needs to grow.
            if (ordinals.Ints.Length < buf.Length)
            {
                ordinals.Ints = ArrayUtil.Grow(ordinals.Ints, buf.Length);
            }

            ordinals.Offset = 0;
            ordinals.Length = 0;

            // it is better if the decoding is inlined like so, and not e.g.
            // in a utility method
            int upto = buf.Offset + buf.Length;
            int value = 0;
            int offset = buf.Offset;
            int prev = 0;
            while (offset < upto)
            {
                byte b = buf.Bytes[offset++];
                if ((sbyte)b >= 0)
                {
                    ordinals.Ints[ordinals.Length] = ((value << 7) | b) + prev;
                    value = 0;
                    prev = ordinals.Ints[ordinals.Length];
                    ordinals.Length++;
                }
                else
                {
                    value = (value << 7) | (b & 0x7F);
                }
            }
        }
    }
}