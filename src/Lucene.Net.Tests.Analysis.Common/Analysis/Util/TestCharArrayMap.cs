using System.Collections.Generic;
using System.Text;

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

namespace org.apache.lucene.analysis.util
{

	using LuceneTestCase = org.apache.lucene.util.LuceneTestCase;

	public class TestCharArrayMap : LuceneTestCase
	{
	  public virtual void doRandom(int iter, bool ignoreCase)
	  {
		CharArrayMap<int?> map = new CharArrayMap<int?>(TEST_VERSION_CURRENT, 1, ignoreCase);
		Dictionary<string, int?> hmap = new Dictionary<string, int?>();

		char[] key;
		for (int i = 0; i < iter; i++)
		{
		  int len = random().Next(5);
		  key = new char[len];
		  for (int j = 0; j < key.Length; j++)
		  {
			key[j] = (char)random().Next(127);
		  }
		  string keyStr = new string(key);
		  string hmapKey = ignoreCase ? keyStr.ToLower(Locale.ROOT) : keyStr;

		  int val = random().Next();

		  object o1 = map.put(key, val);
		  object o2 = hmap[hmapKey].Value = val;
		  assertEquals(o1,o2);

		  // add it again with the string method
		  assertEquals(val, map.put(keyStr,val).intValue());

		  assertEquals(val, map.get(key,0,key.Length).intValue());
		  assertEquals(val, map.get(key).intValue());
		  assertEquals(val, map.get(keyStr).intValue());

		  assertEquals(hmap.Count, map.size());
		}
	  }

	  public virtual void testCharArrayMap()
	  {
		int num = 5 * RANDOM_MULTIPLIER;
		for (int i = 0; i < num; i++)
		{ // pump this up for more random testing
		  doRandom(1000,false);
		  doRandom(1000,true);
		}
	  }

	  public virtual void testMethods()
	  {
		CharArrayMap<int?> cm = new CharArrayMap<int?>(TEST_VERSION_CURRENT, 2, false);
		Dictionary<string, int?> hm = new Dictionary<string, int?>();
		hm["foo"] = 1;
		hm["bar"] = 2;
		cm.putAll(hm);
		assertEquals(hm.Count, cm.size());
		hm["baz"] = 3;
		cm.putAll(hm);
		assertEquals(hm.Count, cm.size());

		CharArraySet cs = cm.Keys;
		int n = 0;
		foreach (object o in cs)
		{
		  assertTrue(cm.containsKey(o));
		  char[] co = (char[]) o;
		  assertTrue(cm.containsKey(co, 0, co.Length));
		  n++;
		}
		assertEquals(hm.Count, n);
		assertEquals(hm.Count, cs.size());
		assertEquals(cm.size(), cs.size());
		cs.clear();
		assertEquals(0, cs.size());
		assertEquals(0, cm.size());
		try
		{
		  cs.add("test");
		  fail("keySet() allows adding new keys");
		}
		catch (System.NotSupportedException)
		{
		  // pass
		}
		cm.putAll(hm);
		assertEquals(hm.Count, cs.size());
		assertEquals(cm.size(), cs.size());

		IEnumerator<KeyValuePair<object, int?>> iter1 = cm.entrySet().GetEnumerator();
		n = 0;
		while (iter1.MoveNext())
		{
		  KeyValuePair<object, int?> entry = iter1.Current;
		  object key = entry.Key;
		  int? val = entry.Value;
		  assertEquals(cm.get(key), val);
		  entry.Value = val * 100;
		  assertEquals(val * 100, (int)cm.get(key));
		  n++;
		}
		assertEquals(hm.Count, n);
		cm.clear();
		cm.putAll(hm);
		assertEquals(cm.size(), n);

		CharArrayMap<int?>.EntryIterator iter2 = cm.entrySet().GetEnumerator();
		n = 0;
		while (iter2.hasNext())
		{
		  char[] keyc = iter2.nextKey();
		  int? val = iter2.currentValue();
		  assertEquals(hm[new string(keyc)], val);
		  iter2.Value = val * 100;
		  assertEquals(val * 100, (int)cm.get(keyc));
		  n++;
		}
		assertEquals(hm.Count, n);

		cm.entrySet().clear();
		assertEquals(0, cm.size());
		assertEquals(0, cm.entrySet().size());
		assertTrue(cm.Empty);
	  }

