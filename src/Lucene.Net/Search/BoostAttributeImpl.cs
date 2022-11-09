using Lucene.Net.Util;
using System;
using Attribute = Lucene.Net.Util.Attribute;

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
    /// Implementation class for <see cref="IBoostAttribute"/>.
    /// <para/>
    /// @lucene.internal
    /// </summary>
    public sealed class BoostAttribute : Attribute, IBoostAttribute
    {
        private float boost = 1.0f;

        /// <summary>
        /// Gets or Sets the boost in this attribute. Default is <c>1.0f</c>.
        /// </summary>
        public float Boost
        {
            get => boost;
            set => boost = value;
        }

        public override void Clear()
        {
            boost = 1.0f;
        }

        public override void CopyTo(IAttribute target) // LUCENENET specific - intentionally expanding target to use IAttribute rather than Attribute
        {
            // LUCENENET: Added guard clauses
            if (target is null)
                throw new ArgumentNullException(nameof(target));
            if (target is not IBoostAttribute t)
                throw new ArgumentException($"Argument type {target.GetType().FullName} must implement {nameof(IBoostAttribute)}", nameof(target));
            t.Boost = boost;
        }
    }
}