namespace org.apache.lucene.analysis.miscellaneous
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

	using LuceneTestCase = org.apache.lucene.util.LuceneTestCase;
	using AttributeImpl = org.apache.lucene.util.AttributeImpl;
	using CharTermAttribute = org.apache.lucene.analysis.tokenattributes.CharTermAttribute;

	public class TestSingleTokenTokenFilter : LuceneTestCase
	{

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void test() throws java.io.IOException
	  public virtual void test()
	  {
		Token token = new Token();
		SingleTokenTokenStream ts = new SingleTokenTokenStream(token);
		AttributeImpl tokenAtt = (AttributeImpl) ts.addAttribute(typeof(CharTermAttribute));
		assertTrue(tokenAtt is Token);
		ts.reset();

		assertTrue(ts.incrementToken());
		assertEquals(token, tokenAtt);
		assertFalse(ts.incrementToken());

		token = new Token("hallo", 10, 20, "someType");
		ts.Token = token;
		ts.reset();

		assertTrue(ts.incrementToken());
		assertEquals(token, tokenAtt);
		assertFalse(ts.incrementToken());
	  }
	}

}