namespace Lucene.Net.Codecs.Lucene42
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

    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;

    /// <summary>
    /// Read-write version of <see cref="Lucene42Codec"/> for testing.
    /// </summary>
#pragma warning disable 612, 618
    public class Lucene42RWCodec : Lucene42Codec
    {
        private readonly DocValuesFormat dv = new Lucene42RWDocValuesFormat();
        private readonly NormsFormat norms = new Lucene42NormsFormat();

        private readonly FieldInfosFormat fieldInfosFormat = new Lucene42FieldInfosFormatAnonymousInnerClassHelper();

        private class Lucene42FieldInfosFormatAnonymousInnerClassHelper : Lucene42FieldInfosFormat
        {
            public override FieldInfosWriter FieldInfosWriter
            {
                get
                {
                    if (!LuceneTestCase.OLD_FORMAT_IMPERSONATION_IS_ACTIVE)
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

        public override DocValuesFormat GetDocValuesFormatForField(string field)
        {
            return dv;
        }

        public override NormsFormat NormsFormat
        {
            get { return norms; }
        }

        public override FieldInfosFormat FieldInfosFormat
        {
            get { return fieldInfosFormat; }
        }
    }
#pragma warning restore 612, 618
}