using System;
using System.Collections.Generic;
using Lucene.Net.Randomized.Generators;
using NUnit.Framework;

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

	using Lucene.Net.Util;

	using RandomInts = com.carrotsearch.randomizedtesting.generators.RandomInts;

	public class TestBasicOperations : LuceneTestCase
	{
	  /// <summary>
	  /// Test string union. </summary>
	  public virtual void TestStringUnion()
	  {
		IList<BytesRef> strings = new List<BytesRef>();
		for (int i = RandomInts.NextIntBetween(Random(), 0, 1000); --i >= 0;)
		{
		  strings.Add(new BytesRef(TestUtil.RandomUnicodeString(Random())));
		}

		strings.Sort();
		Automaton union = BasicAutomata.makeStringUnion(strings);
		Assert.IsTrue(union.Deterministic);
		Assert.IsTrue(BasicOperations.sameLanguage(union, NaiveUnion(strings)));
	  }

	  private static Automaton NaiveUnion(IList<BytesRef> strings)
	  {
		Automaton[] eachIndividual = new Automaton [strings.Count];
		int i = 0;
		foreach (BytesRef bref in strings)
		{
		  eachIndividual[i++] = BasicAutomata.makeString(bref.Utf8ToString());
		}
		return BasicOperations.union(eachIndividual);
	  }

	  /// <summary>
	  /// Test optimization to concatenate() </summary>
	  public virtual void TestSingletonConcatenate()
	  {
		Automaton singleton = BasicAutomata.makeString("prefix");
		Automaton expandedSingleton = singleton.cloneExpanded();
		Automaton other = BasicAutomata.makeCharRange('5', '7');
		Automaton concat = BasicOperations.concatenate(singleton, other);
		Assert.IsTrue(concat.Deterministic);
		Assert.IsTrue(BasicOperations.sameLanguage(BasicOperations.concatenate(expandedSingleton, other), concat));
	  }

	  /// <summary>
	  /// Test optimization to concatenate() to an NFA </summary>
	  public virtual void TestSingletonNFAConcatenate()
	  {
		Automaton singleton = BasicAutomata.makeString("prefix");
		Automaton expandedSingleton = singleton.cloneExpanded();
		// an NFA (two transitions for 't' from initial state)
		Automaton nfa = BasicOperations.union(BasicAutomata.makeString("this"), BasicAutomata.makeString("three"));
		Automaton concat = BasicOperations.concatenate(singleton, nfa);
		Assert.IsFalse(concat.Deterministic);
		Assert.IsTrue(BasicOperations.sameLanguage(BasicOperations.concatenate(expandedSingleton, nfa), concat));
	  }

	  /// <summary>
	  /// Test optimization to concatenate() with empty String </summary>
	  public virtual void TestEmptySingletonConcatenate()
	  {
		Automaton singleton = BasicAutomata.makeString("");
		Automaton expandedSingleton = singleton.cloneExpanded();
		Automaton other = BasicAutomata.makeCharRange('5', '7');
		Automaton concat1 = BasicOperations.concatenate(expandedSingleton, other);
		Automaton concat2 = BasicOperations.concatenate(singleton, other);
		Assert.IsTrue(concat2.Deterministic);
		Assert.IsTrue(BasicOperations.sameLanguage(concat1, concat2));
		Assert.IsTrue(BasicOperations.sameLanguage(other, concat1));
		Assert.IsTrue(BasicOperations.sameLanguage(other, concat2));
	  }

	  /// <summary>
	  /// Test concatenation with empty language returns empty </summary>
	  public virtual void TestEmptyLanguageConcatenate()
	  {
		Automaton a = BasicAutomata.makeString("a");
		Automaton concat = BasicOperations.concatenate(a, BasicAutomata.makeEmpty());
		Assert.IsTrue(BasicOperations.isEmpty(concat));
	  }

	  /// <summary>
	  /// Test optimization to concatenate() with empty String to an NFA </summary>
	  public virtual void TestEmptySingletonNFAConcatenate()
	  {
		Automaton singleton = BasicAutomata.makeString("");
		Automaton expandedSingleton = singleton.cloneExpanded();
		// an NFA (two transitions for 't' from initial state)
		Automaton nfa = BasicOperations.union(BasicAutomata.makeString("this"), BasicAutomata.makeString("three"));
		Automaton concat1 = BasicOperations.concatenate(expandedSingleton, nfa);
		Automaton concat2 = BasicOperations.concatenate(singleton, nfa);
		Assert.IsFalse(concat2.Deterministic);
		Assert.IsTrue(BasicOperations.sameLanguage(concat1, concat2));
		Assert.IsTrue(BasicOperations.sameLanguage(nfa, concat1));
		Assert.IsTrue(BasicOperations.sameLanguage(nfa, concat2));
	  }

	  /// <summary>
	  /// Test singletons work correctly </summary>
	  public virtual void TestSingleton()
	  {
		Automaton singleton = BasicAutomata.makeString("foobar");
		Automaton expandedSingleton = singleton.cloneExpanded();
		Assert.IsTrue(BasicOperations.sameLanguage(singleton, expandedSingleton));

		singleton = BasicAutomata.makeString("\ud801\udc1c");
		expandedSingleton = singleton.cloneExpanded();
		Assert.IsTrue(BasicOperations.sameLanguage(singleton, expandedSingleton));
	  }

	  public virtual void TestGetRandomAcceptedString()
	  {
		int ITER1 = AtLeast(100);
		int ITER2 = AtLeast(100);
		for (int i = 0;i < ITER1;i++)
		{

		  RegExp re = new RegExp(AutomatonTestUtil.randomRegexp(Random()), RegExp.NONE);
		  Automaton a = re.ToAutomaton();
		  Assert.IsFalse(BasicOperations.isEmpty(a));

		  AutomatonTestUtil.RandomAcceptedStrings rx = new AutomatonTestUtil.RandomAcceptedStrings(a);
		  for (int j = 0;j < ITER2;j++)
		  {
			int[] acc = null;
			try
			{
			  acc = rx.getRandomAcceptedString(Random());
			  string s = UnicodeUtil.newString(acc, 0, acc.Length);
			  Assert.IsTrue(BasicOperations.run(a, s));
			}
			catch (Exception t)
			{
			  Console.WriteLine("regexp: " + re);
			  if (acc != null)
			  {
				Console.WriteLine("fail acc re=" + re + " count=" + acc.Length);
				for (int k = 0;k < acc.Length;k++)
				{
				  Console.WriteLine("  " + acc[k].ToString("x"));
				}
			  }
			  throw t;
			}
		  }
		}
	  }
	}

}