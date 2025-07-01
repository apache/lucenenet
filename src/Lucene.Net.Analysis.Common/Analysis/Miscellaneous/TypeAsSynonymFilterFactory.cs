// Lucene version compatibility level 8.2.0
// LUCENENET NOTE: Ported because Lucene.Net.Analysis.OpenNLP requires this to be useful.
using Lucene.Net.Analysis.Util;
using System;
using System.Collections.Generic;
#nullable enable

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
    /// Factory for <see cref="TypeAsSynonymFilter"/>.
    /// <code>
    /// &lt;fieldType name="text_type_as_synonym" class="solr.TextField" positionIncrementGap="100"&gt;
    ///   &lt;analyzer&gt;
    ///     &lt;tokenizer class="solr.UAX29URLEmailTokenizerFactory"/&gt;
    ///     &lt;filter class="solr.TypeAsSynonymFilterFactory" prefix="_type_" /&gt;
    ///   &lt;/analyzer&gt;
    /// &lt;/fieldType&gt;
    /// </code>
    /// 
    /// <para/>
    /// If the optional <c>prefix</c> parameter is used, the specified value will be prepended
    /// to the type, e.g.with prefix = "_type_", for a token "example.com" with type "&lt;URL&gt;",
    /// the emitted synonym will have text "_type_&lt;URL&gt;".
    /// </summary>
    public class TypeAsSynonymFilterFactory : TokenFilterFactory
    {
        private readonly string prefix;

        public TypeAsSynonymFilterFactory(IDictionary<string, string> args)
            : base(args)
        {
            prefix = Get(args, "prefix");  // default value is null
            if (args.Count > 0)
            {
                throw new ArgumentException(string.Format(J2N.Text.StringFormatter.CurrentCulture, "Unknown parameters: {0}", args));
            }
        }

        public override TokenStream Create(TokenStream input)
        {
            return new TypeAsSynonymFilter(input, prefix);
        }
    }
}
