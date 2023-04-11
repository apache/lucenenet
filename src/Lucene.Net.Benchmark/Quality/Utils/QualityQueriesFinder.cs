using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Util;
using System;
using System.IO;
using Console = Lucene.Net.Util.SystemConsole;

namespace Lucene.Net.Benchmarks.Quality.Utils
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
    /// Suggest Quality queries based on an index contents.
    /// Utility class, used for making quality test benchmarks.
    /// </summary>
    public class QualityQueriesFinder
    {
        private static readonly string newline = Environment.NewLine;
        private readonly Store.Directory dir; // LUCENENET: marked readonly

        /// <summary>
        /// Constructor over a directory containing the index.
        /// </summary>
        /// <param name="dir">Directory containing the index we search for the quality test.</param>
        private QualityQueriesFinder(Store.Directory dir)
        {
            this.dir = dir;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="args">{index-dir}</param>
        /// <exception cref="IOException">if cannot access the index.</exception>
        public static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                // LUCENENET specific - our wrapper console shows correct usage
                throw new ArgumentException();
                //Console.Error.WriteLine("Usage: java QualityQueriesFinder <index-dir>");
                //Environment.Exit(1);
            }
            QualityQueriesFinder qqf = new QualityQueriesFinder(FSDirectory.Open(new DirectoryInfo(args[0])));
            string[] q = qqf.BestQueries("body", 20);
            for (int i = 0; i < q.Length; i++)
            {
                Console.WriteLine(newline + FormatQueryAsTrecTopic(i, q[i], null, null));
            }
        }

#pragma warning disable IDE0060 // Remove unused parameter
        private string[] BestQueries(string field, int numQueries)
#pragma warning restore IDE0060 // Remove unused parameter
        {
            string[] words = BestTerms("body", 4 * numQueries);
            int n = words.Length;
            int m = n / 4;
            string[] res = new string[m];
            for (int i = 0; i < res.Length; i++)
            {
                res[i] = words[i] + " " + words[m + i] + "  " + words[n - 1 - m - i] + " " + words[n - 1 - i];
                //System.out.println("query["+i+"]:  "+res[i]);
            }
            return res;
        }

        private static string FormatQueryAsTrecTopic(int qnum, string title, string description, string narrative)
        {
            return
              "<top>" + newline +
              "<num> Number: " + qnum + newline + newline +
              "<title> " + (title ?? "") + newline + newline +
              "<desc> Description:" + newline +
              (description ?? "") + newline + newline +
              "<narr> Narrative:" + newline +
              (narrative ?? "") + newline + newline +
              "</top>";
        }

        private string[] BestTerms(string field, int numTerms)
        {
            PriorityQueue<TermDf> pq = new TermsDfQueue(numTerms);
            IndexReader ir = DirectoryReader.Open(dir);
            try
            {
                int threshold = ir.MaxDoc / 10; // ignore words too common.
                Terms terms = MultiFields.GetTerms(ir, field);
                if (terms != null)
                {
                    TermsEnum termsEnum = terms.GetEnumerator();
                    while (termsEnum.MoveNext())
                    {
                        int df = termsEnum.DocFreq;
                        if (df < threshold)
                        {
                            string ttxt = termsEnum.Term.Utf8ToString();
                            pq.InsertWithOverflow(new TermDf(ttxt, df));
                        }
                    }
                }
            }
            finally
            {
                ir.Dispose();
            }
            string[] res = new string[pq.Count];
            int i = 0;
            while (pq.Count > 0)
            {
                TermDf tdf = pq.Pop();
                res[i++] = tdf.word;
                Console.WriteLine(i + ".   word:  " + tdf.df + "   " + tdf.word);
            }
            return res;
        }

        private class TermDf
        {
            internal string word;
            internal int df;
            internal TermDf(string word, int freq)
            {
                this.word = word;
                this.df = freq;
            }
        }

        private class TermsDfQueue : PriorityQueue<TermDf>
        {
            internal TermsDfQueue(int maxSize)
                : base(maxSize)
            {
            }

            protected internal override bool LessThan(TermDf tf1, TermDf tf2)
            {
                return tf1.df < tf2.df;
            }
        }
    }
}
