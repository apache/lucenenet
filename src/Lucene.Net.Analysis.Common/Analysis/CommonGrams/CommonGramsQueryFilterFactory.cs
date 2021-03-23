// Lucene version compatibility level 4.8.1
using System.Collections.Generic;

namespace Lucene.Net.Analysis.CommonGrams
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
    /// Construct <see cref="CommonGramsQueryFilter"/>.
    /// 
    /// <code>
    /// &lt;fieldType name="text_cmmngrmsqry" class="solr.TextField" positionIncrementGap="100"&gt;
    ///   &lt;analyzer&gt;
    ///     &lt;tokenizer class="solr.WhitespaceTokenizerFactory"/&gt;
    ///     &lt;filter class="solr.CommonGramsQueryFilterFactory" words="commongramsquerystopwords.txt" ignoreCase="false"/&gt;
    ///   &lt;/analyzer&gt;
    /// &lt;/fieldType&gt;
    /// </code>
    /// </summary>
    public class CommonGramsQueryFilterFactory : CommonGramsFilterFactory
    {
        /// <summary>
        /// Creates a new <see cref="CommonGramsQueryFilterFactory"/> </summary>
        public CommonGramsQueryFilterFactory(IDictionary<string, string> args)
            : base(args)
        {
        }

        /// <summary>
        /// Create a <see cref="CommonGramsFilter"/> and wrap it with a <see cref="CommonGramsQueryFilter"/>
        /// </summary>
        public override TokenStream Create(TokenStream input)
        {
            var commonGrams = (CommonGramsFilter)base.Create(input);
            return new CommonGramsQueryFilter(commonGrams);
        }
    }
}