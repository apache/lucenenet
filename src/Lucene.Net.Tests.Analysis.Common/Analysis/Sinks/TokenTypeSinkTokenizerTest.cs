// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.TokenAttributes;
using NUnit.Framework;
using System;
using System.IO;

namespace Lucene.Net.Analysis.Sinks
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

    public class TokenTypeSinkTokenizerTest : BaseTokenStreamTestCase
    {

        [Test]
        public virtual void Test()
        {
            TokenTypeSinkFilter sinkFilter = new TokenTypeSinkFilter("D");
            string test = "The quick red fox jumped over the lazy brown dogs";

            TeeSinkTokenFilter ttf = new TeeSinkTokenFilter(new WordTokenFilter(new MockTokenizer(new StringReader(test), MockTokenizer.WHITESPACE, false)));
            TeeSinkTokenFilter.SinkTokenStream sink = ttf.NewSinkTokenStream(sinkFilter);

            bool seenDogs = false;

            ICharTermAttribute termAtt = ttf.AddAttribute<ICharTermAttribute>();
            ITypeAttribute typeAtt = ttf.AddAttribute<ITypeAttribute>();
            ttf.Reset();
            while (ttf.IncrementToken())
            {
                if (termAtt.ToString().Equals("dogs", StringComparison.Ordinal))
                {
                    seenDogs = true;
                    assertTrue(typeAtt.Type + " is not equal to " + "D", typeAtt.Type.Equals("D", StringComparison.Ordinal) == true);
                }
                else
                {
                    assertTrue(typeAtt.Type + " is not null and it should be", typeAtt.Type.Equals("word", StringComparison.Ordinal));
                }
            }
            assertTrue(seenDogs + " does not equal: " + true, seenDogs == true);

            int sinkCount = 0;
            sink.Reset();
            while (sink.IncrementToken())
            {
                sinkCount++;
            }

            assertTrue("sink Size: " + sinkCount + " is not: " + 1, sinkCount == 1);
        }

        private sealed class WordTokenFilter : TokenFilter
        {
            internal readonly ICharTermAttribute termAtt;
            internal readonly ITypeAttribute typeAtt;

            internal WordTokenFilter(TokenStream input) : base(input)
            {
                termAtt = AddAttribute<ICharTermAttribute>();
                typeAtt = AddAttribute<ITypeAttribute>();
            }

            public override sealed bool IncrementToken()
            {
                if (!m_input.IncrementToken())
                {
                    return false;
                }

                if (termAtt.ToString().Equals("dogs", StringComparison.Ordinal))
                {
                    typeAtt.Type = "D";
                }
                return true;
            }
        }
    }
}