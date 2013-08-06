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

using Lucene.Net.Documents;
using Lucene.Net.Support;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Directory = Lucene.Net.Store.Directory;
using Document = Lucene.Net.Documents.Document;
using IndexInput = Lucene.Net.Store.IndexInput;
using IndexOutput = Lucene.Net.Store.IndexOutput;
using StringHelper = Lucene.Net.Util.StringHelper;

namespace Lucene.Net.Index
{

    /// <summary>Access to the Fieldable Info file that describes document fields and whether or
    /// not they are indexed. Each segment has a separate Fieldable Info file. Objects
    /// of this class are thread-safe for multiple readers, but only one thread can
    /// be adding documents at a time, with no other reader or writer threads
    /// accessing this object.
    /// </summary>
    public sealed class FieldInfos : IEnumerable<FieldInfo>
    {
        private readonly bool hasFreq;
        private readonly bool hasProx;
        private readonly bool hasPayloads;
        private readonly bool hasOffsets;
        private readonly bool hasVectors;
        private readonly bool hasNorms;
        private readonly bool hasDocValues;

        private readonly IDictionary<int, FieldInfo> byNumber = new TreeMap<int, FieldInfo>();
        private readonly HashMap<String, FieldInfo> byName = new HashMap<String, FieldInfo>();
        private readonly ICollection<FieldInfo> values; // for an unmodifiable iterator

        public FieldInfos(FieldInfo[] infos)
        {
            bool hasVectors = false;
            bool hasProx = false;
            bool hasPayloads = false;
            bool hasOffsets = false;
            bool hasFreq = false;
            bool hasNorms = false;
            bool hasDocValues = false;

            foreach (FieldInfo info in infos)
            {
                FieldInfo previous;

                if (byNumber.TryGetValue(info.number, out previous))
                {
                    throw new ArgumentException("duplicate field numbers: " + previous.name + " and " + info.name + " have: " + info.number);
                }

                byNumber[info.number] = info;

                if (byName.TryGetValue(info.name, out previous))
                {
                    throw new ArgumentException("duplicate field names: " + previous.number + " and " + info.number + " have: " + info.name);
                }

                byName[info.name] = info;

                hasVectors |= info.HasVectors;
                hasProx |= info.IsIndexed && info.IndexOptionsValue >= Index.FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS;
                hasFreq |= info.IsIndexed && info.IndexOptionsValue != Index.FieldInfo.IndexOptions.DOCS_ONLY;
                hasOffsets |= info.IsIndexed && info.IndexOptionsValue >= Index.FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS;
                hasNorms |= info.HasNorms;
                hasDocValues |= info.HasDocValues;
                hasPayloads |= info.HasPayloads;
            }

            this.hasVectors = hasVectors;
            this.hasProx = hasProx;
            this.hasPayloads = hasPayloads;
            this.hasOffsets = hasOffsets;
            this.hasFreq = hasFreq;
            this.hasNorms = hasNorms;
            this.hasDocValues = hasDocValues;
            this.values = byNumber.Values;
        }

        public bool HasFreq
        {
            get { return hasFreq; }
        }

        public bool HasProx
        {
            get { return hasProx; }
        }

        public bool HasPayloads
        {
            get { return hasPayloads; }
        }

        public bool HasOffsets
        {
            get { return hasOffsets; }
        }

        public bool HasVectors
        {
            get { return hasVectors; }
        }

        public bool HasNorms
        {
            get { return hasNorms; }
        }

        public bool HasDocValues
        {
            get { return hasDocValues; }
        }

        public int Size
        {
            get { return byNumber.Count; }
        }

