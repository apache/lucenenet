namespace Lucene.Net.Codecs.Compressing
{

	using Document = Lucene.Net.Document.Document;
	using Field = Lucene.Net.Document.Field;
	using FieldType = Lucene.Net.Document.FieldType;
	using TextField = Lucene.Net.Document.TextField;
	using AtomicReader = Lucene.Net.Index.AtomicReader;
	using BaseTermVectorsFormatTestCase = Lucene.Net.Index.BaseTermVectorsFormatTestCase;
	using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;
	using Terms = Lucene.Net.Index.Terms;
	using TermsEnum = Lucene.Net.Index.TermsEnum;
	using SeekStatus = Lucene.Net.Index.TermsEnum.SeekStatus;
	using Directory = Lucene.Net.Store.Directory;
	using BytesRef = Lucene.Net.Util.BytesRef;
    using NUnit.Framework;

	//using Repeat = com.carrotsearch.randomizedtesting.annotations.Repeat;

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

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Repeat(iterations=5) public class TestCompressingTermVectorsFormat extends Lucene.Net.Index.BaseTermVectorsFormatTestCase
	public class TestCompressingTermVectorsFormat : BaseTermVectorsFormatTestCase
	{
		protected internal override Codec Codec
		{
			get
			{
			return CompressingCodec.randomInstance(random());
			}
		}

	  // https://issues.apache.org/jira/browse/LUCENE-5156
	  public virtual void TestNoOrds()
	  {
		Directory dir = newDirectory();
		RandomIndexWriter iw = new RandomIndexWriter(random(), dir);
		Document doc = new Document();
		FieldType ft = new FieldType(TextField.TYPE_NOT_STORED);
		ft.StoreTermVectors = true;
		doc.Add(new Field("foo", "this is a test", ft));
		iw.addDocument(doc);
		AtomicReader ir = getOnlySegmentReader(iw.Reader);
		Terms terms = ir.getTermVector(0, "foo");
		Assert.IsNotNull(terms);
		TermsEnum termsEnum = terms.iterator(null);
		Assert.AreEqual(TermsEnum.SeekStatus.FOUND, termsEnum.SeekCeil(new BytesRef("this")));
		try
		{
		  termsEnum.Ord();
		  Assert.Fail();
		}
		catch (System.NotSupportedException expected)
		{
		  // expected exception
		}

		try
		{
		  termsEnum.SeekExact(0);
		  Assert.Fail();
		}
		catch (System.NotSupportedException expected)
		{
		  // expected exception
		}
		ir.Close();
		iw.Close();
		dir.Close();
	  }
	}

}