using Lucene.Net.Codecs.Lucene46;

namespace Lucene.Net.Codecs.Asserting
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
    /// Acts like <see cref="Lucene46Codec"/> but with additional asserts.
    /// </summary>
    [CodecName("Asserting")]
    public sealed class AssertingCodec : FilterCodec
    {
        private readonly PostingsFormat postings = new AssertingPostingsFormat();
        private readonly TermVectorsFormat vectors = new AssertingTermVectorsFormat();
        private readonly StoredFieldsFormat storedFields = new AssertingStoredFieldsFormat();
        private readonly DocValuesFormat docValues = new AssertingDocValuesFormat();
        private readonly NormsFormat norms = new AssertingNormsFormat();

        public AssertingCodec()
            : base(new Lucene46Codec())
        { }

        public override PostingsFormat PostingsFormat => postings;

        public override TermVectorsFormat TermVectorsFormat => vectors;

        public override StoredFieldsFormat StoredFieldsFormat => storedFields;

        public override DocValuesFormat DocValuesFormat => docValues;

        public override NormsFormat NormsFormat => norms;
    }
}