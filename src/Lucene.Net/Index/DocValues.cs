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

    using IBits = Lucene.Net.Util.IBits;
    using BytesRef = Lucene.Net.Util.BytesRef;

    /// <summary>
    /// This class contains utility methods and constants for <see cref="DocValues"/>
    /// </summary>
    public sealed class DocValues
    {
        /* no instantiation */

        private DocValues()
        {
        }

        /// <summary>
        /// An empty <see cref="BinaryDocValues"/> which returns <see cref="BytesRef.EMPTY_BYTES"/> for every document
        /// </summary>
        public static readonly BinaryDocValues EMPTY_BINARY = new BinaryDocValuesAnonymousInnerClassHelper();

        private class BinaryDocValuesAnonymousInnerClassHelper : BinaryDocValues
        {
            public BinaryDocValuesAnonymousInnerClassHelper()
            {
            }

            public override void Get(int docID, BytesRef result)
            {
                result.Bytes = BytesRef.EMPTY_BYTES;
                result.Offset = 0;
                result.Length = 0;
            }
        }

        /// <summary>
        /// An empty <see cref="NumericDocValues"/> which returns zero for every document
        /// </summary>
        public static readonly NumericDocValues EMPTY_NUMERIC = new NumericDocValuesAnonymousInnerClassHelper();

        private class NumericDocValuesAnonymousInnerClassHelper : NumericDocValues
        {
            public NumericDocValuesAnonymousInnerClassHelper()
            {
            }

            public override long Get(int docID)
            {
                return 0;
            }
        }

        /// <summary>
        /// An empty <see cref="SortedDocValues"/> which returns <see cref="BytesRef.EMPTY_BYTES"/> for every document
        /// </summary>
        public static readonly SortedDocValues EMPTY_SORTED = new SortedDocValuesAnonymousInnerClassHelper();

        private class SortedDocValuesAnonymousInnerClassHelper : SortedDocValues
        {
            public SortedDocValuesAnonymousInnerClassHelper()
            {
            }

            public override int GetOrd(int docID)
            {
                return -1;
            }

            public override void LookupOrd(int ord, BytesRef result)
            {
                result.Bytes = BytesRef.EMPTY_BYTES;
                result.Offset = 0;
                result.Length = 0;
            }

            public override int ValueCount => 0;
        }

        /// <summary>
        /// An empty <see cref="SortedDocValues"/> which returns <see cref="SortedSetDocValues.NO_MORE_ORDS"/> for every document
        /// </summary>
        public static readonly SortedSetDocValues EMPTY_SORTED_SET = new RandomAccessOrdsAnonymousInnerClassHelper();

        private class RandomAccessOrdsAnonymousInnerClassHelper : RandomAccessOrds
        {
            public RandomAccessOrdsAnonymousInnerClassHelper()
            {
            }

            public override long NextOrd()
            {
                return NO_MORE_ORDS;
            }

            public override void SetDocument(int docID)
            {
            }

            public override void LookupOrd(long ord, BytesRef result)
            {
                throw new System.IndexOutOfRangeException();
            }

            public override long ValueCount => 0;

            public override long OrdAt(int index)
            {
                throw new System.IndexOutOfRangeException();
            }

            public override int Cardinality()
            {
                return 0;
            }
        }

        /// <summary>
        /// Returns a multi-valued view over the provided <see cref="SortedDocValues"/>
        /// </summary>
        public static SortedSetDocValues Singleton(SortedDocValues dv)
        {
            return new SingletonSortedSetDocValues(dv);
        }

        /// <summary>
        /// Returns a single-valued view of the <see cref="SortedSetDocValues"/>, if it was previously
        /// wrapped with <see cref="Singleton"/>, or <c>null</c>.
        /// </summary>
        public static SortedDocValues UnwrapSingleton(SortedSetDocValues dv)
        {
            if (dv is SingletonSortedSetDocValues)
            {
                return ((SingletonSortedSetDocValues)dv).SortedDocValues;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Returns a <see cref="IBits"/> representing all documents from <paramref name="dv"/> that have a value.
        /// </summary>
        public static IBits DocsWithValue(SortedDocValues dv, int maxDoc)
        {
            return new BitsAnonymousInnerClassHelper(dv, maxDoc);
        }

        private class BitsAnonymousInnerClassHelper : IBits
        {
            private Lucene.Net.Index.SortedDocValues dv;
            private int maxDoc;

            public BitsAnonymousInnerClassHelper(Lucene.Net.Index.SortedDocValues dv, int maxDoc)
            {
                this.dv = dv;
                this.maxDoc = maxDoc;
            }

            public virtual bool Get(int index)
            {
                return dv.GetOrd(index) >= 0;
            }

            public virtual int Length => maxDoc;
        }

        /// <summary>
        /// Returns a <see cref="IBits"/> representing all documents from <paramref name="dv"/> that have a value.
        /// </summary>
        public static IBits DocsWithValue(SortedSetDocValues dv, int maxDoc)
        {
            return new BitsAnonymousInnerClassHelper2(dv, maxDoc);
        }

        private class BitsAnonymousInnerClassHelper2 : IBits
        {
            private Lucene.Net.Index.SortedSetDocValues dv;
            private int maxDoc;

            public BitsAnonymousInnerClassHelper2(Lucene.Net.Index.SortedSetDocValues dv, int maxDoc)
            {
                this.dv = dv;
                this.maxDoc = maxDoc;
            }

            public virtual bool Get(int index)
            {
                dv.SetDocument(index);
                return dv.NextOrd() != SortedSetDocValues.NO_MORE_ORDS;
            }

            public virtual int Length => maxDoc;
        }
    }
}