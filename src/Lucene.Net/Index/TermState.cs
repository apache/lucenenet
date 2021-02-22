using System;

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
    /// Encapsulates all required internal state to position the associated
    /// <see cref="TermsEnum"/> without re-seeking.
    /// <para/>
    /// @lucene.experimental
    /// </summary>
    /// <seealso cref="TermsEnum.SeekExact(Lucene.Net.Util.BytesRef, TermState)"/>
    /// <seealso cref="TermsEnum.GetTermState()"/>
    public abstract class TermState // LUCENENET specific: Not implementing ICloneable per Microsoft's recommendation
    {
        /// <summary>
        /// Sole constructor. (For invocation by subclass
        /// constructors, typically implicit.)
        /// </summary>
        protected TermState()
        {
        }

        /// <summary>
        /// Copies the content of the given <see cref="TermState"/> to this instance
        /// </summary>
        /// <param name="other">
        ///          the <see cref="TermState"/> to copy </param>
        public abstract void CopyFrom(TermState other);

        public virtual object Clone()
        {
            return (TermState)base.MemberwiseClone();
        }

        public override string ToString()
        {
            return "TermState";
        }
    }
}