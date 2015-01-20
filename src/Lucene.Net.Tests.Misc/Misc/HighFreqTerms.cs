/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Collections.Generic;
using System.Globalization;
using Org.Apache.Lucene.Index;
using Org.Apache.Lucene.Misc;
using Org.Apache.Lucene.Store;
using Org.Apache.Lucene.Util;
using Sharpen;

namespace Org.Apache.Lucene.Misc
{
	/// <summary>
	/// <code>HighFreqTerms</code> class extracts the top n most frequent terms
	/// (by document frequency) from an existing Lucene index and reports their
	/// document frequency.
	/// </summary>
	/// <remarks>
	/// <code>HighFreqTerms</code> class extracts the top n most frequent terms
	/// (by document frequency) from an existing Lucene index and reports their
	/// document frequency.
	/// <p>
	/// If the -t flag is given, both document frequency and total tf (total
	/// number of occurrences) are reported, ordered by descending total tf.
	/// </remarks>
	public class HighFreqTerms
	{
		public const int DEFAULT_NUMTERMS = 100;

		// The top numTerms will be displayed
		/// <exception cref="System.Exception"></exception>
		public static void Main(string[] args)
		{
			string field = null;
			int numTerms = DEFAULT_NUMTERMS;
			if (args.Length == 0 || args.Length > 4)
			{
				Usage();
				System.Environment.Exit(1);
			}
			Directory dir = FSDirectory.Open(new FilePath(args[0]));
			IComparer<TermStats> comparator = new HighFreqTerms.DocFreqComparator();
			for (int i = 1; i < args.Length; i++)
			{
				if (args[i].Equals("-t"))
				{
					comparator = new HighFreqTerms.TotalTermFreqComparator();
				}
				else
				{
					try
					{
						numTerms = System.Convert.ToInt32(args[i]);
					}
					catch (FormatException)
					{
						field = args[i];
					}
				}
			}
			IndexReader reader = DirectoryReader.Open(dir);
			TermStats[] terms = GetHighFreqTerms(reader, numTerms, field, comparator);
			for (int i_1 = 0; i_1 < terms.Length; i_1++)
			{
				System.Console.Out.Printf(CultureInfo.ROOT, "%s:%s \t totalTF = %,d \t docFreq = %,d \n"
					, terms[i_1].field, terms[i_1].termtext.Utf8ToString(), terms[i_1].totalTermFreq
					, terms[i_1].docFreq);
			}
			reader.Close();
		}

		private static void Usage()
		{
			System.Console.Out.WriteLine("\n\n" + "java org.apache.lucene.misc.HighFreqTerms <index dir> [-t] [number_terms] [field]\n\t -t: order by totalTermFreq\n\n"
				);
		}

		/// <summary>Returns TermStats[] ordered by the specified comparator</summary>
		/// <exception cref="System.Exception"></exception>
		public static TermStats[] GetHighFreqTerms(IndexReader reader, int numTerms, string
			 field, IComparer<TermStats> comparator)
		{
			HighFreqTerms.TermStatsQueue tiq = null;
			if (field != null)
			{
				Fields fields = MultiFields.GetFields(reader);
				if (fields == null)
				{
					throw new RuntimeException("field " + field + " not found");
				}
				Terms terms = fields.Terms(field);
				if (terms != null)
				{
					TermsEnum termsEnum = terms.Iterator(null);
					tiq = new HighFreqTerms.TermStatsQueue(numTerms, comparator);
					tiq.Fill(field, termsEnum);
				}
			}
			else
			{
				Fields fields = MultiFields.GetFields(reader);
				if (fields == null)
				{
					throw new RuntimeException("no fields found for this index");
				}
				tiq = new HighFreqTerms.TermStatsQueue(numTerms, comparator);
				foreach (string fieldName in fields)
				{
					Terms terms = fields.Terms(fieldName);
					if (terms != null)
					{
						tiq.Fill(fieldName, terms.Iterator(null));
					}
				}
			}
			TermStats[] result = new TermStats[tiq.Size()];
			// we want highest first so we read the queue and populate the array
			// starting at the end and work backwards
			int count = tiq.Size() - 1;
			while (tiq.Size() != 0)
			{
				result[count] = tiq.Pop();
				count--;
			}
			return result;
		}

		/// <summary>Compares terms by docTermFreq</summary>
		public sealed class DocFreqComparator : IComparer<TermStats>
		{
			public int Compare(TermStats a, TermStats b)
			{
				int res = long.Compare(a.docFreq, b.docFreq);
				if (res == 0)
				{
					res = Sharpen.Runtime.CompareOrdinal(a.field, b.field);
					if (res == 0)
					{
						res = a.termtext.CompareTo(b.termtext);
					}
				}
				return res;
			}
		}

		/// <summary>Compares terms by totalTermFreq</summary>
		public sealed class TotalTermFreqComparator : IComparer<TermStats>
		{
			public int Compare(TermStats a, TermStats b)
			{
				int res = long.Compare(a.totalTermFreq, b.totalTermFreq);
				if (res == 0)
				{
					res = Sharpen.Runtime.CompareOrdinal(a.field, b.field);
					if (res == 0)
					{
						res = a.termtext.CompareTo(b.termtext);
					}
				}
				return res;
			}
		}

		/// <summary>Priority queue for TermStats objects</summary>
		internal sealed class TermStatsQueue : PriorityQueue<TermStats>
		{
			internal readonly IComparer<TermStats> comparator;

			internal TermStatsQueue(int size, IComparer<TermStats> comparator) : base(size)
			{
				this.comparator = comparator;
			}

			protected override bool LessThan(TermStats termInfoA, TermStats termInfoB)
			{
				return comparator.Compare(termInfoA, termInfoB) < 0;
			}

			/// <exception cref="System.IO.IOException"></exception>
			protected internal void Fill(string field, TermsEnum termsEnum)
			{
				BytesRef term = null;
				while ((term = termsEnum.Next()) != null)
				{
					InsertWithOverflow(new TermStats(field, term, termsEnum.DocFreq(), termsEnum.TotalTermFreq
						()));
				}
			}
		}
	}
}
