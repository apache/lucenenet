namespace Lucene.Net.Util.Automaton
{

	using Util = Lucene.Net.Util.Fst.Util;

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

	public class TestSpecialOperations : LuceneTestCase
	{
	  /// <summary>
	  /// tests against the original brics implementation.
	  /// </summary>
	  public virtual void TestIsFinite()
	  {
		int num = atLeast(200);
		for (int i = 0; i < num; i++)
		{
		  Automaton a = AutomatonTestUtil.randomAutomaton(random());
		  Automaton b = a.clone();
		  Assert.AreEqual(AutomatonTestUtil.isFiniteSlow(a), SpecialOperations.isFinite(b));
		}
	  }

	  /// <summary>
	  /// Basic test for getFiniteStrings
	  /// </summary>
	  public virtual void TestFiniteStrings()
	  {
		Automaton a = BasicOperations.union(BasicAutomata.makeString("dog"), BasicAutomata.makeString("duck"));
		MinimizationOperations.minimize(a);
		Set<IntsRef> strings = SpecialOperations.getFiniteStrings(a, -1);
		Assert.AreEqual(2, strings.size());
		IntsRef dog = new IntsRef();
		Util.toIntsRef(new BytesRef("dog"), dog);
		Assert.IsTrue(strings.contains(dog));
		IntsRef duck = new IntsRef();
		Util.toIntsRef(new BytesRef("duck"), duck);
		Assert.IsTrue(strings.contains(duck));
	  }
	}

}