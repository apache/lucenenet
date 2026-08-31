using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.IO;
using Directory = Lucene.Net.Store.Directory;

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

    internal class CrankyStoredFieldsFormat : StoredFieldsFormat
    {
        private readonly StoredFieldsFormat @delegate;
        private readonly Random random;

        internal CrankyStoredFieldsFormat(StoredFieldsFormat @delegate, Random random)
        {
            this.@delegate = @delegate;
            this.random = random;
        }

        public override StoredFieldsReader FieldsReader(Directory directory, SegmentInfo si, FieldInfos fn,
            IOContext context)
            => @delegate.FieldsReader(directory, si, fn, context);

        public override StoredFieldsWriter FieldsWriter(Directory directory, SegmentInfo si, IOContext context)
        {
            if (random.Next(100) == 0)
            {
                throw new IOException("Fake IOException from StoredFieldsFormat.FieldsWriter()");
            }

            return new CrankyStoredFieldsWriter(@delegate.FieldsWriter(directory, si, context), random);
        }

        private class CrankyStoredFieldsWriter : StoredFieldsWriter
        {
            private readonly StoredFieldsWriter @delegate;
            private readonly Random random;

            public CrankyStoredFieldsWriter(StoredFieldsWriter @delegate, Random random)
            {
                this.@delegate = @delegate;
                this.random = random;
            }

            public override void Abort()
            {
                @delegate.Abort();

                if (random.Next(100) == 0)
                {
                    throw RuntimeException.Create(new IOException("Fake IOException from StoredFieldsWriter.Abort()"));
                }
            }

            public override void Finish(FieldInfos fis, int numDocs)
            {
                if (random.Next(100) == 0)
                {
                    throw new IOException("Fake IOException from StoredFieldsWriter.Finish()");
                }

                @delegate.Finish(fis, numDocs);
            }

            public override int Merge(MergeState mergeState)
            {
                if (random.Next(100) == 0)
                {
                    throw new IOException("Fake IOException from StoredFieldsWriter.Merge()");
                }

                return base.Merge(mergeState);
            }

            protected override void Dispose(bool disposing)
            {
                @delegate.Dispose();

                if (random.Next(1000) == 0)
                {
                    throw new IOException("Fake IOException from StoredFieldsWriter.Dispose()");
                }
            }

            // per doc/field methods: lower probability since they are invoked so many times.

            public override void StartDocument(int numStoredFields)
            {
                if (random.Next(10000) == 0)
                {
                    throw new IOException("Fake IOException from StoredFieldsWriter.StartDocument()");
                }

                @delegate.StartDocument(numStoredFields);
            }

            public override void FinishDocument()
            {
                if (random.Next(10000) == 0)
                {
                    throw new IOException("Fake IOException from StoredFieldsWriter.FinishDocument()");
                }

                @delegate.FinishDocument();
            }

            public override void WriteField(FieldInfo info, IIndexableField field)
            {
                if (random.Next(10000) == 0)
                {
                    throw new IOException("Fake IOException from StoredFieldsWriter.WriteField()");
                }

                @delegate.WriteField(info, field);
            }
        }
    }
}
