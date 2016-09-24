using Lucene.Net.Support;

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
    /// Add an instance of this to your <seealso cref="Document"/> for every facet label.
    /// 
    /// <para>
    /// <b>NOTE:</b> you must call <seealso cref="FacetsConfig#build(Document)"/> before
    /// you add the document to IndexWriter.
    /// </para>
    /// </summary>
    public class FacetField : Field
    {
        internal static readonly FieldType TYPE = new FieldType();
        static FacetField()
        {
            TYPE.Indexed = true;
            TYPE.Freeze();
        }

        /// <summary>
        /// Dimension for this field. </summary>
        public readonly string dim;

        /// <summary>
        /// Path for this field. </summary>
        public readonly string[] path;

        /// <summary>
        /// Creates the this from {@code dim} and
        ///  {@code path}. 
        /// </summary>
        public FacetField(string dim, params string[] path)
            : base("dummy", TYPE)
        {
            VerifyLabel(dim);
            foreach (string label in path)
            {
                VerifyLabel(label);
            }
            this.dim = dim;
            if (path.Length == 0)
            {
                throw new System.ArgumentException("path must have at least one element");
            }
            this.path = path;
        }

        public override string ToString()
        {
            return "FacetField(dim=" + dim + " path=[" + Arrays.ToString(path) + "])";
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
                throw new System.ArgumentException("empty or null components not allowed; got: " + label);
            }
        }
    }
}