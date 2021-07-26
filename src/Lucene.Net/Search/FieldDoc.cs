using Lucene.Net.Support;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Lucene.Net.Search
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
    /// Expert: A <see cref="ScoreDoc"/> which also contains information about
    /// how to sort the referenced document.  In addition to the
    /// document number and score, this object contains an array
    /// of values for the document from the field(s) used to sort.
    /// For example, if the sort criteria was to sort by fields
    /// "a", "b" then "c", the <c>fields</c> object array
    /// will have three elements, corresponding respectively to
    /// the term values for the document in fields "a", "b" and "c".
    /// The class of each element in the array will be either
    /// <see cref="int"/>, <see cref="float"/> or <see cref="string"/> depending on the type of values
    /// in the terms of each field.
    ///
    /// <para/>Created: Feb 11, 2004 1:23:38 PM
    /// <para/>
    /// @since   lucene 1.4 </summary>
    /// <seealso cref="ScoreDoc"/>
    /// <seealso cref="TopFieldDocs"/>
    public class FieldDoc : ScoreDoc
    {
        /// <summary>
        /// Expert: The values which are used to sort the referenced document.
        /// The order of these will match the original sort criteria given by a
        /// <see cref="Sort"/> object.  Each Object will have been returned from
        /// the <see cref="FieldComparer.GetValue(int)"/> method corresponding
        /// FieldComparer used to sort this field. </summary>
        /// <seealso cref="Sort"/>
        /// <seealso cref="IndexSearcher.Search(Query,Filter,int,Sort)"/>
        public object[] Fields;

        /// <summary>
        /// Expert: Creates one of these objects with empty sort information. </summary>
        public FieldDoc(int doc, float score)
            : base(doc, score)
        {
        }

        /// <summary>
        /// Expert: Creates one of these objects with the given sort information. </summary>
        public FieldDoc(int doc, float score, object[] fields)
            : base(doc, score)
        {
            this.Fields = fields;
        }

        /// <summary>
        /// Expert: Creates one of these objects with the given sort information. </summary>
        public FieldDoc(int doc, float score, object[] fields, int shardIndex)
            : base(doc, score, shardIndex)
        {
            this.Fields = fields;
        }

        /// <summary>
        /// A convenience method for debugging.
        /// </summary>
        public override string ToString()
        {
            // super.toString returns the doc and score information, so just add the
            // fields information
            StringBuilder sb = new StringBuilder(base.ToString());
            sb.Append(" fields=");
            sb.Append(Arrays.ToString(Fields));
            return sb.ToString();
        }
    }
}