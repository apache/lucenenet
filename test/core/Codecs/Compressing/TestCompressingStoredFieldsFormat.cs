namespace Lucene.Net.Codecs.Compressing
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
	using Store = Lucene.Net.Document.Field.Store;
	using FieldType = Lucene.Net.Document.FieldType;
	using IntField = Lucene.Net.Document.IntField;
	using BaseStoredFieldsFormatTestCase = Lucene.Net.Index.BaseStoredFieldsFormatTestCase;
	using IndexWriterConfig = Lucene.Net.Index.IndexWriterConfig;
	using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;
	using Directory = Lucene.Net.Store.Directory;
    using NUnit.Framework;
	//using Test = org.junit.Test;

	//using Repeat = com.carrotsearch.randomizedtesting.annotations.Repeat;
	//using RandomInts = com.carrotsearch.randomizedtesting.generators.RandomInts;

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Repeat(iterations=5) public class TestCompressingStoredFieldsFormat extends Lucene.Net.Index.BaseStoredFieldsFormatTestCase
	public class TestCompressingStoredFieldsFormat : BaseStoredFieldsFormatTestCase
	{
		protected internal override Codec Codec
		{
			get
			{
			return CompressingCodec.randomInstance(random());
			}
		}

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test(expected=IllegalArgumentException.class) public void testDeletePartiallyWrittenFilesIfAbort() throws java.io.IOException
	  public virtual void TestDeletePartiallyWrittenFilesIfAbort()
	  {
		Directory dir = newDirectory();
		IndexWriterConfig iwConf = newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random()));
		iwConf.MaxBufferedDocs = RandomInts.randomIntBetween(random(), 2, 30);
		iwConf.Codec = CompressingCodec.randomInstance(random());
		// disable CFS because this test checks file names
		iwConf.MergePolicy = newLogMergePolicy(false);
		iwConf.UseCompoundFile = false;
		RandomIndexWriter iw = new RandomIndexWriter(random(), dir, iwConf);

		Document validDoc = new Document();
		validDoc.Add(new IntField("id", 0, Field.Store.YES));
		iw.addDocument(validDoc);
		iw.commit();

		// make sure that #writeField will fail to trigger an abort
		Document invalidDoc = new Document();
		FieldType fieldType = new FieldType();
		fieldType.Stored = true;
		invalidDoc.Add(new FieldAnonymousInnerClassHelper(this, fieldType));

		try
		{
		  iw.addDocument(invalidDoc);
		  iw.commit();
		}
		finally
		{
		  int counter = 0;
		  foreach (string fileName in dir.ListAll())
		  {
			if (fileName.EndsWith(".fdt") || fileName.EndsWith(".fdx"))
			{
			  counter++;
			}
		  }
		  // Only one .fdt and one .fdx files must have been found
		  Assert.AreEqual(2, counter);
		  iw.Close();
		  dir.Close();
		}
	  }

	  private class FieldAnonymousInnerClassHelper : Field
	  {
		  private readonly TestCompressingStoredFieldsFormat OuterInstance;

		  public FieldAnonymousInnerClassHelper(TestCompressingStoredFieldsFormat outerInstance, FieldType fieldType) : base("invalid", fieldType)
		  {
			  this.OuterInstance = outerInstance;
		  }


		  public override string StringValue()
		  {
			return null;
		  }

	  }
	}

}