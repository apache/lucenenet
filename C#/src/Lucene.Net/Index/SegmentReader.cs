/*
 * Copyright 2004 The Apache Software Foundation
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 * 
 * http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using Document = Lucene.Net.Documents.Document;
using Field = Lucene.Net.Documents.Field;
using DefaultSimilarity = Lucene.Net.Search.DefaultSimilarity;
using Directory = Lucene.Net.Store.Directory;
using IndexInput = Lucene.Net.Store.IndexInput;
using IndexOutput = Lucene.Net.Store.IndexOutput;
using BitVector = Lucene.Net.Util.BitVector;

namespace Lucene.Net.Index
{
	
    /// <version>  $Id: SegmentReader.java 329523 2005-10-30 05:37:11Z yonik $
    /// </version>
    public class SegmentReader : IndexReader
    {
        private System.String segment;
		
        internal FieldInfos fieldInfos;
        private FieldsReader fieldsReader;
		
        internal TermInfosReader tis;
        internal TermVectorsReader termVectorsReaderOrig = null;
        internal System.LocalDataStoreSlot termVectorsLocal = System.Threading.Thread.AllocateDataSlot();
		
        internal BitVector deletedDocs = null;
        private bool deletedDocsDirty = false;
        private bool normsDirty = false;
        private bool undeleteAll = false;
		
        internal IndexInput freqStream;
        internal IndexInput proxStream;
		
        // Compound File Reader when based on a compound file segment
        internal CompoundFileReader cfsReader = null;
		
        public FieldInfos FieldInfos
        {
            get {   return fieldInfos;  }
        }

        private class Norm
        {
            private void  InitBlock(SegmentReader enclosingInstance)
            {
                this.enclosingInstance = enclosingInstance;
            }
            private SegmentReader enclosingInstance;
            public SegmentReader Enclosing_Instance
            {
                get
                {
                    return enclosingInstance;
                }
				
            }
            public Norm(SegmentReader enclosingInstance, IndexInput in_Renamed, int number)
            {
                InitBlock(enclosingInstance);
                this.in_Renamed = in_Renamed;
                this.number = number;
            }
			
            public IndexInput in_Renamed;
            public byte[] bytes;
            public bool dirty;
            public int number;
			
            public void  ReWrite()
            {
                // NOTE: norms are re-written in regular directory, not cfs
                IndexOutput out_Renamed = Enclosing_Instance.Directory().CreateOutput(Enclosing_Instance.segment + ".tmp");
                try
                {
                    out_Renamed.WriteBytes(bytes, Enclosing_Instance.MaxDoc());
                }
                finally
                {
                    out_Renamed.Close();
                }
                System.String fileName;
                if (Enclosing_Instance.cfsReader == null)
                    fileName = Enclosing_Instance.segment + ".f" + number;
                else
                {
                    // use a different file name if we have compound format
                    fileName = Enclosing_Instance.segment + ".s" + number;
                }
                Enclosing_Instance.Directory().RenameFile(Enclosing_Instance.segment + ".tmp", fileName);
                this.dirty = false;
            }
        }
		
        private System.Collections.Hashtable norms = System.Collections.Hashtable.Synchronized(new System.Collections.Hashtable());
		
        /// <summary>The class which implements SegmentReader. </summary>
        private static System.Type IMPL;
		
        public SegmentReader() : base(null)
        {
        }
		
        public static SegmentReader Get(SegmentInfo si)
        {
            return Get(si.dir, si, null, false, false);
        }
		
        public static SegmentReader Get(SegmentInfos sis, SegmentInfo si, bool closeDir)
        {
            return Get(si.dir, si, sis, closeDir, true);
        }
		
        public static SegmentReader Get(Directory dir, SegmentInfo si, SegmentInfos sis, bool closeDir, bool ownDir)
        {
            SegmentReader instance;
            try
            {
                instance = (SegmentReader) System.Activator.CreateInstance(IMPL);
            }
            catch (System.Exception e)
            {
                throw new System.Exception("cannot load SegmentReader class: " + e, e);
            }
            instance.Init(dir, sis, closeDir, ownDir);
            instance.Initialize(si);
            return instance;
        }
		
