namespace Lucene.Net.Codecs.Lucene40
{
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using SegmentWriteState = Lucene.Net.Index.SegmentWriteState;

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
    /// Read-write version of <seealso cref="Lucene40PostingsFormat"/> for testing.
    /// </summary>
    public class Lucene40RWPostingsFormat : Lucene40PostingsFormat
    {
        private readonly bool _oldFormatImpersonationIsActive;

        /// <summary>
        /// LUCENENET specific
        /// Creates the codec with OldFormatImpersonationIsActive = true.
        /// </summary>
        /// <remarks>
        /// Added so that SPIClassIterator can locate this Codec.  The iterator
        /// only recognises classes that have empty constructors.
        /// </remarks>
        public Lucene40RWPostingsFormat()
            : this(true)
        { }

        /// <param name="oldFormatImpersonationIsActive">
        /// LUCENENET specific
        /// Added to remove dependency on then-static <see cref="LuceneTestCase.OLD_FORMAT_IMPERSONATION_IS_ACTIVE"/> 
        /// </param>
        public Lucene40RWPostingsFormat(bool oldFormatImpersonationIsActive) : base()
        {
            _oldFormatImpersonationIsActive = oldFormatImpersonationIsActive;
        }

        public override FieldsConsumer FieldsConsumer(SegmentWriteState state)
        {
            if (!_oldFormatImpersonationIsActive)
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
                    FieldsConsumer ret = new BlockTreeTermsWriter(state, docs, MinBlockSize, MaxBlockSize);
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
}