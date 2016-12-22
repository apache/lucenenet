using System;
using System.Collections.Generic;

namespace Lucene.Net.Index
{
    using Lucene.Net.Randomized.Generators;
    using NUnit.Framework;
    using System.IO;

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

    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;

    [TestFixture]
    public class TestTwoPhaseCommitTool : LuceneTestCase
    {
        private class TwoPhaseCommitImpl : ITwoPhaseCommit
        {
            internal static bool CommitCalled = false;
            internal readonly bool FailOnPrepare;
            internal readonly bool FailOnCommit;
            internal readonly bool FailOnRollback;
            internal bool RollbackCalled = false;
            internal IDictionary<string, string> PrepareCommitData = null;
            internal IDictionary<string, string> CommitData = null;

            public TwoPhaseCommitImpl(bool failOnPrepare, bool failOnCommit, bool failOnRollback)
            {
                this.FailOnPrepare = failOnPrepare;
                this.FailOnCommit = failOnCommit;
                this.FailOnRollback = failOnRollback;
            }

            public void PrepareCommit()
            {
                PrepareCommit(null);
            }

            public virtual void PrepareCommit(IDictionary<string, string> commitData)
            {
                this.PrepareCommitData = commitData;
                Assert.IsFalse(CommitCalled, "commit should not have been called before all prepareCommit were");
                if (FailOnPrepare)
                {
                    throw new IOException("failOnPrepare");
                }
            }

            public void Commit()
            {
                Commit(null);
            }

            public virtual void Commit(IDictionary<string, string> commitData)
            {
                this.CommitData = commitData;
                CommitCalled = true;
                if (FailOnCommit)
                {
                    throw new Exception("failOnCommit");
                }
            }

            public void Rollback()
            {
                RollbackCalled = true;
                if (FailOnRollback)
                {
                    throw new Exception("failOnRollback");
                }
            }
        }

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            TwoPhaseCommitImpl.CommitCalled = false; // reset count before every test
        }

        [Test]
        public virtual void TestPrepareThenCommit()
        {
            // tests that prepareCommit() is called on all objects before commit()
            TwoPhaseCommitImpl[] objects = new TwoPhaseCommitImpl[2];
            for (int i = 0; i < objects.Length; i++)
            {
                objects[i] = new TwoPhaseCommitImpl(false, false, false);
            }

            // following call will fail if commit() is called before all prepare() were
            TwoPhaseCommitTool.Execute(objects);
        }

        [Test]
        public virtual void TestRollback()
        {
            // tests that rollback is called if failure occurs at any stage
            int numObjects = Random().Next(8) + 3; // between [3, 10]
            TwoPhaseCommitImpl[] objects = new TwoPhaseCommitImpl[numObjects];
            for (int i = 0; i < objects.Length; i++)
            {
                bool failOnPrepare = Random().NextBoolean();
                // we should not hit failures on commit usually
                bool failOnCommit = Random().NextDouble() < 0.05;
                bool railOnRollback = Random().NextBoolean();
                objects[i] = new TwoPhaseCommitImpl(failOnPrepare, failOnCommit, railOnRollback);
            }

            bool anyFailure = false;
            try
            {
                TwoPhaseCommitTool.Execute(objects);
            }
            catch (Exception t)
            {
                anyFailure = true;
            }

            if (anyFailure)
            {
                // if any failure happened, ensure that rollback was called on all.
                foreach (TwoPhaseCommitImpl tpc in objects)
                {
                    Assert.IsTrue(tpc.RollbackCalled, "rollback was not called while a failure occurred during the 2-phase commit");
                }
            }
        }

        [Test]
        public virtual void TestNullTPCs()
        {
            int numObjects = Random().Next(4) + 3; // between [3, 6]
            ITwoPhaseCommit[] tpcs = new ITwoPhaseCommit[numObjects];
            bool setNull = false;
            for (int i = 0; i < tpcs.Length; i++)
            {
                bool isNull = Random().NextDouble() < 0.3;
                if (isNull)
                {
                    setNull = true;
                    tpcs[i] = null;
                }
                else
                {
                    tpcs[i] = new TwoPhaseCommitImpl(false, false, false);
                }
            }

            if (!setNull)
            {
                // none of the TPCs were picked to be null, pick one at random
                int idx = Random().Next(numObjects);
                tpcs[idx] = null;
            }

            // following call would fail if TPCTool won't handle null TPCs properly
            TwoPhaseCommitTool.Execute(tpcs);
        }
    }
}