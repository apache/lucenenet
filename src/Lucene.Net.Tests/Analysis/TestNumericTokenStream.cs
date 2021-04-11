using Lucene.Net.Analysis.TokenAttributes;
using NUnit.Framework;
using System;
using Assert = Lucene.Net.TestFramework.Assert;

namespace Lucene.Net.Analysis
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

    using BytesRef = Lucene.Net.Util.BytesRef;
    using CharTermAttribute = Lucene.Net.Analysis.TokenAttributes.CharTermAttribute;
    using ICharTermAttribute = Lucene.Net.Analysis.TokenAttributes.ICharTermAttribute;
    using NumericUtils = Lucene.Net.Util.NumericUtils;

    [TestFixture]
    public class TestNumericTokenStream : BaseTokenStreamTestCase
    {
        internal const long lvalue = 4573245871874382L;
        internal const int ivalue = 123456;

        [NUnit.Framework.Test]
        public virtual void TestLongStream()
        {
            using NumericTokenStream stream = (new NumericTokenStream()).SetInt64Value(lvalue);
            // use getAttribute to test if attributes really exist, if not an IAE will be throwed
            ITermToBytesRefAttribute bytesAtt = stream.GetAttribute<ITermToBytesRefAttribute>();
            ITypeAttribute typeAtt = stream.GetAttribute<ITypeAttribute>();
            NumericTokenStream.INumericTermAttribute numericAtt = stream.GetAttribute<NumericTokenStream.INumericTermAttribute>();
            BytesRef bytes = bytesAtt.BytesRef;
            stream.Reset();
            Assert.AreEqual(64, numericAtt.ValueSize);
            for (int shift = 0; shift < 64; shift += NumericUtils.PRECISION_STEP_DEFAULT)
            {
                Assert.IsTrue(stream.IncrementToken(), "New token is available");
                Assert.AreEqual(shift, numericAtt.Shift, "Shift value wrong");
                bytesAtt.FillBytesRef();
                Assert.AreEqual(lvalue & ~((1L << shift) - 1L), NumericUtils.PrefixCodedToInt64(bytes), "Term is incorrectly encoded");
                Assert.AreEqual(lvalue & ~((1L << shift) - 1L), numericAtt.RawValue, "Term raw value is incorrectly encoded");
                Assert.AreEqual((shift == 0) ? NumericTokenStream.TOKEN_TYPE_FULL_PREC : NumericTokenStream.TOKEN_TYPE_LOWER_PREC, typeAtt.Type, "Type incorrect");
            }
            Assert.IsFalse(stream.IncrementToken(), "More tokens available");
            stream.End();
        }

        [NUnit.Framework.Test]
        public virtual void TestIntStream()
        {
            NumericTokenStream stream = (new NumericTokenStream()).SetInt32Value(ivalue);
            // use getAttribute to test if attributes really exist, if not an IAE will be throwed
            ITermToBytesRefAttribute bytesAtt = stream.GetAttribute<ITermToBytesRefAttribute>();
            ITypeAttribute typeAtt = stream.GetAttribute<ITypeAttribute>();
            NumericTokenStream.INumericTermAttribute numericAtt = stream.GetAttribute<NumericTokenStream.INumericTermAttribute>();
            BytesRef bytes = bytesAtt.BytesRef;
            stream.Reset();
            Assert.AreEqual(32, numericAtt.ValueSize);
            for (int shift = 0; shift < 32; shift += NumericUtils.PRECISION_STEP_DEFAULT)
            {
                Assert.IsTrue(stream.IncrementToken(), "New token is available");
                Assert.AreEqual(shift, numericAtt.Shift, "Shift value wrong");
                bytesAtt.FillBytesRef();
                Assert.AreEqual(ivalue & ~((1 << shift) - 1), NumericUtils.PrefixCodedToInt32(bytes), "Term is incorrectly encoded");
                Assert.AreEqual(((long)ivalue) & ~((1L << shift) - 1L), numericAtt.RawValue, "Term raw value is incorrectly encoded");
                Assert.AreEqual((shift == 0) ? NumericTokenStream.TOKEN_TYPE_FULL_PREC : NumericTokenStream.TOKEN_TYPE_LOWER_PREC, typeAtt.Type, "Type incorrect");
            }
            Assert.IsFalse(stream.IncrementToken(), "More tokens available");
            stream.End();
            stream.Dispose();
        }

        [NUnit.Framework.Test]
        public virtual void TestNotInitialized()
        {
            NumericTokenStream stream = new NumericTokenStream();

            try
            {
                stream.Reset();
                Assert.Fail("reset() should not succeed.");
            }
            catch (Exception e) when (e.IsIllegalStateException())
            {
                // pass
            }

            try
            {
                stream.IncrementToken();
                Assert.Fail("IncrementToken() should not succeed.");
            }
            catch (Exception e) when (e.IsIllegalStateException())
            {
                // pass
            }
        }

        public interface ITestAttribute : ICharTermAttribute
        {
        }

        public class TestAttribute : CharTermAttribute, ITestAttribute
        {
        }

        [NUnit.Framework.Test]
        public virtual void TestCTA()
        {
            NumericTokenStream stream = new NumericTokenStream();
            try
            {
                stream.AddAttribute<ICharTermAttribute>();
                Assert.Fail("Succeeded to add CharTermAttribute.");
            }
            catch (Exception iae) when (iae.IsIllegalArgumentException())
            {
                Assert.IsTrue(iae.Message.StartsWith("NumericTokenStream does not support", StringComparison.Ordinal));
            }
            try
            {
                stream.AddAttribute<ITestAttribute>();
                Assert.Fail("Succeeded to add TestAttribute.");
            }
            catch (Exception iae) when (iae.IsIllegalArgumentException())
            {
                Assert.IsTrue(iae.Message.StartsWith("NumericTokenStream does not support", StringComparison.Ordinal));
            }
        }
    }
}