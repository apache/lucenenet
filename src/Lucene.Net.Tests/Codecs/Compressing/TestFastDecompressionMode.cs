using NUnit.Framework;

namespace Lucene.Net.Codecs.Compressing
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

    [TestFixture]
    public class TestFastDecompressionMode : AbstractTestLZ4CompressionMode
    {
        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            Mode = CompressionMode.FAST_DECOMPRESSION;
        }

        public override byte[] Test(byte[] decompressed, int off, int len)
        {
            var compressed = base.Test(decompressed, off, len);
            var compressed2 = Compress(CompressionMode.FAST.NewCompressor(), decompressed, off, len);
            // because of the way this compression mode works, its output is necessarily
            // smaller than the output of CompressionMode.FAST
            Assert.IsTrue(compressed.Length <= compressed2.Length);
            return compressed;
        }


        #region AbstractTestLZ4CompressionMode
        // LUCENENET NOTE: Tests in an abstract base class are not pulled into the correct
        // context in Visual Studio. This fixes that with the minimum amount of code necessary
        // to run them in the correct context without duplicating all of the tests.

        [Test]
        public override void TestShortLiteralsAndMatchs()
        {
            base.TestShortLiteralsAndMatchs();
        }

        [Test]
        public override void TestLongMatchs()
        {
            base.TestLongMatchs();
        }

        [Test]
        public override void TestLongLiterals()
        {
            base.TestLongLiterals();
        }

        [Test]
        public override void TestMatchRightBeforeLastLiterals()
        {
            base.TestMatchRightBeforeLastLiterals();
        }

        #endregion

        #region AbstractTestCompressionMode
        // LUCENENET NOTE: Tests in an abstract base class are not pulled into the correct
        // context in Visual Studio. This fixes that with the minimum amount of code necessary
        // to run them in the correct context without duplicating all of the tests.

        [Test]
        public override void TestDecompress()
        {
            base.TestDecompress();
        }

        [Test]
        public override void TestPartialDecompress()
        {
            base.TestPartialDecompress();
        }

        [Test]
        public override void TestEmptySequence()
        {
            base.TestEmptySequence();
        }

        [Test]
        public override void TestShortSequence()
        {
            base.TestShortSequence();
        }

        [Test]
        public override void TestIncompressible()
        {
            base.TestIncompressible();
        }

        [Test]
        public override void TestConstant()
        {
            base.TestConstant();
        }

        [Test]
        public override void TestLUCENE5201()
        {
            base.TestLUCENE5201();
        }

        #endregion
    }
}