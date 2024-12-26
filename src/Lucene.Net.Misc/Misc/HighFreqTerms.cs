using J2N.Text;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Console = Lucene.Net.Util.SystemConsole;

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

    // LUCENENET: Not used
    // If the -t flag is given, both document frequency and total tf (total
    // number of occurrences) are reported, ordered by descending total tf.

    /// <summary>
    /// <see cref="HighFreqTerms"/> class extracts the top n most frequent terms
    /// (by document frequency) from an existing Lucene index and reports their
    /// document frequency.
    /// <para />
    /// LUCENENET specific: In the Java implementation, this class' Main method
    /// was intended to be called from the command line. However, in .NET a
    /// method within a DLL can't be directly called from the command line so we
    /// provide a <see href="https://learn.microsoft.com/en-us/dotnet/core/tools/global-tools">.NET tool</see>,
    /// <see href="https://www.nuget.org/packages/lucene-cli">lucene-cli</see>,
    /// with a command that maps to that method:
    /// index list-high-freq-terms
    /// </summary>
    public static class HighFreqTerms // LUCENENET specific: CA1052 Static holder types should be Static or NotInheritable
    {
        // The top numTerms will be displayed
        public const int DEFAULT_NUMTERMS = 100;

        /// <summary>
        /// LUCENENET specific: In the Java implementation, this Main method
        /// was intended to be called from the command line. However, in .NET a
        /// method within a DLL can't be directly called from the command line so we
        /// provide a <see href="https://learn.microsoft.com/en-us/dotnet/core/tools/global-tools">.NET tool</see>,
        /// <see href="https://www.nuget.org/packages/lucene-cli">lucene-cli</see>,
        /// with a command that maps to this method:
        /// index list-high-freq-terms
        /// </summary>
        /// <param name="args"></param>
        /// <exception cref="ArgumentException"></exception>
        public static void Main(string[] args)
        {
            string field = null;
            int numTerms = DEFAULT_NUMTERMS;

            if (args.Length == 0 || args.Length > 4)
            {
                // LUCENENET specific - our wrapper console shows the correct usage
                throw new ArgumentException("HighFreqTerms requires 1 to 4 arguments", nameof(args));
                //Usage();
                //Environment.Exit(1);
            }

            using Store.Directory dir = FSDirectory.Open(new DirectoryInfo(args[0]));

            IComparer<TermStats> comparer = DocFreqComparer.Default;

            for (int i = 1; i < args.Length; i++)
            {
                if (args[i].Equals("-t", StringComparison.Ordinal))
                {
                    comparer = TotalTermFreqComparer.Default;
                }
                else
                {
                    if (!int.TryParse(args[i], NumberStyles.Integer, CultureInfo.InvariantCulture, out numTerms))
                        field = args[i];
                }
            }

            using IndexReader reader = DirectoryReader.Open(dir);
            TermStats[] terms = GetHighFreqTerms(reader, numTerms, field, comparer);

            for (int i = 0; i < terms.Length; i++)
            {
                Console.WriteLine("{0}:{1} \t totalTF = {2:#,##0} \t doc freq = {3:#,##0} \n", terms[i].Field, terms[i].GetTermText(), terms[i].TotalTermFreq, terms[i].DocFreq);
            }
        }

        // LUCENENET specific - The lucene-cli docs show the correct usage
        //private static void Usage()
        //{
        //    Console.WriteLine("\n\n" + "java org.apache.lucene.misc.HighFreqTerms <index dir> [-t] [number_terms] [field]\n\t -t: order by totalTermFreq\n\n");
        //}

        /// <summary>
        /// Returns <see cref="T:TermStats[]"/> ordered by the specified comparer
        /// </summary>
        public static TermStats[] GetHighFreqTerms(IndexReader reader, int numTerms, string field, IComparer<TermStats> comparer)
        {
            TermStatsQueue tiq = null;

            if (field != null)
            {
                Fields fields = MultiFields.GetFields(reader);
                if (fields is null)
                {
                    throw RuntimeException.Create("field " + field + " not found");
                }
                Terms terms = fields.GetTerms(field);
                if (terms != null)
                {
                    TermsEnum termsEnum = terms.GetEnumerator();
                    tiq = new TermStatsQueue(numTerms, comparer);
                    tiq.Fill(field, termsEnum);
                }
            }
            else
            {
                Fields fields = MultiFields.GetFields(reader);
                if (fields is null)
                {
                    throw RuntimeException.Create("no fields found for this index");
                }
                tiq = new TermStatsQueue(numTerms, comparer);
                foreach (string fieldName in fields)
                {
                    Terms terms = fields.GetTerms(fieldName);
                    if (terms != null)
                    {
                        tiq.Fill(fieldName, terms.GetEnumerator());
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
        public sealed class DocFreqComparer : IComparer<TermStats>
        {
            private DocFreqComparer() { } // LUCENENET: Made into singleton

            public static IComparer<TermStats> Default { get; } = new DocFreqComparer();

            public int Compare(TermStats a, TermStats b)
            {
                int res = a.DocFreq.CompareTo(b.DocFreq);
                if (res == 0)
                {
                    res = a.Field.CompareToOrdinal(b.Field);
                    if (res == 0)
                    {
                        res = a.TermText.CompareTo(b.TermText);
                    }
                }
                return res;
            }
        }

        /// <summary>
        /// Compares terms by <see cref="TermStats.TotalTermFreq"/>
        /// </summary>
        public sealed class TotalTermFreqComparer : IComparer<TermStats>
        {
            private TotalTermFreqComparer() { } // LUCENENT: Made into singleton

            public static IComparer<TermStats> Default { get; } = new TotalTermFreqComparer();

            public int Compare(TermStats a, TermStats b)
            {
                int res = a.TotalTermFreq.CompareTo(b.TotalTermFreq);
                if (res == 0)
                {
                    res = a.Field.CompareToOrdinal(b.Field);
                    if (res == 0)
                    {
                        res = a.TermText.CompareTo(b.TermText);
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
            internal readonly IComparer<TermStats> comparer;

#nullable enable
            internal TermStatsQueue(int size, IComparer<TermStats> comparer)
                : base(size)
            {
                this.comparer = comparer ?? throw new ArgumentNullException(nameof(comparer)); // LUCENENET: Added null guard clause
            }
#nullable restore

            protected internal override bool LessThan(TermStats termInfoA, TermStats termInfoB)
            {
                return comparer.Compare(termInfoA, termInfoB) < 0;
            }

            internal void Fill(string field, TermsEnum termsEnum)
            {
                while (termsEnum.MoveNext())
                {
                    InsertWithOverflow(new TermStats(field, termsEnum.Term, termsEnum.DocFreq, termsEnum.TotalTermFreq));
                }
            }
        }
    }
}
