namespace org.apache.lucene.analysis.snowball
{

	/// <summary>
	/// Copyright 2004 The Apache Software Foundation
	/// 
	/// Licensed under the Apache License, Version 2.0 (the "License");
	/// you may not use this file except in compliance with the License.
	/// You may obtain a copy of the License at
	/// 
	///     http://www.apache.org/licenses/LICENSE-2.0
	/// 
	/// Unless required by applicable law or agreed to in writing, software
	/// distributed under the License is distributed on an "AS IS" BASIS,
	/// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
	/// See the License for the specific language governing permissions and
	/// limitations under the License.
	/// </summary>

	using BaseTokenStreamFactoryTestCase = org.apache.lucene.analysis.util.BaseTokenStreamFactoryTestCase;
	using StringMockResourceLoader = org.apache.lucene.analysis.util.StringMockResourceLoader;
	using EnglishStemmer = org.tartarus.snowball.ext.EnglishStemmer;


	public class TestSnowballPorterFilterFactory : BaseTokenStreamFactoryTestCase
	{

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void test() throws Exception
	  public virtual void test()
	  {
		string text = "The fledgling banks were counting on a big boom in banking";
		EnglishStemmer stemmer = new EnglishStemmer();
		string[] test = text.Split("\\s", true);
		string[] gold = new string[test.Length];
		for (int i = 0; i < test.Length; i++)
		{
		  stemmer.Current = test[i];
		  stemmer.stem();
		  gold[i] = stemmer.Current;
		}

		Reader reader = new StringReader(text);
		TokenStream stream = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
		stream = tokenFilterFactory("SnowballPorter", "language", "English").create(stream);
		assertTokenStreamContents(stream, gold);
	  }

	  /// <summary>
	  /// Test the protected words mechanism of SnowballPorterFilterFactory
	  /// </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testProtected() throws Exception
	  public virtual void testProtected()
	  {
		Reader reader = new StringReader("ridding of some stemming");
		TokenStream stream = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
		stream = tokenFilterFactory("SnowballPorter", TEST_VERSION_CURRENT, new StringMockResourceLoader("ridding"), "protected", "protwords.txt", "language", "English").create(stream);

		assertTokenStreamContents(stream, new string[] {"ridding", "of", "some", "stem"});
	  }

	  /// <summary>
	  /// Test that bogus arguments result in exception </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testBogusArguments() throws Exception
	  public virtual void testBogusArguments()
	  {
		try
		{
		  tokenFilterFactory("SnowballPorter", "bogusArg", "bogusValue");
		  fail();
		}
		catch (System.ArgumentException expected)
		{
		  assertTrue(expected.Message.contains("Unknown parameters"));
		}
	  }
	}


}