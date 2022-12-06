﻿using System;

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

    using Attribute = Lucene.Net.Util.Attribute;
    using IAttribute = Lucene.Net.Util.IAttribute;
    using BytesRef = Lucene.Net.Util.BytesRef;

    /// <summary>
    /// Implementation class for <see cref="IMaxNonCompetitiveBoostAttribute"/>.
    /// <para/>
    /// @lucene.internal
    /// </summary>
    public sealed class MaxNonCompetitiveBoostAttribute : Attribute, IMaxNonCompetitiveBoostAttribute
    {
        private float maxNonCompetitiveBoost = float.NegativeInfinity;
        private BytesRef competitiveTerm = null;

        public float MaxNonCompetitiveBoost
        {
            get => maxNonCompetitiveBoost;
            set => this.maxNonCompetitiveBoost = value;
        }

        public BytesRef CompetitiveTerm
        {
            get => competitiveTerm;
            set => this.competitiveTerm = value;
        }

        public override void Clear()
        {
            maxNonCompetitiveBoost = float.NegativeInfinity;
            competitiveTerm = null;
        }

        public override void CopyTo(IAttribute target) // LUCENENET specific - intentionally expanding target to use IAttribute rather than Attribute
        {
            // LUCENENET: Added guard clauses
            if (target is null)
                throw new ArgumentNullException(nameof(target));
            if (target is not IMaxNonCompetitiveBoostAttribute t)
                throw new ArgumentException($"Argument type {target.GetType().FullName} must implement {nameof(IMaxNonCompetitiveBoostAttribute)}", nameof(target));
            t.MaxNonCompetitiveBoost = maxNonCompetitiveBoost;
            t.CompetitiveTerm = competitiveTerm;
        }
    }
}