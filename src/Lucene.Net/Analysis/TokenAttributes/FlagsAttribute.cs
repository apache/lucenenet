using Lucene.Net.Util;

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

    /// <summary> This attribute can be used to pass different flags down the <see cref="Tokenizer" /> chain,
    /// eg from one TokenFilter to another one.
    /// <para/>
    /// This is completely distinct from <see cref="ITypeAttribute"/>, although they do share similar purposes.
    /// The flags can be used to encode information about the token for use by other 
    /// <see cref="TokenFilter"/>s.
    /// <para/>
    /// @lucene.experimental While we think this is here to stay, we may want to change it to be a long.
    /// </summary>
    public interface IFlagsAttribute : IAttribute
    {
        /// <summary>
        /// Get the bitset for any bits that have been set.
        /// </summary>
        int Flags { get; set; }
    }
}