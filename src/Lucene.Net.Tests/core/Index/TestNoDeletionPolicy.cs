using System.Reflection;
using Lucene.Net.Documents;

namespace Lucene.Net.Index
{
    using NUnit.Framework;
    using Support;
    using Directory = Lucene.Net.Store.Directory;
    using Document = Documents.Document;
    using Field = Field;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;

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

    [TestFixture]
    public class TestNoDeletionPolicy : LuceneTestCase
    {
        [Test]
        public virtual void TestNoDeletionPolicy_Mem()
        {
            IndexDeletionPolicy idp = NoDeletionPolicy.INSTANCE;
            idp.OnInit<IndexCommit>(null);
            idp.OnCommit<IndexCommit>(null);
        }

        [Test]
        public virtual void TestFinalSingleton()
	    {
		    assertTrue(typeof(NoDeletionPolicy).GetTypeInfo().IsSealed);
		    ConstructorInfo[] ctors = typeof(NoDeletionPolicy).GetConstructors(BindingFlags.Instance |
                    BindingFlags.NonPublic |
                    BindingFlags.Public |
                    BindingFlags.DeclaredOnly); // LUCENENET NOTE: It seems .NET automatically adds a private static constructor, so leaving off the static BindingFlag
		    assertEquals("expected 1 private ctor only: " + Arrays.ToString(ctors), 1, ctors.Length);
		    assertTrue("that 1 should be private: " + ctors[0], ctors[0].IsPrivate);
	    }

        [Test]
        public virtual void TestMethodsOverridden()
        {
            // Ensures that all methods of IndexDeletionPolicy are
            // overridden/implemented. That's important to ensure that NoDeletionPolicy
            // overrides everything, so that no unexpected behavior/error occurs.
            // NOTE: even though IndexDeletionPolicy is an interface today, and so all
            // methods must be implemented by NoDeletionPolicy, this test is important
            // in case one day IDP becomes an abstract class.
            foreach (MethodInfo m in typeof(NoDeletionPolicy).GetMethods())
            {
                // getDeclaredMethods() returns just those methods that are declared on
                // NoDeletionPolicy. getMethods() returns those that are visible in that
                // context, including ones from Object. So just filter out Object. If in
                // the future IndexDeletionPolicy will become a class that extends a
                // different class than Object, this will need to change.
                if (m.DeclaringType != typeof(object))
                {
                    Assert.IsTrue(m.DeclaringType == typeof(NoDeletionPolicy), m + " is not overridden !");
                }
            }
        }

        [Test]
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