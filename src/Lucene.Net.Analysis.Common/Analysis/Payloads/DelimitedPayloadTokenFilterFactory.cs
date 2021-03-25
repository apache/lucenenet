// Lucene version compatibility level 4.8.1
using System;
using System.Collections.Generic;
using Lucene.Net.Analysis.Util;

namespace Lucene.Net.Analysis.Payloads
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
    /// Factory for <see cref="DelimitedPayloadTokenFilter"/>.
    /// <code>
    /// &lt;fieldType name="text_dlmtd" class="solr.TextField" positionIncrementGap="100"&gt;
    ///   &lt;analyzer&gt;
    ///     &lt;tokenizer class="solr.WhitespaceTokenizerFactory"/&gt;
    ///     &lt;filter class="solr.DelimitedPayloadTokenFilterFactory" encoder="float" delimiter="|"/&gt;
    ///   &lt;/analyzer&gt;
    /// &lt;/fieldType&gt;</code>
    /// </summary>
    public class DelimitedPayloadTokenFilterFactory : TokenFilterFactory, IResourceLoaderAware
    {
        public const string ENCODER_ATTR = "encoder";
        public const string DELIMITER_ATTR = "delimiter";

        private readonly string encoderClass;
        private readonly char delimiter;

        private IPayloadEncoder encoder;

        /// <summary>
        /// Creates a new <see cref="DelimitedPayloadTokenFilterFactory"/> </summary>
        public DelimitedPayloadTokenFilterFactory(IDictionary<string, string> args)
            : base(args)
        {
            encoderClass = Require(args, ENCODER_ATTR);
            delimiter = GetChar(args, DELIMITER_ATTR, '|');
            if (args.Count > 0)
            {
                throw new ArgumentException(string.Format(J2N.Text.StringFormatter.CurrentCulture, "Unknown parameters: {0}", args));
            }
        }

        public override TokenStream Create(TokenStream input)
        {
            return new DelimitedPayloadTokenFilter(input, delimiter, encoder);
        }

        public virtual void Inform(IResourceLoader loader)
        {
            if (encoderClass.Equals("float", StringComparison.Ordinal))
            {
                encoder = new SingleEncoder();
            }
            else if (encoderClass.Equals("integer", StringComparison.Ordinal))
            {
                encoder = new IntegerEncoder();
            }
            else if (encoderClass.Equals("identity", StringComparison.Ordinal))
            {
                encoder = new IdentityEncoder();
            }
            else
            {
                encoder = loader.NewInstance<IPayloadEncoder>(encoderClass /*, typeof(PayloadEncoder)*/);
            }
        }
    }
}