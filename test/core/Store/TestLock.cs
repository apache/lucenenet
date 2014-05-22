namespace Lucene.Net.Store
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


	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;

	public class TestLock : LuceneTestCase
	{

		public virtual void TestObtain()
		{
			LockMock @lock = new LockMock(this);
			Lock.LOCK_POLL_INTERVAL = 10;

			try
			{
				@lock.obtain(Lock.LOCK_POLL_INTERVAL);
				Assert.Fail("Should have failed to obtain lock");
			}
			catch (IOException e)
			{
				Assert.AreEqual("should attempt to lock more than once", @lock.LockAttempts, 2);
			}
		}

		private class LockMock : Lock
		{
			private readonly TestLock OuterInstance;

			public LockMock(TestLock outerInstance)
			{
				this.OuterInstance = outerInstance;
			}

			public int LockAttempts;

			public override bool Obtain()
			{
				LockAttempts++;
				return false;
			}
			public override void Close()
			{
				// do nothing
			}
			public override bool Locked
			{
				get
				{
					return false;
				}
			}
		}
	}

}