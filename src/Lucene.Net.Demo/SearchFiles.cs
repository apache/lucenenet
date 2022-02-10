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

// Add NuGet References:

// Lucene.Net.Analysis.Common
// Lucene.Net.QueryParser

using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers.Classic;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace Lucene.Net.Demo
{
    /// <summary>
    /// Simple command-line based search demo.
    /// </summary>
    public static class SearchFiles // LUCENENET specific: CA1052 Static holder types should be Static or NotInheritable
    {
        /// <summary>Simple command-line based search demo.</summary>
        public static void Main(string[] args)
        {
            // The <CONSOLE_APP_NAME> should be the assembly name of the application
            // this code is compiled into. In .NET Framework, it is the name of the EXE file.
            // In .NET Core, you have the option of compiling this into either a DLL or an EXE  
            // (see https://docs.microsoft.com/en-us/dotnet/core/deploying/index).
            // In the first case, the <CONSOLE_APP_NAME> will be "dotnet <DLL_NAME>.dll".
            string usage = "Usage: <CONSOLE_APP_NAME> <INDEX_DIRECTORY> [-f|--field <FIELD>] " +
                "[-r|--repeat <NUMBER>] [-qf|--queries-file <PATH>] [-q|--query <QUERY>] " +
                "[--raw] [-p|--page-size <NUMBER>]\n\n" +
                "Use no --query or --queries-file option for interactive mode.\n\n" +
                "See http://lucene.apache.org/core/4_8_0/demo/ for details.";
            if (args.Length < 1 || args.Length > 0 && 
                ("?".Equals(args[0], StringComparison.Ordinal) || "-h".Equals(args[0], StringComparison.Ordinal) || "--help".Equals(args[0], StringComparison.Ordinal)))
            {
                Console.WriteLine(usage);
                Environment.Exit(0);
            }

            string index = args[0];
            string field = "contents";
            string queries = null;
            int repeat = 0;
            bool raw = false;
            string queryString = null;
            int hitsPerPage = 10;

            for (int i = 0; i < args.Length; i++)
            {
                if ("-f".Equals(args[i], StringComparison.Ordinal) || "-field".Equals(args[i], StringComparison.Ordinal))
                {
                    field = args[i + 1];
                    i++;
                }
                else if ("-qf".Equals(args[i], StringComparison.Ordinal) || "--queries-file".Equals(args[i], StringComparison.Ordinal))
                {
                    queries = args[i + 1];
                    i++;
                }
                else if ("-q".Equals(args[i], StringComparison.Ordinal) || "--query".Equals(args[i], StringComparison.Ordinal))
                {
                    queryString = args[i + 1];
                    i++;
                }
                else if ("-r".Equals(args[i], StringComparison.Ordinal) || "--repeat".Equals(args[i], StringComparison.Ordinal))
                {
                    repeat = int.Parse(args[i + 1], CultureInfo.InvariantCulture);
                    i++;
                }
                else if ("--raw".Equals(args[i], StringComparison.Ordinal))
                {
                    raw = true;
                }
                else if ("-p".Equals(args[i], StringComparison.Ordinal) || "--paging".Equals(args[i], StringComparison.Ordinal))
                {
                    hitsPerPage = int.Parse(args[i + 1], CultureInfo.InvariantCulture);
                    if (hitsPerPage <= 0)
                    {
                        Console.WriteLine("There must be at least 1 hit per page.");
                        Environment.Exit(1);
                    }
                    i++;
                }
            }

            using IndexReader reader = DirectoryReader.Open(FSDirectory.Open(index));
            IndexSearcher searcher = new IndexSearcher(reader);
            // :Post-Release-Update-Version.LUCENE_XY:
            Analyzer analyzer = new StandardAnalyzer(LuceneVersion.LUCENE_48);

            TextReader input = null;
            if (queries != null)
            {
                input = new StreamReader(new FileStream(queries, FileMode.Open, FileAccess.Read), Encoding.UTF8);
            }
            else
            {
                input = Console.In;
            }
            // :Post-Release-Update-Version.LUCENE_XY:
            QueryParser parser = new QueryParser(LuceneVersion.LUCENE_48, field, analyzer);
            while (true)
            {
                if (queries is null && queryString is null)
                {
                    // prompt the user
                    Console.WriteLine("Enter query (or press Enter to exit): ");
                }

                string line = queryString ?? input.ReadLine();

                if (line is null || line.Length == 0)
                {
                    break;
                }

                line = line.Trim();
                if (line.Length == 0)
                {
                    break;
                }

                Query query = parser.Parse(line);
                Console.WriteLine("Searching for: " + query.ToString(field));

                if (repeat > 0) // repeat & time as benchmark
                {
                    DateTime start = DateTime.UtcNow;
                    for (int i = 0; i < repeat; i++)
                    {
                        searcher.Search(query, null, 100);
                    }
                    DateTime end = DateTime.UtcNow;
                    Console.WriteLine("Time: " + (end - start).TotalMilliseconds + "ms");
                }

                DoPagingSearch(searcher, query, hitsPerPage, raw, queries is null && queryString is null);

                if (queryString != null)
                {
                    break;
                }
            }
        }

        /// <summary>
        /// This demonstrates a typical paging search scenario, where the search engine presents 
        /// pages of size n to the user. The user can then go to the next page if interested in
        /// the next hits.
        /// <para/>
        /// When the query is executed for the first time, then only enough results are collected
        /// to fill 5 result pages. If the user wants to page beyond this limit, then the query
        /// is executed another time and all hits are collected.
        /// </summary>
        public static void DoPagingSearch(IndexSearcher searcher, Query query,
                                           int hitsPerPage, bool raw, bool interactive)
        {
            // Collect enough docs to show 5 pages
            TopDocs results = searcher.Search(query, 5 * hitsPerPage);
            ScoreDoc[]
            hits = results.ScoreDocs;

            int numTotalHits = results.TotalHits;
            Console.WriteLine(numTotalHits + " total matching documents");

            int start = 0;
            int end = Math.Min(numTotalHits, hitsPerPage);

            while (true)
            {
                if (end > hits.Length)
                {
                    Console.WriteLine("Only results 1 - " + hits.Length + " of " + numTotalHits + " total matching documents collected.");
                    Console.WriteLine("Collect more (y/n) ?");
                    var key = Console.ReadKey().KeyChar;
                    if (key == 'n')
                    {
                        break;
                    }

                    hits = searcher.Search(query, numTotalHits).ScoreDocs;
                }

                end = Math.Min(hits.Length, start + hitsPerPage);

                for (int i = start; i < end; i++)
                {
                    if (raw) // output raw format
                    {                   
                        Console.WriteLine("doc=" + hits[i].Doc + " score=" + hits[i].Score);
                        continue;
                    }

                    Document doc = searcher.Doc(hits[i].Doc);
                    string path = doc.Get("path");
                    if (path != null)
                    {
                        Console.WriteLine((i + 1) + ". " + path);
                        string title = doc.Get("title");
                        if (title != null)
                        {
                            Console.WriteLine("   Title: " + doc.Get("title"));
                        }
                    }
                    else
                    {
                        Console.WriteLine((i + 1) + ". " + "No path for this document");
                    }

                }

                if (!interactive || end == 0)
                {
                    break;
                }

                if (numTotalHits >= end)
                {
                    var pageNumberBuilder = new StringBuilder();
                    bool quit = false;
                    while (true)
                    {
                        if (pageNumberBuilder.Length == 0)
                        {
                            Console.WriteLine("Press ");
                            if (start - hitsPerPage >= 0)
                            {
                                Console.WriteLine("(p)revious page, ");
                            }
                            if (start + hitsPerPage < numTotalHits)
                            {
                                Console.WriteLine("(n)ext page, ");
                            }
                            Console.WriteLine("(q)uit or enter number to jump to a page.");
                        }
                        var key = Console.ReadKey(intercept: true).KeyChar;
                        if (key == 'q')
                        {
                            quit = true;
                            break;
                        }
                        if (key == 'p')
                        {
                            start = Math.Max(0, start - hitsPerPage);
                            break;
                        }
                        else if (key == 'n')
                        {
                            if (start + hitsPerPage < numTotalHits)
                            {
                                start += hitsPerPage;
                            }
                            break;
                        }
                        else if (key == (char)13) // enter key
                        {
                            Console.WriteLine();
                            int page = int.Parse(pageNumberBuilder.ToString(), CultureInfo.InvariantCulture);
                            pageNumberBuilder.Clear();
                            if ((page - 1) * hitsPerPage < numTotalHits)
                            {
                                start = (page - 1) * hitsPerPage;
                                break;
                            }
                            else
                            {
                                Console.WriteLine("No such page");
                            }
                        }
                        else if (char.IsDigit(key))
                        {
                            Console.Write(key);
                            pageNumberBuilder.Append(key);
                        }
                        else
                        {
                            Console.WriteLine("No such command");
                        }
                    }
                    if (quit) break;
                    end = Math.Min(numTotalHits, start + hitsPerPage);
                }
            }
        }
    }
}
