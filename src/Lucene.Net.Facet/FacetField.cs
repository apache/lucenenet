// Lucene version compatibility level 4.8.1
using Lucene.Net.Support;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Lucene.Net.Facet
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

    using Document = Lucene.Net.Documents.Document;
    using Field = Lucene.Net.Documents.Field;
    using FieldType = Lucene.Net.Documents.FieldType;

    /// <summary>
    /// Add an instance of this to your <see cref="Document"/> for every facet label.
    /// 
    /// <para>
    /// <b>NOTE:</b> you must call <see cref="FacetsConfig.Build(Document)"/> before
    /// you add the document to <see cref="Index.IndexWriter"/>.
    /// </para>
    /// </summary>
    public class FacetField : Field
    {
        // LUCENENET: Avoid static constructors (see https://github.com/apache/lucenenet/pull/224#issuecomment-469284006)
        internal static readonly FieldType TYPE = new FieldType
        {
            IsIndexed = true
        }.Freeze();

        /// <summary>
        /// Dimension for this field.
        /// </summary>
        public string Dim { get; private set; }

        /// <summary>
        /// Path for this field.
        /// </summary>
        [WritableArray]
        [SuppressMessage("Microsoft.Performance", "CA1819", Justification = "Lucene's design requires some writable array properties")]
        public string[] Path { get; private set; }

        /// <summary>
        /// Creates the this from <paramref name="dim"/> and
        /// <paramref name="path"/>. 
        /// </summary>
        public FacetField(string dim, params string[] path)
            : base("dummy", TYPE)
        {
            VerifyLabel(dim);
            foreach (string label in path)
            {
                VerifyLabel(label);
            }
            this.Dim = dim;
            if (path.Length == 0)
            {
                throw new ArgumentException("path must have at least one element");
            }
            this.Path = path;
        }

        public override string ToString()
        {
            return "FacetField(dim=" + Dim + " path=[" + Arrays.ToString(Path) + "])";
        }

        /// <summary>
        /// Verifies the label is not null or empty string.
        /// 
        ///  @lucene.internal 
        /// </summary>
        public static void VerifyLabel(string label)
        {
            if (string.IsNullOrEmpty(label))
            {
                throw new ArgumentException("empty or null components not allowed; got: " + label);
            }
        }
    }
}