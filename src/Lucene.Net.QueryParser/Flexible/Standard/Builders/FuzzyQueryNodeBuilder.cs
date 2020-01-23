using J2N;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers.Flexible.Core.Nodes;
using Lucene.Net.Search;

namespace Lucene.Net.QueryParsers.Flexible.Standard.Builders
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
    /// Builds a <see cref="FuzzyQuery"/> object from a <see cref="FuzzyQueryNode"/> object.
    /// </summary>
    public class FuzzyQueryNodeBuilder : IStandardQueryBuilder
    {
        public FuzzyQueryNodeBuilder()
        {
            // empty constructor
        }

        public virtual Query Build(IQueryNode queryNode)
        {
            FuzzyQueryNode fuzzyNode = (FuzzyQueryNode)queryNode;
            string text = fuzzyNode.GetTextAsString();

#pragma warning disable 612, 618
            int numEdits = FuzzyQuery.SingleToEdits(fuzzyNode.Similarity,
                text.CodePointCount(0, text.Length));
#pragma warning restore 612, 618

            return new FuzzyQuery(new Term(fuzzyNode.GetFieldAsString(), fuzzyNode
                .GetTextAsString()), numEdits, fuzzyNode
                .PrefixLength);
        }
    }
}
