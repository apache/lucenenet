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
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Sinks;
using Lucene.Net.Analysis.Tokenattributes;
using Lucene.Net.Test.Analysis;
using NUnit.Framework;

namespace Lucene.Net.Analyzers.Sinks
{
    [TestFixture]
    public class TokenTypeSinkTokenizerTest : BaseTokenStreamTestCase
    {
        [Test]
        public void Test()
        {
            TokenTypeSinkFilter sinkFilter = new TokenTypeSinkFilter("D");
            String test = "The quick red fox jumped over the lazy brown dogs";

            TeeSinkTokenFilter ttf = new TeeSinkTokenFilter(new WordTokenFilter(new WhitespaceTokenizer(new StringReader(test))));
            TeeSinkTokenFilter.SinkTokenStream sink = ttf.NewSinkTokenStream(sinkFilter);

            bool seenDogs = false;

            ITermAttribute termAtt = ttf.AddAttribute<ITermAttribute>();
            ITypeAttribute typeAtt = ttf.AddAttribute<ITypeAttribute>();
            ttf.Reset();
            while (ttf.IncrementToken())
            {
                if (termAtt.Term.Equals("dogs"))
                {
                    seenDogs = true;
                    Assert.True(typeAtt.Type.Equals("D") == true, typeAtt.Type + " is not equal to " + "D");
                }
                else
                {
                    Assert.True(typeAtt.Type.Equals("word"), typeAtt.Type + " is not null and it should be");
                }
            }
            Assert.True(seenDogs == true, seenDogs + " does not equal: " + true);

            int sinkCount = 0;
            sink.Reset();
            while (sink.IncrementToken())
            {
                sinkCount++;
            }

            Assert.True(sinkCount == 1, "sink Size: " + sinkCount + " is not: " + 1);
        }

        internal class WordTokenFilter : TokenFilter
        {
            private ITermAttribute termAtt;
            private ITypeAttribute typeAtt;

            internal WordTokenFilter(TokenStream input)
                : base(input)
            {
                termAtt = AddAttribute<ITermAttribute>();
                typeAtt = AddAttribute<ITypeAttribute>();
            }

            public sealed override bool IncrementToken()
            {
                if (!input.IncrementToken()) return false;

                if (termAtt.Term.Equals("dogs"))
                {
                    typeAtt.Type = "D";
                }
                return true;
            }
        }
    }
}
