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
    /// Add an instance of this to your <seealso cref="Document"/> to add
    ///  a facet label associated with a float.  Use {@link
    ///  TaxonomyFacetSumFloatAssociations} to aggregate float values
    ///  per facet label at search time.
    /// 
    ///  @lucene.experimental 
    /// </summary>
    public class FloatAssociationFacetField : AssociationFacetField
    {
        /// <summary>
        /// Creates this from {@code dim} and {@code path} and a
        ///  float association 
        /// </summary>
        public FloatAssociationFacetField(float assoc, string dim, params string[] path) 
            : base(FloatToBytesRef(assoc), dim, path)
        {
        }

        /// <summary>
        /// Encodes a {@code float} as a 4-byte <seealso cref="BytesRef"/>. </summary>
        public static BytesRef FloatToBytesRef(float v)
        {
            return IntAssociationFacetField.IntToBytesRef(Number.FloatToIntBits(v));
        }

        /// <summary>
        /// Decodes a previously encoded {@code float}. </summary>
        public static float BytesRefToFloat(BytesRef b)
        {
            return Number.IntBitsToFloat(IntAssociationFacetField.BytesRefToInt(b));
        }

        public override string ToString()
        {
            return "FloatAssociationFacetField(dim=" + dim + " path=" + Arrays.ToString(path) + 
                " value=" + BytesRefToFloat(assoc).ToString("0.0#####", CultureInfo.InvariantCulture) + ")";
        }
    }
}