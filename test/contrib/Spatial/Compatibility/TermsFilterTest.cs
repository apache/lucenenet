/* 
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 * 
 * http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Collections.Generic;
using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Spatial.Util;
using Lucene.Net.Store;
using Lucene.Net.Util;
using NUnit.Framework;

namespace Lucene.Net.Contrib.Spatial.Test.Compatibility
{
	public class TermsFilterTest : LuceneTestCase
	{
		[Test]
		public void testCachability()
		{
			TermsFilter a = new TermsFilter();
			a.AddTerm(new Term("field1", "a"));
			a.AddTerm(new Term("field1", "b"));
			HashSet<Filter> cachedFilters = new HashSet<Filter>();
			cachedFilters.Add(a);
			TermsFilter b = new TermsFilter();
			b.AddTerm(new Term("field1", "a"));
			b.AddTerm(new Term("field1", "b"));

			Assert.True(cachedFilters.Contains(b), "Must be cached");
			b.AddTerm(new Term("field1", "a")); //duplicate term
			Assert.True(cachedFilters.Contains(b), "Must be cached");
			b.AddTerm(new Term("field1", "c"));
			Assert.False(cachedFilters.Contains(b), "Must not be cached");
		}

		[Test]
		public void testMissingTerms()
		{
			String fieldName = "field1";
			Directory rd = new RAMDirectory();
			var w = new IndexWriter(rd, new KeywordAnalyzer(), IndexWriter.MaxFieldLength.UNLIMITED);
			for (int i = 0; i < 100; i++)
			{
				var doc = new Document();
				int term = i*10; //terms are units of 10;
				doc.Add(new Field(fieldName, "" + term, Field.Store.YES, Field.Index.ANALYZED));
				w.AddDocument(doc);
			}
			IndexReader reader = w.GetReader();
			w.Close();

			TermsFilter tf = new TermsFilter();
			tf.AddTerm(new Term(fieldName, "19"));
			FixedBitSet bits = (FixedBitSet) tf.GetDocIdSet(reader);
			Assert.AreEqual(0, bits.Cardinality(), "Must match nothing");

			tf.AddTerm(new Term(fieldName, "20"));
			bits = (FixedBitSet) tf.GetDocIdSet(reader);
			Assert.AreEqual(1, bits.Cardinality(), "Must match 1");

			tf.AddTerm(new Term(fieldName, "10"));
			bits = (FixedBitSet) tf.GetDocIdSet(reader);
			Assert.AreEqual(2, bits.Cardinality(), "Must match 2");

			tf.AddTerm(new Term(fieldName, "00"));
			bits = (FixedBitSet) tf.GetDocIdSet(reader);
			Assert.AreEqual(2, bits.Cardinality(), "Must match 2");

			reader.Close();
			rd.Close();
		}

		//    [Test]
		//    public void testMissingField()
		//    {
		//        String fieldName = "field1";
		//        Directory rd1 = new RAMDirectory();
		//        var w1 = new IndexWriter(rd1, new KeywordAnalyzer(), IndexWriter.MaxFieldLength.UNLIMITED);
		//        var doc = new Document();
		//        doc.Add(new Field(fieldName, "content1", Field.Store.YES, Field.Index.ANALYZED));
		//        w1.AddDocument(doc);
		//        IndexReader reader1 = w1.GetReader();
		//        w1.Close();

		//        fieldName = "field2";
		//        Directory rd2 = new RAMDirectory();
		//        var w2 = new IndexWriter(rd2, new KeywordAnalyzer(), IndexWriter.MaxFieldLength.UNLIMITED);
		//        doc = new Document();
		//        doc.Add(new Field(fieldName, "content2", Field.Store.YES, Field.Index.ANALYZED));
		//        w2.AddDocument(doc);
		//        IndexReader reader2 = w2.GetReader();
		//        w2.Close();

		//        TermsFilter tf = new TermsFilter();
		//        tf.AddTerm(new Term(fieldName, "content1"));

		//        MultiReader multi = new MultiReader(reader1, reader2);
		//        foreach (var reader in multi.Leaves())
		//        {
		//            FixedBitSet bits = (FixedBitSet) tf.GetDocIdSet(reader);
		//            Assert.True(bits.Cardinality() >= 0, "Must be >= 0");
		//        }
		//        multi.Close();
		//        reader1.Close();
		//        reader2.Close();
		//        rd1.Close();
		//        rd2.Close();
		//    }
	}
}
