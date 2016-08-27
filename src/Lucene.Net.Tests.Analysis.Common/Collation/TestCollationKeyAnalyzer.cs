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

using System.Globalization;
using Lucene.Net.Util;
using NUnit.Framework;

namespace Lucene.Net.Analysis.Collation
{
	[TestFixture]
	public class TestCollationKeyAnalyzer : CollationTestBase
	{
		private readonly bool InstanceFieldsInitialized = false;

		public TestCollationKeyAnalyzer()
		{
			if (!this.InstanceFieldsInitialized)
			{
				this.InitializeInstanceFields();
				this.InstanceFieldsInitialized = true;
			}
		}
		
		private void InitializeInstanceFields()
		{
			this.analyzer = new CollationKeyAnalyzer(LuceneTestCase.TEST_VERSION_CURRENT, this.collator);
			this.firstRangeBeginning = new BytesRef(this.collator.GetCollationKey(this.FirstRangeBeginningOriginal).KeyData);
			this.firstRangeEnd = new BytesRef(this.collator.GetCollationKey(this.FirstRangeEndOriginal).KeyData);
			this.secondRangeBeginning = new BytesRef(this.collator.GetCollationKey(this.SecondRangeBeginningOriginal).KeyData);
			this.secondRangeEnd = new BytesRef(this.collator.GetCollationKey(this.SecondRangeEndOriginal).KeyData);
		}

		/// <summary>
		/// the sort order of Ø versus U depends on the version of the rules being used
		/// for the inherited root locale: Ø's order isnt specified in Locale.US since 
		/// its not used in english.
		/// </summary>
		private readonly bool oStrokeFirst = Collator.GetInstance(CultureInfo.GetCultureInfo("")).Compare("Ø", "U") < 0;

		/// <summary>
		/// Neither Java 1.4.2 nor 1.5.0 has Farsi Locale collation available in
		/// RuleBasedCollator.  However, the Arabic Locale seems to order the Farsi
		/// characters properly.
		/// </summary>
		private readonly Collator collator = Collator.GetInstance(CultureInfo.GetCultureInfo("ar"));
		private Analyzer analyzer;

		private BytesRef firstRangeBeginning;
		private BytesRef firstRangeEnd;
		private BytesRef secondRangeBeginning;
		private BytesRef secondRangeEnd;

		[Test]
		public virtual void TestInitVars()
		{
			var sortKey = this.collator.GetCollationKey(this.FirstRangeBeginningOriginal);
			
			var r = new BytesRef(sortKey.KeyData);
		}

		[Test]
		public virtual void TestFarsiRangeFilterCollating()
		{
			this.TestFarsiRangeFilterCollating(this.analyzer, this.firstRangeBeginning, this.firstRangeEnd, this.secondRangeBeginning, this.secondRangeEnd);
		}

		[Test]
		public virtual void TestFarsiRangeQueryCollating()
		{
			this.TestFarsiRangeQueryCollating(this.analyzer, this.firstRangeBeginning, this.firstRangeEnd, this.secondRangeBeginning, this.secondRangeEnd);
		}

		[Test]
		public virtual void TestFarsiTermRangeQuery()
		{
			this.TestFarsiTermRangeQuery(this.analyzer, this.firstRangeBeginning, this.firstRangeEnd, this.secondRangeBeginning, this.secondRangeEnd);
		}

		[Test]
		public virtual void TestCollationKeySort()
		{
			Analyzer usAnalyzer = new CollationKeyAnalyzer(LuceneTestCase.TEST_VERSION_CURRENT, Collator.GetInstance(CultureInfo.GetCultureInfo("us")));
			Analyzer franceAnalyzer = new CollationKeyAnalyzer(LuceneTestCase.TEST_VERSION_CURRENT, Collator.GetInstance(CultureInfo.GetCultureInfo("fr")));
			Analyzer swedenAnalyzer = new CollationKeyAnalyzer(LuceneTestCase.TEST_VERSION_CURRENT, Collator.GetInstance(CultureInfo.GetCultureInfo("sv-SE")));
			Analyzer denmarkAnalyzer = new CollationKeyAnalyzer(LuceneTestCase.TEST_VERSION_CURRENT, Collator.GetInstance(CultureInfo.GetCultureInfo("da-DK")));

			// The ICU Collator and Sun java.text.Collator implementations differ in their
			// orderings - "BFJDH" is the ordering for java.text.Collator for Locale.US.
			this.TestCollationKeySort(usAnalyzer, franceAnalyzer, swedenAnalyzer, denmarkAnalyzer, this.oStrokeFirst ? "BFJHD" : "BFJDH", "EACGI", "BJDFH", "BJDHF");
		}

		[Test]
		public virtual void TestThreadSafe()
		{
			var iters = 20 * LuceneTestCase.RANDOM_MULTIPLIER;
			for (var i = 0; i < iters; i++)
			{
				var collator = Collator.GetInstance(CultureInfo.GetCultureInfo("de"));
				collator.Strength = Collator.Primary;
				this.AssertThreadSafe(new CollationKeyAnalyzer(LuceneTestCase.TEST_VERSION_CURRENT, collator));
			}
		}
	}
}