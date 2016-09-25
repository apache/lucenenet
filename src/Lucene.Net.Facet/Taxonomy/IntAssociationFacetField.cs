using Lucene.Net.Support;

namespace Lucene.Net.Facet.Taxonomy
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

    using BytesRef = Lucene.Net.Util.BytesRef;
    using Document = Lucene.Net.Documents.Document;

    /// <summary>
    /// Add an instance of this to your <seealso cref="Document"/> to add
    ///  a facet label associated with an int.  Use {@link
    ///  TaxonomyFacetSumIntAssociations} to aggregate int values
    ///  per facet label at search time.
    /// 
    ///  @lucene.experimental 
    /// </summary>
    public class IntAssociationFacetField : AssociationFacetField
    {
        /// <summary>
        /// Creates this from {@code dim} and {@code path} and an
        ///  int association 
        /// </summary>
        public IntAssociationFacetField(int assoc, string dim, params string[] path)
            : base(IntToBytesRef(assoc), dim, path)
        {
        }

        /// <summary>
        /// Encodes an {@code int} as a 4-byte <seealso cref="BytesRef"/>,
        ///  big-endian. 
        /// </summary>
        public static BytesRef IntToBytesRef(int v)
        {

            byte[] bytes = new byte[4];
            // big-endian:
            bytes[0] = (byte)(v >> 24);
            bytes[1] = (byte)(v >> 16);
            bytes[2] = (byte)(v >> 8);
            bytes[3] = (byte)v;
            return new BytesRef(bytes);
        }

        /// <summary>
        /// Decodes a previously encoded {@code int}. </summary>
        public static int BytesRefToInt(BytesRef b)
        {
            return ((b.Bytes[b.Offset] & 0xFF) << 24) | ((b.Bytes[b.Offset + 1] & 0xFF) << 16) | 
                ((b.Bytes[b.Offset + 2] & 0xFF) << 8) | (b.Bytes[b.Offset + 3] & 0xFF);
        }

        public override string ToString()
        {
            return "IntAssociationFacetField(dim=" + dim + " path=" + Arrays.ToString(path) + " value=" + BytesRefToInt(assoc) + ")";
        }
    }
}