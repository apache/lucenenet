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
    ///
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
        private DocValuesType_e? docValueType;

        // True if any document indexed term vectors
        private bool storeTermVector;

        private DocValuesType_e? normTypeValue;
        private bool omitNorms; // omit norms associated with indexed fields
        private IndexOptions? indexOptionsValue;
        private bool storePayloads; // whether this field stores payloads together with term positions

        private IDictionary<string, string> attributes;

        private long dvGen = -1; // the DocValues generation of this field

        // LUCENENET specific: De-nested the IndexOptions and DocValuesType enums from this class to prevent naming conflicts

        /// <summary>
        /// Sole Constructor.
        ///
        /// @lucene.experimental
        /// </summary>
        public FieldInfo(string name, bool indexed, int number, bool storeTermVector, bool omitNorms, 
            bool storePayloads, IndexOptions? indexOptions, DocValuesType_e? docValues, DocValuesType_e? normsType, 
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
                this.indexOptionsValue = indexOptions;
                this.normTypeValue = !omitNorms ? normsType : null;
            } // for non-indexed fields, leave defaults
            else
            {
                this.storeTermVector = false;
                this.storePayloads = false;
                this.omitNorms = false;
                this.indexOptionsValue = null;
                this.normTypeValue = null;
            }
            this.attributes = attributes;
            Debug.Assert(CheckConsistency());
        }

        private bool CheckConsistency()
        {
            if (!indexed)
            {
                Debug.Assert(!storeTermVector);
                Debug.Assert(!storePayloads);
                Debug.Assert(!omitNorms);
                Debug.Assert(normTypeValue == null);
                Debug.Assert(indexOptionsValue == null);
            }
            else
            {
                Debug.Assert(indexOptionsValue != null);
                if (omitNorms)
                {
                    Debug.Assert(normTypeValue == null);
                }
                // Cannot store payloads unless positions are indexed:
                Debug.Assert(((int)indexOptionsValue.GetValueOrDefault() >= (int)Index.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS) || !this.storePayloads);
            }

            return true;
        }

        internal void Update(IndexableFieldType ft)
        {
            Update(ft.IsIndexed, false, ft.OmitNorms, false, ft.IndexOptions);
        }

        // should only be called by FieldInfos#addOrUpdate
        internal void Update(bool indexed, bool storeTermVector, bool omitNorms, bool storePayloads, IndexOptions? indexOptions)
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
                    this.normTypeValue = null;
                }
                if (this.indexOptionsValue != indexOptions)
                {
                    if (this.indexOptionsValue == null)
                    {
                        this.indexOptionsValue = indexOptions;
                    }
                    else
                    {
                        // downgrade
                        indexOptionsValue = (int)indexOptionsValue.GetValueOrDefault() < (int)indexOptions ? indexOptionsValue : indexOptions;
                    }
                    if ((int)indexOptionsValue.GetValueOrDefault() < (int)Index.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS)
                    {
                        // cannot store payloads if we don't store positions:
                        this.storePayloads = false;
                    }
                }
            }
            Debug.Assert(CheckConsistency());
        }

        public DocValuesType_e? DocValuesType // LUCENENET TODO: try to make non-nullable
        {
            internal set
            {
                if (docValueType != null && docValueType != value)
                {
                    throw new System.ArgumentException("cannot change DocValues type from " + docValueType + " to " + value + " for field \"" + Name + "\"");
                }
                docValueType = value;
                Debug.Assert(CheckConsistency());
            }
            get
            {
                return docValueType;
            }
        }

        /// <summary>
        /// Returns IndexOptions for the field, or null if the field is not indexed </summary>
        public IndexOptions? IndexOptions // LUCENENET TODO: try to make non-nullable
        {
            get
            {
                return indexOptionsValue;
            }
        }

        /// <summary>
        /// Returns true if this field has any docValues.
        /// </summary>
        public bool HasDocValues() // LUCENENET TODO: Make property
        {
            return docValueType != null;
        }

        /// <summary>
        /// Sets the docValues generation of this field. </summary>
        public long DocValuesGen
        {
            set
            {
                this.dvGen = value;
            }
            get
            {
                return dvGen;
            }
        }

        /// <summary>
        /// Returns <seealso cref="DocValuesType_e"/> of the norm. this may be null if the field has no norms.
        /// </summary>
        public DocValuesType_e? NormType // LUCENENET TODO: try to make non-nullable
        {
            get
            {
                return normTypeValue;
            }
            internal set
            {
                if (normTypeValue != null && normTypeValue != value)
                {
                    throw new System.ArgumentException("cannot change Norm type from " + normTypeValue + " to " + value + " for field \"" + Name + "\"");
                }
                normTypeValue = value;
                Debug.Assert(CheckConsistency());
            }
        }

        internal void SetStoreTermVectors()
        {
            storeTermVector = true;
            Debug.Assert(CheckConsistency());
        }

        internal void SetStorePayloads()
        {
            if (indexed && (int)indexOptionsValue.GetValueOrDefault() >= (int)Index.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS)
            {
                storePayloads = true;
            }
            Debug.Assert(CheckConsistency());
        }

        /// <summary>
        /// Returns true if norms are explicitly omitted for this field
        /// </summary>
        public bool OmitsNorms() // LUCENENET TODO: Make property
        {
            return omitNorms;
        }

        /// <summary>
        /// Returns true if this field actually has any norms.
        /// </summary>
        public bool HasNorms() // LUCENENET TODO: Make property
        {
            return normTypeValue != null;
        }

        /// <summary>
        /// Returns true if this field is indexed.
        /// </summary>
        public bool Indexed // LUCENENET TODO: Rename IsIndexed
        {
            get
            {
                return indexed;
            }
        }

        /// <summary>
        /// Returns true if any payloads exist for this field.
        /// </summary>
        public bool HasPayloads() // LUCENENET TODO: Make property
        {
            return storePayloads;
        }

        /// <summary>
        /// Returns true if any term vectors exist for this field.
        /// </summary>
        public bool HasVectors() // LUCENENET TODO: Make property
        {
            return storeTermVector;
        }

        /// <summary>
        /// Get a codec attribute value, or null if it does not exist
        /// </summary>
        public string GetAttribute(string key)
        {
            if (attributes == null)
            {
                return null;
            }
            else
            {
                string ret;
                attributes.TryGetValue(key, out ret);
                return ret;
            }
        }

        /// <summary>
        /// Puts a codec attribute value.
        /// <p>
        /// this is a key-value mapping for the field that the codec can use
        /// to store additional metadata, and will be available to the codec
        /// when reading the segment via <seealso cref="#getAttribute(String)"/>
        /// <p>
        /// If a value already exists for the field, it will be replaced with
        /// the new value.
        /// </summary>
        public string PutAttribute(string key, string value)
        {
            if (attributes == null)
            {
                attributes = new Dictionary<string, string>();
            }

            string ret;
            // The key was not previously assigned, null will be returned
            if (!attributes.TryGetValue(key, out ret))
            {
                ret = null;
            }

            attributes[key] = value;
            return ret;
        }

        /// <summary>
        /// Returns internal codec attributes map. May be null if no mappings exist.
        /// </summary>
        public IDictionary<string, string> Attributes() // LUCENENET TODO: Make property
        {
            return attributes;
        }
    }

    /// <summary>
    /// Controls how much information is stored in the postings lists.
    /// @lucene.experimental
    /// </summary>
    // LUCENENET TODO: Add a NOT_SET = 0 state so we ca get rid of nullables?
    public enum IndexOptions // LUCENENET specific: de-nested from FieldInfo to prevent naming collisions
    {
        // NOTE: order is important here; FieldInfo uses this
        // order to merge two conflicting IndexOptions (always
        // "downgrades" by picking the lowest).
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
    // LUCENENET TODO: Add a NOT_SET = 0 state so we ca get rid of nullables?
    public enum DocValuesType_e // LUCENENET specific: de-nested from FieldInfo to prevent naming collisions
    {
        /// <summary>
        /// A per-document Number
        /// </summary>
        NUMERIC,

        /// <summary>
        /// A per-document byte[].  Values may be larger than
        /// 32766 bytes, but different codecs may enforce their own limits.
        /// </summary>
        BINARY,

        /// <summary>
        /// A pre-sorted byte[]. Fields with this type only store distinct byte values
        /// and store an additional offset pointer per document to dereference the shared
        /// byte[]. The stored byte[] is presorted and allows access via document id,
        /// ordinal and by-value.  Values must be <= 32766 bytes.
        /// </summary>
        SORTED,

        /// <summary>
        /// A pre-sorted Set&lt;byte[]&gt;. Fields with this type only store distinct byte values
        /// and store additional offset pointers per document to dereference the shared
        /// byte[]s. The stored byte[] is presorted and allows access via document id,
        /// ordinal and by-value.  Values must be <= 32766 bytes.
        /// </summary>
        SORTED_SET
    }
}