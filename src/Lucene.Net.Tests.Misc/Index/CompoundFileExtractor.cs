/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.IO;
using Org.Apache.Lucene.Index;
using Org.Apache.Lucene.Store;
using Org.Apache.Lucene.Util;
using Sharpen;

namespace Org.Apache.Lucene.Index
{
	/// <summary>Command-line tool for extracting sub-files out of a compound file.</summary>
	/// <remarks>Command-line tool for extracting sub-files out of a compound file.</remarks>
	public class CompoundFileExtractor
	{
		public static void Main(string[] args)
		{
			string filename = null;
			bool extract = false;
			string dirImpl = null;
			int j = 0;
			while (j < args.Length)
			{
				string arg = args[j];
				if ("-extract".Equals(arg))
				{
					extract = true;
				}
				else
				{
					if ("-dir-impl".Equals(arg))
					{
						if (j == args.Length - 1)
						{
							System.Console.Out.WriteLine("ERROR: missing value for -dir-impl option");
							System.Environment.Exit(1);
						}
						j++;
						dirImpl = args[j];
					}
					else
					{
						if (filename == null)
						{
							filename = arg;
						}
					}
				}
				j++;
			}
			if (filename == null)
			{
				System.Console.Out.WriteLine("Usage: org.apache.lucene.index.CompoundFileExtractor [-extract] [-dir-impl X] <cfsfile>"
					);
				return;
			}
			Directory dir = null;
			CompoundFileDirectory cfr = null;
			IOContext context = IOContext.READ;
			try
			{
				FilePath file = new FilePath(filename);
				string dirname = file.GetAbsoluteFile().GetParent();
				filename = file.GetName();
				if (dirImpl == null)
				{
					dir = FSDirectory.Open(new FilePath(dirname));
				}
				else
				{
					dir = CommandLineUtil.NewFSDirectory(dirImpl, new FilePath(dirname));
				}
				cfr = new CompoundFileDirectory(dir, filename, IOContext.DEFAULT, false);
				string[] files = cfr.ListAll();
				ArrayUtil.TimSort(files);
				// sort the array of filename so that the output is more readable
				for (int i = 0; i < files.Length; ++i)
				{
					long len = cfr.FileLength(files[i]);
					if (extract)
					{
						System.Console.Out.WriteLine("extract " + files[i] + " with " + len + " bytes to local directory..."
							);
						IndexInput ii = cfr.OpenInput(files[i], context);
						FileOutputStream f = new FileOutputStream(files[i]);
						// read and write with a small buffer, which is more effective than reading byte by byte
						byte[] buffer = new byte[1024];
						int chunk = buffer.Length;
						while (len > 0)
						{
							int bufLen = (int)Math.Min(chunk, len);
							ii.ReadBytes(buffer, 0, bufLen);
							f.Write(buffer, 0, bufLen);
							len -= bufLen;
						}
						f.Close();
						ii.Close();
					}
					else
					{
						System.Console.Out.WriteLine(files[i] + ": " + len + " bytes");
					}
				}
			}
			catch (IOException ioe)
			{
				Sharpen.Runtime.PrintStackTrace(ioe);
			}
			finally
			{
				try
				{
					if (dir != null)
					{
						dir.Close();
					}
					if (cfr != null)
					{
						cfr.Close();
					}
				}
				catch (IOException ioe)
				{
					Sharpen.Runtime.PrintStackTrace(ioe);
				}
			}
		}
	}
}
