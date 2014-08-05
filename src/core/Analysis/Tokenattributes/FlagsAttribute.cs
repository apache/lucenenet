/* 
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 * 
 * http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using Attribute = Lucene.Net.Util.Attribute;

namespace Lucene.Net.Analysis.Tokenattributes
{
    /// <summary> This attribute can be used to pass different flags down the tokenizer chain,
    /// eg from one TokenFilter to another one. 
    /// </summary>
    [Serializable]
    public class FlagsAttribute : Util.Attribute, IFlagsAttribute, System.ICloneable
    {
        /// <summary> EXPERIMENTAL:  While we think this is here to stay, we may want to change it to be a long.
        /// <p/>
        /// 
        /// Get the bitset for any bits that have been set.  This is completely distinct from <see cref="ITypeAttribute.Type()" />, although they do share similar purposes.
        /// The flags can be used to encode information about the token for use by other <see cref="Lucene.Net.Analysis.TokenFilter" />s.
        /// 
        /// 
        /// </summary>
        /// <value> The bits </value>
        public virtual int Flags { get; set; }

        public override void Clear()
        {
            Flags = 0;
        }

        public override bool Equals(System.Object other)
        {
            if (this == other)
            {
                return true;
            }

            if (other is FlagsAttribute)
            {
                return ((FlagsAttribute) other).Flags == Flags;
            }

            return false;
        }

        public override int GetHashCode()
        {
            return Flags;
        }

        public override void CopyTo(Attribute target)
        {
            var t = (IFlagsAttribute) target;
            t.Flags = Flags;
        }

        public override System.Object Clone()
        {
            var impl = new FlagsAttribute();
            impl.Flags = Flags;
            return impl;
        }

        public override string ToString()
        {
            return "flags=" + this.Flags.ToString();
        }
    }
}