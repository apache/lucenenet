﻿//using Microsoft.DocAsCode.Dfm;
//using Microsoft.DocAsCode.MarkdownLite;
//using System;
//using System.Text;
//using System.Text.RegularExpressions;

//namespace LuceneDocsPlugins
//{
//    // LUCENENET TODO: This is not functional yet

//    /*
//     * Licensed to the Apache Software Foundation (ASF) under one or more
//     * contributor license agreements.  See the NOTICE file distributed with
//     * this work for additional information regarding copyright ownership.
//     * The ASF licenses this file to You under the Apache License, Version 2.0
//     * (the "License"); you may not use this file except in compliance with
//     * the License.  You may obtain a copy of the License at
//     *
//     *     http://www.apache.org/licenses/LICENSE-2.0
//     *
//     * Unless required by applicable law or agreed to in writing, software
//     * distributed under the License is distributed on an "AS IS" BASIS,
//     * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//     * See the License for the specific language governing permissions and
//     * limitations under the License.
//     */

//    public class EnvironmentVariableDfmTextInlineRule : DfmTextInlineRule
//    {
//        public static readonly Regex EnvVar = EnvironmentVariableUtil.EnvVarRegexInline;

//        public override IMarkdownToken TryMatch(IMarkdownParser parser, IMarkdownParsingContext context)
//        {
//            var match = Text.Match(context.CurrentMarkdown);
//            if (match.Length == 0)
//            {
//                return null;
//            }
//            var sourceInfo = context.Consume(match.Length);

//            //Console.WriteLine("Hey!!!!");

//            var environmentVariableMatch = EnvVar.Match(match.Groups[0].Value);
//            //Console.WriteLine(environmentVariableMatch.Success);
//            string replacement = environmentVariableMatch.Success ? EnvironmentVariableUtil.ReplaceEnvironmentVariables(match.Groups[0].Value, match, new StringBuilder(match.Groups[0].Value.Length)) : match.Groups[0].Value;
//            return new MarkdownTextToken(this, parser.Context, StringHelper.Escape(Smartypants(parser.Options, replacement)), sourceInfo);
//        }
//    }
//}
