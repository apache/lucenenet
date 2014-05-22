using System;
using System.Collections.Generic;
using System.Threading;

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


	public class TestWeakIdentityMap : LuceneTestCase
	{

	  public virtual void TestSimpleHashMap()
	  {
		WeakIdentityMap<string, string> map = WeakIdentityMap.newHashMap(random().nextBoolean());
		// we keep strong references to the keys,
		// so WeakIdentityMap will not forget about them:
		string key1 = new string("foo");
		string key2 = new string("foo");
		string key3 = new string("foo");

		Assert.AreNotSame(key1, key2);
		Assert.AreEqual(key1, key2);
		Assert.AreNotSame(key1, key3);
		Assert.AreEqual(key1, key3);
		Assert.AreNotSame(key2, key3);
		Assert.AreEqual(key2, key3);

		// try null key & check its iterator also return null:
		map.put(null, "null");
		{
		  IEnumerator<string> it = map.keyIterator();
//JAVA TO C# CONVERTER TODO TASK: Java iterators are only converted within the context of 'while' and 'for' loops:
		  Assert.IsTrue(it.hasNext());
//JAVA TO C# CONVERTER TODO TASK: Java iterators are only converted within the context of 'while' and 'for' loops:
		  assertNull(it.next());
//JAVA TO C# CONVERTER TODO TASK: Java iterators are only converted within the context of 'while' and 'for' loops:
		  Assert.IsFalse(it.hasNext());
//JAVA TO C# CONVERTER TODO TASK: Java iterators are only converted within the context of 'while' and 'for' loops:
		  Assert.IsFalse(it.hasNext());
		}
		// 2 more keys:
		map.put(key1, "bar1");
		map.put(key2, "bar2");

		Assert.AreEqual(3, map.size());

		Assert.AreEqual("bar1", map.get(key1));
		Assert.AreEqual("bar2", map.get(key2));
		Assert.AreEqual(null, map.get(key3));
		Assert.AreEqual("null", map.get(null));

		Assert.IsTrue(map.containsKey(key1));
		Assert.IsTrue(map.containsKey(key2));
		Assert.IsFalse(map.containsKey(key3));
		Assert.IsTrue(map.containsKey(null));

		// repeat and check that we have no double entries
		map.put(key1, "bar1");
		map.put(key2, "bar2");
		map.put(null, "null");

		Assert.AreEqual(3, map.size());

		Assert.AreEqual("bar1", map.get(key1));
		Assert.AreEqual("bar2", map.get(key2));
		Assert.AreEqual(null, map.get(key3));
		Assert.AreEqual("null", map.get(null));

		Assert.IsTrue(map.containsKey(key1));
		Assert.IsTrue(map.containsKey(key2));
		Assert.IsFalse(map.containsKey(key3));
		Assert.IsTrue(map.containsKey(null));

		map.remove(null);
		Assert.AreEqual(2, map.size());
		map.remove(key1);
		Assert.AreEqual(1, map.size());
		map.put(key1, "bar1");
		map.put(key2, "bar2");
		map.put(key3, "bar3");
		Assert.AreEqual(3, map.size());

		int c = 0, keysAssigned = 0;
		for (IEnumerator<string> it = map.keyIterator(); it.MoveNext();)
		{
//JAVA TO C# CONVERTER TODO TASK: Java iterators are only converted within the context of 'while' and 'for' loops:
		  Assert.IsTrue(it.hasNext()); // try again, should return same result!
		  string k = it.Current;
		  Assert.IsTrue(k == key1 || k == key2 | k == key3);
		  keysAssigned += (k == key1) ? 1 : ((k == key2) ? 2 : 4);
		  c++;
		}
		Assert.AreEqual(3, c);
		Assert.AreEqual("all keys must have been seen", 1 + 2 + 4, keysAssigned);

		c = 0;
		for (IEnumerator<string> it = map.valueIterator(); it.MoveNext();)
		{
		  string v = it.Current;
		  Assert.IsTrue(v.StartsWith("bar"));
		  c++;
		}
		Assert.AreEqual(3, c);

		// clear strong refs
		key1 = key2 = key3 = null;

		// check that GC does not cause problems in reap() method, wait 1 second and let GC work:
		int size = map.size();
		for (int i = 0; size > 0 && i < 10; i++)
		{
			try
			{
		  System.runFinalization();
		  System.gc();
		  int newSize = map.size();
		  Assert.IsTrue("previousSize(" + size + ")>=newSize(" + newSize + ")", size >= newSize);
		  size = newSize;
		  Thread.Sleep(100L);
		  c = 0;
		  for (IEnumerator<string> it = map.keyIterator(); it.MoveNext();)
		  {
			Assert.IsNotNull(it.Current);
			c++;
		  }
		  newSize = map.size();
		  Assert.IsTrue("previousSize(" + size + ")>=iteratorSize(" + c + ")", size >= c);
		  Assert.IsTrue("iteratorSize(" + c + ")>=newSize(" + newSize + ")", c >= newSize);
		  size = newSize;
			}
		catch (InterruptedException ie)
		{
		}
		}

		map.clear();
		Assert.AreEqual(0, map.size());
		Assert.IsTrue(map.Empty);

		IEnumerator<string> it = map.keyIterator();
//JAVA TO C# CONVERTER TODO TASK: Java iterators are only converted within the context of 'while' and 'for' loops:
		Assert.IsFalse(it.hasNext());
		try
		{
//JAVA TO C# CONVERTER TODO TASK: Java iterators are only converted within the context of 'while' and 'for' loops:
		  it.next();
		  Assert.Fail("Should throw NoSuchElementException");
		}
		catch (NoSuchElementException nse)
		{
		}

		key1 = new string("foo");
		key2 = new string("foo");
		map.put(key1, "bar1");
		map.put(key2, "bar2");
		Assert.AreEqual(2, map.size());

		map.clear();
		Assert.AreEqual(0, map.size());
		Assert.IsTrue(map.Empty);
	  }

	  public virtual void TestConcurrentHashMap()
	  {
		// don't make threadCount and keyCount random, otherwise easily OOMs or fails otherwise:
		const int threadCount = 8, keyCount = 1024;
		ExecutorService exec = Executors.newFixedThreadPool(threadCount, new NamedThreadFactory("testConcurrentHashMap"));
		WeakIdentityMap<object, int?> map = WeakIdentityMap.newConcurrentHashMap(random().nextBoolean());
		// we keep strong references to the keys,
		// so WeakIdentityMap will not forget about them:
		AtomicReferenceArray<object> keys = new AtomicReferenceArray<object>(keyCount);
		for (int j = 0; j < keyCount; j++)
		{
		  keys.set(j, new object());
		}

		try
		{
		  for (int t = 0; t < threadCount; t++)
		  {
			Random rnd = new Random(random().nextLong());
			exec.execute(new RunnableAnonymousInnerClassHelper(this, keyCount, map, keys, rnd));
		  }
		}
		finally
		{
		  exec.shutdown();
		  while (!exec.awaitTermination(1000L, TimeUnit.MILLISECONDS));
		}

		// clear strong refs
		for (int j = 0; j < keyCount; j++)
		{
		  keys.set(j, null);
		}

		// check that GC does not cause problems in reap() method:
		int size = map.size();
		for (int i = 0; size > 0 && i < 10; i++)
		{
			try
			{
		  System.runFinalization();
		  System.gc();
		  int newSize = map.size();
		  Assert.IsTrue("previousSize(" + size + ")>=newSize(" + newSize + ")", size >= newSize);
		  size = newSize;
		  Thread.Sleep(100L);
		  int c = 0;
		  for (IEnumerator<object> it = map.keyIterator(); it.MoveNext();)
		  {
			Assert.IsNotNull(it.Current);
			c++;
		  }
		  newSize = map.size();
		  Assert.IsTrue("previousSize(" + size + ")>=iteratorSize(" + c + ")", size >= c);
		  Assert.IsTrue("iteratorSize(" + c + ")>=newSize(" + newSize + ")", c >= newSize);
		  size = newSize;
			}
		catch (InterruptedException ie)
		{
		}
		}
	  }

	  private class RunnableAnonymousInnerClassHelper : Runnable
	  {
		  private readonly TestWeakIdentityMap OuterInstance;

		  private int KeyCount;
//JAVA TO C# CONVERTER TODO TASK: Java wildcard generics are not converted to .NET:
//ORIGINAL LINE: private WeakIdentityMap<object, int?> map;
		  private WeakIdentityMap<object, int?> Map;
		  private AtomicReferenceArray<object> Keys;
		  private Random Rnd;

		  public RunnableAnonymousInnerClassHelper<T1>(TestWeakIdentityMap outerInstance, int keyCount, WeakIdentityMap<T1> map, AtomicReferenceArray<object> keys, Random rnd)
		  {
			  this.OuterInstance = outerInstance;
			  this.KeyCount = keyCount;
			  this.Map = map;
			  this.Keys = keys;
			  this.Rnd = rnd;
		  }

		  public override void Run()
		  {
			int count = atLeast(Rnd, 10000);
			for (int i = 0; i < count; i++)
			{
			  int j = Rnd.Next(KeyCount);
			  switch (Rnd.Next(5))
			  {
				case 0:
				  Map.put(Keys.get(j), Convert.ToInt32(j));
				  break;
				case 1:
				  int? v = Map.get(Keys.get(j));
				  if (v != null)
				  {
					Assert.AreEqual(j, (int)v);
				  }
				  break;
				case 2:
				  Map.remove(Keys.get(j));
				  break;
				case 3:
				  // renew key, the old one will be GCed at some time:
				  Keys.set(j, new object());
				  break;
				case 4:
				  // check iterator still working
				  for (IEnumerator<object> it = Map.keyIterator(); it.MoveNext();)
				  {
					Assert.IsNotNull(it.Current);
				  }
				  break;
				default:
				  Assert.Fail("Should not get here.");
			  break;
			  }
			}
		  }
	  }

	}

}