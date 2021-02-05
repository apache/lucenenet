// Lucene version compatibility level 4.8.1
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
    /// Add an instance of this to your <see cref="Document"/> to add
    /// a facet label associated with an <see cref="int"/>.  Use <see cref="TaxonomyFacetSumInt32Associations"/>
    /// to aggregate int values per facet label at search time.
    /// <para/>
    /// NOTE: This was IntAssociationFacetField in Lucene
    /// <para/>
    /// @lucene.experimental 
    /// </summary>
    public class Int32AssociationFacetField : AssociationFacetField
    {
        /// <summary>
        /// Creates this from <paramref name="dim"/> and <paramref name="path"/> and an
        /// int association 
        /// </summary>
        public Int32AssociationFacetField(int assoc, string dim, params string[] path)
            : base(Int32ToBytesRef(assoc), dim, path)
        {
        }

        /// <summary>
        /// Encodes an <see cref="int"/> as a 4-byte <see cref="BytesRef"/>,
        /// big-endian.
        /// <para/>
        /// NOTE: This was intToBytesRef() in Lucene
        /// </summary>
        public static BytesRef Int32ToBytesRef(int v)
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
        /// Decodes a previously encoded <see cref="int"/>.
        /// <para/>
        /// NOTE: This was bytesRefToInt() in Lucene
        /// </summary>
        public static int BytesRefToInt32(BytesRef b)
        {
            return ((b.Bytes[b.Offset] & 0xFF) << 24) | ((b.Bytes[b.Offset + 1] & 0xFF) << 16) | 
                ((b.Bytes[b.Offset + 2] & 0xFF) << 8) | (b.Bytes[b.Offset + 3] & 0xFF);
        }

        public override string ToString()
        {
            return "Int32AssociationFacetField(dim=" + Dim + " path=" + Arrays.ToString(Path) + " value=" + BytesRefToInt32(Assoc) + ")";
        }
    }
}