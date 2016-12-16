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

    public class TokenRangeSinkTokenizerTest : BaseTokenStreamTestCase
    {

        [Test]
        public virtual void Test()
        {
            TokenRangeSinkFilter sinkFilter = new TokenRangeSinkFilter(2, 4);
            string test = "The quick red fox jumped over the lazy brown dogs";
            TeeSinkTokenFilter tee = new TeeSinkTokenFilter(new MockTokenizer(new StringReader(test), MockTokenizer.WHITESPACE, false));
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

            assertTrue(count + " does not equal: " + 10, count == 10);
            assertTrue("rangeToks Size: " + sinkCount + " is not: " + 2, sinkCount == 2);
        }

        [Test]
        public virtual void TestIllegalArguments()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new TokenRangeSinkFilter(4, 2));
        }
    }
}