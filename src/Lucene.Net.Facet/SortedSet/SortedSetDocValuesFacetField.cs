// Lucene version compatibility level 4.8.1

namespace Lucene.Net.Facet.SortedSet
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

    using Field = Lucene.Net.Documents.Field;
    using FieldType = Lucene.Net.Documents.FieldType;

    /// <summary>
    /// Add an instance of this to your <see cref="Documents.Document"/> for every facet
    /// label to be indexed via <see cref="Index.SortedSetDocValues"/>. 
    /// </summary>
    public class SortedSetDocValuesFacetField : Field
    {
        /// <summary>
        /// Indexed <see cref="FieldType"/>. </summary>
        // LUCENENET: Avoid static constructors (see https://github.com/apache/lucenenet/pull/224#issuecomment-469284006)
        public static readonly FieldType TYPE = new FieldType
        {
            IsIndexed = true
        }.Freeze();

        /// <summary>
        /// Dimension. </summary>
        public string Dim { get; private set; }

        /// <summary>
        /// Label. </summary>
        public string Label { get; private set; }

        /// <summary>
        /// Sole constructor. </summary>
        public SortedSetDocValuesFacetField(string dim, string label)
            : base("dummy", TYPE)
        {
            FacetField.VerifyLabel(label);
            FacetField.VerifyLabel(dim);
            this.Dim = dim;
            this.Label = label;
        }

        public override string ToString()
        {
            return "SortedSetDocValuesFacetField(dim=" + Dim + " label=" + Label + ")";
        }
    }
}