        public IEnumerator<FieldInfo> GetEnumerator()
        {
            return values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public FieldInfo FieldInfo(string fieldName)
        {
            return byName[fieldName];
        }

        /// <summary> Return the fieldinfo object referenced by the fieldNumber.</summary>
        /// <param name="fieldNumber">
        /// </param>
        /// <returns> the FieldInfo object or null when the given fieldNumber
        /// doesn't exist.
        /// </returns>
        public FieldInfo FieldInfo(int fieldNumber)
        {
            return (fieldNumber >= 0) ? byNumber[fieldNumber] : null;
        }

        public sealed class FieldNumbers
        {
            private readonly IDictionary<int, string> numberToName;
            private readonly IDictionary<string, int> nameToNumber;
            // We use this to enforce that a given field never
            // changes DV type, even across segments / IndexWriter
            // sessions:
            private readonly IDictionary<string, Index.FieldInfo.DocValuesType?> docValuesType;

            // TODO: we should similarly catch an attempt to turn
            // norms back on after they were already ommitted; today
            // we silently discard the norm but this is badly trappy
            private int lowestUnassignedFieldNumber = -1;

            internal FieldNumbers()
            {
                this.nameToNumber = new HashMap<string, int>();
                this.numberToName = new HashMap<int, string>();
                this.docValuesType = new HashMap<string, Index.FieldInfo.DocValuesType?>();
            }

            internal int AddOrGet(string fieldName, int preferredFieldNumber, Index.FieldInfo.DocValuesType? dvType)
            {
                lock (this)
                {
                    if (dvType != null)
                    {
                        Index.FieldInfo.DocValuesType? currentDVType = docValuesType[fieldName];
                        if (currentDVType == null)
                        {
                            docValuesType[fieldName] = dvType;
                        }
                        else if (currentDVType != null && currentDVType != dvType)
                        {
                            throw new ArgumentException("cannot change DocValues type from " + currentDVType + " to " + dvType + " for field \"" + fieldName + "\"");
                        }
                    }
                    int fieldNumber;
                    if (!nameToNumber.TryGetValue(fieldName, out fieldNumber))
                    {
                        int preferredBoxed = preferredFieldNumber; // .NET port: no need to box here

                        if (preferredFieldNumber != -1 && !numberToName.ContainsKey(preferredBoxed))
                        {
                            // cool - we can use this number globally
                            fieldNumber = preferredBoxed;
                        }
                        else
                        {
                            // find a new FieldNumber
                            while (numberToName.ContainsKey(++lowestUnassignedFieldNumber))
                            {
                                // might not be up to date - lets do the work once needed
                            }
                            fieldNumber = lowestUnassignedFieldNumber;
                        }

                        numberToName[fieldNumber] = fieldName;
                        nameToNumber[fieldName] = fieldNumber;
                    }

                    return fieldNumber;
                }
            }

            internal bool ContainsConsistent(int number, string name, Index.FieldInfo.DocValuesType dvType)
            {
                lock (this)
                {
                    return name.Equals(numberToName[number])
                        && number.Equals(nameToNumber[name]) &&
                      (dvType == null || docValuesType[name] == null || dvType == docValuesType[name]);
                }
            }

            internal void Clear()
            {
                lock (this)
                {
                    numberToName.Clear();
                    nameToNumber.Clear();
                    docValuesType.Clear();
                }
            }
        }

        public sealed class Builder
        {
            private readonly HashMap<string, FieldInfo> byName = new HashMap<string, FieldInfo>();
            internal readonly FieldNumbers globalFieldNumbers;

            internal Builder()
                : this(new FieldNumbers())
            {
            }

            internal Builder(FieldNumbers globalFieldNumbers)
            {
                //assert globalFieldNumbers != null;
                this.globalFieldNumbers = globalFieldNumbers;
            }

            public void Add(FieldInfos other)
            {
                foreach (FieldInfo fieldInfo in other)
                {
                    Add(fieldInfo);
                }
            }

            public FieldInfo AddOrUpdate(String name, IIndexableFieldType fieldType)
            {
                // TODO: really, indexer shouldn't even call this
                // method (it's only called from DocFieldProcessor);
                // rather, each component in the chain should update
                // what it "owns".  EG fieldType.indexOptions() should
                // be updated by maybe FreqProxTermsWriterPerField:
                return AddOrUpdateInternal(name, -1, fieldType.Indexed, false,
                                           fieldType.OmitNorms, false,
                                           fieldType.IndexOptions, fieldType.DocValueType, null);
            }

            private FieldInfo AddOrUpdateInternal(String name, int preferredFieldNumber, bool isIndexed,
                bool storeTermVector, bool omitNorms, bool storePayloads, Index.FieldInfo.IndexOptions indexOptions, Index.FieldInfo.DocValuesType? docValues, Index.FieldInfo.DocValuesType? normType)
            {
                FieldInfo fi = FieldInfo(name);
                if (fi == null)
                {
                    // This field wasn't yet added to this in-RAM
                    // segment's FieldInfo, so now we get a global
                    // number for this field.  If the field was seen
                    // before then we'll get the same name and number,
                    // else we'll allocate a new one:
                    int fieldNumber = globalFieldNumbers.AddOrGet(name, preferredFieldNumber, docValues);
                    fi = new FieldInfo(name, isIndexed, fieldNumber, storeTermVector, omitNorms, storePayloads, indexOptions, docValues, normType, null);
                    //assert !byName.containsKey(fi.name);
                    //assert globalFieldNumbers.containsConsistent(Integer.valueOf(fi.number), fi.name, fi.getDocValuesType());
                    byName[fi.name] = fi;
                }
                else
                {
                    fi.Update(isIndexed, storeTermVector, omitNorms, storePayloads, indexOptions);

                    if (docValues != null)
                    {
                        fi.DocValuesTypeValue = docValues;
                    }

                    if (!fi.OmitsNorms && normType != null)
                    {
                        fi.NormType = normType;
                    }
                }
                return fi;
            }

            public FieldInfo Add(FieldInfo fi)
            {
                // IMPORTANT - reuse the field number if possible for consistent field numbers across segments
                return AddOrUpdateInternal(fi.name, fi.number, fi.IsIndexed, fi.HasVectors,
                           fi.OmitsNorms, fi.HasPayloads,
                           fi.IndexOptionsValue.GetValueOrDefault(), fi.DocValuesTypeValue.GetValueOrDefault(), fi.NormType);
            }

            public FieldInfo FieldInfo(String fieldName)
            {
                return byName[fieldName];
            }

            internal FieldInfos Finish()
            {
                return new FieldInfos(byName.Values.ToArray());
            }
        }
    }
}