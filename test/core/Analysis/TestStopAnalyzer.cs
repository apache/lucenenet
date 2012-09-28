/* 
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 * 
 * http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Collections.Generic;
using Lucene.Net.Analysis.Tokenattributes;
using Lucene.Net.Test.Analysis;
using NUnit.Framework;
using Version = Lucene.Net.Util.Version;

namespace Lucene.Net.Analysis
{
	
    [TestFixture]
	public class TestStopAnalyzer:BaseTokenStreamTestCase
	{
		
		private StopAnalyzer stop = new StopAnalyzer(Version.LUCENE_CURRENT);
		private HashSet<string> inValidTokens = new HashSet<string>();
		
		public TestStopAnalyzer(System.String s):base(s)
		{
		}

        public TestStopAnalyzer() 
        {
        }
		
		[SetUp]
		public override void  SetUp()
		{
			base.SetUp();
			inValidTokens.UnionWith(StopAnalyzer.ENGLISH_STOP_WORDS_SET);
		}
		
        [Test]
		public virtual void  TestDefaults()
		{
			Assert.IsTrue(stop != null);
			System.IO.StringReader reader = new System.IO.StringReader("This is a test of the english stop analyzer");
			TokenStream stream = stop.TokenStream("test", reader);
			Assert.IsTrue(stream != null);
            ITermAttribute termAtt = stream.GetAttribute<ITermAttribute>();
			
			while (stream.IncrementToken())
			{
				Assert.IsFalse(inValidTokens.Contains(termAtt.Term));
			}
		}
		
        [Test]
		public virtual void  TestStopList()
		{
			var stopWordsSet = Support.Compatibility.SetFactory.CreateHashSet<string>();
			stopWordsSet.Add("good");
			stopWordsSet.Add("test");
			stopWordsSet.Add("analyzer");
			StopAnalyzer newStop = new StopAnalyzer(Version.LUCENE_24, stopWordsSet);
			System.IO.StringReader reader = new System.IO.StringReader("This is a good test of the english stop analyzer");
			TokenStream stream = newStop.TokenStream("test", reader);
			Assert.IsNotNull(stream);
            ITermAttribute termAtt = stream.GetAttribute<ITermAttribute>();
            IPositionIncrementAttribute posIncrAtt = stream.AddAttribute<IPositionIncrementAttribute>();
			
			while (stream.IncrementToken())
			{
				System.String text = termAtt.Term;
				Assert.IsFalse(stopWordsSet.Contains(text));
                Assert.AreEqual(1, posIncrAtt.PositionIncrement); // in 2.4 stop tokenizer does not apply increments.
			}
		}
		
        [Test]
		public virtual void  TestStopListPositions()
        {
            var stopWordsSet = Support.Compatibility.SetFactory.CreateHashSet<string>();
            stopWordsSet.Add("good");
            stopWordsSet.Add("test");
            stopWordsSet.Add("analyzer");
            var newStop = new StopAnalyzer(Version.LUCENE_CURRENT, stopWordsSet);
            var reader = new System.IO.StringReader("This is a good test of the english stop analyzer with positions");
            int[] expectedIncr =                   { 1,   1, 1,          3, 1,  1,      1,            2,   1};
            TokenStream stream = newStop.TokenStream("test", reader);
            Assert.NotNull(stream);
            int i = 0;
            ITermAttribute termAtt = stream.GetAttribute<ITermAttribute>();
            IPositionIncrementAttribute posIncrAtt = stream.AddAttribute<IPositionIncrementAttribute>();

            while (stream.IncrementToken())
            {
                string text = termAtt.Term;
                Assert.IsFalse(stopWordsSet.Contains(text));
                Assert.AreEqual(expectedIncr[i++], posIncrAtt.PositionIncrement);
            }
        }
	}
}