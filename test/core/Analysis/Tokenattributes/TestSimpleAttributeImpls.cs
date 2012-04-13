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
using Attribute = Lucene.Net.Util.Attribute;
using Payload = Lucene.Net.Index.Payload;
using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;

namespace Lucene.Net.Analysis.Tokenattributes
{
	
    [TestFixture]
	public class TestSimpleAttributeImpls:LuceneTestCase
	{
		
		public TestSimpleAttributeImpls():base("")
		{
		}
        
        [Test]
		public virtual void  TestFlagsAttribute()
		{
			FlagsAttribute att = new FlagsAttribute();
			Assert.AreEqual(0, att.Flags);
			
			att.Flags = 1234;
			Assert.AreEqual("flags=1234", att.ToString());
			
			FlagsAttribute att2 = (FlagsAttribute) AssertCloneIsEqual(att);
			Assert.AreEqual(1234, att2.Flags);
			
			att2 = (FlagsAttribute) AssertCopyIsEqual(att);
			Assert.AreEqual(1234, att2.Flags);
			
			att.Clear();
			Assert.AreEqual(0, att.Flags);
		}
		
        [Test]
		public virtual void  TestPositionIncrementAttribute()
		{
			PositionIncrementAttribute att = new PositionIncrementAttribute();
			Assert.AreEqual(1, att.PositionIncrement);
			
			att.PositionIncrement = 1234;
			Assert.AreEqual("positionIncrement=1234", att.ToString());
			
			PositionIncrementAttribute att2 = (PositionIncrementAttribute) AssertCloneIsEqual(att);
			Assert.AreEqual(1234, att2.PositionIncrement);
			
			att2 = (PositionIncrementAttribute) AssertCopyIsEqual(att);
			Assert.AreEqual(1234, att2.PositionIncrement);
			
			att.Clear();
			Assert.AreEqual(1, att.PositionIncrement);
		}
		
        [Test]
		public virtual void  TestTypeAttribute()
		{
			TypeAttribute att = new TypeAttribute();
			Assert.AreEqual(TypeAttribute.DEFAULT_TYPE, att.Type);
			
			att.Type = "hallo";
			Assert.AreEqual("type=hallo", att.ToString());
			
			TypeAttribute att2 = (TypeAttribute) AssertCloneIsEqual(att);
			Assert.AreEqual("hallo", att2.Type);
			
			att2 = (TypeAttribute) AssertCopyIsEqual(att);
			Assert.AreEqual("hallo", att2.Type);
			
			att.Clear();
			Assert.AreEqual(TypeAttribute.DEFAULT_TYPE, att.Type);
		}
		
        [Test]
		public virtual void  TestPayloadAttribute()
		{
			PayloadAttribute att = new PayloadAttribute();
			Assert.IsNull(att.Payload);
			
			Payload pl = new Payload(new byte[]{1, 2, 3, 4});
			att.Payload = pl;
			
			PayloadAttribute att2 = (PayloadAttribute) AssertCloneIsEqual(att);
			Assert.AreEqual(pl, att2.Payload);
			Assert.AreNotSame(pl, att2.Payload);
			
			att2 = (PayloadAttribute) AssertCopyIsEqual(att);
			Assert.AreEqual(pl, att2.Payload);
            Assert.AreNotSame(pl, att2.Payload);
			
			att.Clear();
			Assert.IsNull(att.Payload);
		}
		
        [Test]
		public virtual void  TestOffsetAttribute()
		{
			OffsetAttribute att = new OffsetAttribute();
			Assert.AreEqual(0, att.StartOffset);
			Assert.AreEqual(0, att.EndOffset);
			
			att.SetOffset(12, 34);
			// no string test here, because order unknown
			
			OffsetAttribute att2 = (OffsetAttribute) AssertCloneIsEqual(att);
			Assert.AreEqual(12, att2.StartOffset);
			Assert.AreEqual(34, att2.EndOffset);
			
			att2 = (OffsetAttribute) AssertCopyIsEqual(att);
			Assert.AreEqual(12, att2.StartOffset);
			Assert.AreEqual(34, att2.EndOffset);
			
			att.Clear();
			Assert.AreEqual(0, att.StartOffset);
			Assert.AreEqual(0, att.EndOffset);
		}
		
		public static Attribute AssertCloneIsEqual(Attribute att)
		{
			Attribute clone = (Attribute) att.Clone();
			Assert.AreEqual(att, clone, "Clone must be equal");
			Assert.AreEqual(att.GetHashCode(), clone.GetHashCode(), "Clone's hashcode must be equal");
			return clone;
		}
		
		public static Attribute AssertCopyIsEqual(Attribute att)
		{
			Attribute copy = (Attribute) System.Activator.CreateInstance(att.GetType());
			att.CopyTo(copy);
			Assert.AreEqual(att, copy, "Copied instance must be equal");
			Assert.AreEqual(att.GetHashCode(), copy.GetHashCode(), "Copied instance's hashcode must be equal");
			return copy;
		}
	}
}