using Lucene.Net.Index;
using Lucene.Net.Util;
using Console = Lucene.Net.Util.SystemConsole;

namespace Lucene.Net.Codecs.Lucene3x
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
    /// Codec, only for testing, that can write and read the
    /// pre-flex index format.
    /// <para/>
    /// @lucene.experimental
    /// </summary>
#pragma warning disable 612, 618
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

            return new Lucene3xFieldsAnonymousClass(state.Directory, state.FieldInfos, state.SegmentInfo, state.Context, state.TermsIndexDivisor);
        }

        private sealed class Lucene3xFieldsAnonymousClass : Lucene3xFields
        {
            public Lucene3xFieldsAnonymousClass(Store.Directory directory, FieldInfos fieldInfos, SegmentInfo segmentInfo, Store.IOContext context, int termsIndexDivisor)
                : base(directory, fieldInfos, segmentInfo, context, termsIndexDivisor)
            {
            }

            protected override bool SortTermsByUnicode
            {
                get
                {
                    // We carefully peek into stack track above us: if
                    // we are part of a "merge", we must sort by UTF16:
                    bool unicodeSortOrder = true;

                    // LUCENENET specific: for these to work in release mode, we have added [MethodImpl(MethodImplOptions.NoInlining)]
                    // to each possible target of the StackTraceHelper. If these change, so must the attribute on the target methods.
                    if (StackTraceHelper.DoesStackTraceContainMethod("Merge"))
                    {
                        unicodeSortOrder = false;
                        if (LuceneTestCase.Verbose)
                        {
                            Console.WriteLine("NOTE: PreFlexRW codec: forcing legacy UTF16 term sort order");
                        }
                    }

                    return unicodeSortOrder;
                }
            }
        }
    }
#pragma warning restore 612, 618
}