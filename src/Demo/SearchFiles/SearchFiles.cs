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

using Document = Lucene.Net.Documents.Document;
using FilterIndexReader = Lucene.Net.Index.FilterIndexReader;
using IndexReader = Lucene.Net.Index.IndexReader;
using QueryParser = Lucene.Net.QueryParsers.QueryParser;
using Analyzer = Lucene.Net.Analysis.Analyzer;
using StandardAnalyzer = Lucene.Net.Analysis.Standard.StandardAnalyzer;
using Hits = Lucene.Net.Search.Hits;
using IndexSearcher = Lucene.Net.Search.IndexSearcher;
using Query = Lucene.Net.Search.Query;
using Searcher = Lucene.Net.Search.Searcher;

namespace Lucene.Net.Demo
{
	
	/// <summary>Simple command-line based search demo. </summary>
	public class SearchFiles
	{
		
		/// <summary>Use the norms from one field for all fields.  Norms are read into memory,
		/// using a byte of memory per document per searched field.  This can cause
		/// search of large collections with a large number of fields to run out of
		/// memory.  If all of the fields contain only a single token, then the norms
		/// are all identical, then single norm vector may be shared. 
		/// </summary>
		private class OneNormsReader:FilterIndexReader
		{
			private System.String field;
			
			public OneNormsReader(IndexReader in_Renamed, System.String field) : base(in_Renamed)
			{
				this.field = field;
			}
			
			public override byte[] Norms(System.String field)
			{
				return in_Renamed.Norms(this.field);
			}
		}
		
		private SearchFiles()
		{
		}
		
		/// <summary>Simple command-line based search demo. </summary>
		[STAThread]
		public static void  Main(System.String[] args)
		{
			System.String usage = "Usage: " + typeof(SearchFiles) + " [-index dir] [-field f] [-repeat n] [-queries file] [-raw] [-norms field]";
			if (args.Length > 0 && ("-h".Equals(args[0]) || "-help".Equals(args[0])))
			{
				System.Console.Out.WriteLine(usage);
				System.Environment.Exit(0);
			}
			
			System.String index = "index";
			System.String field = "contents";
			System.String queries = null;
			int repeat = 0;
			bool raw = false;
			System.String normsField = null;
			
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
					repeat = System.Int32.Parse(args[i + 1]);
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
			}
			
			IndexReader reader = IndexReader.Open(index);
			
			if (normsField != null)
				reader = new OneNormsReader(reader, normsField);
			
			Searcher searcher = new IndexSearcher(reader);
			Analyzer analyzer = new StandardAnalyzer();
			
			System.IO.StreamReader in_Renamed = null;
			if (queries != null)
			{
				in_Renamed = new System.IO.StreamReader(new System.IO.StreamReader(queries, System.Text.Encoding.Default).BaseStream, new System.IO.StreamReader(queries, System.Text.Encoding.Default).CurrentEncoding);
			}
			else
			{
				in_Renamed = new System.IO.StreamReader(new System.IO.StreamReader(System.Console.OpenStandardInput(), System.Text.Encoding.GetEncoding("UTF-8")).BaseStream, new System.IO.StreamReader(System.Console.OpenStandardInput(), System.Text.Encoding.GetEncoding("UTF-8")).CurrentEncoding);
			}
			QueryParser parser = new QueryParser(field, analyzer);
			while (true)
			{
				if (queries == null)
                    // prompt the user
					System.Console.Out.Write("Query: ");
				
				System.String line = in_Renamed.ReadLine();
				
				if (line == null || line.Length == 0)
					break;
				
				Query query = parser.Parse(line);
				System.Console.Out.WriteLine("Searching for: " + query.ToString(field));
				
				Hits hits = searcher.Search(query);
				
				if (repeat > 0)
				{
					// repeat & time as benchmark
					System.DateTime start = System.DateTime.Now;
					for (int i = 0; i < repeat; i++)
					{
						hits = searcher.Search(query);
					}
					System.DateTime end = System.DateTime.Now;
					System.Console.Out.WriteLine("Time: " + (end.Millisecond - start.Millisecond) + "ms");
				}
				
				System.Console.Out.WriteLine(hits.Length() + " total matching documents");
				
				int HITS_PER_PAGE = 10;
				for (int start = 0; start < hits.Length(); start += HITS_PER_PAGE)
				{
					int end = System.Math.Min(hits.Length(), start + HITS_PER_PAGE);
					for (int i = start; i < end; i++)
					{
						
						if (raw)
						{
							// output raw format
							System.Console.Out.WriteLine("doc=" + hits.Id(i) + " score=" + hits.Score(i));
							continue;
						}
						
						Document doc = hits.Doc(i);
						System.String path = doc.Get("path");
						if (path != null)
						{
							System.Console.Out.WriteLine((i + 1) + ". " + path);
							System.String title = doc.Get("title");
							if (title != null)
							{
								System.Console.Out.WriteLine("   Title: " + doc.Get("title"));
							}
						}
						else
						{
							System.Console.Out.WriteLine((i + 1) + ". " + "No path for this document");
						}
					}
					
					if (queries != null)
					// non-interactive
						break;
					
					if (hits.Length() > end)
					{
						System.Console.Out.Write("more (y/n) ? ");
						line = in_Renamed.ReadLine();
						if (line.Length == 0 || line[0] == 'n')
							break;
					}
				}
			}
			reader.Close();
		}
	}
}
