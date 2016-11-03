using System;
using System.Reflection;
using System.Diagnostics;

namespace Lucene.Net.Codecs.Lucene3x
{
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using SegmentReadState = Lucene.Net.Index.SegmentReadState;

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

    using SegmentWriteState = Lucene.Net.Index.SegmentWriteState;

    /// <summary>
    /// Codec, only for testing, that can write and read the
    ///  pre-flex index format.
    ///
    /// @lucene.experimental
    /// </summary>
    internal class PreFlexRWPostingsFormat : Lucene3xPostingsFormat
    {
        public PreFlexRWPostingsFormat()
        {
            // NOTE: we impersonate the PreFlex codec so that it can
            // read the segments we write!
        }

        public override FieldsConsumer FieldsConsumer(SegmentWriteState state)
        {
            return new PreFlexRWFieldsWriter(state);
        }

        public override FieldsProducer FieldsProducer(SegmentReadState state)
        {
            // Whenever IW opens readers, eg for merging, we have to
            // keep terms order in UTF16:

            return new Lucene3xFieldsAnonymousInnerClassHelper(this, state.Directory, state.FieldInfos, state.SegmentInfo, state.Context, state.TermsIndexDivisor);
        }

        private class Lucene3xFieldsAnonymousInnerClassHelper : Lucene3xFields
        {
            private readonly PreFlexRWPostingsFormat OuterInstance;

            public Lucene3xFieldsAnonymousInnerClassHelper(PreFlexRWPostingsFormat outerInstance, Store.Directory directory, Index.FieldInfos fieldInfos, Index.SegmentInfo segmentInfo, Store.IOContext context, int termsIndexDivisor)
                : base(directory, fieldInfos, segmentInfo, context, termsIndexDivisor)
            {
                this.OuterInstance = outerInstance;
            }

            protected internal override bool SortTermsByUnicode()
            {
                // We carefully peek into stack track above us: if
                // we are part of a "merge", we must sort by UTF16:
                bool unicodeSortOrder = true;

                if(Util.StackTraceHelper.DoesStackTraceContainMethod("Merge"))
                {
                       unicodeSortOrder = false;
                        if (LuceneTestCase.VERBOSE)
                        {
                            Console.WriteLine("NOTE: PreFlexRW codec: forcing legacy UTF16 term sort order");
                        }
                }

                return unicodeSortOrder;
            }
        }
    }
}