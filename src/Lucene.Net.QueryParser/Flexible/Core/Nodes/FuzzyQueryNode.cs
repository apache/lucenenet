using Lucene.Net.QueryParsers.Flexible.Core.Parser;
using Lucene.Net.Support;

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
    /// A {@link FuzzyQueryNode} represents a element that contains
    /// field/text/similarity tuple
    /// </summary>
    public class FuzzyQueryNode : FieldQueryNode
    {
        private float similarity;

        private int prefixLength;

        /**
         * @param field
         *          Name of the field query will use.
         * @param termStr
         *          Term token to use for building term for the query
         */
        /**
         * @param field
         *          - Field name
         * @param term
         *          - Value
         * @param minSimilarity
         *          - similarity value
         * @param begin
         *          - position in the query string
         * @param end
         *          - position in the query string
         */
         // LUCENENET specific overload for string term
        public FuzzyQueryNode(string field, string term,
            float minSimilarity, int begin, int end)
            : this(field, term.ToCharSequence(), minSimilarity, begin, end)
        {
        }

        /**
         * @param field
         *          - Field name
         * @param term
         *          - Value
         * @param minSimilarity
         *          - similarity value
         * @param begin
         *          - position in the query string
         * @param end
         *          - position in the query string
         */
        public FuzzyQueryNode(string field, ICharSequence term,
            float minSimilarity, int begin, int end)
            : base(field, term, begin, end)
        {
            this.similarity = minSimilarity;
            IsLeaf = true;
        }

        public virtual int PrefixLength
        {
            get { return this.prefixLength; }
            set { this.prefixLength = value; }
        }

        public override string ToQueryString(IEscapeQuerySyntax escaper)
        {
            if (IsDefaultField(this.field))
            {
                return GetTermEscaped(escaper) + "~" + this.similarity;
            }
            else
            {
                return this.field + ":" + GetTermEscaped(escaper) + "~" + this.similarity;
            }
        }

        public override string ToString()
        {
            return "<fuzzy field='" + this.field + "' similarity='" + this.similarity
                + "' term='" + this.text + "'/>";
        }

        /**
         * @return the similarity
         */
        public virtual float Similarity
        {
            get { return this.similarity; }
            set { this.similarity = value; }
        }

        public override IQueryNode CloneTree()
        {
            FuzzyQueryNode clone = (FuzzyQueryNode)base.CloneTree();

            clone.similarity = this.similarity;

            return clone;
        }
    }
}