        private void  Initialize(SegmentInfo si)
        {
            segment = si.name;
			
            // Use compound file directory for some files, if it exists
            Directory cfsDir = Directory();
            if (Directory().FileExists(segment + ".cfs"))
            {
                cfsReader = new CompoundFileReader(Directory(), segment + ".cfs");
                cfsDir = cfsReader;
            }
			
            // No compound file exists - use the multi-file format
            fieldInfos = new FieldInfos(cfsDir, segment + ".fnm");
            fieldsReader = new FieldsReader(cfsDir, segment, fieldInfos);
			
            tis = new TermInfosReader(cfsDir, segment, fieldInfos);
			
            // NOTE: the bitvector is stored using the regular directory, not cfs
            if (HasDeletions(si))
                deletedDocs = new BitVector(Directory(), segment + ".del");
			
            // make sure that all index files have been read or are kept open
            // so that if an index update removes them we'll still have them
            freqStream = cfsDir.OpenInput(segment + ".frq");
            proxStream = cfsDir.OpenInput(segment + ".prx");
            OpenNorms(cfsDir);
			
            if (fieldInfos.HasVectors())
            {
                // open term vector files only as needed
                termVectorsReaderOrig = new TermVectorsReader(cfsDir, segment, fieldInfos);
            }
        }
		
        ~SegmentReader()
        {
            // patch for pre-1.4.2 JVMs, whose ThreadLocals leak
            //System.Threading.Thread.SetData(termVectorsLocal, null);
        }
		
        protected internal override void  DoCommit()
        {
            if (deletedDocsDirty)
            {
                // re-write deleted
                deletedDocs.Write(Directory(), segment + ".tmp");
                Directory().RenameFile(segment + ".tmp", segment + ".del");
            }
            if (undeleteAll && Directory().FileExists(segment + ".del"))
            {
                Directory().DeleteFile(segment + ".del");
            }
            if (normsDirty)
            {
                // re-write norms
                System.Collections.IEnumerator values = norms.Values.GetEnumerator();
                while (values.MoveNext())
                {
                    Norm norm = (Norm) values.Current;
                    if (norm.dirty)
                    {
                        norm.ReWrite();
                    }
                }
            }
            deletedDocsDirty = false;
            normsDirty = false;
            undeleteAll = false;
        }
		
        protected internal override void  DoClose()
        {
            fieldsReader.Close();
            tis.Close();
			
            if (freqStream != null)
                freqStream.Close();
            if (proxStream != null)
                proxStream.Close();
			
            CloseNorms();
			
            if (termVectorsReaderOrig != null)
                termVectorsReaderOrig.Close();
			
            if (cfsReader != null)
                cfsReader.Close();
        }
		
        internal static bool HasDeletions(SegmentInfo si)
        {
            return si.dir.FileExists(si.name + ".del");
        }
		
        public override bool HasDeletions()
        {
            return deletedDocs != null;
        }
		
		
        internal static bool UsesCompoundFile(SegmentInfo si)
        {
            return si.dir.FileExists(si.name + ".cfs");
        }
		
        internal static bool HasSeparateNorms(SegmentInfo si)
        {
            System.String[] result = si.dir.List();
            System.String pattern = si.name + ".s";
            int patternLength = pattern.Length;
            for (int i = 0; i < result.Length; i++)
            {
                if (result[i].StartsWith(pattern) && System.Char.IsDigit(result[i][patternLength]))
                    return true;
            }
            return false;
        }
		
        protected internal override void  DoDelete(int docNum)
        {
            if (deletedDocs == null)
                deletedDocs = new BitVector(MaxDoc());
            deletedDocsDirty = true;
            undeleteAll = false;
            deletedDocs.Set(docNum);
        }
		
        protected internal override void  DoUndeleteAll()
        {
            deletedDocs = null;
            deletedDocsDirty = false;
            undeleteAll = true;
        }
		
        internal virtual System.Collections.ArrayList Files()
        {
            System.Collections.ArrayList files = System.Collections.ArrayList.Synchronized(new System.Collections.ArrayList(16));
			
            for (int i = 0; i < IndexFileNames.INDEX_EXTENSIONS.Length; i++)
            {
                System.String name = segment + "." + IndexFileNames.INDEX_EXTENSIONS[i];
                if (Directory().FileExists(name))
                    files.Add(name);
            }
			
            for (int i = 0; i < fieldInfos.Size(); i++)
            {
                FieldInfo fi = fieldInfos.FieldInfo(i);
                if (fi.isIndexed && !fi.omitNorms)
                {
                    System.String name;
                    if (cfsReader == null)
                        name = segment + ".f" + i;
                    else
                        name = segment + ".s" + i;
                    if (Directory().FileExists(name))
                        files.Add(name);
                }
            }
            return files;
        }
		
        public override TermEnum Terms()
        {
            return tis.Terms();
        }
		
        public override TermEnum Terms(Term t)
        {
            return tis.Terms(t);
        }
		
        public override Document Document(int n)
        {
            lock (this)
            {
                if (IsDeleted(n))
                    throw new System.ArgumentException("attempt to access a deleted document");
                return fieldsReader.Doc(n);
            }
        }
		
        public override bool IsDeleted(int n)
        {
            lock (this)
            {
                return (deletedDocs != null && deletedDocs.Get(n));
            }
        }
		
