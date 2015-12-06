namespace org.apache.lucene.analysis.ckb
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


	using KeywordTokenizer = org.apache.lucene.analysis.core.KeywordTokenizer;

	/// <summary>
	/// Tests normalization for Sorani (this is more critical than stemming...)
	/// </summary>
	public class TestSoraniNormalizationFilter : BaseTokenStreamTestCase
	{
	  internal Analyzer a = new AnalyzerAnonymousInnerClassHelper();

	  private class AnalyzerAnonymousInnerClassHelper : Analyzer
	  {
		  public AnalyzerAnonymousInnerClassHelper()
		  {
		  }

		  protected internal override TokenStreamComponents createComponents(string fieldName, Reader reader)
		  {
			Tokenizer tokenizer = new KeywordTokenizer(reader);
			return new TokenStreamComponents(tokenizer, new SoraniNormalizationFilter(tokenizer));
		  }
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testY() throws Exception
	  public virtual void testY()
	  {
		checkOneTerm(a, "\u064A", "\u06CC");
		checkOneTerm(a, "\u0649", "\u06CC");
		checkOneTerm(a, "\u06CC", "\u06CC");
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testK() throws Exception
	  public virtual void testK()
	  {
		checkOneTerm(a, "\u0643", "\u06A9");
		checkOneTerm(a, "\u06A9", "\u06A9");
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testH() throws Exception
	  public virtual void testH()
	  {
		// initial
		checkOneTerm(a, "\u0647\u200C", "\u06D5");
		// medial
		checkOneTerm(a, "\u0647\u200C\u06A9", "\u06D5\u06A9");

		checkOneTerm(a, "\u06BE", "\u0647");
		checkOneTerm(a, "\u0629", "\u06D5");
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testFinalH() throws Exception
	  public virtual void testFinalH()
	  {
		// always (and in final form by def), so frequently omitted
		checkOneTerm(a, "\u0647\u0647\u0647", "\u0647\u0647\u06D5");
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testRR() throws Exception
	  public virtual void testRR()
	  {
		checkOneTerm(a, "\u0692", "\u0695");
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testInitialRR() throws Exception
	  public virtual void testInitialRR()
	  {
		// always, so frequently omitted
		checkOneTerm(a, "\u0631\u0631\u0631", "\u0695\u0631\u0631");
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testRemove() throws Exception
	  public virtual void testRemove()
	  {
		checkOneTerm(a, "\u0640", "");
		checkOneTerm(a, "\u064B", "");
		checkOneTerm(a, "\u064C", "");
		checkOneTerm(a, "\u064D", "");
		checkOneTerm(a, "\u064E", "");
		checkOneTerm(a, "\u064F", "");
		checkOneTerm(a, "\u0650", "");
		checkOneTerm(a, "\u0651", "");
		checkOneTerm(a, "\u0652", "");
		// we peek backwards in this case to look for h+200C, ensure this works
		checkOneTerm(a, "\u200C", "");
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testEmptyTerm() throws java.io.IOException
	  public virtual void testEmptyTerm()
	  {
		checkOneTerm(a, "", "");
	  }
	}

}