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

//using System;
//using System.Collections.Immutable;
//using System.Text.RegularExpressions;
//using Microsoft.DocAsCode.MarkdownLite;

//namespace LuceneDocsPlugins
//{
//    public class EnvironmentVariableInlineRule : IMarkdownRule
//    {
//        // give it a name
//        public string Name => "EnvVarToken";

//        // define my regex to match
//        private static readonly Regex _envVarRegex = new Regex(@"^\[EnvVar:(\w+?)\]", RegexOptions.Compiled);

//        // process the match
//        public IMarkdownToken TryMatch(IMarkdownParser parser, IMarkdownParsingContext context)
//        {
//            // TODO: This does not process this token from within a code block like

//            // ```
//            // dotnet tool install lucene-cli -g --version [EnvVar: LuceneNetVersion]
//            // ```

//            var match = _envVarRegex.Match(context.CurrentMarkdown);
//            if (match.Length == 0) return null;

//            var envVar = match.Groups[1].Value;
//            var text = Environment.GetEnvironmentVariable(envVar);
//            if (text == null) return null;

//            // 'eat' the characters of the current markdown token so they anr
//            var sourceInfo = context.Consume(match.Length);
//            return new MarkdownTextToken(this, parser.Context, text, sourceInfo);
//        }
//    }
//}