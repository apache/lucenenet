// lucene version compatibility level: 4.8.1
using ICU4N.Text;
using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Support;
using Lucene.Net.Util;

namespace Lucene.Net.Collation.TokenAttributes
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
    /// Extension of <see cref="CharTermAttribute"/> that encodes the term
    /// text as a binary Unicode collation key instead of as UTF-8 bytes.
    /// </summary>
    [ExceptionToClassNameConvention]
    public class ICUCollatedTermAttribute : CharTermAttribute
    {
        private readonly Collator collator;
        private readonly RawCollationKey key = new RawCollationKey();

        /// <summary>
        /// Create a new ICUCollatedTermAttribute
        /// </summary>
        /// <param name="collator">Collation key generator.</param>
        public ICUCollatedTermAttribute(Collator collator)
        {
            // clone the collator: see http://userguide.icu-project.org/collation/architecture
            this.collator = (Collator)collator.Clone();
        }

        public override void FillBytesRef()
        {
            BytesRef bytes = this.BytesRef;
            collator.GetRawCollationKey(ToString(), key);
            bytes.Bytes = key.Bytes;
            bytes.Offset = 0;
            bytes.Length = key.Length;
        }
    }
}
