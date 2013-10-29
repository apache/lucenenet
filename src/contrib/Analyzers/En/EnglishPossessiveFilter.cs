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

using System;
using Lucene.Net.Analysis.Tokenattributes;
using Lucene.Net.Util;
using Version = Lucene.Net.Util.Version;

namespace Lucene.Net.Analysis.En
{
    public sealed class EnglishPossessiveFilter : TokenFilter
    {
        private readonly CharTermAttribute termAtt;
        private Version matchVersion;

        [Obsolete]
        public EnglishPossessiveFilter(TokenStream input) : this(Version.LUCENE_35, input) { }

        public EnglishPossessiveFilter(Version version, TokenStream input)
            :base (input)
        {
            this.matchVersion = version;
            termAtt = AddAttribute<CharTermAttribute>();
        }

        public override bool IncrementToken()
        {
            if (!input.IncrementToken())
            {
                return false;
            }

            var buffer = termAtt.Buffer;
            var bufferLength = termAtt.Length;

            if (bufferLength >= 2 &&
                (buffer[bufferLength - 2] == '\'' ||
                 (matchVersion.OnOrAfter(Version.LUCENE_36) &&
                  (buffer[bufferLength - 2] == '\u2019' || buffer[bufferLength - 2] == '\uFF07'))) &&
                (buffer[bufferLength - 1] == 's' || buffer[bufferLength - 1] == 'S'))
            {
                termAtt.SetLength(bufferLength - 2); // strip last two characters off
            }

            return true;
        }
    }
}
