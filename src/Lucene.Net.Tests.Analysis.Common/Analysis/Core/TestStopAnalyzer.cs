using System.Collections.Generic;

namespace org.apache.lucene.analysis.core
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

	using PositionIncrementAttribute = org.apache.lucene.analysis.tokenattributes.PositionIncrementAttribute;
	using CharTermAttribute = org.apache.lucene.analysis.tokenattributes.CharTermAttribute;
	using CharArraySet = org.apache.lucene.analysis.util.CharArraySet;
	using IOUtils = org.apache.lucene.util.IOUtils;
	using Version = org.apache.lucene.util.Version;


	public class TestStopAnalyzer : BaseTokenStreamTestCase
	{

	  private StopAnalyzer stop = new StopAnalyzer(TEST_VERSION_CURRENT);
	  private ISet<object> inValidTokens = new HashSet<object>();

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public void setUp() throws Exception
	  public override void setUp()
	  {
		base.setUp();

//JAVA TO C# CONVERTER TODO TASK: Java wildcard generics are not converted to .NET:
//ORIGINAL LINE: java.util.Iterator<?> it = StopAnalyzer.ENGLISH_STOP_WORDS_SET.iterator();
		IEnumerator<?> it = StopAnalyzer.ENGLISH_STOP_WORDS_SET.GetEnumerator();
		while (it.MoveNext())
		{
		  inValidTokens.Add(it.Current);
		}
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testDefaults() throws java.io.IOException
	  public virtual void testDefaults()
	  {
		assertTrue(stop != null);
		TokenStream stream = stop.tokenStream("test", "This is a test of the english stop analyzer");
		try
		{
		  assertTrue(stream != null);
		  CharTermAttribute termAtt = stream.getAttribute(typeof(CharTermAttribute));
		  stream.reset();

		  while (stream.incrementToken())
		  {
			assertFalse(inValidTokens.Contains(termAtt.ToString()));
		  }
		  stream.end();
		}
		finally
		{
		  IOUtils.closeWhileHandlingException(stream);
		}
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testStopList() throws java.io.IOException
	  public virtual void testStopList()
	  {
		CharArraySet stopWordsSet = new CharArraySet(TEST_VERSION_CURRENT, asSet("good", "test", "analyzer"), false);
		StopAnalyzer newStop = new StopAnalyzer(TEST_VERSION_CURRENT, stopWordsSet);
		TokenStream stream = newStop.tokenStream("test", "This is a good test of the english stop analyzer");
		try
		{
		  assertNotNull(stream);
		  CharTermAttribute termAtt = stream.getAttribute(typeof(CharTermAttribute));

		  stream.reset();
		  while (stream.incrementToken())
		  {
			string text = termAtt.ToString();
			assertFalse(stopWordsSet.contains(text));
		  }
		  stream.end();
		}
		finally
		{
		  IOUtils.closeWhileHandlingException(stream);
		}
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testStopListPositions() throws java.io.IOException
	  public virtual void testStopListPositions()
	  {
		CharArraySet stopWordsSet = new CharArraySet(TEST_VERSION_CURRENT, asSet("good", "test", "analyzer"), false);
		StopAnalyzer newStop = new StopAnalyzer(TEST_VERSION_CURRENT, stopWordsSet);
		string s = "This is a good test of the english stop analyzer with positions";
		int[] expectedIncr = new int[] {1, 1, 1, 3, 1, 1, 1, 2, 1};
		TokenStream stream = newStop.tokenStream("test", s);
		try
		{
		  assertNotNull(stream);
		  int i = 0;
		  CharTermAttribute termAtt = stream.getAttribute(typeof(CharTermAttribute));
		  PositionIncrementAttribute posIncrAtt = stream.addAttribute(typeof(PositionIncrementAttribute));

		  stream.reset();
		  while (stream.incrementToken())
		  {
			string text = termAtt.ToString();
			assertFalse(stopWordsSet.contains(text));
			assertEquals(expectedIncr[i++],posIncrAtt.PositionIncrement);
		  }
		  stream.end();
		}
		finally
		{
		  IOUtils.closeWhileHandlingException(stream);
		}
	  }

	}

}