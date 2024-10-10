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

using Docfx.Common;
using System;
using System.Text;
using System.Text.RegularExpressions;

namespace LuceneDocsPlugins;

public partial class EnvironmentVariableUtil
{
    [GeneratedRegex(@"EnvVar\:(\w+)", RegexOptions.Compiled)]
    private static partial Regex EnvVarRegex();

    public static string ReplaceEnvironmentVariables(string sourceText)
    {
        var matches = EnvVarRegex().Matches(sourceText);

        if (matches.Count > 0)
        {
            var sb = new StringBuilder(sourceText.Length);

            int lastMatchEnd = 0;

            foreach (Match match in matches)
            {
                var envVar = match.Groups[1].Value;

                // Append the prefix that didn't match the regex (or text since last match)
                sb.Append(sourceText.AsSpan(lastMatchEnd, match.Index - lastMatchEnd));

                // Do the replacement of the regex match
                string envVarValue = Environment.GetEnvironmentVariable(envVar);
                sb.Append(envVarValue);

                Logger.LogInfo($"Replaced environment variable '{envVar}' with value '{envVarValue}' on match {match.Value}");
                lastMatchEnd = match.Index + match.Length;
            }

            // Append the suffix that didn't match the regex
            sb.Append(sourceText.AsSpan(lastMatchEnd));

            return sb.ToString();
        }

        return sourceText;
    }
}
