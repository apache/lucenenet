using System;
using System.Text;
using System.Text.RegularExpressions;

namespace LuceneDocsPlugins
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

    public class EnvironmentVariableUtil
    {
        public const string EnvVarRegexString = @"[\<\[]EnvVar\:(\w+?)[\>\]]";
        public static readonly Regex EnvVarRegex = new Regex("^" + EnvVarRegexString, RegexOptions.Compiled);
        public static readonly Regex EnvVarRegexInline = new Regex(EnvVarRegexString, RegexOptions.Compiled | RegexOptions.Multiline);


        public static string ReplaceEnvironmentVariables(string sourceText, Match match)
            => ReplaceEnvironmentVariables(sourceText, match, reuse: null);

        public static string ReplaceEnvironmentVariables(string sourceText, Match match, StringBuilder reuse)
        {
            if (match.Success)
            {
                if (reuse is null)
                    reuse = new StringBuilder();
                else
                    reuse.Clear();

                int lastMatchEnd = 0;

                do
                {
                    var envVar = match.Groups[1].Value;

                    // Append the prefix that didn't match the regex (or text since last match)
                    reuse.Append(sourceText.Substring(lastMatchEnd, match.Index - lastMatchEnd));

                    // Do the replacement of the regex match
                    reuse.Append(Environment.GetEnvironmentVariable(envVar));

                    lastMatchEnd = match.Index + match.Length;

                } while ((match = match.NextMatch()).Success);

                // Append the suffix that didn't match the regex
                reuse.Append(sourceText.Substring(lastMatchEnd));

                return reuse.ToString();
            }
            return sourceText;
        }
    }
}
