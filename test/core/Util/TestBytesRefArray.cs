using System;
using System.Collections.Generic;

namespace Lucene.Net.Util
{

	/*
	 * Licensed to the Apache Software Foundation (ASF) under one or more
	 * contributor license agreements. See the NOTICE file distributed with this
	 * work for additional information regarding copyright ownership. The ASF
	 * licenses this file to You under the Apache License, Version 2.0 (the
	 * "License"); you may not use this file except in compliance with the License.
	 * You may obtain a copy of the License at
	 * 
	 * http://www.apache.org/licenses/LICENSE-2.0
	 * 
	 * Unless required by applicable law or agreed to in writing, software
	 * distributed under the License is distributed on an "AS IS" BASIS, WITHOUT
	 * WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the
	 * License for the specific language governing permissions and limitations under
	 * the License.
	 */



	public class TestBytesRefArray : LuceneTestCase
	{

	  public virtual void TestAppend()
	  {
		Random random = random();
		BytesRefArray list = new BytesRefArray(Counter.newCounter());
		IList<string> stringList = new List<string>();
		for (int j = 0; j < 2; j++)
		{
		  if (j > 0 && random.nextBoolean())
		  {
			list.clear();
			stringList.Clear();
		  }
		  int entries = atLeast(500);
		  BytesRef spare = new BytesRef();
		  int initSize = list.size();
		  for (int i = 0; i < entries; i++)
		  {
			string randomRealisticUnicodeString = TestUtil.randomRealisticUnicodeString(random);
			spare.copyChars(randomRealisticUnicodeString);
			Assert.AreEqual(i + initSize, list.append(spare));
			stringList.Add(randomRealisticUnicodeString);
		  }
		  for (int i = 0; i < entries; i++)
		  {
			Assert.IsNotNull(list.get(spare, i));
			Assert.AreEqual("entry " + i + " doesn't match", stringList[i], spare.utf8ToString());
		  }

		  // check random
		  for (int i = 0; i < entries; i++)
		  {
			int e = random.Next(entries);
			Assert.IsNotNull(list.get(spare, e));
			Assert.AreEqual("entry " + i + " doesn't match", stringList[e], spare.utf8ToString());
		  }
		  for (int i = 0; i < 2; i++)
		  {

			BytesRefIterator iterator = list.GetEnumerator();
			foreach (string @string in stringList)
			{
			  Assert.AreEqual(@string, iterator.next().utf8ToString());
			}
		  }
		}
	  }

	  public virtual void TestSort()
	  {
		Random random = random();
		BytesRefArray list = new BytesRefArray(Counter.newCounter());
		IList<string> stringList = new List<string>();

		for (int j = 0; j < 2; j++)
		{
		  if (j > 0 && random.nextBoolean())
		  {
			list.clear();
			stringList.Clear();
		  }
		  int entries = atLeast(500);
		  BytesRef spare = new BytesRef();
		  int initSize = list.size();
		  for (int i = 0; i < entries; i++)
		  {
			string randomRealisticUnicodeString = TestUtil.randomRealisticUnicodeString(random);
			spare.copyChars(randomRealisticUnicodeString);
			Assert.AreEqual(initSize + i, list.append(spare));
			stringList.Add(randomRealisticUnicodeString);
		  }

		  stringList.Sort();
		  BytesRefIterator iter = list.iterator(BytesRef.UTF8SortedAsUTF16Comparator);
		  int i = 0;
		  while ((spare = iter.next()) != null)
		  {
			Assert.AreEqual("entry " + i + " doesn't match", stringList[i], spare.utf8ToString());
			i++;
		  }
		  assertNull(iter.next());
		  Assert.AreEqual(i, stringList.Count);
		}

	  }

	}

}