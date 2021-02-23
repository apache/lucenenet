// Lucene version compatibility level 4.8.1
using NUnit.Framework;
using System.Globalization;
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


    public class DateRecognizerSinkTokenizerTest : BaseTokenStreamTestCase
    {
        /// <summary>
        /// LUCENENET: This test was changed (the date format inputs) to account for .NET's strict ParseExact format.
        /// </summary>
        [Test]
        public virtual void Test()
        {
            DateRecognizerSinkFilter sinkFilter = new DateRecognizerSinkFilter(new string[] { "MM/dd/yyyy", "M/dd/yyyy", "MM/d/yyyy", "M/d/yyyy" }, CultureInfo.InvariantCulture);
            string test = "The quick red fox jumped over the lazy brown dogs on 7/11/2006  The dogs finally reacted on 7/12/2006";
            TeeSinkTokenFilter tee = new TeeSinkTokenFilter(new MockTokenizer(new StringReader(test), MockTokenizer.WHITESPACE, false));
            TeeSinkTokenFilter.SinkTokenStream sink = tee.NewSinkTokenStream(sinkFilter);
            int count = 0;

            tee.Reset();
            while (tee.IncrementToken())
            {
                count++;
            }
            assertTrue(count + " does not equal: " + 18, count == 18);

            int sinkCount = 0;
            sink.Reset();
            while (sink.IncrementToken())
            {
                sinkCount++;
            }
            assertTrue("sink Size: " + sinkCount + " is not: " + 2, sinkCount == 2);

        }

        /// <summary>
        /// LUCENENET: This test was added to test .NET's loose Parse format.
        /// </summary>
        [Test]
        public virtual void TestLooseDateFormat()
        {
            DateRecognizerSinkFilter sinkFilter = new DateRecognizerSinkFilter(CultureInfo.InvariantCulture);
            string test = "The quick red fox jumped over the lazy brown dogs on 7/11/2006  The dogs finally reacted on 7/2/2006";
            TeeSinkTokenFilter tee = new TeeSinkTokenFilter(new MockTokenizer(new StringReader(test), MockTokenizer.WHITESPACE, false));
            TeeSinkTokenFilter.SinkTokenStream sink = tee.NewSinkTokenStream(sinkFilter);
            int count = 0;

            tee.Reset();
            while (tee.IncrementToken())
            {
                count++;
            }
            assertTrue(count + " does not equal: " + 18, count == 18);

            int sinkCount = 0;
            sink.Reset();
            while (sink.IncrementToken())
            {
                sinkCount++;
            }
            assertTrue("sink Size: " + sinkCount + " is not: " + 2, sinkCount == 2);

        }
    }
}