using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Search;
using System;
using System.Globalization;

namespace Lucene.Net.QueryParsers.Flexible.Standard
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
    /// Configuration options common across queryparser implementations.
    /// </summary>
    public interface ICommonQueryParserConfiguration
    {
        /// <summary>
        /// Whether terms of multi-term queries (e.g., wildcard,
        /// prefix, fuzzy and range) should be automatically
        /// lower-cased or not.  Default is <c>true</c>.
        /// </summary>
        bool LowercaseExpandedTerms { get; set; }

        /// <summary>
        /// Set to <c>true</c> to allow leading wildcard characters.
        /// <para/>
        /// When set, <c>*</c> or <c>?</c> are allowed as the first
        /// character of a <see cref="PrefixQuery"/> and <see cref="WildcardQuery"/>. Note that this can produce
        /// very slow queries on big indexes.
        /// <para/>
        /// Default: false.
        /// </summary>
        bool AllowLeadingWildcard { get; set; }

        /// <summary>
        /// Set to <c>true</c> to enable position increments in result query.
        /// <para/>
        /// When set, result phrase and multi-phrase queries will be aware of position
        /// increments. Useful when e.g. a <see cref="Analysis.Core.StopFilter"/> increases the position increment
        /// of the token that follows an omitted token.
        /// <para/>
        /// Default: false.
        /// </summary>
        bool EnablePositionIncrements { get; set; }

        /// <summary>
        /// By default, it uses 
        /// <see cref="MultiTermQuery.CONSTANT_SCORE_AUTO_REWRITE_DEFAULT"/> when creating a
        /// prefix, wildcard and range queries. This implementation is generally
        /// preferable because it a) Runs faster b) Does not have the scarcity of terms
        /// unduly influence score c) avoids any exception due to too many listeners.
        /// However, if your application really needs to use the
        /// old-fashioned boolean queries expansion rewriting and the above points are
        /// not relevant then use this change the rewrite method.
        /// </summary>
        MultiTermQuery.RewriteMethod MultiTermRewriteMethod { get; set; }

        /// <summary>
        /// Get or Set the prefix length for fuzzy queries. Default is 0.
        /// </summary>
        int FuzzyPrefixLength { get; set; }

        /// <summary>
        /// Get or Set locale used by date range parsing.
        /// </summary>
        CultureInfo Locale { get; set; }

        /// <summary>
        /// Gets or Sets the time zone.
        /// </summary>
        TimeZoneInfo TimeZone { get; set; }

        /// <summary>
        /// Gets or Sets the default slop for phrases. If zero, then exact phrase matches are
        /// required. Default value is zero.
        /// </summary>
        int PhraseSlop { get; set; }

        Analyzer Analyzer { get; }

        /// <summary>
        /// Get the minimal similarity for fuzzy queries.
        /// </summary>
        float FuzzyMinSim { get; set; }

        /// <summary>
        /// Sets the default <see cref="DateResolution"/> used for certain field when
        /// no <see cref="DateResolution"/> is defined for this field.
        /// </summary>
        void SetDateResolution(DateResolution dateResolution);
    }
}
