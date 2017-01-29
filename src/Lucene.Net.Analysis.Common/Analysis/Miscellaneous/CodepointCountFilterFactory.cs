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
    /// Factory for <seealso cref="CodepointCountFilter"/>. 
    /// <pre class="prettyprint">
    /// &lt;fieldType name="text_lngth" class="solr.TextField" positionIncrementGap="100"&gt;
    ///   &lt;analyzer&gt;
    ///     &lt;tokenizer class="solr.WhitespaceTokenizerFactory"/&gt;
    ///     &lt;filter class="solr.CodepointCountFilterFactory" min="0" max="1" /&gt;
    ///   &lt;/analyzer&gt;
    /// &lt;/fieldType&gt;</pre>
    /// </summary>
    public class CodepointCountFilterFactory : TokenFilterFactory
    {
        internal readonly int min;
        internal readonly int max;
        public const string MIN_KEY = "min";
        public const string MAX_KEY = "max";

        /// <summary>
        /// Creates a new CodepointCountFilterFactory </summary>
        public CodepointCountFilterFactory(IDictionary<string, string> args) : base(args)
        {
            min = RequireInt(args, MIN_KEY);
            max = RequireInt(args, MAX_KEY);
            if (args.Count > 0)
            {
                throw new System.ArgumentException("Unknown parameters: " + args);
            }
        }

        public override TokenStream Create(TokenStream input)
        {
            return new CodepointCountFilter(m_luceneMatchVersion, input, min, max);
        }
    }
}