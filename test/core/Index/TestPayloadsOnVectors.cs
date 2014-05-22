using System.Diagnostics;

namespace Lucene.Net.Index
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

	using CannedTokenStream = Lucene.Net.Analysis.CannedTokenStream;
	using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
	using MockTokenizer = Lucene.Net.Analysis.MockTokenizer;
	using Token = Lucene.Net.Analysis.Token;
	using TokenStream = Lucene.Net.Analysis.TokenStream;
	using PayloadAttribute = Lucene.Net.Analysis.Tokenattributes.PayloadAttribute;
	using Document = Lucene.Net.Document.Document;
	using Field = Lucene.Net.Document.Field;
	using FieldType = Lucene.Net.Document.FieldType;
	using TextField = Lucene.Net.Document.TextField;
	using Directory = Lucene.Net.Store.Directory;
	using BytesRef = Lucene.Net.Util.BytesRef;
	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
	using SuppressCodecs = Lucene.Net.Util.LuceneTestCase.SuppressCodecs;

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @SuppressCodecs("Lucene3x") public class TestPayloadsOnVectors extends Lucene.Net.Util.LuceneTestCase
	public class TestPayloadsOnVectors : LuceneTestCase
	{

	  /// <summary>
	  /// some docs have payload att, some not </summary>
	  public virtual void TestMixupDocs()
	  {
		Directory dir = newDirectory();
		IndexWriterConfig iwc = newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random()));
		iwc.MergePolicy = newLogMergePolicy();
		RandomIndexWriter writer = new RandomIndexWriter(random(), dir, iwc);
		Document doc = new Document();
		FieldType customType = new FieldType(TextField.TYPE_NOT_STORED);
		customType.StoreTermVectors = true;
		customType.StoreTermVectorPositions = true;
		customType.StoreTermVectorPayloads = true;
		customType.StoreTermVectorOffsets = random().nextBoolean();
		Field field = new Field("field", "", customType);
		TokenStream ts = new MockTokenizer(new StringReader("here we go"), MockTokenizer.WHITESPACE, true);
		Assert.IsFalse(ts.HasAttribute(typeof(PayloadAttribute)));
		field.TokenStream = ts;
		doc.add(field);
		writer.addDocument(doc);

		Token withPayload = new Token("withPayload", 0, 11);
		withPayload.Payload = new BytesRef("test");
		ts = new CannedTokenStream(withPayload);
		Assert.IsTrue(ts.HasAttribute(typeof(PayloadAttribute)));
		field.TokenStream = ts;
		writer.addDocument(doc);

		ts = new MockTokenizer(new StringReader("another"), MockTokenizer.WHITESPACE, true);
		Assert.IsFalse(ts.HasAttribute(typeof(PayloadAttribute)));
		field.TokenStream = ts;
		writer.addDocument(doc);

		DirectoryReader reader = writer.Reader;
		Terms terms = reader.getTermVector(1, "field");
		Debug.Assert(terms != null);
		TermsEnum termsEnum = terms.iterator(null);
		Assert.IsTrue(termsEnum.seekExact(new BytesRef("withPayload")));
		DocsAndPositionsEnum de = termsEnum.docsAndPositions(null, null);
		Assert.AreEqual(0, de.nextDoc());
		Assert.AreEqual(0, de.nextPosition());
		Assert.AreEqual(new BytesRef("test"), de.Payload);
		writer.close();
		reader.close();
		dir.close();
	  }

	  /// <summary>
	  /// some field instances have payload att, some not </summary>
	  public virtual void TestMixupMultiValued()
	  {
		Directory dir = newDirectory();
		RandomIndexWriter writer = new RandomIndexWriter(random(), dir);
		Document doc = new Document();
		FieldType customType = new FieldType(TextField.TYPE_NOT_STORED);
		customType.StoreTermVectors = true;
		customType.StoreTermVectorPositions = true;
		customType.StoreTermVectorPayloads = true;
		customType.StoreTermVectorOffsets = random().nextBoolean();
		Field field = new Field("field", "", customType);
		TokenStream ts = new MockTokenizer(new StringReader("here we go"), MockTokenizer.WHITESPACE, true);
		Assert.IsFalse(ts.HasAttribute(typeof(PayloadAttribute)));
		field.TokenStream = ts;
		doc.add(field);
		Field field2 = new Field("field", "", customType);
		Token withPayload = new Token("withPayload", 0, 11);
		withPayload.Payload = new BytesRef("test");
		ts = new CannedTokenStream(withPayload);
		Assert.IsTrue(ts.HasAttribute(typeof(PayloadAttribute)));
		field2.TokenStream = ts;
		doc.add(field2);
		Field field3 = new Field("field", "", customType);
		ts = new MockTokenizer(new StringReader("nopayload"), MockTokenizer.WHITESPACE, true);
		Assert.IsFalse(ts.HasAttribute(typeof(PayloadAttribute)));
		field3.TokenStream = ts;
		doc.add(field3);
		writer.addDocument(doc);
		DirectoryReader reader = writer.Reader;
		Terms terms = reader.getTermVector(0, "field");
		Debug.Assert(terms != null);
		TermsEnum termsEnum = terms.iterator(null);
		Assert.IsTrue(termsEnum.seekExact(new BytesRef("withPayload")));
		DocsAndPositionsEnum de = termsEnum.docsAndPositions(null, null);
		Assert.AreEqual(0, de.nextDoc());
		Assert.AreEqual(3, de.nextPosition());
		Assert.AreEqual(new BytesRef("test"), de.Payload);
		writer.close();
		reader.close();
		dir.close();
	  }

	  public virtual void TestPayloadsWithoutPositions()
	  {
		Directory dir = newDirectory();
		RandomIndexWriter writer = new RandomIndexWriter(random(), dir);
		Document doc = new Document();
		FieldType customType = new FieldType(TextField.TYPE_NOT_STORED);
		customType.StoreTermVectors = true;
		customType.StoreTermVectorPositions = false;
		customType.StoreTermVectorPayloads = true;
		customType.StoreTermVectorOffsets = random().nextBoolean();
		doc.add(new Field("field", "foo", customType));
		try
		{
		  writer.addDocument(doc);
		  Assert.Fail();
		}
		catch (System.ArgumentException expected)
		{
		  // expected
		}
		writer.close();
		dir.close();
	  }

	}

}