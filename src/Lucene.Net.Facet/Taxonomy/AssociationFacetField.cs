// Lucene version compatibility level 4.8.1
using Lucene.Net.Support;
using System;
using System.Diagnostics.CodeAnalysis;

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
    /// Add an instance of this to your <see cref="Document"/> to add
    /// a facet label associated with an arbitrary <see cref="T:byte[]"/>.
    /// This will require a custom <see cref="Facets"/>
    /// implementation at search time; see <see cref="Int32AssociationFacetField"/> 
    /// and <see cref="SingleAssociationFacetField"/> to use existing 
    /// <see cref="Facets"/> implementations.
    /// <para/>
    /// @lucene.experimental 
    /// </summary>
    public class AssociationFacetField : Field
    {
        /// <summary>
        /// Indexed <see cref="FieldType"/>.
        /// </summary>
        // LUCENENET: Avoid static constructors (see https://github.com/apache/lucenenet/pull/224#issuecomment-469284006)
        public static readonly FieldType TYPE = new FieldType
        {
            IsIndexed = true
        }.Freeze();

        /// <summary>
        /// Dimension for this field.
        /// </summary>
        public string Dim { get; private set; }

        /// <summary>
        /// Facet path for this field.
        /// </summary>
        [WritableArray]
        [SuppressMessage("Microsoft.Performance", "CA1819", Justification = "Lucene's design requires some writable array properties")]
        public string[] Path { get; private set; }

        /// <summary>
        /// Associated value.
        /// </summary>
        public BytesRef Assoc { get; private set; }

        /// <summary>
        /// Creates this from <paramref name="dim"/> and <paramref name="path"/> and an
        /// association 
        /// </summary>
        public AssociationFacetField(BytesRef assoc, string dim, params string[] path)
            : base("dummy", TYPE)
        {
            FacetField.VerifyLabel(dim);
            foreach (string label in path)
            {
                FacetField.VerifyLabel(label);
            }
            this.Dim = dim;
            this.Assoc = assoc;
            if (path.Length == 0)
            {
                throw new ArgumentException("path must have at least one element");
            }
            this.Path = path;
        }

        public override string ToString()
        {
            return "AssociationFacetField(dim=" + Dim + " path=" + Arrays.ToString(Path) + " bytes=" + Assoc + ")";
        }
    }
}