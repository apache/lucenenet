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

	/// <summary>
	/// Test the truncate token filter.
	/// </summary>
	public class TestTruncateTokenFilter : BaseTokenStreamTestCase
	{

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testTruncating() throws Exception
	  public virtual void testTruncating()
	  {
		TokenStream stream = new MockTokenizer(new StringReader("abcdefg 1234567 ABCDEFG abcde abc 12345 123"), MockTokenizer.WHITESPACE, false);
		stream = new TruncateTokenFilter(stream, 5);
		assertTokenStreamContents(stream, new string[]{"abcde", "12345", "ABCDE", "abcde", "abc", "12345", "123"});
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test(expected = IllegalArgumentException.class) public void testNonPositiveLength() throws Exception
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
	  public virtual void testNonPositiveLength()
	  {
		new TruncateTokenFilter(new MockTokenizer(new StringReader("length must be a positive number")), -48);
	  }
	}

}