// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.CharFilters;
using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace Lucene.Net.Analysis.Pattern
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
    /// <see cref="CharFilter"/> that uses a regular expression for the target of replace string.
    /// The pattern match will be done in each "block" in char stream.
    /// 
    /// <para>
    /// ex1) source="aa  bb aa bb", pattern="(aa)\\s+(bb)" replacement="$1#$2"
    /// output="aa#bb aa#bb"
    /// </para>
    /// 
    /// NOTE: If you produce a phrase that has different length to source string
    /// and the field is used for highlighting for a term of the phrase, you will
    /// face a trouble.
    /// 
    /// <para>
    /// ex2) source="aa123bb", pattern="(aa)\\d+(bb)" replacement="$1 $2"
    /// output="aa bb"
    /// and you want to search bb and highlight it, you will get
    /// highlight snippet="aa1&lt;em&gt;23bb&lt;/em&gt;"
    /// </para>
    /// 
    /// @since Solr 1.5
    /// </summary>
    public class PatternReplaceCharFilter : BaseCharFilter
    {
        [Obsolete]
        public const int DEFAULT_MAX_BLOCK_CHARS = 10000;

        private readonly Regex pattern;
        private readonly string replacement;
        private TextReader transformedInput;

        public PatternReplaceCharFilter(Regex pattern, string replacement, TextReader @in)
            : base(@in)
        {
            this.pattern = pattern;
            this.replacement = replacement;
        }

        [Obsolete]
        public PatternReplaceCharFilter(Regex pattern, string replacement, int maxBlockChars, string blockDelimiter, TextReader @in)
            : this(pattern, replacement, @in)
        {
        }

        public override int Read(char[] cbuf, int off, int len)
        {
            // Buffer all input on the first call.
            if (transformedInput is null)
            {
                Fill();
            }

            return transformedInput.Read(cbuf, off, len);
        }

        private void Fill()
        {
            StringBuilder buffered = new StringBuilder();
            char[] temp = new char[1024];
            for (int cnt = m_input.Read(temp, 0, temp.Length); cnt > 0; cnt = m_input.Read(temp, 0, temp.Length))
            {
                buffered.Append(temp, 0, cnt);
            }
            transformedInput = new StringReader(ProcessPattern(buffered)); // LUCENENET specific: ProcessPattern already returns string, no need to call ToString() on it
        }

        public override int Read()
        {
            if (transformedInput is null)
            {
                Fill();
            }

            return transformedInput.Read();
        }

        protected override int Correct(int currentOff)
        {
            return Math.Max(0, base.Correct(currentOff));
        }

        /// <summary>
        /// Replace pattern in input and mark correction offsets. 
        /// </summary>
        private string ProcessPattern(StringBuilder input)
        {
            // LUCENENET TODO: Replacing characters in a StringBuilder via regex is not natively
            // supported in .NET, so this is the approach we are left with.
            // At some point it might make sense to try to port the
            // MONO implementation over that DOES support Regex on a StringBuilder.
            // Or alternatively port over the Java implementation.

            string inputStr = input.ToString();
            Match m = pattern.Match(inputStr);

            StringBuilder cumulativeOutput = new StringBuilder();
            int cumulative = 0;
            int lastMatchEnd = 0;

            if (m.Success)
            {
                do
                {
                    int skippedSize = m.Index - lastMatchEnd;
                    int lengthBeforeReplacement = cumulativeOutput.Length + skippedSize;

                    // Add the part that didn't match the regex
#if FEATURE_STRINGBUILDER_APPEND_READONLYSPAN
                    cumulativeOutput.Append(inputStr.AsSpan(lastMatchEnd, m.Index - lastMatchEnd));
#else
                    cumulativeOutput.Append(inputStr.Substring(lastMatchEnd, m.Index - lastMatchEnd));
#endif

                    int groupSize = m.Length;
                    lastMatchEnd = m.Index + m.Length;

                    // Do the actual replacement.
                    cumulativeOutput.Append(pattern.Replace(m.Value, replacement, 1));

                    // Calculate how many characters have been appended before the replacement.
                    // Skipped characters have been added as part of appendReplacement.
                    int replacementSize = cumulativeOutput.Length - lengthBeforeReplacement;

                    if (groupSize != replacementSize)
                    {
                        if (replacementSize < groupSize)
                        {
                            // The replacement is smaller. 
                            // Add the 'backskip' to the next index after the replacement (this is possibly 
                            // after the end of string, but it's fine -- it just means the last character 
                            // of the replaced block doesn't reach the end of the original string.
                            cumulative += groupSize - replacementSize;
                            int atIndex = lengthBeforeReplacement + replacementSize;
                            // System.err.println(atIndex + "!" + cumulative);
                            AddOffCorrectMap(atIndex, cumulative);
                        }
                        else
                        {
                            // The replacement is larger. Every new index needs to point to the last
                            // element of the original group (if any).
                            for (int i = groupSize; i < replacementSize; i++)
                            {
                                AddOffCorrectMap(lengthBeforeReplacement + i, --cumulative);
                                // System.err.println((lengthBeforeReplacement + i) + " " + cumulative);
                            }
                        }
                    }

                } while ((m = m.NextMatch()).Success);

                // Append the remaining output, no further changes to indices.
#if FEATURE_STRINGBUILDER_APPEND_READONLYSPAN
                cumulativeOutput.Append(inputStr.AsSpan(lastMatchEnd, input.Length - lastMatchEnd));
#else
                cumulativeOutput.Append(inputStr.Substring(lastMatchEnd, input.Length - lastMatchEnd));
#endif
                return cumulativeOutput.ToString();
            }

            // No match - just return the original string.
            return inputStr; // LUCENENET: Since we have already dumped the string from input, just return it directly.
        }
    }
}