using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

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

    using AlreadyClosedException = Lucene.Net.Store.AlreadyClosedException;
    using Directory = Lucene.Net.Store.Directory;
    using IOContext = Lucene.Net.Store.IOContext;
    using IOUtils = Lucene.Net.Util.IOUtils;

    internal sealed class StandardDirectoryReader : DirectoryReader
    {
        private readonly IndexWriter Writer;
        private readonly SegmentInfos SegmentInfos;
        private readonly int TermInfosIndexDivisor;
        private readonly bool ApplyAllDeletes;

        /// <summary>
        /// called only from static open() methods </summary>
        internal StandardDirectoryReader(Directory directory, AtomicReader[] readers, IndexWriter writer, SegmentInfos sis, int termInfosIndexDivisor, bool applyAllDeletes)
            : base(directory, readers)
        {
            this.Writer = writer;
            this.SegmentInfos = sis;
            this.TermInfosIndexDivisor = termInfosIndexDivisor;
            this.ApplyAllDeletes = applyAllDeletes;
        }

        /// <summary>
        /// called from DirectoryReader.open(...) methods </summary>
        internal static DirectoryReader Open(Directory directory, IndexCommit commit, int termInfosIndexDivisor)
        {
            return (DirectoryReader)new FindSegmentsFileAnonymousInnerClassHelper(directory, termInfosIndexDivisor).Run(commit);
        }

        private class FindSegmentsFileAnonymousInnerClassHelper : SegmentInfos.FindSegmentsFile
        {
            private readonly int termInfosIndexDivisor;

            public FindSegmentsFileAnonymousInnerClassHelper(Directory directory, int termInfosIndexDivisor)
                : base(directory)
            {
                this.termInfosIndexDivisor = termInfosIndexDivisor;
            }

            protected internal override object DoBody(string segmentFileName)
            {
                var sis = new SegmentInfos();
                sis.Read(directory, segmentFileName);
                var readers = new SegmentReader[sis.Size()];
                for (int i = sis.Size() - 1; i >= 0; i--)
                {
                    System.IO.IOException prior = null;
                    bool success = false;
                    try
                    {
                        readers[i] = new SegmentReader(sis.Info(i), termInfosIndexDivisor, IOContext.READ);
                        success = true;
                    }
                    catch (System.IO.IOException ex)
                    {
                        prior = ex;
                    }
                    finally
                    {
                        if (!success)
                        {
                            IOUtils.CloseWhileHandlingException(prior, readers);
                        }
                    }
                }
                return new StandardDirectoryReader(directory, readers, null, sis, termInfosIndexDivisor, false);
            }
        }

        /// <summary>
        /// Used by near real-time search </summary>
        internal static DirectoryReader Open(IndexWriter writer, SegmentInfos infos, bool applyAllDeletes)
        {
            // IndexWriter synchronizes externally before calling
            // us, which ensures infos will not change; so there's
            // no need to process segments in reverse order
            int numSegments = infos.Size();

            IList<SegmentReader> readers = new List<SegmentReader>();
            Directory dir = writer.Directory;

            SegmentInfos segmentInfos = (SegmentInfos)infos.Clone();
            int infosUpto = 0;
            bool success = false;
            try
            {
                for (int i = 0; i < numSegments; i++)
                {
                    // NOTE: important that we use infos not
                    // segmentInfos here, so that we are passing the
                    // actual instance of SegmentInfoPerCommit in
                    // IndexWriter's segmentInfos:
                    SegmentCommitInfo info = infos.Info(i);
                    Debug.Assert(info.Info.Dir == dir);
                    ReadersAndUpdates rld = writer.readerPool.Get(info, true);
                    try
                    {
                        SegmentReader reader = rld.GetReadOnlyClone(IOContext.READ);
                        if (reader.NumDocs > 0 || writer.KeepFullyDeletedSegments)
                        {
                            // Steal the ref:
                            readers.Add(reader);
                            infosUpto++;
                        }
                        else
                        {
                            reader.DecRef();
                            segmentInfos.Remove(infosUpto);
                        }
                    }
                    finally
                    {
                        writer.readerPool.Release(rld);
                    }
                }

                writer.IncRefDeleter(segmentInfos);

                StandardDirectoryReader result = new StandardDirectoryReader(dir, readers.ToArray(), writer, segmentInfos, writer.Config.ReaderTermsIndexDivisor, applyAllDeletes);
                success = true;
                return result;
            }
            finally
            {
                if (!success)
                {
                    foreach (SegmentReader r in readers)
                    {
                        try
                        {
                            r.DecRef();
                        }
                        catch (Exception th)
                        {
                            // ignore any exception that is thrown here to not mask any original
                            // exception.
                        }
                    }
                }
            }
        }

        /// <summary>
        /// this constructor is only used for <seealso cref="#doOpenIfChanged(SegmentInfos)"/> </summary>
        private static DirectoryReader Open(Directory directory, SegmentInfos infos, IList<AtomicReader> oldReaders, int termInfosIndexDivisor)
        {
            // we put the old SegmentReaders in a map, that allows us
            // to lookup a reader using its segment name
            IDictionary<string, int?> segmentReaders = new Dictionary<string, int?>();

            if (oldReaders != null)
            {
                // create a Map SegmentName->SegmentReader
                for (int i = 0, c = oldReaders.Count; i < c; i++)
                {
                    SegmentReader sr = (SegmentReader)oldReaders[i];
                    segmentReaders[sr.SegmentName] = Convert.ToInt32(i);
                }
            }

            SegmentReader[] newReaders = new SegmentReader[infos.Size()];

            // remember which readers are shared between the old and the re-opened
            // DirectoryReader - we have to incRef those readers
            bool[] readerShared = new bool[infos.Size()];

            for (int i = infos.Size() - 1; i >= 0; i--)
            {
                // find SegmentReader for this segment
                int? oldReaderIndex;
                segmentReaders.TryGetValue(infos.Info(i).Info.Name, out oldReaderIndex);
                if (oldReaderIndex == null)
                {
                    // this is a new segment, no old SegmentReader can be reused
                    newReaders[i] = null;
                }
                else
                {
                    // there is an old reader for this segment - we'll try to reopen it
                    newReaders[i] = (SegmentReader)oldReaders[(int)oldReaderIndex];
                }

                bool success = false;
                Exception prior = null;
                try
                {
                    SegmentReader newReader;
                    if (newReaders[i] == null || infos.Info(i).Info.UseCompoundFile != newReaders[i].SegmentInfo.Info.UseCompoundFile)
                    {
                        // this is a new reader; in case we hit an exception we can close it safely
                        newReader = new SegmentReader(infos.Info(i), termInfosIndexDivisor, IOContext.READ);
                        readerShared[i] = false;
                        newReaders[i] = newReader;
                    }
                    else
                    {
                        if (newReaders[i].SegmentInfo.DelGen == infos.Info(i).DelGen && newReaders[i].SegmentInfo.FieldInfosGen == infos.Info(i).FieldInfosGen)
                        {
                            // No change; this reader will be shared between
                            // the old and the new one, so we must incRef
                            // it:
                            readerShared[i] = true;
                            newReaders[i].IncRef();
                        }
                        else
                        {
                            // there are changes to the reader, either liveDocs or DV updates
                            readerShared[i] = false;
                            // Steal the ref returned by SegmentReader ctor:
                            Debug.Assert(infos.Info(i).Info.Dir == newReaders[i].SegmentInfo.Info.Dir);
                            Debug.Assert(infos.Info(i).HasDeletions() || infos.Info(i).HasFieldUpdates());
                            if (newReaders[i].SegmentInfo.DelGen == infos.Info(i).DelGen)
                            {
                                // only DV updates
                                newReaders[i] = new SegmentReader(infos.Info(i), newReaders[i], newReaders[i].LiveDocs, newReaders[i].NumDocs);
                            }
                            else
                            {
                                // both DV and liveDocs have changed
                                newReaders[i] = new SegmentReader(infos.Info(i), newReaders[i]);
                            }
                        }
                    }
                    success = true;
                }
                catch (Exception ex)
                {
                    prior = ex;
                }
                finally
                {
                    if (!success)
                    {
                        for (i++; i < infos.Size(); i++)
                        {
                            if (newReaders[i] != null)
                            {
                                try
                                {
                                    if (!readerShared[i])
                                    {
                                        // this is a new subReader that is not used by the old one,
                                        // we can close it
                                        newReaders[i].Dispose();
                                    }
                                    else
                                    {
                                        // this subReader is also used by the old reader, so instead
                                        // closing we must decRef it
                                        newReaders[i].DecRef();
                                    }
                                }
                                catch (Exception t)
                                {
                                    if (prior == null)
                                    {
                                        prior = t;
                                    }
                                }
                            }
                        }
                    }
                    // throw the first exception
                    IOUtils.ReThrow(prior);
                }
            }
            return new StandardDirectoryReader(directory, newReaders, null, infos, termInfosIndexDivisor, false);
        }

        public override string ToString()
        {
            StringBuilder buffer = new StringBuilder();
            buffer.Append(this.GetType().Name);
            buffer.Append('(');
            string segmentsFile = SegmentInfos.SegmentsFileName;
            if (segmentsFile != null)
            {
                buffer.Append(segmentsFile).Append(":").Append(SegmentInfos.Version);
            }
            if (Writer != null)
            {
                buffer.Append(":nrt");
            }
            foreach (AtomicReader r in GetSequentialSubReaders())
            {
                buffer.Append(' ');
                buffer.Append(r);
            }
            buffer.Append(')');
            return buffer.ToString();
        }

        protected internal override DirectoryReader DoOpenIfChanged()
        {
            return DoOpenIfChanged((IndexCommit)null);
        }

        protected internal override DirectoryReader DoOpenIfChanged(IndexCommit commit)
        {
            EnsureOpen();

            // If we were obtained by writer.getReader(), re-ask the
            // writer to get a new reader.
            if (Writer != null)
            {
                return DoOpenFromWriter(commit);
            }
            else
            {
                return DoOpenNoWriter(commit);
            }
        }

        protected internal override DirectoryReader DoOpenIfChanged(IndexWriter writer, bool applyAllDeletes)
        {
            EnsureOpen();
            if (writer == this.Writer && applyAllDeletes == this.ApplyAllDeletes)
            {
                return DoOpenFromWriter(null);
            }
            else
            {
                return writer.GetReader(applyAllDeletes);
            }
        }

        private DirectoryReader DoOpenFromWriter(IndexCommit commit)
        {
            if (commit != null)
            {
                return DoOpenFromCommit(commit);
            }

            if (Writer.NrtIsCurrent(SegmentInfos))
            {
                return null;
            }

            DirectoryReader reader = Writer.GetReader(ApplyAllDeletes);

            // If in fact no changes took place, return null:
            if (reader.Version == SegmentInfos.Version)
            {
                reader.DecRef();
                return null;
            }

            return reader;
        }

        private DirectoryReader DoOpenNoWriter(IndexCommit commit)
        {
            if (commit == null)
            {
                if (IsCurrent)
                {
                    return null;
                }
            }
            else
            {
                if (directory != commit.Directory)
                {
                    throw new System.IO.IOException("the specified commit does not match the specified Directory");
                }
                if (SegmentInfos != null && commit.SegmentsFileName.Equals(SegmentInfos.SegmentsFileName))
                {
                    return null;
                }
            }

            return DoOpenFromCommit(commit);
        }

        private DirectoryReader DoOpenFromCommit(IndexCommit commit)
        {
            return (DirectoryReader)new FindSegmentsFileAnonymousInnerClassHelper2(this, directory).Run(commit);
        }

        private class FindSegmentsFileAnonymousInnerClassHelper2 : SegmentInfos.FindSegmentsFile
        {
            private readonly StandardDirectoryReader OuterInstance;

            public FindSegmentsFileAnonymousInnerClassHelper2(StandardDirectoryReader outerInstance, Directory directory)
                : base(directory)
            {
                this.OuterInstance = outerInstance;
            }

            protected internal override object DoBody(string segmentFileName)
            {
                SegmentInfos infos = new SegmentInfos();
                infos.Read(OuterInstance.directory, segmentFileName);
                return OuterInstance.DoOpenIfChanged(infos);
            }
        }

        internal DirectoryReader DoOpenIfChanged(SegmentInfos infos)
        {
            return StandardDirectoryReader.Open(directory, infos, GetSequentialSubReaders().OfType<AtomicReader>().ToList(), TermInfosIndexDivisor);
        }

        public override long Version
        {
            get
            {
                EnsureOpen();
                return SegmentInfos.Version;
            }
        }

        public override bool IsCurrent
        {
            get
            {
                EnsureOpen();
                if (Writer == null || Writer.IsClosed)
                {
                    // Fully read the segments file: this ensures that it's
                    // completely written so that if
                    // IndexWriter.prepareCommit has been called (but not
                    // yet commit), then the reader will still see itself as
                    // current:
                    SegmentInfos sis = new SegmentInfos();
                    sis.Read(directory);

                    // we loaded SegmentInfos from the directory
                    return sis.Version == SegmentInfos.Version;
                }
                else
                {
                    return Writer.NrtIsCurrent(SegmentInfos);
                }
            }
        }

        protected internal override void DoClose()
        {
            Exception firstExc = null;
            foreach (AtomicReader r in GetSequentialSubReaders())
            {
                // try to close each reader, even if an exception is thrown
                try
                {
                    r.DecRef();
                }
                catch (Exception t)
                {
                    if (firstExc == null)
                    {
                        firstExc = t;
                    }
                }
            }

            if (Writer != null)
            {
                try
                {
                    Writer.DecRefDeleter(SegmentInfos);
                }
                catch (AlreadyClosedException ex)
                {
                    // this is OK, it just means our original writer was
                    // closed before we were, and this may leave some
                    // un-referenced files in the index, which is
                    // harmless.  The next time IW is opened on the
                    // index, it will delete them.
                }
            }

            // throw the first exception
            IOUtils.ReThrow(firstExc);
        }

        public override IndexCommit IndexCommit
        {
            get
            {
                EnsureOpen();
                return new ReaderCommit(SegmentInfos, directory);
            }
        }

        internal sealed class ReaderCommit : IndexCommit
        {
            internal string SegmentsFileName_Renamed;
            internal ICollection<string> Files;
            internal Directory Dir;
            internal long Generation_Renamed;
            internal readonly IDictionary<string, string> UserData_Renamed;
            internal readonly int SegmentCount_Renamed;

            internal ReaderCommit(SegmentInfos infos, Directory dir)
            {
                SegmentsFileName_Renamed = infos.SegmentsFileName;
                this.Dir = dir;
                UserData_Renamed = infos.UserData;
                Files = infos.Files(dir, true);
                Generation_Renamed = infos.Generation;
                SegmentCount_Renamed = infos.Size();
            }

            public override string ToString()
            {
                return "DirectoryReader.ReaderCommit(" + SegmentsFileName_Renamed + ")";
            }

            public override int SegmentCount
            {
                get
                {
                    return SegmentCount_Renamed;
                }
            }

            public override string SegmentsFileName
            {
                get
                {
                    return SegmentsFileName_Renamed;
                }
            }

            public override ICollection<string> FileNames
            {
                get
                {
                    return Files;
                }
            }

            public override Directory Directory
            {
                get
                {
                    return Dir;
                }
            }

            public override long Generation
            {
                get
                {
                    return Generation_Renamed;
                }
            }

            public override bool IsDeleted
            {
                get
                {
                    return false;
                }
            }

            public override IDictionary<string, string> UserData
            {
                get
                {
                    return UserData_Renamed;
                }
            }

            public override void Delete()
            {
                throw new NotSupportedException("this IndexCommit does not support deletions");
            }
        }
    }
}