using Lucene.Net.Codecs.MockIntBlock;
using Lucene.Net.Codecs.Sep;
using Lucene.Net.Store;
using Lucene.Net.Util;
using NUnit.Framework;

namespace Lucene.Net.Codecs.IntBlock
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

    public class TestIntBlockCodec : LuceneTestCase
    {

        [Test]
        public virtual void TestSimpleIntBlocks()
        {
            Directory dir = NewDirectory();

            Int32StreamFactory f = (new MockFixedInt32BlockPostingsFormat(128)).GetInt32Factory();

            Int32IndexOutput @out = f.CreateOutput(dir, "test", NewIOContext(Random));
            for (int i = 0; i < 11777; i++)
            {
                @out.Write(i);
            }
            @out.Dispose();

            Int32IndexInput @in = f.OpenInput(dir, "test", NewIOContext(Random));
            Int32IndexInput.Reader r = @in.GetReader();

            for (int i = 0; i < 11777; i++)
            {
                assertEquals(i, r.Next());
            }
            @in.Dispose();

            dir.Dispose();
        }

        [Test]
        public virtual void TestEmptySimpleIntBlocks()
        {
            Directory dir = NewDirectory();

            Int32StreamFactory f = (new MockFixedInt32BlockPostingsFormat(128)).GetInt32Factory();
            Int32IndexOutput @out = f.CreateOutput(dir, "test", NewIOContext(Random));

            // write no ints
            @out.Dispose();

            Int32IndexInput @in = f.OpenInput(dir, "test", NewIOContext(Random));
            @in.GetReader();
            // read no ints
            @in.Dispose();
            dir.Dispose();
        }
    }
}