// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.Util;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.IO;

namespace Lucene.Net.Analysis.NGram
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
    /// Creates new instances of <see cref="EdgeNGramTokenizer"/>.
    /// <code>
    /// &lt;fieldType name="text_edgngrm" class="solr.TextField" positionIncrementGap="100"&gt;
    ///   &lt;analyzer&gt;
    ///     &lt;tokenizer class="solr.EdgeNGramTokenizerFactory" minGramSize="1" maxGramSize="1"/&gt;
    ///   &lt;/analyzer&gt;
    /// &lt;/fieldType&gt;</code>
    /// </summary>
    public class EdgeNGramTokenizerFactory : TokenizerFactory
    {
        private readonly int maxGramSize;
        private readonly int minGramSize;
        private readonly string side;

        /// <summary>
        /// Creates a new <see cref="EdgeNGramTokenizerFactory"/> </summary>
        public EdgeNGramTokenizerFactory(IDictionary<string, string> args) : base(args)
        {
            minGramSize = GetInt32(args, "minGramSize", EdgeNGramTokenizer.DEFAULT_MIN_GRAM_SIZE);
            maxGramSize = GetInt32(args, "maxGramSize", EdgeNGramTokenizer.DEFAULT_MAX_GRAM_SIZE);
            side = Get(args, "side", EdgeNGramTokenFilter.Side.FRONT.ToString());
            if (args.Count > 0)
            {
                throw new ArgumentException(string.Format(J2N.Text.StringFormatter.CurrentCulture, "Unknown parameters: {0}", args));
            }
        }

        public override Tokenizer Create(AttributeSource.AttributeFactory factory, TextReader input)
        {
#pragma warning disable 612, 618
            if (m_luceneMatchVersion.OnOrAfter(LuceneVersion.LUCENE_44))
#pragma warning restore 612, 618
            {
                EdgeNGramTokenFilter.Side sideEnum;
                if (!Enum.TryParse(this.side, true, out sideEnum))
                {
                    throw new ArgumentException(typeof(EdgeNGramTokenizer).Name + " does not support backward n-grams as of Lucene 4.4");
                }
                return new EdgeNGramTokenizer(m_luceneMatchVersion, input, minGramSize, maxGramSize);
            }
            else
            {
#pragma warning disable 612, 618
                return new Lucene43EdgeNGramTokenizer(m_luceneMatchVersion, input, side, minGramSize, maxGramSize);
#pragma warning restore 612, 618
            }
        }
    }
}