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
using Lucene.Net.Test.Analysis;
using NUnit.Framework;

namespace Lucene.Net.Analyzers.Sinks
{
    [TestFixture]
    public class TokenRangeSinkTokenizerTest : BaseTokenStreamTestCase
    {
        [Test]
        public void Test()
        {
            TokenRangeSinkFilter sinkFilter = new TokenRangeSinkFilter(2, 4);
            String test = "The quick red fox jumped over the lazy brown dogs";
            TeeSinkTokenFilter tee = new TeeSinkTokenFilter(new WhitespaceTokenizer(new StringReader(test)));
            TeeSinkTokenFilter.SinkTokenStream rangeToks = tee.NewSinkTokenStream(sinkFilter);

            int count = 0;
            tee.Reset();
            while (tee.IncrementToken())
            {
                count++;
            }

            int sinkCount = 0;
            rangeToks.Reset();
            while (rangeToks.IncrementToken())
            {
                sinkCount++;
            }

            Assert.True(count == 10, count + " does not equal: " + 10);
            Assert.True(sinkCount == 2, "rangeToks Size: " + sinkCount + " is not: " + 2);
        }
    }
}
