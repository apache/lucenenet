namespace org.apache.lucene.analysis.sinks
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

	public class TokenRangeSinkTokenizerTest : BaseTokenStreamTestCase
	{

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void test() throws java.io.IOException
	  public virtual void test()
	  {
		TokenRangeSinkFilter sinkFilter = new TokenRangeSinkFilter(2, 4);
		string test = "The quick red fox jumped over the lazy brown dogs";
		TeeSinkTokenFilter tee = new TeeSinkTokenFilter(new MockTokenizer(new StringReader(test), MockTokenizer.WHITESPACE, false));
		TeeSinkTokenFilter.SinkTokenStream rangeToks = tee.newSinkTokenStream(sinkFilter);

		int count = 0;
		tee.reset();
		while (tee.incrementToken())
		{
		  count++;
		}

		int sinkCount = 0;
		rangeToks.reset();
		while (rangeToks.incrementToken())
		{
		  sinkCount++;
		}

		assertTrue(count + " does not equal: " + 10, count == 10);
		assertTrue("rangeToks Size: " + sinkCount + " is not: " + 2, sinkCount == 2);
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test(expected = IllegalArgumentException.class) public void testIllegalArguments() throws Exception
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
	  public virtual void testIllegalArguments()
	  {
		new TokenRangeSinkFilter(4, 2);
	  }
	}
}