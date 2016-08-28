namespace Lucene.Net.Codecs.Lucene45
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

    using Lucene42FieldInfosFormat = Lucene.Net.Codecs.Lucene42.Lucene42FieldInfosFormat;
    using Lucene42FieldInfosWriter = Lucene.Net.Codecs.Lucene42.Lucene42FieldInfosWriter;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;

    /// <summary>
    /// Read-write version of <seealso cref="Lucene45Codec"/> for testing.
    /// </summary>
    public class Lucene45RWCodec : Lucene45Codec
    {
        private readonly FieldInfosFormat fieldInfosFormat;

        /// <summary>
        /// LUCENENET specific
        /// Creates the codec with OldFormatImpersonationIsActive = true.
        /// </summary>
        /// <remarks>
        /// Added so that SPIClassIterator can locate this Codec.  The iterator
        /// only recognises classes that have empty constructors.
        /// </remarks>
        public Lucene45RWCodec()
            : this(true)
        { }

        /// <param name="oldFormatImpersonationIsActive">
        /// LUCENENET specific
        /// Added to remove dependency on then-static <see cref="LuceneTestCase.OLD_FORMAT_IMPERSONATION_IS_ACTIVE"/> 
        /// </param>
        public Lucene45RWCodec(bool oldFormatImpersonationIsActive) : base()
        {
             fieldInfosFormat = new Lucene42FieldInfosFormatAnonymousInnerClassHelper(oldFormatImpersonationIsActive);
        }

        private class Lucene42FieldInfosFormatAnonymousInnerClassHelper : Lucene42FieldInfosFormat
        {
            private readonly bool _oldFormatImpersonationIsActive;

            /// <param name="oldFormatImpersonationIsActive">
            /// LUCENENET specific
            /// Added to remove dependency on then-static <see cref="LuceneTestCase.OLD_FORMAT_IMPERSONATION_IS_ACTIVE"/> 
            /// </param>
            public Lucene42FieldInfosFormatAnonymousInnerClassHelper(bool oldFormatImpersonationIsActive) : base()
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
                        return new Lucene42FieldInfosWriter();
                    }
                }
            }
        }

        public override FieldInfosFormat FieldInfosFormat()
        {
            return fieldInfosFormat;
        }
    }
}