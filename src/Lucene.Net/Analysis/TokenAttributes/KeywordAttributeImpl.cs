using Lucene.Net.Util;
using System;
using Attribute = Lucene.Net.Util.Attribute;

namespace Lucene.Net.Analysis.TokenAttributes
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
    /// Default implementation of <see cref="IKeywordAttribute"/>. </summary>
    public sealed class KeywordAttribute : Attribute, IKeywordAttribute
    {
        private bool keyword;

        /// <summary>
        /// Initialize this attribute with the keyword value as false. </summary>
        public KeywordAttribute()
        {
        }

        public override void Clear()
        {
            keyword = false;
        }

        public override void CopyTo(IAttribute target) // LUCENENET specific - intentionally expanding target to use IAttribute rather than Attribute
        {
            // LUCENENET: Added guard clauses
            if (target is null)
                throw new ArgumentNullException(nameof(target));
            if (target is not IKeywordAttribute attr)
                throw new ArgumentException($"Argument type {target.GetType().FullName} must implement {nameof(IKeywordAttribute)}", nameof(target));
            attr.IsKeyword = keyword;
        }

        public override int GetHashCode()
        {
            return keyword ? 31 : 37;
        }

        public override bool Equals(object obj)
        {
            if (this == obj)
            {
                return true;
            }
            if (this.GetType() != obj.GetType())
            {
                return false;
            }
            KeywordAttribute other = (KeywordAttribute)obj;
            return keyword == other.keyword;
        }

        public bool IsKeyword
        {
            get => keyword;
            set => keyword = value;
        }
    }
}