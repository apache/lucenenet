using Lucene.Net.Facet;
using Lucene.Net.Facet.SortedSet;
using Lucene.Net.Facet.Taxonomy;
using Lucene.Net.Util;
using System;

namespace Lucene.Net.Documents.Extensions
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
    /// LUCENENET specific extensions to the <see cref="Document"/> class.
    /// </summary>
    public static class DocumentExtensions
    {
        /// <summary>
        /// Adds a new <see cref="SortedSetDocValuesFacetField"/>.
        /// </summary>
        /// <remarks>
        /// Add a <see cref="SortedSetDocValuesFacetField"/> to your <see cref="Documents.Document"/> for every facet
        /// label to be indexed via <see cref="Index.SortedSetDocValues"/>. 
        /// </remarks>
        /// <param name="document">This <see cref="Document"/>.</param>
        /// <param name="dim">Dimension for this field.</param>
        /// <param name="label">Label for this field.</param>
        /// <returns>The field that was added to this <see cref="Document"/>.</returns>
        /// <exception cref="ArgumentNullException">This <paramref name="document"/> is <c>null</c>. </exception>
        public static SortedSetDocValuesFacetField AddSortedSetDocValuesFacetField(this Document document, string dim, string label)
        {
            if (document is null)
                throw new ArgumentNullException(nameof(document));

            var field = new SortedSetDocValuesFacetField(dim, label);
            document.Add(field);
            return field;
        }

        /// <summary>
        /// Adds a new <see cref="AssociationFacetField"/> using <paramref name="dim"/> and <paramref name="path"/> and an
        /// association.
        /// </summary>
        /// <param name="document">This <see cref="Document"/>.</param>
        /// <param name="assoc">Associated value.</param>
        /// <param name="dim">Dimension for this field.</param>
        /// <param name="path">Facet path for this field.</param>
        /// <returns>The field that was added to this <see cref="Document"/>.</returns>
        /// <exception cref="ArgumentNullException">This <paramref name="document"/> is <c>null</c>. </exception>
        public static AssociationFacetField AddAssociationFacetField(this Document document, BytesRef assoc, string dim, params string[] path)
        {
            if (document is null)
                throw new ArgumentNullException(nameof(document));

            var field = new AssociationFacetField(assoc, dim, path);
            document.Add(field);
            return field;
        }

        /// <summary>
        /// Adds a new <see cref="Int32AssociationFacetField"/> using <paramref name="dim"/> and <paramref name="path"/> and an
        /// <see cref="int"/> association.
        /// </summary>
        /// <param name="document">This <see cref="Document"/>.</param>
        /// <param name="assoc">Associated value.</param>
        /// <param name="dim">Dimension for this field.</param>
        /// <param name="path">Facet path for this field.</param>
        /// <returns>The field that was added to this <see cref="Document"/>.</returns>
        /// <exception cref="ArgumentNullException">This <paramref name="document"/> is <c>null</c>. </exception>
        public static Int32AssociationFacetField AddInt32AssociationFacetField(this Document document, int assoc, string dim, params string[] path)
        {
            if (document is null)
                throw new ArgumentNullException(nameof(document));

            var field = new Int32AssociationFacetField(assoc, dim, path);
            document.Add(field);
            return field;
        }

        /// <summary>
        /// Adds a new <see cref="SingleAssociationFacetField"/> using <paramref name="dim"/> and <paramref name="path"/> and a
        /// <see cref="float"/> association.
        /// </summary>
        /// <param name="document">This <see cref="Document"/>.</param>
        /// <param name="assoc">Associated value.</param>
        /// <param name="dim">Dimension for this field.</param>
        /// <param name="path">Facet path for this field.</param>
        /// <returns>The field that was added to this <see cref="Document"/>.</returns>
        /// <exception cref="ArgumentNullException">This <paramref name="document"/> is <c>null</c>. </exception>
        public static SingleAssociationFacetField AddSingleAssociationFacetField(this Document document, float assoc, string dim, params string[] path)
        {
            if (document is null)
                throw new ArgumentNullException(nameof(document));

            var field = new SingleAssociationFacetField(assoc, dim, path);
            document.Add(field);
            return field;
        }

        /// <summary>
        /// Adds a new <see cref="FacetField"/> with the specified <paramref name="dim"/> and
        /// <paramref name="path"/>. 
        /// </summary>
        /// <param name="document">This <see cref="Document"/>.</param>
        /// <param name="dim">Dimension for this field.</param>
        /// <param name="path">Facet path for this field.</param>
        /// <returns>The field that was added to this <see cref="Document"/>.</returns>
        /// <exception cref="ArgumentNullException">This <paramref name="document"/> is <c>null</c>. </exception>
        public static FacetField AddFacetField(this Document document, string dim, params string[] path)
        {
            if (document is null)
                throw new ArgumentNullException(nameof(document));

            var field = new FacetField(dim, path);
            document.Add(field);
            return field;
        }
    }
}
