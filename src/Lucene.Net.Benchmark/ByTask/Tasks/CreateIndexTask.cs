using Lucene.Net.Benchmarks.ByTask.Utils;
using Lucene.Net.Codecs;
using Lucene.Net.Index;
using Lucene.Net.Support;
using Lucene.Net.Util;
using System;
using System.IO;
using System.Text;
using Console = Lucene.Net.Util.SystemConsole;

namespace Lucene.Net.Benchmarks.ByTask.Tasks
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
    /// Creates an index.
    /// </summary>
    /// <remarks>
    /// Other side effects: index writer object in perfRunData is set.
    /// <para/>
    /// Relevant properties:
    /// <list type="bullet">
    ///     <item><term>merge.factor</term><description>(default 10)</description></item>
    ///     <item><term>max.buffered</term><description>(default no flush)</description></item>
    ///     <item><term>compound</term><description>(default true)</description></item>
    ///     <item><term>ram.flush.mb</term><description>[default 0]</description></item>
    ///     <item><term>merge.policy</term><description>(default Lucene.Net.Index.LogByteSizeMergePolicy, Lucene.Net)</description></item>
    ///     <item><term>merge.scheduler</term><description>(default Lucene.Net.Index.ConcurrentMergeScheduler, Lucene.Net)</description></item>
    ///     <item><term>concurrent.merge.scheduler.max.thread.count</term><description>(defaults per ConcurrentMergeScheduler)</description></item>
    ///     <item><term>concurrent.merge.scheduler.max.merge.count</term><description>(defaults per ConcurrentMergeScheduler)</description></item>
    ///     <item><term>default.codec</term><description></description></item>
    /// </list>
    /// <para/>
    /// This task also supports a "writer.info.stream" property with the following
    /// values:
    /// <list type="bullet">
    ///     <item><term>SystemOut</term><description>Sets <see cref="IndexWriterConfig.SetInfoStream(InfoStream)"/> to <see cref="Console.Out"/>.</description></item>
    ///     <item><term>SystemErr</term><description>Sets <see cref="IndexWriterConfig.SetInfoStream(InfoStream)"/> to <see cref="Console.Error"/></description></item>
    ///     <item><term>&lt;file_name&gt;</term><description>
    ///     Attempts to create a file given that name and sets <see cref="IndexWriterConfig.SetInfoStream(InfoStream)"/>
    ///     to that file. If this denotes an invalid file name, or some error occurs, an exception will be thrown.
    ///     </description></item>
    /// </list>
    /// </remarks>
    public class CreateIndexTask : PerfTask
    {
        public CreateIndexTask(PerfRunData runData)
            : base(runData)
        {
        }

        public static IndexDeletionPolicy GetIndexDeletionPolicy(Config config)
        {
            string deletionPolicyName = config.Get("deletion.policy", "Lucene.Net.Index.KeepOnlyLastCommitDeletionPolicy, Lucene.Net");
            Type deletionPolicyType = Type.GetType(deletionPolicyName);
            if (deletionPolicyType is null)
            {
                throw RuntimeException.Create("Unrecognized deletion policy type '" + deletionPolicyName + "'"); // LUCENENET: In .NET we don't get an error here, so throwing one for compatibility
            }
            else if (deletionPolicyType.Equals(typeof(NoDeletionPolicy)))
            {
                return NoDeletionPolicy.INSTANCE;
            }
            else
            {
                try
                {
                    return (IndexDeletionPolicy)Activator.CreateInstance(deletionPolicyType);
                }
                catch (Exception e) when (e.IsException())
                {
                    throw RuntimeException.Create("unable to instantiate class '" + deletionPolicyName + "' as IndexDeletionPolicy", e);
                }
            }
        }

        public override int DoLogic()
        {
            PerfRunData runData = RunData;
            Config config = runData.Config;
            runData.IndexWriter = ConfigureWriter(config, runData, OpenMode.CREATE, null);
            return 1;
        }

        public static IndexWriterConfig CreateWriterConfig(Config config, PerfRunData runData, OpenMode mode, IndexCommit commit)
        {
            // :Post-Release-Update-Version.LUCENE_XY:
            LuceneVersion version = (LuceneVersion)Enum.Parse(typeof(LuceneVersion), config.Get("writer.version", LuceneVersion.LUCENE_48.ToString()));
            IndexWriterConfig iwConf = new IndexWriterConfig(version, runData.Analyzer);
            iwConf.OpenMode = mode;
            IndexDeletionPolicy indexDeletionPolicy = GetIndexDeletionPolicy(config);
            iwConf.IndexDeletionPolicy = indexDeletionPolicy;
            if (commit != null)
                iwConf.IndexCommit = commit;


            string mergeScheduler = config.Get("merge.scheduler",
                                                     "Lucene.Net.Index.ConcurrentMergeScheduler, Lucene.Net");

            Type mergeSchedulerType = Type.GetType(mergeScheduler);
            if (mergeSchedulerType is null)
            {
                throw RuntimeException.Create("Unrecognized merge scheduler type '" + mergeScheduler + "'"); // LUCENENET: We don't get an exception in this case, so throwing one for compatibility
            }
            else if (mergeSchedulerType.Equals(typeof(NoMergeScheduler)))
            {
                iwConf.MergeScheduler = NoMergeScheduler.INSTANCE;
            }
            else
            {
                try
                {
                    iwConf.MergeScheduler = (IMergeScheduler)Activator.CreateInstance(mergeSchedulerType);
                }
                catch (Exception e) when (e.IsException())
                {
                    throw RuntimeException.Create("unable to instantiate class '" + mergeScheduler + "' as merge scheduler", e);
                }

                if (mergeScheduler.Equals("Lucene.Net.Index.ConcurrentMergeScheduler", StringComparison.Ordinal))
                {
                    ConcurrentMergeScheduler cms = (ConcurrentMergeScheduler)iwConf.MergeScheduler;
                    int maxThreadCount = config.Get("concurrent.merge.scheduler.max.thread.count", ConcurrentMergeScheduler.DEFAULT_MAX_THREAD_COUNT);
                    int maxMergeCount = config.Get("concurrent.merge.scheduler.max.merge.count", ConcurrentMergeScheduler.DEFAULT_MAX_MERGE_COUNT);
                    cms.SetMaxMergesAndThreads(maxMergeCount, maxThreadCount);
                }
            }

            string defaultCodec = config.Get("default.codec", null);
            if (defaultCodec != null)
            {
                try
                {
                    Type clazz = Type.GetType(defaultCodec);
                    iwConf.Codec = (Codec)Activator.CreateInstance(clazz);
                }
                catch (Exception e) when (e.IsException())
                {
                    throw RuntimeException.Create("Couldn't instantiate Codec: " + defaultCodec, e);
                }
            }

            string mergePolicy = config.Get("merge.policy",
                                                  "Lucene.Net.Index.LogByteSizeMergePolicy, Lucene.Net");
            bool isCompound = config.Get("compound", true);
            Type mergePolicyType = Type.GetType(mergePolicy);
            if (mergePolicyType is null)
            {
                throw RuntimeException.Create("Unrecognized merge policy type '" + mergePolicy + "'"); // LUCENENET: We don't get an exception in this case, so throwing one for compatibility
            }
            else if (mergePolicyType.Equals(typeof(NoMergePolicy)))
            {
                iwConf.MergePolicy = isCompound ? NoMergePolicy.COMPOUND_FILES : NoMergePolicy.NO_COMPOUND_FILES;
            }
            else
            {
                try
                {
                    iwConf.MergePolicy = (MergePolicy)Activator.CreateInstance(mergePolicyType);
                }
                catch (Exception e) when (e.IsException())
                {
                    throw RuntimeException.Create("unable to instantiate class '" + mergePolicy + "' as merge policy", e);
                }
                iwConf.MergePolicy.NoCFSRatio = isCompound ? 1.0 : 0.0;
                if (iwConf.MergePolicy is LogMergePolicy logMergePolicy)
                {
                    logMergePolicy.MergeFactor = config.Get("merge.factor", OpenIndexTask.DEFAULT_MERGE_PFACTOR);
                }
            }
            double ramBuffer = config.Get("ram.flush.mb", OpenIndexTask.DEFAULT_RAM_FLUSH_MB);
            int maxBuffered = config.Get("max.buffered", OpenIndexTask.DEFAULT_MAX_BUFFERED);
            if (maxBuffered == IndexWriterConfig.DISABLE_AUTO_FLUSH)
            {
                iwConf.RAMBufferSizeMB = ramBuffer;
                iwConf.MaxBufferedDocs = maxBuffered;
            }
            else
            {
                iwConf.MaxBufferedDocs = maxBuffered;
                iwConf.RAMBufferSizeMB = ramBuffer;
            }

            return iwConf;
        }

        public static IndexWriter ConfigureWriter(Config config, PerfRunData runData, OpenMode mode, IndexCommit commit)
        {
            IndexWriterConfig iwc = CreateWriterConfig(config, runData, mode, commit);
            string infoStreamVal = config.Get("writer.info.stream", null);
            if (infoStreamVal != null)
            {
                if (infoStreamVal.Equals("SystemOut", StringComparison.Ordinal))
                {
                    iwc.SetInfoStream(Console.Out);
                }
                else if (infoStreamVal.Equals("SystemErr", StringComparison.Ordinal))
                {
                    iwc.SetInfoStream(Console.Error);
                }
                else
                {
                    FileInfo f = new FileInfo(infoStreamVal);
                    iwc.SetInfoStream(new StreamWriter(new FileStream(f.FullName, FileMode.Create, FileAccess.Write), Encoding.GetEncoding(0)));
                }
            }
            IndexWriter writer = new IndexWriter(runData.Directory, iwc);
            return writer;
        }
    }
}
