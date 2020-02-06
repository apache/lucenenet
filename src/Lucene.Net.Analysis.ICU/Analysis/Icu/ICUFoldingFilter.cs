// Lucene version compatibility level 7.1.0
using ICU4N.Text;
using J2N;
using Lucene.Net.Support;
using System.Reflection;

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
    /// A <see cref="TokenFilter"/> that applies search term folding to Unicode text,
    /// applying foldings from UTR#30 Character Foldings.
    /// </summary>
    /// <remarks>
    /// This filter applies the following foldings from the report to unicode text:
    /// <list type="bullet">
    ///     <item><description>Accent removal</description></item>
    ///     <item><description>Case folding</description></item>
    ///     <item><description>Canonical duplicates folding</description></item>
    ///     <item><description>Dashes folding</description></item>
    ///     <item><description>Diacritic removal (including stroke, hook, descender)</description></item>
    ///     <item><description>Greek letterforms folding</description></item>
    ///     <item><description>Han Radical folding</description></item>
    ///     <item><description>Hebrew Alternates folding</description></item>
    ///     <item><description>Jamo folding</description></item>
    ///     <item><description>Letterforms folding</description></item>
    ///     <item><description>Math symbol folding</description></item>
    ///     <item><description>Multigraph Expansions: All</description></item>
    ///     <item><description>Native digit folding</description></item>
    ///     <item><description>No-break folding</description></item>
    ///     <item><description>Overline folding</description></item>
    ///     <item><description>Positional forms folding</description></item>
    ///     <item><description>Small forms folding</description></item>
    ///     <item><description>Space folding</description></item>
    ///     <item><description>Spacing Accents folding</description></item>
    ///     <item><description>Subscript folding</description></item>
    ///     <item><description>Superscript folding</description></item>
    ///     <item><description>Suzhou Numeral folding</description></item>
    ///     <item><description>Symbol folding</description></item>
    ///     <item><description>Underline folding</description></item>
    ///     <item><description>Vertical forms folding</description></item>
    ///     <item><description>Width folding</description></item>
    /// </list>
    /// <para/>
    /// Additionally, Default Ignorables are removed, and text is normalized to NFKC.
    /// All foldings, case folding, and normalization mappings are applied recursively
    /// to ensure a fully folded and normalized result.
    /// </remarks>
    [ExceptionToClassNameConvention]
    public sealed class ICUFoldingFilter : ICUNormalizer2Filter
    {
        // TODO: if the wrong version of the ICU jar is used, loading these data files may give a strange error.
        // maybe add an explicit check? http://icu-project.org/apiref/icu4j/com/ibm/icu/util/VersionInfo.html
        private static readonly Normalizer2 normalizer = Normalizer2.GetInstance(
            typeof(ICUFoldingFilter).FindAndGetManifestResourceStream("utr30.nrm"),
            "utr30", Normalizer2Mode.Compose);

        /// <summary>
        /// Create a new <see cref="ICUFoldingFilter"/> on the specified input
        /// </summary>
        public ICUFoldingFilter(TokenStream input)
            : base(input, normalizer)
        {
        }
    }
}