        public override TermDocs TermDocs()
        {
            return new SegmentTermDocs(this);
        }
		
        public override TermPositions TermPositions()
        {
            return new SegmentTermPositions(this);
        }
		
        public override int DocFreq(Term t)
        {
            TermInfo ti = tis.Get(t);
            if (ti != null)
                return ti.docFreq;
            else
                return 0;
        }
		
        public override int NumDocs()
        {
            int n = MaxDoc();
            if (deletedDocs != null)
                n -= deletedDocs.Count();
            return n;
        }
		
        public override int MaxDoc()
        {
            return fieldsReader.Size();
        }
		
        /// <seealso cref="IndexReader.GetFieldNames(IndexReader.FieldOption fldOption)">
        /// </seealso>
        public override System.Collections.ICollection GetFieldNames(IndexReader.FieldOption fieldOption)
        {
            System.Collections.Hashtable fieldSet = new System.Collections.Hashtable();
            for (int i = 0; i < fieldInfos.Size(); i++)
            {
                FieldInfo fi = fieldInfos.FieldInfo(i);
                if (fieldOption == IndexReader.FieldOption.ALL)
                {
                    fieldSet.Add(fi.name, fi.name);
                }
                else if (!fi.isIndexed && fieldOption == IndexReader.FieldOption.UNINDEXED)
                {
                    fieldSet.Add(fi.name, fi.name);
                }
                else if (fi.isIndexed && fieldOption == IndexReader.FieldOption.INDEXED)
                {
                    fieldSet.Add(fi.name, fi.name);
                }
                else if (fi.isIndexed && fi.storeTermVector == false && fieldOption == IndexReader.FieldOption.INDEXED_NO_TERMVECTOR)
                {
                    fieldSet.Add(fi.name, fi.name);
                }
                else if (fi.storeTermVector == true && fi.storePositionWithTermVector == false && fi.storeOffsetWithTermVector == false && fieldOption == IndexReader.FieldOption.TERMVECTOR)
                {
                    fieldSet.Add(fi.name, fi.name);
                }
                else if (fi.isIndexed && fi.storeTermVector && fieldOption == IndexReader.FieldOption.INDEXED_WITH_TERMVECTOR)
                {
                    fieldSet.Add(fi.name, fi.name);
                }
                else if (fi.storePositionWithTermVector && fi.storeOffsetWithTermVector == false && fieldOption == IndexReader.FieldOption.TERMVECTOR_WITH_POSITION)
                {
                    fieldSet.Add(fi.name, fi.name);
                }
                else if (fi.storeOffsetWithTermVector && fi.storePositionWithTermVector == false && fieldOption == IndexReader.FieldOption.TERMVECTOR_WITH_OFFSET)
                {
                    fieldSet.Add(fi.name, fi.name);
                }
                else if ((fi.storeOffsetWithTermVector && fi.storePositionWithTermVector) && fieldOption == IndexReader.FieldOption.TERMVECTOR_WITH_POSITION_OFFSET)
                {
                    fieldSet.Add(fi.name, fi.name);
                }
            }
            return fieldSet;
        }
		
		
        public override bool HasNorms(System.String field)
        {
            lock (this)
            {
                return norms.ContainsKey(field);
            }
        }
		
        internal static byte[] CreateFakeNorms(int size)
        {
            byte[] ones = new byte[size];
            byte val = DefaultSimilarity.EncodeNorm(1.0f);
            for (int index = 0; index < size; index++)
                ones.SetValue(val, index);

            return ones;
        }
		
        private byte[] ones;
        private byte[] FakeNorms()
        {
            if (ones == null)
                ones = CreateFakeNorms(MaxDoc());
            return ones;
        }
		
        // can return null if norms aren't stored
        protected internal virtual byte[] GetNorms(System.String field)
        {
            lock (this)
            {
                Norm norm = (Norm) norms[field];
                if (norm == null)
                    return null; // not indexed, or norms not stored
				
                if (norm.bytes == null)
                {
                    // value not yet read
                    byte[] bytes = new byte[MaxDoc()];
                    Norms(field, bytes, 0);
                    norm.bytes = bytes; // cache it
                }
                return norm.bytes;
            }
        }
		
        // returns fake norms if norms aren't available
        public override byte[] Norms(System.String field)
        {
            lock (this)
            {
                byte[] bytes = GetNorms(field);
                if (bytes == null)
                    bytes = FakeNorms();
                return bytes;
            }
        }
		
        protected internal override void  DoSetNorm(int doc, System.String field, byte value_Renamed)
        {
            Norm norm = (Norm) norms[field];
            if (norm == null)
                // not an indexed field
                return ;
            norm.dirty = true; // mark it dirty
            normsDirty = true;
			
            Norms(field)[doc] = value_Renamed; // set the value
        }
		
