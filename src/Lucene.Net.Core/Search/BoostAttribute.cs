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

    // javadocs only
    // javadocs only
    // javadocs only

    /// <summary>
    /// Add this <seealso cref="Attribute"/> to a <seealso cref="TermsEnum"/> returned by <seealso cref="MultiTermQuery#getTermsEnum(Terms,AttributeSource)"/>
    /// and update the boost on each returned term. this enables to control the boost factor
    /// for each matching term in <seealso cref="MultiTermQuery#SCORING_BOOLEAN_QUERY_REWRITE"/> or
    /// <seealso cref="TopTermsRewrite"/> mode.
    /// <seealso cref="FuzzyQuery"/> is using this to take the edit distance into account.
    /// <p><b>Please note:</b> this attribute is intended to be added only by the TermsEnum
    /// to itself in its constructor and consumed by the <seealso cref="MultiTermQuery.RewriteMethod"/>.
    /// @lucene.internal
    /// </summary>
    // LUCENENET TODO: Move IBoostAttribute here, put this implementation in BoostAttributeImpl (since that is what it is)
    public sealed class BoostAttribute : Util.Attribute, IBoostAttribute
    {
        /*/// <summary>
        /// Sets the boost in this attribute </summary>
        float Boost {set;get;}*/
        private float boost = 1.0f;

        public float Boost
        {
            get { return boost; }
            set { boost = value; }
        }

        public override void Clear()
        {
            boost = 1.0f;
        }

        public override void CopyTo(Util.Attribute target)
        {
            ((BoostAttribute)target).Boost = boost;
        }
    }
}