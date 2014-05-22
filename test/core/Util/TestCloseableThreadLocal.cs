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

namespace Lucene.Net.Util
{

	public class TestIDisposableThreadLocal : LuceneTestCase
	{
	  public const string TEST_VALUE = "initvaluetest";

	  public virtual void TestInitValue()
	  {
		InitValueThreadLocal tl = new InitValueThreadLocal(this);
		string str = (string)tl.get();
		Assert.AreEqual(TEST_VALUE, str);
	  }

	  public virtual void TestNullValue()
	  {
		// Tests that null can be set as a valid value (LUCENE-1805). this
		// previously failed in get().
		IDisposableThreadLocal<object> ctl = new IDisposableThreadLocal<object>();
		ctl.set(null);
		assertNull(ctl.get());
	  }

	  public virtual void TestDefaultValueWithoutSetting()
	  {
		// LUCENE-1805: make sure default get returns null,
		// twice in a row
		IDisposableThreadLocal<object> ctl = new IDisposableThreadLocal<object>();
		assertNull(ctl.get());
	  }

	  public class InitValueThreadLocal : IDisposableThreadLocal<object>
	  {
		  private readonly TestIDisposableThreadLocal OuterInstance;

		  public InitValueThreadLocal(TestIDisposableThreadLocal outerInstance)
		  {
			  this.OuterInstance = outerInstance;
		  }

		protected internal override object InitialValue()
		{
		  return TEST_VALUE;
		}
	  }
	}

}