// Lucene version compatibility level 7.1.0
using ICU4N.Text;
using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Support;
using System.Text;

namespace Lucene.Net.Analysis.Icu
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
    /// Normalize token text with ICU's <see cref="Normalizer2"/>.
    /// </summary>
    /// <remarks>
    /// With this filter, you can normalize text in the following ways:
    /// <list type="bullet">
    ///     <item><description>NFKC Normalization, Case Folding, and removing Ignorables (the default)</description></item>
    ///     <item><description>Using a standard Normalization mode (NFC, NFD, NFKC, NFKD)</description></item>
    ///     <item><description>Based on rules from a custom normalization mapping.</description></item>
    /// </list>
    /// <para/>
    /// If you use the defaults, this filter is a simple way to standardize Unicode text
    /// in a language-independent way for search:
    /// <list type="bullet">
    ///     <item><description>
    ///         The case folding that it does can be seen as a replacement for
    ///         LowerCaseFilter: For example, it handles cases such as the Greek sigma, so that
    ///         "Μάϊος" and "ΜΆΪΟΣ" will match correctly.
    ///     </description></item>
    ///     <item><description>
    ///         The normalization will standardizes different forms of the same 
    ///         character in Unicode. For example, CJK full-width numbers will be standardized
    ///         to their ASCII forms.
    ///     </description></item>
    ///     <item><description>
    ///         Ignorables such as Zero-Width Joiner and Variation Selectors are removed.
    ///         These are typically modifier characters that affect display.
    ///     </description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="Normalizer2"/>
    /// <seealso cref="FilteredNormalizer2"/>
    [ExceptionToClassNameConvention]
    public class ICUNormalizer2Filter : TokenFilter
    {
        private readonly ICharTermAttribute termAtt;
        private readonly Normalizer2 normalizer;
        private readonly StringBuilder buffer = new StringBuilder();

        /// <summary>
        /// Create a new <see cref="ICUNormalizer2Filter"/> that combines NFKC normalization, Case
        /// Folding, and removes Default Ignorables (NFKC_Casefold)
        /// </summary>
        /// <param name="input"></param>
        public ICUNormalizer2Filter(TokenStream input)
            : this(input, Normalizer2.GetInstance(null, "nfkc_cf", Normalizer2Mode.Compose))
        {
        }

        /// <summary>
        /// Create a new <see cref="ICUNormalizer2Filter"/> with the specified <see cref="Normalizer2"/>
        /// </summary>
        /// <param name="input">stream</param>
        /// <param name="normalizer">normalizer to use</param>
        public ICUNormalizer2Filter(TokenStream input, Normalizer2 normalizer)
            : base(input)
        {
            this.normalizer = normalizer;
            this.termAtt = AddAttribute<ICharTermAttribute>();
        }

        public override sealed bool IncrementToken()
        {
            if (m_input.IncrementToken())
            {
                if (normalizer.QuickCheck(termAtt) != QuickCheckResult.Yes)
                {
                    buffer.Length = 0;
                    normalizer.Normalize(termAtt, buffer);
                    termAtt.SetEmpty().Append(buffer);
                }
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
