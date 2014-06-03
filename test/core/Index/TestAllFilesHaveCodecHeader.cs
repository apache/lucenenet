using System;

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

	using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
	using CodecUtil = Lucene.Net.Codecs.CodecUtil;
	using Lucene46Codec = Lucene.Net.Codecs.lucene46.Lucene46Codec;
	using Document = Lucene.Net.Document.Document;
	using Field = Lucene.Net.Document.Field;
	using NumericDocValuesField = Lucene.Net.Document.NumericDocValuesField;
	using CompoundFileDirectory = Lucene.Net.Store.CompoundFileDirectory;
	using Directory = Lucene.Net.Store.Directory;
	using IndexInput = Lucene.Net.Store.IndexInput;
	using IOUtils = Lucene.Net.Util.IOUtils;
	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
	using TestUtil = Lucene.Net.Util.TestUtil;

	/// <summary>
	/// Test that a plain default puts codec headers in all files.
	/// </summary>
	public class TestAllFilesHaveCodecHeader : LuceneTestCase
	{
	  public virtual void Test()
	  {
		Directory dir = newDirectory();
		IndexWriterConfig conf = newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random()));
		conf.Codec = new Lucene46Codec();
		RandomIndexWriter riw = new RandomIndexWriter(random(), dir, conf);
		Document doc = new Document();
		// these fields should sometimes get term vectors, etc
		Field idField = newStringField("id", "", Field.Store.NO);
		Field bodyField = newTextField("body", "", Field.Store.NO);
		Field dvField = new NumericDocValuesField("dv", 5);
		doc.add(idField);
		doc.add(bodyField);
		doc.add(dvField);
		for (int i = 0; i < 100; i++)
		{
		  idField.StringValue = Convert.ToString(i);
		  bodyField.StringValue = TestUtil.randomUnicodeString(random());
		  riw.addDocument(doc);
		  if (random().Next(7) == 0)
		  {
			riw.commit();
		  }
		  // TODO: we should make a new format with a clean header...
		  // if (random().nextInt(20) == 0) {
		  //  riw.deleteDocuments(new Term("id", Integer.toString(i)));
		  // }
		}
		riw.close();
		CheckHeaders(dir);
		dir.close();
	  }

	  private void CheckHeaders(Directory dir)
	  {
		foreach (string file in dir.listAll())
		{
		  if (file.Equals(IndexWriter.WRITE_LOCK_NAME))
		  {
			continue; // write.lock has no header, thats ok
		  }
		  if (file.Equals(IndexFileNames.SEGMENTS_GEN))
		  {
			continue; // segments.gen has no header, thats ok
		  }
		  if (file.EndsWith(IndexFileNames.COMPOUND_FILE_EXTENSION))
		  {
			CompoundFileDirectory cfsDir = new CompoundFileDirectory(dir, file, newIOContext(random()), false);
			CheckHeaders(cfsDir); // recurse into cfs
			cfsDir.close();
		  }
		  IndexInput @in = null;
		  bool success = false;
		  try
		  {
			@in = dir.openInput(file, newIOContext(random()));
			int val = @in.readInt();
			Assert.AreEqual(file + " has no codec header, instead found: " + val, CodecUtil.CODEC_MAGIC, val);
			success = true;
		  }
		  finally
		  {
			if (success)
			{
			  IOUtils.Close(@in);
			}
			else
			{
			  IOUtils.CloseWhileHandlingException(@in);
			}
		  }
		}
	  }
	}

}