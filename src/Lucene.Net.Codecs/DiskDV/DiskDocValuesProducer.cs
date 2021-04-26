using Lucene.Net.Codecs.Lucene45;
using Lucene.Net.Diagnostics;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Util.Packed;
using System;

namespace Lucene.Net.Codecs.DiskDV
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
            return new MonotonicBlockPackedReader((IndexInput)data.Clone(), bytes.PackedInt32sVersion, bytes.BlockSize, bytes.Count,
                true);
        }

        protected override MonotonicBlockPackedReader GetIntervalInstance(IndexInput data, FieldInfo field,
            BinaryEntry bytes)
        {
            throw AssertionError.Create();
        }

        protected override MonotonicBlockPackedReader GetOrdIndexInstance(IndexInput data, FieldInfo field,
            NumericEntry entry)
        {
            data.Seek(entry.Offset);
            return new MonotonicBlockPackedReader((IndexInput)data.Clone(), entry.PackedInt32sVersion, entry.BlockSize, entry.Count,
                true);
        }
    }
}