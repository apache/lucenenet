using System;

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

    using Attribute = Lucene.Net.Util.Attribute;
    using IAttribute = Lucene.Net.Util.IAttribute;

    /// <summary>
    /// Default implementation of <see cref="IPositionIncrementAttribute"/>. </summary>
    public class PositionIncrementAttribute : Attribute, IPositionIncrementAttribute // LUCENENET specific: Not implementing ICloneable per Microsoft's recommendation
    {
        private int positionIncrement = 1;

        /// <summary>
        /// Initialize this attribute with position increment of 1 </summary>
        public PositionIncrementAttribute()
        {
        }

        public virtual int PositionIncrement
        {
            get => positionIncrement;
            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(PositionIncrement), "Increment must be zero or greater: got " + value); // LUCENENET specific - changed from IllegalArgumentException to ArgumentOutOfRangeException (.NET convention)
                }
                this.positionIncrement = value;
            }
        }

        public override void Clear()
        {
            this.positionIncrement = 1;
        }

        public override bool Equals(object other)
        {
            if (other == this)
            {
                return true;
            }

            if (other is PositionIncrementAttribute _other)
            {
                return positionIncrement == _other.positionIncrement;
            }

            return false;
        }

        public override int GetHashCode()
        {
            return positionIncrement;
        }

        public override void CopyTo(IAttribute target) // LUCENENET specific - intentionally expanding target to use IAttribute rather than Attribute
        {
            // LUCENENET: Added guard clauses
            if (target is null)
                throw new ArgumentNullException(nameof(target));
            if (target is not IPositionIncrementAttribute t)
                throw new ArgumentException($"Argument type {target.GetType().FullName} must implement {nameof(IPositionIncrementAttribute)}", nameof(target));
            t.PositionIncrement = positionIncrement;
        }
    }
}