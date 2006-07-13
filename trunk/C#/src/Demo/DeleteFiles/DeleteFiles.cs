/* 
 * Copyright 2004 The Apache Software Foundation
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
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
using IndexReader = Lucene.Net.Index.IndexReader;
using Term = Lucene.Net.Index.Term;
using Directory = Lucene.Net.Store.Directory;
using FSDirectory = Lucene.Net.Store.FSDirectory;

namespace Lucene.Net.Demo
{
	//import Lucene.Net.index.Term;
	
    /// <summary>Deletes documents from an index that do not contain a term. </summary>
    class DeleteFiles
	{

        private DeleteFiles()
        {
        } // singleton
		
		[STAThread]
		public static void  Main(System.String[] args)
		{
			System.String usage = typeof(DeleteFiles) + " <unique_term>";
			if (args.Length == 0)
			{
				System.Console.Error.WriteLine("Usage: " + usage);
				System.Environment.Exit(1);
			}
			try
			{
				Directory directory = FSDirectory.GetDirectory("index", false);
				IndexReader reader = IndexReader.Open(directory);
				
				Term term = new Term("path", args[0]);
				int deleted = reader.Delete(term);
				
				System.Console.Out.WriteLine("deleted " + deleted + " documents containing " + term);
				
				// one can also delete documents by their internal id:
				/*
				for (int i = 0; i < reader.maxDoc(); i++) {
				System.out.println("Deleting document with id " + i);
				reader.delete(i);
				}*/
				
				reader.Close();
				directory.Close();
			}
			catch (System.Exception e)
			{
				System.Console.Out.WriteLine(" caught a " + e.GetType() + "\n with message: " + e.Message);
			}
		}
	}
}