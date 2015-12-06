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

	using BaseTokenStreamFactoryTestCase = org.apache.lucene.analysis.util.BaseTokenStreamFactoryTestCase;
	using CharArraySet = org.apache.lucene.analysis.util.CharArraySet;
	using ClasspathResourceLoader = org.apache.lucene.analysis.util.ClasspathResourceLoader;
	using ResourceLoader = org.apache.lucene.analysis.util.ResourceLoader;

	public class TestStopFilterFactory : BaseTokenStreamFactoryTestCase
	{

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testInform() throws Exception
	  public virtual void testInform()
	  {
		ResourceLoader loader = new ClasspathResourceLoader(this.GetType());
		assertTrue("loader is null and it shouldn't be", loader != null);
		StopFilterFactory factory = (StopFilterFactory) tokenFilterFactory("Stop", "words", "stop-1.txt", "ignoreCase", "true");
		CharArraySet words = factory.StopWords;
		assertTrue("words is null and it shouldn't be", words != null);
		assertTrue("words Size: " + words.size() + " is not: " + 2, words.size() == 2);
		assertTrue(factory.IgnoreCase + " does not equal: " + true, factory.IgnoreCase == true);

		factory = (StopFilterFactory) tokenFilterFactory("Stop", "words", "stop-1.txt, stop-2.txt", "ignoreCase", "true");
		words = factory.StopWords;
		assertTrue("words is null and it shouldn't be", words != null);
		assertTrue("words Size: " + words.size() + " is not: " + 4, words.size() == 4);
		assertTrue(factory.IgnoreCase + " does not equal: " + true, factory.IgnoreCase == true);

		factory = (StopFilterFactory) tokenFilterFactory("Stop", "words", "stop-snowball.txt", "format", "snowball", "ignoreCase", "true");
		words = factory.StopWords;
		assertEquals(8, words.size());
		assertTrue(words.contains("he"));
		assertTrue(words.contains("him"));
		assertTrue(words.contains("his"));
		assertTrue(words.contains("himself"));
		assertTrue(words.contains("she"));
		assertTrue(words.contains("her"));
		assertTrue(words.contains("hers"));
		assertTrue(words.contains("herself"));

		// defaults
		factory = (StopFilterFactory) tokenFilterFactory("Stop");
		assertEquals(StopAnalyzer.ENGLISH_STOP_WORDS_SET, factory.StopWords);
		assertEquals(false, factory.IgnoreCase);
	  }

	  /// <summary>
	  /// Test that bogus arguments result in exception </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testBogusArguments() throws Exception
	  public virtual void testBogusArguments()
	  {
		try
		{
		  tokenFilterFactory("Stop", "bogusArg", "bogusValue");
		  fail();
		}
		catch (System.ArgumentException expected)
		{
		  assertTrue(expected.Message.contains("Unknown parameters"));
		}
	  }

	  /// <summary>
	  /// Test that bogus arguments result in exception </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testBogusFormats() throws Exception
	  public virtual void testBogusFormats()
	  {
		try
		{
		  tokenFilterFactory("Stop", "words", "stop-snowball.txt", "format", "bogus");
		  fail();
		}
		catch (System.ArgumentException expected)
		{
		  string msg = expected.Message;
		  assertTrue(msg, msg.Contains("Unknown"));
		  assertTrue(msg, msg.Contains("format"));
		  assertTrue(msg, msg.Contains("bogus"));
		}
		try
		{
		  tokenFilterFactory("Stop", "format", "bogus");
							 // implicit default words file
		  fail();
		}
		catch (System.ArgumentException expected)
		{
		  string msg = expected.Message;
		  assertTrue(msg, msg.Contains("can not be specified"));
		  assertTrue(msg, msg.Contains("format"));
		  assertTrue(msg, msg.Contains("bogus"));
		}
	  }
	}

}