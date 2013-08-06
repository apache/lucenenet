using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Support;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Codecs.Lucene3x
{
    [Obsolete]
    internal class Lucene3xNormsProducer : DocValuesProducer
    {
        /** norms header placeholder */
        internal static readonly byte[] NORMS_HEADER = new byte[] { (byte)'N', (byte)'R', (byte)'M', unchecked((byte)-1) };

        /** Extension of norms file */
        internal const string NORMS_EXTENSION = "nrm";

        /** Extension of separate norms file */
        internal const string SEPARATE_NORMS_EXTENSION = "s";

        internal readonly IDictionary<String, NormsDocValues> norms = new HashMap<String, NormsDocValues>();
        // any .nrm or .sNN files we have open at any time.
        // TODO: just a list, and double-close() separate norms files?
        internal readonly ISet<IndexInput> openFiles = new IdentityHashSet<IndexInput>();
        // points to a singleNormFile
        internal IndexInput singleNormStream;
        internal readonly int maxdoc;

        // note: just like segmentreader in 3.x, we open up all the files here (including separate norms) up front.
        // but we just don't do any seeks or reading yet.
        public Lucene3xNormsProducer(Directory dir, SegmentInfo info, FieldInfos fields, IOContext context)
        {
            Directory separateNormsDir = info.dir; // separate norms are never inside CFS
            maxdoc = info.DocCount;
            String segmentName = info.name;
            bool success = false;
            try
            {
                long nextNormSeek = NORMS_HEADER.Length; //skip header (header unused for now)
                foreach (FieldInfo fi in fields)
                {
                    if (fi.HasNorms)
                    {
                        String fileName = GetNormFilename(info, fi.number);
                        Directory d = HasSeparateNorms(info, fi.number) ? separateNormsDir : dir;

                        // singleNormFile means multiple norms share this file
                        bool singleNormFile = IndexFileNames.MatchesExtension(fileName, NORMS_EXTENSION);
                        IndexInput normInput = null;
                        long normSeek;

                        if (singleNormFile)
                        {
                            normSeek = nextNormSeek;
                            if (singleNormStream == null)
                            {
                                singleNormStream = d.OpenInput(fileName, context);
                                openFiles.Add(singleNormStream);
                            }
                            // All norms in the .nrm file can share a single IndexInput since
                            // they are only used in a synchronized context.
                            // If this were to change in the future, a clone could be done here.
                            normInput = singleNormStream;
                        }
                        else
                        {
                            normInput = d.OpenInput(fileName, context);
                            openFiles.Add(normInput);
                            // if the segment was created in 3.2 or after, we wrote the header for sure,
                            // and don't need to do the sketchy file size check. otherwise, we check 
                            // if the size is exactly equal to maxDoc to detect a headerless file.
                            // NOTE: remove this check in Lucene 5.0!
                            String version = info.Version;
                            bool isUnversioned =
                                (version == null || StringHelper.VersionComparator.Compare(version, "3.2") < 0)
                                && normInput.Length == maxdoc;
                            if (isUnversioned)
                            {
                                normSeek = 0;
                            }
                            else
                            {
                                normSeek = NORMS_HEADER.Length;
                            }
                        }
                        NormsDocValues norm = new NormsDocValues(this, normInput, normSeek);
                        norms[fi.name] = norm;
                        nextNormSeek += maxdoc; // increment also if some norms are separate
                    }
                }
                // TODO: change to a real check? see LUCENE-3619
                //assert singleNormStream == null || nextNormSeek == singleNormStream.length() : singleNormStream != null ? "len: " + singleNormStream.length() + " expected: " + nextNormSeek : "null";
                success = true;
            }
            finally
            {
                if (!success)
                {
                    IOUtils.CloseWhileHandlingException((IDisposable)openFiles);
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                try
                {
                    IOUtils.Close(openFiles);
                }
                finally
                {
                    norms.Clear();
                    openFiles.Clear();
                }
            }
        }

        private static String GetNormFilename(SegmentInfo info, int number)
        {
            if (HasSeparateNorms(info, number))
            {
                long gen = long.Parse(info.GetAttribute(Lucene3xSegmentInfoFormat.NORMGEN_PREFIX + number));
                return IndexFileNames.FileNameFromGeneration(info.name, SEPARATE_NORMS_EXTENSION + number, gen);
            }
            else
            {
                // single file for all norms
                return IndexFileNames.SegmentFileName(info.name, "", NORMS_EXTENSION);
            }
        }

        private static bool HasSeparateNorms(SegmentInfo info, int number)
        {
            String v = info.GetAttribute(Lucene3xSegmentInfoFormat.NORMGEN_PREFIX + number);
            if (v == null)
            {
                return false;
            }
            else
            {
                //assert Long.parseLong(v) != SegmentInfo.NO;
                return true;
            }
        }

        // holds a file+offset pointing to a norms, and lazy-loads it
        // to a singleton NumericDocValues instance
        internal class NormsDocValues
        {
            private readonly Lucene3xNormsProducer parent;
            private readonly IndexInput file;
            private readonly long offset;
            private NumericDocValues instance;

            public NormsDocValues(Lucene3xNormsProducer parent, IndexInput normInput, long normSeek)
            {
                this.parent = parent;
                this.file = normInput;
                this.offset = normSeek;
            }

            internal NumericDocValues GetInstance()
            {
                lock (this)
                {
                    if (instance == null)
                    {
                        byte[] bytes = new byte[parent.maxdoc];
                        // some norms share fds
                        lock (file)
                        {
                            file.Seek(offset);
                            file.ReadBytes(bytes, 0, bytes.Length, false);
                        }
                        // we are done with this file
                        if (file != parent.singleNormStream)
                        {
                            parent.openFiles.Remove(file);
                            file.Dispose();
                        }
                        instance = new AnonymousGetInstanceNumericDocValues(bytes);
                    }
                    return instance;
                }
            }

            private sealed class AnonymousGetInstanceNumericDocValues : NumericDocValues
            {
                private readonly byte[] bytes;

                public AnonymousGetInstanceNumericDocValues(byte[] bytes)
                {
                    this.bytes = bytes;
                }

                public override long Get(int docID)
                {
                    return bytes[docID];
                }
            }
        }

        public override NumericDocValues GetNumeric(FieldInfo field)
        {
            NormsDocValues dv = norms[field.name];
            //assert dv != null;
            return dv.GetInstance();
        }

        public override BinaryDocValues GetBinary(FieldInfo field)
        {
            throw new InvalidOperationException();
        }

        public override SortedDocValues GetSorted(FieldInfo field)
        {
            throw new InvalidOperationException();
        }

        public override SortedSetDocValues GetSortedSet(FieldInfo field)
        {
            throw new InvalidOperationException();
        }
    }
}
