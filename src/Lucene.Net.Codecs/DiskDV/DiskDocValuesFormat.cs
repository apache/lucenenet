using Lucene.Net.Codecs.Lucene45;
using Lucene.Net.Index;
using Lucene.Net.Util;
using System.Collections.Generic;

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

    /// <summary>
    /// DocValues format that keeps most things on disk.
    /// Only things like disk offsets are loaded into ram.
    /// <para/>
    /// @lucene.experimental
    /// </summary>
    [DocValuesFormatName("Disk")] // LUCENENET specific - using DocValuesFormatName attribute to ensure the default name passed from subclasses is the same as this class name
    public sealed class DiskDocValuesFormat : DocValuesFormat
    {
        public DiskDocValuesFormat() 
            : base()
        {
        }

        public override DocValuesConsumer FieldsConsumer(SegmentWriteState state)
        {
            return new Lucene45DocValuesConsumerAnonymousClass(state);
        }

        private sealed class Lucene45DocValuesConsumerAnonymousClass : Lucene45DocValuesConsumer
        {
            public Lucene45DocValuesConsumerAnonymousClass(SegmentWriteState state)
                : base(state, DATA_CODEC, DATA_EXTENSION, META_CODEC, META_EXTENSION)
            {
            }

            protected override void AddTermsDict(FieldInfo field, IEnumerable<BytesRef> values)
            {
                AddBinaryField(field, values);
            }
        }

        public override DocValuesProducer FieldsProducer(SegmentReadState state)
        {
            return new DiskDocValuesProducer(state, DATA_CODEC, DATA_EXTENSION, META_CODEC, META_EXTENSION);
        }

        public static readonly string DATA_CODEC = "DiskDocValuesData";
        public static readonly string DATA_EXTENSION = "dvdd";
        public static readonly string META_CODEC = "DiskDocValuesMetadata";
        public static readonly string META_EXTENSION = "dvdm";
    }
}