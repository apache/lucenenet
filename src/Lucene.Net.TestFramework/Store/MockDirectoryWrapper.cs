using System.Collections.Concurrent;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace Lucene.Net.Store
{
    using Lucene.Net.Randomized.Generators;
    using Lucene.Net.Support;
    using System.IO;

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

    using DirectoryReader = Lucene.Net.Index.DirectoryReader;
    using IndexWriter = Lucene.Net.Index.IndexWriter;
    using IndexWriterConfig = Lucene.Net.Index.IndexWriterConfig;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using NoDeletionPolicy = Lucene.Net.Index.NoDeletionPolicy;
    using SegmentInfos = Lucene.Net.Index.SegmentInfos;
    using TestUtil = Lucene.Net.Util.TestUtil;
    using ThrottledIndexOutput = Lucene.Net.Util.ThrottledIndexOutput;

    /// <summary>
    /// this is a Directory Wrapper that adds methods
    /// intended to be used only by unit tests.
    /// It also adds a number of features useful for testing:
    /// <ul>
    ///   <li> Instances created by <seealso cref="LuceneTestCase#newDirectory()"/> are tracked
    ///        to ensure they are closed by the test.</li>
    ///   <li> When a MockDirectoryWrapper is closed, it will throw an exception if
    ///        it has any open files against it (with a stacktrace indicating where
    ///        they were opened from).</li>
    ///   <li> When a MockDirectoryWrapper is closed, it runs CheckIndex to test if
    ///        the index was corrupted.</li>
    ///   <li> MockDirectoryWrapper simulates some "features" of Windows, such as
    ///        refusing to write/delete to open files.</li>
    /// </ul>
    /// </summary>
    public class MockDirectoryWrapper : BaseDirectoryWrapper
    {
        internal long MaxSize;

        // Max actual bytes used. this is set by MockRAMOutputStream:
        internal long MaxUsedSize;

        internal double RandomIOExceptionRate_Renamed;
        internal double RandomIOExceptionRateOnOpen_Renamed;
        internal Random RandomState;
        internal bool NoDeleteOpenFile_Renamed = true;
        internal bool assertNoDeleteOpenFile = false;
        internal bool PreventDoubleWrite_Renamed = true;
        internal bool TrackDiskUsage_Renamed = false;
        internal bool WrapLockFactory_Renamed = true;
        internal bool AllowRandomFileNotFoundException_Renamed = true;
        internal bool AllowReadingFilesStillOpenForWrite_Renamed = false;
        private ISet<string> UnSyncedFiles;
        private ISet<string> CreatedFiles;
        private ISet<string> OpenFilesForWrite = new HashSet<string>();
        internal ISet<string> OpenLocks = new ConcurrentHashSet<string>();
        internal volatile bool Crashed;
        private ThrottledIndexOutput ThrottledOutput;
        private Throttling_e throttling = Throttling_e.SOMETIMES;
        protected internal LockFactory LockFactory_Renamed;

        internal readonly AtomicLong InputCloneCount_Renamed = new AtomicLong();

        // use this for tracking files for crash.
        // additionally: provides debugging information in case you leave one open
        private readonly ConcurrentDictionary<IDisposable, Exception> OpenFileHandles = new ConcurrentDictionary<IDisposable, Exception>();

        // NOTE: we cannot initialize the Map here due to the
        // order in which our constructor actually does this
        // member initialization vs when it calls super.  It seems
        // like super is called, then our members are initialized:
        private IDictionary<string, int> OpenFiles;

        // Only tracked if noDeleteOpenFile is true: if an attempt
        // is made to delete an open file, we enroll it here.
        private ISet<string> OpenFilesDeleted;

        private void Init()
        {
            lock (this)
            {
                if (OpenFiles == null)
                {
                    OpenFiles = new Dictionary<string, int>();
                    OpenFilesDeleted = new HashSet<string>();
                }

                if (CreatedFiles == null)
                {
                    CreatedFiles = new HashSet<string>();
                }
                if (UnSyncedFiles == null)
                {
                    UnSyncedFiles = new HashSet<string>();
                }
            }
        }

        public MockDirectoryWrapper(Random random, Directory @delegate)
            : base(@delegate)
        {
            // must make a private random since our methods are
            // called from different threads; else test failures may
            // not be reproducible from the original seed
            this.RandomState = new Random(random.Next());
            this.ThrottledOutput = new ThrottledIndexOutput(ThrottledIndexOutput.MBitsToBytes(40 + RandomState.Next(10)), 5 + RandomState.Next(5), null);
            // force wrapping of lockfactory
            this.LockFactory_Renamed = new MockLockFactoryWrapper(this, @delegate.LockFactory);
            Init();
        }

        public virtual int InputCloneCount
        {
            get
            {
                return (int)InputCloneCount_Renamed.Get();
            }
        }

        public virtual bool TrackDiskUsage
        {
            set
            {
                TrackDiskUsage_Renamed = value;
            }
        }

        /// <summary>
        /// If set to true, we throw anSystem.IO.IOException if the same
        ///  file is opened by createOutput, ever.
        /// </summary>
        public virtual bool PreventDoubleWrite
        {
            set
            {
                PreventDoubleWrite_Renamed = value;
            }
        }

        /// <summary>
        /// If set to true (the default), when we throw random
        /// System.IO.IOException on openInput or createOutput, we may
        ///  sometimes throw FileNotFoundException or
        ///  NoSuchFileException.
        /// </summary>
        public virtual bool AllowRandomFileNotFoundException
        {
            set
            {
                AllowRandomFileNotFoundException_Renamed = value;
            }
        }

        /// <summary>
        /// If set to true, you can open an inputstream on a file
        ///  that is still open for writes.
        /// </summary>
        public virtual bool AllowReadingFilesStillOpenForWrite
        {
            set
            {
                AllowReadingFilesStillOpenForWrite_Renamed = value;
            }
        }

        /// <summary>
        /// Enum for controlling hard disk throttling.
        /// Set via <seealso cref="MockDirectoryWrapper #setThrottling(Throttling)"/>
        /// <p>
        /// WARNING: can make tests very slow.
        /// </summary>
        public enum Throttling_e
        {
            /// <summary>
            /// always emulate a slow hard disk. could be very slow! </summary>
            ALWAYS,

            /// <summary>
            /// sometimes (2% of the time) emulate a slow hard disk. </summary>
            SOMETIMES,

            /// <summary>
            /// never throttle output </summary>
            NEVER
        }

        public virtual Throttling_e Throttling
        {
            set
            {
                this.throttling = value;
            }
        }

        /// <summary>
        /// Returns true if <seealso cref="#in"/> must sync its files.
        /// Currently, only <seealso cref="NRTCachingDirectory"/> requires sync'ing its files
        /// because otherwise they are cached in an internal <seealso cref="RAMDirectory"/>. If
        /// other directories require that too, they should be added to this method.
        /// </summary>
        private bool MustSync()
        {
            Directory @delegate = @in;
            while (@delegate is FilterDirectory)
            {
                @delegate = ((FilterDirectory)@delegate).Delegate;
            }
            return @delegate is NRTCachingDirectory;
        }

        public override void Sync(ICollection<string> names)
        {
            lock (this)
            {
                MaybeYield();
                MaybeThrowDeterministicException();
                if (Crashed)
                {
                    throw new System.IO.IOException("cannot sync after crash");
                }
                // don't wear out our hardware so much in tests.
                if (LuceneTestCase.Rarely(RandomState) || MustSync())
                {
                    foreach (string name in names)
                    {
                        // randomly fail with IOE on any file
                        MaybeThrowIOException(name);
                        @in.Sync(new[] { name });
                        UnSyncedFiles.Remove(name);
                    }
                }
                else
                {
                    UnSyncedFiles.RemoveAll(names);
                }
            }
        }

        public long SizeInBytes()
        {
            lock (this)
            {
                if (@in is RAMDirectory)
                {
                    return ((RAMDirectory)@in).SizeInBytes();
                }
                else
                {
                    // hack
                    long size = 0;
                    foreach (string file in @in.ListAll())
                    {
                        size += @in.FileLength(file);
                    }
                    return size;
                }
            }
        }

        /// <summary>
        /// Simulates a crash of OS or machine by overwriting
        ///  unsynced files.
        /// </summary>
        public void Crash()
        {
            lock (this)
            {
                Crashed = true;
                OpenFiles = new Dictionary<string, int>();
                OpenFilesForWrite = new HashSet<string>();
                OpenFilesDeleted = new HashSet<string>();
                IEnumerator<string> it = UnSyncedFiles.GetEnumerator();
                UnSyncedFiles = new HashSet<string>();
                // first force-close all files, so we can corrupt on windows etc.
                // clone the file map, as these guys want to remove themselves on close.
                var m = OpenFileHandles.Keys.ToArray();
                foreach (IDisposable f in m)
                {
                    try
                    {
                        f.Dispose();
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("Crash(): f.Dispose() FAILED for {0}:\n{1}", f.ToString(), ex.ToString());
                    }
                }

                while (it.MoveNext())
                {
                    string name = it.Current;
                    int damage = RandomState.Next(5);
                    string action = null;

                    if (damage == 0)
                    {
                        action = "deleted";
                        DeleteFile(name, true);
                    }
                    else if (damage == 1)
                    {
                        action = "zeroed";
                        // Zero out file entirely
                        long length = FileLength(name);
                        var zeroes = new byte[256]; // LUCENENET TODO: Don't we want to fill the array before writing from it?
                        long upto = 0;
                        IndexOutput @out = @in.CreateOutput(name, LuceneTestCase.NewIOContext(RandomState));
                        while (upto < length)
                        {
                            var limit = (int)Math.Min(length - upto, zeroes.Length);
                            @out.WriteBytes(zeroes, 0, limit);
                            upto += limit;
                        }
                        @out.Dispose();
                    }
                    else if (damage == 2)
                    {
                        action = "partially truncated";
                        // Partially Truncate the file:

                        // First, make temp file and copy only half this
                        // file over:
                        string tempFileName;
                        while (true)
                        {
                            tempFileName = "" + RandomState.Next();
                            if (!LuceneTestCase.SlowFileExists(@in, tempFileName))
                            {
                                break;
                            }
                        }
                        IndexOutput tempOut = @in.CreateOutput(tempFileName, LuceneTestCase.NewIOContext(RandomState));
                        IndexInput ii = @in.OpenInput(name, LuceneTestCase.NewIOContext(RandomState));
                        tempOut.CopyBytes(ii, ii.Length() / 2);
                        tempOut.Dispose();
                        ii.Dispose();

                        // Delete original and copy bytes back:
                        DeleteFile(name, true);

                        IndexOutput @out = @in.CreateOutput(name, LuceneTestCase.NewIOContext(RandomState));
                        ii = @in.OpenInput(tempFileName, LuceneTestCase.NewIOContext(RandomState));
                        @out.CopyBytes(ii, ii.Length());
                        @out.Dispose();
                        ii.Dispose();
                        DeleteFile(tempFileName, true);
                    }
                    else if (damage == 3)
                    {
                        // The file survived intact:
                        action = "didn't change";
                    }
                    else
                    {
                        action = "fully truncated";
                        // Totally truncate the file to zero bytes
                        DeleteFile(name, true);
                        IndexOutput @out = @in.CreateOutput(name, LuceneTestCase.NewIOContext(RandomState));
                        @out.Length = 0;
                        @out.Dispose();
                    }
                    if (LuceneTestCase.VERBOSE)
                    {
                        Console.WriteLine("MockDirectoryWrapper: " + action + " unsynced file: " + name);
                    }
                }
            }
        }

        public virtual void ClearCrash()
        {
            lock (this)
            {
                Crashed = false;
                OpenLocks.Clear();
            }
        }

        public virtual long MaxSizeInBytes
        {
            set
            {
                this.MaxSize = value;
            }
            get
            {
                return this.MaxSize;
            }
        }

        /// <summary>
        /// Returns the peek actual storage used (bytes) in this
        /// directory.
        /// </summary>
        public virtual long MaxUsedSizeInBytes
        {
            get
            {
                return this.MaxUsedSize;
            }
        }

        public virtual void ResetMaxUsedSizeInBytes()
        {
            this.MaxUsedSize = RecomputedActualSizeInBytes;
        }

        /// <summary>
        /// Emulate windows whereby deleting an open file is not
        /// allowed (raiseSystem.IO.IOException).
        /// </summary>
        public virtual bool NoDeleteOpenFile
        {
            set
            {
                this.NoDeleteOpenFile_Renamed = value;
            }
            get
            {
                return NoDeleteOpenFile_Renamed;
            }
        }

        /// <summary>
        /// Trip a test assert if there is an attempt
        /// to delete an open file.
        /// </summary>
        public virtual bool AssertNoDeleteOpenFile
        {
            set
            {
                this.assertNoDeleteOpenFile = value;
            }
            get
            {
                return assertNoDeleteOpenFile;
            }
        }

        /// <summary>
        /// If 0.0, no exceptions will be thrown.  Else this should
        /// be a double 0.0 - 1.0.  We will randomly throw an
        ///System.IO.IOException on the first write to an OutputStream based
        /// on this probability.
        /// </summary>
        public virtual double RandomIOExceptionRate
        {
            set
            {
                RandomIOExceptionRate_Renamed = value;
            }
            get
            {
                return RandomIOExceptionRate_Renamed;
            }
        }

        /// <summary>
        /// If 0.0, no exceptions will be thrown during openInput
        /// and createOutput.  Else this should
        /// be a double 0.0 - 1.0 and we will randomly throw an
        ///System.IO.IOException in openInput and createOutput with
        /// this probability.
        /// </summary>
        public virtual double RandomIOExceptionRateOnOpen
        {
            set
            {
                RandomIOExceptionRateOnOpen_Renamed = value;
            }
            get
            {
                return RandomIOExceptionRateOnOpen_Renamed;
            }
        }

        internal virtual void MaybeThrowIOException(string message)
        {
            if (RandomState.NextDouble() < RandomIOExceptionRate_Renamed)
            {
                /*if (LuceneTestCase.VERBOSE)
                {
                  Console.WriteLine(Thread.CurrentThread.Name + ": MockDirectoryWrapper: now throw random exception" + (message == null ? "" : " (" + message + ")"));
                  (new Exception()).printStackTrace(System.out);
                }*/
                throw new System.IO.IOException("a randomSystem.IO.IOException" + (message == null ? "" : " (" + message + ")"));
            }
        }

        internal virtual void MaybeThrowIOExceptionOnOpen(string name)
        {
            if (RandomState.NextDouble() < RandomIOExceptionRateOnOpen_Renamed)
            {
                /*if (LuceneTestCase.VERBOSE)
                {
                  Console.WriteLine(Thread.CurrentThread.Name + ": MockDirectoryWrapper: now throw random exception during open file=" + name);
                  (new Exception()).printStackTrace(System.out);
                }*/
                if (AllowRandomFileNotFoundException_Renamed == false || RandomState.NextBoolean())
                {
                    throw new System.IO.IOException("a randomSystem.IO.IOException (" + name + ")");
                }
                else
                {
                    throw RandomState.NextBoolean() ? new FileNotFoundException("a randomSystem.IO.IOException (" + name + ")") : new FileNotFoundException("a randomSystem.IO.IOException (" + name + ")");
                }
            }
        }

        public override void DeleteFile(string name)
        {
            lock (this)
            {
                MaybeYield();
                DeleteFile(name, false);
            }
        }

        // if there are any exceptions in OpenFileHandles
        // capture those as inner exceptions
        private Exception WithAdditionalErrorInformation(Exception t, string name, bool input)
        {
            lock (this)
            {
                foreach (var ent in OpenFileHandles)
                {
                    if (input && ent.Key is MockIndexInputWrapper && ((MockIndexInputWrapper)ent.Key).Name.Equals(name))
                    {
                        t = CreateException(t, ent.Value);
                        break;
                    }
                    else if (!input && ent.Key is MockIndexOutputWrapper && ((MockIndexOutputWrapper)ent.Key).Name.Equals(name))
                    {
                        t = CreateException(t, ent.Value);
                        break;
                    }
                }
                return t;
            }
        }

        private Exception CreateException(Exception exception, Exception innerException)
        {
            return (Exception)Activator.CreateInstance(exception.GetType(), exception.Message, innerException);
        }

        private void MaybeYield()
        {
            if (RandomState.NextBoolean())
            {
                Thread.Sleep(0);
            }
        }

        private void DeleteFile(string name, bool forced)
        {
            lock (this)
            {
                MaybeYield();

                MaybeThrowDeterministicException();

                if (Crashed && !forced)
                {
                    throw new System.IO.IOException("cannot delete after crash");
                }

                if (UnSyncedFiles.Contains(name))
                {
                    UnSyncedFiles.Remove(name);
                }
                if (!forced && (NoDeleteOpenFile_Renamed || assertNoDeleteOpenFile))
                {
                    if (OpenFiles.ContainsKey(name))
                    {
                        OpenFilesDeleted.Add(name);

                        if (!assertNoDeleteOpenFile)
                        {
                            throw WithAdditionalErrorInformation(new IOException("MockDirectoryWrapper: file \"" + name + "\" is still open: cannot delete"), name, true);
                        }
                        else
                        {
                            throw WithAdditionalErrorInformation(new AssertionException("MockDirectoryWrapper: file \"" + name + "\" is still open: cannot delete"), name, true);
                        }
                    }
                    else
                    {
                        OpenFilesDeleted.Remove(name);
                    }
                }
                @in.DeleteFile(name);
            }
        }

        public virtual ISet<string> OpenDeletedFiles
        {
            get
            {
                lock (this)
                {
                    return new HashSet<string>(OpenFilesDeleted);
                }
            }
        }

        private bool FailOnCreateOutput_Renamed = true;

        public virtual bool FailOnCreateOutput
        {
            set
            {
                FailOnCreateOutput_Renamed = value;
            }
        }

        public override IndexOutput CreateOutput(string name, IOContext context)
        {
            lock (this)
            {
                MaybeThrowDeterministicException();
                MaybeThrowIOExceptionOnOpen(name);
                MaybeYield();
                if (FailOnCreateOutput_Renamed)
                {
                    MaybeThrowDeterministicException();
                }
                if (Crashed)
                {
                    throw new System.IO.IOException("cannot createOutput after crash");
                }
                Init();
                lock (this)
                {
                    if (PreventDoubleWrite_Renamed && CreatedFiles.Contains(name) && !name.Equals("segments.gen"))
                    {
                        throw new System.IO.IOException("file \"" + name + "\" was already written to");
                    }
                }
                if ((NoDeleteOpenFile_Renamed || assertNoDeleteOpenFile) && OpenFiles.ContainsKey(name))
                {
                    if (!assertNoDeleteOpenFile)
                    {
                        throw new System.IO.IOException("MockDirectoryWrapper: file \"" + name + "\" is still open: cannot overwrite");
                    }
                    else
                    {
                        throw new InvalidOperationException("MockDirectoryWrapper: file \"" + name + "\" is still open: cannot overwrite");
                    }
                }

                if (Crashed)
                {
                    throw new System.IO.IOException("cannot createOutput after crash");
                }
                UnSyncedFiles.Add(name);
                CreatedFiles.Add(name);

                if (@in is RAMDirectory)
                {
                    RAMDirectory ramdir = (RAMDirectory)@in;
                    RAMFile file = new RAMFile(ramdir);
                    RAMFile existing = ramdir.GetNameFromFileMap_Nunit(name);

                    // Enforce write once:
                    if (existing != null && !name.Equals("segments.gen") && PreventDoubleWrite_Renamed)
                    {
                        throw new System.IO.IOException("file " + name + " already exists");
                    }
                    else
                    {
                        if (existing != null)
                        {
                            ramdir.GetAndAddSizeInBytes_Nunit(-existing.SizeInBytes);
                            existing.SetDirectory_Nunit(null);
                        }
                        ramdir.SetNameForFileMap_Nunit(name, file);
                    }
                }
                //System.out.println(Thread.currentThread().getName() + ": MDW: create " + name);
                IndexOutput delegateOutput = @in.CreateOutput(name, LuceneTestCase.NewIOContext(RandomState, context));
                if (RandomState.Next(10) == 0)
                {
                    // once in a while wrap the IO in a Buffered IO with random buffer sizes
                    delegateOutput = new BufferedIndexOutputWrapper(this, 1 + RandomState.Next(BufferedIndexOutput.DEFAULT_BUFFER_SIZE), delegateOutput);
                }
                IndexOutput io = new MockIndexOutputWrapper(this, delegateOutput, name);
                AddFileHandle(io, name, Handle.Output);
                OpenFilesForWrite.Add(name);

                // throttling REALLY slows down tests, so don't do it very often for SOMETIMES.
                if (throttling == Throttling_e.ALWAYS || (throttling == Throttling_e.SOMETIMES && RandomState.Next(50) == 0) && !(@in is RateLimitedDirectoryWrapper))
                {
                    if (LuceneTestCase.VERBOSE)
                    {
                        Console.WriteLine("MockDirectoryWrapper: throttling indexOutput (" + name + ")");
                    }
                    return ThrottledOutput.NewFromDelegate(io);
                }
                else
                {
                    return io;
                }
            }
        }

        internal enum Handle
        {
            Input,
            Output,
            Slice
        }

        internal void AddFileHandle(IDisposable c, string name, Handle handle)
        {
            //Trace.TraceInformation("Add {0} {1}", c, name);

            lock (this)
            {
                int v;
                if (OpenFiles.TryGetValue(name, out v))
                {
                    v++;
                    //Debug.WriteLine("Add {0} - {1} - {2}", c, name, v);
                    OpenFiles[name] = v;
                }
                else
                {
                    //Debug.WriteLine("Add {0} - {1} - {2}", c, name, 1);
                    OpenFiles[name] = 1;
                }

                OpenFileHandles[c] = new Exception("unclosed Index" + handle.ToString() + ": " + name);
            }
        }

        private bool FailOnOpenInput_Renamed = true;

        public virtual bool FailOnOpenInput
        {
            set
            {
                FailOnOpenInput_Renamed = value;
            }
        }

        public override IndexInput OpenInput(string name, IOContext context)
        {
            lock (this)
            {
                MaybeThrowDeterministicException();
                MaybeThrowIOExceptionOnOpen(name);
                MaybeYield();
                if (FailOnOpenInput_Renamed)
                {
                    MaybeThrowDeterministicException();
                }
                if (!LuceneTestCase.SlowFileExists(@in, name))
                {
                    throw new FileNotFoundException(name + " in dir=" + @in);
                }

                // cannot open a file for input if it's still open for
                // output, except for segments.gen and segments_N
                if (!AllowReadingFilesStillOpenForWrite_Renamed && OpenFilesForWrite.Contains(name) && !name.StartsWith("segments"))
                {
                    throw WithAdditionalErrorInformation(new IOException("MockDirectoryWrapper: file \"" + name + "\" is still open for writing"), name, false);
                }

                IndexInput delegateInput = @in.OpenInput(name, LuceneTestCase.NewIOContext(RandomState, context));

                IndexInput ii;
                int randomInt = RandomState.Next(500);
                if (randomInt == 0)
                {
                    if (LuceneTestCase.VERBOSE)
                    {
                        Console.WriteLine("MockDirectoryWrapper: using SlowClosingMockIndexInputWrapper for file " + name);
                    }
                    ii = new SlowClosingMockIndexInputWrapper(this, name, delegateInput);
                }
                else if (randomInt == 1)
                {
                    if (LuceneTestCase.VERBOSE)
                    {
                        Console.WriteLine("MockDirectoryWrapper: using SlowOpeningMockIndexInputWrapper for file " + name);
                    }
                    ii = new SlowOpeningMockIndexInputWrapper(this, name, delegateInput);
                }
                else
                {
                    ii = new MockIndexInputWrapper(this, name, delegateInput);
                }
                AddFileHandle(ii, name, Handle.Input);
                return ii;
            }
        }

        /// <summary>
        /// Provided for testing purposes.  Use sizeInBytes() instead. </summary>
        public long RecomputedSizeInBytes
        {
            get
            {
                lock (this)
                {
                    if (!(@in is RAMDirectory))
                    {
                        return SizeInBytes();
                    }
                    long size = 0;
                    foreach (RAMFile file in ((RAMDirectory)@in).GetFileMapValues_Nunit())
                    {
                        size += file.SizeInBytes;
                    }
                    return size;
                }
            }
        }

        /// <summary>
        /// Like getRecomputedSizeInBytes(), but, uses actual file
        /// lengths rather than buffer allocations (which are
        /// quantized up to nearest
        /// RAMOutputStream.BUFFER_SIZE (now 1024) bytes.
        /// </summary>

        public long RecomputedActualSizeInBytes
        {
            get
            {
                lock (this)
                {
                    if (!(@in is RAMDirectory))
                    {
                        return SizeInBytes();
                    }
                    long size = 0;
                    foreach (RAMFile file in ((RAMDirectory)@in).GetFileMapValues_Nunit())
                    {
                        size += file.Length;
                    }
                    return size;
                }
            }
        }

        // NOTE: this is off by default; see LUCENE-5574
        private bool AssertNoUnreferencedFilesOnClose;

        public virtual bool AssertNoUnrefencedFilesOnClose
        {
            set
            {
                AssertNoUnreferencedFilesOnClose = value;
            }
        }

        /// <summary>
        /// Set to false if you want to return the pure lockfactory
        /// and not wrap it with MockLockFactoryWrapper.
        /// <p>
        /// Be careful if you turn this off: MockDirectoryWrapper might
        /// no longer be able to detect if you forget to close an IndexWriter,
        /// and spit out horribly scary confusing exceptions instead of
        /// simply telling you that.
        /// </summary>
        public virtual bool WrapLockFactory
        {
            set
            {
                this.WrapLockFactory_Renamed = value;
            }
        }

        public override void Dispose()
        {
            lock (this)
            {
                // files that we tried to delete, but couldn't because readers were open.
                // all that matters is that we tried! (they will eventually go away)
                ISet<string> pendingDeletions = new HashSet<string>(OpenFilesDeleted);
                MaybeYield();
                if (OpenFiles == null)
                {
                    OpenFiles = new Dictionary<string, int>();
                    OpenFilesDeleted = new HashSet<string>();
                }
                if (OpenFiles.Count > 0)
                {
                    // print the first one as its very verbose otherwise
                    Exception cause = null;
                    IEnumerator<Exception> stacktraces = OpenFileHandles.Values.GetEnumerator();
                    if (stacktraces.MoveNext())
                    {
                        cause = stacktraces.Current;
                    }

                    // RuntimeException instead ofSystem.IO.IOException because
                    // super() does not throwSystem.IO.IOException currently:
                    throw new Exception("MockDirectoryWrapper: cannot close: there are still open files: "
                        + String.Join(" ,", OpenFiles.ToArray().Select(x => x.Key)), cause);
                }
                if (OpenLocks.Count > 0)
                {
                    throw new Exception("MockDirectoryWrapper: cannot close: there are still open locks: "
                        + String.Join(" ,", OpenLocks.ToArray()));
                }

                IsOpen = false;
                if (CheckIndexOnClose)
                {
                    RandomIOExceptionRate_Renamed = 0.0;
                    RandomIOExceptionRateOnOpen_Renamed = 0.0;
                    if (DirectoryReader.IndexExists(this))
                    {
                        if (LuceneTestCase.VERBOSE)
                        {
                            Console.WriteLine("\nNOTE: MockDirectoryWrapper: now crush");
                        }
                        Crash(); // corrupt any unsynced-files
                        if (LuceneTestCase.VERBOSE)
                        {
                            Console.WriteLine("\nNOTE: MockDirectoryWrapper: now run CheckIndex");
                        }
                        TestUtil.CheckIndex(this, CrossCheckTermVectorsOnClose);

                        // TODO: factor this out / share w/ TestIW.assertNoUnreferencedFiles
                        if (AssertNoUnreferencedFilesOnClose)
                        {
                            // now look for unreferenced files: discount ones that we tried to delete but could not
                            HashSet<string> allFiles = new HashSet<string>(Arrays.AsList(ListAll()));
                            allFiles.RemoveAll(pendingDeletions);
                            string[] startFiles = allFiles.ToArray(/*new string[0]*/);
                            IndexWriterConfig iwc = new IndexWriterConfig(LuceneTestCase.TEST_VERSION_CURRENT, null);
                            iwc.SetIndexDeletionPolicy(NoDeletionPolicy.INSTANCE);
                            (new IndexWriter(@in, iwc)).Rollback();
                            string[] endFiles = @in.ListAll();

                            ISet<string> startSet = new SortedSet<string>(Arrays.AsList(startFiles));
                            ISet<string> endSet = new SortedSet<string>(Arrays.AsList(endFiles));

                            if (pendingDeletions.Contains("segments.gen") && endSet.Contains("segments.gen"))
                            {
                                // this is possible if we hit an exception while writing segments.gen, we try to delete it
                                // and it ends out in pendingDeletions (but IFD wont remove this).
                                startSet.Add("segments.gen");
                                if (LuceneTestCase.VERBOSE)
                                {
                                    Console.WriteLine("MDW: Unreferenced check: Ignoring segments.gen that we could not delete.");
                                }
                            }

                            // its possible we cannot delete the segments_N on windows if someone has it open and
                            // maybe other files too, depending on timing. normally someone on windows wouldnt have
                            // an issue (IFD would nuke this stuff eventually), but we pass NoDeletionPolicy...
                            foreach (string file in pendingDeletions)
                            {
                                if (file.StartsWith("segments") && !file.Equals("segments.gen") && endSet.Contains(file))
                                {
                                    startSet.Add(file);
                                    if (LuceneTestCase.VERBOSE)
                                    {
                                        Console.WriteLine("MDW: Unreferenced check: Ignoring segments file: " + file + " that we could not delete.");
                                    }
                                    SegmentInfos sis = new SegmentInfos();
                                    try
                                    {
                                        sis.Read(@in, file);
                                    }
                                    catch (System.IO.IOException ioe)
                                    {
                                        // OK: likely some of the .si files were deleted
                                    }

                                    try
                                    {
                                        ISet<string> ghosts = new HashSet<string>(sis.Files(@in, false));
                                        foreach (string s in ghosts)
                                        {
                                            if (endSet.Contains(s) && !startSet.Contains(s))
                                            {
                                                Debug.Assert(pendingDeletions.Contains(s));
                                                if (LuceneTestCase.VERBOSE)
                                                {
                                                    Console.WriteLine("MDW: Unreferenced check: Ignoring referenced file: " + s + " " + "from " + file + " that we could not delete.");
                                                }
                                                startSet.Add(s);
                                            }
                                        }
                                    }
                                    catch (Exception t)
                                    {
                                        Console.Error.WriteLine("ERROR processing leftover segments file " + file + ":");
                                        Console.WriteLine(t.ToString());
                                        Console.Write(t.StackTrace);
                                    }
                                }
                            }

                            startFiles = startSet.ToArray(/*new string[0]*/);
                            endFiles = endSet.ToArray(/*new string[0]*/);

                            if (!Arrays.Equals(startFiles, endFiles))
                            {
                                IList<string> removed = new List<string>();
                                foreach (string fileName in startFiles)
                                {
                                    if (!endSet.Contains(fileName))
                                    {
                                        removed.Add(fileName);
                                    }
                                }

                                IList<string> added = new List<string>();
                                foreach (string fileName in endFiles)
                                {
                                    if (!startSet.Contains(fileName))
                                    {
                                        added.Add(fileName);
                                    }
                                }

                                string extras;
                                if (removed.Count != 0)
                                {
                                    extras = "\n\nThese files were removed: " + removed;
                                }
                                else
                                {
                                    extras = "";
                                }

                                if (added.Count != 0)
                                {
                                    extras += "\n\nThese files were added (waaaaaaaaaat!): " + added;
                                }

                                if (pendingDeletions.Count != 0)
                                {
                                    extras += "\n\nThese files we had previously tried to delete, but couldn't: " + pendingDeletions;
                                }

                                Debug.Assert(false, "unreferenced files: before delete:\n    " + Arrays.ToString(startFiles) + "\n  after delete:\n    " + Arrays.ToString(endFiles) + extras);
                            }

                            DirectoryReader ir1 = DirectoryReader.Open(this);
                            int numDocs1 = ir1.NumDocs;
                            ir1.Dispose();
                            (new IndexWriter(this, new IndexWriterConfig(LuceneTestCase.TEST_VERSION_CURRENT, null))).Dispose();
                            DirectoryReader ir2 = DirectoryReader.Open(this);
                            int numDocs2 = ir2.NumDocs;
                            ir2.Dispose();
                            Debug.Assert(numDocs1 == numDocs2, "numDocs changed after opening/closing IW: before=" + numDocs1 + " after=" + numDocs2);
                        }
                    }
                }
                @in.Dispose();
            }
        }

        internal virtual void RemoveOpenFile(IDisposable c, string name)
        {
            //Trace.TraceInformation("Rem {0} {1}", c, name);

            lock (this)
            {
                int v;
                if (OpenFiles.TryGetValue(name, out v))
                {
                    if (v == 1)
                    {
                        //Debug.WriteLine("RemoveOpenFile OpenFiles.Remove {0} - {1}", c, name);
                        OpenFiles.Remove(name);
                    }
                    else
                    {
                        v--;
                        OpenFiles[name] = v;
                        //Debug.WriteLine("RemoveOpenFile OpenFiles DECREMENT {0} - {1} - {2}", c, name, v);
                    }
                }

                Exception _;
                OpenFileHandles.TryRemove(c, out _);
            }
        }

        public virtual void RemoveIndexOutput(IndexOutput @out, string name)
        {
            lock (this)
            {
                OpenFilesForWrite.Remove(name);
                RemoveOpenFile(@out, name);
            }
        }

        public virtual void RemoveIndexInput(IndexInput @in, string name)
        {
            lock (this)
            {
                RemoveOpenFile(@in, name);
            }
        }

        /// <summary>
        /// Objects that represent fail-able conditions. Objects of a derived
        /// class are created and registered with the mock directory. After
        /// register, each object will be invoked once for each first write
        /// of a file, giving the object a chance to throw anSystem.IO.IOException.
        /// </summary>
        public class Failure
        {
            /// <summary>
            /// eval is called on the first write of every new file.
            /// </summary>
            public virtual void Eval(MockDirectoryWrapper dir)
            {
            }

            /// <summary>
            /// reset should set the state of the failure to its default
            /// (freshly constructed) state. Reset is convenient for tests
            /// that want to create one failure object and then reuse it in
            /// multiple cases. this, combined with the fact that Failure
            /// subclasses are often anonymous classes makes reset difficult to
            /// do otherwise.
            ///
            /// A typical example of use is
            /// Failure failure = new Failure() { ... };
            /// ...
            /// mock.failOn(failure.reset())
            /// </summary>
            public virtual Failure Reset()
            {
                return this;
            }

            protected internal bool DoFail;

            public virtual void SetDoFail()
            {
                DoFail = true;
            }

            public virtual void ClearDoFail()
            {
                DoFail = false;
            }
        }

        internal List<Failure> Failures;

        /// <summary>
        /// add a Failure object to the list of objects to be evaluated
        /// at every potential failure point
        /// </summary>
        public virtual void FailOn(Failure fail)
        {
            lock (this)
            {
                if (Failures == null)
                {
                    Failures = new List<Failure>();
                }
                Failures.Add(fail);
            }
        }

        /// <summary>
        /// Iterate through the failures list, giving each object a
        /// chance to throw an IOE
        /// </summary>
        internal virtual void MaybeThrowDeterministicException()
        {
            lock (this)
            {
                if (Failures != null)
                {
                    for (int i = 0; i < Failures.Count; i++)
                    {
                        Failures[i].Eval(this);
                    }
                }
            }
        }

        public override string[] ListAll()
        {
            lock (this)
            {
                MaybeYield();
                return @in.ListAll();
            }
        }

        public override bool FileExists(string name)
        {
            lock (this)
            {
                MaybeYield();
                return @in.FileExists(name);
            }
        }

        public override long FileLength(string name)
        {
            lock (this)
            {
                MaybeYield();
                return @in.FileLength(name);
            }
        }

        public override Lock MakeLock(string name)
        {
            lock (this)
            {
                MaybeYield();
                return LockFactory.MakeLock(name);
            }
        }

        public override void ClearLock(string name)
        {
            lock (this)
            {
                MaybeYield();
                LockFactory.ClearLock(name);
            }
        }

        public override LockFactory LockFactory
        {
            set
            {
                lock (this)
                {
                    MaybeYield();
                    // sneaky: we must pass the original this way to the dir, because
                    // some impls (e.g. FSDir) do instanceof here.
                    @in.LockFactory = value;
                    // now set our wrapped factory here
                    this.LockFactory_Renamed = new MockLockFactoryWrapper(this, value);
                }
            }
            get
            {
                lock (this)
                {
                    MaybeYield();
                    if (WrapLockFactory_Renamed)
                    {
                        return LockFactory_Renamed;
                    }
                    else
                    {
                        return @in.LockFactory;
                    }
                }
            }
        }

        public override string LockID
        {
            get
            {
                lock (this)
                {
                    MaybeYield();
                    return @in.LockID;
                }
            }
        }

        public override void Copy(Directory to, string src, string dest, IOContext context)
        {
            lock (this)
            {
                MaybeYield();
                // randomize the IOContext here?
                @in.Copy(to, src, dest, context);
            }
        }

        public override IndexInputSlicer CreateSlicer(string name, IOContext context)
        {
            MaybeYield();
            if (!LuceneTestCase.SlowFileExists(@in, name))
            {
                throw RandomState.NextBoolean() ? new FileNotFoundException(name) : new FileNotFoundException(name);
            }
            // cannot open a file for input if it's still open for
            // output, except for segments.gen and segments_N

            if (OpenFilesForWrite.Contains(name) && !name.StartsWith("segments"))
            {
                throw WithAdditionalErrorInformation(new IOException("MockDirectoryWrapper: file \"" + name + "\" is still open for writing"), name, false);
            }

            IndexInputSlicer delegateHandle = @in.CreateSlicer(name, context);
            IndexInputSlicer handle = new IndexInputSlicerAnonymousInnerClassHelper(this, name, delegateHandle);
            AddFileHandle(handle, name, Handle.Slice);
            return handle;
        }

        private class IndexInputSlicerAnonymousInnerClassHelper : IndexInputSlicer
        {
            private readonly MockDirectoryWrapper OuterInstance;

            private string Name;
            private IndexInputSlicer DelegateHandle;

            public IndexInputSlicerAnonymousInnerClassHelper(MockDirectoryWrapper outerInstance, string name, IndexInputSlicer delegateHandle)
                : base(outerInstance)
            {
                this.OuterInstance = outerInstance;
                this.Name = name;
                this.DelegateHandle = delegateHandle;
            }

            private int disposed = 0;

            public override void Dispose(bool disposing)
            {
                if (0 == Interlocked.CompareExchange(ref this.disposed, 1, 0))
                {
                    if (disposing)
                    {
                        DelegateHandle.Dispose();
                        OuterInstance.RemoveOpenFile(OuterInstance, Name);
                    }
                }
            }

            public override IndexInput OpenSlice(string sliceDescription, long offset, long length)
            {
                OuterInstance.MaybeYield();
                IndexInput ii = new MockIndexInputWrapper(OuterInstance, Name, DelegateHandle.OpenSlice(sliceDescription, offset, length));
                OuterInstance.AddFileHandle(ii, Name, Handle.Input);
                return ii;
            }

            public override IndexInput OpenFullSlice()
            {
                OuterInstance.MaybeYield();
                IndexInput ii = new MockIndexInputWrapper(OuterInstance, Name, DelegateHandle.OpenFullSlice());
                OuterInstance.AddFileHandle(ii, Name, Handle.Input);
                return ii;
            }
        }

        internal sealed class BufferedIndexOutputWrapper : BufferedIndexOutput
        {
            private readonly MockDirectoryWrapper OuterInstance;

            internal readonly IndexOutput Io;

            public BufferedIndexOutputWrapper(MockDirectoryWrapper outerInstance, int bufferSize, IndexOutput io)
                : base(bufferSize)
            {
                this.OuterInstance = outerInstance;
                this.Io = io;
            }

            public override long Length
            {
                get
                {
                    return Io.Length;
                }
            }

            protected internal override void FlushBuffer(byte[] b, int offset, int len)
            {
                Io.WriteBytes(b, offset, len);
            }

            public override void Seek(long pos)
            {
                Flush();
                Io.Seek(pos);
            }

            public override void Flush()
            {
                try
                {
                    base.Flush();
                }
                finally
                {
                    Io.Flush();
                }
            }

            public override void Dispose()
            {
                try
                {
                    base.Dispose();
                }
                finally
                {
                    Io.Dispose();
                }
            }
        }

        /// <summary>
        /// Use this when throwing fake {@codeSystem.IO.IOException},
        ///  e.g. from <seealso cref="MockDirectoryWrapper.Failure"/>.
        /// </summary>
        public class FakeIOException : System.IO.IOException
        {
        }
    }
}