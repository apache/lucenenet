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
    /// Factory for <see cref="ASCIIFoldingFilter"/>.
    /// <code>
    /// &lt;fieldType name="text_ascii" class="solr.TextField" positionIncrementGap="100"&gt;
    ///   &lt;analyzer&gt;
    ///     &lt;tokenizer class="solr.WhitespaceTokenizerFactory"/&gt;
    ///     &lt;filter class="solr.ASCIIFoldingFilterFactory" preserveOriginal="false"/&gt;
    ///   &lt;/analyzer&gt;
    /// &lt;/fieldType&gt;</code>
    /// </summary>
    public class ASCIIFoldingFilterFactory : TokenFilterFactory, IMultiTermAwareComponent
    {
        private readonly bool preserveOriginal;

        /// <summary>
        /// Creates a new <see cref="ASCIIFoldingFilterFactory"/> </summary>
        public ASCIIFoldingFilterFactory(IDictionary<string, string> args)
            : base(args)
        {
            preserveOriginal = GetBoolean(args, "preserveOriginal", false);
            if (args.Count > 0)
            {
                throw new System.ArgumentException("Unknown parameters: " + args);
            }
        }

        public override TokenStream Create(TokenStream input)
        {
            return new ASCIIFoldingFilter(input, preserveOriginal);
        }

        public virtual AbstractAnalysisFactory GetMultiTermComponent()
        {
            return this;
        }
    }
}