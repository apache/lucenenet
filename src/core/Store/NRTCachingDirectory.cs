using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lucene.Net.Index;
using Lucene.Net.Util;

namespace Lucene.Net.Store
{
    public class NRTCachingDirectory : Directory
    {
        private RAMDirectory cache = new RAMDirectory();

        private Directory del;

        private readonly long maxMergeSizeBytes;
        private readonly long maxCachedBytes;

        private static readonly bool VERBOSE = false;

        public NRTCachingDirectory(Directory del, double maxMergeSizeMB, double maxCachedMB)
        {
            this.del = del;
            maxMergeSizeBytes = (long)(maxMergeSizeMB * 1024 * 1024);
            maxCachedBytes = (long)(maxCachedMB * 1024 * 1024);
        }

        public Directory Delegate
        {
            get { return del; }
        }

        public override LockFactory LockFactory
        {
            get
            {
                return del.LockFactory;
            }
            set
            {
                del.LockFactory = value;
            }
        }

        public override string LockId
        {
            get
            {
                return del.LockId;
            }
        }

        public override Lock MakeLock(string name)
        {
            return del.MakeLock(name);
        }

        public override void ClearLock(string name)
        {
            del.ClearLock(name);
        }

        public override string ToString()
        {
            return "NRTCachingDirectory(" + del + "; maxCacheMB=" + (maxCachedBytes / 1024 / 1024) + " maxMergeSizeMB=" + (maxMergeSizeBytes / 1024 / 1024) + ")";
        }

        public override string[] ListAll()
        {
            ISet<String> files = new HashSet<String>();
            foreach (String f in cache.ListAll())
            {
                files.Add(f);
            }
            // LUCENE-1468: our NRTCachingDirectory will actually exist (RAMDir!),
            // but if the underlying delegate is an FSDir and mkdirs() has not
            // yet been called, because so far everything is a cached write,
            // in this case, we don't want to throw a NoSuchDirectoryException
            try
            {
                foreach (String f in del.ListAll())
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

        public long SizeInBytes
        {
            get { return cache.SizeInBytes; }
        }

        public override bool FileExists(string name)
        {
            lock (this)
            {
                return cache.FileExists(name) || del.FileExists(name);
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
                if (cache.FileExists(name))
                {
                    //assert !delegate.fileExists(name): "name=" + name;
                    cache.DeleteFile(name);
                }
                else
                {
                    del.DeleteFile(name);
                }
            }
        }

        public override long FileLength(string name)
        {
            lock (this)
            {
                if (cache.FileExists(name))
                {
                    return cache.FileLength(name);
                }
                else
                {
                    return del.FileLength(name);
                }
            }
        }

        public string[] ListCachedFiles()
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
                    del.DeleteFile(name);
                }
                catch (System.IO.IOException)
                {
                    // This is fine: file may not exist
                }
                return cache.CreateOutput(name, context);
            }
            else
            {
                try
                {
                    cache.DeleteFile(name);
                }
                catch (System.IO.IOException)
                {
                    // This is fine: file may not exist
                }
                return del.CreateOutput(name, context);
            }
        }

        public override void Sync(ICollection<string> fileNames)
        {
            if (VERBOSE)
            {
                Console.WriteLine("nrtdir.sync files=" + fileNames);
            }
            foreach (String fileName in fileNames)
            {
                UnCache(fileName);
            }
            del.Sync(fileNames);
        }

        public override IndexInput OpenInput(string name, IOContext context)
        {
            lock (this)
            {
                if (VERBOSE)
                {
                    Console.WriteLine("nrtdir.openInput name=" + name);
                }
                if (cache.FileExists(name))
                {
                    if (VERBOSE)
                    {
                        Console.WriteLine("  from cache");
                    }
                    return cache.OpenInput(name, context);
                }
                else
                {
                    return del.OpenInput(name, context);
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
                if (cache.FileExists(name))
                {
                    if (VERBOSE)
                    {
                        Console.WriteLine("  from cache");
                    }
                    return cache.CreateSlicer(name, context);
                }
                else
                {
                    return del.CreateSlicer(name, context);
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // NOTE: technically we shouldn't have to do this, ie,
                // IndexWriter should have sync'd all files, but we do
                // it for defensive reasons... or in case the app is
                // doing something custom (creating outputs directly w/o
                // using IndexWriter):
                foreach (String fileName in cache.ListAll())
                {
                    UnCache(fileName);
                }
                cache.Dispose();
                del.Dispose();
            }

            cache = null;
            del = null;
        }

        protected bool DoCacheWrite(string name, IOContext context)
        {
            //System.out.println(Thread.currentThread().getName() + ": CACHE check merge=" + merge + " size=" + (merge==null ? 0 : merge.estimatedMergeBytes));

            long bytes = 0;
            if (context.mergeInfo != null)
            {
                bytes = context.mergeInfo.estimatedMergeBytes;
            }
            else if (context.flushInfo != null)
            {
                bytes = context.flushInfo.estimatedSegmentSize;
            }

            return !name.Equals(IndexFileNames.SEGMENTS_GEN) && (bytes <= maxMergeSizeBytes) && (bytes + cache.SizeInBytes) <= maxCachedBytes;
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
                if (!cache.FileExists(fileName))
                {
                    // Another thread beat us...
                    return;
                }
                if (del.FileExists(fileName))
                {
                    throw new System.IO.IOException("cannot uncache file=\"" + fileName + "\": it was separately also created in the delegate directory");
                }
                IOContext context = IOContext.DEFAULT;
                IndexOutput output = del.CreateOutput(fileName, context);
                IndexInput input = null;
                try
                {
                    input = cache.OpenInput(fileName, context);
                    output.CopyBytes(input, input.Length);
                }
                finally
                {
                    IOUtils.Close(input, output);
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
