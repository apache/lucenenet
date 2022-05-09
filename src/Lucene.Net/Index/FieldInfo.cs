using Lucene.Net.Diagnostics;
using System;
using System.Collections.Generic;
using System.Diagnostics;

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

    /// <summary>
    ///  Access to the Field Info file that describes document fields and whether or
    ///  not they are indexed. Each segment has a separate Field Info file. Objects
    ///  of this class are thread-safe for multiple readers, but only one thread can
    ///  be adding documents at a time, with no other reader or writer threads
    ///  accessing this object.
    /// </summary>

    public sealed class FieldInfo
    {
        /// <summary>
        /// Field's name </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Internal field number </summary>
        public int Number { get; private set; }

        private bool indexed;
        private DocValuesType docValueType;

        // True if any document indexed term vectors
        private bool storeTermVector;

        private DocValuesType normType;
        private bool omitNorms; // omit norms associated with indexed fields
        private IndexOptions indexOptions;
        private bool storePayloads; // whether this field stores payloads together with term positions

        private IDictionary<string, string> attributes;

        private long dvGen = -1; // the DocValues generation of this field

        // LUCENENET specific: De-nested the IndexOptions and DocValuesType enums from this class to prevent naming conflicts

        /// <summary>
        /// Sole Constructor.
        /// <para/>
        /// @lucene.experimental
        /// </summary>
        public FieldInfo(string name, bool indexed, int number, bool storeTermVector, bool omitNorms, 
            bool storePayloads, IndexOptions indexOptions, DocValuesType docValues, DocValuesType normsType, 
            IDictionary<string, string> attributes)
        {
            this.Name = name;
            this.indexed = indexed;
            this.Number = number;
            this.docValueType = docValues;
            if (indexed)
            {
                this.storeTermVector = storeTermVector;
                this.storePayloads = storePayloads;
                this.omitNorms = omitNorms;
                this.indexOptions = indexOptions;
                this.normType = !omitNorms ? normsType : DocValuesType.NONE;
            } // for non-indexed fields, leave defaults
            else
            {
                this.storeTermVector = false;
                this.storePayloads = false;
                this.omitNorms = false;
                this.indexOptions = IndexOptions.NONE;
                this.normType = DocValuesType.NONE;
            }
            this.attributes = attributes;
            if (Debugging.AssertsEnabled) Debugging.Assert(CheckConsistency());
        }

        private bool CheckConsistency()
        {
            if (!indexed)
            {
                if (Debugging.AssertsEnabled)
                {
                    Debugging.Assert(!storeTermVector);
                    Debugging.Assert(!storePayloads);
                    Debugging.Assert(!omitNorms);
                    Debugging.Assert(normType == DocValuesType.NONE);
                    Debugging.Assert(indexOptions == IndexOptions.NONE);
                }
            }
            else
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(indexOptions != IndexOptions.NONE);
                if (omitNorms)
                {
                    if (Debugging.AssertsEnabled) Debugging.Assert(normType == DocValuesType.NONE);
                }
                // Cannot store payloads unless positions are indexed:
                // LUCENENET specific - to avoid boxing, changed from CompareTo() to IndexOptionsComparer.Compare()
                if (Debugging.AssertsEnabled) Debugging.Assert(IndexOptionsComparer.Default.Compare(indexOptions, IndexOptions.DOCS_AND_FREQS_AND_POSITIONS) >= 0 || !this.storePayloads);
            }

            return true;
        }

        internal void Update(IIndexableFieldType ft)
        {
            Update(ft.IsIndexed, false, ft.OmitNorms, false, ft.IndexOptions);
        }

        // should only be called by FieldInfos#addOrUpdate
        internal void Update(bool indexed, bool storeTermVector, bool omitNorms, bool storePayloads, IndexOptions indexOptions)
        {
            //System.out.println("FI.update field=" + name + " indexed=" + indexed + " omitNorms=" + omitNorms + " this.omitNorms=" + this.omitNorms);
            if (this.indexed != indexed)
            {
                this.indexed = true; // once indexed, always index
            }
            if (indexed) // if updated field data is not for indexing, leave the updates out
            {
                if (this.storeTermVector != storeTermVector)
                {
                    this.storeTermVector = true; // once vector, always vector
                }
                if (this.storePayloads != storePayloads)
                {
                    this.storePayloads = true;
                }
                if (this.omitNorms != omitNorms)
                {
                    this.omitNorms = true; // if one require omitNorms at least once, it remains off for life
                    this.normType = DocValuesType.NONE;
                }
                if (this.indexOptions != indexOptions)
                {
                    if (this.indexOptions == IndexOptions.NONE)
                    {
                        this.indexOptions = indexOptions;
                    }
                    else
                    {
                        // downgrade
                        // LUCENENET specific - to avoid boxing, changed from CompareTo() to IndexOptionsComparer.Compare()
                        this.indexOptions = IndexOptionsComparer.Default.Compare(this.indexOptions, indexOptions) < 0 ? this.indexOptions : indexOptions;
                    }
                    // LUCENENET specific - to avoid boxing, changed from CompareTo() to IndexOptionsComparer.Compare()
                    if (IndexOptionsComparer.Default.Compare(this.indexOptions, IndexOptions.DOCS_AND_FREQS_AND_POSITIONS) < 0)
                    {
                        // cannot store payloads if we don't store positions:
                        this.storePayloads = false;
                    }
                }
            }
            if (Debugging.AssertsEnabled) Debugging.Assert(CheckConsistency());
        }

        public DocValuesType DocValuesType
        {
            get => docValueType;
            internal set
            {
                if (docValueType != DocValuesType.NONE && docValueType != value)
                {
                    throw new ArgumentException("cannot change DocValues type from " + docValueType + " to " + value + " for field \"" + Name + "\"");
                }
                docValueType = value;
                if (Debugging.AssertsEnabled) Debugging.Assert(CheckConsistency());
            }
        }

        /// <summary>
        /// Returns <see cref="Index.IndexOptions"/> for the field, or <c>null</c> if the field is not indexed </summary>
        public IndexOptions IndexOptions => indexOptions;

        /// <summary>
        /// Returns <c>true</c> if this field has any docValues.
        /// </summary>
        public bool HasDocValues => docValueType != DocValuesType.NONE;

        /// <summary>
        /// Gets or Sets the docValues generation of this field, or -1 if no docValues. </summary>
        public long DocValuesGen
        {
            get => dvGen;
            set => this.dvGen = value;
        }

        /// <summary>
        /// Returns <see cref="Index.DocValuesType"/> of the norm. This may be <see cref="DocValuesType.NONE"/> if the field has no norms.
        /// </summary>
        public DocValuesType NormType
        {
            get => normType;
            internal set
            {
                if (normType != DocValuesType.NONE && normType != value)
                {
                    throw new ArgumentException("cannot change Norm type from " + normType + " to " + value + " for field \"" + Name + "\"");
                }
                normType = value;
                if (Debugging.AssertsEnabled) Debugging.Assert(CheckConsistency());
            }
        }

        internal void SetStoreTermVectors()
        {
            storeTermVector = true;
            if (Debugging.AssertsEnabled) Debugging.Assert(CheckConsistency());
        }

        internal void SetStorePayloads()
        {
            // LUCENENET specific - to avoid boxing, changed from CompareTo() to IndexOptionsComparer.Compare()
            if (indexed && IndexOptionsComparer.Default.Compare(indexOptions, IndexOptions.DOCS_AND_FREQS_AND_POSITIONS) >= 0)
            {
                storePayloads = true;
            }
            if (Debugging.AssertsEnabled) Debugging.Assert(CheckConsistency());
        }

        /// <summary>
        /// Returns <c>true</c> if norms are explicitly omitted for this field
        /// </summary>
        public bool OmitsNorms => omitNorms;

        /// <summary>
        /// Returns <c>true</c> if this field actually has any norms.
        /// </summary>
        public bool HasNorms => normType != DocValuesType.NONE;

        /// <summary>
        /// Returns <c>true</c> if this field is indexed.
        /// </summary>
        public bool IsIndexed => indexed;

        /// <summary>
        /// Returns <c>true</c> if any payloads exist for this field.
        /// </summary>
        public bool HasPayloads => storePayloads;

        /// <summary>
        /// Returns <c>true</c> if any term vectors exist for this field.
        /// </summary>
        public bool HasVectors => storeTermVector;

        /// <summary>
        /// Get a codec attribute value, or <c>null</c> if it does not exist
        /// </summary>
        public string GetAttribute(string key)
        {
            if (attributes is null)
            {
                return null;
            }
            else
            {
                attributes.TryGetValue(key, out string ret);
                return ret;
            }
        }

        /// <summary>
        /// Puts a codec attribute value.
        /// <para/>
        /// this is a key-value mapping for the field that the codec can use
        /// to store additional metadata, and will be available to the codec
        /// when reading the segment via <see cref="GetAttribute(string)"/>
        /// <para/>
        /// If a value already exists for the field, it will be replaced with
        /// the new value.
        /// </summary>
        public string PutAttribute(string key, string value)
        {
            if (attributes is null)
            {
                attributes = new Dictionary<string, string>();
            }

            // The key was not previously assigned, null will be returned
            if (!attributes.TryGetValue(key, out string ret))
            {
                ret = null;
            }

            attributes[key] = value;
            return ret;
        }

        /// <summary>
        /// Returns internal codec attributes map. May be <c>null</c> if no mappings exist.
        /// </summary>
        public IDictionary<string, string> Attributes => attributes;
    }

    /// <summary>
    /// Controls how much information is stored in the postings lists.
    /// <para/>
    /// @lucene.experimental
    /// </summary>
    public enum IndexOptions // LUCENENET specific: de-nested from FieldInfo to prevent naming collisions
    {
        // NOTE: order is important here; FieldInfo uses this
        // order to merge two conflicting IndexOptions (always
        // "downgrades" by picking the lowest).

        /// <summary>
        /// No index options will be used.
        /// <para/>
        /// NOTE: This is the same as setting to <c>null</c> in Lucene
        /// </summary>
        // LUCENENET specific
        NONE,

        /// <summary>
        /// Only documents are indexed: term frequencies and positions are omitted.
        /// Phrase and other positional queries on the field will throw an exception, and scoring
        /// will behave as if any term in the document appears only once.
        /// </summary>
        // TODO: maybe rename to just DOCS?
        DOCS_ONLY,

        /// <summary>
        /// Only documents and term frequencies are indexed: positions are omitted.
        /// this enables normal scoring, except Phrase and other positional queries
        /// will throw an exception.
        /// </summary>
        DOCS_AND_FREQS,

        /// <summary>
        /// Indexes documents, frequencies and positions.
        /// this is a typical default for full-text search: full scoring is enabled
        /// and positional queries are supported.
        /// </summary>
        DOCS_AND_FREQS_AND_POSITIONS,

        /// <summary>
        /// Indexes documents, frequencies, positions and offsets.
        /// Character offsets are encoded alongside the positions.
        /// </summary>
        DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS
    }

    /// <summary>
    /// DocValues types.
    /// Note that DocValues is strongly typed, so a field cannot have different types
    /// across different documents.
    /// </summary>
    public enum DocValuesType // LUCENENET specific: de-nested from FieldInfo to prevent naming collisions
    {
        /// <summary>
        /// No doc values type will be used.
        /// <para/>
        /// NOTE: This is the same as setting to <c>null</c> in Lucene
        /// </summary>
        // LUCENENET specific
        NONE, // LUCENENET NOTE: The value of this option is 0, which is the default value for any .NET value type

        /// <summary>
        /// A per-document numeric type
        /// </summary>
        NUMERIC,

        /// <summary>
        /// A per-document <see cref="T:byte[]"/>.  Values may be larger than
        /// 32766 bytes, but different codecs may enforce their own limits.
        /// </summary>
        BINARY,

        /// <summary>
        /// A pre-sorted <see cref="T:byte[]"/>. Fields with this type only store distinct byte values
        /// and store an additional offset pointer per document to dereference the shared
        /// byte[]. The stored byte[] is presorted and allows access via document id,
        /// ordinal and by-value.  Values must be &lt;= 32766 bytes.
        /// </summary>
        SORTED,

        /// <summary>
        /// A pre-sorted ISet&lt;byte[]&gt;. Fields with this type only store distinct byte values
        /// and store additional offset pointers per document to dereference the shared
        /// <see cref="T:byte[]"/>s. The stored <see cref="T:byte[]"/> is presorted and allows access via document id,
        /// ordinal and by-value.  Values must be &lt;= 32766 bytes.
        /// </summary>
        SORTED_SET
    }
}