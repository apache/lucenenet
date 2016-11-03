using System;
using System.Collections.Generic;
using Lucene.Net.Util;

namespace Lucene.Net.Index
{
    using System.IO;
    using CommandLineUtil = Lucene.Net.Util.CommandLineUtil;
    using Constants = Lucene.Net.Util.Constants;

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

    using Directory = Lucene.Net.Store.Directory;
    using FSDirectory = Lucene.Net.Store.FSDirectory;
    using InfoStream = Lucene.Net.Util.InfoStream;

    /// <summary>
    /// this is an easy-to-use tool that upgrades all segments of an index from previous Lucene versions
    /// to the current segment file format. It can be used from command line:
    /// <pre>
    ///  java -cp lucene-core.jar Lucene.Net.Index.IndexUpgrader [-delete-prior-commits] [-verbose] indexDir
    /// </pre>
    /// Alternatively this class can be instantiated and <seealso cref="#upgrade"/> invoked. It uses <seealso cref="UpgradeIndexMergePolicy"/>
    /// and triggers the upgrade via an forceMerge request to <seealso cref="IndexWriter"/>.
    /// <p>this tool keeps only the last commit in an index; for this
    /// reason, if the incoming index has more than one commit, the tool
    /// refuses to run by default. Specify {@code -delete-prior-commits}
    /// to override this, allowing the tool to delete all but the last commit.
    /// From Java code this can be enabled by passing {@code true} to
    /// <seealso cref="#IndexUpgrader(Directory,Version,StreamWriter,boolean)"/>.
    /// <p><b>Warning:</b> this tool may reorder documents if the index was partially
    /// upgraded before execution (e.g., documents were added). If your application relies
    /// on &quot;monotonicity&quot; of doc IDs (which means that the order in which the documents
    /// were added to the index is preserved), do a full forceMerge instead.
    /// The <seealso cref="MergePolicy"/> set by <seealso cref="IndexWriterConfig"/> may also reorder
    /// documents.
    /// </summary>
    public sealed class IndexUpgrader
    {
        private static void PrintUsage()
        {
            Console.Error.WriteLine("Upgrades an index so all segments created with a previous Lucene version are rewritten.");
            Console.Error.WriteLine("Usage:");
            Console.Error.WriteLine("  java " + typeof(IndexUpgrader).Name + " [-delete-prior-commits] [-verbose] [-dir-impl X] indexDir");
            Console.Error.WriteLine("this tool keeps only the last commit in an index; for this");
            Console.Error.WriteLine("reason, if the incoming index has more than one commit, the tool");
            Console.Error.WriteLine("refuses to run by default. Specify -delete-prior-commits to override");
            Console.Error.WriteLine("this, allowing the tool to delete all but the last commit.");
            Console.Error.WriteLine("Specify a " + typeof(FSDirectory).Name + " implementation through the -dir-impl option to force its use. If no package is specified the " + typeof(FSDirectory).Namespace + " package will be used.");
            Console.Error.WriteLine("WARNING: this tool may reorder document IDs!");
            Environment.FailFast("1");
        }

        /// <summary>
        /// Main method to run {code IndexUpgrader} from the
        ///  command-line.
        /// </summary>
        /*public static void Main(string[] args)
        {
          ParseArgs(args).Upgrade();
        }*/

