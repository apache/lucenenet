// Lucene version compatibility level 4.8.1
using System;
using System.Collections.Generic;
using Lucene.Net.Analysis.Util;
using Lucene.Net.Util;

namespace Lucene.Net.Analysis.Position
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
    /// Factory for <see cref="PositionFilter"/>.
    /// Set the positionIncrement of all tokens to the "positionIncrement", except the first return token which retains its
    /// original positionIncrement value. The default positionIncrement value is zero.
    /// <code>
    /// &lt;fieldType name="text_position" class="solr.TextField" positionIncrementGap="100"&gt;
    ///   &lt;analyzer&gt;
    ///     &lt;tokenizer class="solr.WhitespaceTokenizerFactory"/&gt;
    ///     &lt;filter class="solr.PositionFilterFactory" positionIncrement="0"/&gt;
    ///   &lt;/analyzer&gt;
    /// &lt;/fieldType&gt;</code>
    /// </summary>
    /// <seealso cref="PositionFilter"/>
    [Obsolete("(4.4)")]
    public class PositionFilterFactory : TokenFilterFactory
    {
        private readonly int positionIncrement;

        /// <summary>
        /// Creates a new <see cref="PositionFilterFactory"/> </summary>
        public PositionFilterFactory(IDictionary<string, string> args)
            : base(args)
        {
            positionIncrement = GetInt32(args, "positionIncrement", 0);
            if (args.Count > 0)
            {
                throw new ArgumentException(string.Format(J2N.Text.StringFormatter.CurrentCulture, "Unknown parameters: {0}", args));
            }
            if (m_luceneMatchVersion.OnOrAfter(Lucene.Net.Util.LuceneVersion.LUCENE_44))
            {
                throw new ArgumentException("PositionFilter is deprecated as of Lucene 4.4. You should either fix your code to not use it or use Lucene 4.3 version compatibility");
            }
        }

        public override TokenStream Create(TokenStream input)
        {
            return new PositionFilter(input, positionIncrement);
        }
    }
}