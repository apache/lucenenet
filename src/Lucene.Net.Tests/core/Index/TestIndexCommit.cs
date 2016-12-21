using System.Collections.Generic;

namespace Lucene.Net.Index
{
    using NUnit.Framework;

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

            IndexCommit ic1 = new IndexCommitAnonymousInnerClassHelper(this, dir);

            IndexCommit ic2 = new IndexCommitAnonymousInnerClassHelper2(this, dir);

            Assert.AreEqual(ic1, ic2);
            Assert.AreEqual(ic1.GetHashCode(), ic2.GetHashCode(), "hash codes are not equals");
            dir.Dispose();
        }

        private class IndexCommitAnonymousInnerClassHelper : IndexCommit
        {
            private readonly TestIndexCommit OuterInstance;

            private Directory Dir;

            public IndexCommitAnonymousInnerClassHelper(TestIndexCommit outerInstance, Directory dir)
            {
                this.OuterInstance = outerInstance;
                this.Dir = dir;
            }

            public override string SegmentsFileName
            {
                get
                {
                    return "a";
                }
            }

            public override Directory Directory
            {
                get
                {
                    return Dir;
                }
            }

            public override ICollection<string> FileNames
            {
                get
                {
                    return null;
                }
            }

            public override void Delete()
            {
            }

            public override long Generation
            {
                get
                {
                    return 0;
                }
            }

            public override IDictionary<string, string> UserData
            {
                get
                {
                    return null;
                }
            }

            public override bool IsDeleted
            {
                get
                {
                    return false;
                }
            }

            public override int SegmentCount
            {
                get
                {
                    return 2;
                }
            }
        }

        private class IndexCommitAnonymousInnerClassHelper2 : IndexCommit
        {
            private readonly TestIndexCommit OuterInstance;

            private Directory Dir;

            public IndexCommitAnonymousInnerClassHelper2(TestIndexCommit outerInstance, Directory dir)
            {
                this.OuterInstance = outerInstance;
                this.Dir = dir;
            }

            public override string SegmentsFileName
            {
                get
                {
                    return "b";
                }
            }

            public override Directory Directory
            {
                get
                {
                    return Dir;
                }
            }

            public override ICollection<string> FileNames
            {
                get
                {
                    return null;
                }
            }

            public override void Delete()
            {
            }

            public override long Generation
            {
                get
                {
                    return 0;
                }
            }

            public override IDictionary<string, string> UserData
            {
                get
                {
                    return null;
                }
            }

            public override bool IsDeleted
            {
                get
                {
                    return false;
                }
            }

            public override int SegmentCount
            {
                get
                {
                    return 2;
                }
            }
        }
    }
}