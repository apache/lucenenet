// Lucene version compatibility level 8.6.1
using ICU4N;
using ICU4N.Globalization;
using ICU4N.Text;

namespace Lucene.Net.Analysis.Icu.Segmentation
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
    /// An internal <see cref="BreakIterator"/> for multilingual text, following recommendations
    /// from: UAX #29: Unicode Text Segmentation. (http://unicode.org/reports/tr29/)
    /// <para/>
    /// See http://unicode.org/reports/tr29/#Tailoring for the motivation of this
    /// design.
    /// <para/>
    /// Text is first divided into script boundaries. The processing is then
    /// delegated to the appropriate break iterator for that specific script.
    /// <para/>
    /// This break iterator also allows you to retrieve the ISO 15924 script code
    /// associated with a piece of text.
    /// <para/>
    /// See also UAX #29, UTR #24
    /// <para/>
    /// @lucene.experimental
    /// </summary>
    internal sealed class CompositeBreakIterator
    {
        private readonly ICUTokenizerConfig config;
        private readonly BreakIteratorWrapper[] wordBreakers = new BreakIteratorWrapper[1 + UChar.GetIntPropertyMaxValue(UProperty.Script)];

        private BreakIteratorWrapper rbbi;
        private readonly ScriptIterator scriptIterator;

        private char[] text;

        public CompositeBreakIterator(ICUTokenizerConfig config)
        {
            this.config = config;
            this.scriptIterator = new ScriptIterator(config.CombineCJ);
        }

        /// <summary>
        /// Retrieve the next break position. If the RBBI range is exhausted within the
        /// script boundary, examine the next script boundary.
        /// </summary>
        /// <returns>The next break position or <see cref="BreakIterator.Done"/>.</returns>
        public int Next()
        {
            int next = rbbi.Next();
            while (next == BreakIterator.Done && scriptIterator.Next())
            {
                rbbi = GetBreakIterator(scriptIterator.ScriptCode);
                rbbi.SetText(text, scriptIterator.ScriptStart,
                    scriptIterator.ScriptLimit - scriptIterator.ScriptStart);
                next = rbbi.Next();
            }
            return (next == BreakIterator.Done) ? BreakIterator.Done : next
                + scriptIterator.ScriptStart;
        }

        /// <summary>
        /// Gets the current break position. Returns the current break position or <see cref="BreakIterator.Done"/>.
        /// </summary>
        public int Current
        {
            get
            {
                int current = rbbi.Current;
                return (current == BreakIterator.Done) ? BreakIterator.Done : current
                    + scriptIterator.ScriptStart;
            }
        }

        /// <summary>
        /// Gets the rule status code (token type) from the underlying break
        /// iterator. See <see cref="RuleBasedBreakIterator"/> constants.
        /// </summary>
        public int RuleStatus => rbbi.RuleStatus;

        /// <summary>
        /// Gets the <see cref="UScript"/> script code for the current token. This code can be
        /// decoded with <see cref="UScript"/> into a name or ISO 15924 code.
        /// </summary>
        public int ScriptCode => scriptIterator.ScriptCode;

        /// <summary>
        /// Set a new region of text to be examined by this iterator.
        /// </summary>
        /// <param name="text">Buffer of text.</param>
        /// <param name="start">Offset into buffer.</param>
        /// <param name="length">Maximum length to examine.</param>
        public void SetText(char[] text, int start, int length)
        {
            this.text = text;
            scriptIterator.SetText(text, start, length);
            if (scriptIterator.Next())
            {
                rbbi = GetBreakIterator(scriptIterator.ScriptCode);
                rbbi.SetText(text, scriptIterator.ScriptStart,
                    scriptIterator.ScriptLimit - scriptIterator.ScriptStart);
            }
            else
            {
                rbbi = GetBreakIterator(UScript.Common);
                rbbi.SetText(text, 0, 0);
            }
        }

        private BreakIteratorWrapper GetBreakIterator(int scriptCode)
        {
            if (wordBreakers[scriptCode] is null)
                wordBreakers[scriptCode] = new BreakIteratorWrapper(config.GetBreakIterator(scriptCode));
            return wordBreakers[scriptCode];
        }
    }
}
