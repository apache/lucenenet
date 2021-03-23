// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.TokenAttributes;
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
    /// This tokenizer uses regex pattern matching to construct distinct tokens
    /// for the input stream.  It takes two arguments:  "pattern" and "group".
    /// <para/>
    /// <list type="bullet">
    ///     <item><description>"pattern" is the regular expression.</description></item>
    ///     <item><description>"group" says which group to extract into tokens.</description></item>
    /// </list>
    /// <para>
    /// group=-1 (the default) is equivalent to "split".  In this case, the tokens will
    /// be equivalent to the output from (without empty tokens):
    /// <see cref="Regex.Replace(string, string)"/>
    /// </para>
    /// <para>
    /// Using group >= 0 selects the matching group as the token.  For example, if you have:<br/>
    /// <code>
    ///  pattern = \'([^\']+)\'
    ///  group = 0
    ///  input = aaa 'bbb' 'ccc'
    /// </code>
    /// the output will be two tokens: 'bbb' and 'ccc' (including the ' marks).  With the same input
    /// but using group=1, the output would be: bbb and ccc (no ' marks)
    /// </para>
    /// <para>NOTE: This <see cref="Tokenizer"/> does not output tokens that are of zero length.</para>
    /// </summary>
    /// <seealso cref="Regex"/>
    public sealed class PatternTokenizer : Tokenizer
    {
        private readonly ICharTermAttribute termAtt;
        private readonly IOffsetAttribute offsetAtt;

        private readonly StringBuilder str = new StringBuilder();
        private int index;
        private bool isReset = false;

        private readonly int group;
        private Match matcher;
        private readonly Regex pattern;

        /// <summary>
        /// creates a new <see cref="PatternTokenizer"/> returning tokens from group (-1 for split functionality) </summary>
        public PatternTokenizer(TextReader input, Regex pattern, int group)
            : this(AttributeFactory.DEFAULT_ATTRIBUTE_FACTORY, input, pattern, group)
        {
        }

        /// <summary>
        /// creates a new <see cref="PatternTokenizer"/> returning tokens from group (-1 for split functionality) </summary>
        public PatternTokenizer(AttributeFactory factory, TextReader input, Regex pattern, int group)
            : base(factory, input)
        {
            this.termAtt = AddAttribute<ICharTermAttribute>();
            this.offsetAtt = AddAttribute<IOffsetAttribute>();
            this.group = group;

            // Use "" instead of str so don't consume chars
            // (fillBuffer) from the input on throwing IAE below:
            this.matcher = pattern.Match("");
            this.pattern = pattern;

            // confusingly group count depends ENTIRELY on the pattern but is only accessible via matcher
            var groupCount = pattern.GetGroupNumbers().Length;
            if (group >= 0 && group > groupCount)
            {
                throw new ArgumentException("invalid group specified: pattern only has: " + groupCount + " capturing groups");
            }

        }

        public override bool IncrementToken()
        {
            if (index >= str.Length)
            {
                return false;
            }
            ClearAttributes();

            if (group >= 0)
            {

                // match a specific group
                if (matcher.Success)
                {
                    do
                    {
                        // We have alredy parsed from this index, go to the next token.
                        if (!isReset && matcher.Groups[group].Index == index)
                        {
                            continue;
                        }
                        isReset = false;

                        index = matcher.Groups[group].Index;
                        int endIndex = matcher.Groups[group].Index + matcher.Groups[group].Length;

                        if (index == endIndex)
                        {
                            continue;
                        }

                        termAtt.SetEmpty().Append(str.ToString(), index, endIndex - index); // LUCENENET: Corrected 3rd parameter
                        offsetAtt.SetOffset(CorrectOffset(index), CorrectOffset(endIndex));
                        return true;

                    } while ((matcher = matcher.NextMatch()).Success);
                }


                index = int.MaxValue; // mark exhausted
                return false;

            }
            else
            {

                // String.split() functionality
                if (matcher.Success)
                {
                    do
                    {
                        if (matcher.Index - index > 0)
                        {
                            // found a non-zero-length token
                            termAtt.SetEmpty().Append(str.ToString(), index, matcher.Index - index); // LUCENENET: Corrected 3rd parameter
                            offsetAtt.SetOffset(CorrectOffset(index), CorrectOffset(matcher.Index));
                            index = matcher.Index + matcher.Length;
                            return true;
                        }

                        isReset = false;
                        index = matcher.Index + matcher.Length;
                    } while ((matcher = matcher.NextMatch()).Success);
                }

                if (str.Length - index == 0)
                {
                    index = int.MaxValue; // mark exhausted
                    return false;
                }

                termAtt.SetEmpty().Append(str.ToString(), index, str.Length - index); // LUCENENET: Corrected 3rd parameter
                offsetAtt.SetOffset(CorrectOffset(index), CorrectOffset(str.Length));
                index = int.MaxValue; // mark exhausted
                return true;
            }
        }

        public override void End()
        {
            base.End();
            int ofs = CorrectOffset(str.Length);
            offsetAtt.SetOffset(ofs, ofs);
        }

        public override void Reset()
        {
            base.Reset();
            FillBuffer(str, m_input);

            // LUCENENET: Since we need to "reset" the Match
            // object, we also need an "isReset" flag to indicate
            // whether we are at the head of the match and to 
            // take the appropriate measures to ensure we don't 
            // overwrite our matcher variable with 
            // matcher = matcher.NextMatch();
            // before it is time. A string could potentially
            // match on index 0, so we need another variable to
            // manage this state.
            matcher = pattern.Match(str.ToString());
            isReset = true;
            index = 0;
        }

        // TODO: we should see if we can make this tokenizer work without reading
        // the entire document into RAM, perhaps with Matcher.hitEnd/requireEnd ?
        private readonly char[] buffer = new char[8192];

        private void FillBuffer(StringBuilder sb, TextReader input)
        {
            int len;
            sb.Length = 0;
            while ((len = input.Read(buffer, 0, buffer.Length)) > 0)
            {
                sb.Append(buffer, 0, len);
            }
        }
    }
}