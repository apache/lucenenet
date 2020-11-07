// lucene version compatibility level: 4.8.1
using Lucene.Net.Analysis.Util;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.IO;

namespace Lucene.Net.Analysis.Cn.Smart
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
    /// Factory for <see cref="HMMChineseTokenizer"/>
    /// <para/>
    /// Note: this class will currently emit tokens for punctuation. So you should either add
    /// a <see cref="Miscellaneous.WordDelimiterFilter"/> after to remove these (with concatenate off), or use the 
    /// SmartChinese stoplist with a StopFilterFactory via:
    /// <code>words="org/apache/lucene/analysis/cn/smart/stopwords.txt"</code>
    /// <para/>
    /// @lucene.experimental
    /// </summary>
    public sealed class HMMChineseTokenizerFactory : TokenizerFactory
    {
        /// <summary>
        /// Creates a new <see cref="HMMChineseTokenizerFactory"/> 
        /// </summary>
        public HMMChineseTokenizerFactory(IDictionary<string, string> args)
            : base(args)
        {
            if (args.Count > 0)
            {
                throw new ArgumentException(string.Format(J2N.Text.StringFormatter.CurrentCulture, "Unknown parameters: {0}", args));
            }
        }

        public override Tokenizer Create(AttributeSource.AttributeFactory factory, TextReader reader)
        {
            return new HMMChineseTokenizer(factory, reader);
        }
    }
}
