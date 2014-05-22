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


	public class TestSimpleAttributeImpl : LuceneTestCase
	{

	  // this checks using reflection API if the defaults are correct
	  public virtual void TestAttributes()
	  {
		TestUtil.assertAttributeReflection(new PositionIncrementAttributeImpl(), Collections.singletonMap(typeof(PositionIncrementAttribute).Name + "#positionIncrement", 1));
		TestUtil.assertAttributeReflection(new PositionLengthAttributeImpl(), Collections.singletonMap(typeof(PositionLengthAttribute).Name + "#positionLength", 1));
		TestUtil.assertAttributeReflection(new FlagsAttributeImpl(), Collections.singletonMap(typeof(FlagsAttribute).Name + "#flags", 0));
		TestUtil.assertAttributeReflection(new TypeAttributeImpl(), Collections.singletonMap(typeof(TypeAttribute).Name + "#type", TypeAttribute.DEFAULT_TYPE));
		TestUtil.assertAttributeReflection(new PayloadAttributeImpl(), Collections.singletonMap(typeof(PayloadAttribute).Name + "#payload", null));
		TestUtil.assertAttributeReflection(new KeywordAttributeImpl(), Collections.singletonMap(typeof(KeywordAttribute).Name + "#keyword", false));
		TestUtil.assertAttributeReflection(new OffsetAttributeImpl(), new Dictionary<string, object>() {{put(typeof(OffsetAttribute).Name + "#startOffset", 0); put(typeof(OffsetAttribute).Name + "#endOffset", 0);}});
	  }

      public static Attribute AssertCloneIsEqual(Attribute att)
      {
          Attribute clone = (Attribute)att.Clone();
          Assert.AreEqual(att, clone, "Clone must be equal");
          Assert.AreEqual(att.GetHashCode(), clone.GetHashCode(), "Clone's hashcode must be equal");
          return clone;
      }

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