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

using System.Collections.Generic;
using Lucene.Net;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Core;
using Lucene.Net.Analysis.Tokenattributes;
using Lucene.Net.Analysis.Util;
using Lucene.Net.Util;
using NUnit.Framework;

namespace Lucene.Net.Tests.Analysis.Common.Analysis.Core
{
	[TestFixture]
	public class TestStopAnalyzer : BaseTokenStreamTestCase
	{
		private readonly StopAnalyzer stop = new StopAnalyzer(LuceneTestCase.TEST_VERSION_CURRENT);
		private readonly ISet<object> inValidTokens = new HashSet<object>();

		[SetUp]
		public override void SetUp()
		{
			base.SetUp();

			var it = StopAnalyzer.ENGLISH_STOP_WORDS_SET.GetEnumerator();

			while (it.MoveNext())
			{
				this.inValidTokens.Add(it.Current);
			}
		}

		[Test]
		public virtual void TestDefaults()
		{
			assertTrue(this.stop != null);
			var stream = this.stop.TokenStream("test", "This is a test of the english stop analyzer");

			try
			{
				assertTrue(stream != null);
				var termAtt = stream.GetAttribute<ICharTermAttribute>();
				stream.Reset();

				while (stream.IncrementToken())
				{
					assertFalse(this.inValidTokens.Contains(termAtt.ToString()));
				}

				stream.End();
			}
			finally
			{
				IOUtils.CloseWhileHandlingException(stream);
			}
		}

		[Test]
		public virtual void TestStopList()
		{
			var stopWordsSet = new CharArraySet(LuceneTestCase.TEST_VERSION_CURRENT, AsSet("good", "test", "analyzer"), false);
			var newStop = new StopAnalyzer(LuceneTestCase.TEST_VERSION_CURRENT, stopWordsSet);
			var stream = newStop.TokenStream("test", "This is a good test of the english stop analyzer");

			try
			{
				assertNotNull(stream);
				var termAtt = stream.GetAttribute<ICharTermAttribute>();

				stream.Reset();

				while (stream.IncrementToken())
				{
					var text = termAtt.ToString();
					assertFalse(stopWordsSet.contains(text));
				}

				stream.End();
			}
			finally
			{
				IOUtils.CloseWhileHandlingException(stream);
			}
		}

		[Test]
		public virtual void TestStopListPositions()
		{
			var stopWordsSet = new CharArraySet(LuceneTestCase.TEST_VERSION_CURRENT, AsSet("good", "test", "analyzer"), false);
			var newStop = new StopAnalyzer(LuceneTestCase.TEST_VERSION_CURRENT, stopWordsSet);
			var s = "This is a good test of the english stop analyzer with positions";
			var expectedIncr = new int[] { 1, 1, 1, 3, 1, 1, 1, 2, 1 };
			var stream = newStop.TokenStream("test", s);

			try
			{
				assertNotNull(stream);
				var i = 0;
				var termAtt = stream.GetAttribute<ICharTermAttribute>();
				var posIncrAtt = stream.AddAttribute<IPositionIncrementAttribute>();

				stream.Reset();

				while (stream.IncrementToken())
				{
					var text = termAtt.ToString();
					assertFalse(stopWordsSet.contains(text));
					assertEquals(expectedIncr[i++], posIncrAtt.PositionIncrement);
				}

				stream.End();
			}
			finally
			{
				IOUtils.CloseWhileHandlingException(stream);
			}
		}
	}
}