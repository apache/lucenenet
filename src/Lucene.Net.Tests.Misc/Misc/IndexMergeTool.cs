/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Org.Apache.Lucene.Index;
using Org.Apache.Lucene.Misc;
using Org.Apache.Lucene.Store;
using Org.Apache.Lucene.Util;
using Sharpen;

namespace Org.Apache.Lucene.Misc
{
	/// <summary>
	/// Merges indices specified on the command line into the index
	/// specified as the first command line argument.
	/// </summary>
	/// <remarks>
	/// Merges indices specified on the command line into the index
	/// specified as the first command line argument.
	/// </remarks>
	public class IndexMergeTool
	{
		/// <exception cref="System.IO.IOException"></exception>
		public static void Main(string[] args)
		{
			if (args.Length < 3)
			{
				System.Console.Error.WriteLine("Usage: IndexMergeTool <mergedIndex> <index1> <index2> [index3] ..."
					);
				System.Environment.Exit(1);
			}
			FSDirectory mergedIndex = FSDirectory.Open(new FilePath(args[0]));
			IndexWriter writer = new IndexWriter(mergedIndex, new IndexWriterConfig(Version.LUCENE_CURRENT
				, null).SetOpenMode(IndexWriterConfig.OpenMode.CREATE));
			Directory[] indexes = new Directory[args.Length - 1];
			for (int i = 1; i < args.Length; i++)
			{
				indexes[i - 1] = FSDirectory.Open(new FilePath(args[i]));
			}
			System.Console.Out.WriteLine("Merging...");
			writer.AddIndexes(indexes);
			System.Console.Out.WriteLine("Full merge...");
			writer.ForceMerge(1);
			writer.Close();
			System.Console.Out.WriteLine("Done.");
		}
	}
}
