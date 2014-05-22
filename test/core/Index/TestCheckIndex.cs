using System;
using System.Collections.Generic;

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


	using IOUtils = Lucene.Net.Util.IOUtils;
	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
	using Directory = Lucene.Net.Store.Directory;
	using CannedTokenStream = Lucene.Net.Analysis.CannedTokenStream;
	using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
	using Token = Lucene.Net.Analysis.Token;
	using Document = Lucene.Net.Document.Document;
	using Field = Lucene.Net.Document.Field;
	using FieldType = Lucene.Net.Document.FieldType;
	using TextField = Lucene.Net.Document.TextField;

	public class TestCheckIndex : LuceneTestCase
	{

	  public virtual void TestDeletedDocs()
	  {
		Directory dir = newDirectory();
		IndexWriter writer = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setMaxBufferedDocs(2));
		for (int i = 0;i < 19;i++)
		{
		  Document doc = new Document();
		  FieldType customType = new FieldType(TextField.TYPE_STORED);
		  customType.StoreTermVectors = true;
		  customType.StoreTermVectorPositions = true;
		  customType.StoreTermVectorOffsets = true;
		  doc.add(newField("field", "aaa" + i, customType));
		  writer.addDocument(doc);
		}
		writer.forceMerge(1);
		writer.commit();
		writer.deleteDocuments(new Term("field","aaa5"));
		writer.close();

		ByteArrayOutputStream bos = new ByteArrayOutputStream(1024);
		CheckIndex checker = new CheckIndex(dir);
		checker.InfoStream = new PrintStream(bos, false, IOUtils.UTF_8);
		if (VERBOSE)
		{
			checker.InfoStream = System.out;
		}
		CheckIndex.Status indexStatus = checker.checkIndex();
		if (indexStatus.clean == false)
		{
		  Console.WriteLine("CheckIndex failed");
		  Console.WriteLine(bos.ToString(IOUtils.UTF_8));
		  Assert.Fail();
		}

		CheckIndex.Status.SegmentInfoStatus seg = indexStatus.segmentInfos.get(0);
		Assert.IsTrue(seg.openReaderPassed);

		Assert.IsNotNull(seg.diagnostics);

		Assert.IsNotNull(seg.fieldNormStatus);
		assertNull(seg.fieldNormStatus.error);
		Assert.AreEqual(1, seg.fieldNormStatus.totFields);

		Assert.IsNotNull(seg.termIndexStatus);
		assertNull(seg.termIndexStatus.error);
		Assert.AreEqual(18, seg.termIndexStatus.termCount);
		Assert.AreEqual(18, seg.termIndexStatus.totFreq);
		Assert.AreEqual(18, seg.termIndexStatus.totPos);

		Assert.IsNotNull(seg.storedFieldStatus);
		assertNull(seg.storedFieldStatus.error);
		Assert.AreEqual(18, seg.storedFieldStatus.docCount);
		Assert.AreEqual(18, seg.storedFieldStatus.totFields);

		Assert.IsNotNull(seg.termVectorStatus);
		assertNull(seg.termVectorStatus.error);
		Assert.AreEqual(18, seg.termVectorStatus.docCount);
		Assert.AreEqual(18, seg.termVectorStatus.totVectors);

		Assert.IsTrue(seg.diagnostics.size() > 0);
		IList<string> onlySegments = new List<string>();
		onlySegments.Add("_0");

		Assert.IsTrue(checker.checkIndex(onlySegments).clean == true);
		dir.close();
	  }

	  // LUCENE-4221: we have to let these thru, for now
	  public virtual void TestBogusTermVectors()
	  {
		Directory dir = newDirectory();
		IndexWriter iw = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, null));
		Document doc = new Document();
		FieldType ft = new FieldType(TextField.TYPE_NOT_STORED);
		ft.StoreTermVectors = true;
		ft.StoreTermVectorOffsets = true;
		Field field = new Field("foo", "", ft);
		field.setTokenStream(new CannedTokenStream(new Token("bar", 5, 10), new Token("bar", 1, 4)
	   ));
		doc.add(field);
		iw.addDocument(doc);
		iw.close();
		dir.close(); // checkindex
	  }
	}

}