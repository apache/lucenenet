using Lucene.Net.Util;

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
    /// Add this <see cref="IAttribute"/> to a <see cref="Index.TermsEnum"/> returned by <see cref="MultiTermQuery.GetTermsEnum(Index.Terms, AttributeSource)"/>
    /// and update the boost on each returned term. This enables to control the boost factor
    /// for each matching term in <see cref="MultiTermQuery.SCORING_BOOLEAN_QUERY_REWRITE"/> or
    /// <see cref="TopTermsRewrite{Q}"/> mode.
    /// <see cref="FuzzyQuery"/> is using this to take the edit distance into account.
    /// <para/><b>Please note:</b> this attribute is intended to be added only by the <see cref="Index.TermsEnum"/>
    /// to itself in its constructor and consumed by the <see cref="MultiTermQuery.RewriteMethod"/>.
    /// <para/>
    /// @lucene.internal
    /// </summary>
    public interface IBoostAttribute : IAttribute
    {
        /// <summary>
        /// Gets or Sets the boost in this attribute. Default is <c>1.0f</c>.
        /// </summary>
        float Boost { get; set; }
    }
}