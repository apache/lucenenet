using Lucene.Net.Store;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Console = Lucene.Net.Util.SystemConsole;
using JCG = J2N.Collections.Generic;

namespace Lucene.Net.Index
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
    /// Command-line tool that enables listing segments in an
    /// index, copying specific segments to another index, and
    /// deleting segments from an index.
    /// 
    /// <para>This tool does file-level copying of segments files.
    /// This means it's unable to split apart a single segment
    /// into multiple segments.  For example if your index is a
    /// single segment, this tool won't help.  Also, it does basic
    /// file-level copying (using simple
    /// Stream) so it will not work with non
    /// FSDirectory Directory impls.</para>
    /// 
    /// @lucene.experimental You can easily
    /// accidentally remove segments from your index so be
    /// careful!
    /// </summary>
    public class IndexSplitter
    {
        public SegmentInfos Infos { get; set; }

        internal FSDirectory fsDir;

        internal DirectoryInfo dir;

        public static void Main(string[] args)
        {
            if (args.Length < 2)
            {
                // LUCENENET specific - our wrapper console shows the correct usage
                throw new ArgumentException();
                //Console.Error.WriteLine("Usage: IndexSplitter <srcDir> -l (list the segments and their sizes)");
                //Console.Error.WriteLine("IndexSplitter <srcDir> <destDir> <segments>+");
                //Console.Error.WriteLine("IndexSplitter <srcDir> -d (delete the following segments)");
                //return;
            }
            DirectoryInfo srcDir = new DirectoryInfo(args[0]);
            IndexSplitter @is = new IndexSplitter(srcDir);
            if (!srcDir.Exists)
            {
                throw new Exception("srcdir:" + srcDir.FullName + " doesn't exist");
            }
            if (args[1].Equals("-l", StringComparison.Ordinal))
            {
                @is.ListSegments();
            }
            else if (args[1].Equals("-d", StringComparison.Ordinal))
            {
                IList<string> segs = new JCG.List<string>();
                for (int x = 2; x < args.Length; x++)
                {
                    segs.Add(args[x]);
                }
                @is.Remove(segs);
            }
            else
            {
                DirectoryInfo targetDir = new DirectoryInfo(args[1]);
                IList<string> segs = new JCG.List<string>();
                for (int x = 2; x < args.Length; x++)
                {
                    segs.Add(args[x]);
                }
                @is.Split(targetDir, segs);
            }
        }

        public IndexSplitter(DirectoryInfo dir)
        {
            this.dir = dir;
            fsDir = FSDirectory.Open(dir);
            Infos = new SegmentInfos();
            Infos.Read(fsDir);
        }

        public virtual void ListSegments()
        {
            for (int x = 0; x < Infos.Count; x++)
            {
                SegmentCommitInfo info = Infos[x];
                string sizeStr = string.Format(CultureInfo.InvariantCulture, "{0:###,###.###}", info.GetSizeInBytes());
                Console.WriteLine(info.Info.Name + " " + sizeStr);
            }
        }

        private int GetIdx(string name)
        {
            for (int x = 0; x < Infos.Count; x++)
            {
                if (name.Equals(Infos[x].Info.Name, StringComparison.Ordinal))
                {
                    return x;
                }
            }
            return -1;
        }

        private SegmentCommitInfo GetInfo(string name)
        {
            for (int x = 0; x < Infos.Count; x++)
            {
                if (name.Equals(Infos[x].Info.Name, StringComparison.Ordinal))
                {
                    return Infos[x];
                }
            }
            return null;
        }

        public virtual void Remove(ICollection<string> segs) // LUCENENET specific - changed to ICollection to reduce copy operations
        {
            foreach (string n in segs)
            {
                int idx = GetIdx(n);
                Infos.Remove(idx);
            }
            Infos.Changed();
            Infos.Commit(fsDir);
        }

        public virtual void Split(DirectoryInfo destDir, ICollection<string> segs) // LUCENENET specific - changed to ICollection to reduce copy operations
        {
            destDir.Create();
            FSDirectory destFSDir = FSDirectory.Open(destDir);
            SegmentInfos destInfos = new SegmentInfos();
            destInfos.Counter = Infos.Counter;
            foreach (string n in segs)
            {
                SegmentCommitInfo infoPerCommit = GetInfo(n);
                SegmentInfo info = infoPerCommit.Info;
                // Same info just changing the dir:
                SegmentInfo newInfo = new SegmentInfo(destFSDir, info.Version, info.Name, info.DocCount, info.UseCompoundFile, info.Codec, info.Diagnostics);
                destInfos.Add(new SegmentCommitInfo(newInfo, infoPerCommit.DelCount, infoPerCommit.DelGen, infoPerCommit.FieldInfosGen));
                // now copy files over
                ICollection<string> files = infoPerCommit.GetFiles();
                foreach (string srcName in files)
                {
                    FileInfo srcFile = new FileInfo(Path.Combine(dir.FullName, srcName));
                    FileInfo destFile = new FileInfo(Path.Combine(destDir.FullName, srcName));
                    CopyFile(srcFile, destFile);
                }
            }
            destInfos.Changed();
            destInfos.Commit(destFSDir);
            // Console.WriteLine("destDir:"+destDir.getAbsolutePath());
        }

        private static void CopyFile(FileInfo src, FileInfo dst)
        {
            using Stream @in = new FileStream(src.FullName, FileMode.Open, FileAccess.Read);
            using Stream @out = new FileStream(dst.FullName, FileMode.OpenOrCreate, FileAccess.Write);
            @in.CopyTo(@out);
        }
    }
}