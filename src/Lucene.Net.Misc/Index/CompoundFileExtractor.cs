using System;

namespace org.apache.lucene.index
{

	/*
	 * Licensed to the Apache Software Foundation (ASF) under one or more
	 * contributor license agreements.  See the NOTICE file distributed with
	 * this work for additional information regarding copyright ownership.
	 * The ASF licenses this file to You under the Apache License, Version 2.0
	 * (the "License"); you may not use this file except in compliance with
	 * the License.  You may obtain a copy of the License at
	 *
	 *     http://www.apache.org/licenses/LICENSE-2.0
	 *
	 * Unless required by applicable law or agreed to in writing, software
	 * distributed under the License is distributed on an "AS IS" BASIS,
	 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
	 * See the License for the specific language governing permissions and
	 * limitations under the License.
	 */

	/// <summary>
	/// Prints the filename and size of each file within a given compound file.
	/// Add the -extract flag to extract files to the current working directory.
	/// In order to make the extracted version of the index work, you have to copy
	/// the segments file from the compound index into the directory where the extracted files are stored. </summary>
	/// <param name="args"> Usage: org.apache.lucene.index.IndexReader [-extract] &lt;cfsfile&gt; </param>


	using CompoundFileDirectory = org.apache.lucene.store.CompoundFileDirectory;
	using Directory = org.apache.lucene.store.Directory;
	using FSDirectory = org.apache.lucene.store.FSDirectory;
	using IOContext = org.apache.lucene.store.IOContext;
	using IndexInput = org.apache.lucene.store.IndexInput;
	using ArrayUtil = org.apache.lucene.util.ArrayUtil;
	using CommandLineUtil = org.apache.lucene.util.CommandLineUtil;

	/// <summary>
	/// Command-line tool for extracting sub-files out of a compound file.
	/// </summary>
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
		  else if ("-dir-impl".Equals(arg))
		  {
			if (j == args.Length - 1)
			{
			  Console.WriteLine("ERROR: missing value for -dir-impl option");
			  Environment.Exit(1);
			}
			j++;
			dirImpl = args[j];
		  }
		  else if (filename == null)
		  {
			filename = arg;
		  }
		  j++;
		}

		if (filename == null)
		{
		  Console.WriteLine("Usage: org.apache.lucene.index.CompoundFileExtractor [-extract] [-dir-impl X] <cfsfile>");
		  return;
		}

		Directory dir = null;
		CompoundFileDirectory cfr = null;
		IOContext context = IOContext.READ;

		try
		{
		  File file = new File(filename);
		  string dirname = file.AbsoluteFile.Parent;
		  filename = file.Name;
		  if (dirImpl == null)
		  {
			dir = FSDirectory.open(new File(dirname));
		  }
		  else
		  {
			dir = CommandLineUtil.newFSDirectory(dirImpl, new File(dirname));
		  }

		  cfr = new CompoundFileDirectory(dir, filename, IOContext.DEFAULT, false);

		  string[] files = cfr.listAll();
		  ArrayUtil.timSort(files); // sort the array of filename so that the output is more readable

		  for (int i = 0; i < files.Length; ++i)
		  {
			long len = cfr.fileLength(files[i]);

			if (extract)
			{
			  Console.WriteLine("extract " + files[i] + " with " + len + " bytes to local directory...");
			  IndexInput ii = cfr.openInput(files[i], context);

			  FileOutputStream f = new FileOutputStream(files[i]);

			  // read and write with a small buffer, which is more effective than reading byte by byte
			  sbyte[] buffer = new sbyte[1024];
			  int chunk = buffer.Length;
			  while (len > 0)
			  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int bufLen = (int) Math.min(chunk, len);
				int bufLen = (int) Math.Min(chunk, len);
				ii.readBytes(buffer, 0, bufLen);
				f.write(buffer, 0, bufLen);
				len -= bufLen;
			  }

			  f.close();
			  ii.close();
			}
			else
			{
			  Console.WriteLine(files[i] + ": " + len + " bytes");
			}
		  }
		}
		catch (IOException ioe)
		{
		  Console.WriteLine(ioe.ToString());
		  Console.Write(ioe.StackTrace);
		}
		finally
		{
		  try
		  {
			if (dir != null)
			{
			  dir.close();
			}
			if (cfr != null)
			{
			  cfr.close();
			}
		  }
		  catch (IOException ioe)
		  {
			Console.WriteLine(ioe.ToString());
			Console.Write(ioe.StackTrace);
		  }
		}
	  }
	}

}