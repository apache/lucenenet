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
        public readonly string Name;

        /// <summary>
        /// Internal field number </summary>
        public readonly int Number;

        private bool indexed;
        private DocValuesType_e? docValueType;

        // True if any document indexed term vectors
        private bool StoreTermVector;

        private DocValuesType_e? NormTypeValue;
        private bool OmitNorms; // omit norms associated with indexed fields
        private IndexOptions? IndexOptionsValue;
        private bool StorePayloads; // whether this field stores payloads together with term positions

        private IDictionary<string, string> Attributes_Renamed;

        private long DvGen = -1; // the DocValues generation of this field

        /// <summary>
        /// Controls how much information is stored in the postings lists.
        /// @lucene.experimental
        /// </summary>
        public enum IndexOptions // LUCENENET TODO: de-nest from this class
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
        public enum DocValuesType_e // LUCENENET TODO: Rename back to DocValuesType and de-nest from this class
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

        /// <summary>
        /// Sole Constructor.
        ///
        /// @lucene.experimental
        /// </summary>
        public FieldInfo(string name, bool indexed, int number, bool storeTermVector, bool omitNorms, bool storePayloads, IndexOptions? indexOptions, DocValuesType_e? docValues, DocValuesType_e? normsType, IDictionary<string, string> attributes)
        {
            this.Name = name;
            this.indexed = indexed;
            this.Number = number;
            this.docValueType = docValues;
            if (indexed)
            {
                this.StoreTermVector = storeTermVector;
                this.StorePayloads = storePayloads;
                this.OmitNorms = omitNorms;
                this.IndexOptionsValue = indexOptions;
                this.NormTypeValue = !omitNorms ? normsType : null;
            } // for non-indexed fields, leave defaults
            else
            {
                this.StoreTermVector = false;
                this.StorePayloads = false;
                this.OmitNorms = false;
                this.IndexOptionsValue = null;
                this.NormTypeValue = null;
            }
            this.Attributes_Renamed = attributes;
            Debug.Assert(CheckConsistency());
        }

        private bool CheckConsistency()
        {
            if (!indexed)
            {
                Debug.Assert(!StoreTermVector);
                Debug.Assert(!StorePayloads);
                Debug.Assert(!OmitNorms);
                Debug.Assert(NormTypeValue == null);
                Debug.Assert(IndexOptionsValue == null);
            }
            else
            {
                Debug.Assert(IndexOptionsValue != null);
                if (OmitNorms)
                {
                    Debug.Assert(NormTypeValue == null);
                }
                // Cannot store payloads unless positions are indexed:
                Debug.Assert(((int)IndexOptionsValue.GetValueOrDefault() >= (int)IndexOptions.DOCS_AND_FREQS_AND_POSITIONS) || !this.StorePayloads);
            }

            return true;
        }

        internal void Update(IndexableFieldType ft)
        {
            Update(ft.Indexed, false, ft.OmitNorms, false, ft.IndexOptions);
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
                if (this.StoreTermVector != storeTermVector)
                {
                    this.StoreTermVector = true; // once vector, always vector
                }
                if (this.StorePayloads != storePayloads)
                {
                    this.StorePayloads = true;
                }
                if (this.OmitNorms != omitNorms)
                {
                    this.OmitNorms = true; // if one require omitNorms at least once, it remains off for life
                    this.NormTypeValue = null;
                }
                if (this.IndexOptionsValue != indexOptions)
                {
                    if (this.IndexOptionsValue == null)
                    {
                        this.IndexOptionsValue = indexOptions;
                    }
                    else
                    {
                        // downgrade
                        IndexOptionsValue = (int)IndexOptionsValue.GetValueOrDefault() < (int)indexOptions ? IndexOptionsValue : indexOptions;
                    }
                    if ((int)IndexOptionsValue.GetValueOrDefault() < (int)FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS)
                    {
                        // cannot store payloads if we don't store positions:
                        this.StorePayloads = false;
                    }
                }
            }
            Debug.Assert(CheckConsistency());
        }

        public DocValuesType_e? DocValuesType
        {
            set
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
        public IndexOptions? FieldIndexOptions
        {
            get
            {
                return IndexOptionsValue;
            }
        }

        /// <summary>
        /// Returns true if this field has any docValues.
        /// </summary>
        public bool HasDocValues()
        {
            return docValueType != null;
        }

        /// <summary>
        /// Sets the docValues generation of this field. </summary>
        public long DocValuesGen
        {
            set
            {
                this.DvGen = value;
            }
            get
            {
                return DvGen;
            }
        }

        /// <summary>
        /// Returns <seealso cref="DocValuesType_e"/> of the norm. this may be null if the field has no norms.
        /// </summary>
        public DocValuesType_e? NormType
        {
            get
            {
                return NormTypeValue;
            }
            set
            {
                if (NormTypeValue != null && NormTypeValue != value)
                {
                    throw new System.ArgumentException("cannot change Norm type from " + NormTypeValue + " to " + value + " for field \"" + Name + "\"");
                }
                NormTypeValue = value;
                Debug.Assert(CheckConsistency());
            }
        }

        internal void SetStoreTermVectors()
        {
            StoreTermVector = true;
            Debug.Assert(CheckConsistency());
        }

        public void SetStorePayloads()
        {
            if (indexed && (int)IndexOptionsValue.GetValueOrDefault() >= (int)FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS)
            {
                StorePayloads = true;
            }
            Debug.Assert(CheckConsistency());
        }

        /// <summary>
        /// Returns true if norms are explicitly omitted for this field
        /// </summary>
        public bool OmitsNorms()
        {
            return OmitNorms;
        }

        /// <summary>
        /// Returns true if this field actually has any norms.
        /// </summary>
        public bool HasNorms()
        {
            return NormTypeValue != null;
        }

        /// <summary>
        /// Returns true if this field is indexed.
        /// </summary>
        public bool Indexed
        {
            get
            {
                return indexed;
            }
        }

        /// <summary>
        /// Returns true if any payloads exist for this field.
        /// </summary>
        public bool HasPayloads()
        {
            return StorePayloads;
        }

        /// <summary>
        /// Returns true if any term vectors exist for this field.
        /// </summary>
        public bool HasVectors()
        {
            return StoreTermVector;
        }

        /// <summary>
        /// Get a codec attribute value, or null if it does not exist
        /// </summary>
        public string GetAttribute(string key)
        {
            if (Attributes_Renamed == null)
            {
                return null;
            }
            else
            {
                string ret;
                Attributes_Renamed.TryGetValue(key, out ret);
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
            if (Attributes_Renamed == null)
            {
                Attributes_Renamed = new Dictionary<string, string>();
            }

            string ret;
            // The key was not previously assigned, null will be returned
            if (!Attributes_Renamed.TryGetValue(key, out ret))
            {
                ret = null;
            }

            Attributes_Renamed[key] = value;
            return ret;
        }

        /// <summary>
        /// Returns internal codec attributes map. May be null if no mappings exist.
        /// </summary>
        public IDictionary<string, string> Attributes()
        {
            return Attributes_Renamed;
        }
    }
}