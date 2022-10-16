using J2N.Collections.Generic.Extensions;
using Lucene.Net.Diagnostics;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using JCG = J2N.Collections.Generic;

namespace Lucene.Net.Benchmarks.Quality
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
    /// Results of quality benchmark run for a single query or for a set of queries.
    /// </summary>
    public class QualityStats
    {
        /// <summary>Number of points for which precision is computed.</summary>
        public static readonly int MAX_POINTS = 20;

        private double maxGoodPoints;
        private double recall;
        private readonly double[] pAt; // LUCENENET: marked readonly
        private double pReleventSum = 0;
        private double numPoints = 0;
        private double numGoodPoints = 0;
        private double mrr = 0;
        private long searchTime;
        private long docNamesExtractTime;

        /// <summary>
        /// A certain rank in which a relevant doc was found.
        /// </summary>
        public class RecallPoint
        {
            private readonly int rank; // LUCENENET: marked readonly
            private readonly double recall; // LUCENENET: marked readonly
            internal RecallPoint(int rank, double recall)
            {
                this.rank = rank;
                this.recall = recall;
            }

            /// <summary>Returns the rank: where on the list of returned docs this relevant doc appeared.</summary>
            public virtual int Rank => rank;

            /// <summary>Returns the recall: how many relevant docs were returned up to this point, inclusive.</summary>
            public virtual double Recall => recall;
        }

        private readonly IList<RecallPoint> recallPoints; // LUCENENET: marked readonly

        /// <summary>
        /// Construct a QualityStats object with anticipated maximal number of relevant hits. 
        /// </summary>
        /// <param name="maxGoodPoints">maximal possible relevant hits.</param>
        /// <param name="searchTime"></param>
        public QualityStats(double maxGoodPoints, long searchTime)
        {
            this.maxGoodPoints = maxGoodPoints;
            this.searchTime = searchTime;
            this.recallPoints = new JCG.List<RecallPoint>();
            pAt = new double[MAX_POINTS + 1]; // pAt[0] unused. 
        }

        /// <summary>
        /// Add a (possibly relevant) doc.
        /// </summary>
        /// <param name="n">rank of the added doc (its ordinal position within the query results).</param>
        /// <param name="isRelevant"><c>true</c> if the added doc is relevant, <c>false</c> otherwise.</param>
        /// <param name="docNameExtractTime"></param>
        public virtual void AddResult(int n, bool isRelevant, long docNameExtractTime)
        {
            if (Math.Abs(numPoints + 1 - n) > 1E-6)
            {
                throw new ArgumentOutOfRangeException(nameof(n), "point " + n + " illegal after " + numPoints + " points!");// LUCENENET specific - changed from IllegalArgumentException to ArgumentOutOfRangeException (.NET convention)
            }
            if (isRelevant)
            {
                numGoodPoints += 1;
                recallPoints.Add(new RecallPoint(n, numGoodPoints));
                if (recallPoints.Count == 1 && n <= 5)
                { // first point, but only within 5 top scores. 
                    mrr = 1.0 / n;
                }
            }
            numPoints = n;
            double p = numGoodPoints / numPoints;
            if (isRelevant)
            {
                pReleventSum += p;
            }
            if (n < pAt.Length)
            {
                pAt[n] = p;
            }
            recall = maxGoodPoints <= 0 ? p : numGoodPoints / maxGoodPoints;
            docNamesExtractTime += docNameExtractTime;
        }

        /// <summary>
        /// Return the precision at rank n:
        /// |{relevant hits within first <c>n</c> hits}| / <c>n</c>.
        /// </summary>
        /// <param name="n">requested precision point, must be at least 1 and at most <see cref="MAX_POINTS"/>.</param>
        /// <returns></returns>
        public virtual double GetPrecisionAt(int n)
        {
            if (n < 1 || n > MAX_POINTS)
            {
                throw new ArgumentException("n=" + n + " - but it must be in [1," + MAX_POINTS + "] range!");
            }
            if (n > numPoints)
            {
                return (numPoints * pAt[(int)numPoints]) / n;
            }
            return pAt[n];
        }

        /// <summary>
        /// Return the average precision at recall points.
        /// </summary>
        public virtual double GetAvp()
        {
            return maxGoodPoints == 0 ? 0 : pReleventSum / maxGoodPoints;
        }

        /// <summary>
        /// Return the recall: |{relevant hits found}| / |{relevant hits existing}|.
        /// </summary>
        public virtual double Recall => recall;

        /// <summary>
        /// Log information on this <see cref="QualityStats"/> object.
        /// </summary>
        /// <param name="title"></param>
        /// <param name="paddLines"></param>
        /// <param name="logger">Logger.</param>
        /// <param name="prefix">prefix before each log line.</param>
        public virtual void Log(string title, int paddLines, TextWriter logger, string prefix)
        {
            for (int i = 0; i < paddLines; i++)
            {
                logger.WriteLine();
            }
            if (title != null && title.Trim().Length > 0)
            {
                logger.WriteLine(title);
            }
            prefix = prefix ?? "";
            string nf = "{0:F3}";
            int M = 19;
            logger.WriteLine(prefix + Format("Search Seconds: ", M) +
                FracFormat(string.Format(CultureInfo.InvariantCulture, nf, (double)searchTime / 1000)));
            logger.WriteLine(prefix + Format("DocName Seconds: ", M) +
                FracFormat(string.Format(CultureInfo.InvariantCulture, nf, (double)docNamesExtractTime / 1000)));
            logger.WriteLine(prefix + Format("Num Points: ", M) +
                FracFormat(string.Format(CultureInfo.InvariantCulture, nf, numPoints)));
            logger.WriteLine(prefix + Format("Num Good Points: ", M) +
                FracFormat(string.Format(CultureInfo.InvariantCulture, nf, numGoodPoints)));
            logger.WriteLine(prefix + Format("Max Good Points: ", M) +
                FracFormat(string.Format(CultureInfo.InvariantCulture, nf, maxGoodPoints)));
            logger.WriteLine(prefix + Format("Average Precision: ", M) +
                FracFormat(string.Format(CultureInfo.InvariantCulture, nf, GetAvp())));
            logger.WriteLine(prefix + Format("MRR: ", M) +
                FracFormat(string.Format(CultureInfo.InvariantCulture, nf, MRR)));
            logger.WriteLine(prefix + Format("Recall: ", M) +
                FracFormat(string.Format(CultureInfo.InvariantCulture, nf, Recall)));
            for (int i = 1; i < (int)numPoints && i < pAt.Length; i++)
            {
                logger.WriteLine(prefix + Format("Precision At " + i + ": ", M) +
                    FracFormat(string.Format(CultureInfo.InvariantCulture, nf, GetPrecisionAt(i))));
            }
            for (int i = 0; i < paddLines; i++)
            {
                logger.WriteLine();
            }
        }

        private const string padd = "                                    ";
        private static string Format(string s, int minLen) // LUCENENET: CA1822: Mark members as static
        {
            s = (s ?? "");
            int n = Math.Max(minLen, s.Length);
            return (s + padd).Substring(0, n-0);
        }
        private static string FracFormat(string frac) // LUCENENET: CA1822: Mark members as static
        {
            int k = frac.IndexOf('.');
            string s1 = padd + frac.Substring(0, k-0);
            int n = Math.Max(k, 6);
            s1 = s1.Substring(s1.Length - n);
            return s1 + frac.Substring(k);
        }

        /// <summary>
        /// Create a <see cref="QualityStats"/> object that is the average of the input <see cref="QualityStats"/> objects. 
        /// </summary>
        /// <param name="stats">array of input stats to be averaged.</param>
        /// <returns>an average over the input stats.</returns>
        public static QualityStats Average(QualityStats[] stats)
        {
            QualityStats avg = new QualityStats(0, 0);
            if (stats.Length == 0)
            {
                // weired, no stats to average!
                return avg;
            }
            int m = 0; // queries with positive judgements
                       // aggregate
            for (int i = 0; i < stats.Length; i++)
            {
                avg.searchTime += stats[i].searchTime;
                avg.docNamesExtractTime += stats[i].docNamesExtractTime;
                if (stats[i].maxGoodPoints > 0)
                {
                    m++;
                    avg.numGoodPoints += stats[i].numGoodPoints;
                    avg.numPoints += stats[i].numPoints;
                    avg.pReleventSum += stats[i].GetAvp();
                    avg.recall += stats[i].recall;
                    avg.mrr += stats[i].MRR;
                    avg.maxGoodPoints += stats[i].maxGoodPoints;
                    for (int j = 1; j < avg.pAt.Length; j++)
                    {
                        avg.pAt[j] += stats[i].GetPrecisionAt(j);
                    }
                }
            }
            if (Debugging.AssertsEnabled) Debugging.Assert(m > 0, "Fishy: no \"good\" queries!");
            // take average: times go by all queries, other measures go by "good" queries only.
            avg.searchTime /= stats.Length;
            avg.docNamesExtractTime /= stats.Length;
            avg.numGoodPoints /= m;
            avg.numPoints /= m;
            avg.recall /= m;
            avg.mrr /= m;
            avg.maxGoodPoints /= m;
            for (int j = 1; j < avg.pAt.Length; j++)
            {
                avg.pAt[j] /= m;
            }
            avg.pReleventSum /= m;                 // this is actually avgp now 
            avg.pReleventSum *= avg.maxGoodPoints; // so that getAvgP() would be correct

            return avg;
        }

        /// <summary>
        /// Returns the time it took to extract doc names for judging the measured query, in milliseconds.
        /// </summary>
        public virtual long DocNamesExtractTime => docNamesExtractTime;

        /// <summary>
        /// Returns the maximal number of good points.
        /// This is the number of relevant docs known by the judge for the measured query.
        /// </summary>
        public virtual double MaxGoodPoints => maxGoodPoints;

        /// <summary>
        /// Returns the number of good points (only relevant points).
        /// </summary>
        public virtual double NumGoodPoints => numGoodPoints;

        /// <summary>
        /// Returns the number of points (both relevant and irrelevant points).
        /// </summary>
        public virtual double NumPoints => numPoints;

        /// <summary>
        /// Returns the recallPoints.
        /// </summary>
        public virtual RecallPoint[] GetRecallPoints()
        {
            return recallPoints.ToArray();
        }

        /// <summary>
        /// Returns the Mean reciprocal rank over the queries or RR for a single query.
        /// </summary>
        /// <remarks>
        /// Reciprocal rank is defined as <c>1/r</c> where <c>r</c> is the 
        /// rank of the first correct result, or <c>0</c> if there are no correct 
        /// results within the top 5 results.
        /// <para/>
        /// This follows the definition in 
        /// <a href="http://www.cnlp.org/publications/02cnlptrec10.pdf">
        /// Question Answering - CNLP at the TREC-10 Question Answering Track</a>.
        /// </remarks>
        public virtual double MRR => mrr;


        /// <summary>
        /// Returns the search time in milliseconds for the measured query.
        /// </summary>
        public virtual long SearchTime => searchTime;
    }
}
