namespace org.apache.lucene.analysis.en
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

//JAVA TO C# CONVERTER TODO TASK: This Java 'import static' statement cannot be converted to C#:
//	import static org.apache.lucene.analysis.VocabularyAssert.assertVocabulary;


	using KeywordTokenizer = org.apache.lucene.analysis.core.KeywordTokenizer;

	/// <summary>
	/// Tests for <seealso cref="KStemmer"/>
	/// </summary>
	public class TestKStemmer : BaseTokenStreamTestCase
	{
	  internal Analyzer a = new AnalyzerAnonymousInnerClassHelper();

	  private class AnalyzerAnonymousInnerClassHelper : Analyzer
	  {
		  public AnalyzerAnonymousInnerClassHelper()
		  {
		  }

		  protected internal override TokenStreamComponents createComponents(string fieldName, Reader reader)
		  {
			Tokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.WHITESPACE, true);
			return new TokenStreamComponents(tokenizer, new KStemFilter(tokenizer));
		  }
	  }

	  /// <summary>
	  /// blast some random strings through the analyzer </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testRandomStrings() throws Exception
	  public virtual void testRandomStrings()
	  {
		checkRandomData(random(), a, 1000 * RANDOM_MULTIPLIER);
	  }

	  /// <summary>
	  /// test the kstemmer optimizations against a bunch of words
	  /// that were stemmed with the original java kstemmer (generated from
	  /// testCreateMap, commented out below).
	  /// </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testVocabulary() throws Exception
	  public virtual void testVocabulary()
	  {
		assertVocabulary(a, getDataFile("kstemTestData.zip"), "kstem_examples.txt");
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testEmptyTerm() throws java.io.IOException
	  public virtual void testEmptyTerm()
	  {
		Analyzer a = new AnalyzerAnonymousInnerClassHelper2(this);
		checkOneTerm(a, "", "");
	  }

	  private class AnalyzerAnonymousInnerClassHelper2 : Analyzer
	  {
		  private readonly TestKStemmer outerInstance;

		  public AnalyzerAnonymousInnerClassHelper2(TestKStemmer outerInstance)
		  {
			  this.outerInstance = outerInstance;
		  }

		  protected internal override TokenStreamComponents createComponents(string fieldName, Reader reader)
		  {
			Tokenizer tokenizer = new KeywordTokenizer(reader);
			return new TokenStreamComponents(tokenizer, new KStemFilter(tokenizer));
		  }
	  }

	  /// <summary>
	  ///**** requires original java kstem source code to create map
	  /// public void testCreateMap() throws Exception {
	  ///  String input = getBigDoc();
	  ///  Reader r = new StringReader(input);
	  ///  TokenFilter tf = new LowerCaseFilter(new LetterTokenizer(r));
	  ///  // tf = new KStemFilter(tf);
	  /// 
	  ///  KStemmer kstem = new KStemmer();
	  ///  Map<String,String> map = new TreeMap<>();
	  ///  for(;;) {
	  ///    Token t = tf.next();
	  ///    if (t==null) break;
	  ///    String s = t.termText();
	  ///    if (map.containsKey(s)) continue;
	  ///    map.put(s, kstem.stem(s));
	  ///  }
	  /// 
	  ///  Writer out = new BufferedWriter(new FileWriter("kstem_examples.txt"));
	  ///  for (String key : map.keySet()) {
	  ///    out.write(key);
	  ///    out.write('\t');
	  ///    out.write(map.get(key));
	  ///    out.write('\n');
	  ///  }
	  ///  out.close();
	  /// }
	  /// *****
	  /// </summary>

	}

}