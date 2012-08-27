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
using Lucene.Net.Index;
using FSDirectory = Lucene.Net.Store.FSDirectory;

namespace Lucene.Net.Demo
{
	/// <summary>Deletes documents from an index that do not contain a term. </summary>
	public static class DeleteFiles
	{
		
		/// <summary>Deletes documents from an index that do not contain a term. </summary>
		[STAThread]
		public static void Main(System.String[] args)
		{
			var usage = typeof(DeleteFiles) + " <unique_term>";
			if (args.Length == 0)
			{
				Console.Error.WriteLine("Usage: " + usage);
				Environment.Exit(1);
			}

			try
			{
                // We don't want a read-only reader because we are about to delete.
				using (var directory = FSDirectory.Open("index"))
                using (var reader = IndexReader.Open(directory, false))
                {
                    var term = new Term("path", args[0]);
                    var deleted = reader.DeleteDocuments(term);

                    Console.Out.WriteLine("deleted " + deleted + " documents containing " + term);

                    // one can also delete documents by their internal id:
                    /*
                    for (int i = 0; i < reader.MaxDoc; i++) {
                        Console.Out.WriteLine("Deleting document with id " + i);
                        reader.DeleteDocument(i);
                    }
                    */

                    reader.Commit();
                }
			}
			catch (Exception e)
			{
				Console.Out.WriteLine(" caught a " + e.GetType() + "\n with message: " + e.Message);
			}
		}
	}
}