	  public virtual void testModifyOnUnmodifiable()
	  {
		CharArrayMap<int?> map = new CharArrayMap<int?>(TEST_VERSION_CURRENT, 2, false);
		map.put("foo",1);
		map.put("bar",2);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int size = map.size();
		int size = map.size();
		assertEquals(2, size);
		assertTrue(map.containsKey("foo"));
		assertEquals(1, map.get("foo").intValue());
		assertTrue(map.containsKey("bar"));
		assertEquals(2, map.get("bar").intValue());

		map = CharArrayMap.unmodifiableMap(map);
		assertEquals("Map size changed due to unmodifiableMap call", size, map.size());
		string NOT_IN_MAP = "SirGallahad";
		assertFalse("Test String already exists in map", map.containsKey(NOT_IN_MAP));
		assertNull("Test String already exists in map", map.get(NOT_IN_MAP));

		try
		{
		  map.put(NOT_IN_MAP.ToCharArray(), 3);
		  fail("Modified unmodifiable map");
		}
		catch (System.NotSupportedException)
		{
		  // expected
		  assertFalse("Test String has been added to unmodifiable map", map.containsKey(NOT_IN_MAP));
		  assertNull("Test String has been added to unmodifiable map", map.get(NOT_IN_MAP));
		  assertEquals("Size of unmodifiable map has changed", size, map.size());
		}

		try
		{
		  map.put(NOT_IN_MAP, 3);
		  fail("Modified unmodifiable map");
		}
		catch (System.NotSupportedException)
		{
		  // expected
		  assertFalse("Test String has been added to unmodifiable map", map.containsKey(NOT_IN_MAP));
		  assertNull("Test String has been added to unmodifiable map", map.get(NOT_IN_MAP));
		  assertEquals("Size of unmodifiable map has changed", size, map.size());
		}

		try
		{
		  map.put(new StringBuilder(NOT_IN_MAP), 3);
		  fail("Modified unmodifiable map");
		}
		catch (System.NotSupportedException)
		{
		  // expected
		  assertFalse("Test String has been added to unmodifiable map", map.containsKey(NOT_IN_MAP));
		  assertNull("Test String has been added to unmodifiable map", map.get(NOT_IN_MAP));
		  assertEquals("Size of unmodifiable map has changed", size, map.size());
		}

		try
		{
		  map.clear();
		  fail("Modified unmodifiable map");
		}
		catch (System.NotSupportedException)
		{
		  // expected
		  assertEquals("Size of unmodifiable map has changed", size, map.size());
		}

		try
		{
		  map.entrySet().clear();
		  fail("Modified unmodifiable map");
		}
		catch (System.NotSupportedException)
		{
		  // expected
		  assertEquals("Size of unmodifiable map has changed", size, map.size());
		}

		try
		{
		  map.Keys.Clear();
		  fail("Modified unmodifiable map");
		}
		catch (System.NotSupportedException)
		{
		  // expected
		  assertEquals("Size of unmodifiable map has changed", size, map.size());
		}

		try
		{
		  map.put((object) NOT_IN_MAP, 3);
		  fail("Modified unmodifiable map");
		}
		catch (System.NotSupportedException)
		{
		  // expected
		  assertFalse("Test String has been added to unmodifiable map", map.containsKey(NOT_IN_MAP));
		  assertNull("Test String has been added to unmodifiable map", map.get(NOT_IN_MAP));
		  assertEquals("Size of unmodifiable map has changed", size, map.size());
		}

		try
		{
		  map.putAll(Collections.singletonMap(NOT_IN_MAP, 3));
		  fail("Modified unmodifiable map");
		}
		catch (System.NotSupportedException)
		{
		  // expected
		  assertFalse("Test String has been added to unmodifiable map", map.containsKey(NOT_IN_MAP));
		  assertNull("Test String has been added to unmodifiable map", map.get(NOT_IN_MAP));
		  assertEquals("Size of unmodifiable map has changed", size, map.size());
		}

		assertTrue(map.containsKey("foo"));
		assertEquals(1, map.get("foo").intValue());
		assertTrue(map.containsKey("bar"));
		assertEquals(2, map.get("bar").intValue());
	  }

	  public virtual void testToString()
	  {
		CharArrayMap<int?> cm = new CharArrayMap<int?>(TEST_VERSION_CURRENT, Collections.singletonMap("test",1), false);
		assertEquals("[test]",cm.Keys.ToString());
		assertEquals("[1]",cm.values().ToString());
		assertEquals("[test=1]",cm.entrySet().ToString());
		assertEquals("{test=1}",cm.ToString());
		cm.put("test2", 2);
		assertTrue(cm.Keys.ToString().Contains(", "));
		assertTrue(cm.values().ToString().Contains(", "));
		assertTrue(cm.entrySet().ToString().Contains(", "));
		assertTrue(cm.ToString().Contains(", "));
	  }
	}


}