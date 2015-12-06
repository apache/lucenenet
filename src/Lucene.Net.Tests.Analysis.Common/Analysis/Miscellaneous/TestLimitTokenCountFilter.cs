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

	using Test = org.junit.Test;

	public class TestLimitTokenCountFilter : BaseTokenStreamTestCase
	{

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void test() throws Exception
	  public virtual void test()
	  {
		foreach (bool consumeAll in new bool[]{true, false})
		{
		  MockTokenizer tokenizer = new MockTokenizer(new StringReader("A1 B2 C3 D4 E5 F6"), MockTokenizer.WHITESPACE, false);
		  tokenizer.EnableChecks = consumeAll;
		  TokenStream stream = new LimitTokenCountFilter(tokenizer, 3, consumeAll);
		  assertTokenStreamContents(stream, new string[]{"A1", "B2", "C3"});
		}
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test(expected = IllegalArgumentException.class) public void testIllegalArguments() throws Exception
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
	  public virtual void testIllegalArguments()
	  {
		new LimitTokenCountFilter(new MockTokenizer(new StringReader("A1 B2 C3 D4 E5 F6"), MockTokenizer.WHITESPACE, false), -1);
	  }
	}

}