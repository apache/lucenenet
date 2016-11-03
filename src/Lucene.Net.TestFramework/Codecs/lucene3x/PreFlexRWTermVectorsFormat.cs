using System;
using System.Diagnostics;

namespace Lucene.Net.Codecs.Lucene3x
{
    using Directory = Lucene.Net.Store.Directory;

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

    using FieldInfos = Lucene.Net.Index.FieldInfos;
    using IOContext = Lucene.Net.Store.IOContext;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using SegmentInfo = Lucene.Net.Index.SegmentInfo;

    internal class PreFlexRWTermVectorsFormat : Lucene3xTermVectorsFormat
    {
        public override TermVectorsWriter VectorsWriter(Directory directory, SegmentInfo segmentInfo, IOContext context)
        {
            return new PreFlexRWTermVectorsWriter(directory, segmentInfo.Name, context);
        }

        public override TermVectorsReader VectorsReader(Directory directory, SegmentInfo segmentInfo, FieldInfos fieldInfos, IOContext context)
        {
            return new Lucene3xTermVectorsReaderAnonymousInnerClassHelper(this, directory, segmentInfo, fieldInfos, context);
        }

        private class Lucene3xTermVectorsReaderAnonymousInnerClassHelper : Lucene3xTermVectorsReader
        {
            private readonly PreFlexRWTermVectorsFormat OuterInstance;

            public Lucene3xTermVectorsReaderAnonymousInnerClassHelper(PreFlexRWTermVectorsFormat outerInstance, Directory directory, SegmentInfo segmentInfo, FieldInfos fieldInfos, IOContext context)
                : base(directory, segmentInfo, fieldInfos, context)
            {
                this.OuterInstance = outerInstance;
            }

            protected internal override bool SortTermsByUnicode()
            {

                // We carefully peek into stack track above us: if
                // we are part of a "merge", we must sort by UTF16:
                bool unicodeSortOrder = true;

                if (Util.StackTraceHelper.DoesStackTraceContainMethod("Merge"))
                {
                        unicodeSortOrder = false;
                        if (LuceneTestCase.VERBOSE)
                        {
                            Console.WriteLine("NOTE: PreFlexRW codec: forcing legacy UTF16 vector term sort order");
                        }
                }

                return unicodeSortOrder;
            }
        }
    }
}