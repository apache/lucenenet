using System.Text;

namespace Lucene.Net.Codecs.Lucene41
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

	using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
	using Document = Lucene.Net.Document.Document;
	using Field = Lucene.Net.Document.Field;
	using FieldType = Lucene.Net.Document.FieldType;
	using TextField = Lucene.Net.Document.TextField;
	using FieldInfo = Lucene.Net.Index.FieldInfo;
	using IndexOptions = Lucene.Net.Index.FieldInfo.IndexOptions;
	using IndexWriter = Lucene.Net.Index.IndexWriter;
	using IndexWriterConfig = Lucene.Net.Index.IndexWriterConfig;
	using OpenMode = Lucene.Net.Index.IndexWriterConfig.OpenMode;
	using IndexableField = Lucene.Net.Index.IndexableField;
	using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;
	using Directory = Lucene.Net.Store.Directory;
	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
	using TestUtil = Lucene.Net.Util.TestUtil;

	/// <summary>
	/// Tests special cases of BlockPostingsFormat 
	/// </summary>
	public class TestBlockPostingsFormat2 : LuceneTestCase
	{
	  internal Directory Dir;
	  internal RandomIndexWriter Iw;
	  internal IndexWriterConfig Iwc;

	  public override void SetUp()
	  {
		base.setUp();
		Dir = newFSDirectory(createTempDir("testDFBlockSize"));
		Iwc = newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random()));
		Iwc.Codec = TestUtil.alwaysPostingsFormat(new Lucene41PostingsFormat());
		Iw = new RandomIndexWriter(random(), Dir, Iwc.clone());
		Iw.DoRandomForceMerge = false; // we will ourselves
	  }

	  public override void TearDown()
	  {
		Iw.close();
		TestUtil.checkIndex(Dir); // for some extra coverage, checkIndex before we forceMerge
		Iwc.OpenMode = IndexWriterConfig.OpenMode.APPEND;
		IndexWriter iw = new IndexWriter(Dir, Iwc.clone());
		iw.forceMerge(1);
		iw.close();
		Dir.close(); // just force a checkindex for now
		base.tearDown();
	  }

	  private Document NewDocument()
	  {
		Document doc = new Document();
		foreach (FieldInfo.IndexOptions option in FieldInfo.IndexOptions.values())
		{
		  FieldType ft = new FieldType(TextField.TYPE_NOT_STORED);
		  // turn on tvs for a cross-check, since we rely upon checkindex in this test (for now)
		  ft.StoreTermVectors = true;
		  ft.StoreTermVectorOffsets = true;
		  ft.StoreTermVectorPositions = true;
		  ft.StoreTermVectorPayloads = true;
		  ft.IndexOptions = option;
		  doc.add(new Field(option.ToString(), "", ft));
		}
		return doc;
	  }

	  /// <summary>
	  /// tests terms with df = blocksize </summary>
	  public virtual void TestDFBlockSize()
	  {
		Document doc = NewDocument();
		for (int i = 0; i < Lucene41PostingsFormat.BLOCK_SIZE; i++)
		{
		  foreach (IndexableField f in doc.Fields)
		  {
			((Field)f).StringValue = f.name() + " " + f.name() + "_2";
		  }
		  Iw.addDocument(doc);
		}
	  }

	  /// <summary>
	  /// tests terms with df % blocksize = 0 </summary>
	  public virtual void TestDFBlockSizeMultiple()
	  {
		Document doc = NewDocument();
		for (int i = 0; i < Lucene41PostingsFormat.BLOCK_SIZE * 16; i++)
		{
		  foreach (IndexableField f in doc.Fields)
		  {
			((Field)f).StringValue = f.name() + " " + f.name() + "_2";
		  }
		  Iw.addDocument(doc);
		}
	  }

	  /// <summary>
	  /// tests terms with ttf = blocksize </summary>
	  public virtual void TestTTFBlockSize()
	  {
		Document doc = NewDocument();
		for (int i = 0; i < Lucene41PostingsFormat.BLOCK_SIZE / 2; i++)
		{
		  foreach (IndexableField f in doc.Fields)
		  {
			((Field)f).StringValue = f.name() + " " + f.name() + " " + f.name() + "_2 " + f.name() + "_2";
		  }
		  Iw.addDocument(doc);
		}
	  }

	  /// <summary>
	  /// tests terms with ttf % blocksize = 0 </summary>
	  public virtual void TestTTFBlockSizeMultiple()
	  {
		Document doc = NewDocument();
		for (int i = 0; i < Lucene41PostingsFormat.BLOCK_SIZE / 2; i++)
		{
		  foreach (IndexableField f in doc.Fields)
		  {
			string proto = (f.name() + " " + f.name() + " " + f.name() + " " + f.name() + " " + f.name() + "_2 " + f.name() + "_2 " + f.name() + "_2 " + f.name() + "_2");
			StringBuilder val = new StringBuilder();
			for (int j = 0; j < 16; j++)
			{
			  val.Append(proto);
			  val.Append(" ");
			}
			((Field)f).StringValue = val.ToString();
		  }
		  Iw.addDocument(doc);
		}
	  }
	}

}