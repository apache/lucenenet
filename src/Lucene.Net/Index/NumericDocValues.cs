using Lucene.Net.Search;

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
    /// A per-document numeric value.
    /// </summary>
    public abstract class NumericDocValues : IFieldCacheGetter<byte>,  // LUCENENET specific - Add interfaces per type to reduce previous Func<int,T> allocations on reading from cache
                                             IFieldCacheGetter<short>, 
                                             IFieldCacheGetter<int>,
                                             IFieldCacheGetter<long>, 
                                             IFieldCacheGetter<float>, 
                                             IFieldCacheGetter<double>
    {
        /// <summary>
        /// Sole constructor. (For invocation by subclass
        /// constructors, typically implicit.)
        /// </summary>
        protected NumericDocValues()
        {
        }

        /// <summary>
        /// Returns the numeric value for the specified document ID. </summary>
        /// <param name="docID"> document ID to lookup </param>
        /// <returns> numeric value </returns>
        public abstract long Get(int docID);

        byte IFieldCacheGetter<byte>.GetCached(int docID) // LUCENENET specific - moved read logic from FieldCacheImpl to here
        {
            return (byte)Get(docID);
        }

        short IFieldCacheGetter<short>.GetCached(int docID) // LUCENENET specific - moved read logic from FieldCacheImpl to here
        {
            return (short)Get(docID);
        }

        int IFieldCacheGetter<int>.GetCached(int docID) // LUCENENET specific - moved read logic from FieldCacheImpl to here
        {
            return (int)Get(docID);
        }

        long IFieldCacheGetter<long>.GetCached(int docID) // LUCENENET specific - moved read logic from FieldCacheImpl to here
        {
            return Get(docID);
        }

        float IFieldCacheGetter<float>.GetCached(int docID) // LUCENENET specific - moved read logic from FieldCacheImpl to here
        {
            return J2N.BitConversion.Int32BitsToSingle((int)Get(docID));
        }

        double IFieldCacheGetter<double>.GetCached(int docID) // LUCENENET specific - moved read logic from FieldCacheImpl to here
        {
            return J2N.BitConversion.Int64BitsToDouble(Get(docID));
        }


        private FieldCache.Bytes   _asBytes;
        private FieldCache.Int16s  _asInt16s;
        private FieldCache.Int32s  _asInt32s;
        private FieldCache.Int64s  _asInt64s;
        private FieldCache.Singles _asSingles;
        private FieldCache.Doubles _asDoubles;

        internal FieldCache.Bytes AsBytes() // LUCENENET specific - Avoid allocation of a new FieldCache.Bytes on every Get call
        {
            if (_asBytes is null) //No need to lock, as we don't care if this is replaced
            {
                _asBytes = new FieldCache.Bytes(this);
            }
            return _asBytes;
        }

        internal FieldCache.Int16s AsInt16s() // LUCENENET specific - Avoid allocation of a new FieldCache.Int16s on every Get call
        {
            if (_asInt16s is null) //No need to lock, as we don't care if this is replaced
            {
                _asInt16s = new FieldCache.Int16s(this);
            }
            return _asInt16s;
        }

        internal FieldCache.Int32s AsInt32s() // LUCENENET specific - Avoid allocation of a new FieldCache.Int32s on every Get call
        {
            if (_asInt32s is null) //No need to lock, as we don't care if this is replaced
            {
                _asInt32s = new FieldCache.Int32s(this);
            }
            return _asInt32s;
        }

        internal FieldCache.Int64s AsInt64s() // LUCENENET specific - Avoid allocation of a new FieldCache.Int64s on every Get call
        {
            if (_asInt64s is null) //No need to lock, as we don't care if this is replaced
            {
                _asInt64s = new FieldCache.Int64s(this);
            }
            return _asInt64s;
        }

        internal FieldCache.Singles AsSingles() // LUCENENET specific - Avoid allocation of a new FieldCache.Singles on every Get call
        {
            if (_asSingles is null) //No need to lock, as we don't care if this is replaced
            {
                _asSingles = new FieldCache.Singles(this);
            }
            return _asSingles;
        }

        internal FieldCache.Doubles AsDoubles() // LUCENENET specific - Avoid allocation of a new FieldCache.Doubles on every Get call
        {
            if(_asDoubles is null) //No need to lock, as we don't care if this is replaced
            {
                _asDoubles = new FieldCache.Doubles(this);
            }
            return _asDoubles;
        }
    }
}