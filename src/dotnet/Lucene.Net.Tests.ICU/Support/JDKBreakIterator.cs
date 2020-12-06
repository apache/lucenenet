using ICU4N.Text;
using J2N;
using System.Globalization;
using System.IO;

namespace Lucene.Net.Support
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
    /// Static methods to create <see cref="BreakIterator"/> instances that behave (somewhat) like the JDK.
    /// It is recommended to either use the default ICU <see cref="BreakIterator"/> methods instead of these
    /// or else use the <see cref="RuleBasedBreakIterator.GetInstanceFromCompiledRules(Stream)"/> or the 
    /// <see cref="RuleBasedBreakIterator.RuleBasedBreakIterator(string)"/> constructor to create a <see cref="BreakIterator"/>
    /// for the specific context it is used in rather than using these methods.
    /// </summary>
    public static class JdkBreakIterator
    {
        private static readonly RuleBasedBreakIterator SentenceInstance = LoadBreakRules("jdksent.brk"); // LUCENENET: CA1810: Initialize reference type static fields inline
        private static readonly RuleBasedBreakIterator WordInstance = LoadBreakRules("jdkword.brk"); // LUCENENET: CA1810: Initialize reference type static fields inline

        private static RuleBasedBreakIterator LoadBreakRules(string fileName)
        {
            using Stream @is = typeof(JdkBreakIterator).FindAndGetManifestResourceStream(fileName);
            return RuleBasedBreakIterator.GetInstanceFromCompiledRules(@is);
        }

        /// <summary>
        /// Returns a <see cref="BreakIterator"/> that ignores newline characters and
        /// breaks on sentences that do not start with capital letters
        /// similar to the JDK, but otherwise has the default word break functionality
        /// described at <a href="http://userguide.icu-project.org/boundaryanalysis">http://userguide.icu-project.org/boundaryanalysis</a>.
        /// </summary>
        /// <remarks>
        /// NOTE: If the culture is Thai, Lao, Burmese, Khmer, Japanese, Korean, or Chinese,
        /// the instance returned has the same dictionary-based <see cref="BreakIterator"/> behavior
        /// as if you call <see cref="BreakIterator.GetWordInstance(CultureInfo)"/>. See the 
        /// section titled "Details about Dictionary-Based Break Iteration" at
        /// <a href="http://userguide.icu-project.org/boundaryanalysis">http://userguide.icu-project.org/boundaryanalysis</a>.
        /// </remarks>
        /// <param name="culture">The culture of the <see cref="BreakIterator"/> instance to return.</param>
        /// <returns>A sentence <see cref="BreakIterator"/> instance.</returns>
        public static BreakIterator GetSentenceInstance(CultureInfo culture)
        {
            switch (culture.TwoLetterISOLanguageName)
            {
                case "th": // Thai
                case "lo": // Lao
                case "my": // Burmese
                case "km": // Khmer
                case "ja": // Japanese
                case "ko": // Korean
                case "zh": // Chinese
                    return BreakIterator.GetSentenceInstance(culture);
            }

            return SentenceInstance;
        }

        /// <summary>
        /// Returns a <see cref="BreakIterator"/> that breaks on hyphens
        /// similar to the JDK, but otherwise has the default word break functionality
        /// described at <a href="http://userguide.icu-project.org/boundaryanalysis">http://userguide.icu-project.org/boundaryanalysis</a>.
        /// </summary>
        /// <remarks>
        /// NOTE: If the culture is Thai, Lao, Burmese, Khmer, Japanese, Korean, or Chinese,
        /// the instance returned has the same dictionary-based <see cref="BreakIterator"/> behavior
        /// as if you call <see cref="BreakIterator.GetWordInstance(CultureInfo)"/>. See the 
        /// section titled "Details about Dictionary-Based Break Iteration" at
        /// <a href="http://userguide.icu-project.org/boundaryanalysis">http://userguide.icu-project.org/boundaryanalysis</a>.
        /// </remarks>
        /// <param name="culture">The culture of the <see cref="BreakIterator"/> instance to return.</param>
        /// <returns>A word <see cref="BreakIterator"/> instance.</returns>
        public static BreakIterator GetWordInstance(CultureInfo culture)
        {
            switch (culture.TwoLetterISOLanguageName)
            {
                case "th": // Thai
                case "lo": // Lao
                case "my": // Burmese
                case "km": // Khmer
                case "ja": // Japanese
                case "ko": // Korean
                case "zh": // Chinese
                    return BreakIterator.GetWordInstance(culture);
            }

            return WordInstance;
        }
    }
}
