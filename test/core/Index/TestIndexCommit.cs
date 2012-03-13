/*
 *
 * Licensed to the Apache Software Foundation (ASF) under one
 * or more contributor license agreements.  See the NOTICE file
 * distributed with this work for additional information
 * regarding copyright ownership.  The ASF licenses this file
 * to you under the Apache License, Version 2.0 (the
 * "License"); you may not use this file except in compliance
 * with the License.  You may obtain a copy of the License at
 *
 *   http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing,
 * software distributed under the License is distributed on an
 * "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
 * KIND, either express or implied.  See the License for the
 * specific language governing permissions and limitations
 * under the License.
 *
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Util;
using NUnit.Framework;

namespace Lucene.Net.Index
{
    public class TestIndexCommit : LuceneTestCase
    {
        private Directory dir;

        private class IndexCommitFirst : IndexCommit
        {
            private readonly Directory _dir;

            public IndexCommitFirst(Directory dir)
            {
                _dir = dir;
            }

            public override string SegmentsFileName
            {
                get { return "a"; }
            }

            public override ICollection<string> FileNames
            {
                get { return null; }
            }

            public override void Delete()
            { }

            public override bool IsDeleted
            {
                get { return false; }
            }

            public override bool IsOptimized
            {
                get { return false; }
            }

            public override long Version
            {
                get { return 12; }
            }

            public override long Generation
            {
                get { return 0; }
            }

            public override IDictionary<string, string> UserData
            {
                get { return null; }
            }

            public override Directory Directory
            {
                get { return _dir; }
            }

            public override long Timestamp
            {
                get { return 1; }
            }
        }
        private class IndexCommitSecond : IndexCommit
        {
            private readonly Directory _dir;

            public IndexCommitSecond(Directory dir)
            {
                _dir = dir;
            }

            public override string SegmentsFileName
            {
                get { return "b"; }
            }

            public override ICollection<string> FileNames
            {
                get { return null; }
            }

            public override void Delete()
            { }

            public override bool IsDeleted
            {
                get { return false; }
            }

            public override bool IsOptimized
            {
                get { return false; }
            }

            public override long Version
            {
                get { return 12; }
            }

            public override long Generation
            {
                get { return 0; }
            }

            public override IDictionary<string, string> UserData
            {
                get { return null; }
            }

            public override Directory Directory
            {
                get { return _dir; }
            }

            public override long Timestamp
            {
                get { return 1; }
            }
        }

        [Test]
        public void TestEqualsHashCode()
        {
            dir = new RAMDirectory();
            var ic1 = new IndexCommitFirst(dir);
            var ic2 = new IndexCommitSecond(dir);
            Assert.AreEqual(ic1, ic2);
            Assert.AreEqual(ic1.GetHashCode(), ic2.GetHashCode(), "Hash codes are not equals");
        }
    }
}
