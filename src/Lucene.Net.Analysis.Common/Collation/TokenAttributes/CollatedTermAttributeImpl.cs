// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Util;
using System;
using System.Globalization;

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
    public class CollatedTermAttribute : CharTermAttribute
    {
        private readonly CompareInfo collator;
        private readonly CompareOptions options;

        /// <summary>
        /// Create a new <see cref="CollatedTermAttribute"/> </summary>
        /// <param name="collator"> Collation key generator </param>
        public CollatedTermAttribute(CompareInfo collator)
            : this(collator, CompareOptions.None)
        {
        }

        /// <summary>
        /// Create a new <see cref="CollatedTermAttribute"/> </summary>
        /// <param name="collator"> Collation key generator </param>
        /// <param name="options"> Collation options that control the collation strength and
        /// Unicode normalization (decomposition) of the generated sort key. </param>
        public CollatedTermAttribute(CompareInfo collator, CompareOptions options)
        {
            // LUCENENET: Unlike java.text.Collator, System.Globalization.CompareInfo is
            // immutable and thread-safe, so there is no need to clone it here.
            this.collator = collator;
            this.options = options;
        }

        public override void FillBytesRef()
        {
            BytesRef bytes = this.BytesRef;
#if FEATURE_COMPAREINFO_SPAN_SORTKEY
            // LUCENENET: On .NET 5+ with ICU, CompareInfo has a ReadOnlySpan<char> overload of GetSortKey
            // that writes directly into a destination buffer, so we can generate the sort key from the term
            // text without the intermediate string allocation. The NLS backend does not normalize internally
            // (see Normalize), so we still need the slow path when the app is configured for NLS.
            if (CollationUtil.IsICU)
            {
                ReadOnlySpan<char> source = this.AsSpan();
                int keyLength = this.collator.GetSortKeyLength(source, this.options);
                bytes.Bytes = new byte[keyLength];
                bytes.Offset = 0;
                bytes.Length = this.collator.GetSortKey(source, bytes.Bytes, this.options);
                return;
            }
#endif
            byte[] keyData = this.collator.GetSortKey(Normalize(this.ToString()), this.options).KeyData;
            bytes.Bytes = keyData;
            bytes.Offset = 0;
            bytes.Length = keyData.Length;
        }

#if FEATURE_COMPAREINFO_SPAN_SORTKEY
        // LUCENENET: Returns the term text as a span over the backing buffer, avoiding a string allocation.
        private ReadOnlySpan<char> AsSpan() => this.Buffer.AsSpan(0, this.Length);
#endif

        // LUCENENET: Normalize the term to Unicode Normalization Form C (NFC) before generating the
        // sort key. ICU-backed collators (.NET 5+) normalize internally, but the NLS-backed collator
        // (.NET Framework) does not fully handle decomposed combining sequences (e.g. "I" + U+0307 vs
        // the precomposed "İ"), so normalizing here keeps sort keys consistent across globalization
        // backends. This is a no-op for text that is already normalized.
        internal static string Normalize(string s)
            => s.IsNormalized() ? s : s.Normalize();
    }
}
