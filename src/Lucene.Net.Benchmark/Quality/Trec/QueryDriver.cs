using J2N.Collections.Generic.Extensions;
using Lucene.Net.Benchmarks.Quality.Utils;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Console = Lucene.Net.Util.SystemConsole;
using JCG = J2N.Collections.Generic;

namespace Lucene.Net.Benchmarks.Quality.Trec
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
    /// Command-line tool for doing a TREC evaluation run.
    /// </summary>
    public static class QueryDriver // LUCENENET specific: CA1052 Static holder types should be Static or NotInheritable
    {
        public static void Main(string[] args)
        {
            if (args.Length < 4 || args.Length > 5)
            {
                // LUCENENET specific - our wrapper console shows correct usage
                throw new ArgumentException();
                //Console.Error.WriteLine("Usage: QueryDriver <topicsFile> <qrelsFile> <submissionFile> <indexDir> [querySpec]");
                //Console.Error.WriteLine("topicsFile: input file containing queries");
                //Console.Error.WriteLine("qrelsFile: input file containing relevance judgements");
                //Console.Error.WriteLine("submissionFile: output submission file for trec_eval");
                //Console.Error.WriteLine("indexDir: index directory");
                //Console.Error.WriteLine("querySpec: string composed of fields to use in query consisting of T=title,D=description,N=narrative:");
                //Console.Error.WriteLine("\texample: TD (query on Title + Description). The default is T (title only)");
                //Environment.Exit(1);
            }

            FileInfo topicsFile = new FileInfo(args[0]);
            FileInfo qrelsFile = new FileInfo(args[1]);
            SubmissionReport submitLog = new SubmissionReport(new StreamWriter(new FileStream(args[2], FileMode.Create, FileAccess.Write), Encoding.UTF8 /* huh, no nio.Charset ctor? */), "lucene");
            using Store.FSDirectory dir = Store.FSDirectory.Open(new DirectoryInfo(args[3]));
            using IndexReader reader = DirectoryReader.Open(dir);
            string fieldSpec = args.Length == 5 ? args[4] : "T"; // default to Title-only if not specified.
            IndexSearcher searcher = new IndexSearcher(reader);

            int maxResults = 1000;
            string docNameField = "docname";

            TextWriter logger = Console.Out; //new StreamWriter(Console, Encoding.GetEncoding(0));

            // use trec utilities to read trec topics into quality queries
            TrecTopicsReader qReader = new TrecTopicsReader();
            QualityQuery[] qqs = qReader.ReadQueries(IOUtils.GetDecodingReader(topicsFile, Encoding.UTF8));

            // prepare judge, with trec utilities that read from a QRels file
            IJudge judge = new TrecJudge(IOUtils.GetDecodingReader(qrelsFile, Encoding.UTF8));

            // validate topics & judgments match each other
            judge.ValidateData(qqs, logger);

            ISet<string> fieldSet = new JCG.HashSet<string>();
            if (fieldSpec.IndexOf('T') >= 0) fieldSet.Add("title");
            if (fieldSpec.IndexOf('D') >= 0) fieldSet.Add("description");
            if (fieldSpec.IndexOf('N') >= 0) fieldSet.Add("narrative");

            // set the parsing of quality queries into Lucene queries.
            IQualityQueryParser qqParser = new SimpleQQParser(fieldSet.ToArray(), "body");

            // run the benchmark
            QualityBenchmark qrun = new QualityBenchmark(qqs, qqParser, searcher, docNameField);
            qrun.MaxResults = maxResults;
            QualityStats[] stats = qrun.Execute(judge, submitLog, logger);

            // print an avarage sum of the results
            QualityStats avg = QualityStats.Average(stats);
            avg.Log("SUMMARY", 2, logger, "  ");
        }
    }
}
