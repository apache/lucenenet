// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Diagnostics;
using Lucene.Net.Util;
using System.Diagnostics;
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
    /// CaptureGroup uses .NET regexes to emit multiple tokens - one for each capture
    /// group in one or more patterns.
    /// 
    /// <para>
    /// For example, a pattern like:
    /// </para>
    /// 
    /// <para>
    /// <c>"(https?://([a-zA-Z\-_0-9.]+))"</c>
    /// </para>
    /// 
    /// <para>
    /// when matched against the string "http://www.foo.com/index" would return the
    /// tokens "https://www.foo.com" and "www.foo.com".
    /// </para>
    /// 
    /// <para>
    /// If none of the patterns match, or if preserveOriginal is true, the original
    /// token will be preserved.
    /// </para>
    /// <para>
    /// Each pattern is matched as often as it can be, so the pattern
    /// <c> "(...)"</c>, when matched against <c>"abcdefghi"</c> would
    /// produce <c>["abc","def","ghi"]</c>
    /// </para>
    /// <para>
    /// A camelCaseFilter could be written as:
    /// </para>
    /// <para>
    /// <code>
    ///   "([A-Z]{2,})",                                 
    ///   "(?&lt;![A-Z])([A-Z][a-z]+)",                     
    ///   "(?:^|\\b|(?&lt;=[0-9_])|(?&lt;=[A-Z]{2}))([a-z]+)", 
    ///   "([0-9]+)"
    /// </code>
    /// </para>
    /// <para>
    /// plus if <see cref="preserveOriginal"/> is true, it would also return
    /// <c>camelCaseFilter</c>
    /// </para>
    /// </summary>
    public sealed class PatternCaptureGroupTokenFilter : TokenFilter
    {
        private readonly ICharTermAttribute charTermAttr;
        private readonly IPositionIncrementAttribute posAttr;
        private State state;
        private readonly Match[] matchers;
        private readonly Regex[] patterns;
        private readonly CharsRef spare = new CharsRef();
        private readonly int[] groupCounts;
        private readonly bool preserveOriginal;
        private int[] currentGroup;
        private int currentMatcher;

        /// <summary>
        /// Creates a new <see cref="PatternCaptureGroupTokenFilter"/>
        /// </summary>
        /// <param name="input">
        ///          the input <see cref="TokenStream"/> </param>
        /// <param name="preserveOriginal">
        ///          set to true to return the original token even if one of the
        ///          patterns matches </param>
        /// <param name="patterns">
        ///          an array of <see cref="Pattern"/> objects to match against each token </param>
        public PatternCaptureGroupTokenFilter(TokenStream input, bool preserveOriginal, params Regex[] patterns) 
            : base(input)
        {
            this.preserveOriginal = preserveOriginal;
            this.matchers = new Match[patterns.Length];
            this.groupCounts = new int[patterns.Length];
            this.currentGroup = new int[patterns.Length];
            this.patterns = patterns;
            for (int i = 0; i < patterns.Length; i++)
            {
                this.groupCounts[i] = patterns[i].GetGroupNumbers().Length;
                this.currentGroup[i] = -1;
                this.matchers[i] = null; // Reset to null so we can tell we are at the head of the chain
            }
            this.charTermAttr = AddAttribute<ICharTermAttribute>();
            this.posAttr = AddAttribute<IPositionIncrementAttribute>();
        }

        private bool NextCapture()
        {
            int min_offset = int.MaxValue;
            currentMatcher = -1;
            Match matcher;

            for (int i = 0; i < matchers.Length; i++)
            {
                if (currentGroup[i] == -1)
                {
                    if (matchers[i] is null)
                        matchers[i] = patterns[i].Match(new string(spare.Chars, spare.Offset, spare.Length)); 
                    else
                        matchers[i] = matchers[i].NextMatch();
                    currentGroup[i] = matchers[i].Success ? 1 : 0;
                }
                matcher = matchers[i];
                if (currentGroup[i] != 0)
                {
                    while (currentGroup[i] < groupCounts[i] + 1)
                    {
                        int start = matcher.Groups[currentGroup[i]].Index;
                        int end = matcher.Groups[currentGroup[i]].Index + matcher.Groups[currentGroup[i]].Length;
                        if (start == end || preserveOriginal && start == 0 && spare.Length == end)
                        {
                            currentGroup[i]++;
                            continue;
                        }
                        if (start < min_offset)
                        {
                            min_offset = start;
                            currentMatcher = i;
                        }
                        break;
                    }
                    if (currentGroup[i] == groupCounts[i] + 1)
                    {
                        currentGroup[i] = -1;
                        i--;
                    }
                }
            }
            return currentMatcher != -1;
        }

        public override bool IncrementToken()
        {
            if (currentMatcher != -1 && NextCapture())
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(state != null);
                ClearAttributes();
                RestoreState(state);
                int start = matchers[currentMatcher].Groups[currentGroup[currentMatcher]].Index;
                int end = matchers[currentMatcher].Groups[currentGroup[currentMatcher]].Index + 
                    matchers[currentMatcher].Groups[currentGroup[currentMatcher]].Length;

                posAttr.PositionIncrement = 0;
                charTermAttr.CopyBuffer(spare.Chars, start, end - start);
                currentGroup[currentMatcher]++;
                return true;
            }

            if (!m_input.IncrementToken())
            {
                return false;
            }

            char[] buffer = charTermAttr.Buffer;
            int length = charTermAttr.Length;
            spare.CopyChars(buffer, 0, length);
            state = CaptureState();

            for (int i = 0; i < matchers.Length; i++)
            {
                matchers[i] = null;
                currentGroup[i] = -1;
            }

            if (preserveOriginal)
            {
                currentMatcher = 0;
            }
            else if (NextCapture())
            {
                int start = matchers[currentMatcher].Groups[currentGroup[currentMatcher]].Index;
                int end = matchers[currentMatcher].Groups[currentGroup[currentMatcher]].Index + 
                    matchers[currentMatcher].Groups[currentGroup[currentMatcher]].Length;

                // if we start at 0 we can simply set the length and save the copy
                if (start == 0)
                {
                    charTermAttr.Length = end;
                }
                else
                {
                    charTermAttr.CopyBuffer(spare.Chars, start, end - start);
                }
                currentGroup[currentMatcher]++;
            }
            return true;

        }

        public override void Reset()
        {
            base.Reset();
            state = null;
            currentMatcher = -1;
        }
    }
}