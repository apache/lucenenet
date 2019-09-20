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
    /// Read-write version of <see cref="Lucene41Codec"/> for testing.
    /// </summary>
#pragma warning disable 612, 618
    public class Lucene41RWCodec : Lucene41Codec
    {
        private readonly StoredFieldsFormat fieldsFormat = new Lucene41StoredFieldsFormat();
        private readonly FieldInfosFormat fieldInfos = new Lucene40FieldInfosFormatAnonymousInnerClassHelper();

        private class Lucene40FieldInfosFormatAnonymousInnerClassHelper : Lucene40FieldInfosFormat
        {
            public override FieldInfosWriter FieldInfosWriter
            {
                get
                {
                    if (!LuceneTestCase.OldFormatImpersonationIsActive)
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

        private readonly DocValuesFormat docValues = new Lucene40RWDocValuesFormat();
        private readonly NormsFormat norms = new Lucene40RWNormsFormat();


        public override FieldInfosFormat FieldInfosFormat
        {
            get { return fieldInfos; }
        }

        public override StoredFieldsFormat StoredFieldsFormat
        {
            get { return fieldsFormat; }
        }

        public override DocValuesFormat DocValuesFormat
        {
            get { return docValues; }
        }

        public override NormsFormat NormsFormat
        {
            get { return norms; }
        }
    }
#pragma warning restore 612, 618
}