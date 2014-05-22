using System.Collections.Generic;

namespace Lucene.Net.Index
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
	using MergedIterator = Lucene.Net.Util.MergedIterator;
	using TestUtil = Lucene.Net.Util.TestUtil;

	public class TestPrefixCodedTerms : LuceneTestCase
	{

	  public virtual void TestEmpty()
	  {
		PrefixCodedTerms.Builder b = new PrefixCodedTerms.Builder();
		PrefixCodedTerms pb = b.finish();
		Assert.IsFalse(pb.GetEnumerator().hasNext());
	  }

	  public virtual void TestOne()
	  {
		Term term = new Term("foo", "bogus");
		PrefixCodedTerms.Builder b = new PrefixCodedTerms.Builder();
		b.add(term);
		PrefixCodedTerms pb = b.finish();
		IEnumerator<Term> iterator = pb.GetEnumerator();
//JAVA TO C# CONVERTER TODO TASK: Java iterators are only converted within the context of 'while' and 'for' loops:
		Assert.IsTrue(iterator.hasNext());
//JAVA TO C# CONVERTER TODO TASK: Java iterators are only converted within the context of 'while' and 'for' loops:
		Assert.AreEqual(term, iterator.next());
	  }

	  public virtual void TestRandom()
	  {
		Set<Term> terms = new SortedSet<Term>();
		int nterms = atLeast(10000);
		for (int i = 0; i < nterms; i++)
		{
		  Term term = new Term(TestUtil.randomUnicodeString(random(), 2), TestUtil.randomUnicodeString(random()));
		  terms.add(term);
		}

		PrefixCodedTerms.Builder b = new PrefixCodedTerms.Builder();
		foreach (Term @ref in terms)
		{
		  b.add(@ref);
		}
		PrefixCodedTerms pb = b.finish();

		IEnumerator<Term> expected = terms.GetEnumerator();
		foreach (Term t in pb)
		{
//JAVA TO C# CONVERTER TODO TASK: Java iterators are only converted within the context of 'while' and 'for' loops:
		  Assert.IsTrue(expected.hasNext());
//JAVA TO C# CONVERTER TODO TASK: Java iterators are only converted within the context of 'while' and 'for' loops:
		  Assert.AreEqual(expected.next(), t);
		}
//JAVA TO C# CONVERTER TODO TASK: Java iterators are only converted within the context of 'while' and 'for' loops:
		Assert.IsFalse(expected.hasNext());
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @SuppressWarnings("unchecked") public void testMergeOne()
	  public virtual void TestMergeOne()
	  {
		Term t1 = new Term("foo", "a");
		PrefixCodedTerms.Builder b1 = new PrefixCodedTerms.Builder();
		b1.add(t1);
		PrefixCodedTerms pb1 = b1.finish();

		Term t2 = new Term("foo", "b");
		PrefixCodedTerms.Builder b2 = new PrefixCodedTerms.Builder();
		b2.add(t2);
		PrefixCodedTerms pb2 = b2.finish();

		IEnumerator<Term> merged = new MergedIterator<Term>(pb1.GetEnumerator(), pb2.GetEnumerator());
//JAVA TO C# CONVERTER TODO TASK: Java iterators are only converted within the context of 'while' and 'for' loops:
		Assert.IsTrue(merged.hasNext());
//JAVA TO C# CONVERTER TODO TASK: Java iterators are only converted within the context of 'while' and 'for' loops:
		Assert.AreEqual(t1, merged.next());
//JAVA TO C# CONVERTER TODO TASK: Java iterators are only converted within the context of 'while' and 'for' loops:
		Assert.IsTrue(merged.hasNext());
//JAVA TO C# CONVERTER TODO TASK: Java iterators are only converted within the context of 'while' and 'for' loops:
		Assert.AreEqual(t2, merged.next());
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @SuppressWarnings({"unchecked","rawtypes"}) public void testMergeRandom()
	  public virtual void TestMergeRandom()
	  {
		PrefixCodedTerms[] pb = new PrefixCodedTerms[TestUtil.Next(random(), 2, 10)];
		Set<Term> superSet = new SortedSet<Term>();

		for (int i = 0; i < pb.Length; i++)
		{
		  Set<Term> terms = new SortedSet<Term>();
		  int nterms = TestUtil.Next(random(), 0, 10000);
		  for (int j = 0; j < nterms; j++)
		  {
			Term term = new Term(TestUtil.randomUnicodeString(random(), 2), TestUtil.randomUnicodeString(random(), 4));
			terms.add(term);
		  }
		  superSet.addAll(terms);

		  PrefixCodedTerms.Builder b = new PrefixCodedTerms.Builder();
		  foreach (Term @ref in terms)
		  {
			b.add(@ref);
		  }
		  pb[i] = b.finish();
		}

		IList<IEnumerator<Term>> subs = new List<IEnumerator<Term>>();
		for (int i = 0; i < pb.Length; i++)
		{
		  subs.Add(pb[i].GetEnumerator());
		}

		IEnumerator<Term> expected = superSet.GetEnumerator();
		IEnumerator<Term> actual = new MergedIterator<Term>(subs.ToArray());
		while (actual.MoveNext())
		{
//JAVA TO C# CONVERTER TODO TASK: Java iterators are only converted within the context of 'while' and 'for' loops:
		  Assert.IsTrue(expected.hasNext());
//JAVA TO C# CONVERTER TODO TASK: Java iterators are only converted within the context of 'while' and 'for' loops:
		  Assert.AreEqual(expected.next(), actual.Current);
		}
//JAVA TO C# CONVERTER TODO TASK: Java iterators are only converted within the context of 'while' and 'for' loops:
		Assert.IsFalse(expected.hasNext());
	  }
	}

}