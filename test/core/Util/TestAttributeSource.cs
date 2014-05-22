using System;
using System.Collections;
using System.Collections.Generic;

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
	using Lucene.Net.Analysis.Tokenattributes;

	public class TestAttributeSource : LuceneTestCase
	{

	  public virtual void TestCaptureState()
	  {
		// init a first instance
		AttributeSource src = new AttributeSource();
		CharTermAttribute termAtt = src.addAttribute(typeof(CharTermAttribute));
		TypeAttribute typeAtt = src.addAttribute(typeof(TypeAttribute));
		termAtt.append("TestTerm");
		typeAtt.Type = "TestType";
		int hashCode = src.GetHashCode();

		AttributeSource.State state = src.captureState();

		// modify the attributes
		termAtt.SetEmpty().append("AnotherTestTerm");
		typeAtt.Type = "AnotherTestType";
		Assert.IsTrue("Hash code should be different", hashCode != src.GetHashCode());

		src.restoreState(state);
		Assert.AreEqual("TestTerm", termAtt.ToString());
		Assert.AreEqual("TestType", typeAtt.type());
		Assert.AreEqual("Hash code should be equal after restore", hashCode, src.GetHashCode());

		// restore into an exact configured copy
		AttributeSource copy = new AttributeSource();
		copy.addAttribute(typeof(CharTermAttribute));
		copy.addAttribute(typeof(TypeAttribute));
		copy.restoreState(state);
		Assert.AreEqual("Both AttributeSources should have same hashCode after restore", src.GetHashCode(), copy.GetHashCode());
		Assert.AreEqual("Both AttributeSources should be equal after restore", src, copy);

		// init a second instance (with attributes in different order and one additional attribute)
		AttributeSource src2 = new AttributeSource();
		typeAtt = src2.addAttribute(typeof(TypeAttribute));
		FlagsAttribute flagsAtt = src2.addAttribute(typeof(FlagsAttribute));
		termAtt = src2.addAttribute(typeof(CharTermAttribute));
		flagsAtt.Flags = 12345;

		src2.restoreState(state);
		Assert.AreEqual("TestTerm", termAtt.ToString());
		Assert.AreEqual("TestType", typeAtt.type());
		Assert.AreEqual("FlagsAttribute should not be touched", 12345, flagsAtt.Flags);

		// init a third instance missing one Attribute
		AttributeSource src3 = new AttributeSource();
		termAtt = src3.addAttribute(typeof(CharTermAttribute));
		try
		{
		  src3.restoreState(state);
		  Assert.Fail("The third instance is missing the TypeAttribute, so restoreState() should throw IllegalArgumentException");
		}
		catch (System.ArgumentException iae)
		{
		  // pass
		}
	  }

	  public virtual void TestCloneAttributes()
	  {
		AttributeSource src = new AttributeSource();
		FlagsAttribute flagsAtt = src.addAttribute(typeof(FlagsAttribute));
		TypeAttribute typeAtt = src.addAttribute(typeof(TypeAttribute));
		flagsAtt.Flags = 1234;
		typeAtt.Type = "TestType";

		AttributeSource clone = src.cloneAttributes();
		IEnumerator<Type> it = clone.AttributeClassesIterator;
//JAVA TO C# CONVERTER TODO TASK: Java iterators are only converted within the context of 'while' and 'for' loops:
		Assert.AreEqual("FlagsAttribute must be the first attribute", typeof(FlagsAttribute), it.next());
//JAVA TO C# CONVERTER TODO TASK: Java iterators are only converted within the context of 'while' and 'for' loops:
		Assert.AreEqual("TypeAttribute must be the second attribute", typeof(TypeAttribute), it.next());
//JAVA TO C# CONVERTER TODO TASK: Java iterators are only converted within the context of 'while' and 'for' loops:
		Assert.IsFalse("No more attributes", it.hasNext());

		FlagsAttribute flagsAtt2 = clone.getAttribute(typeof(FlagsAttribute));
		TypeAttribute typeAtt2 = clone.getAttribute(typeof(TypeAttribute));
		Assert.AreNotSame("FlagsAttribute of original and clone must be different instances", flagsAtt2, flagsAtt);
		Assert.AreNotSame("TypeAttribute of original and clone must be different instances", typeAtt2, typeAtt);
		Assert.AreEqual("FlagsAttribute of original and clone must be equal", flagsAtt2, flagsAtt);
		Assert.AreEqual("TypeAttribute of original and clone must be equal", typeAtt2, typeAtt);

		// test copy back
		flagsAtt2.Flags = 4711;
		typeAtt2.Type = "OtherType";
		clone.copyTo(src);
		Assert.AreEqual("FlagsAttribute of original must now contain updated term", 4711, flagsAtt.Flags);
		Assert.AreEqual("TypeAttribute of original must now contain updated type", "OtherType", typeAtt.type());
		// verify again:
		Assert.AreNotSame("FlagsAttribute of original and clone must be different instances", flagsAtt2, flagsAtt);
		Assert.AreNotSame("TypeAttribute of original and clone must be different instances", typeAtt2, typeAtt);
		Assert.AreEqual("FlagsAttribute of original and clone must be equal", flagsAtt2, flagsAtt);
		Assert.AreEqual("TypeAttribute of original and clone must be equal", typeAtt2, typeAtt);
	  }

	  public virtual void TestDefaultAttributeFactory()
	  {
		AttributeSource src = new AttributeSource();

		Assert.IsTrue("CharTermAttribute is not implemented by CharTermAttributeImpl", src.addAttribute(typeof(CharTermAttribute)) is CharTermAttributeImpl);
		Assert.IsTrue("OffsetAttribute is not implemented by OffsetAttributeImpl", src.addAttribute(typeof(OffsetAttribute)) is OffsetAttributeImpl);
		Assert.IsTrue("FlagsAttribute is not implemented by FlagsAttributeImpl", src.addAttribute(typeof(FlagsAttribute)) is FlagsAttributeImpl);
		Assert.IsTrue("PayloadAttribute is not implemented by PayloadAttributeImpl", src.addAttribute(typeof(PayloadAttribute)) is PayloadAttributeImpl);
		Assert.IsTrue("PositionIncrementAttribute is not implemented by PositionIncrementAttributeImpl", src.addAttribute(typeof(PositionIncrementAttribute)) is PositionIncrementAttributeImpl);
		Assert.IsTrue("TypeAttribute is not implemented by TypeAttributeImpl", src.addAttribute(typeof(TypeAttribute)) is TypeAttributeImpl);
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @SuppressWarnings({"rawtypes","unchecked"}) public void testInvalidArguments() throws Exception
	  public virtual void TestInvalidArguments()
	  {
		try
		{
		  AttributeSource src = new AttributeSource();
		  src.addAttribute(typeof(Token));
		  Assert.Fail("Should throw IllegalArgumentException");
		}
		catch (System.ArgumentException iae)
		{
		}

		try
		{
		  AttributeSource src = new AttributeSource(Token.TOKEN_ATTRIBUTE_FACTORY);
		  src.addAttribute(typeof(Token));
		  Assert.Fail("Should throw IllegalArgumentException");
		}
		catch (System.ArgumentException iae)
		{
		}

		try
		{
		  AttributeSource src = new AttributeSource();
		  // break this by unsafe cast
		  src.addAttribute(typeof((Type) IEnumerator));
		  Assert.Fail("Should throw IllegalArgumentException");
		}
		catch (System.ArgumentException iae)
		{
		}
	  }

	  public virtual void TestLUCENE_3042()
	  {
		AttributeSource src1 = new AttributeSource();
		src1.addAttribute(typeof(CharTermAttribute)).append("foo");
		int hash1 = src1.GetHashCode(); // this triggers a cached state
		AttributeSource src2 = new AttributeSource(src1);
		src2.addAttribute(typeof(TypeAttribute)).Type = "bar";
		Assert.IsTrue("The hashCode is identical, so the captured state was preserved.", hash1 != src1.GetHashCode());
		Assert.AreEqual(src2.GetHashCode(), src1.GetHashCode());
	  }
	}

}