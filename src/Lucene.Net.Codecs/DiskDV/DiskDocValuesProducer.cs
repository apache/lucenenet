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

namespace Lucene.Net.Codecs.DiskDV
{
    using System;
    using Lucene45;
    using Index;
    using Store;
    using Util.Packed;

    internal class DiskDocValuesProducer : Lucene45DocValuesProducer
    {
        internal DiskDocValuesProducer(SegmentReadState state, string dataCodec, string dataExtension, string metaCodec,
            string metaExtension) 
            : base(state, dataCodec, dataExtension, metaCodec, metaExtension)
        {
        }

        protected override MonotonicBlockPackedReader GetAddressInstance(IndexInput data, FieldInfo field,
            BinaryEntry bytes)
        {
            data.Seek(bytes.AddressesOffset);
            return new MonotonicBlockPackedReader((IndexInput)data.Clone(), bytes.PackedIntsVersion, bytes.BlockSize, bytes.Count,
                true);
        }

        protected override MonotonicBlockPackedReader GetIntervalInstance(IndexInput data, FieldInfo field,
            BinaryEntry bytes)
        {
            throw new InvalidOperationException(); // LUCENENET NOTE: This was AssertionError in Lucene
        }

        protected override MonotonicBlockPackedReader GetOrdIndexInstance(IndexInput data, FieldInfo field,
            NumericEntry entry)
        {
            data.Seek(entry.Offset);
            return new MonotonicBlockPackedReader((IndexInput)data.Clone(), entry.PackedIntsVersion, entry.BlockSize, entry.Count,
                true);
        }
    }
}