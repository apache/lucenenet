using System.Collections.Generic;
using System.Text;

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

namespace org.apache.lucene.analysis.pattern
{


	using TokenStreamComponents = org.apache.lucene.analysis.Analyzer.TokenStreamComponents;
	using MappingCharFilter = org.apache.lucene.analysis.charfilter.MappingCharFilter;
	using NormalizeCharMap = org.apache.lucene.analysis.charfilter.NormalizeCharMap;
	using PathHierarchyTokenizer = org.apache.lucene.analysis.path.PathHierarchyTokenizer;
	using CharTermAttribute = org.apache.lucene.analysis.tokenattributes.CharTermAttribute;

	public class TestPatternTokenizer : BaseTokenStreamTestCase
	{
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testSplitting() throws Exception
	  public virtual void testSplitting()
	  {
		string qpattern = "\\'([^\\']+)\\'"; // get stuff between "'"
		string[][] tests = new string[][]
		{
			new string[] {"-1", "--", "aaa--bbb--ccc", "aaa bbb ccc"},
			new string[] {"-1", ":", "aaa:bbb:ccc", "aaa bbb ccc"},
			new string[] {"-1", "\\p{Space}", "aaa   bbb \t\tccc  ", "aaa bbb ccc"},
			new string[] {"-1", ":", "boo:and:foo", "boo and foo"},
			new string[] {"-1", "o", "boo:and:foo", "b :and:f"},
			new string[] {"0", ":", "boo:and:foo", ": :"},
			new string[] {"0", qpattern, "aaa 'bbb' 'ccc'", "'bbb' 'ccc'"},
			new string[] {"1", qpattern, "aaa 'bbb' 'ccc'", "bbb ccc"}
		};

		foreach (string[] test in tests)
		{
		  TokenStream stream = new PatternTokenizer(new StringReader(test[2]), Pattern.compile(test[1]), int.Parse(test[0]));
		  string @out = tsToString(stream);
		  // System.out.println( test[2] + " ==> " + out );

		  assertEquals("pattern: " + test[1] + " with input: " + test[2], test[3], @out);

		  // Make sure it is the same as if we called 'split'
		  // test disabled, as we remove empty tokens
		  /*if( "-1".equals( test[0] ) ) {
		    String[] split = test[2].split( test[1] );
		    stream = tokenizer.create( new StringReader( test[2] ) );
		    int i=0;
		    for( Token t = stream.next(); null != t; t = stream.next() ) 
		    {
		      assertEquals( "split: "+test[1] + " "+i, split[i++], new String(t.termBuffer(), 0, t.termLength()) );
		    }
		  }*/
		}
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testOffsetCorrection() throws Exception
	  public virtual void testOffsetCorrection()
	  {
		const string INPUT = "G&uuml;nther G&uuml;nther is here";

		// create MappingCharFilter
		IList<string> mappingRules = new List<string>();
		mappingRules.Add("\"&uuml;\" => \"ü\"");
		NormalizeCharMap.Builder builder = new NormalizeCharMap.Builder();
		builder.add("&uuml;", "ü");
		NormalizeCharMap normMap = builder.build();
		CharFilter charStream = new MappingCharFilter(normMap, new StringReader(INPUT));

		// create PatternTokenizer
		TokenStream stream = new PatternTokenizer(charStream, Pattern.compile("[,;/\\s]+"), -1);
		assertTokenStreamContents(stream, new string[] {"Günther", "Günther", "is", "here"}, new int[] {0, 13, 26, 29}, new int[] {12, 25, 28, 33}, INPUT.Length);

		charStream = new MappingCharFilter(normMap, new StringReader(INPUT));
		stream = new PatternTokenizer(charStream, Pattern.compile("Günther"), 0);
		assertTokenStreamContents(stream, new string[] {"Günther", "Günther"}, new int[] {0, 13}, new int[] {12, 25}, INPUT.Length);
	  }

	  /// <summary>
	  /// TODO: rewrite tests not to use string comparison.
	  /// </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: private static String tsToString(org.apache.lucene.analysis.TokenStream in) throws java.io.IOException
	  private static string tsToString(TokenStream @in)
	  {
		StringBuilder @out = new StringBuilder();
		CharTermAttribute termAtt = @in.addAttribute(typeof(CharTermAttribute));
		// extra safety to enforce, that the state is not preserved and also
		// assign bogus values
		@in.clearAttributes();
		termAtt.setEmpty().append("bogusTerm");
		@in.reset();
		while (@in.incrementToken())
		{
		  if (@out.Length > 0)
		  {
			@out.Append(' ');
		  }
		  @out.Append(termAtt.ToString());
		  @in.clearAttributes();
		  termAtt.setEmpty().append("bogusTerm");
		}

		@in.close();
		return @out.ToString();
	  }

	  /// <summary>
	  /// blast some random strings through the analyzer </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testRandomStrings() throws Exception
	  public virtual void testRandomStrings()
	  {
		Analyzer a = new AnalyzerAnonymousInnerClassHelper(this);
		checkRandomData(random(), a, 1000 * RANDOM_MULTIPLIER);

		Analyzer b = new AnalyzerAnonymousInnerClassHelper2(this);
		checkRandomData(random(), b, 1000 * RANDOM_MULTIPLIER);
	  }

	  private class AnalyzerAnonymousInnerClassHelper : Analyzer
	  {
		  private readonly TestPatternTokenizer outerInstance;

		  public AnalyzerAnonymousInnerClassHelper(TestPatternTokenizer outerInstance)
		  {
			  this.outerInstance = outerInstance;
		  }

		  protected internal override Analyzer.TokenStreamComponents createComponents(string fieldName, Reader reader)
		  {
			Tokenizer tokenizer = new PatternTokenizer(reader, Pattern.compile("a"), -1);
			return new Analyzer.TokenStreamComponents(tokenizer);
		  }
	  }

	  private class AnalyzerAnonymousInnerClassHelper2 : Analyzer
	  {
		  private readonly TestPatternTokenizer outerInstance;

		  public AnalyzerAnonymousInnerClassHelper2(TestPatternTokenizer outerInstance)
		  {
			  this.outerInstance = outerInstance;
		  }

		  protected internal override Analyzer.TokenStreamComponents createComponents(string fieldName, Reader reader)
		  {
			Tokenizer tokenizer = new PatternTokenizer(reader, Pattern.compile("a"), 0);
			return new Analyzer.TokenStreamComponents(tokenizer);
		  }
	  }
	}

}