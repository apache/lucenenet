/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Globalization;
using Org.Apache.Lucene.Index;
using Org.Apache.Lucene.Store;
using Sharpen;

namespace Org.Apache.Lucene.Misc
{
	/// <summary>Utility to get document frequency and total number of occurrences (sum of the tf for each doc)  of a term.
	/// 	</summary>
	/// <remarks>Utility to get document frequency and total number of occurrences (sum of the tf for each doc)  of a term.
	/// 	</remarks>
	public class GetTermInfo
	{
		/// <exception cref="System.Exception"></exception>
		public static void Main(string[] args)
		{
			FSDirectory dir = null;
			string inputStr = null;
			string field = null;
			if (args.Length == 3)
			{
				dir = FSDirectory.Open(new FilePath(args[0]));
				field = args[1];
				inputStr = args[2];
			}
			else
			{
				Usage();
				System.Environment.Exit(1);
			}
			GetTermInfo(dir, new Term(field, inputStr));
		}

		/// <exception cref="System.Exception"></exception>
		public static void GetTermInfo(Directory dir, Term term)
		{
			IndexReader reader = DirectoryReader.Open(dir);
			System.Console.Out.Printf(CultureInfo.ROOT, "%s:%s \t totalTF = %,d \t doc freq = %,d \n"
				, term.Field(), term.Text(), reader.TotalTermFreq(term), reader.DocFreq(term));
		}

		private static void Usage()
		{
			System.Console.Out.WriteLine("\n\nusage:\n\t" + "java " + typeof(Org.Apache.Lucene.Misc.GetTermInfo
				).FullName + " <index dir> field term \n\n");
		}
	}
}
