// lucene version compatibility level: 4.8.1
using Lucene.Net.Analysis.Util;
using System;
using System.Collections.Generic;

namespace Lucene.Net.Analysis.Phonetic
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
    /// Factory for <see cref="DoubleMetaphoneFilter"/>.
    /// <code>
    /// &lt;fieldType name="text_dblmtphn" class="solr.TextField" positionIncrementGap="100"&gt;
    ///   &lt;analyzer&gt;
    ///     &lt;tokenizer class="solr.WhitespaceTokenizerFactory"/&gt;
    ///     &lt;filter class="solr.DoubleMetaphoneFilterFactory" inject="true" maxCodeLength="4"/&gt;
    ///   &lt;/analyzer&gt;
    /// &lt;/fieldType&gt;
    /// </code>
    /// </summary>
    public class DoubleMetaphoneFilterFactory : TokenFilterFactory
    {
        /// <summary>parameter name: true if encoded tokens should be added as synonyms</summary>
        public static readonly string INJECT = "inject";
        /// <summary>parameter name: restricts the length of the phonetic code</summary>
        public static readonly string MAX_CODE_LENGTH = "maxCodeLength";
        /// <summary>default maxCodeLength if not specified</summary>
        public static readonly int DEFAULT_MAX_CODE_LENGTH = 4;

        private readonly bool inject;
        private readonly int maxCodeLength;

        /// <summary>
        /// Creates a new <see cref="DoubleMetaphoneFilterFactory"/>
        /// </summary>
        public DoubleMetaphoneFilterFactory(IDictionary<string, string> args)
            : base(args)
        {
            inject = GetBoolean(args, INJECT, true);
            maxCodeLength = GetInt32(args, MAX_CODE_LENGTH, DEFAULT_MAX_CODE_LENGTH);
            if (args.Count > 0)
            {
                throw new ArgumentException(string.Format(J2N.Text.StringFormatter.CurrentCulture, "Unknown parameters: {0}", args));
            }
        }

        public override TokenStream Create(TokenStream input)
        {
            return new DoubleMetaphoneFilter(input, maxCodeLength, inject);
        }
    }
}
