using Lucene.Net.Codecs.DiskDV;
using Lucene.Net.Codecs.Lucene40;
using Lucene.Net.Codecs.Lucene41;
using Lucene.Net.Codecs.Lucene46;

namespace Lucene.Net.Codecs.CheapBastard
{
    /**
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
    /// Codec that tries to use as little ram as possible because he spent all his money on beer
    /// </summary>
    // TODO: better name :) 
    // but if we named it "LowMemory" in codecs/ package, it would be irresistible like optimize()!
    public class CheapBastardCodec : FilterCodec
    {
        // TODO: would be better to have no terms index at all and bsearch a terms dict
        private readonly PostingsFormat postings = new Lucene41PostingsFormat(100, 200);
        // uncompressing versions, waste lots of disk but no ram
        private readonly StoredFieldsFormat storedFields = new Lucene40StoredFieldsFormat();
        private readonly TermVectorsFormat termVectors = new Lucene40TermVectorsFormat();
        // these go to disk for all docvalues/norms datastructures
        private readonly DocValuesFormat docValues = new DiskDocValuesFormat();
        private readonly NormsFormat norms = new DiskNormsFormat();

        public CheapBastardCodec()
            : base(new Lucene46Codec())
        { }

        public override PostingsFormat PostingsFormat => postings;

        public override DocValuesFormat DocValuesFormat => docValues;

        public override NormsFormat NormsFormat => norms;

        public override StoredFieldsFormat StoredFieldsFormat => storedFields;

        public override TermVectorsFormat TermVectorsFormat => termVectors;
    }
}
