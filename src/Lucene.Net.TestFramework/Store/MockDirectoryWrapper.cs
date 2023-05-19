using J2N.Runtime.CompilerServices;
using J2N.Threading.Atomic;
using Lucene.Net.Diagnostics;
using Lucene.Net.Index;
using Lucene.Net.Index.Extensions;
using Lucene.Net.Support;
using Lucene.Net.Support.Threading;
using Lucene.Net.Util;
using RandomizedTesting.Generators;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Console = Lucene.Net.Util.SystemConsole;
using JCG = J2N.Collections.Generic;
#if FEATURE_SERIALIZABLE_EXCEPTIONS
using System.Runtime.Serialization;
#endif

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

    /// <summary>
    /// Enum for controlling hard disk throttling.
    /// Set via <see cref="MockDirectoryWrapper.Throttling"/>
    /// <para/>
    /// WARNING: can make tests very slow.
    /// </summary>
    public enum Throttling
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

    /// <summary>
    /// This is a Directory Wrapper that adds methods
    /// intended to be used only by unit tests.
    /// It also adds a number of features useful for testing:
    /// <list type="bullet">
    ///     <item>
    ///         <description>
    ///             Instances created by <see cref="LuceneTestCase.NewDirectory()"/> are tracked
    ///             to ensure they are disposed by the test.
    ///         </description>
    ///     </item>
    ///     <item>
    ///         <description>
    ///             When a <see cref="MockDirectoryWrapper"/> is disposed, it will throw an exception if
    ///             it has any open files against it (with a stacktrace indicating where
    ///             they were opened from).
    ///         </description>
    ///     </item>
    ///     <item>
    ///         <description>
    ///             When a <see cref="MockDirectoryWrapper"/> is disposed, it runs <see cref="Index.CheckIndex"/> to test if
    ///             the index was corrupted.
    ///         </description>
    ///     </item>
    ///     <item>
    ///         <description>
    ///             <see cref="MockDirectoryWrapper"/> simulates some "features" of Windows, such as
    ///             refusing to write/delete to open files.
    ///         </description>
    ///     </item>
    /// </list>
    /// </summary>
    public class MockDirectoryWrapper : BaseDirectoryWrapper
    {
        internal long maxSize;

        // Max actual bytes used. this is set by MockRAMOutputStream:
        internal long maxUsedSize;

        internal double randomIOExceptionRate;
        internal double randomIOExceptionRateOnOpen;
        internal Random randomState;
        internal bool noDeleteOpenFile = true;
        internal bool assertNoDeleteOpenFile = false;
        internal bool preventDoubleWrite = true;
        internal bool trackDiskUsage = false;
        internal bool wrapLockFactory = true;
        internal bool allowRandomFileNotFoundException = true;
        internal bool allowReadingFilesStillOpenForWrite = false;
        private ISet<string> unSyncedFiles;
        private ISet<string> createdFiles;
        private ISet<string> openFilesForWrite = new JCG.HashSet<string>(StringComparer.Ordinal);
        internal ISet<string> openLocks = new ConcurrentHashSet<string>(StringComparer.Ordinal);
        internal volatile bool crashed;
        private readonly ThrottledIndexOutput throttledOutput; // LUCENENET: marked readonly
        private Throttling throttling = Throttling.SOMETIMES;
        protected LockFactory m_lockFactory;

        internal readonly AtomicInt32 inputCloneCount = new AtomicInt32();

        // use this for tracking files for crash.
        // additionally: provides debugging information in case you leave one open
        private readonly ConcurrentDictionary<IDisposable, Exception> openFileHandles = new ConcurrentDictionary<IDisposable, Exception>(IdentityEqualityComparer<IDisposable>.Default);

        // NOTE: we cannot initialize the Map here due to the
        // order in which our constructor actually does this
        // member initialization vs when it calls super.  It seems
        // like super is called, then our members are initialized:
        private IDictionary<string, int> openFiles;

        // Only tracked if noDeleteOpenFile is true: if an attempt
        // is made to delete an open file, we enroll it here.
        private ISet<string> openFilesDeleted;

        private void Init()
        {
            UninterruptableMonitor.Enter(this);
            try
            {
                if (openFiles is null)
                {
                    openFiles = new Dictionary<string, int>(StringComparer.Ordinal);
                    openFilesDeleted = new JCG.HashSet<string>(StringComparer.Ordinal);
                }

                if (createdFiles is null)
                {
                    createdFiles = new JCG.HashSet<string>(StringComparer.Ordinal);
                }
                if (unSyncedFiles is null)
                {
                    unSyncedFiles = new JCG.HashSet<string>(StringComparer.Ordinal);
                }
            }
            finally
            {
                UninterruptableMonitor.Exit(this);
            }
        }

        public MockDirectoryWrapper(Random random, Directory @delegate)
            : base(@delegate)
        {
            // must make a private random since our methods are
            // called from different threads; else test failures may
            // not be reproducible from the original seed
            this.randomState = new J2N.Randomizer(random.NextInt64());
            this.throttledOutput = new ThrottledIndexOutput(ThrottledIndexOutput.MBitsToBytes(40 + randomState.Next(10)), 5 + randomState.Next(5), null);
            // force wrapping of lockfactory
            this.m_lockFactory = new MockLockFactoryWrapper(this, @delegate.LockFactory);
            Init();
        }

        public virtual int InputCloneCount => inputCloneCount;

        public virtual bool TrackDiskUsage
        {
            get => trackDiskUsage; // LUCENENET specific - added getter (to follow MSDN property guidelines)
            set => trackDiskUsage = value;
        }

        /// <summary>
        /// If set to true, we throw an <see cref="IOException"/> if the same
        /// file is opened by <see cref="CreateOutput(string, IOContext)"/>, ever.
        /// </summary>
        public virtual bool PreventDoubleWrite
        {
            get => preventDoubleWrite; // LUCENENET specific - added getter (to follow MSDN property guidelines)
            set => preventDoubleWrite = value;
        }

        /// <summary>
        /// If set to true (the default), when we throw random
        /// <see cref="IOException"/> on <see cref="OpenInput(string, IOContext)"/> or 
        /// <see cref="CreateOutput(string, IOContext)"/>, we may
        /// sometimes throw <see cref="FileNotFoundException"/>.
        /// </summary>
        public virtual bool AllowRandomFileNotFoundException
        {
            get => allowRandomFileNotFoundException; // LUCENENET specific - added getter (to follow MSDN property guidelines)
            set => allowRandomFileNotFoundException = value;
        }

        /// <summary>
        /// If set to true, you can open an inputstream on a file
        /// that is still open for writes.
        /// </summary>
        public virtual bool AllowReadingFilesStillOpenForWrite
        {
            get => allowRandomFileNotFoundException; // LUCENENET specific - added getter (to follow MSDN property guidelines)
            set => allowReadingFilesStillOpenForWrite = value;
        }

        // LUCENENET specific - de-nested Throttling enum

        public virtual Throttling Throttling
        {
            get => throttling; // LUCENENET specific - added getter (to follow MSDN property guidelines)
            set => throttling = value;
        }

        /// <summary>
        /// Returns true if <see cref="FilterDirectory.m_input"/> must sync its files.
        /// Currently, only <see cref="NRTCachingDirectory"/> requires sync'ing its files
        /// because otherwise they are cached in an internal <see cref="RAMDirectory"/>. If
        /// other directories require that too, they should be added to this method.
        /// </summary>
        private bool MustSync()
        {
            Directory @delegate = m_input;
            while (@delegate is FilterDirectory filterDirectory)
            {
                @delegate = filterDirectory.Delegate;
            }
            return @delegate is NRTCachingDirectory;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public override void Sync(ICollection<string> names)
        {
            UninterruptableMonitor.Enter(this);
            try
            {
                MaybeYield();
                MaybeThrowDeterministicException();
                if (crashed)
                {
                    throw new IOException("cannot sync after crash");
                }
                // don't wear out our hardware so much in tests.
                if (LuceneTestCase.Rarely(randomState) || MustSync())
                {
                    foreach (string name in names)
                    {
                        // randomly fail with IOE on any file
                        MaybeThrowIOException(name);
                        m_input.Sync(new[] { name });
                        unSyncedFiles.Remove(name);
                    }
                }
                else
                {
                    unSyncedFiles.ExceptWith(names);
                }
            }
            finally
            {
                UninterruptableMonitor.Exit(this);
            }
        }

        public long GetSizeInBytes()
        {
            UninterruptableMonitor.Enter(this);
            try
            {
                if (m_input is RAMDirectory ramDirectory)
                {
                    return ramDirectory.GetSizeInBytes();
                }
                else
                {
                    // hack
                    long size = 0;
                    foreach (string file in m_input.ListAll())
                    {
                        size += m_input.FileLength(file);
                    }
                    return size;
                }
            }
            finally
            {
                UninterruptableMonitor.Exit(this);
            }
        }

        /// <summary>
        /// Simulates a crash of OS or machine by overwriting
        /// unsynced files.
        /// </summary>
        public virtual void Crash()
        {
            UninterruptableMonitor.Enter(this);
            try
            {
                crashed = true;
                openFiles = new Dictionary<string, int>(StringComparer.Ordinal);
                openFilesForWrite = new JCG.HashSet<string>(StringComparer.Ordinal);
                openFilesDeleted = new JCG.HashSet<string>(StringComparer.Ordinal);
                using IEnumerator<string> it = unSyncedFiles.GetEnumerator();
                unSyncedFiles = new JCG.HashSet<string>(StringComparer.Ordinal);
                // first force-close all files, so we can corrupt on windows etc.
                // clone the file map, as these guys want to remove themselves on close.
                var m = new JCG.Dictionary<IDisposable, Exception>(openFileHandles, IdentityEqualityComparer<IDisposable>.Default);
                foreach (IDisposable f in m.Keys)
                {
                    try
                    {
                        f.Dispose();
                    }
                    catch (Exception ignored) when (ignored.IsException())
                    {
                        //Debug.WriteLine("Crash(): f.Dispose() FAILED for {0}:\n{1}", f.ToString(), ignored.ToString());
                    }
                }

                while (it.MoveNext())
                {
                    string name = it.Current;
                    int damage = randomState.Next(5);
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
                        var zeroes = new byte[256];
                        long upto = 0;
                        using IndexOutput @out = m_input.CreateOutput(name, LuceneTestCase.NewIOContext(randomState));
                        while (upto < length)
                        {
                            var limit = (int)Math.Min(length - upto, zeroes.Length);
                            @out.WriteBytes(zeroes, 0, limit);
                            upto += limit;
                        }
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
                            tempFileName = "" + randomState.Next();
                            if (!LuceneTestCase.SlowFileExists(m_input, tempFileName))
                            {
                                break;
                            }
                        }
                        using (IndexOutput tempOut = m_input.CreateOutput(tempFileName, LuceneTestCase.NewIOContext(randomState)))
                        using (IndexInput ii = m_input.OpenInput(name, LuceneTestCase.NewIOContext(randomState)))
                        {
                            tempOut.CopyBytes(ii, ii.Length / 2);
                        }

                        // Delete original and copy bytes back:
                        DeleteFile(name, true);

                        using (IndexOutput @out = m_input.CreateOutput(name, LuceneTestCase.NewIOContext(randomState)))
                        using (IndexInput ii = m_input.OpenInput(tempFileName, LuceneTestCase.NewIOContext(randomState)))
                        {
                            @out.CopyBytes(ii, ii.Length);
                        }
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
                        using IndexOutput @out = m_input.CreateOutput(name, LuceneTestCase.NewIOContext(randomState));
                        @out.Length = 0;
                    }
                    if (LuceneTestCase.Verbose)
                    {
                        Console.WriteLine("MockDirectoryWrapper: " + action + " unsynced file: " + name);
                    }
                }
            }
            finally
            {
                UninterruptableMonitor.Exit(this);
            }
        }

        public virtual void ClearCrash()
        {
            UninterruptableMonitor.Enter(this);
            try
            {
                crashed = false;
                openLocks.Clear();
            }
            finally
            {
                UninterruptableMonitor.Exit(this);
            }
        }

        public virtual long MaxSizeInBytes
        {
            get => maxSize;
            set => maxSize = value;
        }

        /// <summary>
        /// Returns the peek actual storage used (bytes) in this
        /// directory.
        /// </summary>
        public virtual long MaxUsedSizeInBytes => maxUsedSize;

        public virtual void ResetMaxUsedSizeInBytes()
        {
            this.maxUsedSize = GetRecomputedActualSizeInBytes();
        }

        /// <summary>
        /// Emulate Windows whereby deleting an open file is not
        /// allowed (raise <see cref="IOException"/>).
        /// </summary>
        public virtual bool NoDeleteOpenFile
        {
            get => noDeleteOpenFile;
            set => noDeleteOpenFile = value;
        }

        /// <summary>
        /// Trip a test assert if there is an attempt
        /// to delete an open file.
        /// </summary>
        public virtual bool AssertNoDeleteOpenFile
        {
            get => assertNoDeleteOpenFile;
            set => assertNoDeleteOpenFile = value;
        }

        /// <summary>
        /// If 0.0, no exceptions will be thrown.  Else this should
        /// be a double 0.0 - 1.0.  We will randomly throw an
        /// <see cref="IOException"/> on the first write to a <see cref="Stream"/> based
        /// on this probability.
        /// </summary>
        public virtual double RandomIOExceptionRate
        {
            get => randomIOExceptionRate;
            set => randomIOExceptionRate = value;
        }

        /// <summary>
        /// If 0.0, no exceptions will be thrown during <see cref="OpenInput(string, IOContext)"/>
        /// and <see cref="CreateOutput(string, IOContext)"/>.  Else this should
        /// be a double 0.0 - 1.0 and we will randomly throw an
        /// <see cref="IOException"/> in <see cref="OpenInput(string, IOContext)"/> and <see cref="CreateOutput(string, IOContext)"/> with
        /// this probability.
        /// </summary>
        public virtual double RandomIOExceptionRateOnOpen
        {
            get => randomIOExceptionRateOnOpen;
            set => randomIOExceptionRateOnOpen = value;
        }

        internal virtual void MaybeThrowIOException(string message)
        {
            if (randomState.NextDouble() < randomIOExceptionRate)
            {
                if (LuceneTestCase.Verbose)
                {
                    Console.WriteLine(Thread.CurrentThread.Name + ": MockDirectoryWrapper: now throw random exception" + (message is null ? "" : " (" + message + ")"));
                }
                throw new IOException("a random IOException" + (message is null ? "" : " (" + message + ")"));
            }
        }

        internal virtual void MaybeThrowIOExceptionOnOpen(string name)
        {
            if (randomState.NextDouble() < randomIOExceptionRateOnOpen)
            {
                if (LuceneTestCase.Verbose)
                {
                  Console.WriteLine(Thread.CurrentThread.Name + ": MockDirectoryWrapper: now throw random exception during open file=" + name);
                }
                if (allowRandomFileNotFoundException == false || randomState.NextBoolean())
                {
                    throw new IOException("a random IOException (" + name + ")");
                }
                else
                {
                    throw randomState.NextBoolean() ? (IOException)new FileNotFoundException("a random IOException (" + name + ")") : new DirectoryNotFoundException("a random IOException (" + name + ")");
                }
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public override void DeleteFile(string name)
        {
            UninterruptableMonitor.Enter(this);
            try
            {
                MaybeYield();
                DeleteFile(name, false);
            }
            finally
            {
                UninterruptableMonitor.Exit(this);
            }
        }

        // if there are any exceptions in OpenFileHandles
        // capture those as inner exceptions
        private Exception WithAdditionalErrorInformation(Exception t, string name, bool input)
        {
            UninterruptableMonitor.Enter(this);
            try
            {
                foreach (var ent in openFileHandles)
                {
                    if (input && ent.Key is MockIndexInputWrapper mockIndexInputWrapper && mockIndexInputWrapper.name.Equals(name, StringComparison.Ordinal))
                    {
                        t = CreateException(t, ent.Value);
                        break;
                    }
                    else if (!input && ent.Key is MockIndexOutputWrapper mockIndexOutputWrapper && mockIndexOutputWrapper.name.Equals(name, StringComparison.Ordinal))
                    {
                        t = CreateException(t, ent.Value);
                        break;
                    }
                }
                return t;
            }
            finally
            {
                UninterruptableMonitor.Exit(this);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Exception CreateException(Exception exception, Exception innerException) // LUCENENET: CA1822: Mark members as static
        {
            return (Exception)Activator.CreateInstance(exception.GetType(), exception.Message, innerException);
        }

        private void MaybeYield()
        {
            if (randomState.NextBoolean())
            {
                Thread.Yield();
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void DeleteFile(string name, bool forced)
        {
            UninterruptableMonitor.Enter(this);
            try
            {
                MaybeYield();

                MaybeThrowDeterministicException();

                if (crashed && !forced)
                {
                    throw new IOException("cannot delete after crash");
                }

                if (unSyncedFiles.Contains(name))
                {
                    unSyncedFiles.Remove(name);
                }
                if (!forced && (noDeleteOpenFile || assertNoDeleteOpenFile))
                {
                    if (openFiles.ContainsKey(name))
                    {
                        openFilesDeleted.Add(name);

                        if (!assertNoDeleteOpenFile)
                        {
                            throw WithAdditionalErrorInformation(new IOException("MockDirectoryWrapper: file \"" + name + "\" is still open: cannot delete"), name, true);
                        }
                        else
                        {
                            throw WithAdditionalErrorInformation(AssertionError.Create("MockDirectoryWrapper: file \"" + name + "\" is still open: cannot delete"), name, true);
                        }
                    }
                    else
                    {
                        openFilesDeleted.Remove(name);
                    }
                }
                m_input.DeleteFile(name);
            }
            finally
            {
                UninterruptableMonitor.Exit(this);
            }
        }

        public virtual ICollection<string> GetOpenDeletedFiles()
        {
            UninterruptableMonitor.Enter(this);
            try
            {
                return new JCG.HashSet<string>(openFilesDeleted, StringComparer.Ordinal);
            }
            finally
            {
                UninterruptableMonitor.Exit(this);
            }
        }

        private bool failOnCreateOutput = true;

        public virtual bool FailOnCreateOutput
        {
            get => failOnCreateOutput; // LUCENENET specific - added getter (to follow MSDN property guidelines)
            set => failOnCreateOutput = value;
        }

        public override IndexOutput CreateOutput(string name, IOContext context)
        {
            UninterruptableMonitor.Enter(this);
            try
            {
                MaybeThrowDeterministicException();
                MaybeThrowIOExceptionOnOpen(name);
                MaybeYield();
                if (failOnCreateOutput)
                {
                    MaybeThrowDeterministicException();
                }
                if (crashed)
                {
                    throw new IOException("cannot createOutput after crash");
                }
                Init();
                UninterruptableMonitor.Enter(this);
                try
                {
                    if (preventDoubleWrite && createdFiles.Contains(name) && !name.Equals("segments.gen", StringComparison.Ordinal))
                    {
                        throw new IOException("file \"" + name + "\" was already written to");
                    }
                }
                finally
                {
                    UninterruptableMonitor.Exit(this);
                }
                if ((noDeleteOpenFile || assertNoDeleteOpenFile) && openFiles.ContainsKey(name))
                {
                    if (!assertNoDeleteOpenFile)
                    {
                        throw new IOException("MockDirectoryWrapper: file \"" + name + "\" is still open: cannot overwrite");
                    }
                    else
                    {
                        throw AssertionError.Create("MockDirectoryWrapper: file \"" + name + "\" is still open: cannot overwrite");
                    }
                }

                if (crashed)
                {
                    throw new IOException("cannot createOutput after crash");
                }
                unSyncedFiles.Add(name);
                createdFiles.Add(name);

                if (m_input is RAMDirectory ramdir)
                {
                    RAMFile file = new RAMFile(ramdir);
                    ramdir.m_fileMap.TryGetValue(name, out RAMFile existing);

                    // Enforce write once:
                    if (existing != null && !name.Equals("segments.gen", StringComparison.Ordinal) && preventDoubleWrite)
                    {
                        throw new IOException("file " + name + " already exists");
                    }
                    else
                    {
                        if (existing != null)
                        {
                            ramdir.m_sizeInBytes.AddAndGet(-existing.GetSizeInBytes()); // LUCENENET: GetAndAdd in Lucene, but we are not using the value
                            existing.directory = null;
                        }
                        ramdir.m_fileMap[name] = file;
                    }
                }
                //System.out.println(Thread.currentThread().getName() + ": MDW: create " + name);
                IndexOutput delegateOutput = m_input.CreateOutput(name, LuceneTestCase.NewIOContext(randomState, context));
                if (randomState.Next(10) == 0)
                {
                    // once in a while wrap the IO in a Buffered IO with random buffer sizes
                    delegateOutput = new BufferedIndexOutputWrapper(1 + randomState.Next(BufferedIndexOutput.DEFAULT_BUFFER_SIZE), delegateOutput);
                }
                IndexOutput io = new MockIndexOutputWrapper(this, delegateOutput, name);
                AddFileHandle(io, name, Handle.Output);
                openFilesForWrite.Add(name);

                // throttling REALLY slows down tests, so don't do it very often for SOMETIMES.
                if (throttling == Throttling.ALWAYS || (throttling == Throttling.SOMETIMES && randomState.Next(50) == 0) && !(m_input is RateLimitedDirectoryWrapper))
                {
                    if (LuceneTestCase.Verbose)
                    {
                        Console.WriteLine("MockDirectoryWrapper: throttling indexOutput (" + name + ")");
                    }
                    return throttledOutput.NewFromDelegate(io);
                }
                else
                {
                    return io;
                }
            }
            finally
            {
                UninterruptableMonitor.Exit(this);
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

            UninterruptableMonitor.Enter(this);
            try
            {
                if (openFiles.TryGetValue(name, out int v))
                {
                    v++;
                    //Debug.WriteLine("Add {0} - {1} - {2}", c, name, v);
                    openFiles[name] = v;
                }
                else
                {
                    //Debug.WriteLine("Add {0} - {1} - {2}", c, name, 1);
                    openFiles[name] = 1;
                }

                openFileHandles[c] = RuntimeException.Create("unclosed Index" + handle.ToString() + ": " + name);
            }
            finally
            {
                UninterruptableMonitor.Exit(this);
            }
        }

        private bool failOnOpenInput = true;

        public virtual bool FailOnOpenInput
        {
            get => failOnOpenInput; // LUCENENET specific - added getter (to follow MSDN property guidelines)
            set => failOnOpenInput = value;
        }

        public override IndexInput OpenInput(string name, IOContext context)
        {
            UninterruptableMonitor.Enter(this);
            try
            {
                MaybeThrowDeterministicException();
                MaybeThrowIOExceptionOnOpen(name);
                MaybeYield();
                if (failOnOpenInput)
                {
                    MaybeThrowDeterministicException();
                }
                if (!LuceneTestCase.SlowFileExists(m_input, name))
                {
                    throw randomState.NextBoolean() ? (IOException)new FileNotFoundException(name + " in dir=" + m_input) : new DirectoryNotFoundException(name + " in dir=" + m_input);
                }

                // cannot open a file for input if it's still open for
                // output, except for segments.gen and segments_N
                if (!allowReadingFilesStillOpenForWrite && openFilesForWrite.Contains(name) && !name.StartsWith("segments", StringComparison.Ordinal))
                {
                    throw WithAdditionalErrorInformation(new IOException("MockDirectoryWrapper: file \"" + name + "\" is still open for writing"), name, false);
                }

                IndexInput delegateInput = m_input.OpenInput(name, LuceneTestCase.NewIOContext(randomState, context));

                IndexInput ii;
                int randomInt = randomState.Next(500);
                if (randomInt == 0)
                {
                    if (LuceneTestCase.Verbose)
                    {
                        Console.WriteLine("MockDirectoryWrapper: using SlowClosingMockIndexInputWrapper for file " + name);
                    }
                    ii = new SlowClosingMockIndexInputWrapper(this, name, delegateInput);
                }
                else if (randomInt == 1)
                {
                    if (LuceneTestCase.Verbose)
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
            finally
            {
                UninterruptableMonitor.Exit(this);
            }
        }

        /// <summary>
        /// Provided for testing purposes.  Use <see cref="GetSizeInBytes()"/> instead. </summary>
        public long GetRecomputedSizeInBytes()
        {
            UninterruptableMonitor.Enter(this);
            try
            {
                if (!(m_input is RAMDirectory))
                {
                    return GetSizeInBytes();
                }
                long size = 0;
                foreach (RAMFile file in ((RAMDirectory)m_input).m_fileMap.Values)
                {
                    size += file.GetSizeInBytes();
                }
                return size;
            }
            finally
            {
                UninterruptableMonitor.Exit(this);
            }
        }

        /// <summary>
        /// Like <see cref="GetRecomputedSizeInBytes()"/>, but, uses actual file
        /// lengths rather than buffer allocations (which are
        /// quantized up to nearest
        /// <see cref="RAMOutputStream.BUFFER_SIZE"/> (now 1024) bytes.
        /// </summary>

        public long GetRecomputedActualSizeInBytes()
        {
            UninterruptableMonitor.Enter(this);
            try
            {
                if (!(m_input is RAMDirectory))
                {
                    return GetSizeInBytes();
                }
                long size = 0;
                foreach (RAMFile file in ((RAMDirectory)m_input).m_fileMap.Values)
                {
                    size += file.Length;
                }
                return size;
            }
            finally
            {
                UninterruptableMonitor.Exit(this);
            }
        }

        // NOTE: this is off by default; see LUCENE-5574
        private bool assertNoUnreferencedFilesOnClose;

        public virtual bool AssertNoUnreferencedFilesOnDispose // LUCENENET specific: Renamed from AssertNoUnreferencedFilesOnClose
        {
            get => assertNoUnreferencedFilesOnClose; // LUCENENET specific - added getter (to follow MSDN property guidelines)
            set => assertNoUnreferencedFilesOnClose = value;
        }

        /// <summary>
        /// Set to false if you want to return the pure lockfactory
        /// and not wrap it with <see cref="MockLockFactoryWrapper"/>.
        /// <para/>
        /// Be careful if you turn this off: <see cref="MockDirectoryWrapper"/> might
        /// no longer be able to detect if you forget to close an <see cref="IndexWriter"/>,
        /// and spit out horribly scary confusing exceptions instead of
        /// simply telling you that.
        /// </summary>
        public virtual bool WrapLockFactory
        {
            get => wrapLockFactory; // LUCENENET specific - added getter (to follow MSDN property guidelines)
            set => wrapLockFactory = value;
        }

        protected override void Dispose(bool disposing)
        {
            if (!CompareAndSetIsOpen(expect: true, update: false)) return; // LUCENENET: allow dispose more than once as per https://learn.microsoft.com/en-us/dotnet/standard/design-guidelines/dispose-pattern

            UninterruptableMonitor.Enter(this);
            try
            {
                if (disposing)
                {
                    // files that we tried to delete, but couldn't because readers were open.
                    // all that matters is that we tried! (they will eventually go away)
                    ISet<string> pendingDeletions = new JCG.HashSet<string>(openFilesDeleted, StringComparer.Ordinal);
                    MaybeYield();
                    if (openFiles is null)
                    {
                        openFiles = new Dictionary<string, int>(StringComparer.Ordinal);
                        openFilesDeleted = new JCG.HashSet<string>(StringComparer.Ordinal);
                    }
                    if (openFiles.Count > 0)
                    {
                        // print the first one as its very verbose otherwise
                        Exception cause = openFileHandles.Values.FirstOrDefault();

                        // RuntimeException instead ofIOException because
                        // super() does not throw IOException currently:
                        throw RuntimeException.Create("MockDirectoryWrapper: cannot close: there are still open files: "
                            + Collections.ToString(openFiles), cause);
                    }
                    if (openLocks.Count > 0)
                    {
                        throw RuntimeException.Create("MockDirectoryWrapper: cannot close: there are still open locks: "
                            + Collections.ToString(openLocks));
                    }

                    IsOpen = false;
                    if (CheckIndexOnDispose)
                    {
                        randomIOExceptionRate = 0.0;
                        randomIOExceptionRateOnOpen = 0.0;
                        if (DirectoryReader.IndexExists(this))
                        {
                            if (LuceneTestCase.Verbose)
                            {
                                Console.WriteLine("\nNOTE: MockDirectoryWrapper: now crush");
                            }
                            Crash(); // corrupt any unsynced-files
                            if (LuceneTestCase.Verbose)
                            {
                                Console.WriteLine("\nNOTE: MockDirectoryWrapper: now run CheckIndex");
                            }
                            TestUtil.CheckIndex(this, CrossCheckTermVectorsOnDispose);

                            // TODO: factor this out / share w/ TestIW.assertNoUnreferencedFiles
                            if (assertNoUnreferencedFilesOnClose)
                            {
                                // now look for unreferenced files: discount ones that we tried to delete but could not
                                ISet<string> allFiles = new JCG.HashSet<string>(ListAll());
                                allFiles.ExceptWith(pendingDeletions);
                                string[] startFiles = allFiles.ToArray(/*new string[0]*/);
                                IndexWriterConfig iwc = new IndexWriterConfig(LuceneTestCase.TEST_VERSION_CURRENT, null);
                                iwc.SetIndexDeletionPolicy(NoDeletionPolicy.INSTANCE);
                                new IndexWriter(m_input, iwc).Rollback();
                                string[] endFiles = m_input.ListAll();

                                ISet<string> startSet = new JCG.SortedSet<string>(startFiles, StringComparer.Ordinal);
                                ISet<string> endSet = new JCG.SortedSet<string>(endFiles, StringComparer.Ordinal);

                                if (pendingDeletions.Contains("segments.gen") && endSet.Contains("segments.gen"))
                                {
                                    // this is possible if we hit an exception while writing segments.gen, we try to delete it
                                    // and it ends out in pendingDeletions (but IFD wont remove this).
                                    startSet.Add("segments.gen");
                                    if (LuceneTestCase.Verbose)
                                    {
                                        Console.WriteLine("MDW: Unreferenced check: Ignoring segments.gen that we could not delete.");
                                    }
                                }

                                // its possible we cannot delete the segments_N on windows if someone has it open and
                                // maybe other files too, depending on timing. normally someone on windows wouldnt have
                                // an issue (IFD would nuke this stuff eventually), but we pass NoDeletionPolicy...
                                foreach (string file in pendingDeletions)
                                {
                                    if (file.StartsWith("segments", StringComparison.Ordinal) && !file.Equals("segments.gen", StringComparison.Ordinal) && endSet.Contains(file))
                                    {
                                        startSet.Add(file);
                                        if (LuceneTestCase.Verbose)
                                        {
                                            Console.WriteLine("MDW: Unreferenced check: Ignoring segments file: " + file + " that we could not delete.");
                                        }
                                        SegmentInfos sis = new SegmentInfos();
                                        try
                                        {
                                            sis.Read(m_input, file);
                                        }
                                        catch (Exception ioe) when (ioe.IsIOException())
                                        {
                                            // OK: likely some of the .si files were deleted
                                        }

                                        try
                                        {
                                            ISet<string> ghosts = new JCG.HashSet<string>(sis.GetFiles(m_input, false));
                                            foreach (string s in ghosts)
                                            {
                                                if (endSet.Contains(s) && !startSet.Contains(s))
                                                {
                                                    if (Debugging.AssertsEnabled) Debugging.Assert(pendingDeletions.Contains(s));
                                                    if (LuceneTestCase.Verbose)
                                                    {
                                                        Console.WriteLine("MDW: Unreferenced check: Ignoring referenced file: " + s + " " + 
                                                            "from " + file + " that we could not delete.");
                                                    }
                                                    startSet.Add(s);
                                                }
                                            }
                                        }
                                        catch (Exception t) when (t.IsThrowable())
                                        {
                                            Console.Error.WriteLine("ERROR processing leftover segments file " + file + ":");
                                            Console.WriteLine(t.ToString());
                                        }
                                    }
                                }

                                startFiles = startSet.ToArray(/*new string[0]*/);
                                endFiles = endSet.ToArray(/*new string[0]*/);

                                if (!Arrays.Equals(startFiles, endFiles))
                                {
                                    IList<string> removed = new JCG.List<string>();
                                    foreach (string fileName in startFiles)
                                    {
                                        if (!endSet.Contains(fileName))
                                        {
                                            removed.Add(fileName);
                                        }
                                    }

                                    IList<string> added = new JCG.List<string>();
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
                                        extras = "\n\nThese files were removed: " + Collections.ToString(removed);
                                    }
                                    else
                                    {
                                        extras = "";
                                    }

                                    if (added.Count != 0)
                                    {
                                        extras += "\n\nThese files were added (waaaaaaaaaat!): " + Collections.ToString(added);
                                    }

                                    if (pendingDeletions.Count != 0)
                                    {
                                        extras += "\n\nThese files we had previously tried to delete, but couldn't: " + pendingDeletions;
                                    }

                                    if (Debugging.AssertsEnabled) Debugging.Assert(false, "unreferenced files: before delete:\n    {0}\n  after delete:\n    {1}{2}", startFiles, endFiles, extras);
                                }

                                DirectoryReader ir1 = DirectoryReader.Open(this);
                                int numDocs1 = ir1.NumDocs;
                                ir1.Dispose();
                                (new IndexWriter(this, new IndexWriterConfig(LuceneTestCase.TEST_VERSION_CURRENT, null))).Dispose();
                                DirectoryReader ir2 = DirectoryReader.Open(this);
                                int numDocs2 = ir2.NumDocs;
                                ir2.Dispose();
                                if (Debugging.AssertsEnabled) Debugging.Assert(numDocs1 == numDocs2,"numDocs changed after opening/closing IW: before={0} after={1}", numDocs1, numDocs2);
                            }
                        }
                    }
                    // LUCENENET specific: While the Microsoft docs say that Dispose() should not throw errors,
                    // we are being defensive because this is a test mock.
                    try
                    {
                        m_input.Dispose();
                    }
                    finally
                    {
                        throttledOutput.Dispose(); // LUCENENET specific
                    }
                }
            }
            finally
            {
                UninterruptableMonitor.Exit(this);
            }
        }

        internal virtual void RemoveOpenFile(IDisposable c, string name)
        {
            //Trace.TraceInformation("Rem {0} {1}", c, name);

            UninterruptableMonitor.Enter(this);
            try
            {
                if (openFiles.TryGetValue(name, out int v))
                {
                    if (v == 1)
                    {
                        //Debug.WriteLine("RemoveOpenFile OpenFiles.Remove {0} - {1}", c, name);
                        openFiles.Remove(name);
                    }
                    else
                    {
                        v--;
                        openFiles[name] = v;
                        //Debug.WriteLine("RemoveOpenFile OpenFiles DECREMENT {0} - {1} - {2}", c, name, v);
                    }
                }

                openFileHandles.TryRemove(c, out Exception _);
            }
            finally
            {
                UninterruptableMonitor.Exit(this);
            }
        }

        public virtual void RemoveIndexOutput(IndexOutput @out, string name)
        {
            UninterruptableMonitor.Enter(this);
            try
            {
                openFilesForWrite.Remove(name);
                RemoveOpenFile(@out, name);
            }
            finally
            {
                UninterruptableMonitor.Exit(this);
            }
        }

        public virtual void RemoveIndexInput(IndexInput @in, string name)
        {
            UninterruptableMonitor.Enter(this);
            try
            {
                RemoveOpenFile(@in, name);
            }
            finally
            {
                UninterruptableMonitor.Exit(this);
            }
        }

        // LUCENENET specific - de-nested Failure

        internal JCG.List<Failure> failures;

        /// <summary>
        /// Add a <see cref="Failure"/> object to the list of objects to be evaluated
        /// at every potential failure point.
        /// </summary>
        public virtual void FailOn(Failure fail)
        {
            UninterruptableMonitor.Enter(this);
            try
            {
                if (failures is null)
                {
                    failures = new JCG.List<Failure>();
                }
                failures.Add(fail);
            }
            finally
            {
                UninterruptableMonitor.Exit(this);
            }
        }

        /// <summary>
        /// Iterate through the failures list, giving each object a
        /// chance to throw an <see cref="IOException"/>.
        /// </summary>
        internal virtual void MaybeThrowDeterministicException()
        {
            UninterruptableMonitor.Enter(this);
            try
            {
                if (failures != null)
                {
                    for (int i = 0; i < failures.Count; i++)
                    {
                        failures[i].Eval(this);
                    }
                }
            }
            finally
            {
                UninterruptableMonitor.Exit(this);
            }
        }

        public override string[] ListAll()
        {
            UninterruptableMonitor.Enter(this);
            try
            {
                MaybeYield();
                return m_input.ListAll();
            }
            finally
            {
                UninterruptableMonitor.Exit(this);
            }
        }

        [Obsolete("this method will be removed in 5.0")]
        public override bool FileExists(string name)
        {
            UninterruptableMonitor.Enter(this);
            try
            {
                MaybeYield();
                return m_input.FileExists(name);
            }
            finally
            {
                UninterruptableMonitor.Exit(this);
            }
        }

        public override long FileLength(string name)
        {
            UninterruptableMonitor.Enter(this);
            try
            {
                MaybeYield();
                return m_input.FileLength(name);
            }
            finally
            {
                UninterruptableMonitor.Exit(this);
            }
        }

        public override Lock MakeLock(string name)
        {
            UninterruptableMonitor.Enter(this);
            try
            {
                MaybeYield();
                return LockFactory.MakeLock(name);
            }
            finally
            {
                UninterruptableMonitor.Exit(this);
            }
        }

        public override void ClearLock(string name)
        {
            UninterruptableMonitor.Enter(this);
            try
            {
                MaybeYield();
                LockFactory.ClearLock(name);
            }
            finally
            {
                UninterruptableMonitor.Exit(this);
            }
        }

        public override void SetLockFactory(LockFactory lockFactory)
        {
            UninterruptableMonitor.Enter(this);
            try
            {
                MaybeYield();
                // sneaky: we must pass the original this way to the dir, because
                // some impls (e.g. FSDir) do instanceof here.
                m_input.SetLockFactory(lockFactory);
                // now set our wrapped factory here
                this.m_lockFactory = new MockLockFactoryWrapper(this, lockFactory);
            }
            finally
            {
                UninterruptableMonitor.Exit(this);
            }
        }

        public override LockFactory LockFactory
        {
            get
            {
                UninterruptableMonitor.Enter(this);
                try
                {
                    MaybeYield();
                    if (wrapLockFactory)
                    {
                        return m_lockFactory;
                    }
                    else
                    {
                        return m_input.LockFactory;
                    }
                }
                finally
                {
                    UninterruptableMonitor.Exit(this);
                }
            }
        }

        public override string GetLockID()
        {
            UninterruptableMonitor.Enter(this);
            try
            {
                MaybeYield();
                return m_input.GetLockID();
            }
            finally
            {
                UninterruptableMonitor.Exit(this);
            }
        }

        public override void Copy(Directory to, string src, string dest, IOContext context)
        {
            UninterruptableMonitor.Enter(this);
            try
            {
                MaybeYield();
                // randomize the IOContext here?
                m_input.Copy(to, src, dest, context);
            }
            finally
            {
                UninterruptableMonitor.Exit(this);
            }
        }

        public override IndexInputSlicer CreateSlicer(string name, IOContext context)
        {
            MaybeYield();
            if (!LuceneTestCase.SlowFileExists(m_input, name))
            {
                throw randomState.NextBoolean() ? (IOException)new FileNotFoundException(name) : new DirectoryNotFoundException(name);
            }
            // cannot open a file for input if it's still open for
            // output, except for segments.gen and segments_N

            if (openFilesForWrite.Contains(name) && !name.StartsWith("segments", StringComparison.Ordinal))
            {
                throw WithAdditionalErrorInformation(new IOException("MockDirectoryWrapper: file \"" + name + "\" is still open for writing"), name, false);
            }

            IndexInputSlicer delegateHandle = m_input.CreateSlicer(name, context);
            IndexInputSlicer handle = new IndexInputSlicerAnonymousClass(this, name, delegateHandle);
            AddFileHandle(handle, name, Handle.Slice);
            return handle;
        }

        private sealed class IndexInputSlicerAnonymousClass : IndexInputSlicer
        {
            private readonly MockDirectoryWrapper outerInstance;

            private readonly string name;
            private readonly IndexInputSlicer delegateHandle;

            public IndexInputSlicerAnonymousClass(MockDirectoryWrapper outerInstance, string name, IndexInputSlicer delegateHandle)
            {
                this.outerInstance = outerInstance;
                this.name = name;
                this.delegateHandle = delegateHandle;
            }

            private int disposed = 0; // LUCENENET specific - allow double-dispose

            protected override void Dispose(bool disposing)
            {
                if (0 != Interlocked.CompareExchange(ref this.disposed, 1, 0)) return; // LUCENENET specific - allow double-dispose

                if (disposing)
                {
                    delegateHandle.Dispose();
                    outerInstance.RemoveOpenFile(this, name);
                }
            }

            public override IndexInput OpenSlice(string sliceDescription, long offset, long length)
            {
                outerInstance.MaybeYield();
                IndexInput ii = new MockIndexInputWrapper(outerInstance, name, delegateHandle.OpenSlice(sliceDescription, offset, length));
                outerInstance.AddFileHandle(ii, name, Handle.Input);
                return ii;
            }

            [Obsolete("Only for reading CFS files from 3.x indexes.")]
            public override IndexInput OpenFullSlice()
            {
                outerInstance.MaybeYield();
                IndexInput ii = new MockIndexInputWrapper(outerInstance, name, delegateHandle.OpenFullSlice());
                outerInstance.AddFileHandle(ii, name, Handle.Input);
                return ii;
            }
        }

        internal sealed class BufferedIndexOutputWrapper : BufferedIndexOutput
        {
            private readonly IndexOutput io;

            public BufferedIndexOutputWrapper(int bufferSize, IndexOutput io)
                : base(bufferSize)
            {
                this.io = io;
            }

            public override long Length => io.Length;

            protected internal override void FlushBuffer(byte[] b, int offset, int len)
            {
                io.WriteBytes(b, offset, len);
            }

            [Obsolete("(4.1) this method will be removed in Lucene 5.0")]
            public override void Seek(long pos)
            {
                Flush();
                io.Seek(pos);
            }

            public override void Flush()
            {
                try
                {
                    base.Flush();
                }
                finally
                {
                    io.Flush();
                }
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    try
                    {
                        base.Dispose(disposing);
                    }
                    finally
                    {
                        io.Dispose();
                    }
                }
            }
        }

        // LUCENENET specific - de-nested FakeIOException
    }

    /// <summary>
    /// Objects that represent fail-able conditions. Objects of a derived
    /// class are created and registered with the mock directory. After
    /// register, each object will be invoked once for each first write
    /// of a file, giving the object a chance to throw an <see cref="IOException"/>.
    /// </summary>
    public class Failure
    {
        /// <summary>
        /// Eval is called on the first write of every new file.
        /// </summary>
        public virtual void Eval(MockDirectoryWrapper dir)
        {
        }

        /// <summary>
        /// Reset should set the state of the failure to its default
        /// (freshly constructed) state. Reset is convenient for tests
        /// that want to create one failure object and then reuse it in
        /// multiple cases. This, combined with the fact that <see cref="Failure"/>
        /// subclasses are often anonymous classes makes reset difficult to
        /// do otherwise.
        /// <para/>
        /// A typical example of use is
        /// <code>
        /// Failure failure = new Failure() { ... };
        /// ...
        /// mock.FailOn(failure.Reset())
        /// </code>
        /// </summary>
        public virtual Failure Reset()
        {
            return this;
        }

        protected internal bool m_doFail;

        public virtual void SetDoFail()
        {
            m_doFail = true;
        }

        public virtual void ClearDoFail()
        {
            m_doFail = false;
        }
    }

    /// <summary>
    /// Use this when throwing fake <see cref="IOException"/>,
    /// e.g. from <see cref="Failure"/>.
    /// </summary>
    // LUCENENET: It is no longer good practice to use binary serialization. 
    // See: https://github.com/dotnet/corefx/issues/23584#issuecomment-325724568
#if FEATURE_SERIALIZABLE_EXCEPTIONS
    [Serializable]
#endif
    public class FakeIOException : IOException
    {
        public FakeIOException() { } // LUCENENET specific - added public constructor for serialization

#if FEATURE_SERIALIZABLE_EXCEPTIONS
        /// <summary>
        /// Initializes a new instance of this class with serialized data.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo"/> that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The <see cref="StreamingContext"/> that contains contextual information about the source or destination.</param>
        protected FakeIOException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
#endif
    }
}