using System;
using System.Collections;
using System.Collections.Generic;
using Lucene.Net.Documents;

namespace Lucene.Net.Index
{
    using Lucene.Net.Support;
    using NUnit.Framework;
    using IBits = Lucene.Net.Util.IBits;
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

    /// <summary>
    /// Test class to illustrate using IndexDeletionPolicy to provide multi-level rollback capability.
    /// this test case creates an index of records 1 to 100, introducing a commit point every 10 records.
    ///
    /// A "keep all" deletion policy is used to ensure we keep all commit points for testing purposes
    /// </summary>

    [TestFixture]
    public class TestTransactionRollback : LuceneTestCase
    {
        private const string FIELD_RECORD_ID = "record_id";
        private Directory Dir;

        //Rolls back index to a chosen ID
        private void RollBackLast(int id)
        {
            // System.out.println("Attempting to rollback to "+id);
            string ids = "-" + id;
            IndexCommit last = null;
            ICollection<IndexCommit> commits = DirectoryReader.ListCommits(Dir);
            for (IEnumerator<IndexCommit> iterator = commits.GetEnumerator(); iterator.MoveNext(); )
            {
                IndexCommit commit = iterator.Current;
                IDictionary<string, string> ud = commit.UserData;
                if (ud.Count > 0)
                {
                    if (ud["index"].EndsWith(ids))
                    {
                        last = commit;
                    }
                }
            }

            if (last == null)
            {
                throw new Exception("Couldn't find commit point " + id);
            }

            IndexWriter w = new IndexWriter(Dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetIndexDeletionPolicy(new RollbackDeletionPolicy<IndexCommit>(this, id)).SetIndexCommit(last));
            IDictionary<string, string> data = new Dictionary<string, string>();
            data["index"] = "Rolled back to 1-" + id;
            w.CommitData = data;
            w.Dispose();
        }

        [Test]
        public virtual void TestRepeatedRollBacks()
        {
            int expectedLastRecordId = 100;
            while (expectedLastRecordId > 10)
            {
                expectedLastRecordId -= 10;
                RollBackLast(expectedLastRecordId);

                BitArray expecteds = new BitArray(100);
                expecteds.Set(1, (expectedLastRecordId + 1), true);
                CheckExpecteds(expecteds);
            }
        }

        private void CheckExpecteds(BitArray expecteds)
        {
            IndexReader r = DirectoryReader.Open(Dir);

            //Perhaps not the most efficient approach but meets our
            //needs here.
            IBits liveDocs = MultiFields.GetLiveDocs(r);
            for (int i = 0; i < r.MaxDoc; i++)
            {
                if (liveDocs == null || liveDocs.Get(i))
                {
                    string sval = r.Document(i).Get(FIELD_RECORD_ID);
                    if (sval != null)
                    {
                        int val = Convert.ToInt32(sval);
                        Assert.IsTrue(expecteds.SafeGet(val), "Did not expect document #" + val);
                        expecteds.SafeSet(val, false);
                    }
                }
            }
            r.Dispose();
            Assert.AreEqual(0, expecteds.Cardinality(), "Should have 0 docs remaining ");
        }

        /*
        private void showAvailableCommitPoints() throws Exception {
          Collection commits = DirectoryReader.ListCommits(dir);
          for (Iterator iterator = commits.iterator(); iterator.hasNext();) {
            IndexCommit comm = (IndexCommit) iterator.Next();
            System.out.print("\t Available commit point:["+comm.getUserData()+"] files=");
            Collection files = comm.getFileNames();
            for (Iterator iterator2 = files.iterator(); iterator2.hasNext();) {
              String filename = (String) iterator2.Next();
              System.out.print(filename+", ");
            }
            System.out.println();
          }
        }
        */

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            Dir = NewDirectory();

