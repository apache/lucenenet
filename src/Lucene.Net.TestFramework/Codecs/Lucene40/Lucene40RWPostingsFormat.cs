using Lucene.Net.Index;
using Lucene.Net.Util;

namespace Lucene.Net.Codecs.Lucene40
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
    /// Read-write version of <see cref="Lucene40PostingsFormat"/> for testing.
    /// </summary>
#pragma warning disable 612, 618
    public class Lucene40RWPostingsFormat : Lucene40PostingsFormat
    {
        public override FieldsConsumer FieldsConsumer(SegmentWriteState state)
        {
            if (!LuceneTestCase.OldFormatImpersonationIsActive)
            {
                return base.FieldsConsumer(state);
            }
            else
            {
                PostingsWriterBase docs = new Lucene40PostingsWriter(state);

                // TODO: should we make the terms index more easily
                // pluggable?  Ie so that this codec would record which
                // index impl was used, and switch on loading?
                // Or... you must make a new Codec for this?
                bool success = false;
                try
                {
                    FieldsConsumer ret = new BlockTreeTermsWriter<object>(state, docs, m_minBlockSize, m_maxBlockSize, subclassState: null);
                    success = true;
                    return ret;
                }
                finally
                {
                    if (!success)
                    {
                        docs.Dispose();
                    }
                }
            }
        }
    }
#pragma warning restore 612, 618
}