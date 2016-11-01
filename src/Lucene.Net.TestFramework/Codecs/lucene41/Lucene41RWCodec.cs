namespace Lucene.Net.Codecs.Lucene41
{
    using Lucene40FieldInfosFormat = Lucene.Net.Codecs.Lucene40.Lucene40FieldInfosFormat;
    using Lucene40FieldInfosWriter = Lucene.Net.Codecs.Lucene40.Lucene40FieldInfosWriter;
    using Lucene40RWDocValuesFormat = Lucene.Net.Codecs.Lucene40.Lucene40RWDocValuesFormat;
    using Lucene40RWNormsFormat = Lucene.Net.Codecs.Lucene40.Lucene40RWNormsFormat;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;

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
    /// Read-write version of <seealso cref="Lucene41Codec"/> for testing.
    /// </summary>
    public class Lucene41RWCodec : Lucene41Codec
    {
        private readonly StoredFieldsFormat FieldsFormat = new Lucene41StoredFieldsFormat();
        private readonly FieldInfosFormat fieldInfos;
        private readonly DocValuesFormat DocValues;
        private readonly NormsFormat Norms;
        private readonly bool _oldFormatImpersonationIsActive;

        /// <summary>
        /// LUCENENET specific
        /// Creates the codec with OldFormatImpersonationIsActive = true.
        /// </summary>
        /// <remarks>
        /// Added so that SPIClassIterator can locate this Codec.  The iterator
        /// only recognises classes that have empty constructors.
        /// </remarks>
        public Lucene41RWCodec()
            : this(true)
        { }

        /// <param name="oldFormatImpersonationIsActive">
        /// LUCENENET specific
        /// Added to remove dependency on then-static <see cref="LuceneTestCase.OLD_FORMAT_IMPERSONATION_IS_ACTIVE"/> 
        /// </param>
        public Lucene41RWCodec(bool oldFormatImpersonationIsActive) : base()
        {
            _oldFormatImpersonationIsActive = oldFormatImpersonationIsActive;

            Norms = new Lucene40RWNormsFormat(oldFormatImpersonationIsActive);
            fieldInfos = new Lucene40FieldInfosFormatAnonymousInnerClassHelper(oldFormatImpersonationIsActive);
            DocValues = new Lucene40RWDocValuesFormat(oldFormatImpersonationIsActive);
        }

        private class Lucene40FieldInfosFormatAnonymousInnerClassHelper : Lucene40FieldInfosFormat
        {
            private readonly bool _oldFormatImpersonationIsActive;

            /// <param name="oldFormatImpersonationIsActive">
            /// LUCENENET specific
            /// Added to remove dependency on then-static <see cref="LuceneTestCase.OLD_FORMAT_IMPERSONATION_IS_ACTIVE"/> 
            /// </param>
            public Lucene40FieldInfosFormatAnonymousInnerClassHelper(bool oldFormatImpersonationIsActive) : base()
            {
                _oldFormatImpersonationIsActive = oldFormatImpersonationIsActive;
            }

            public override FieldInfosWriter FieldInfosWriter
            {
                get
                {
                    if (!_oldFormatImpersonationIsActive)
                    {
                        return base.FieldInfosWriter;
                    }
                    else
                    {
                        return new Lucene40FieldInfosWriter();
                    }
                }
            }
        }

        public override FieldInfosFormat FieldInfosFormat()
        {
            return fieldInfos;
        }

        public override StoredFieldsFormat StoredFieldsFormat()
        {
            return FieldsFormat;
        }

        public override DocValuesFormat DocValuesFormat()
        {
            return DocValues;
        }

        public override NormsFormat NormsFormat()
        {
            return Norms;
        }
    }
}