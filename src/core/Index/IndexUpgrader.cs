using Lucene.Net.Store;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Directory = Lucene.Net.Store.Directory;
using Version = Lucene.Net.Util.Version;

namespace Lucene.Net.Index
{
    public sealed class IndexUpgrader
    {
        private static void PrintUsage()
        {
            // .NET Port TO-DO: change this usage to match .NET!
            Console.Error.WriteLine("Upgrades an index so all segments created with a previous Lucene version are rewritten.");
            Console.Error.WriteLine("Usage:");
            Console.Error.WriteLine("  java " + typeof(IndexUpgrader).FullName + " [-delete-prior-commits] [-verbose] [-dir-impl X] indexDir");
            Console.Error.WriteLine("This tool keeps only the last commit in an index; for this");
            Console.Error.WriteLine("reason, if the incoming index has more than one commit, the tool");
            Console.Error.WriteLine("refuses to run by default. Specify -delete-prior-commits to override");
            Console.Error.WriteLine("this, allowing the tool to delete all but the last commit.");
            Console.Error.WriteLine("Specify a " + typeof(FSDirectory).Name +
                " implementation through the -dir-impl option to force its use. If no package is specified the "
                + typeof(FSDirectory).Namespace + " package will be used.");
            Console.Error.WriteLine("WARNING: This tool may reorder document IDs!");
            Environment.Exit(1);
        }

        public static void Main(string[] args)
        {
            String path = null;
            bool deletePriorCommits = false;
            TextWriter output = null;
            String dirImpl = null;
            int i = 0;
            while (i < args.Length)
            {
                String arg = args[i];
                if ("-delete-prior-commits".Equals(arg))
                {
                    deletePriorCommits = true;
                }
                else if ("-verbose".Equals(arg))
                {
                    output = Console.Out;
                }
                else if (path == null)
                {
                    path = arg;
                }
                else if ("-dir-impl".Equals(arg))
                {
                    if (i == args.Length - 1)
                    {
                        Console.Out.WriteLine("ERROR: missing value for -dir-impl option");
                        Environment.Exit(1);
                    }
                    i++;
                    dirImpl = args[i];
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
            new IndexUpgrader(dir, Version.LUCENE_CURRENT, output, deletePriorCommits).Upgrade();
        }

        private readonly Directory dir;
        private readonly IndexWriterConfig iwc;
        private readonly bool deletePriorCommits;

        public IndexUpgrader(Directory dir, Version matchVersion)
            : this(dir, new IndexWriterConfig(matchVersion, null), false)
        {
        }

        public IndexUpgrader(Directory dir, Version matchVersion, TextWriter infoStream, bool deletePriorCommits)
            : this(dir, new IndexWriterConfig(matchVersion, null).SetInfoStream(infoStream), deletePriorCommits)
        {
        }

        public IndexUpgrader(Directory dir, IndexWriterConfig iwc, bool deletePriorCommits)
        {
            this.dir = dir;
            this.iwc = iwc;
            this.deletePriorCommits = deletePriorCommits;
        }

        public void Upgrade()
        {
            if (!DirectoryReader.IndexExists(dir))
            {
                throw new IndexNotFoundException(dir.ToString());
            }

            if (!deletePriorCommits)
            {
                ICollection<IndexCommit> commits = DirectoryReader.ListCommits(dir);
                if (commits.Count > 1)
                {
                    throw new ArgumentException("This tool was invoked to not delete prior commit points, but the following commits were found: " + commits);
                }
            }

            IndexWriterConfig c = (IndexWriterConfig)iwc.Clone();
            c.MergePolicy = new UpgradeIndexMergePolicy(c.MergePolicy);
            c.SetIndexDeletionPolicy(new KeepOnlyLastCommitDeletionPolicy());

            IndexWriter w = new IndexWriter(dir, c);
            try
            {
                InfoStream infoStream = c.InfoStream;
                if (infoStream.IsEnabled("IndexUpgrader"))
                {
                    infoStream.Message("IndexUpgrader", "Upgrading all pre-" + Constants.LUCENE_MAIN_VERSION + " segments of index directory '" + dir + "' to version " + Constants.LUCENE_MAIN_VERSION + "...");
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
