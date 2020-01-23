using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using JCG = J2N.Collections.Generic;
using Console = Lucene.Net.Support.SystemConsole;

namespace Lucene.Net.Store
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

    using IndexFileNames = Lucene.Net.Index.IndexFileNames;
    using IOUtils = Lucene.Net.Util.IOUtils;

    // TODO
    //   - let subclass dictate policy...?
    //   - rename to MergeCacheingDir?  NRTCachingDir

    // :Post-Release-Update-Version.LUCENE_XY: (in <pre> block in javadoc below)
    /// <summary>
    /// Wraps a <see cref="RAMDirectory"/>
    /// around any provided delegate directory, to
    /// be used during NRT search.
    ///
    /// <para>This class is likely only useful in a near-real-time
    /// context, where indexing rate is lowish but reopen
    /// rate is highish, resulting in many tiny files being
    /// written.  This directory keeps such segments (as well as
    /// the segments produced by merging them, as long as they
    /// are small enough), in RAM.</para>
    ///
    /// <para>This is safe to use: when your app calls <see cref="Index.IndexWriter.Commit()"/>,
    /// all cached files will be flushed from the cached and sync'd.</para>
    ///
    /// <para/>Here's a simple example usage:
    ///
    /// <code>
    ///     Directory fsDir = FSDirectory.Open(new DirectoryInfo("/path/to/index"));
    ///     NRTCachingDirectory cachedFSDir = new NRTCachingDirectory(fsDir, 5.0, 60.0);
    ///     IndexWriterConfig conf = new IndexWriterConfig(Version.LUCENE_48, analyzer);
    ///     IndexWriter writer = new IndexWriter(cachedFSDir, conf);
    /// </code>
    ///
    /// <para>This will cache all newly flushed segments, all merges
    /// whose expected segment size is &lt;= 5 MB, unless the net
    /// cached bytes exceeds 60 MB at which point all writes will
    /// not be cached (until the net bytes falls below 60 MB).</para>
    /// <para/>
    /// @lucene.experimental
    /// </summary>
    public class NRTCachingDirectory : Directory
    {
        private readonly RAMDirectory cache = new RAMDirectory();

        private readonly Directory @delegate;

        private readonly long maxMergeSizeBytes;
        private readonly long maxCachedBytes;

        private static bool VERBOSE = false;

        /// <summary>
        /// We will cache a newly created output if 1) it's a
        /// flush or a merge and the estimated size of the merged segment is &lt;=
        /// maxMergeSizeMB, and 2) the total cached bytes is &lt;=
        /// maxCachedMB
        /// </summary>
        public NRTCachingDirectory(Directory @delegate, double maxMergeSizeMB, double maxCachedMB)
        {
            this.@delegate = @delegate;
            maxMergeSizeBytes = (long)(maxMergeSizeMB * 1024 * 1024);
            maxCachedBytes = (long)(maxCachedMB * 1024 * 1024);
        }

        public virtual Directory Delegate
        {
            get
            {
                return @delegate;
            }
        }

        public override LockFactory LockFactory
        {
            get
            {
                return @delegate.LockFactory;
            }
        }

        public override void SetLockFactory(LockFactory lockFactory)
        {
            @delegate.SetLockFactory(lockFactory);
        }

        public override string GetLockID()
        {
            return @delegate.GetLockID();
        }

        public override Lock MakeLock(string name)
        {
            return @delegate.MakeLock(name);
        }

        public override void ClearLock(string name)
        {
            @delegate.ClearLock(name);
        }

        public override string ToString()
        {
            return "NRTCachingDirectory(" + @delegate + "; maxCacheMB=" + (maxCachedBytes / 1024 / 1024.0) + " maxMergeSizeMB=" + (maxMergeSizeBytes / 1024 / 1024.0) + ")";
        }

        public override string[] ListAll()
        {
            lock (this)
            {
                ISet<string> files = new JCG.HashSet<string>();
                foreach (string f in cache.ListAll())
                {
                    files.Add(f);
                }
                // LUCENE-1468: our NRTCachingDirectory will actually exist (RAMDir!),
                // but if the underlying delegate is an FSDir and mkdirs() has not
                // yet been called, because so far everything is a cached write,
                // in this case, we don't want to throw a NoSuchDirectoryException
                try
                {
                    foreach (string f in @delegate.ListAll())
                    {
                        // Cannot do this -- if lucene calls createOutput but
                        // file already exists then this falsely trips:
                        //assert !files.contains(f): "file \"" + f + "\" is in both dirs";
                        files.Add(f);
                    }
                }
                catch (DirectoryNotFoundException /*ex*/)
                {
                    // however, if there are no cached files, then the directory truly
                    // does not "exist"
                    if (files.Count == 0)
                    {
                        throw; // LUCENENET: CA2200: Rethrow to preserve stack details (https://docs.microsoft.com/en-us/visualstudio/code-quality/ca2200-rethrow-to-preserve-stack-details)
                    }
                }
                return files.ToArray();
            }
        }

        /// <summary>
        /// Returns how many bytes are being used by the
        /// <see cref="RAMDirectory"/> cache
        /// </summary>
        public virtual long GetSizeInBytes()
        {
            return cache.GetSizeInBytes();
        }

        [Obsolete("this method will be removed in 5.0")]
        public override bool FileExists(string name)
        {
            lock (this)
            {
                return cache.FileExists(name) || @delegate.FileExists(name);
            }
        }

        public override void DeleteFile(string name)
        {
            lock (this)
            {
                if (VERBOSE)
                {
                    Console.WriteLine("nrtdir.deleteFile name=" + name);
                }
#pragma warning disable 612, 618
                if (cache.FileExists(name))
#pragma warning restore 612, 618
                {
                    cache.DeleteFile(name);
                }
                else
                {
                    @delegate.DeleteFile(name);
                }
            }
        }

        public override long FileLength(string name)
        {
            lock (this)
            {
#pragma warning disable 612, 618
                if (cache.FileExists(name))
#pragma warning restore 612, 618
                {
                    return cache.FileLength(name);
                }
                else
                {
                    return @delegate.FileLength(name);
                }
            }
        }

        public virtual string[] ListCachedFiles()
        {
            return cache.ListAll();
        }

        public override IndexOutput CreateOutput(string name, IOContext context)
        {
            if (VERBOSE)
            {
                Console.WriteLine("nrtdir.createOutput name=" + name);
            }
            if (DoCacheWrite(name, context))
            {
                if (VERBOSE)
                {
                    Console.WriteLine("  to cache");
                }
                try
                {
                    @delegate.DeleteFile(name);
                }
#pragma warning disable 168
                catch (IOException ioe)
#pragma warning restore 168
                {
                    // this is fine: file may not exist
                }
                return cache.CreateOutput(name, context);
            }
            else
            {
                try
                {
                    cache.DeleteFile(name);
                }
#pragma warning disable 168
                catch (IOException ioe)
#pragma warning restore 168
                {
                    // this is fine: file may not exist
                }
                return @delegate.CreateOutput(name, context);
            }
        }

        public override void Sync(ICollection<string> fileNames)
        {
            if (VERBOSE)
            {
                Console.WriteLine("nrtdir.sync files=" + fileNames);
            }
            foreach (string fileName in fileNames)
            {
                UnCache(fileName);
            }
            @delegate.Sync(fileNames);
        }

        public override IndexInput OpenInput(string name, IOContext context)
        {
            lock (this)
            {
                if (VERBOSE)
                {
                    Console.WriteLine("nrtdir.openInput name=" + name);
                }
#pragma warning disable 612, 618
                if (cache.FileExists(name))
#pragma warning restore 612, 618
                {
                    if (VERBOSE)
                    {
                        Console.WriteLine("  from cache");
                    }
                    return cache.OpenInput(name, context);
                }
                else
                {
                    return @delegate.OpenInput(name, context);
                }
            }
        }

        public override IndexInputSlicer CreateSlicer(string name, IOContext context)
        {
            lock (this)
            {
                EnsureOpen();
                if (VERBOSE)
                {
                    Console.WriteLine("nrtdir.openInput name=" + name);
                }
#pragma warning disable 612, 618
                if (cache.FileExists(name))
#pragma warning restore 612, 618
                {
                    if (VERBOSE)
                    {
                        Console.WriteLine("  from cache");
                    }
                    return cache.CreateSlicer(name, context);
                }
                else
                {
                    return @delegate.CreateSlicer(name, context);
                }
            }
        }

        /// <summary>
        /// Dispose this directory, which flushes any cached files
        /// to the delegate and then disposes the delegate.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // NOTE: technically we shouldn't have to do this, ie,
                // IndexWriter should have sync'd all files, but we do
                // it for defensive reasons... or in case the app is
                // doing something custom (creating outputs directly w/o
                // using IndexWriter):
                foreach (string fileName in cache.ListAll())
                {
                    UnCache(fileName);
                }
                cache.Dispose();
                @delegate.Dispose();
            }
        }

        /// <summary>
        /// Subclass can override this to customize logic; return
        /// <c>true</c> if this file should be written to the <see cref="RAMDirectory"/>.
        /// </summary>
        protected virtual bool DoCacheWrite(string name, IOContext context)
        {
            //System.out.println(Thread.currentThread().getName() + ": CACHE check merge=" + merge + " size=" + (merge==null ? 0 : merge.estimatedMergeBytes));

            long bytes = 0;
            if (context.MergeInfo != null)
            {
                bytes = context.MergeInfo.EstimatedMergeBytes;
            }
            else if (context.FlushInfo != null)
            {
                bytes = context.FlushInfo.EstimatedSegmentSize;
            }

            return !name.Equals(IndexFileNames.SEGMENTS_GEN, StringComparison.Ordinal) && (bytes <= maxMergeSizeBytes) && (bytes + cache.GetSizeInBytes()) <= maxCachedBytes;
        }

        private readonly object uncacheLock = new object();

        private void UnCache(string fileName)
        {
            // Only let one thread uncache at a time; this only
            // happens during commit() or close():
            lock (uncacheLock)
            {
                if (VERBOSE)
                {
                    Console.WriteLine("nrtdir.unCache name=" + fileName);
                }
#pragma warning disable 612, 618
                if (!cache.FileExists(fileName))
#pragma warning restore 612, 618
                {
                    // Another thread beat us...
                    return;
                }
                IOContext context = IOContext.DEFAULT;
                IndexOutput @out = @delegate.CreateOutput(fileName, context);
                IndexInput @in = null;
                try
                {
                    @in = cache.OpenInput(fileName, context);
                    @out.CopyBytes(@in, @in.Length);
                }
                finally
                {
                    IOUtils.Dispose(@in, @out);
                }

                // Lock order: uncacheLock -> this
                lock (this)
                {
                    // Must sync here because other sync methods have
                    // if (cache.fileExists(name)) { ... } else { ... }:
                    cache.DeleteFile(fileName);
                }
            }
        }
    }
}