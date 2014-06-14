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
	using Directory = Lucene.Net.Store.Directory;
	using MockDirectoryWrapper = Lucene.Net.Store.MockDirectoryWrapper;
	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using NUnit.Framework;

	public class TestNoDeletionPolicy : LuceneTestCase
	{

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testNoDeletionPolicy() throws Exception
	  public virtual void TestNoDeletionPolicy()
	  {
		IndexDeletionPolicy idp = NoDeletionPolicy.INSTANCE;
		idp.OnInit(null);
		idp.OnCommit(null);
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testFinalSingleton() throws Exception
	  public virtual void TestFinalSingleton()
	  {
		Assert.IsTrue(Modifier.isFinal(typeof(NoDeletionPolicy).Modifiers));
//JAVA TO C# CONVERTER TODO TASK: Java wildcard generics are not converted to .NET:
//ORIGINAL LINE: Constructor<?>[] ctors = NoDeletionPolicy.class.getDeclaredConstructors();
		Constructor<?>[] ctors = typeof(NoDeletionPolicy).DeclaredConstructors;
		Assert.AreEqual("expected 1 private ctor only: " + Arrays.ToString(ctors), 1, ctors.Length);
		Assert.IsTrue("that 1 should be private: " + ctors[0], Modifier.isPrivate(ctors[0].Modifiers));
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testMethodsOverridden() throws Exception
	  public virtual void TestMethodsOverridden()
	  {
		// Ensures that all methods of IndexDeletionPolicy are
		// overridden/implemented. That's important to ensure that NoDeletionPolicy 
		// overrides everything, so that no unexpected behavior/error occurs.
		// NOTE: even though IndexDeletionPolicy is an interface today, and so all
		// methods must be implemented by NoDeletionPolicy, this test is important
		// in case one day IDP becomes an abstract class.
		foreach (Method m in typeof(NoDeletionPolicy).Methods)
		{
		  // getDeclaredMethods() returns just those methods that are declared on
		  // NoDeletionPolicy. getMethods() returns those that are visible in that
		  // context, including ones from Object. So just filter out Object. If in
		  // the future IndexDeletionPolicy will become a class that extends a
		  // different class than Object, this will need to change.
		  if (m.DeclaringClass != typeof(object))
		  {
			Assert.IsTrue(m + " is not overridden !", m.DeclaringClass == typeof(NoDeletionPolicy));
		  }
		}
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testAllCommitsRemain() throws Exception
	  public virtual void TestAllCommitsRemain()
	  {
		Directory dir = NewDirectory();
		IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetIndexDeletionPolicy(NoDeletionPolicy.INSTANCE));
		for (int i = 0; i < 10; i++)
		{
		  Document doc = new Document();
		  doc.Add(NewTextField("c", "a" + i, Field.Store.YES));
		  writer.AddDocument(doc);
		  writer.Commit();
		  Assert.AreEqual(i + 1, DirectoryReader.ListCommits(dir).Count, "wrong number of commits !");
		}
		writer.Dispose();
		dir.Dispose();
	  }

	}

}