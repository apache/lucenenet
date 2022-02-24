using Microsoft.DocAsCode.MarkdownLite;
using System;

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

    public class EnvironmentVariableInlineRule : IMarkdownRule
    {
        // give it a name
        public string Name => "EnvVarToken";

        // process the match
        public IMarkdownToken TryMatch(IMarkdownParser parser, IMarkdownParsingContext context)
        {
            // TODO: This does not process this token from within a code block like

            // ```
            // dotnet tool install lucene-cli -g --version [EnvVar: LuceneNetVersion]
            // ```

            //Console.Write(context.CurrentMarkdown);
            //Console.WriteLine("------------------------------------------------------------------------------");

            var match = EnvironmentVariableUtil.EnvVarRegex.Match(context.CurrentMarkdown);

            if (!match.Success) return null;

            var envVar = match.Groups[1].Value;
            var text = Environment.GetEnvironmentVariable(envVar);
            if (text == null) return null;

            // 'eat' the characters of the current markdown token so they anr
            var sourceInfo = context.Consume(match.Length);
            return new MarkdownTextToken(this, parser.Context, text, sourceInfo);
        }
    }
}