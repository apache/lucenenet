//using Microsoft.DocAsCode.MarkdownLite;
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

//    public class EnvironmentVariableInCodeBlockRule : MarkdownCodeBlockRule
//    {
//        public static readonly Regex EnvVarRegex = EnvironmentVariableUtil.EnvVarRegexInline;

//        public override IMarkdownToken TryMatch(IMarkdownParser parser, IMarkdownParsingContext context)
//        {
//            //Console.WriteLine("Hello World!");

//            var token = base.TryMatch(parser, context) as MarkdownCodeBlockToken;
//            if (token is null) return null;

//            //Console.WriteLine("Alert: Code.");
//            //Console.WriteLine(token.Code);

//            var environmentVariableMatch = EnvVarRegex.Match(token.Code);
//            if (!environmentVariableMatch.Success) return token;

//            var code = EnvironmentVariableUtil.ReplaceEnvironmentVariables(token.Code, environmentVariableMatch);

//            return new MarkdownCodeBlockToken(token.Rule, token.Context, code, token.Lang, token.SourceInfo);
//        }
//    }
//}
