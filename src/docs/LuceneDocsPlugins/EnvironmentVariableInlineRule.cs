/*
 * Licensed to the Apache Software Foundation (ASF) under one
 * or more contributor license agreements.  See the NOTICE file
 * distributed with this work for additional information
 * regarding copyright ownership.  The ASF licenses this file
 * to you under the Apache License, Version 2.0 (the
 * "License"); you may not use this file except in compliance
 * with the License.  You may obtain a copy of the License at
 * 
 *   http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing,
 * software distributed under the License is distributed on an
 * "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
 * KIND, either express or implied.  See the License for the
 * specific language governing permissions and limitations
 * under the License.
 */

using System;
using System.Collections.Immutable;
using System.Text.RegularExpressions;
using Microsoft.DocAsCode.MarkdownLite;

namespace LuceneDocsPlugins
{
    internal static class RegexExtentions
    {
        public static string NotEmpty(this Match match, int index1, int index2)
        {
            if (match.Groups.Count > index1 && !string.IsNullOrEmpty(match.Groups[index1].Value))
            {
                return match.Groups[index1].Value;
            }
            return match.Groups[index2].Value;
        }
    }

    public class EnvironmentVariableInlineRule : IMarkdownRule
    {
        public string Name => "EnvVarToken";
        private static readonly Regex _envVarRegex = new Regex(@"^\[EnvVar:(\w+?)\]", RegexOptions.Compiled);

        public IMarkdownToken TryMatch(IMarkdownParser parser, IMarkdownParsingContext context)
        {           
            var match = _envVarRegex.Match(context.CurrentMarkdown);
            if (match.Length == 0)
            {
                return null;
            }
            var envVar = match.Groups[1].Value;
            var text = Environment.GetEnvironmentVariable(envVar);
            if (text == null)
            {
                return null;
            }
            else
            {
                var sourceInfo = context.Consume(match.Length);
                return new MarkdownTextToken(this, parser.Context, text, sourceInfo);
            }
        }
    }
}