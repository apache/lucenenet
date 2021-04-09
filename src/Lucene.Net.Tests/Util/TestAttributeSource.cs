using Lucene.Net.Analysis.TokenAttributes;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using Assert = Lucene.Net.TestFramework.Assert;
using FlagsAttribute = Lucene.Net.Analysis.TokenAttributes.FlagsAttribute;

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

    using Token = Lucene.Net.Analysis.Token;

    [TestFixture]
    public class TestAttributeSource : LuceneTestCase
    {
        [Test]
        public virtual void TestCaptureState()
        {
            // init a first instance
            AttributeSource src = new AttributeSource();
            ICharTermAttribute termAtt = src.AddAttribute<ICharTermAttribute>();
            ITypeAttribute typeAtt = src.AddAttribute<ITypeAttribute>();
            termAtt.Append("TestTerm");
            typeAtt.Type = "TestType";
            int hashCode = src.GetHashCode();

            AttributeSource.State state = src.CaptureState();

            // modify the attributes
            termAtt.SetEmpty().Append("AnotherTestTerm");
            typeAtt.Type = "AnotherTestType";
            Assert.IsTrue(hashCode != src.GetHashCode(), "Hash code should be different");

            src.RestoreState(state);
            Assert.AreEqual(termAtt.ToString(), "TestTerm");
            Assert.AreEqual(typeAtt.Type, "TestType");
            Assert.AreEqual(hashCode, src.GetHashCode(), "Hash code should be equal after restore");

            // restore into an exact configured copy
            AttributeSource copy = new AttributeSource();
            copy.AddAttribute<ICharTermAttribute>();
            copy.AddAttribute<ITypeAttribute>();
            copy.RestoreState(state);
            Assert.AreEqual(src.GetHashCode(), copy.GetHashCode(), "Both AttributeSources should have same hashCode after restore");
            Assert.AreEqual(src, copy, "Both AttributeSources should be equal after restore");

            // init a second instance (with attributes in different order and one additional attribute)
            AttributeSource src2 = new AttributeSource();
            typeAtt = src2.AddAttribute<ITypeAttribute>();
            IFlagsAttribute flagsAtt = src2.AddAttribute<IFlagsAttribute>();
            termAtt = src2.AddAttribute<ICharTermAttribute>();
            flagsAtt.Flags = 12345;

            src2.RestoreState(state);
            Assert.AreEqual(termAtt.ToString(), "TestTerm");
            Assert.AreEqual(typeAtt.Type, "TestType");
            Assert.AreEqual(12345, flagsAtt.Flags, "FlagsAttribute should not be touched");

            // init a third instance missing one Attribute
            AttributeSource src3 = new AttributeSource();
            termAtt = src3.AddAttribute<ICharTermAttribute>();
            try
            {
                src3.RestoreState(state);
                Assert.Fail("The third instance is missing the TypeAttribute, so restoreState() should throw IllegalArgumentException");
            }
            catch (Exception iae) when (iae.IsIllegalArgumentException())
            {
                // pass
            }
        }

        [Test]
        public virtual void TestCloneAttributes()
        {
            AttributeSource src = new AttributeSource();
            IFlagsAttribute flagsAtt = src.AddAttribute<IFlagsAttribute>();
            ITypeAttribute typeAtt = src.AddAttribute<ITypeAttribute>();
            flagsAtt.Flags = 1234;
            typeAtt.Type = "TestType";

            AttributeSource clone = src.CloneAttributes();
            IEnumerator<Type> it = clone.GetAttributeClassesEnumerator();
            it.MoveNext();
            Assert.AreEqual(typeof(IFlagsAttribute), it.Current, "FlagsAttribute must be the first attribute");
            it.MoveNext();
            Assert.AreEqual(typeof(ITypeAttribute), it.Current, "TypeAttribute must be the second attribute");
            Assert.IsFalse(it.MoveNext(), "No more attributes");

            IFlagsAttribute flagsAtt2 = clone.GetAttribute<IFlagsAttribute>();
            ITypeAttribute typeAtt2 = clone.GetAttribute<ITypeAttribute>();
            Assert.AreNotSame(flagsAtt2, flagsAtt, "FlagsAttribute of original and clone must be different instances");
            Assert.AreNotSame(typeAtt2, typeAtt, "TypeAttribute of original and clone must be different instances");
            Assert.AreEqual(flagsAtt2, flagsAtt, "FlagsAttribute of original and clone must be equal");
            Assert.AreEqual(typeAtt2, typeAtt, "TypeAttribute of original and clone must be equal");

            // test copy back
            flagsAtt2.Flags = 4711;
            typeAtt2.Type = "OtherType";
            clone.CopyTo(src);
            Assert.AreEqual(4711, flagsAtt.Flags, "FlagsAttribute of original must now contain updated term");
            Assert.AreEqual(typeAtt.Type, "OtherType", "TypeAttribute of original must now contain updated type");
            // verify again:
            Assert.AreNotSame(flagsAtt2, flagsAtt, "FlagsAttribute of original and clone must be different instances");
            Assert.AreNotSame(typeAtt2, typeAtt, "TypeAttribute of original and clone must be different instances");
            Assert.AreEqual(flagsAtt2, flagsAtt, "FlagsAttribute of original and clone must be equal");
            Assert.AreEqual(typeAtt2, typeAtt, "TypeAttribute of original and clone must be equal");
        }

        [Test]
        public virtual void TestDefaultAttributeFactory()
        {
            AttributeSource src = new AttributeSource();

            Assert.IsTrue(src.AddAttribute<ICharTermAttribute>() is CharTermAttribute, "CharTermAttribute is not implemented by CharTermAttributeImpl");
            Assert.IsTrue(src.AddAttribute<IOffsetAttribute>() is OffsetAttribute, "OffsetAttribute is not implemented by OffsetAttributeImpl");
            Assert.IsTrue(src.AddAttribute<IFlagsAttribute>() is FlagsAttribute, "FlagsAttribute is not implemented by FlagsAttributeImpl");
            Assert.IsTrue(src.AddAttribute<IPayloadAttribute>() is PayloadAttribute, "PayloadAttribute is not implemented by PayloadAttributeImpl");
            Assert.IsTrue(src.AddAttribute<IPositionIncrementAttribute>() is PositionIncrementAttribute, "PositionIncrementAttribute is not implemented by PositionIncrementAttributeImpl");
            Assert.IsTrue(src.AddAttribute<ITypeAttribute>() is TypeAttribute, "TypeAttribute is not implemented by TypeAttributeImpl");
        }

        [Test]
        public virtual void TestInvalidArguments()
        {
            try
            {
                AttributeSource src = new AttributeSource();
                src.AddAttribute<Token>();
                Assert.Fail("Should throw IllegalArgumentException");
            }
            catch (Exception iae) when (iae.IsIllegalArgumentException())
            {
            }

            try
            {
                AttributeSource src = new AttributeSource(Token.TOKEN_ATTRIBUTE_FACTORY);
                src.AddAttribute<Token>();
                Assert.Fail("Should throw IllegalArgumentException");
            }
            catch (Exception iae) when (iae.IsIllegalArgumentException())
            {
            }


            // LUCENENET NOTE: Invalid type won't compile because
            // of the generic constraint, so this test is not necessary in .NET.

            /*try
            {
              AttributeSource src = new AttributeSource();
              // break this by unsafe cast
              src.AddAttribute<typeof((Type)IEnumerator)>();
              Assert.Fail("Should throw IllegalArgumentException");
            }
            catch (Exception iae) when (iae.IsIllegalArgumentException())
            {
            }*/
        }

        [Test]
        public virtual void TestLUCENE_3042()
        {
            AttributeSource src1 = new AttributeSource();
            src1.AddAttribute<ICharTermAttribute>().Append("foo");
            int hash1 = src1.GetHashCode(); // this triggers a cached state
            AttributeSource src2 = new AttributeSource(src1);
            src2.AddAttribute<ITypeAttribute>().Type = "bar";
            Assert.IsTrue(hash1 != src1.GetHashCode(), "The hashCode is identical, so the captured state was preserved.");
            Assert.AreEqual(src2.GetHashCode(), src1.GetHashCode());
        }
    }
}