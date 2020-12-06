using Lucene.Net.Diagnostics;

namespace Lucene.Net.Index
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
    /// An ordinal based <see cref="TermState"/>
    /// <para/>
    /// @lucene.experimental
    /// </summary>
    public class OrdTermState : TermState
    {
        /// <summary>
        /// Term ordinal, i.e. it's position in the full list of
        /// sorted terms.
        /// </summary>
        public long Ord { get; set; }

        /// <summary>
        /// Sole constructor. </summary>
        public OrdTermState()
        {
        }

        public override void CopyFrom(TermState other)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(other is OrdTermState,"can not copy from {0}", other.GetType().Name);
            this.Ord = ((OrdTermState)other).Ord;
        }

        public override string ToString()
        {
            return "OrdTermState ord=" + Ord;
        }
    }
}