        /// <summary>Read norms into a pre-allocated array. </summary>
        public override void  Norms(System.String field, byte[] bytes, int offset)
        {
            lock (this)
            {
				
                Norm norm = (Norm) norms[field];
                if (norm == null)
                {
                    Array.Copy(FakeNorms(), 0, bytes, offset, MaxDoc());
                    return ;
                }
				
                if (norm.bytes != null)
                {
                    // can copy from cache
                    Array.Copy(norm.bytes, 0, bytes, offset, MaxDoc());
                    return ;
                }
				
                IndexInput normStream = (IndexInput) norm.in_Renamed.Clone();
                try
                {
                    // read from disk
                    normStream.Seek(0);
                    normStream.ReadBytes(bytes, offset, MaxDoc());
                }
                finally
                {
                    normStream.Close();
                }
            }
        }
		
		
        private void  OpenNorms(Directory cfsDir)
        {
            for (int i = 0; i < fieldInfos.Size(); i++)
            {
                FieldInfo fi = fieldInfos.FieldInfo(i);
                if (fi.isIndexed && !fi.omitNorms)
                {
                    // look first if there are separate norms in compound format
                    System.String fileName = segment + ".s" + fi.number;
                    Directory d = Directory();
                    if (!d.FileExists(fileName))
                    {
                        fileName = segment + ".f" + fi.number;
                        d = cfsDir;
                    }
                    norms[fi.name] = new Norm(this, d.OpenInput(fileName), fi.number);
                }
            }
        }
		
        private void  CloseNorms()
        {
            lock (norms.SyncRoot)
            {
                System.Collections.IEnumerator enumerator = norms.Values.GetEnumerator();
                while (enumerator.MoveNext())
                {
                    Norm norm = (Norm) enumerator.Current;
                    norm.in_Renamed.Close();
                }
            }
        }
		
        /// <summary> Create a clone from the initial TermVectorsReader and store it in the ThreadLocal.</summary>
        /// <returns> TermVectorsReader
        /// </returns>
        private TermVectorsReader GetTermVectorsReader()
        {
            TermVectorsReader tvReader = (TermVectorsReader) System.Threading.Thread.GetData(termVectorsLocal);
            if (tvReader == null)
            {
                tvReader = (TermVectorsReader) termVectorsReaderOrig.Clone();
                System.Threading.Thread.SetData(termVectorsLocal, tvReader);
            }
            return tvReader;
        }
		
        /// <summary>Return a term frequency vector for the specified document and field. The
        /// vector returned contains term numbers and frequencies for all terms in
        /// the specified field of this document, if the field had storeTermVector
        /// flag set.  If the flag was not set, the method returns null.
        /// </summary>
        /// <throws>  IOException </throws>
        public override TermFreqVector GetTermFreqVector(int docNumber, System.String field)
        {
            // Check if this field is invalid or has no stored term vector
            FieldInfo fi = fieldInfos.FieldInfo(field);
            if (fi == null || !fi.storeTermVector || termVectorsReaderOrig == null)
                return null;
			
            TermVectorsReader termVectorsReader = GetTermVectorsReader();
            if (termVectorsReader == null)
                return null;
			
            return termVectorsReader.Get(docNumber, field);
        }
		
		
        /// <summary>Return an array of term frequency vectors for the specified document.
        /// The array contains a vector for each vectorized field in the document.
        /// Each vector vector contains term numbers and frequencies for all terms
        /// in a given vectorized field.
        /// If no such fields existed, the method returns null.
        /// </summary>
        /// <throws>  IOException </throws>
        public override TermFreqVector[] GetTermFreqVectors(int docNumber)
        {
            if (termVectorsReaderOrig == null)
                return null;
			
            TermVectorsReader termVectorsReader = GetTermVectorsReader();
            if (termVectorsReader == null)
                return null;
			
            return termVectorsReader.Get(docNumber);
        }

        static SegmentReader()
        {
            {
                try
                {
                    System.String name = SupportClass.AppSettings.Get("Lucene.Net.SegmentReader.class", typeof(SegmentReader).FullName);
                    IMPL = System.Type.GetType(name);
                }
                catch (System.Security.SecurityException)
                {
                    try
                    {
                        IMPL = System.Type.GetType(typeof(SegmentReader).FullName);
                    }
                    catch (System.Exception e)
                    {
                        throw new System.Exception("cannot load default SegmentReader class: " + e, e); // {{Aroush-2.0}} How do we throw a RuntimeException
                    }
                }
                catch (System.Exception e)
                {
                    throw new System.Exception("cannot load SegmentReader class: " + e, e); // {{Aroush-2.0}} How do we throw a RuntimeException
                }
            }
        }
    }
}