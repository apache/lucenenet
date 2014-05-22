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

	public class TestBytesRef : LuceneTestCase
	{
	  public virtual void TestEmpty()
	  {
		BytesRef b = new BytesRef();
		Assert.AreEqual(BytesRef.EMPTY_BYTES, b.bytes);
		Assert.AreEqual(0, b.offset);
		Assert.AreEqual(0, b.length);
	  }

	  public virtual void TestFromBytes()
	  {
		sbyte[] bytes = new sbyte[] {(sbyte)'a', (sbyte)'b', (sbyte)'c', (sbyte)'d'};
		BytesRef b = new BytesRef(bytes);
		Assert.AreEqual(bytes, b.bytes);
		Assert.AreEqual(0, b.offset);
		Assert.AreEqual(4, b.length);

		BytesRef b2 = new BytesRef(bytes, 1, 3);
		Assert.AreEqual("bcd", b2.utf8ToString());

		Assert.IsFalse(b.Equals(b2));
	  }

	  public virtual void TestFromChars()
	  {
		for (int i = 0; i < 100; i++)
		{
		  string s = TestUtil.randomUnicodeString(random());
		  string s2 = (new BytesRef(s)).utf8ToString();
		  Assert.AreEqual(s, s2);
		}

		// only for 4.x
		Assert.AreEqual("\uFFFF", (new BytesRef("\uFFFF")).utf8ToString());
	  }

	  // LUCENE-3590, AIOOBE if you append to a bytesref with offset != 0
	  public virtual void TestAppend()
	  {
		sbyte[] bytes = new sbyte[] {(sbyte)'a', (sbyte)'b', (sbyte)'c', (sbyte)'d'};
		BytesRef b = new BytesRef(bytes, 1, 3); // bcd
		b.append(new BytesRef("e"));
		Assert.AreEqual("bcde", b.utf8ToString());
	  }

	  // LUCENE-3590, AIOOBE if you copy to a bytesref with offset != 0
	  public virtual void TestCopyBytes()
	  {
		sbyte[] bytes = new sbyte[] {(sbyte)'a', (sbyte)'b', (sbyte)'c', (sbyte)'d'};
		BytesRef b = new BytesRef(bytes, 1, 3); // bcd
		b.copyBytes(new BytesRef("bcde"));
		Assert.AreEqual("bcde", b.utf8ToString());
	  }
	}

}