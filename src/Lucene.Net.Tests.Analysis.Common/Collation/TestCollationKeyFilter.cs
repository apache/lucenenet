using System;

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


	using org.apache.lucene.analysis;
	using KeywordTokenizer = org.apache.lucene.analysis.core.KeywordTokenizer;
	using BytesRef = org.apache.lucene.util.BytesRef;


	/// @deprecated remove when CollationKeyFilter is removed. 
	[Obsolete("remove when CollationKeyFilter is removed.")]
	public class TestCollationKeyFilter : CollationTestBase
	{
		private bool InstanceFieldsInitialized = false;

		public TestCollationKeyFilter()
		{
			if (!InstanceFieldsInitialized)
			{
				InitializeInstanceFields();
				InstanceFieldsInitialized = true;
			}
		}

		private void InitializeInstanceFields()
		{
			analyzer = new TestAnalyzer(this, collator);
			firstRangeBeginning = new BytesRef(encodeCollationKey(collator.getCollationKey(firstRangeBeginningOriginal).toByteArray()));
			firstRangeEnd = new BytesRef(encodeCollationKey(collator.getCollationKey(firstRangeEndOriginal).toByteArray()));
			secondRangeBeginning = new BytesRef(encodeCollationKey(collator.getCollationKey(secondRangeBeginningOriginal).toByteArray()));
			secondRangeEnd = new BytesRef(encodeCollationKey(collator.getCollationKey(secondRangeEndOriginal).toByteArray()));
		}

	  // the sort order of Ø versus U depends on the version of the rules being used
	  // for the inherited root locale: Ø's order isnt specified in Locale.US since 
	  // its not used in english.
	  internal bool oStrokeFirst = Collator.getInstance(new Locale("")).compare("Ø", "U") < 0;

	  // Neither Java 1.4.2 nor 1.5.0 has Farsi Locale collation available in
	  // RuleBasedCollator.  However, the Arabic Locale seems to order the Farsi
	  // characters properly.
	  private Collator collator = Collator.getInstance(new Locale("ar"));
	  private Analyzer analyzer;

	  private BytesRef firstRangeBeginning;
	  private BytesRef firstRangeEnd;
	  private BytesRef secondRangeBeginning;
	  private BytesRef secondRangeEnd;


	  public sealed class TestAnalyzer : Analyzer
	  {
		  private readonly TestCollationKeyFilter outerInstance;

		internal Collator _collator;

		internal TestAnalyzer(TestCollationKeyFilter outerInstance, Collator collator)
		{
			this.outerInstance = outerInstance;
		  _collator = collator;
		}

		public override TokenStreamComponents createComponents(string fieldName, Reader reader)
		{
		  Tokenizer result = new KeywordTokenizer(reader);
		  return new TokenStreamComponents(result, new CollationKeyFilter(result, _collator));
		}
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testFarsiRangeFilterCollating() throws Exception
	  public virtual void testFarsiRangeFilterCollating()
	  {
		testFarsiRangeFilterCollating(analyzer, firstRangeBeginning, firstRangeEnd, secondRangeBeginning, secondRangeEnd);
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testFarsiRangeQueryCollating() throws Exception
	  public virtual void testFarsiRangeQueryCollating()
	  {
		testFarsiRangeQueryCollating(analyzer, firstRangeBeginning, firstRangeEnd, secondRangeBeginning, secondRangeEnd);
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testFarsiTermRangeQuery() throws Exception
	  public virtual void testFarsiTermRangeQuery()
	  {
		testFarsiTermRangeQuery(analyzer, firstRangeBeginning, firstRangeEnd, secondRangeBeginning, secondRangeEnd);
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testCollationKeySort() throws Exception
	  public virtual void testCollationKeySort()
	  {
		Analyzer usAnalyzer = new TestAnalyzer(this, Collator.getInstance(Locale.US));
		Analyzer franceAnalyzer = new TestAnalyzer(this, Collator.getInstance(Locale.FRANCE));
		Analyzer swedenAnalyzer = new TestAnalyzer(this, Collator.getInstance(new Locale("sv", "se")));
		Analyzer denmarkAnalyzer = new TestAnalyzer(this, Collator.getInstance(new Locale("da", "dk")));

		// The ICU Collator and Sun java.text.Collator implementations differ in their
		// orderings - "BFJDH" is the ordering for java.text.Collator for Locale.US.
		testCollationKeySort(usAnalyzer, franceAnalyzer, swedenAnalyzer, denmarkAnalyzer, oStrokeFirst ? "BFJHD" : "BFJDH", "EACGI", "BJDFH", "BJDHF");
	  }
	}

}