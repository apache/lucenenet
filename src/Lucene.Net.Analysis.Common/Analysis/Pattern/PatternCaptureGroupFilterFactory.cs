// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.Util;
using System.Collections.Generic;
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
    /// Factory for <see cref="PatternCaptureGroupTokenFilter"/>.
    /// <code>
    /// &lt;fieldType name="text_ptncapturegroup" class="solr.TextField" positionIncrementGap="100"&gt;
    ///   &lt;analyzer&gt;
    ///     &lt;tokenizer class="solr.KeywordTokenizerFactory"/&gt;
    ///     &lt;filter class="solr.PatternCaptureGroupFilterFactory" pattern="([^a-z])" preserve_original="true"/&gt;
    ///   &lt;/analyzer&gt;
    /// &lt;/fieldType&gt;</code>
    /// </summary>
    /// <seealso cref="PatternCaptureGroupTokenFilter"/>
    public class PatternCaptureGroupFilterFactory : TokenFilterFactory
    {
        private readonly Regex pattern; // LUCENENET: marked readonly
        private readonly bool preserveOriginal /*= true*/; // LUCENENET: marked readonly, removed overwritten initializer

        public PatternCaptureGroupFilterFactory(IDictionary<string, string> args)
            : base(args)
        {
            pattern = GetPattern(args, "pattern");
            preserveOriginal = args.TryGetValue("preserve_original", out string value) ? bool.Parse(value) : true;
        }

        public override TokenStream Create(TokenStream input)
        {
            return new PatternCaptureGroupTokenFilter(input, preserveOriginal, pattern);
        }
    }
}
