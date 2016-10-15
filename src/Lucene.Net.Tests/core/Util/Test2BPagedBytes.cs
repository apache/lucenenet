using Lucene.Net.Randomized.Generators;
using NUnit.Framework;
using System;
using System.Diagnostics;

namespace Lucene.Net.Util
{
    /*
    /// Licensed to the Apache Software Foundation (ASF) under one or more
    /// contributor license agreements.  See the NOTICE file distributed with
    /// this work for additional information regarding copyright ownership.
    /// The ASF licenses this file to You under the Apache License, Version 2.0
    /// (the "License"); you may not use this file except in compliance with
    /// the License.  You may obtain a copy of the License at
    ///
    ///     http://www.apache.org/licenses/LICENSE-2.0
    ///
    /// Unless required by applicable law or agreed to in writing, software
    /// distributed under the License is distributed on an "AS IS" BASIS,
    /// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
    /// See the License for the specific language governing permissions and
    /// limitations under the License.
    */

    using BaseDirectoryWrapper = Lucene.Net.Store.BaseDirectoryWrapper;
    using IndexInput = Lucene.Net.Store.IndexInput;
    using IndexOutput = Lucene.Net.Store.IndexOutput;
    using IOContext = Lucene.Net.Store.IOContext;
    using MockDirectoryWrapper = Lucene.Net.Store.MockDirectoryWrapper;

    //using Ignore = org.junit.Ignore;

    //ORIGINAL LINE: @Ignore("You must increase heap to > 2 G to run this") public class Test2BPagedBytes extends LuceneTestCase
    [Ignore("You must increase heap to > 2 G to run this")]
    [TestFixture]
    public class Test2BPagedBytes : LuceneTestCase
    {
        [Test]
        public virtual void Test()
        {
            BaseDirectoryWrapper dir = NewFSDirectory(CreateTempDir("test2BPagedBytes"));
            if (dir is MockDirectoryWrapper)
            {
                ((MockDirectoryWrapper)dir).Throttling = MockDirectoryWrapper.Throttling_e.NEVER;
            }
            PagedBytes pb = new PagedBytes(15);
            IndexOutput dataOutput = dir.CreateOutput("foo", IOContext.DEFAULT);
            long netBytes = 0;
            long seed = Random().NextLong();
            long lastFP = 0;
            Random r2 = new Random((int)seed);
            while (netBytes < 1.1 * int.MaxValue)
            {
                int numBytes = TestUtil.NextInt(r2, 1, 32768);
                byte[] bytes = new byte[numBytes];
                r2.NextBytes(bytes);
                dataOutput.WriteBytes(bytes, bytes.Length);
                long fp = dataOutput.FilePointer;
                Debug.Assert(fp == lastFP + numBytes);
                lastFP = fp;
                netBytes += numBytes;
            }
            dataOutput.Dispose();
            IndexInput input = dir.OpenInput("foo", IOContext.DEFAULT);
            pb.Copy(input, input.Length());
            input.Dispose();
            PagedBytes.Reader reader = pb.Freeze(true);

            r2 = new Random((int)seed);
            netBytes = 0;
            while (netBytes < 1.1 * int.MaxValue)
            {
                int numBytes = TestUtil.NextInt(r2, 1, 32768);
                var bytes = new byte[numBytes];
                r2.NextBytes(bytes);
                BytesRef expected = new BytesRef(bytes);

                BytesRef actual = new BytesRef();
                reader.FillSlice(actual, netBytes, numBytes);
                Assert.AreEqual(expected, actual);

                netBytes += numBytes;
            }
            dir.Dispose();
        }
    }
}