using NUnit.Framework;
using System;
using System.IO;

namespace Lucene.Net.Store
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

    using IndexWriter = Lucene.Net.Index.IndexWriter;
    using IndexWriterConfig = Lucene.Net.Index.IndexWriterConfig;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;

    [TestFixture]
    public class TestMockDirectoryWrapper : LuceneTestCase
    {
        [Test]
        public virtual void TestFailIfIndexWriterNotClosed()
        {
            MockDirectoryWrapper dir = NewMockDirectory();
            IndexWriter iw = new IndexWriter(dir, new IndexWriterConfig(TEST_VERSION_CURRENT, null));
            try
            {
                dir.Dispose();
                Assert.Fail();
            }
            catch (Exception expected)
            {
                Assert.IsTrue(expected.Message.Contains("there are still open locks"));
            }
            iw.Dispose();
            dir.Dispose();
        }

        [Test]
        public virtual void TestFailIfIndexWriterNotClosedChangeLockFactory()
        {
            MockDirectoryWrapper dir = NewMockDirectory();
            dir.LockFactory = new SingleInstanceLockFactory();
            IndexWriter iw = new IndexWriter(dir, new IndexWriterConfig(TEST_VERSION_CURRENT, null));
            try
            {
                dir.Dispose();
                Assert.Fail();
            }
            catch (Exception expected)
            {
                Assert.IsTrue(expected.Message.Contains("there are still open locks"));
            }
            iw.Dispose();
            dir.Dispose();
        }

        [Test]
        public void TestDiskFull()
        {
            // test writeBytes
            var dir = NewMockDirectory();
            dir.MaxSizeInBytes = 3;
            var bytes = new byte[] { 1, 2 };
            IndexOutput @out = dir.CreateOutput("foo", IOContext.DEFAULT);
            @out.WriteBytes(bytes, bytes.Length); // first write should succeed
            // flush() to ensure the written bytes are not buffered and counted
            // against the directory size
            @out.Flush();
            try
            {
                @out.WriteBytes(bytes, bytes.Length);
                Assert.Fail("should have failed on disk full");
            }
            catch (IOException e)
            {
                // expected
            }
            @out.Dispose();
            dir.Dispose();

            // test copyBytes
            dir = NewMockDirectory();
            dir.MaxSizeInBytes = 3;
            @out = dir.CreateOutput("foo", IOContext.DEFAULT);
            @out.CopyBytes(new ByteArrayDataInput(bytes), bytes.Length); // first copy should succeed
            // flush() to ensure the written bytes are not buffered and counted
            // against the directory size
            @out.Flush();
            try
            {
                @out.CopyBytes(new ByteArrayDataInput(bytes), bytes.Length);
                Assert.Fail("should have failed on disk full");
            }
            catch (IOException e)
            {
                // expected
            }
            @out.Dispose();
            dir.Dispose();
        }
    }
}