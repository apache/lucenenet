//using Microsoft.DocAsCode.Dfm;
//using Microsoft.DocAsCode.MarkdownLite;

//namespace LuceneDocsPlugins
//{
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

//    // TODO: this is actually not needed since we are inserting our own inline rule, although the docs
//    // claim we need it https://dotnet.github.io/docfx/tutorial/howto_customize_docfx_flavored_markdown.html
//    // maybe that is only the case for block level items? I cannot quite figure it out looking at the docfx code

//    /// <summary>
//    /// Used to replace environment variables tokens with their values
//    /// </summary>
//    public sealed class EnvironmentVariableRendererPart : DfmCustomizedRendererPartBase<IMarkdownRenderer, MarkdownTextToken, MarkdownInlineContext>
//    {
//        public override string Name => "EnvironmentVariableRendererPart";

//        public override bool Match(IMarkdownRenderer renderer, MarkdownTextToken token, MarkdownInlineContext context)
//            => true;

//        public override StringBuffer Render(IMarkdownRenderer renderer, MarkdownTextToken token, MarkdownInlineContext context)
//            => token.Content;
//    }
//}