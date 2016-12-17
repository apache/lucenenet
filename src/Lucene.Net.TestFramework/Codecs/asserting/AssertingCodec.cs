namespace Lucene.Net.Codecs.asserting
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

    using Lucene46Codec = Lucene.Net.Codecs.Lucene46.Lucene46Codec;

    /// <summary>
    /// Acts like <seealso cref="Lucene46Codec"/> but with additional asserts.
    /// </summary>
    public sealed class AssertingCodec : FilterCodec
    {
        private readonly PostingsFormat Postings = new AssertingPostingsFormat();
        private readonly TermVectorsFormat Vectors = new AssertingTermVectorsFormat();
        private readonly StoredFieldsFormat StoredFields = new AssertingStoredFieldsFormat();
        private readonly DocValuesFormat DocValues = new AssertingDocValuesFormat();
        private readonly NormsFormat Norms = new AssertingNormsFormat();

        public AssertingCodec()
            : base("Asserting", new Lucene46Codec())
        {
        }

        public override PostingsFormat PostingsFormat
        {
            get { return Postings; }
        }

        public override TermVectorsFormat TermVectorsFormat
        {
            get { return Vectors; }
        }

        public override StoredFieldsFormat StoredFieldsFormat
        {
            get { return StoredFields; }
        }

        public override DocValuesFormat DocValuesFormat
        {
            get { return DocValues; }
        }

        public override NormsFormat NormsFormat
        {
            get { return Norms; }
        }
    }
}