using System.Collections.Generic;

namespace org.apache.lucene.collation
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


	using MockTokenizer = org.apache.lucene.analysis.MockTokenizer;
	using TokenStream = org.apache.lucene.analysis.TokenStream;
	using CharTermAttribute = org.apache.lucene.analysis.tokenattributes.CharTermAttribute;
	using BaseTokenStreamFactoryTestCase = org.apache.lucene.analysis.util.BaseTokenStreamFactoryTestCase;
	using StringMockResourceLoader = org.apache.lucene.analysis.util.StringMockResourceLoader;
	using TokenFilterFactory = org.apache.lucene.analysis.util.TokenFilterFactory;

	public class TestCollationKeyFilterFactory : BaseTokenStreamFactoryTestCase
	{

	  /*
	   * Turkish has some funny casing.
	   * This test shows how you can solve this kind of thing easily with collation.
	   * Instead of using LowerCaseFilter, use a turkish collator with primary strength.
	   * Then things will sort and match correctly.
	   */
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testBasicUsage() throws Exception
	  public virtual void testBasicUsage()
	  {
		string turkishUpperCase = "I WİLL USE TURKİSH CASING";
		string turkishLowerCase = "ı will use turkish casıng";
		TokenFilterFactory factory = tokenFilterFactory("CollationKey", "language", "tr", "strength", "primary");
		TokenStream tsUpper = factory.create(new MockTokenizer(new StringReader(turkishUpperCase), MockTokenizer.KEYWORD, false));
		TokenStream tsLower = factory.create(new MockTokenizer(new StringReader(turkishLowerCase), MockTokenizer.KEYWORD, false));
		assertCollatesToSame(tsUpper, tsLower);
	  }

	  /*
	   * Test usage of the decomposition option for unicode normalization.
	   */
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testNormalization() throws Exception
	  public virtual void testNormalization()
	  {
		string turkishUpperCase = "I W\u0049\u0307LL USE TURKİSH CASING";
		string turkishLowerCase = "ı will use turkish casıng";
		TokenFilterFactory factory = tokenFilterFactory("CollationKey", "language", "tr", "strength", "primary", "decomposition", "canonical");
		TokenStream tsUpper = factory.create(new MockTokenizer(new StringReader(turkishUpperCase), MockTokenizer.KEYWORD, false));
		TokenStream tsLower = factory.create(new MockTokenizer(new StringReader(turkishLowerCase), MockTokenizer.KEYWORD, false));
		assertCollatesToSame(tsUpper, tsLower);
	  }

	  /*
	   * Test usage of the K decomposition option for unicode normalization.
	   * This works even with identical strength.
	   */
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testFullDecomposition() throws Exception
	  public virtual void testFullDecomposition()
	  {
		string fullWidth = "Ｔｅｓｔｉｎｇ";
		string halfWidth = "Testing";
		TokenFilterFactory factory = tokenFilterFactory("CollationKey", "language", "zh", "strength", "identical", "decomposition", "full");
		TokenStream tsFull = factory.create(new MockTokenizer(new StringReader(fullWidth), MockTokenizer.KEYWORD, false));
		TokenStream tsHalf = factory.create(new MockTokenizer(new StringReader(halfWidth), MockTokenizer.KEYWORD, false));
		assertCollatesToSame(tsFull, tsHalf);
	  }

	  /*
	   * Test secondary strength, for english case is not significant.
	   */
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testSecondaryStrength() throws Exception
	  public virtual void testSecondaryStrength()
	  {
		string upperCase = "TESTING";
		string lowerCase = "testing";
		TokenFilterFactory factory = tokenFilterFactory("CollationKey", "language", "en", "strength", "secondary", "decomposition", "no");
		TokenStream tsUpper = factory.create(new MockTokenizer(new StringReader(upperCase), MockTokenizer.KEYWORD, false));
		TokenStream tsLower = factory.create(new MockTokenizer(new StringReader(lowerCase), MockTokenizer.KEYWORD, false));
		assertCollatesToSame(tsUpper, tsLower);
	  }

	  /*
	   * For german, you might want oe to sort and match with o umlaut.
	   * This is not the default, but you can make a customized ruleset to do this.
	   *
	   * The default is DIN 5007-1, this shows how to tailor a collator to get DIN 5007-2 behavior.
	   *  http://bugs.sun.com/bugdatabase/view_bug.do?bug_id=4423383
	   */
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testCustomRules() throws Exception
	  public virtual void testCustomRules()
	  {
		RuleBasedCollator baseCollator = (RuleBasedCollator) Collator.getInstance(new Locale("de", "DE"));

		string DIN5007_2_tailorings = "& ae , a\u0308 & AE , A\u0308" + "& oe , o\u0308 & OE , O\u0308" + "& ue , u\u0308 & UE , u\u0308";

		RuleBasedCollator tailoredCollator = new RuleBasedCollator(baseCollator.Rules + DIN5007_2_tailorings);
		string tailoredRules = tailoredCollator.Rules;
		//
		// at this point, you would save these tailoredRules to a file, 
		// and use the custom parameter.
		//
		string germanUmlaut = "Töne";
		string germanOE = "Toene";
		IDictionary<string, string> args = new Dictionary<string, string>();
		args["custom"] = "rules.txt";
		args["strength"] = "primary";
		CollationKeyFilterFactory factory = new CollationKeyFilterFactory(args);
		factory.inform(new StringMockResourceLoader(tailoredRules));
		TokenStream tsUmlaut = factory.create(new MockTokenizer(new StringReader(germanUmlaut), MockTokenizer.KEYWORD, false));
		TokenStream tsOE = factory.create(new MockTokenizer(new StringReader(germanOE), MockTokenizer.KEYWORD, false));

		assertCollatesToSame(tsUmlaut, tsOE);
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: private void assertCollatesToSame(org.apache.lucene.analysis.TokenStream stream1, org.apache.lucene.analysis.TokenStream stream2) throws java.io.IOException
	  private void assertCollatesToSame(TokenStream stream1, TokenStream stream2)
	  {
		stream1.reset();
		stream2.reset();
		CharTermAttribute term1 = stream1.addAttribute(typeof(CharTermAttribute));
		CharTermAttribute term2 = stream2.addAttribute(typeof(CharTermAttribute));
		assertTrue(stream1.incrementToken());
		assertTrue(stream2.incrementToken());
		assertEquals(term1.ToString(), term2.ToString());
		assertFalse(stream1.incrementToken());
		assertFalse(stream2.incrementToken());
		stream1.end();
		stream2.end();
		stream1.close();
		stream2.close();
	  }
	}

}