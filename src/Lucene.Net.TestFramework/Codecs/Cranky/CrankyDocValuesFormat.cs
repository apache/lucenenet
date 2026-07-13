using Lucene.Net.Index;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.IO;

namespace Lucene.Net.Codecs.Cranky
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

    internal class CrankyDocValuesFormat : DocValuesFormat
    {
        private readonly DocValuesFormat @delegate;
        private readonly Random random;

        internal CrankyDocValuesFormat(DocValuesFormat @delegate, Random random)
            // we impersonate the passed-in codec, so we don't need to be in SPI,
            // and so we don't change file formats
            : base(@delegate.Name)
        {
            this.@delegate = @delegate;
            this.random = random;
        }

        public override DocValuesConsumer FieldsConsumer(SegmentWriteState state)
        {
            if (random.Next(100) == 0)
            {
                throw new IOException("Fake IOException from DocValuesFormat.FieldsConsumer()");
            }

            return new CrankyDocValuesConsumer(@delegate.FieldsConsumer(state), random);
        }

        public override DocValuesProducer FieldsProducer(SegmentReadState state) => @delegate.FieldsProducer(state);

        internal class CrankyDocValuesConsumer : DocValuesConsumer
        {
            private readonly DocValuesConsumer @delegate;
            private readonly Random random;

            public CrankyDocValuesConsumer(DocValuesConsumer @delegate, Random random)
            {
                this.@delegate = @delegate;
                this.random = random;
            }

            protected override void Dispose(bool disposing)
            {
                @delegate.Dispose();
                if (random.Next(100) == 0)
                {
                    throw new IOException("Fake IOException from DocValuesConsumer.Dispose()");
                }
            }

            public override void AddNumericField(FieldInfo field, IEnumerable<long?> values)
            {
                if (random.Next(100) == 0)
                {
                    throw new IOException("Fake IOException from DocValuesConsumer.AddNumericField()");
                }

                @delegate.AddNumericField(field, values);
            }

            public override void AddBinaryField(FieldInfo field, IEnumerable<BytesRef> values)
            {
                if (random.Next(100) == 0)
                {
                    throw new IOException("Fake IOException from DocValuesConsumer.AddBinaryField()");
                }

                @delegate.AddBinaryField(field, values);
            }

            public override void AddSortedField(FieldInfo field, IEnumerable<BytesRef> values,
                IEnumerable<long?> docToOrd)
            {
                if (random.Next(100) == 0)
                {
                    throw new IOException("Fake IOException from DocValuesConsumer.AddSortedField()");
                }

                @delegate.AddSortedField(field, values, docToOrd);
            }

            public override void AddSortedSetField(FieldInfo field, IEnumerable<BytesRef> values,
                IEnumerable<long?> docToOrdCount, IEnumerable<long?> ords)
            {
                if (random.Next(100) == 0)
                {
                    throw new IOException("Fake IOException from DocValuesConsumer.AddSortedSetField()");
                }

                @delegate.AddSortedSetField(field, values, docToOrdCount, ords);
            }
        }
    }
}
