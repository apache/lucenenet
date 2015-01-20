/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Org.Apache.Lucene.Index;
using Org.Apache.Lucene.Store;
using Sharpen;

namespace Org.Apache.Lucene.Index
{
	/// <summary>
	/// Command-line tool that enables listing segments in an
	/// index, copying specific segments to another index, and
	/// deleting segments from an index.
	/// </summary>
	/// <remarks>
	/// Command-line tool that enables listing segments in an
	/// index, copying specific segments to another index, and
	/// deleting segments from an index.
	/// <p>This tool does file-level copying of segments files.
	/// This means it's unable to split apart a single segment
	/// into multiple segments.  For example if your index is a
	/// single segment, this tool won't help.  Also, it does basic
	/// file-level copying (using simple
	/// File{In,Out}putStream) so it will not work with non
	/// FSDirectory Directory impls.</p>
	/// </remarks>
	/// <lucene.experimental>
	/// You can easily
	/// accidentally remove segments from your index so be
	/// careful!
	/// </lucene.experimental>
	public class IndexSplitter
	{
		public SegmentInfos infos;

		internal FSDirectory fsDir;

		internal FilePath dir;

		/// <exception cref="System.Exception"></exception>
		public static void Main(string[] args)
		{
			if (args.Length < 2)
			{
				System.Console.Error.WriteLine("Usage: IndexSplitter <srcDir> -l (list the segments and their sizes)"
					);
				System.Console.Error.WriteLine("IndexSplitter <srcDir> <destDir> <segments>+");
				System.Console.Error.WriteLine("IndexSplitter <srcDir> -d (delete the following segments)"
					);
				return;
			}
			FilePath srcDir = new FilePath(args[0]);
			Org.Apache.Lucene.Index.IndexSplitter @is = new Org.Apache.Lucene.Index.IndexSplitter
				(srcDir);
			if (!srcDir.Exists())
			{
				throw new Exception("srcdir:" + srcDir.GetAbsolutePath() + " doesn't exist");
			}
			if (args[1].Equals("-l"))
			{
				@is.ListSegments();
			}
			else
			{
				if (args[1].Equals("-d"))
				{
					IList<string> segs = new AList<string>();
					for (int x = 2; x < args.Length; x++)
					{
						segs.AddItem(args[x]);
					}
					@is.Remove(Sharpen.Collections.ToArray(segs, new string[0]));
				}
				else
				{
					FilePath targetDir = new FilePath(args[1]);
					IList<string> segs = new AList<string>();
					for (int x = 2; x < args.Length; x++)
					{
						segs.AddItem(args[x]);
					}
					@is.Split(targetDir, Sharpen.Collections.ToArray(segs, new string[0]));
				}
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		public IndexSplitter(FilePath dir)
		{
			this.dir = dir;
			fsDir = FSDirectory.Open(dir);
			infos = new SegmentInfos();
			infos.Read(fsDir);
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void ListSegments()
		{
			DecimalFormat formatter = new DecimalFormat("###,###.###", DecimalFormatSymbols.GetInstance
				(CultureInfo.ROOT));
			for (int x = 0; x < infos.Size(); x++)
			{
				SegmentCommitInfo info = infos.Info(x);
				string sizeStr = formatter.Format(info.SizeInBytes());
				System.Console.Out.WriteLine(info.info.name + " " + sizeStr);
			}
		}

		private int GetIdx(string name)
		{
			for (int x = 0; x < infos.Size(); x++)
			{
				if (name.Equals(infos.Info(x).info.name))
				{
					return x;
				}
			}
			return -1;
		}

		private SegmentCommitInfo GetInfo(string name)
		{
			for (int x = 0; x < infos.Size(); x++)
			{
				if (name.Equals(infos.Info(x).info.name))
				{
					return infos.Info(x);
				}
			}
			return null;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void Remove(string[] segs)
		{
			foreach (string n in segs)
			{
				int idx = GetIdx(n);
				infos.Remove(idx);
			}
			infos.Changed();
			infos.Commit(fsDir);
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void Split(FilePath destDir, string[] segs)
		{
			destDir.Mkdirs();
			FSDirectory destFSDir = FSDirectory.Open(destDir);
			SegmentInfos destInfos = new SegmentInfos();
			destInfos.counter = infos.counter;
			foreach (string n in segs)
			{
				SegmentCommitInfo infoPerCommit = GetInfo(n);
				SegmentInfo info = infoPerCommit.info;
				// Same info just changing the dir:
				SegmentInfo newInfo = new SegmentInfo(destFSDir, info.GetVersion(), info.name, info
					.GetDocCount(), info.GetUseCompoundFile(), info.GetCodec(), info.GetDiagnostics(
					));
				destInfos.Add(new SegmentCommitInfo(newInfo, infoPerCommit.GetDelCount(), infoPerCommit
					.GetDelGen(), infoPerCommit.GetFieldInfosGen()));
				// now copy files over
				ICollection<string> files = infoPerCommit.Files();
				foreach (string srcName in files)
				{
					FilePath srcFile = new FilePath(dir, srcName);
					FilePath destFile = new FilePath(destDir, srcName);
					CopyFile(srcFile, destFile);
				}
			}
			destInfos.Changed();
			destInfos.Commit(destFSDir);
		}

		private static readonly byte[] copyBuffer = new byte[32 * 1024];

		// System.out.println("destDir:"+destDir.getAbsolutePath());
		/// <exception cref="System.IO.IOException"></exception>
		private static void CopyFile(FilePath src, FilePath dst)
		{
			InputStream @in = new FileInputStream(src);
			OutputStream @out = new FileOutputStream(dst);
			int len;
			while ((len = @in.Read(copyBuffer)) > 0)
			{
				@out.Write(copyBuffer, 0, len);
			}
			@in.Close();
			@out.Close();
		}
	}
}
