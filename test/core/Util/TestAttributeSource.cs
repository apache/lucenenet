/* 
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 * 
 * http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Collections;
using NUnit.Framework;

using Token = Lucene.Net.Analysis.Token;
using Lucene.Net.Analysis.Tokenattributes;
using FlagsAttribute = Lucene.Net.Analysis.Tokenattributes.FlagsAttribute;

namespace Lucene.Net.Util
{

    [TestFixture]
    public class TestAttributeSource : LuceneTestCase
    {

        [Test]
        public virtual void TestCaptureState()
        {
            // init a first instance
            var src = new AttributeSource();
            var termAtt = src.AddAttribute<ICharTermAttribute>();
            var typeAtt = src.AddAttribute<ITypeAttribute>();
            termAtt.Append("TestTerm");
            typeAtt.Type = "TestType";
            var hashCode = src.GetHashCode();

            var state = src.CaptureState();

            // modify the attributes
            termAtt.SetEmpty().Append("AnotherTestTerm");
            typeAtt.Type = "AnotherTestType";
            Assert.IsTrue(hashCode != src.GetHashCode(), "Hash code should be different");

            src.RestoreState(state);
            Assert.AreEqual("TestTerm", termAtt.ToString());
            Assert.AreEqual("TestType", typeAtt.Type);
            Assert.AreEqual(hashCode, src.GetHashCode(), "Hash code should be equal after restore");

            // restore into an exact configured copy
            var copy = new AttributeSource();
            copy.AddAttribute<ICharTermAttribute>();
            copy.AddAttribute<ITypeAttribute>();
            copy.RestoreState(state);
            Assert.AreEqual(src.GetHashCode(), copy.GetHashCode(), "Both AttributeSources should have same hashCode after restore");
            Assert.AreEqual(src, copy, "Both AttributeSources should be equal after restore");

            // init a second instance (with attributes in different order and one additional attribute)
            var src2 = new AttributeSource();
            typeAtt = src2.AddAttribute<ITypeAttribute>();
            var flagsAtt = src2.AddAttribute<IFlagsAttribute>();
            termAtt = src2.AddAttribute<ICharTermAttribute>();
            flagsAtt.Flags = 12345;

            src2.RestoreState(state);
            Assert.AreEqual("TestTerm", termAtt.ToString());
            Assert.AreEqual("TestType", typeAtt.Type);
            Assert.AreEqual(12345, flagsAtt.Flags, "FlagsAttribute should not be touched");

            // init a third instance missing one Attribute
            var src3 = new AttributeSource();
            termAtt = src3.AddAttribute<ICharTermAttribute>();

            Assert.Throws<ArgumentException>(() => src3.RestoreState(state),
                                             "The third instance is missing the TypeAttribute, so restoreState() should throw IllegalArgumentException");
        }

        [Test]
        public virtual void TestCloneAttributes()
        {
            var src = new AttributeSource();
            var flagsAtt = src.AddAttribute<IFlagsAttribute>();
            var typeAtt = src.AddAttribute<ITypeAttribute>();
            flagsAtt.Flags = 1234;
            typeAtt.Type = "TestType";

            var clone = src.CloneAttributes();
            var it = clone.GetAttributeTypesIterator().GetEnumerator();
            Assert.IsTrue(it.MoveNext());
            Assert.AreEqual(typeof(IFlagsAttribute), it.Current, "FlagsAttribute must be the first attribute");
            Assert.IsTrue(it.MoveNext());
            Assert.AreEqual(typeof(ITypeAttribute), it.Current, "TypeAttribute must be the second attribute");
            Assert.IsFalse(it.MoveNext(), "No more attributes");

            var flagsAtt2 = clone.GetAttribute<IFlagsAttribute>();
            var typeAtt2 = clone.GetAttribute<ITypeAttribute>();
            Assert.That(flagsAtt2 != flagsAtt, "TermAttribute of original and clone must be different instances");
            Assert.That(typeAtt2 != typeAtt, "TypeAttribute of original and clone must be different instances");
            Assert.AreEqual(flagsAtt2, flagsAtt, "TermAttribute of original and clone must be equal");
            Assert.AreEqual(typeAtt2, typeAtt, "TypeAttribute of original and clone must be equal");
        }

        [Test]
        public void TestDefaultAttributeFactory()
        {
            var src = new AttributeSource();

            Assert.IsTrue(src.AddAttribute<ICharTermAttribute>() is CharTermAttribute,
                          "CharTermAttribute is not implemented by CharTermAttributeImpl");
            Assert.IsTrue(src.AddAttribute<IOffsetAttribute>() is OffsetAttribute,
                          "OffsetAttribute is not implemented by OffsetAttributeImpl");
            Assert.IsTrue(src.AddAttribute<IFlagsAttribute>() is FlagsAttribute,
                          "FlagsAttribute is not implemented by FlagsAttributeImpl");
            Assert.IsTrue(src.AddAttribute<IPayloadAttribute>() is PayloadAttribute,
                          "PayloadAttribute is not implemented by PayloadAttributeImpl");
            Assert.IsTrue(src.AddAttribute<IPositionIncrementAttribute>() is PositionIncrementAttribute,
                          "PositionIncrementAttribute is not implemented by PositionIncrementAttributeImpl");
            Assert.IsTrue(src.AddAttribute<ITypeAttribute>() is TypeAttribute,
                          "TypeAttribute is not implemented by TypeAttributeImpl");
        }

        [Test]
        public void TestInvalidArguments()
        {
            var src = new AttributeSource();
            Assert.Throws<ArgumentException>(() => src.AddAttribute<Token>(), "Should throw ArgumentException");

            src = new AttributeSource(Token.TOKEN_ATTRIBUTE_FACTORY);
            Assert.Throws<ArgumentException>(() => src.AddAttribute<Token>(), "Should throw ArgumentException");
        
            src = new AttributeSource();
            // TODO: how to fix this??
            // orginal Java is: src.addAttribute((Class) Iterator.class);  // break this by unsafe cast
            Assert.Throws<ArgumentException>(() => src.AddAttribute<IEnumerator>(), "Should throw ArgumentException");
        }

        [Test]
        public void TestLUCENE_3042()
        {
            var src1 = new AttributeSource();
            src1.AddAttribute<ICharTermAttribute>().Append("foo");
            var hash1 = src1.GetHashCode(); // this triggers a cached state
            var src2 = new AttributeSource(src1);
            src2.AddAttribute<ITypeAttribute>().Type = "bar";
            Assert.True(hash1 != src1.GetHashCode(), "The hashCode is identical, so the captured state was preserved.");
            Assert.Equals(src2.GetHashCode(), src1.GetHashCode());
        }
    }
    
    
}