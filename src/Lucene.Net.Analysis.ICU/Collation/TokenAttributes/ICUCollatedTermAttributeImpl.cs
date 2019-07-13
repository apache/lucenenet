using Icu.Collation;
using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Support;
using Lucene.Net.Util;
using System;
#if NETSTANDARD
using SortKey = Icu.SortKey;
#else
using SortKey = System.Globalization.SortKey;
#endif

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
    public sealed class ICUCollatedTermAttribute : CharTermAttribute, IDisposable
    {
        private readonly Collator collator;

        /// <summary>
        /// Create a new ICUCollatedTermAttribute
        /// </summary>
        /// <param name="collator"><see cref="SortKey"/> generator.</param>
        public ICUCollatedTermAttribute(Collator collator)
        {
            // clone the collator: see http://userguide.icu-project.org/collation/architecture
            this.collator = (Collator)collator.Clone();
        }

        public override void FillBytesRef()
        {
            BytesRef bytes = this.BytesRef;
            SortKey key = collator.GetSortKey(ToString());
            bytes.Bytes = key.KeyData;
            bytes.Offset = 0;
            bytes.Length = key.KeyData.Length;
        }

        // LUCENENET specific - must dispose collator
        public void Dispose()
        {
            collator.Dispose();
        }
    }
}
