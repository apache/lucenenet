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
using System.Diagnostics;
using System.IO;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Index;

using FSDirectory = Lucene.Net.Store.FSDirectory;
using Version = Lucene.Net.Util.Version;

namespace Lucene.Net.Demo
{
	
	/// <summary>Indexer for HTML files. </summary>
	public static class IndexHTML
	{
	    
		/// <summary>Indexer for HTML files.</summary>
		[STAThread]
		public static void Main(System.String[] argv)
		{
			try
			{
                var index = new DirectoryInfo("index");
				bool create = false;
                DirectoryInfo root = null;
				
				var usage = "IndexHTML [-create] [-index <index>] <root_directory>";
				
				if (argv.Length == 0)
				{
					Console.Error.WriteLine("Usage: " + usage);
					return ;
				}
				
				for (int i = 0; i < argv.Length; i++)
				{
					if (argv[i].Equals("-index"))
					{
						// parse -index option
                        index = new DirectoryInfo(argv[++i]);
					}
					else if (argv[i].Equals("-create"))
					{
						// parse -create option
						create = true;
					}
					else if (i != argv.Length - 1)
					{
						Console.Error.WriteLine("Usage: " + usage);
						return ;
					}
					else
                        root = new DirectoryInfo(argv[i]);
				}
				
				if (root == null)
				{
					Console.Error.WriteLine("Specify directory to index");
					Console.Error.WriteLine("Usage: " + usage);
					return ;
				}
				
				var start = DateTime.Now;

                using (var writer = new IndexWriter(FSDirectory.Open(index), new StandardAnalyzer(Version.LUCENE_30), create, new IndexWriter.MaxFieldLength(1000000)))
                {
				    if (!create)
				    {
					    // We're not creating a new index, iterate our index and remove
                        // any stale documents.
					    IndexDocs(writer, root, index, Operation.RemoveStale);
				    }

                    var operation = create 
                        ? Operation.CompleteReindex 
                        : Operation.IncrementalReindex;
                    IndexDocs(writer, root, index, operation); // add new docs

                    Console.Out.WriteLine("Optimizing index...");
                    writer.Optimize();
                    writer.Commit();
                }

			    var end = DateTime.Now;
				
				Console.Out.Write(end.Millisecond - start.Millisecond);
				Console.Out.WriteLine(" total milliseconds");
			}
			catch (Exception e)
			{
				Console.Error.WriteLine(e.StackTrace);
			}
		}
		
		/* Walk directory hierarchy in uid order, while keeping uid iterator from
		/* existing index in sync.  Mismatches indicate one of: (a) old documents to
		/* be deleted; (b) unchanged documents, to be left alone; or (c) new
		/* documents, to be indexed.
		*/

        private static void IndexDocs(IndexWriter writer, DirectoryInfo file, DirectoryInfo index, Operation operation)
		{
            if (operation == Operation.CompleteReindex) 
            {
                // Perform a full reindexing.
                IndexDirectory(writer, null, file, operation);
            }
            else
            {
                // Perform an incremental reindexing.

                using (var reader = IndexReader.Open(FSDirectory.Open(index), true)) // open existing index
                using (var uidIter = reader.Terms(new Term("uid", ""))) // init uid iterator
                {
                    IndexDirectory(writer, uidIter, file, operation);

                    if (operation == Operation.RemoveStale) {
                        // Delete remaining, presumed stale, documents. This works since
                        // the above call to IndexDirectory should have positioned the uidIter
                        // after any uids matching existing documents. Any remaining uid
                        // is remains from documents that has been deleted since they was
                        // indexed.
                        while (uidIter.Term != null && uidIter.Term.Field == "uid") {
                            Console.Out.WriteLine("deleting " + HTMLDocument.Uid2url(uidIter.Term.Text));
                            writer.DeleteDocuments(uidIter.Term);
                            uidIter.Next();
                        }
                    }
                }
            }
		}

        private static void IndexDirectory(IndexWriter writer, TermEnum uidIter, DirectoryInfo dir, Operation operation) {
            var entries = Directory.GetFileSystemEntries(dir.FullName);

            // Sort the entries. This is important, the uidIter TermEnum is
            // iterated in a forward-only fashion, requiring all files to be
            // passed in ascending order.
            Array.Sort(entries);

            foreach (var entry in entries) {
                var path = Path.Combine(dir.FullName, entry);
                if (Directory.Exists(path)) {
                    IndexDirectory(writer, uidIter, new DirectoryInfo(path), operation);
                } else if (File.Exists(path)) {
                    IndexFile(writer, uidIter, new FileInfo(path), operation);
                }
            }
        }

        private static void IndexFile(IndexWriter writer, TermEnum uidIter, FileInfo file, Operation operation)
		{
			if (file.FullName.EndsWith(".html") || file.FullName.EndsWith(".htm") || file.FullName.EndsWith(".txt"))
			{
				// We've found a file we should index.
				
				if (operation == Operation.IncrementalReindex ||
                    operation == Operation.RemoveStale)
				{
                    // We should only get here with an open uidIter.
                    Debug.Assert(uidIter != null, "Expected uidIter != null for operation " + operation);

					var uid = HTMLDocument.Uid(file); // construct uid for doc
					
					while (uidIter.Term != null && uidIter.Term.Field == "uid" && String.CompareOrdinal(uidIter.Term.Text, uid) < 0)
					{
						if (operation == Operation.RemoveStale)
						{
							Console.Out.WriteLine("deleting " + HTMLDocument.Uid2url(uidIter.Term.Text));
							writer.DeleteDocuments(uidIter.Term);
						}
						uidIter.Next();
					}

                    // The uidIter TermEnum should now be pointing at either
                    //  1) a null term, meaning there are no more uids to check.
                    //  2) a term matching the current file.
                    //  3) a term not matching us.
                    if (uidIter.Term != null && uidIter.Term.Field == "uid" && String.CompareOrdinal(uidIter.Term.Text, uid) == 0)
					{
                        // uidIter points to the current document, we should move one
                        // step ahead to keep state consistant, and carry on.
						uidIter.Next();
					}
					else if (operation == Operation.IncrementalReindex)
					{
                        // uidIter does not point to the current document, and we're
                        // currently indexing documents.
						var doc = HTMLDocument.Document(file);
						Console.Out.WriteLine("adding " + doc.Get("path"));
						writer.AddDocument(doc);
					}
				}
				else
				{
                    // We're doing a complete reindexing. We aren't using uidIter,
                    // but for completeness we assert that it's null (as expected).
                    Debug.Assert(uidIter == null, "Expected uidIter == null for operation == " + operation);

					var doc = HTMLDocument.Document(file);
					Console.Out.WriteLine("adding " + doc.Get("path"));
					writer.AddDocument(doc);
				}
			}
		}

        private enum Operation {
            /// <summary>
            ///   Indicates an incremental indexing.
            /// </summary>
            IncrementalReindex,

            /// <summary>
            ///   Indicates that stale entries in the index should be removed.
            /// </summary>
            RemoveStale,

            /// <summary>
            ///   Indicates an complete reindexing.
            /// </summary>
            CompleteReindex
        }
	}
}