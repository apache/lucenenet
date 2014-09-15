using System;

namespace org.apache.lucene.misc
{

	/// <summary>
	/// Copyright 2005 The Apache Software Foundation
	///  
	/// Licensed under the Apache License, Version 2.0 (the "License");
	/// you may not use this file except in compliance with the License.
	/// You may obtain a copy of the License at
	///  
	///     http://www.apache.org/licenses/LICENSE-2.0
	///  
	/// Unless required by applicable law or agreed to in writing, software
	/// distributed under the License is distributed on an "AS IS" BASIS,
	/// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
	/// See the License for the specific language governing permissions and
	/// limitations under the License.
	/// </summary>

	using IndexWriter = org.apache.lucene.index.IndexWriter;
	using IndexWriterConfig = org.apache.lucene.index.IndexWriterConfig;
	using OpenMode = org.apache.lucene.index.IndexWriterConfig.OpenMode;
	using Directory = org.apache.lucene.store.Directory;
	using FSDirectory = org.apache.lucene.store.FSDirectory;
	using Version = org.apache.lucene.util.Version;


	/// <summary>
	/// Merges indices specified on the command line into the index
	/// specified as the first command line argument.
	/// </summary>
	public class IndexMergeTool
	{
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public static void main(String[] args) throws java.io.IOException
	  public static void Main(string[] args)
	  {
		if (args.Length < 3)
		{
		  Console.Error.WriteLine("Usage: IndexMergeTool <mergedIndex> <index1> <index2> [index3] ...");
		  Environment.Exit(1);
		}
		FSDirectory mergedIndex = FSDirectory.open(new File(args[0]));

		IndexWriter writer = new IndexWriter(mergedIndex, new IndexWriterConfig(Version.LUCENE_CURRENT, null)
		   .setOpenMode(IndexWriterConfig.OpenMode.CREATE));

		Directory[] indexes = new Directory[args.Length - 1];
		for (int i = 1; i < args.Length; i++)
		{
		  indexes[i - 1] = FSDirectory.open(new File(args[i]));
		}

		Console.WriteLine("Merging...");
		writer.addIndexes(indexes);

		Console.WriteLine("Full merge...");
		writer.forceMerge(1);
		writer.close();
		Console.WriteLine("Done.");
	  }
	}

}