/* 
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 * 
 * http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.IO;
using System.Text;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.QueryParsers;
using Lucene.Net.Index;
using Lucene.Net.Search;
using FSDirectory = Lucene.Net.Store.FSDirectory;
using Version = Lucene.Net.Util.Version;

namespace Lucene.Net.Demo
{
	
	/// <summary>Simple command-line based search demo. </summary>
	public static class SearchFiles
	{
		private class AnonymousClassCollector : Collector
		{
			private Scorer scorer;
			private int docBase;
			
			// simply print docId and score of every matching document
			public override void Collect(int doc)
			{
				Console.Out.WriteLine("doc=" + doc + docBase + " score=" + scorer.Score());
			}
			
			public override bool AcceptsDocsOutOfOrder
			{
                get { return true; }
			}
			
			public override void SetNextReader(IndexReader reader, int docBase)
			{
				this.docBase = docBase;
			}
			
			public override void  SetScorer(Scorer scorer)
			{
				this.scorer = scorer;
			}
		}
		
		/// <summary>
		/// Use the norms from one field for all fields.  Norms are read into memory,
		/// using a byte of memory per document per searched field.  This can cause
		/// search of large collections with a large number of fields to run out of
		/// memory.  If all of the fields contain only a single token, then the norms
		/// are all identical, then single norm vector may be shared. 
		/// </summary>
		private class OneNormsReader : FilterIndexReader
		{
			private readonly String field;
			
			public OneNormsReader(IndexReader in_Renamed, String field):base(in_Renamed)
			{
				this.field = field;
			}
			
			public override byte[] Norms(String field)
			{
				return in_Renamed.Norms(this.field);
			}
		}
				
		/// <summary>Simple command-line based search demo. </summary>
		[STAThread]
		public static void Main(String[] args)
		{
			String usage = "Usage:\t" + typeof(SearchFiles) + "[-index dir] [-field f] [-repeat n] [-queries file] [-raw] [-norms field] [-paging hitsPerPage]";
			usage += "\n\tSpecify 'false' for hitsPerPage to use streaming instead of paging search.";
			if (args.Length > 0 && ("-h".Equals(args[0]) || "-help".Equals(args[0])))
			{
				Console.Out.WriteLine(usage);
				Environment.Exit(0);
			}
			
			String index = "index";
			String field = "contents";
			String queries = null;
			int repeat = 0;
			bool raw = false;
			String normsField = null;
			bool paging = true;
			int hitsPerPage = 10;
			
			for (int i = 0; i < args.Length; i++)
			{
				if ("-index".Equals(args[i]))
				{
					index = args[i + 1];
					i++;
				}
				else if ("-field".Equals(args[i]))
				{
					field = args[i + 1];
					i++;
				}
				else if ("-queries".Equals(args[i]))
				{
					queries = args[i + 1];
					i++;
				}
				else if ("-repeat".Equals(args[i]))
				{
					repeat = Int32.Parse(args[i + 1]);
					i++;
				}
				else if ("-raw".Equals(args[i]))
				{
					raw = true;
				}
				else if ("-norms".Equals(args[i]))
				{
					normsField = args[i + 1];
					i++;
				}
				else if ("-paging".Equals(args[i]))
				{
					if (args[i + 1].Equals("false"))
					{
						paging = false;
					}
					else
					{
						hitsPerPage = Int32.Parse(args[i + 1]);
						if (hitsPerPage == 0)
						{
							paging = false;
						}
					}
					i++;
				}
			}

		    IndexReader indexReader = null;
            try
            {
                // only searching, so read-only=true
                indexReader = IndexReader.Open(FSDirectory.Open(new System.IO.DirectoryInfo(index)), true); // only searching, so read-only=true

			    if (normsField != null)
				    indexReader = new OneNormsReader(indexReader, normsField);
			
			    Searcher searcher = new IndexSearcher(indexReader);
			    Analyzer analyzer = new StandardAnalyzer(Version.LUCENE_30);
			
			    StreamReader queryReader;
			    if (queries != null)
			    {
				    queryReader = new StreamReader(new StreamReader(queries, Encoding.Default).BaseStream, new StreamReader(queries, Encoding.Default).CurrentEncoding);
			    }
			    else
			    {
				    queryReader = new StreamReader(new StreamReader(Console.OpenStandardInput(), Encoding.UTF8).BaseStream, new StreamReader(Console.OpenStandardInput(), Encoding.UTF8).CurrentEncoding);
			    }

                var parser = new QueryParser(Version.LUCENE_30, field, analyzer);
			    while (true)
			    {
				    if (queries == null)
				    // prompt the user
					    Console.Out.WriteLine("Enter query: ");
				
				    String line = queryReader.ReadLine();
				
				    if (line == null || line.Length == - 1)
					    break;
				
				    line = line.Trim();
				    if (line.Length == 0)
					    break;
				
				    Query query = parser.Parse(line);
				    Console.Out.WriteLine("Searching for: " + query.ToString(field));
				
				    if (repeat > 0)
				    {
					    // repeat & time as benchmark
					    DateTime start = DateTime.Now;
					    for (int i = 0; i < repeat; i++)
					    {
						    searcher.Search(query, null, 100);
					    }
					    DateTime end = DateTime.Now;
					    Console.Out.WriteLine("Time: " + (end.Millisecond - start.Millisecond) + "ms");
				    }
				
				    if (paging)
				    {
					    DoPagingSearch(queryReader, searcher, query, hitsPerPage, raw, queries == null);
				    }
				    else
				    {
					    DoStreamingSearch(searcher, query);
				    }
			    }
			    queryReader.Close();
            } 
            finally 
            {
                if (indexReader != null)
                {
                    indexReader.Dispose();
                }
            }
		}
		
		/// <summary>
		/// This method uses a custom HitCollector implementation which simply prints out
		/// the docId and score of every matching document. 
		/// 
		/// This simulates the streaming search use case, where all hits are supposed to
		/// be processed, regardless of their relevance.
		/// </summary>
		public static void  DoStreamingSearch(Searcher searcher, Query query)
		{
			Collector streamingHitCollector = new AnonymousClassCollector();
			searcher.Search(query, streamingHitCollector);
		}
		
		/// <summary> This demonstrates a typical paging search scenario, where the search engine presents 
		/// pages of size n to the user. The user can then go to the next page if interested in
		/// the next hits.
		/// 
		/// When the query is executed for the first time, then only enough results are collected
		/// to fill 5 result pages. If the user wants to page beyond this limit, then the query
		/// is executed another time and all hits are collected.
		/// 
		/// </summary>
		public static void  DoPagingSearch(StreamReader input, Searcher searcher, Query query, int hitsPerPage, bool raw, bool interactive)
		{
			
			// Collect enough docs to show 5 pages
			var collector = TopScoreDocCollector.Create(5 * hitsPerPage, false);
			searcher.Search(query, collector);
			var hits = collector.TopDocs().ScoreDocs;
			
			int numTotalHits = collector.TotalHits;
			Console.Out.WriteLine(numTotalHits + " total matching documents");
			
			int start = 0;
			int end = Math.Min(numTotalHits, hitsPerPage);
			
			while (true)
			{
				if (end > hits.Length)
				{
					Console.Out.WriteLine("Only results 1 - " + hits.Length + " of " + numTotalHits + " total matching documents collected.");
					Console.Out.WriteLine("Collect more (y/n) ?");
					String line = input.ReadLine();
					if (String.IsNullOrEmpty(line) || line[0] == 'n')
					{
						break;
					}
					
					collector = TopScoreDocCollector.Create(numTotalHits, false);
					searcher.Search(query, collector);
					hits = collector.TopDocs().ScoreDocs;
				}
				
				end = Math.Min(hits.Length, start + hitsPerPage);
				
				for (int i = start; i < end; i++)
				{
					if (raw)
					{
						// output raw format
						Console.Out.WriteLine("doc=" + hits[i].Doc + " score=" + hits[i].Score);
						continue;
					}
					
					Document doc = searcher.Doc(hits[i].Doc);
					String path = doc.Get("path");
					if (path != null)
					{
						Console.Out.WriteLine((i + 1) + ". " + path);
						String title = doc.Get("title");
						if (title != null)
						{
							Console.Out.WriteLine("   Title: " + doc.Get("title"));
						}
					}
					else
					{
						Console.Out.WriteLine((i + 1) + ". " + "No path for this document");
					}
				}
				
				if (!interactive)
				{
					break;
				}
				
				if (numTotalHits >= end)
				{
					bool quit = false;
					while (true)
					{
						Console.Out.Write("Press ");
						if (start - hitsPerPage >= 0)
						{
							Console.Out.Write("(p)revious page, ");
						}
						if (start + hitsPerPage < numTotalHits)
						{
							Console.Out.Write("(n)ext page, ");
						}
						Console.Out.WriteLine("(q)uit or enter number to jump to a page.");
						
						String line = input.ReadLine();
						if (String.IsNullOrEmpty(line) || line[0] == 'q')
						{
							quit = true;
							break;
						}
						if (line[0] == 'p')
						{
							start = Math.Max(0, start - hitsPerPage);
							break;
						}
						else if (line[0] == 'n')
						{
							if (start + hitsPerPage < numTotalHits)
							{
								start += hitsPerPage;
							}
							break;
						}
						else
						{
							int page = Int32.Parse(line);
							if ((page - 1) * hitsPerPage < numTotalHits)
							{
								start = (page - 1) * hitsPerPage;
								break;
							}
							else
							{
								Console.Out.WriteLine("No such page");
							}
						}
					}
					if (quit)
						break;
					end = Math.Min(numTotalHits, start + hitsPerPage);
				}
			}
		}
	}
}