using J2N.Text;
using Lucene.Net.QueryParsers.Flexible.Core.Parser;
using System.Text;

namespace Lucene.Net.QueryParsers.Flexible.Core.Nodes
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
    /// A <see cref="FuzzyQueryNode"/> represents a element that contains
    /// field/text/similarity tuple
    /// </summary>
    public class FuzzyQueryNode : FieldQueryNode
    {
        private float similarity;

        private int prefixLength;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="field">Name of the field query will use.</param>
        /// <param name="term">Term token to use for building term for the query</param>
        /// <param name="minSimilarity">similarity value</param>
        /// <param name="begin">position in the query string</param>
        /// <param name="end">position in the query string</param>
         // LUCENENET specific overload for string term
        public FuzzyQueryNode(string field, string term,
            float minSimilarity, int begin, int end)
            : this(field, term.AsCharSequence(), minSimilarity, begin, end)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="field">Name of the field query will use.</param>
        /// <param name="term">Term token to use for building term for the query</param>
        /// <param name="minSimilarity">similarity value</param>
        /// <param name="begin">position in the query string</param>
        /// <param name="end">position in the query string</param>
         // LUCENENET specific overload for StringBuilder term
        public FuzzyQueryNode(string field, StringBuilder term,
            float minSimilarity, int begin, int end)
            : this(field, term.AsCharSequence(), minSimilarity, begin, end)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="field">Name of the field query will use.</param>
        /// <param name="term">Term token to use for building term for the query</param>
        /// <param name="minSimilarity">similarity value</param>
        /// <param name="begin">position in the query string</param>
        /// <param name="end">position in the query string</param>
        public FuzzyQueryNode(string field, ICharSequence term,
            float minSimilarity, int begin, int end)
            : base(field, term, begin, end)
        {
            this.similarity = minSimilarity;
            IsLeaf = true;
        }

        public virtual int PrefixLength
        {
            get => this.prefixLength;
            set => this.prefixLength = value;
        }

        public override string ToQueryString(IEscapeQuerySyntax escaper)
        {
            if (IsDefaultField(this.m_field))
            {
                return GetTermEscaped(escaper) + "~" + this.similarity;
            }
            else
            {
                return this.m_field + ":" + GetTermEscaped(escaper) + "~" + this.similarity;
            }
        }

        public override string ToString()
        {
            return "<fuzzy field='" + this.m_field + "' similarity='" + this.similarity
                + "' term='" + this.m_text + "'/>";
        }

        /// <summary>
        /// Gets or Sets the similarity
        /// </summary>
        public virtual float Similarity
        {
            get => this.similarity;
            set => this.similarity = value;
        }

        public override IQueryNode CloneTree()
        {
            FuzzyQueryNode clone = (FuzzyQueryNode)base.CloneTree();

            clone.similarity = this.similarity;

            return clone;
        }
    }
}
