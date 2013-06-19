/* 
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
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
using System.Collections.Generic;
using Lucene.Net.Support;

namespace Lucene.Net.Index
{

    public sealed class FieldInfo
    {
        public readonly string name;
        public readonly int number;

        private bool indexed;
        private DocValuesType? docValueType;

        private bool storeTermVector;

        private DocValuesType? normType;
        private bool omitNorms;
        private IndexOptions? indexOptions;
        private bool storePayloads;

        private IDictionary<string, string> attributes;

        public enum IndexOptions
        {
            DOCS_ONLY,
            DOCS_AND_FREQS,
            DOCS_AND_FREQS_AND_POSITIONS,
            DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS,
        }

        public enum DocValuesType
        {
            NUMERIC,
            BINARY,
            SORTED,
            SORTED_SET
        }

        public FieldInfo(String name, bool indexed, int number, bool storeTermVector,
            bool omitNorms, bool storePayloads, IndexOptions? indexOptions, DocValuesType? docValues, DocValuesType? normsType, IDictionary<String, String> attributes)
        {
            this.name = name;
            this.indexed = indexed;
            this.number = number;
            this.docValueType = docValues;
            if (indexed)
            {
                this.storeTermVector = storeTermVector;
                this.storePayloads = storePayloads;
                this.omitNorms = omitNorms;
                this.indexOptions = indexOptions;
                this.normType = !omitNorms ? normsType : (DocValuesType?)null;
            }
            else
            { // for non-indexed fields, leave defaults
                this.storeTermVector = false;
                this.storePayloads = false;
                this.omitNorms = false;
                this.indexOptions = null;
                this.normType = null;
            }
            this.attributes = attributes;
            //assert checkConsistency();
        }

        private bool CheckConsistency()
        {
            if (!indexed)
            {
                //assert !storeTermVector;
                //assert !storePayloads;
                //assert !omitNorms;
                //assert normType == null;
                //assert indexOptions == null;
            }
            else
            {
                //assert indexOptions != null;
                if (omitNorms)
                {
                    //assert normType == null;
                }
                // Cannot store payloads unless positions are indexed:
                //assert indexOptions.compareTo(IndexOptions.DOCS_AND_FREQS_AND_POSITIONS) >= 0 || !this.storePayloads;
            }

            return true;
        }

        internal void Update(IIndexableFieldType ft)
        {
            Update(ft.Indexed, false, ft.OmitNorms, false, ft.IndexOptions);
        }

        internal void Update(bool indexed, bool storeTermVector, bool omitNorms, bool storePayloads, IndexOptions indexOptions)
        {
            //System.out.println("FI.update field=" + name + " indexed=" + indexed + " omitNorms=" + omitNorms + " this.omitNorms=" + this.omitNorms);
            if (this.indexed != indexed)
            {
                this.indexed = true;                      // once indexed, always index
            }
            if (indexed)
            { // if updated field data is not for indexing, leave the updates out
                if (this.storeTermVector != storeTermVector)
                {
                    this.storeTermVector = true;                // once vector, always vector
                }
                if (this.storePayloads != storePayloads)
                {
                    this.storePayloads = true;
                }
                if (this.omitNorms != omitNorms)
                {
                    this.omitNorms = true;                // if one require omitNorms at least once, it remains off for life
                    this.normType = null;
                }
                if (this.indexOptions != indexOptions)
                {
                    if (this.indexOptions == null)
                    {
                        this.indexOptions = indexOptions;
                    }
                    else
                    {
                        // downgrade
                        this.indexOptions = (int)this.indexOptions.GetValueOrDefault() < (int)indexOptions ? this.indexOptions : indexOptions;
                    }
                    if ((int)this.indexOptions.GetValueOrDefault() < (int)FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS)
                    {
                        // cannot store payloads if we don't store positions:
                        this.storePayloads = false;
                    }
                }
            }
            //assert checkConsistency();
        }

        public DocValuesType? DocValuesTypeValue
        {
            get { return docValueType; }
            set
            {
                if (docValueType != null && docValueType != value)
                {
                    throw new ArgumentException("cannot change DocValues type from " + docValueType + " to " + value + " for field \"" + name + "\"");
                }
                docValueType = value;
                //assert checkConsistency();
            }
        }

        public IndexOptions? IndexOptionsValue
        {
            get { return indexOptions; }
        }

        public bool HasDocValues
        {
            get { return docValueType != null; }
        }

        public DocValuesType? NormType
        {
            get { return normType; }
            set
            {
                if (normType != null && normType != value)
                {
                    throw new ArgumentException("cannot change Norm type from " + normType + " to " + value + " for field \"" + name + "\"");
                }
                normType = value;
                //assert checkConsistency();
            }
        }

        internal bool StoreTermVectors
        {
            get { return storeTermVector; }
            set
            {
                storeTermVector = true;
                // assert checkConsistency();
            }
        }

        internal bool StorePayloads
        {
            get { return storePayloads; }
            set
            {
                if (indexed && (int)indexOptions.GetValueOrDefault() >= (int)FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS)
                {
                    storePayloads = true;
                }
                //assert checkConsistency();
            }
        }

        public bool OmitsNorms
        {
            get { return omitNorms; }
        }

        public bool HasNorms
        {
            get { return normType.HasValue; }
        }

        public bool IsIndexed
        {
            get { return indexed; }
        }

        public bool HasPayloads
        {
            get { return storePayloads; }
        }

        public bool HasVectors
        {
            get { return storeTermVector; }
        }

        public string GetAttribute(string key)
        {
            if (attributes == null)
                return null;
            else
                return attributes[key];
        }

        public string PutAttribute(string key, string value)
        {
            if (attributes == null)
            {
                attributes = new HashMap<string, string>();
            }
            return attributes[key] = value;
        }

        public IDictionary<string, string> Attributes
        {
            get { return attributes; }
        }
    }
}