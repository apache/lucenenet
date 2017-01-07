using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.IO;

namespace Lucene.Net.Misc
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

    /// <summary>
    /// <see cref="HighFreqTerms"/> class extracts the top n most frequent terms
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

        public static void Main(string[] args)
        {
            string field = null;
            int numTerms = DEFAULT_NUMTERMS;

            if (args.Length == 0 || args.Length > 4)
            {
                Usage();
                Environment.Exit(1);
            }

            Store.Directory dir = FSDirectory.Open(new DirectoryInfo(args[0]));

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
                    catch (FormatException)
                    {
                        field = args[i];
                    }
                }
            }

            using (IndexReader reader = DirectoryReader.Open(dir))
            {
                TermStats[] terms = GetHighFreqTerms(reader, numTerms, field, comparator);

                for (int i = 0; i < terms.Length; i++)
                {
                    Console.WriteLine("{0}:{1} \t totalTF = {2:#,##0} \t doc freq = {3:#,##0} \n", terms[i].Field, terms[i].TermText, terms[i].TotalTermFreq, terms[i].DocFreq);
                }
            }
        }

        private static void Usage()
        {
            // LUCENENET TODO: Usage depends on packaging this into an assembly executable.
            Console.WriteLine("\n\n" + "java org.apache.lucene.misc.HighFreqTerms <index dir> [-t] [number_terms] [field]\n\t -t: order by totalTermFreq\n\n");
        }

        /// <summary>
        /// Returns <see cref="TermStats[]"/> ordered by the specified comparator
        /// </summary>
        public static TermStats[] GetHighFreqTerms(IndexReader reader, int numTerms, string field, IComparer<TermStats> comparator)
        {
            TermStatsQueue tiq = null;

            if (field != null)
            {
                Fields fields = MultiFields.GetFields(reader);
                if (fields == null)
                {
                    throw new Exception("field " + field + " not found");
                }
                Terms terms = fields.Terms(field);
                if (terms != null)
                {
                    TermsEnum termsEnum = terms.Iterator(null);
                    tiq = new TermStatsQueue(numTerms, comparator);
                    tiq.Fill(field, termsEnum);
                }
            }
            else
            {
                Fields fields = MultiFields.GetFields(reader);
                if (fields == null)
                {
                    throw new Exception("no fields found for this index");
                }
                tiq = new TermStatsQueue(numTerms, comparator);
                foreach (string fieldName in fields)
                {
                    Terms terms = fields.Terms(fieldName);
                    if (terms != null)
                    {
                        tiq.Fill(fieldName, terms.Iterator(null));
                    }
                }
            }

            TermStats[] result = new TermStats[tiq.Count];
            // we want highest first so we read the queue and populate the array
            // starting at the end and work backwards
            int count = tiq.Count - 1;
            while (tiq.Count != 0)
            {
                result[count] = tiq.Pop();
                count--;
            }
            return result;
        }

        /// <summary>
        /// Compares terms by <see cref="TermStats.DocFreq"/>
        /// </summary>
        public sealed class DocFreqComparator : IComparer<TermStats>
        {

            public int Compare(TermStats a, TermStats b)
            {
                int res = a.DocFreq.CompareTo(b.DocFreq);
                if (res == 0)
                {
                    res = a.Field.CompareTo(b.Field);
                    if (res == 0)
                    {
                        res = a.termtext.CompareTo(b.termtext);
                    }
                }
                return res;
            }
        }

        /// <summary>
        /// Compares terms by <see cref="TermStats.TotalTermFreq"/> 
        /// </summary>
        public sealed class TotalTermFreqComparator : IComparer<TermStats>
        {
            public int Compare(TermStats a, TermStats b)
            {
                int res = a.TotalTermFreq.CompareTo(b.TotalTermFreq);
                if (res == 0)
                {
                    res = a.Field.CompareTo(b.Field);
                    if (res == 0)
                    {
                        res = a.termtext.CompareTo(b.termtext);
                    }
                }
                return res;
            }
        }

        /// <summary>
        /// Priority queue for <see cref="TermStats"/> objects
        /// 
        /// </summary>
        internal sealed class TermStatsQueue : PriorityQueue<TermStats>
        {
            internal readonly IComparer<TermStats> comparator;

            internal TermStatsQueue(int size, IComparer<TermStats> comparator) 
                : base(size)
            {
                this.comparator = comparator;
            }

            protected internal override bool LessThan(TermStats termInfoA, TermStats termInfoB)
            {
                return comparator.Compare(termInfoA, termInfoB) < 0;
            }

            internal void Fill(string field, TermsEnum termsEnum)
            {
                BytesRef term = null;
                while ((term = termsEnum.Next()) != null)
                {
                    InsertWithOverflow(new TermStats(field, term, termsEnum.DocFreq, termsEnum.TotalTermFreq));
                }
            }
        }
    }
}