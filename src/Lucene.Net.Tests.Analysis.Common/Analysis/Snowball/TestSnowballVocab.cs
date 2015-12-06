using System;

namespace org.apache.lucene.analysis.snowball
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
	using LuceneTestCase = org.apache.lucene.util.LuceneTestCase;

//JAVA TO C# CONVERTER TODO TASK: This Java 'import static' statement cannot be converted to C#:
//	import static org.apache.lucene.analysis.VocabularyAssert.*;

	/// <summary>
	/// Test the snowball filters against the snowball data tests
	/// </summary>
	public class TestSnowballVocab : LuceneTestCase
	{
	  /// <summary>
	  /// Run all languages against their snowball vocabulary tests.
	  /// </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testStemmers() throws java.io.IOException
	  public virtual void testStemmers()
	  {
		assertCorrectOutput("Danish", "danish");
		assertCorrectOutput("Dutch", "dutch");
		assertCorrectOutput("English", "english");
		// disabled due to snowball java code generation bug: 
		// see http://article.gmane.org/gmane.comp.search.snowball/1139
		// assertCorrectOutput("Finnish", "finnish");
		assertCorrectOutput("French", "french");
		assertCorrectOutput("German", "german");
		assertCorrectOutput("German2", "german2");
		assertCorrectOutput("Hungarian", "hungarian");
		assertCorrectOutput("Italian", "italian");
		assertCorrectOutput("Kp", "kraaij_pohlmann");
		// disabled due to snowball java code generation bug: 
		// see http://article.gmane.org/gmane.comp.search.snowball/1139
		// assertCorrectOutput("Lovins", "lovins");
		assertCorrectOutput("Norwegian", "norwegian");
		assertCorrectOutput("Porter", "porter");
		assertCorrectOutput("Portuguese", "portuguese");
		assertCorrectOutput("Romanian", "romanian");
		assertCorrectOutput("Russian", "russian");
		assertCorrectOutput("Spanish", "spanish");
		assertCorrectOutput("Swedish", "swedish");
		assertCorrectOutput("Turkish", "turkish");
	  }

	  /// <summary>
	  /// For the supplied language, run the stemmer against all strings in voc.txt
	  /// The output should be the same as the string in output.txt
	  /// </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: private void assertCorrectOutput(final String snowballLanguage, String dataDirectory) throws java.io.IOException
//JAVA TO C# CONVERTER WARNING: 'final' parameters are not available in .NET:
	  private void assertCorrectOutput(string snowballLanguage, string dataDirectory)
	  {
		if (VERBOSE)
		{
			Console.WriteLine("checking snowball language: " + snowballLanguage);
		}

		Analyzer a = new AnalyzerAnonymousInnerClassHelper(this, snowballLanguage);

		assertVocabulary(a, getDataFile("TestSnowballVocabData.zip"), dataDirectory + "/voc.txt", dataDirectory + "/output.txt");
	  }

	  private class AnalyzerAnonymousInnerClassHelper : Analyzer
	  {
		  private readonly TestSnowballVocab outerInstance;

		  private string snowballLanguage;

		  public AnalyzerAnonymousInnerClassHelper(TestSnowballVocab outerInstance, string snowballLanguage)
		  {
			  this.outerInstance = outerInstance;
			  this.snowballLanguage = snowballLanguage;
		  }

		  protected internal override TokenStreamComponents createComponents(string fieldName, Reader reader)
		  {
			Tokenizer t = new KeywordTokenizer(reader);
			return new TokenStreamComponents(t, new SnowballFilter(t, snowballLanguage));
		  }
	  }
	}

}