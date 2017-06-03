using Lucene.Net.Util;
using BytesRef = Lucene.Net.Util.BytesRef;

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
    /// Add this <see cref="IAttribute"/> to a fresh <see cref="AttributeSource"/> before calling
    /// <see cref="MultiTermQuery.GetTermsEnum(Index.Terms, AttributeSource)"/>.
    /// <see cref="FuzzyQuery"/> is using this to control its internal behaviour
    /// to only return competitive terms.
    /// <para/><b>Please note:</b> this attribute is intended to be added by the <see cref="MultiTermQuery.RewriteMethod"/>
    /// to an empty <see cref="AttributeSource"/> that is shared for all segments
    /// during query rewrite. This attribute source is passed to all segment enums
    /// on <see cref="MultiTermQuery.GetTermsEnum(Index.Terms,AttributeSource)"/>.
    /// <see cref="TopTermsRewrite{Q}"/> uses this attribute to
    /// inform all enums about the current boost, that is not competitive.
    /// <para/>
    /// @lucene.internal
    /// </summary>
    public interface IMaxNonCompetitiveBoostAttribute : IAttribute
    {
        /// <summary>
        /// This is the maximum boost that would not be competitive. </summary>
        float MaxNonCompetitiveBoost { set; get; }

        /// <summary>
        /// This is the term or <c>null</c> of the term that triggered the boost change. </summary>
        BytesRef CompetitiveTerm { set; get; }
    }
}