        public static IndexUpgrader ParseArgs(string[] args)
        {
            string path = null;
            bool deletePriorCommits = false;
            TextWriter @out = null;
            string dirImpl = null;
            int i = 0;
            while (i < args.Length)
            {
                string arg = args[i];
                if ("-delete-prior-commits".Equals(arg))
                {
                    deletePriorCommits = true;
                }
                else if ("-verbose".Equals(arg))
                {
                    @out = Console.Out;
                }
                else if ("-dir-impl".Equals(arg))
                {
                    if (i == args.Length - 1)
                    {
                        Console.WriteLine("ERROR: missing value for -dir-impl option");
                        Environment.FailFast("1");
                    }
                    i++;
                    dirImpl = args[i];
                }
                else if (path == null)
                {
                    path = arg;
                }
                else
                {
                    PrintUsage();
                }
                i++;
            }
            if (path == null)
            {
                PrintUsage();
            }

            Directory dir = null;
            if (dirImpl == null)
            {
                dir = FSDirectory.Open(new DirectoryInfo(path));
            }
            else
            {
                dir = CommandLineUtil.NewFSDirectory(dirImpl, new DirectoryInfo(path));
            }
            return new IndexUpgrader(dir, LuceneVersion.LUCENE_CURRENT, @out, deletePriorCommits);
        }

        private readonly Directory Dir;
        private readonly IndexWriterConfig Iwc;
        private readonly bool DeletePriorCommits;

        /// <summary>
        /// Creates index upgrader on the given directory, using an <seealso cref="IndexWriter"/> using the given
        /// {@code matchVersion}. The tool refuses to upgrade indexes with multiple commit points.
        /// </summary>
        public IndexUpgrader(Directory dir, LuceneVersion matchVersion)
            : this(dir, new IndexWriterConfig(matchVersion, null), false)
        {
        }

        /// <summary>
        /// Creates index upgrader on the given directory, using an <seealso cref="IndexWriter"/> using the given
        /// {@code matchVersion}. You have the possibility to upgrade indexes with multiple commit points by removing
        /// all older ones. If {@code infoStream} is not {@code null}, all logging output will be sent to this stream.
        /// </summary>
        public IndexUpgrader(Directory dir, LuceneVersion matchVersion, TextWriter infoStream, bool deletePriorCommits)
            : this(dir, new IndexWriterConfig(matchVersion, null), deletePriorCommits)
        {
            if (null != infoStream)
            {
                this.Iwc.SetInfoStream(infoStream);
            }
        }

        /// <summary>
        /// Creates index upgrader on the given directory, using an <seealso cref="IndexWriter"/> using the given
        /// config. You have the possibility to upgrade indexes with multiple commit points by removing
        /// all older ones.
        /// </summary>
        public IndexUpgrader(Directory dir, IndexWriterConfig iwc, bool deletePriorCommits)
        {
            this.Dir = dir;
            this.Iwc = iwc;
            this.DeletePriorCommits = deletePriorCommits;
        }

        /// <summary>
        /// Perform the upgrade. </summary>
        public void Upgrade()
        {
            if (!DirectoryReader.IndexExists(Dir))
            {
                throw new IndexNotFoundException(Dir.ToString());
            }

            if (!DeletePriorCommits)
            {
                ICollection<IndexCommit> commits = DirectoryReader.ListCommits(Dir);
                if (commits.Count > 1)
                {
                    throw new System.ArgumentException("this tool was invoked to not delete prior commit points, but the following commits were found: " + commits);
                }
            }

            IndexWriterConfig c = (IndexWriterConfig)Iwc.Clone();
            c.SetMergePolicy(new UpgradeIndexMergePolicy(c.MergePolicy));
            c.SetIndexDeletionPolicy(new KeepOnlyLastCommitDeletionPolicy());

            IndexWriter w = new IndexWriter(Dir, c);
            try
            {
                InfoStream infoStream = c.InfoStream;
                if (infoStream.IsEnabled("IndexUpgrader"))
                {
                    infoStream.Message("IndexUpgrader", "Upgrading all pre-" + Constants.LUCENE_MAIN_VERSION + " segments of index directory '" + Dir + "' to version " + Constants.LUCENE_MAIN_VERSION + "...");
                }
                w.ForceMerge(1);
                if (infoStream.IsEnabled("IndexUpgrader"))
                {
                    infoStream.Message("IndexUpgrader", "All segments upgraded to version " + Constants.LUCENE_MAIN_VERSION);
                }
            }
            finally
            {
                w.Dispose();
            }
        }
    }
}