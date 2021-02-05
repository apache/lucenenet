// Lucene version compatibility level 4.8.1
using Lucene.Net.Support;
using System.Globalization;

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
    /// a facet label associated with a <see cref="float"/>.  Use <see cref="TaxonomyFacetSumSingleAssociations"/>
    /// to aggregate <see cref="float"/> values per facet label at search time.
    /// <para/>
    /// NOTE: This was FloatAssociationFacetField in Lucene
    /// <para/>
    /// @lucene.experimental 
    /// </summary>
    public class SingleAssociationFacetField : AssociationFacetField
    {
        /// <summary>
        /// Creates this from <paramref name="dim"/> and <paramref name="path"/> and a
        /// <see cref="float"/> association 
        /// </summary>
        public SingleAssociationFacetField(float assoc, string dim, params string[] path) 
            : base(SingleToBytesRef(assoc), dim, path)
        {
        }

        /// <summary>
        /// Encodes a <see cref="float"/> as a 4-byte <see cref="BytesRef"/>.
        /// <para/>
        /// NOTE: This was floatToBytesRef() in Lucene
        /// </summary>
        public static BytesRef SingleToBytesRef(float v)
        {
            return Int32AssociationFacetField.Int32ToBytesRef(J2N.BitConversion.SingleToInt32Bits(v));
        }

        /// <summary>
        /// Decodes a previously encoded <see cref="float"/>.
        /// <para/>
        /// NOTE: This was bytesRefToFloat() in Lucene
        /// </summary>
        public static float BytesRefToSingle(BytesRef b)
        {
            return J2N.BitConversion.Int32BitsToSingle(Int32AssociationFacetField.BytesRefToInt32(b));
        }

        public override string ToString()
        {
            return "SingleAssociationFacetField(dim=" + Dim + " path=" + Arrays.ToString(Path) + 
                " value=" + BytesRefToSingle(Assoc).ToString("0.0#####", CultureInfo.InvariantCulture) + ")";
        }
    }
}