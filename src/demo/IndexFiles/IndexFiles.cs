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
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Index;

using FSDirectory = Lucene.Net.Store.FSDirectory;
using Version = Lucene.Net.Util.Version;

namespace Lucene.Net.Demo
{
	
	/// <summary>Index all text files under a directory. </summary>
	public static class IndexFiles
	{
        internal static readonly DirectoryInfo INDEX_DIR = new DirectoryInfo("index");
		
		/// <summary>Index all text files under a directory. </summary>
		[STAThread]
		public static void Main(String[] args)
		{
			var usage = typeof(IndexFiles) + " <root_directory>";
			if (args.Length == 0)
			{
				Console.Error.WriteLine("Usage: " + usage);
				Environment.Exit(1);
			}

			if (File.Exists(INDEX_DIR.FullName) || Directory.Exists(INDEX_DIR.FullName))
			{
				Console.Out.WriteLine("Cannot save index to '" + INDEX_DIR + "' directory, please delete it first");
				Environment.Exit(1);
			}

            var docDir = new DirectoryInfo(args[0]);
		    var docDirExists = File.Exists(docDir.FullName) || Directory.Exists(docDir.FullName);
			if (!docDirExists) // || !docDir.canRead()) // {{Aroush}} what is canRead() in C#?
			{
				Console.Out.WriteLine("Document directory '" + docDir.FullName + "' does not exist or is not readable, please check the path");
				Environment.Exit(1);
			}
			
			var start = DateTime.Now;
			try
			{
                using (var writer = new IndexWriter(FSDirectory.Open(INDEX_DIR), new StandardAnalyzer(Version.LUCENE_30), true, IndexWriter.MaxFieldLength.LIMITED))
                {
                    Console.Out.WriteLine("Indexing to directory '" + INDEX_DIR + "'...");
                    IndexDirectory(writer, docDir);
                    Console.Out.WriteLine("Optimizing...");
                    writer.Optimize();
                    writer.Commit();
                }
			    var end = DateTime.Now;
				Console.Out.WriteLine(end.Millisecond - start.Millisecond + " total milliseconds");
			}
			catch (IOException e)
			{
				Console.Out.WriteLine(" caught a " + e.GetType() + "\n with message: " + e.Message);
			}
		}
		
        internal static void IndexDirectory(IndexWriter writer, DirectoryInfo directory)
        {
            foreach(var subDirectory in directory.GetDirectories())
                IndexDirectory(writer, subDirectory);

            foreach (var file in directory.GetFiles())
                IndexDocs(writer, file);
        }

		internal static void IndexDocs(IndexWriter writer, FileInfo file)
		{
			Console.Out.WriteLine("adding " + file);

			try
			{
				writer.AddDocument(FileDocument.Document(file));
			}
			catch (FileNotFoundException)
			{
                // At least on Windows, some temporary files raise this exception with an
                // "access denied" message checking if the file can be read doesn't help.
			}
            catch (UnauthorizedAccessException)
            {
                // Handle any access-denied errors that occur while reading the file.    
            }
            catch (IOException)
            {
                // Generic handler for any io-related exceptions that occur.
            }
		}
	}
}