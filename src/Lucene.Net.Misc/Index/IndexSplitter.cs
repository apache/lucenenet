using System;
using System.Collections.Generic;

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
namespace org.apache.lucene.index
{


	using FSDirectory = org.apache.lucene.store.FSDirectory;

	/// <summary>
	/// Command-line tool that enables listing segments in an
	/// index, copying specific segments to another index, and
	/// deleting segments from an index.
	/// 
	/// <para>This tool does file-level copying of segments files.
	/// This means it's unable to split apart a single segment
	/// into multiple segments.  For example if your index is a
	/// single segment, this tool won't help.  Also, it does basic
	/// file-level copying (using simple
	/// File{In,Out}putStream) so it will not work with non
	/// FSDirectory Directory impls.</para>
	/// 
	/// @lucene.experimental You can easily
	/// accidentally remove segments from your index so be
	/// careful!
	/// </summary>
	public class IndexSplitter
	{
	  public SegmentInfos infos;

	  internal FSDirectory fsDir;

	  internal File dir;

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public static void main(String[] args) throws Exception
	  public static void Main(string[] args)
	  {
		if (args.Length < 2)
		{
		  Console.Error.WriteLine("Usage: IndexSplitter <srcDir> -l (list the segments and their sizes)");
		  Console.Error.WriteLine("IndexSplitter <srcDir> <destDir> <segments>+");
		  Console.Error.WriteLine("IndexSplitter <srcDir> -d (delete the following segments)");
		  return;
		}
		File srcDir = new File(args[0]);
		IndexSplitter @is = new IndexSplitter(srcDir);
		if (!srcDir.exists())
		{
		  throw new Exception("srcdir:" + srcDir.AbsolutePath + " doesn't exist");
		}
		if (args[1].Equals("-l"))
		{
		  @is.listSegments();
		}
		else if (args[1].Equals("-d"))
		{
		  IList<string> segs = new List<string>();
		  for (int x = 2; x < args.Length; x++)
		  {
			segs.Add(args[x]);
		  }
		  @is.remove(segs.ToArray());
		}
		else
		{
		  File targetDir = new File(args[1]);
		  IList<string> segs = new List<string>();
		  for (int x = 2; x < args.Length; x++)
		  {
			segs.Add(args[x]);
		  }
		  @is.Split(targetDir, segs.ToArray());
		}
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public IndexSplitter(java.io.File dir) throws java.io.IOException
	  public IndexSplitter(File dir)
	  {
		this.dir = dir;
		fsDir = FSDirectory.open(dir);
		infos = new SegmentInfos();
		infos.read(fsDir);
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void listSegments() throws java.io.IOException
	  public virtual void listSegments()
	  {
		DecimalFormat formatter = new DecimalFormat("###,###.###", DecimalFormatSymbols.getInstance(Locale.ROOT));
		for (int x = 0; x < infos.size(); x++)
		{
		  SegmentCommitInfo info = infos.info(x);
		  string sizeStr = formatter.format(info.sizeInBytes());
		  Console.WriteLine(info.info.name + " " + sizeStr);
		}
	  }

	  private int getIdx(string name)
	  {
		for (int x = 0; x < infos.size(); x++)
		{
		  if (name.Equals(infos.info(x).info.name))
		  {
			return x;
		  }
		}
		return -1;
	  }

	  private SegmentCommitInfo getInfo(string name)
	  {
		for (int x = 0; x < infos.size(); x++)
		{
		  if (name.Equals(infos.info(x).info.name))
		  {
			return infos.info(x);
		  }
		}
		return null;
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void remove(String[] segs) throws java.io.IOException
	  public virtual void remove(string[] segs)
	  {
		foreach (string n in segs)
		{
		  int idx = getIdx(n);
		  infos.remove(idx);
		}
		infos.changed();
		infos.commit(fsDir);
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void split(java.io.File destDir, String[] segs) throws java.io.IOException
	  public virtual void Split(File destDir, string[] segs)
	  {
		destDir.mkdirs();
		FSDirectory destFSDir = FSDirectory.open(destDir);
		SegmentInfos destInfos = new SegmentInfos();
		destInfos.counter = infos.counter;
		foreach (string n in segs)
		{
		  SegmentCommitInfo infoPerCommit = getInfo(n);
		  SegmentInfo info = infoPerCommit.info;
		  // Same info just changing the dir:
		  SegmentInfo newInfo = new SegmentInfo(destFSDir, info.Version, info.name, info.DocCount, info.UseCompoundFile, info.Codec, info.Diagnostics);
		  destInfos.add(new SegmentCommitInfo(newInfo, infoPerCommit.DelCount, infoPerCommit.DelGen, infoPerCommit.FieldInfosGen));
		  // now copy files over
		  ICollection<string> files = infoPerCommit.files();
		  foreach (String srcName in files)
		  {
			File srcFile = new File(dir, srcName);
			File destFile = new File(destDir, srcName);
			copyFile(srcFile, destFile);
		  }
		}
		destInfos.changed();
		destInfos.commit(destFSDir);
		// System.out.println("destDir:"+destDir.getAbsolutePath());
	  }

	  private static readonly sbyte[] copyBuffer = new sbyte[32 * 1024];

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: private static void copyFile(java.io.File src, java.io.File dst) throws java.io.IOException
	  private static void copyFile(File src, File dst)
	  {
		InputStream @in = new FileInputStream(src);
		OutputStream @out = new FileOutputStream(dst);
		int len;
		while ((len = @in.read(copyBuffer)) > 0)
		{
		  @out.write(copyBuffer, 0, len);
		}
		@in.close();
		@out.close();
	  }
	}

}