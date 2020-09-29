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

using System.Collections.Generic;
using System.Composition;
using Microsoft.DocAsCode.Dfm;
using Microsoft.DocAsCode.MarkdownLite;

namespace LuceneDocsPlugins
{
    /// <summary>
    /// Exports our custom markdown parser via MEF to DocFx
    /// </summary>
    [Export(typeof(IDfmEngineCustomizer))]
    public class LuceneDfmEngineCustomizer : IDfmEngineCustomizer
    {
        public void Customize(DfmEngineBuilder builder, IReadOnlyDictionary<string, object> parameters)
         {
            // insert inline rule at the top
            builder.InlineRules = builder.InlineRules.Insert(0, new EnvironmentVariableInlineRule());

            // insert block rule above header rule. Why? I dunno, that's what the docs say: 
            // https://dotnet.github.io/docfx/tutorial/intro_markdown_lite.html#select-token-kind
            var blockIndex = builder.BlockRules.FindIndex(r => r is MarkdownHeadingBlockRule);
            builder.BlockRules = builder.BlockRules.Insert(blockIndex, new LuceneNoteBlockRule());
        }
    }
}
