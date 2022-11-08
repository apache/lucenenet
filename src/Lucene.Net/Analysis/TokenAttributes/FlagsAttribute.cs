﻿namespace Lucene.Net.Analysis.TokenAttributes
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
    /// Default implementation of <see cref="IFlagsAttribute"/>. </summary>
    public class FlagsAttribute : Attribute, IFlagsAttribute // LUCENENET specific: Not implementing ICloneable per Microsoft's recommendation
    {
        private int flags = 0;

        /// <summary>
        /// Initialize this attribute with no bits set </summary>
        public FlagsAttribute()
        {
        }

        public virtual int Flags
        {
            get => flags;
            set => this.flags = value;
        }

        public override void Clear()
        {
            flags = 0;
        }

        public override bool Equals(object other)
        {
            if (this == other)
            {
                return true;
            }

            if (other is FlagsAttribute flagsAttribute)
            {
                return flagsAttribute.flags == flags;
            }

            return false;
        }

        public override int GetHashCode()
        {
            return flags;
        }

        public override void CopyTo(IAttribute target)
        {
            FlagsAttribute t = (FlagsAttribute)target;
            t.Flags = flags;
        }
    }
}