using System;

namespace org.apache.lucene.analysis.ar
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


	using Version = org.apache.lucene.util.Version;

	/// <summary>
	/// Testcase for <seealso cref="TestArabicLetterTokenizer"/> </summary>
	/// @deprecated (3.1) Remove in Lucene 5.0 
	[Obsolete("(3.1) Remove in Lucene 5.0")]
	public class TestArabicLetterTokenizer : BaseTokenStreamTestCase
	{

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testArabicLetterTokenizer() throws java.io.IOException
	  public virtual void testArabicLetterTokenizer()
	  {
		StringReader reader = new StringReader("1234567890 Tokenizer \ud801\udc1c\u0300test");
		ArabicLetterTokenizer tokenizer = new ArabicLetterTokenizer(Version.LUCENE_31, reader);
		assertTokenStreamContents(tokenizer, new string[] {"Tokenizer", "\ud801\udc1c\u0300test"});
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testArabicLetterTokenizerBWCompat() throws java.io.IOException
	  public virtual void testArabicLetterTokenizerBWCompat()
	  {
		StringReader reader = new StringReader("1234567890 Tokenizer \ud801\udc1c\u0300test");
		ArabicLetterTokenizer tokenizer = new ArabicLetterTokenizer(Version.LUCENE_30, reader);
		assertTokenStreamContents(tokenizer, new string[] {"Tokenizer", "\u0300test"});
	  }
	}

}