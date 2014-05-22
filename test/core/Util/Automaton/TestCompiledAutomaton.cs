using System;
using System.Collections.Generic;

namespace Lucene.Net.Util.Automaton
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



	public class TestCompiledAutomaton : LuceneTestCase
	{

	  private CompiledAutomaton Build(params string[] strings)
	  {
		IList<BytesRef> terms = new List<BytesRef>();
		foreach (string s in strings)
		{
		  terms.Add(new BytesRef(s));
		}
		terms.Sort();
		Automaton a = DaciukMihovAutomatonBuilder.build(terms);
		return new CompiledAutomaton(a, true, false);
	  }

	  private void TestFloor(CompiledAutomaton c, string input, string expected)
	  {
		BytesRef b = new BytesRef(input);
		BytesRef result = c.floor(b, b);
		if (expected == null)
		{
		  assertNull(result);
		}
		else
		{
		  Assert.IsNotNull(result);
		  Assert.AreEqual("actual=" + result.utf8ToString() + " vs expected=" + expected + " (input=" + input + ")", result, new BytesRef(expected));
		}
	  }

	  private void TestTerms(string[] terms)
	  {
		CompiledAutomaton c = Build(terms);
		BytesRef[] termBytes = new BytesRef[terms.Length];
		for (int idx = 0;idx < terms.Length;idx++)
		{
		  termBytes[idx] = new BytesRef(terms[idx]);
		}
		Arrays.sort(termBytes);

		if (VERBOSE)
		{
		  Console.WriteLine("\nTEST: terms in unicode order");
		  foreach (BytesRef t in termBytes)
		  {
			Console.WriteLine("  " + t.utf8ToString());
		  }
		  //System.out.println(c.utf8.toDot());
		}

		for (int iter = 0;iter < 100 * RANDOM_MULTIPLIER;iter++)
		{
		  string s = random().Next(10) == 1 ? terms[random().Next(terms.Length)] : RandomString();
		  if (VERBOSE)
		  {
			Console.WriteLine("\nTEST: floor(" + s + ")");
		  }
		  int loc = Arrays.binarySearch(termBytes, new BytesRef(s));
		  string expected;
		  if (loc >= 0)
		  {
			expected = s;
		  }
		  else
		  {
			// term doesn't exist
			loc = -(loc + 1);
			if (loc == 0)
			{
			  expected = null;
			}
			else
			{
			  expected = termBytes[loc - 1].utf8ToString();
			}
		  }
		  if (VERBOSE)
		  {
			Console.WriteLine("  expected=" + expected);
		  }
		  TestFloor(c, s, expected);
		}
	  }

	  public virtual void TestRandom()
	  {
		int numTerms = atLeast(400);
		Set<string> terms = new HashSet<string>();
		while (terms.size() != numTerms)
		{
		  terms.add(RandomString());
		}
		TestTerms(terms.toArray(new string[terms.size()]));
	  }

	  private string RandomString()
	  {
		// return TestUtil.randomSimpleString(random);
		return TestUtil.randomRealisticUnicodeString(random());
	  }

	  public virtual void TestBasic()
	  {
		CompiledAutomaton c = build("fob", "foo", "goo");
		TestFloor(c, "goo", "goo");
		TestFloor(c, "ga", "foo");
		TestFloor(c, "g", "foo");
		TestFloor(c, "foc", "fob");
		TestFloor(c, "foz", "foo");
		TestFloor(c, "f", null);
		TestFloor(c, "", null);
		TestFloor(c, "aa", null);
		TestFloor(c, "zzz", "goo");
	  }
	}

}