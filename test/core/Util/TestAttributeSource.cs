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
            AttributeSource src = new AttributeSource();
            ITermAttribute termAtt = src.AddAttribute<ITermAttribute>();
            ITypeAttribute typeAtt = src.AddAttribute<ITypeAttribute>();
            termAtt.SetTermBuffer("TestTerm");
            typeAtt.Type = "TestType";
            int hashCode = src.GetHashCode();

            AttributeSource.State state = src.CaptureState();

            // modify the attributes
            termAtt.SetTermBuffer("AnotherTestTerm");
            typeAtt.Type = "AnotherTestType";
            Assert.IsTrue(hashCode != src.GetHashCode(), "Hash code should be different");

            src.RestoreState(state);
            Assert.AreEqual("TestTerm", termAtt.Term);
            Assert.AreEqual("TestType", typeAtt.Type);
            Assert.AreEqual(hashCode, src.GetHashCode(), "Hash code should be equal after restore");

            // restore into an exact configured copy
            AttributeSource copy = new AttributeSource();
            copy.AddAttribute<ITermAttribute>();
            copy.AddAttribute<ITypeAttribute>();
            copy.RestoreState(state);
            Assert.AreEqual(src.GetHashCode(), copy.GetHashCode(), "Both AttributeSources should have same hashCode after restore");
            Assert.AreEqual(src, copy, "Both AttributeSources should be equal after restore");

            // init a second instance (with attributes in different order and one additional attribute)
            AttributeSource src2 = new AttributeSource();
            typeAtt = src2.AddAttribute<ITypeAttribute>();
            IFlagsAttribute flagsAtt = src2.AddAttribute<IFlagsAttribute>();
            termAtt = src2.AddAttribute<ITermAttribute>();
            flagsAtt.Flags = 12345;

            src2.RestoreState(state);
            Assert.AreEqual("TestTerm", termAtt.Term);
            Assert.AreEqual("TestType", typeAtt.Type);
            Assert.AreEqual(12345, flagsAtt.Flags, "FlagsAttribute should not be touched");

            // init a third instance missing one Attribute
            AttributeSource src3 = new AttributeSource();
            termAtt = src3.AddAttribute<ITermAttribute>();

            Assert.Throws<ArgumentException>(() => src3.RestoreState(state),
                                             "The third instance is missing the TypeAttribute, so restoreState() should throw IllegalArgumentException");
        }

        [Test]
        public virtual void TestCloneAttributes()
        {
            AttributeSource src = new AttributeSource();
            ITermAttribute termAtt = src.AddAttribute<ITermAttribute>();
            ITypeAttribute typeAtt = src.AddAttribute<ITypeAttribute>();
            termAtt.SetTermBuffer("TestTerm");
            typeAtt.Type = "TestType";

            AttributeSource clone = src.CloneAttributes();
            System.Collections.Generic.IEnumerator<Type> it = clone.GetAttributeTypesIterator().GetEnumerator();
            Assert.IsTrue(it.MoveNext());
            Assert.AreEqual(typeof(ITermAttribute), it.Current, "TermAttribute must be the first attribute");
            Assert.IsTrue(it.MoveNext());
            Assert.AreEqual(typeof(ITypeAttribute), it.Current, "TypeAttribute must be the second attribute");
            Assert.IsFalse(it.MoveNext(), "No more attributes");

            ITermAttribute termAtt2 = clone.GetAttribute<ITermAttribute>();
            ITypeAttribute typeAtt2 = clone.GetAttribute<ITypeAttribute>();
            Assert.IsFalse(ReferenceEquals(termAtt2, termAtt), "TermAttribute of original and clone must be different instances");
            Assert.IsFalse(ReferenceEquals(typeAtt2, typeAtt), "TypeAttribute of original and clone must be different instances");
            Assert.AreEqual(termAtt2, termAtt, "TermAttribute of original and clone must be equal");
            Assert.AreEqual(typeAtt2, typeAtt, "TypeAttribute of original and clone must be equal");
        }

        [Test]
        public virtual void TestToStringAndMultiAttributeImplementations()
        {
            AttributeSource src = new AttributeSource();
            ITermAttribute termAtt = src.AddAttribute<ITermAttribute>();
            ITypeAttribute typeAtt = src.AddAttribute<ITypeAttribute>();
            termAtt.SetTermBuffer("TestTerm");
            typeAtt.Type = "TestType";
            Assert.AreEqual("(" + termAtt.ToString() + "," + typeAtt.ToString() + ")", src.ToString(), "Attributes should appear in original order");
            System.Collections.Generic.IEnumerator<Attribute> it = src.GetAttributeImplsIterator().GetEnumerator();
            Assert.IsTrue(it.MoveNext(), "Iterator should have 2 attributes left");
            Assert.AreSame(termAtt, it.Current, "First AttributeImpl from iterator should be termAtt");
            Assert.IsTrue(it.MoveNext(), "Iterator should have 1 attributes left");
            Assert.AreSame(typeAtt, it.Current, "Second AttributeImpl from iterator should be typeAtt");
            Assert.IsFalse(it.MoveNext(), "Iterator should have 0 attributes left");

            src = new AttributeSource();
            src.AddAttributeImpl(new Token());
            // this should not add a new attribute as Token implements TermAttribute, too
            termAtt = src.AddAttribute<ITermAttribute>();
            Assert.IsTrue(termAtt is Token, "TermAttribute should be implemented by Token");
            // get the Token attribute and check, that it is the only one
            it = src.GetAttributeImplsIterator().GetEnumerator();
            Assert.IsTrue(it.MoveNext());
            Token tok = (Token)it.Current;
            Assert.IsFalse(it.MoveNext(), "There should be only one attribute implementation instance");

            termAtt.SetTermBuffer("TestTerm");
            Assert.AreEqual("(" + tok.ToString() + ")", src.ToString(), "Token should only printed once");
        }

        [Test]
        public void TestDefaultAttributeFactory()
        {
            AttributeSource src = new AttributeSource();

            Assert.IsTrue(src.AddAttribute<ITermAttribute>() is TermAttribute,
                          "TermAttribute is not implemented by TermAttributeImpl");
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

            src = new AttributeSource();
            Assert.Throws<ArgumentException>(() => src.AddAttribute<Token>(), "Should throw ArgumentException");

            //try
            //{
            //    AttributeSource src = new AttributeSource();
            //    src.AddAttribute<System.Collections.IEnumerator>(); //Doesn't compile.
            //    Assert.Fail("Should throw IllegalArgumentException");
            //}
            //catch (ArgumentException iae) { }
        }
    }
    
    
}