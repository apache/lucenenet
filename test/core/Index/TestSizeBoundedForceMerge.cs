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

	using Document = Lucene.Net.Document.Document;
	using Field = Lucene.Net.Document.Field;
	using StringField = Lucene.Net.Document.StringField;
	using Directory = Lucene.Net.Store.Directory;
	using RAMDirectory = Lucene.Net.Store.RAMDirectory;
	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;

	public class TestSizeBoundedForceMerge : LuceneTestCase
	{

	  private void AddDocs(IndexWriter writer, int numDocs)
	  {
		AddDocs(writer, numDocs, false);
	  }

	  private void AddDocs(IndexWriter writer, int numDocs, bool withID)
	  {
		for (int i = 0; i < numDocs; i++)
		{
		  Document doc = new Document();
		  if (withID)
		  {
			doc.add(new StringField("id", "" + i, Field.Store.NO));
		  }
		  writer.addDocument(doc);
		}
		writer.commit();
	  }

	  private static IndexWriterConfig NewWriterConfig()
	  {
		IndexWriterConfig conf = newIndexWriterConfig(TEST_VERSION_CURRENT, null);
		conf.MaxBufferedDocs = IndexWriterConfig.DISABLE_AUTO_FLUSH;
		conf.RAMBufferSizeMB = IndexWriterConfig.DEFAULT_RAM_BUFFER_SIZE_MB;
		// prevent any merges by default.
		conf.MergePolicy = NoMergePolicy.COMPOUND_FILES;
		return conf;
	  }

	  public virtual void TestByteSizeLimit()
	  {
		// tests that the max merge size constraint is applied during forceMerge.
		Directory dir = new RAMDirectory();

		// Prepare an index w/ several small segments and a large one.
		IndexWriterConfig conf = NewWriterConfig();
		IndexWriter writer = new IndexWriter(dir, conf);
		const int numSegments = 15;
		for (int i = 0; i < numSegments; i++)
		{
		  int numDocs = i == 7 ? 30 : 1;
		  AddDocs(writer, numDocs);
		}
		writer.close();

		SegmentInfos sis = new SegmentInfos();
		sis.read(dir);
		double min = sis.info(0).sizeInBytes();

		conf = NewWriterConfig();
		LogByteSizeMergePolicy lmp = new LogByteSizeMergePolicy();
		lmp.MaxMergeMBForForcedMerge = (min + 1) / (1 << 20);
		conf.MergePolicy = lmp;

		writer = new IndexWriter(dir, conf);
		writer.forceMerge(1);
		writer.close();

		// Should only be 3 segments in the index, because one of them exceeds the size limit
		sis = new SegmentInfos();
		sis.read(dir);
		Assert.AreEqual(3, sis.size());
	  }

	  public virtual void TestNumDocsLimit()
	  {
		// tests that the max merge docs constraint is applied during forceMerge.
		Directory dir = new RAMDirectory();

		// Prepare an index w/ several small segments and a large one.
		IndexWriterConfig conf = NewWriterConfig();
		IndexWriter writer = new IndexWriter(dir, conf);

		AddDocs(writer, 3);
		AddDocs(writer, 3);
		AddDocs(writer, 5);
		AddDocs(writer, 3);
		AddDocs(writer, 3);
		AddDocs(writer, 3);
		AddDocs(writer, 3);

		writer.close();

		conf = NewWriterConfig();
		LogMergePolicy lmp = new LogDocMergePolicy();
		lmp.MaxMergeDocs = 3;
		conf.MergePolicy = lmp;

		writer = new IndexWriter(dir, conf);
		writer.forceMerge(1);
		writer.close();

		// Should only be 3 segments in the index, because one of them exceeds the size limit
		SegmentInfos sis = new SegmentInfos();
		sis.read(dir);
		Assert.AreEqual(3, sis.size());
	  }

	  public virtual void TestLastSegmentTooLarge()
	  {
		Directory dir = new RAMDirectory();

		IndexWriterConfig conf = NewWriterConfig();
		IndexWriter writer = new IndexWriter(dir, conf);

		AddDocs(writer, 3);
		AddDocs(writer, 3);
		AddDocs(writer, 3);
		AddDocs(writer, 5);

		writer.close();

		conf = NewWriterConfig();
		LogMergePolicy lmp = new LogDocMergePolicy();
		lmp.MaxMergeDocs = 3;
		conf.MergePolicy = lmp;

		writer = new IndexWriter(dir, conf);
		writer.forceMerge(1);
		writer.close();

		SegmentInfos sis = new SegmentInfos();
		sis.read(dir);
		Assert.AreEqual(2, sis.size());
	  }

	  public virtual void TestFirstSegmentTooLarge()
	  {
		Directory dir = new RAMDirectory();

		IndexWriterConfig conf = NewWriterConfig();
		IndexWriter writer = new IndexWriter(dir, conf);

		AddDocs(writer, 5);
		AddDocs(writer, 3);
		AddDocs(writer, 3);
		AddDocs(writer, 3);

		writer.close();

		conf = NewWriterConfig();
		LogMergePolicy lmp = new LogDocMergePolicy();
		lmp.MaxMergeDocs = 3;
		conf.MergePolicy = lmp;

		writer = new IndexWriter(dir, conf);
		writer.forceMerge(1);
		writer.close();

		SegmentInfos sis = new SegmentInfos();
		sis.read(dir);
		Assert.AreEqual(2, sis.size());
	  }

	  public virtual void TestAllSegmentsSmall()
	  {
		Directory dir = new RAMDirectory();

		IndexWriterConfig conf = NewWriterConfig();
		IndexWriter writer = new IndexWriter(dir, conf);

		AddDocs(writer, 3);
		AddDocs(writer, 3);
		AddDocs(writer, 3);
		AddDocs(writer, 3);

		writer.close();

		conf = NewWriterConfig();
		LogMergePolicy lmp = new LogDocMergePolicy();
		lmp.MaxMergeDocs = 3;
		conf.MergePolicy = lmp;

		writer = new IndexWriter(dir, conf);
		writer.forceMerge(1);
		writer.close();

		SegmentInfos sis = new SegmentInfos();
		sis.read(dir);
		Assert.AreEqual(1, sis.size());
	  }

	  public virtual void TestAllSegmentsLarge()
	  {
		Directory dir = new RAMDirectory();

		IndexWriterConfig conf = NewWriterConfig();
		IndexWriter writer = new IndexWriter(dir, conf);

		AddDocs(writer, 3);
		AddDocs(writer, 3);
		AddDocs(writer, 3);

		writer.close();

		conf = NewWriterConfig();
		LogMergePolicy lmp = new LogDocMergePolicy();
		lmp.MaxMergeDocs = 2;
		conf.MergePolicy = lmp;

		writer = new IndexWriter(dir, conf);
		writer.forceMerge(1);
		writer.close();

		SegmentInfos sis = new SegmentInfos();
		sis.read(dir);
		Assert.AreEqual(3, sis.size());
	  }

	  public virtual void TestOneLargeOneSmall()
	  {
		Directory dir = new RAMDirectory();

		IndexWriterConfig conf = NewWriterConfig();
		IndexWriter writer = new IndexWriter(dir, conf);

		AddDocs(writer, 3);
		AddDocs(writer, 5);
		AddDocs(writer, 3);
		AddDocs(writer, 5);

		writer.close();

		conf = NewWriterConfig();
		LogMergePolicy lmp = new LogDocMergePolicy();
		lmp.MaxMergeDocs = 3;
		conf.MergePolicy = lmp;

		writer = new IndexWriter(dir, conf);
		writer.forceMerge(1);
		writer.close();

		SegmentInfos sis = new SegmentInfos();
		sis.read(dir);
		Assert.AreEqual(4, sis.size());
	  }

	  public virtual void TestMergeFactor()
	  {
		Directory dir = new RAMDirectory();

		IndexWriterConfig conf = NewWriterConfig();
		IndexWriter writer = new IndexWriter(dir, conf);

		AddDocs(writer, 3);
		AddDocs(writer, 3);
		AddDocs(writer, 3);
		AddDocs(writer, 3);
		AddDocs(writer, 5);
		AddDocs(writer, 3);
		AddDocs(writer, 3);

		writer.close();

		conf = NewWriterConfig();
		LogMergePolicy lmp = new LogDocMergePolicy();
		lmp.MaxMergeDocs = 3;
		lmp.MergeFactor = 2;
		conf.MergePolicy = lmp;

		writer = new IndexWriter(dir, conf);
		writer.forceMerge(1);
		writer.close();

		// Should only be 4 segments in the index, because of the merge factor and
		// max merge docs settings.
		SegmentInfos sis = new SegmentInfos();
		sis.read(dir);
		Assert.AreEqual(4, sis.size());
	  }

	  public virtual void TestSingleMergeableSegment()
	  {
		Directory dir = new RAMDirectory();

		IndexWriterConfig conf = NewWriterConfig();
		IndexWriter writer = new IndexWriter(dir, conf);

		AddDocs(writer, 3);
		AddDocs(writer, 5);
		AddDocs(writer, 3);

		// delete the last document, so that the last segment is merged.
		writer.deleteDocuments(new Term("id", "10"));
		writer.close();

		conf = NewWriterConfig();
		LogMergePolicy lmp = new LogDocMergePolicy();
		lmp.MaxMergeDocs = 3;
		conf.MergePolicy = lmp;

		writer = new IndexWriter(dir, conf);
		writer.forceMerge(1);
		writer.close();

		// Verify that the last segment does not have deletions.
		SegmentInfos sis = new SegmentInfos();
		sis.read(dir);
		Assert.AreEqual(3, sis.size());
		Assert.IsFalse(sis.info(2).hasDeletions());
	  }

	  public virtual void TestSingleNonMergeableSegment()
	  {
		Directory dir = new RAMDirectory();

		IndexWriterConfig conf = NewWriterConfig();
		IndexWriter writer = new IndexWriter(dir, conf);

		AddDocs(writer, 3, true);

		writer.close();

		conf = NewWriterConfig();
		LogMergePolicy lmp = new LogDocMergePolicy();
		lmp.MaxMergeDocs = 3;
		conf.MergePolicy = lmp;

		writer = new IndexWriter(dir, conf);
		writer.forceMerge(1);
		writer.close();

		// Verify that the last segment does not have deletions.
		SegmentInfos sis = new SegmentInfos();
		sis.read(dir);
		Assert.AreEqual(1, sis.size());
	  }

	  public virtual void TestSingleMergeableTooLargeSegment()
	  {
		Directory dir = new RAMDirectory();

		IndexWriterConfig conf = NewWriterConfig();
		IndexWriter writer = new IndexWriter(dir, conf);

		AddDocs(writer, 5, true);

		// delete the last document

		writer.deleteDocuments(new Term("id", "4"));
		writer.close();

		conf = NewWriterConfig();
		LogMergePolicy lmp = new LogDocMergePolicy();
		lmp.MaxMergeDocs = 2;
		conf.MergePolicy = lmp;

		writer = new IndexWriter(dir, conf);
		writer.forceMerge(1);
		writer.close();

		// Verify that the last segment does not have deletions.
		SegmentInfos sis = new SegmentInfos();
		sis.read(dir);
		Assert.AreEqual(1, sis.size());
		Assert.IsTrue(sis.info(0).hasDeletions());
	  }

	}

}