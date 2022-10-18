using System.Collections.Generic;
using NUnit.Framework;
using Assert = Lucene.Net.TestFramework.Assert;

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

    using Directory = Lucene.Net.Store.Directory;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;

    [TestFixture]
    public class TestIndexCommit : LuceneTestCase
    {
        [Test]
        public virtual void TestEqualsHashCode()
        {
            // LUCENE-2417: equals and hashCode() impl was inconsistent
            Directory dir = NewDirectory();

            IndexCommit ic1 = new IndexCommitAnonymousClass(this, dir);

            IndexCommit ic2 = new IndexCommitAnonymousClass2(this, dir);

            Assert.AreEqual(ic1, ic2);
            Assert.AreEqual(ic1.GetHashCode(), ic2.GetHashCode(), "hash codes are not equals");
            dir.Dispose();
        }

        private sealed class IndexCommitAnonymousClass : IndexCommit
        {
            private readonly TestIndexCommit outerInstance;

            private Directory dir;

            public IndexCommitAnonymousClass(TestIndexCommit outerInstance, Directory dir)
            {
                this.outerInstance = outerInstance;
                this.dir = dir;
            }

            public override string SegmentsFileName => "a";

            public override Directory Directory => dir;

            public override ICollection<string> FileNames => null;

            public override void Delete()
            {
            }

            public override long Generation => 0;

            public override IDictionary<string, string> UserData => null;

            public override bool IsDeleted => false;

            public override int SegmentCount => 2;
        }

        private sealed class IndexCommitAnonymousClass2 : IndexCommit
        {
            private readonly TestIndexCommit outerInstance;

            private Directory dir;

            public IndexCommitAnonymousClass2(TestIndexCommit outerInstance, Directory dir)
            {
                this.outerInstance = outerInstance;
                this.dir = dir;
            }

            public override string SegmentsFileName => "b";

            public override Directory Directory => dir;

            public override ICollection<string> FileNames => null;

            public override void Delete()
            {
            }

            public override long Generation => 0;

            public override IDictionary<string, string> UserData => null;

            public override bool IsDeleted => false;

            public override int SegmentCount => 2;
        }
    }
}