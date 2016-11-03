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
    /// Default implementation of <seealso cref="PositionLengthAttribute"/>. </summary>
    public class PositionLengthAttribute : Attribute, IPositionLengthAttribute
    {
        private int positionLength = 1;

        /// <summary>
        /// Initializes this attribute with position length of 1. </summary>
        public PositionLengthAttribute()
        {
        }

        public virtual int PositionLength
        {
            set
            {
                if (value < 1)
                {
                    throw new System.ArgumentException("Position length must be 1 or greater: got " + value);
                }
                this.positionLength = value;
            }
            get
            {
                return positionLength;
            }
        }

        public override void Clear()
        {
            this.positionLength = 1;
        }

        public override bool Equals(object other)
        {
            if (other == this)
            {
                return true;
            }

            if (other is PositionLengthAttribute)
            {
                PositionLengthAttribute _other = (PositionLengthAttribute)other;
                return positionLength == _other.positionLength;
            }

            return false;
        }

        public override int GetHashCode()
        {
            return positionLength;
        }

        public override void CopyTo(Attribute target)
        {
            PositionLengthAttribute t = (PositionLengthAttribute)target;
            t.PositionLength = positionLength;
        }
    }
}