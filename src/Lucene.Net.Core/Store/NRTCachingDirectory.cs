using System;
using System.Collections.Generic;
using System.Linq;

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
    /// Wraps a <seealso cref="RAMDirectory"/>
    /// around any provided delegate directory, to
    /// be used during NRT search.
    ///
    /// <p>this class is likely only useful in a near-real-time
    /// context, where indexing rate is lowish but reopen
    /// rate is highish, resulting in many tiny files being
    /// written.  this directory keeps such segments (as well as
    /// the segments produced by merging them, as long as they
    /// are small enough), in RAM.</p>
    ///
    /// <p>this is safe to use: when your app calls {IndexWriter#commit},
    /// all cached files will be flushed from the cached and sync'd.</p>
    ///
    /// <p>Here's a simple example usage:
    ///
    /// <pre class="prettyprint">
    ///   Directory fsDir = FSDirectory.open(new File("/path/to/index"));
    ///   NRTCachingDirectory cachedFSDir = new NRTCachingDirectory(fsDir, 5.0, 60.0);
    ///   IndexWriterConfig conf = new IndexWriterConfig(Version.LUCENE_48, analyzer);
    ///   IndexWriter writer = new IndexWriter(cachedFSDir, conf);
    /// </pre>
    ///
    /// <p>this will cache all newly flushed segments, all merges
    /// whose expected segment size is <= 5 MB, unless the net
    /// cached bytes exceeds 60 MB at which point all writes will
    /// not be cached (until the net bytes falls below 60 MB).</p>
    ///
    /// @lucene.experimental
    /// </summary>

    public class NRTCachingDirectory : Directory
    {
        private readonly RAMDirectory Cache = new RAMDirectory();

        private readonly Directory @delegate;

        private readonly long MaxMergeSizeBytes;
        private readonly long MaxCachedBytes;

        private const bool VERBOSE = false;

        /// <summary>
        ///  We will cache a newly created output if 1) it's a
        ///  flush or a merge and the estimated size of the merged segment is <=
        ///  maxMergeSizeMB, and 2) the total cached bytes is <=
        ///  maxCachedMB
        /// </summary>
        public NRTCachingDirectory(Directory @delegate, double maxMergeSizeMB, double maxCachedMB)
        {
            this.@delegate = @delegate;
            MaxMergeSizeBytes = (long)(maxMergeSizeMB * 1024 * 1024);
            MaxCachedBytes = (long)(maxCachedMB * 1024 * 1024);
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

        public override string LockID
        {
            get
            {
                return @delegate.LockID;
            }
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
            return "NRTCachingDirectory(" + @delegate + "; maxCacheMB=" + (MaxCachedBytes / 1024 / 1024.0) + " maxMergeSizeMB=" + (MaxMergeSizeBytes / 1024 / 1024.0) + ")";
        }

        public override string[] ListAll()
        {
            lock (this)
            {
                ISet<string> files = new HashSet<string>();
                foreach (string f in Cache.ListAll())
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
                catch (NoSuchDirectoryException ex)
                {
                    // however, if there are no cached files, then the directory truly
                    // does not "exist"
                    if (files.Count == 0)
                    {
                        throw ex;
                    }
                }
                return files.ToArray();
            }
        }

        /// <summary>
        /// Returns how many bytes are being used by the
        ///  RAMDirectory cache
        /// </summary>
        public virtual long SizeInBytes()
        {
            return Cache.SizeInBytes();
        }

        public override bool FileExists(string name)
        {
            lock (this)
            {
                return Cache.FileExists(name) || @delegate.FileExists(name);
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
                if (Cache.FileExists(name))
                {
                    Cache.DeleteFile(name);
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
                if (Cache.FileExists(name))
                {
                    return Cache.FileLength(name);
                }
                else
                {
                    return @delegate.FileLength(name);
                }
            }
        }

        public virtual string[] ListCachedFiles()
        {
            return Cache.ListAll();
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
                catch (System.IO.IOException ioe)
                {
                    // this is fine: file may not exist
                }
                return Cache.CreateOutput(name, context);
            }
            else
            {
                try
                {
                    Cache.DeleteFile(name);
                }
                catch (System.IO.IOException ioe)
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
                if (Cache.FileExists(name))
                {
                    if (VERBOSE)
                    {
                        Console.WriteLine("  from cache");
                    }
                    return Cache.OpenInput(name, context);
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
                if (Cache.FileExists(name))
                {
                    if (VERBOSE)
                    {
                        Console.WriteLine("  from cache");
                    }
                    return Cache.CreateSlicer(name, context);
                }
                else
                {
                    return @delegate.CreateSlicer(name, context);
                }
            }
        }

        /// <summary>
        /// Close this directory, which flushes any cached files
        ///  to the delegate and then closes the delegate.
        /// </summary>
        public override void Dispose()
        {
            // NOTE: technically we shouldn't have to do this, ie,
            // IndexWriter should have sync'd all files, but we do
            // it for defensive reasons... or in case the app is
            // doing something custom (creating outputs directly w/o
            // using IndexWriter):
            foreach (string fileName in Cache.ListAll())
            {
                UnCache(fileName);
            }
            Cache.Dispose();
            @delegate.Dispose();
        }

        /// <summary>
        /// Subclass can override this to customize logic; return
        ///  true if this file should be written to the RAMDirectory.
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

            return !name.Equals(IndexFileNames.SEGMENTS_GEN) && (bytes <= MaxMergeSizeBytes) && (bytes + Cache.SizeInBytes()) <= MaxCachedBytes;
        }

        private readonly object UncacheLock = new object();

        private void UnCache(string fileName)
        {
            // Only let one thread uncache at a time; this only
            // happens during commit() or close():
            lock (UncacheLock)
            {
                if (VERBOSE)
                {
                    Console.WriteLine("nrtdir.unCache name=" + fileName);
                }
                if (!Cache.FileExists(fileName))
                {
                    // Another thread beat us...
                    return;
                }
                IOContext context = IOContext.DEFAULT;
                IndexOutput @out = @delegate.CreateOutput(fileName, context);
                IndexInput @in = null;
                try
                {
                    @in = Cache.OpenInput(fileName, context);
                    @out.CopyBytes(@in, @in.Length());
                }
                finally
                {
                    IOUtils.Close(@in, @out);
                }

                // Lock order: uncacheLock -> this
                lock (this)
                {
                    // Must sync here because other sync methods have
                    // if (cache.fileExists(name)) { ... } else { ... }:
                    Cache.DeleteFile(fileName);
                }
            }
        }
    }
}