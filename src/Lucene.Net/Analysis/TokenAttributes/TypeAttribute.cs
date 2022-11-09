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
    /// Default implementation of <see cref="ITypeAttribute"/>. </summary>
    public partial class TypeAttribute : Attribute, ITypeAttribute // LUCENENET specific: Not implementing ICloneable per Microsoft's recommendation
    {
        private string type;

        /// <summary>
        /// Initialize this attribute with <see cref="TypeAttribute.DEFAULT_TYPE"/> </summary>
        public TypeAttribute()
            : this(TypeAttribute.DEFAULT_TYPE)
        {
        }

        /// <summary>
        /// Initialize this attribute with <paramref name="type"/> </summary>
        public TypeAttribute(string type)
        {
            this.type = type;
        }

        public virtual string Type
        {
            get => type;
            set => type = value;
        }

        public override void Clear()
        {
            type = TypeAttribute.DEFAULT_TYPE;
        }

        public override bool Equals(object other)
        {
            if (other == this)
            {
                return true;
            }

            if (other is TypeAttribute o)
            {
                return (this.type is null ? o.type is null : this.type.Equals(o.type, StringComparison.Ordinal));
            }

            return false;
        }

        public override int GetHashCode()
        {
            return (type is null) ? 0 : type.GetHashCode();
        }

        public override void CopyTo(IAttribute target) // LUCENENET specific - intentionally expanding target to use IAttribute rather than Attribute
        {
            ITypeAttribute t = (ITypeAttribute)target;
            t.Type = type;
        }
    }
}