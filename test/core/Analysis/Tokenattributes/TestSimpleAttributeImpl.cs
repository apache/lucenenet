using System;
using System.Collections.Generic;

namespace Lucene.Net.Analysis.Tokenattributes
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

	using TestUtil = Lucene.Net.Util.TestUtil;
	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using NUnit.Framework;
    using Attribute = Lucene.Net.Util.Attribute;
    using Lucene.Net.Support;

    [TestFixture]
	public class TestSimpleAttributeImpl : LuceneTestCase
	{

	  // this checks using reflection API if the defaults are correct
	  public virtual void TestAttributes()
	  {
		TestUtil.AssertAttributeReflection(new PositionIncrementAttribute(), CollectionsHelper.SingletonMap(typeof(PositionIncrementAttribute).Name + "#positionIncrement", 1));
        TestUtil.AssertAttributeReflection(new PositionLengthAttribute(), CollectionsHelper.SingletonMap(typeof(PositionLengthAttribute).Name + "#positionLength", 1));
        TestUtil.AssertAttributeReflection(new FlagsAttribute(), CollectionsHelper.SingletonMap(typeof(FlagsAttribute).Name + "#flags", 0));
        TestUtil.AssertAttributeReflection(new TypeAttribute(), CollectionsHelper.SingletonMap(typeof(TypeAttribute).Name + "#type", TypeAttribute_Fields.DEFAULT_TYPE));
        TestUtil.AssertAttributeReflection(new PayloadAttribute(), CollectionsHelper.SingletonMap(typeof(PayloadAttribute).Name + "#payload", (object)null));
        TestUtil.AssertAttributeReflection(new KeywordAttribute(), CollectionsHelper.SingletonMap(typeof(KeywordAttribute).Name + "#keyword", false));
		TestUtil.AssertAttributeReflection(new OffsetAttribute(), new Dictionary<string, object>() 
        {
            {typeof(OffsetAttribute).Name + "#startOffset", 0 },
            {typeof(OffsetAttribute).Name + "#endOffset", 0}
        });
	  }

      [Test]
      public static Attribute AssertCloneIsEqual(Attribute att)
      {
          Attribute clone = (Attribute)att.Clone();
          Assert.AreEqual(att, clone, "Clone must be equal");
          Assert.AreEqual(att.GetHashCode(), clone.GetHashCode(), "Clone's hashcode must be equal");
          return clone;
      }

      [Test]
      public static Attribute AssertCopyIsEqual(Attribute att)
      {
          Attribute copy = (Attribute)System.Activator.CreateInstance(att.GetType());
          att.CopyTo(copy);
          Assert.AreEqual(att, copy, "Copied instance must be equal");
          Assert.AreEqual(att.GetHashCode(), copy.GetHashCode(), "Copied instance's hashcode must be equal");
          return copy;
      }

	}

}