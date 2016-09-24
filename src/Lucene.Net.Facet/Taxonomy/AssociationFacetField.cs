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
    using Document = Lucene.Net.Documents.Document; // javadocs
    using Field = Lucene.Net.Documents.Field;
    using FieldType = Lucene.Net.Documents.FieldType;

    /// <summary>
    /// Add an instance of this to your <seealso cref="Document"/> to add
    ///  a facet label associated with an arbitrary byte[].
    ///  This will require a custom <seealso cref="Facets"/>
    ///  implementation at search time; see {@link
    ///  IntAssociationFacetField} and {@link
    ///  FloatAssociationFacetField} to use existing {@link
    ///  Facets} implementations.
    /// 
    ///  @lucene.experimental 
    /// </summary>
    public class AssociationFacetField : Field
    {
        /// <summary>
        /// Indexed <seealso cref="FieldType"/>. </summary>
        public static readonly FieldType TYPE = new FieldType();
        static AssociationFacetField()
        {
            TYPE.Indexed = true;
            TYPE.Freeze();
        }

        /// <summary>
        /// Dimension for this field. </summary>
        public readonly string dim;

        /// <summary>
        /// Facet path for this field. </summary>
        public readonly string[] path;

        /// <summary>
        /// Associated value. </summary>
        public readonly BytesRef assoc;

        /// <summary>
        /// Creates this from {@code dim} and {@code path} and an
        ///  association 
        /// </summary>
        public AssociationFacetField(BytesRef assoc, string dim, params string[] path)
            : base("dummy", TYPE)
        {
            FacetField.VerifyLabel(dim);
            foreach (string label in path)
            {
                FacetField.VerifyLabel(label);
            }
            this.dim = dim;
            this.assoc = assoc;
            if (path.Length == 0)
            {
                throw new System.ArgumentException("path must have at least one element");
            }
            this.path = path;
        }

        public override string ToString()
        {
            return "AssociationFacetField(dim=" + dim + " path=" + Arrays.ToString(path) + " bytes=" + assoc + ")";
        }
    }
}