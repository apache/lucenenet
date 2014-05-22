using System;
using System.Collections.Generic;

namespace Lucene.Net.Codecs.Lucene3x
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

	using Lucene.Net.Store;
	using Lucene.Net.Document;
	using Lucene.Net.Analysis;
	using Lucene.Net.Index;
	using Lucene.Net.Util;


	using BeforeClass = org.junit.BeforeClass;
	using Test = org.junit.Test;

	public class TestSurrogates : LuceneTestCase
	{
	  /// <summary>
	  /// we will manually instantiate preflex-rw here </summary>
//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @BeforeClass public static void beforeClass()
	  public static void BeforeClass()
	  {
		LuceneTestCase.OLD_FORMAT_IMPERSONATION_IS_ACTIVE = true;
	  }

	  private static string MakeDifficultRandomUnicodeString(Random r)
	  {
		int end = r.Next(20);
		if (end == 0)
		{
		  // allow 0 length
		  return "";
		}
		char[] buffer = new char[end];
		for (int i = 0; i < end; i++)
		{
		  int t = r.Next(5);

		  if (0 == t && i < end - 1)
		  {
			// hi
			buffer[i++] = (char)(0xd800 + r.Next(2));
			// lo
			buffer[i] = (char)(0xdc00 + r.Next(2));
		  }
		  else if (t <= 3)
		  {
			buffer[i] = (char)('a' + r.Next(2));
		  }
		  else if (4 == t)
		  {
			buffer[i] = (char)(0xe000 + r.Next(2));
		  }
		}

		return new string(buffer, 0, end);
	  }

	  private string ToHexString(Term t)
	  {
		return t.field() + ":" + UnicodeUtil.toHexString(t.text());
	  }

	  private string GetRandomString(Random r)
	  {
		string s;
		if (r.Next(5) == 1)
		{
		  if (r.Next(3) == 1)
		  {
			s = MakeDifficultRandomUnicodeString(r);
		  }
		  else
		  {
			s = TestUtil.randomUnicodeString(r);
		  }
		}
		else
		{
		  s = TestUtil.randomRealisticUnicodeString(r);
		}
		return s;
	  }

	  private class SortTermAsUTF16Comparator : IComparer<Term>
	  {
		internal static readonly IComparer<BytesRef> LegacyComparator = BytesRef.UTF8SortedAsUTF16Comparator;

		public virtual int Compare(Term term1, Term term2)
		{
		  if (term1.field().Equals(term2.field()))
		  {
			return LegacyComparator.Compare(term1.bytes(), term2.bytes());
		  }
		  else
		  {
			return term1.field().compareTo(term2.field());
		  }
		}
	  }

	  private static readonly SortTermAsUTF16Comparator TermAsUTF16Comparator = new SortTermAsUTF16Comparator();

	  // single straight enum
	  private void DoTestStraightEnum(IList<Term> fieldTerms, IndexReader reader, int uniqueTermCount)
	  {

		if (VERBOSE)
		{
		  Console.WriteLine("\nTEST: top now enum reader=" + reader);
		}
		Fields fields = MultiFields.getFields(reader);

		{
		  // Test straight enum:
		  int termCount = 0;
		  foreach (string field in fields)
		  {
			Terms terms = fields.terms(field);
			Assert.IsNotNull(terms);
			TermsEnum termsEnum = terms.iterator(null);
			BytesRef text;
			BytesRef lastText = null;
			while ((text = termsEnum.next()) != null)
			{
			  Term exp = fieldTerms[termCount];
			  if (VERBOSE)
			  {
				Console.WriteLine("  got term=" + field + ":" + UnicodeUtil.toHexString(text.utf8ToString()));
				Console.WriteLine("       exp=" + exp.field() + ":" + UnicodeUtil.toHexString(exp.text().ToString()));
				Console.WriteLine();
			  }
			  if (lastText == null)
			  {
				lastText = BytesRef.deepCopyOf(text);
			  }
			  else
			  {
				Assert.IsTrue(lastText.compareTo(text) < 0);
				lastText.copyBytes(text);
			  }
			  Assert.AreEqual(exp.field(), field);
			  Assert.AreEqual(exp.bytes(), text);
			  termCount++;
			}
			if (VERBOSE)
			{
			  Console.WriteLine("  no more terms for field=" + field);
			}
		  }
		  Assert.AreEqual(uniqueTermCount, termCount);
		}
	  }

	  // randomly seeks to term that we know exists, then next's
	  // from there
	  private void DoTestSeekExists(Random r, IList<Term> fieldTerms, IndexReader reader)
	  {

		IDictionary<string, TermsEnum> tes = new Dictionary<string, TermsEnum>();

		// Test random seek to existing term, then enum:
		if (VERBOSE)
		{
		  Console.WriteLine("\nTEST: top now seek");
		}

		int num = atLeast(100);
		for (int iter = 0; iter < num; iter++)
		{

		  // pick random field+term
		  int spot = r.Next(fieldTerms.Count);
		  Term term = fieldTerms[spot];
		  string field = term.field();

		  if (VERBOSE)
		  {
			Console.WriteLine("TEST: exist seek field=" + field + " term=" + UnicodeUtil.toHexString(term.text()));
		  }

		  // seek to it
		  TermsEnum te = tes[field];
		  if (te == null)
		  {
			te = MultiFields.getTerms(reader, field).iterator(null);
			tes[field] = te;
		  }

		  if (VERBOSE)
		  {
			Console.WriteLine("  done get enum");
		  }

		  // seek should find the term
		  Assert.AreEqual(TermsEnum.SeekStatus.FOUND, te.seekCeil(term.bytes()));

		  // now .next() this many times:
		  int ct = TestUtil.Next(r, 5, 100);
		  for (int i = 0;i < ct;i++)
		  {
			if (VERBOSE)
			{
			  Console.WriteLine("TEST: now next()");
			}
			if (1 + spot + i >= fieldTerms.Count)
			{
			  break;
			}
			term = fieldTerms[1 + spot + i];
			if (!term.field().Equals(field))
			{
			  assertNull(te.next());
			  break;
			}
			else
			{
			  BytesRef t = te.next();

			  if (VERBOSE)
			  {
				Console.WriteLine("  got term=" + (t == null ? null : UnicodeUtil.toHexString(t.utf8ToString())));
				Console.WriteLine("       exp=" + UnicodeUtil.toHexString(term.text().ToString()));
			  }

			  Assert.AreEqual(term.bytes(), t);
			}
		  }
		}
	  }

	  private void DoTestSeekDoesNotExist(Random r, int numField, IList<Term> fieldTerms, Term[] fieldTermsArray, IndexReader reader)
	  {

		IDictionary<string, TermsEnum> tes = new Dictionary<string, TermsEnum>();

		if (VERBOSE)
		{
		  Console.WriteLine("TEST: top random seeks");
		}

		{
		  int num = atLeast(100);
		  for (int iter = 0; iter < num; iter++)
		  {

			// seek to random spot
			string field = ("f" + r.Next(numField)).intern();
			Term tx = new Term(field, GetRandomString(r));

			int spot = Array.BinarySearch(fieldTermsArray, tx);

			if (spot < 0)
			{
			  if (VERBOSE)
			  {
				Console.WriteLine("TEST: non-exist seek to " + field + ":" + UnicodeUtil.toHexString(tx.text()));
			  }

			  // term does not exist:
			  TermsEnum te = tes[field];
			  if (te == null)
			  {
				te = MultiFields.getTerms(reader, field).iterator(null);
				tes[field] = te;
			  }

			  if (VERBOSE)
			  {
				Console.WriteLine("  got enum");
			  }

			  spot = -spot - 1;

			  if (spot == fieldTerms.Count || !fieldTerms[spot].field().Equals(field))
			  {
				Assert.AreEqual(TermsEnum.SeekStatus.END, te.seekCeil(tx.bytes()));
			  }
			  else
			  {
				Assert.AreEqual(TermsEnum.SeekStatus.NOT_FOUND, te.seekCeil(tx.bytes()));

				if (VERBOSE)
				{
				  Console.WriteLine("  got term=" + UnicodeUtil.toHexString(te.term().utf8ToString()));
				  Console.WriteLine("  exp term=" + UnicodeUtil.toHexString(fieldTerms[spot].text()));
				}

				Assert.AreEqual(fieldTerms[spot].bytes(), te.term());

				// now .next() this many times:
				int ct = TestUtil.Next(r, 5, 100);
				for (int i = 0;i < ct;i++)
				{
				  if (VERBOSE)
				  {
					Console.WriteLine("TEST: now next()");
				  }
				  if (1 + spot + i >= fieldTerms.Count)
				  {
					break;
				  }
				  Term term = fieldTerms[1 + spot + i];
				  if (!term.field().Equals(field))
				  {
					assertNull(te.next());
					break;
				  }
				  else
				  {
					BytesRef t = te.next();

					if (VERBOSE)
					{
					  Console.WriteLine("  got term=" + (t == null ? null : UnicodeUtil.toHexString(t.utf8ToString())));
					  Console.WriteLine("       exp=" + UnicodeUtil.toHexString(term.text().ToString()));
					}

					Assert.AreEqual(term.bytes(), t);
				  }
				}

			  }
			}
		  }
		}
	  }


//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testSurrogatesOrder() throws Exception
	  public virtual void TestSurrogatesOrder()
	  {
		Directory dir = newDirectory();
		RandomIndexWriter w = new RandomIndexWriter(random(), dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setCodec(new PreFlexRWCodec()));

		int numField = TestUtil.Next(random(), 2, 5);

		int uniqueTermCount = 0;

		int tc = 0;

		IList<Term> fieldTerms = new List<Term>();

		for (int f = 0;f < numField;f++)
		{
		  string field = "f" + f;
		  int numTerms = atLeast(200);

		  Set<string> uniqueTerms = new HashSet<string>();

		  for (int i = 0;i < numTerms;i++)
		  {
			string term = GetRandomString(random()) + "_ " + (tc++);
			uniqueTerms.add(term);
			fieldTerms.Add(new Term(field, term));
			Document doc = new Document();
			doc.add(newStringField(field, term, Field.Store.NO));
			w.addDocument(doc);
		  }
		  uniqueTermCount += uniqueTerms.size();
		}

		IndexReader reader = w.Reader;

		if (VERBOSE)
		{
		  fieldTerms.Sort(TermAsUTF16Comparator);

		  Console.WriteLine("\nTEST: UTF16 order");
		  foreach (Term t in fieldTerms)
		  {
			Console.WriteLine("  " + ToHexString(t));
		  }
		}

		// sorts in code point order:
		fieldTerms.Sort();

		if (VERBOSE)
		{
		  Console.WriteLine("\nTEST: codepoint order");
		  foreach (Term t in fieldTerms)
		  {
			Console.WriteLine("  " + ToHexString(t));
		  }
		}

		Term[] fieldTermsArray = fieldTerms.ToArray();

		//SegmentInfo si = makePreFlexSegment(r, "_0", dir, fieldInfos, codec, fieldTerms);

		//FieldsProducer fields = codec.fieldsProducer(new SegmentReadState(dir, si, fieldInfos, 1024, 1));
		//Assert.IsNotNull(fields);

		DoTestStraightEnum(fieldTerms, reader, uniqueTermCount);
		DoTestSeekExists(random(), fieldTerms, reader);
		DoTestSeekDoesNotExist(random(), numField, fieldTerms, fieldTermsArray, reader);

		reader.close();
		w.close();
		dir.close();
	  }
	}

}