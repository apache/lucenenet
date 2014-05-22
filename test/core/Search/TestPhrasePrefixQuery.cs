using System.Collections.Generic;

namespace Lucene.Net.Search
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

	using Field = Lucene.Net.Document.Field;
	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
	using Document = Lucene.Net.Document.Document;
	using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;
	using TermsEnum = Lucene.Net.Index.TermsEnum;
	using IndexReader = Lucene.Net.Index.IndexReader;
	using Term = Lucene.Net.Index.Term;
	using MultiFields = Lucene.Net.Index.MultiFields;
	using BytesRef = Lucene.Net.Util.BytesRef;
	using Directory = Lucene.Net.Store.Directory;


	/// <summary>
	/// this class tests PhrasePrefixQuery class.
	/// </summary>
	public class TestPhrasePrefixQuery : LuceneTestCase
	{

	  ///   
	  public virtual void TestPhrasePrefix()
	  {
		Directory indexStore = newDirectory();
		RandomIndexWriter writer = new RandomIndexWriter(random(), indexStore);
		Document doc1 = new Document();
		Document doc2 = new Document();
		Document doc3 = new Document();
		Document doc4 = new Document();
		Document doc5 = new Document();
		doc1.add(newTextField("body", "blueberry pie", Field.Store.YES));
		doc2.add(newTextField("body", "blueberry strudel", Field.Store.YES));
		doc3.add(newTextField("body", "blueberry pizza", Field.Store.YES));
		doc4.add(newTextField("body", "blueberry chewing gum", Field.Store.YES));
		doc5.add(newTextField("body", "piccadilly circus", Field.Store.YES));
		writer.addDocument(doc1);
		writer.addDocument(doc2);
		writer.addDocument(doc3);
		writer.addDocument(doc4);
		writer.addDocument(doc5);
		IndexReader reader = writer.Reader;
		writer.close();

		IndexSearcher searcher = newSearcher(reader);

		// PhrasePrefixQuery query1 = new PhrasePrefixQuery();
		MultiPhraseQuery query1 = new MultiPhraseQuery();
		// PhrasePrefixQuery query2 = new PhrasePrefixQuery();
		MultiPhraseQuery query2 = new MultiPhraseQuery();
		query1.add(new Term("body", "blueberry"));
		query2.add(new Term("body", "strawberry"));

		LinkedList<Term> termsWithPrefix = new LinkedList<Term>();

		// this TermEnum gives "piccadilly", "pie" and "pizza".
		string prefix = "pi";
		TermsEnum te = MultiFields.getFields(reader).terms("body").iterator(null);
		te.seekCeil(new BytesRef(prefix));
		do
		{
		  string s = te.term().utf8ToString();
		  if (s.StartsWith(prefix))
		  {
			termsWithPrefix.AddLast(new Term("body", s));
		  }
		  else
		  {
			break;
		  }
		} while (te.next() != null);

		query1.add(termsWithPrefix.toArray(new Term[0]));
		query2.add(termsWithPrefix.toArray(new Term[0]));

		ScoreDoc[] result;
		result = searcher.search(query1, null, 1000).scoreDocs;
		Assert.AreEqual(2, result.Length);

		result = searcher.search(query2, null, 1000).scoreDocs;
		Assert.AreEqual(0, result.Length);
		reader.close();
		indexStore.close();
	  }
	}

}