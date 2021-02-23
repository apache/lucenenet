// Lucene version compatibility level 4.8.1
using System;
using System.Collections.Generic;
using Lucene.Net.Analysis.Util;

namespace Lucene.Net.Analysis.Miscellaneous
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
    /// Factory for <see cref="KeywordRepeatFilter"/>.
    /// 
    /// Since <see cref="KeywordRepeatFilter"/> emits two tokens for every input token, and any tokens that aren't transformed
    /// later in the analysis chain will be in the document twice. Therefore, consider adding
    /// <see cref="RemoveDuplicatesTokenFilterFactory"/> later in the analysis chain.
    /// </summary>
    public sealed class KeywordRepeatFilterFactory : TokenFilterFactory
    {
        /// <summary>
        /// Creates a new <see cref="KeywordRepeatFilterFactory"/> </summary>
        public KeywordRepeatFilterFactory(IDictionary<string, string> args)
            : base(args)
        {
            if (args.Count > 0)
            {
                throw new ArgumentException(string.Format(J2N.Text.StringFormatter.CurrentCulture, "Unknown parameters: {0}", args));
            }
        }

        public override TokenStream Create(TokenStream input)
        {
            return new KeywordRepeatFilter(input);
        }
    }
}