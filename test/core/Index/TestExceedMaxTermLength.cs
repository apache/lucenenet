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
	using Document = Lucene.Net.Document.Document;
	using Field = Lucene.Net.Document.Field;
	using FieldType = Lucene.Net.Document.FieldType;
	using Directory = Lucene.Net.Store.Directory;
	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
	using TestUtil = Lucene.Net.Util.TestUtil;

	using Before = org.junit.Before;
	using After = org.junit.After;

	/// <summary>
	/// Tests that a useful exception is thrown when attempting to index a term that is 
	/// too large
	/// </summary>
	/// <seealso cref= IndexWriter#MAX_TERM_LENGTH </seealso>
	public class TestExceedMaxTermLength : LuceneTestCase
	{

	  private static readonly int MinTestTermLength = IndexWriter.MAX_TERM_LENGTH + 1;
	  private static readonly int MaxTestTermLegnth = IndexWriter.MAX_TERM_LENGTH * 2;

	  internal Directory Dir = null;

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Before public void createDir()
	  public virtual void CreateDir()
	  {
		Dir = newDirectory();
	  }
//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @After public void destroyDir() throws java.io.IOException
	  public virtual void DestroyDir()
	  {
		Dir.close();
		Dir = null;
	  }

	  public virtual void Test()
	  {

		IndexWriter w = new IndexWriter(Dir, newIndexWriterConfig(random(), TEST_VERSION_CURRENT, new MockAnalyzer(random())));
		try
		{
		  FieldType ft = new FieldType();
		  ft.Indexed = true;
		  ft.Stored = random().nextBoolean();
		  ft.freeze();

		  Document doc = new Document();
		  if (random().nextBoolean())
		  {
			// totally ok short field value
			doc.add(new Field(TestUtil.randomSimpleString(random(), 1, 10), TestUtil.randomSimpleString(random(), 1, 10), ft));
		  }
		  // problematic field
		  string name = TestUtil.randomSimpleString(random(), 1, 50);
		  string value = TestUtil.randomSimpleString(random(), MinTestTermLength, MaxTestTermLegnth);
		  Field f = new Field(name, value, ft);
		  if (random().nextBoolean())
		  {
			// totally ok short field value
			doc.add(new Field(TestUtil.randomSimpleString(random(), 1, 10), TestUtil.randomSimpleString(random(), 1, 10), ft));
		  }
		  doc.add(f);

		  try
		  {
			w.addDocument(doc);
			Assert.Fail("Did not get an exception from adding a monster term");
		  }
		  catch (System.ArgumentException e)
		  {
			string maxLengthMsg = Convert.ToString(IndexWriter.MAX_TERM_LENGTH);
			string msg = e.Message;
			Assert.IsTrue("IllegalArgumentException didn't mention 'immense term': " + msg, msg.Contains("immense term"));
			Assert.IsTrue("IllegalArgumentException didn't mention max length (" + maxLengthMsg + "): " + msg, msg.Contains(maxLengthMsg));
			Assert.IsTrue("IllegalArgumentException didn't mention field name (" + name + "): " + msg, msg.Contains(name));
		  }
		}
		finally
		{
		  w.close();
		}
	  }
	}

}