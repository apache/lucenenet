using System;
using System.Collections.Generic;

namespace org.apache.lucene.misc
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

	using DirectoryReader = org.apache.lucene.index.DirectoryReader;
	using IndexReader = org.apache.lucene.index.IndexReader;
	using MultiFields = org.apache.lucene.index.MultiFields;
	using Fields = org.apache.lucene.index.Fields;
	using TermsEnum = org.apache.lucene.index.TermsEnum;
	using Terms = org.apache.lucene.index.Terms;
	using Directory = org.apache.lucene.store.Directory;
	using FSDirectory = org.apache.lucene.store.FSDirectory;
	using PriorityQueue = org.apache.lucene.util.PriorityQueue;
	using BytesRef = org.apache.lucene.util.BytesRef;


	/// <summary>
	/// <code>HighFreqTerms</code> class extracts the top n most frequent terms
	/// (by document frequency) from an existing Lucene index and reports their
	/// document frequency.
	/// <para>
	/// If the -t flag is given, both document frequency and total tf (total
	/// number of occurrences) are reported, ordered by descending total tf.
	/// 
	/// </para>
	/// </summary>
	public class HighFreqTerms
	{

	  // The top numTerms will be displayed
	  public const int DEFAULT_NUMTERMS = 100;

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public static void main(String[] args) throws Exception
	  public static void Main(string[] args)
	  {
		string field = null;
		int numTerms = DEFAULT_NUMTERMS;

		if (args.Length == 0 || args.Length > 4)
		{
		  usage();
		  Environment.Exit(1);
		}

		Directory dir = FSDirectory.open(new File(args[0]));

		IComparer<TermStats> comparator = new DocFreqComparator();

		for (int i = 1; i < args.Length; i++)
		{
		  if (args[i].Equals("-t"))
		  {
			comparator = new TotalTermFreqComparator();
		  }
		  else
		  {
			try
			{
			  numTerms = Convert.ToInt32(args[i]);
			}
			catch (NumberFormatException)
			{
			  field = args[i];
			}
		  }
		}

		IndexReader reader = DirectoryReader.open(dir);
		TermStats[] terms = getHighFreqTerms(reader, numTerms, field, comparator);

		for (int i = 0; i < terms.Length; i++)
		{
		  System.out.printf(Locale.ROOT, "%s:%s \t totalTF = %,d \t docFreq = %,d \n", terms[i].field, terms[i].termtext.utf8ToString(), terms[i].totalTermFreq, terms[i].docFreq);
		}
		reader.close();
	  }

	  private static void usage()
	  {
		Console.WriteLine("\n\n" + "java org.apache.lucene.misc.HighFreqTerms <index dir> [-t] [number_terms] [field]\n\t -t: order by totalTermFreq\n\n");
	  }

	  /// <summary>
	  /// Returns TermStats[] ordered by the specified comparator
	  /// </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public static TermStats[] getHighFreqTerms(org.apache.lucene.index.IndexReader reader, int numTerms, String field, java.util.Comparator<TermStats> comparator) throws Exception
	  public static TermStats[] getHighFreqTerms(IndexReader reader, int numTerms, string field, IComparer<TermStats> comparator)
	  {
		TermStatsQueue tiq = null;

		if (field != null)
		{
		  Fields fields = MultiFields.getFields(reader);
		  if (fields == null)
		  {
			throw new Exception("field " + field + " not found");
		  }
		  Terms terms = fields.terms(field);
		  if (terms != null)
		  {
			TermsEnum termsEnum = terms.iterator(null);
			tiq = new TermStatsQueue(numTerms, comparator);
			tiq.fill(field, termsEnum);
		  }
		}
		else
		{
		  Fields fields = MultiFields.getFields(reader);
		  if (fields == null)
		  {
			throw new Exception("no fields found for this index");
		  }
		  tiq = new TermStatsQueue(numTerms, comparator);
		  foreach (string fieldName in fields)
		  {
			Terms terms = fields.terms(fieldName);
			if (terms != null)
			{
			  tiq.fill(fieldName, terms.iterator(null));
			}
		  }
		}

		TermStats[] result = new TermStats[tiq.size()];
		// we want highest first so we read the queue and populate the array
		// starting at the end and work backwards
		int count = tiq.size() - 1;
		while (tiq.size() != 0)
		{
		  result[count] = tiq.pop();
		  count--;
		}
		return result;
	  }

	  /// <summary>
	  /// Compares terms by docTermFreq
	  /// </summary>
	  public sealed class DocFreqComparator : IComparer<TermStats>
	  {

		public int Compare(TermStats a, TermStats b)
		{
		  int res = long.compare(a.docFreq, b.docFreq);
		  if (res == 0)
		  {
			res = a.field.CompareTo(b.field);
			if (res == 0)
			{
			  res = a.termtext.compareTo(b.termtext);
			}
		  }
		  return res;
		}
	  }

	  /// <summary>
	  /// Compares terms by totalTermFreq
	  /// </summary>
	  public sealed class TotalTermFreqComparator : IComparer<TermStats>
	  {

		public int Compare(TermStats a, TermStats b)
		{
		  int res = long.compare(a.totalTermFreq, b.totalTermFreq);
		  if (res == 0)
		  {
			res = a.field.CompareTo(b.field);
			if (res == 0)
			{
			  res = a.termtext.compareTo(b.termtext);
			}
		  }
		  return res;
		}
	  }

	  /// <summary>
	  /// Priority queue for TermStats objects
	  /// 
	  /// </summary>
	  internal sealed class TermStatsQueue : PriorityQueue<TermStats>
	  {
		internal readonly IComparer<TermStats> comparator;

		internal TermStatsQueue(int size, IComparer<TermStats> comparator) : base(size)
		{
		  this.comparator = comparator;
		}

		protected internal override bool lessThan(TermStats termInfoA, TermStats termInfoB)
		{
		  return comparator.Compare(termInfoA, termInfoB) < 0;
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: protected void fill(String field, org.apache.lucene.index.TermsEnum termsEnum) throws java.io.IOException
		protected internal void fill(string field, TermsEnum termsEnum)
		{
		  BytesRef term = null;
		  while ((term = termsEnum.next()) != null)
		  {
			insertWithOverflow(new TermStats(field, term, termsEnum.docFreq(), termsEnum.totalTermFreq()));
		  }
		}
	  }
	}

}