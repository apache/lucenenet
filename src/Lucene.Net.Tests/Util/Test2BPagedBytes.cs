﻿using Lucene.Net.Diagnostics;
using Lucene.Net.Store;
using NUnit.Framework;
using System;
using Assert = Lucene.Net.TestFramework.Assert;

#if !FEATURE_RANDOM_NEXTINT64_NEXTSINGLE
using RandomizedTesting.Generators; // for Random.NextInt64 extension method
#endif

namespace Lucene.Net.Util
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

    using BaseDirectoryWrapper = Lucene.Net.Store.BaseDirectoryWrapper;
    using IndexInput = Lucene.Net.Store.IndexInput;
    using IndexOutput = Lucene.Net.Store.IndexOutput;
    using IOContext = Lucene.Net.Store.IOContext;
    using MockDirectoryWrapper = Lucene.Net.Store.MockDirectoryWrapper;

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
                ((MockDirectoryWrapper)dir).Throttling = Throttling.NEVER;
            }
            PagedBytes pb = new PagedBytes(15);
            IndexOutput dataOutput = dir.CreateOutput("foo", IOContext.DEFAULT);
            long netBytes = 0;
            long seed = Random.NextInt64();
            long lastFP = 0;
            Random r2 = new J2N.Randomizer(seed);
            while (netBytes < 1.1 * int.MaxValue)
            {
                int numBytes = TestUtil.NextInt32(r2, 1, 32768);
                byte[] bytes = new byte[numBytes];
                r2.NextBytes(bytes);
                dataOutput.WriteBytes(bytes, bytes.Length);
                long fp = dataOutput.Position; // LUCENENET specific: Renamed from getFilePointer() to match FileStream
                if (Debugging.AssertsEnabled) Debugging.Assert(fp == lastFP + numBytes);
                lastFP = fp;
                netBytes += numBytes;
            }
            dataOutput.Dispose();
            IndexInput input = dir.OpenInput("foo", IOContext.DEFAULT);
            pb.Copy(input, input.Length);
            input.Dispose();
            PagedBytes.Reader reader = pb.Freeze(true);

            r2 = new J2N.Randomizer(seed);
            netBytes = 0;
            while (netBytes < 1.1 * int.MaxValue)
            {
                int numBytes = TestUtil.NextInt32(r2, 1, 32768);
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
