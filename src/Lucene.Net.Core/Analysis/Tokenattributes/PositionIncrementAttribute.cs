using System;

namespace Lucene.Net.Analysis.Tokenattributes
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

    /// <summary>
    /// Default implementation of <seealso cref="PositionIncrementAttribute"/>. </summary>
    public class PositionIncrementAttribute : Attribute, IPositionIncrementAttribute
    {
        private int positionIncrement = 1;

        /// <summary>
        /// Initialize this attribute with position increment of 1 </summary>
        public PositionIncrementAttribute()
        {
        }

        public int PositionIncrement
        {
            set
            {
                if (value < 0)
                {
                    throw new System.ArgumentException("Increment must be zero or greater: got " + value);
                }
                this.positionIncrement = value;
            }
            get
            {
                return positionIncrement;
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

            if (other is PositionIncrementAttribute)
            {
                PositionIncrementAttribute _other = (PositionIncrementAttribute)other;
                return positionIncrement == _other.positionIncrement;
            }

            return false;
        }

        public override int GetHashCode()
        {
            return positionIncrement;
        }

        public override void CopyTo(Attribute target)
        {
            PositionIncrementAttribute t = (PositionIncrementAttribute)target;
            t.PositionIncrement = positionIncrement;
        }
    }
}