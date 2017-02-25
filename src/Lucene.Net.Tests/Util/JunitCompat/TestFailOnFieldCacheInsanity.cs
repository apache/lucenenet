using NUnit.Framework;

namespace Lucene.Net.Util.JunitCompat
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
	using StringField = Lucene.Net.Document.StringField;
	using AtomicReader = Lucene.Net.Index.AtomicReader;
	using IndexReader = Lucene.Net.Index.IndexReader;
	using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;
	using FieldCache_Fields = Lucene.Net.Search.FieldCache_Fields;
	using Directory = Lucene.Net.Store.Directory;
	using RAMDirectory = Lucene.Net.Store.RAMDirectory;

	public class TestFailOnFieldCacheInsanity : WithNestedTests
	{
	  public TestFailOnFieldCacheInsanity() : base(true)
	  {
	  }

	  public class Nested1 : WithNestedTests.AbstractNestedTest
	  {
		    internal Directory d;
		    internal IndexReader r;
		    internal AtomicReader SubR;

		    internal virtual void MakeIndex()
		    {
		      // we use RAMDirectory here, because we dont want to stay on open files on Windows:
		      d = new RAMDirectory();
		      RandomIndexWriter w = new RandomIndexWriter(Random(), d);
		      Document doc = new Document();
		      doc.Add(newField("ints", "1", StringField.TYPE_NOT_STORED));
		      w.AddDocument(doc);
		      w.ForceMerge(1);
		      r = w.Reader;
		      w.Dispose();

		      SubR = (AtomicReader)(r.Leaves()[0]).Reader();
            }
	   }

		public virtual void TestDummy()
		{
		  MakeIndex();
		  Assert.IsNotNull(FieldCache_Fields.DEFAULT.getTermsIndex(SubR, "ints"));
          Assert.IsNotNull(FieldCache_Fields.DEFAULT.GetTerms(SubR, "ints", false));
		  // NOTE: do not close reader/directory, else it
		  // purges FC entries
		}
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testFailOnFieldCacheInsanity()
	  public virtual void TestFailOnFieldCacheInsanity()
	  {
		Result r = JUnitCore.runClasses(typeof(Nested1));
		bool insane = false;
		foreach (Failure f in r.Failures)
		{
		  if (f.Message.IndexOf("Insane") != -1)
		  {
			insane = true;
			break;
		  }
		}
		Assert.IsTrue(insane);
	  }
	}

}