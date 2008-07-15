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

using ConcurrentMergeScheduler = Lucene.Net.Index.ConcurrentMergeScheduler;

namespace Lucene.Net.Util
{
	
	/// <summary>Base class for all Lucene unit tests.  Currently the
	/// only added functionality over JUnit's TestCase is
	/// asserting that no unhandled exceptions occurred in
	/// threads launched by ConcurrentMergeScheduler.  If you
	/// override either <code>setUp()</code> or
	/// <code>tearDown()</code> in your unit test, make sure you
	/// call <code>super.setUp()</code> and
	/// <code>super.tearDown()</code>.
	/// </summary>
	
	[TestFixture]
	public abstract class LuceneTestCase
	{
		
		public LuceneTestCase() : base()
		{
		}
		
		public LuceneTestCase(System.String name) : base()
		{
		}
		
		[SetUp]
		public virtual void  SetUp()
		{
			ConcurrentMergeScheduler.SetTestMode();
		}
		
		[TearDown]
		public virtual void  TearDown()
		{
			if (ConcurrentMergeScheduler.AnyUnhandledExceptions())
			{
				Assert.Fail("ConcurrentMergeScheduler hit unhandled exceptions");
			}
		}
	}
}