            //Build index, of records 1 to 100, committing after each batch of 10
            IndexDeletionPolicy sdp = new KeepAllDeletionPolicy<IndexCommit>(this);
            IndexWriter w = new IndexWriter(Dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetIndexDeletionPolicy(sdp));

            for (int currentRecordId = 1; currentRecordId <= 100; currentRecordId++)
            {
                Document doc = new Document();
                doc.Add(NewTextField(FIELD_RECORD_ID, "" + currentRecordId, Field.Store.YES));
                w.AddDocument(doc);

                if (currentRecordId % 10 == 0)
                {
                    IDictionary<string, string> data = new Dictionary<string, string>();
                    data["index"] = "records 1-" + currentRecordId;
                    w.CommitData = data;
                    w.Commit();
                }
            }

            w.Dispose();
        }

        [TearDown]
        public override void TearDown()
        {
            Dir.Dispose();
            base.TearDown();
        }

        // Rolls back to previous commit point
        internal class RollbackDeletionPolicy<T> : IndexDeletionPolicy
            where T : IndexCommit
        {
            private readonly TestTransactionRollback OuterInstance;

            internal int RollbackPoint;

            public RollbackDeletionPolicy(TestTransactionRollback outerInstance, int rollbackPoint)
            {
                this.OuterInstance = outerInstance;
                this.RollbackPoint = rollbackPoint;
            }

            public override void OnCommit<T>(IList<T> commits)
            {
            }

            public override void OnInit<T>(IList<T> commits)
            {
                foreach (IndexCommit commit in commits)
                {
                    IDictionary<string, string> userData = commit.UserData;
                    if (userData.Count > 0)
                    {
                        // Label for a commit point is "Records 1-30"
                        // this code reads the last id ("30" in this example) and deletes it
                        // if it is after the desired rollback point
                        string x = userData["index"];
                        string lastVal = x.Substring(x.LastIndexOf("-") + 1);
                        int last = Convert.ToInt32(lastVal);
                        if (last > RollbackPoint)
                        {
                            /*
                            System.out.print("\tRolling back commit point:" +
                                             " UserData="+commit.getUserData() +")  ("+(commits.Size()-1)+" commit points left) files=");
                            Collection files = commit.getFileNames();
                            for (Iterator iterator2 = files.iterator(); iterator2.hasNext();) {
                              System.out.print(" "+iterator2.Next());
                            }
                            System.out.println();
                            */

                            commit.Delete();
                        }
                    }
                }
            }
        }

        internal class DeleteLastCommitPolicy<T> : IndexDeletionPolicy
            where T : IndexCommit
        {
            private readonly TestTransactionRollback OuterInstance;

            public DeleteLastCommitPolicy(TestTransactionRollback outerInstance)
            {
                this.OuterInstance = outerInstance;
            }

            public override void OnCommit<T>(IList<T> commits)
            {
            }

            public override void OnInit<T>(IList<T> commits)
            {
                commits.RemoveAt(commits.Count - 1);
            }
        }

        [Test]
        public virtual void TestRollbackDeletionPolicy()
        {
            for (int i = 0; i < 2; i++)
            {
                // Unless you specify a prior commit point, rollback
                // should not work:
                (new IndexWriter(Dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetIndexDeletionPolicy(new DeleteLastCommitPolicy<IndexCommit>(this)))).Dispose();
                IndexReader r = DirectoryReader.Open(Dir);
                Assert.AreEqual(100, r.NumDocs);
                r.Dispose();
            }
        }

        // Keeps all commit points (used to build index)
        internal class KeepAllDeletionPolicy<T> : IndexDeletionPolicy
            where T : IndexCommit
        {
            private readonly TestTransactionRollback OuterInstance;

            public KeepAllDeletionPolicy(TestTransactionRollback outerInstance)
            {
                this.OuterInstance = outerInstance;
            }

            public override void OnCommit<T>(IList<T> commits)
            {
            }

            public override void OnInit<T>(IList<T> commits)
            {
            }
        }
    }
}