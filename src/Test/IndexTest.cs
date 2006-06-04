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
using SimpleAnalyzer = Lucene.Net.Analysis.SimpleAnalyzer;
using FileDocument = Lucene.Net.Demo.FileDocument;
using IndexWriter = Lucene.Net.Index.IndexWriter;

namespace Lucene.Net
{
	
	class IndexTest
	{
		[STAThread]
		public static void  Main(System.String[] args)
		{
			try
			{
				System.DateTime start = System.DateTime.Now;
				System.String tempFileName = System.IO.Path.GetTempFileName();
				System.String tempDirectory = System.IO.Path.GetDirectoryName(tempFileName);
				tempFileName = System.IO.Path.GetFileName(tempFileName);
				IndexWriter writer = new IndexWriter(System.IO.Path.Combine(tempDirectory, "luceneTest") + tempFileName + ".idx", new SimpleAnalyzer(), true);

				writer.SetMergeFactor(20);
				
				IndexDocs(writer, new System.IO.FileInfo("/tmp"));
				
				writer.Optimize();
				writer.Close();
				
				System.DateTime end = System.DateTime.Now;
				
				System.Console.Out.Write(end.Ticks - start.Ticks);
				System.Console.Out.WriteLine(" total milliseconds");
				
				System.Diagnostics.Process runtime = System.Diagnostics.Process.GetCurrentProcess();
				
				// System.Console.Out.Write(java.lang.Runtime.freeMemory());    // {{Aroush}} how do we get freeMemory() in .NET?
				System.Console.Out.WriteLine(" free memory before gc");
				// System.Console.Out.Write(java.lang.Runtime.totalMemory());   // {{Aroush}} how do we get totalMemory() in .NET?
				System.Console.Out.WriteLine(" total memory before gc");
				
				System.GC.Collect();
				
				// System.Console.Out.Write(java.lang.Runtime.freeMemory());       // {{Aroush}} how do we get freeMemory() in .NET?
				System.Console.Out.WriteLine(" free memory after gc");
				// System.Console.Out.Write(java.lang.Runtime.totalMemory());    // {{Aroush}} how do we get totalMemory() in .NET?
				System.Console.Out.WriteLine(" total memory after gc");
			}
			catch (System.Exception e)
			{
				System.Console.Out.WriteLine(" caught a " + e.GetType() + "\n with message: " + e.Message);
			}
		}
		
		public static void  IndexDocs(IndexWriter writer, System.IO.FileInfo file)
		{
			if (System.IO.Directory.Exists(file.FullName))
			{
				System.String[] files = System.IO.Directory.GetFileSystemEntries(file.FullName);
				for (int i = 0; i < files.Length; i++)
					IndexDocs(writer, new System.IO.FileInfo(file.FullName + "\\" + files[i]));
			}
			else
			{
				System.Console.Out.WriteLine("adding " + file);
				writer.AddDocument(FileDocument.Document(file));
			}
		}
	}
}