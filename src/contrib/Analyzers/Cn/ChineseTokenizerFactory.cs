/*
 *
 * Licensed to the Apache Software Foundation (ASF) under one
 * or more contributor license agreements.  See the NOTICE file
 * distributed with this work for additional information
 * regarding copyright ownership.  The ASF licenses this file
 * to you under the Apache License, Version 2.0 (the
 * "License"); you may not use this file except in compliance
 * with the License.  You may obtain a copy of the License at
 *
 *   http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing,
 * software distributed under the License is distributed on an
 * "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
 * KIND, either express or implied.  See the License for the
 * specific language governing permissions and limitations
 * under the License.
 *
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Lucene.Net.Analysis.Util;
using Lucene.Net.Util;

namespace Lucene.Net.Analysis.Cn
{
    /// <summary>
    /// Factory for <see cref="ChineseTokenizer"/>
    /// </summary>
    [Obsolete("Use {Lucene.Net.Analysis.Standard.StandardTokenizerFactory} instead.")]
    public class ChineseTokenizerFactory : TokenizerFactory
    {
        /// <summary>
        /// Creates a new ChineseTokenizerFactory
        /// </summary>
        public ChineseTokenizerFactory(IDictionary<string, string> args)
            : base(args)
        {
            if (args.Count > 0)
            {
                throw new ArgumentException("Unknown parameters: " + args);
            }
        }

        public override Tokenizer Create(AttributeSource.AttributeFactory factory, TextReader _in)
        {
            return new ChineseTokenizer(factory, _in);
        }
    }
}
