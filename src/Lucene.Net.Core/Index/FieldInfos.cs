using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

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
    /// Collection of <seealso cref="FieldInfo"/>s (accessible by number or by name).
    ///  @lucene.experimental
    /// </summary>
    public class FieldInfos : IEnumerable<FieldInfo>
    {
        private readonly bool hasFreq;
        private readonly bool hasProx;
        private readonly bool hasPayloads;
        private readonly bool hasOffsets;
        private readonly bool hasVectors;
        private readonly bool hasNorms;
        private readonly bool hasDocValues;

        private readonly SortedDictionary<int, FieldInfo> byNumber = new SortedDictionary<int, FieldInfo>();
        private readonly Dictionary<string, FieldInfo> byName = new Dictionary<string, FieldInfo>();
        private readonly ICollection<FieldInfo> values; // for an unmodifiable iterator

        /// <summary>
        /// Constructs a new FieldInfos from an array of FieldInfo objects
        /// </summary>
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
                if (info.Number < 0)
                {
                    throw new System.ArgumentException("illegal field number: " + info.Number + " for field " + info.Name);
                }

                FieldInfo previous;

                if (byNumber.TryGetValue(info.Number, out previous))
                {
                    throw new ArgumentException("duplicate field numbers: " + previous.Name + " and " + info.Name + " have: " + info.Number);
                }

                byNumber[info.Number] = info;

                if (byName.TryGetValue(info.Name, out previous))
                {
                    throw new ArgumentException("duplicate field names: " + previous.Number + " and " + info.Number + " have: " + info.Name);
                }

                byName[info.Name] = info;

                hasVectors |= info.HasVectors;
                hasProx |= info.IsIndexed && info.IndexOptions >= IndexOptions.DOCS_AND_FREQS_AND_POSITIONS;
                hasFreq |= info.IsIndexed && info.IndexOptions != IndexOptions.DOCS_ONLY;
                hasOffsets |= info.IsIndexed && info.IndexOptions >= IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS;
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

        /// <summary>
        /// Returns true if any fields have freqs </summary>
        public virtual bool HasFreq
        {
            get { return hasFreq; }
        }

        /// <summary>
        /// Returns true if any fields have positions </summary>
        public virtual bool HasProx
        {
            get { return hasProx; }
        }

        /// <summary>
        /// Returns true if any fields have payloads </summary>
        public virtual bool HasPayloads
        {
            get { return hasPayloads; }
        }

        /// <summary>
        /// Returns true if any fields have offsets </summary>
        public virtual bool HasOffsets
        {
            get { return hasOffsets; }
        }

        /// <summary>
        /// Returns true if any fields have vectors </summary>
        public virtual bool HasVectors
        {
            get { return hasVectors; }
        }

        /// <summary>
        /// Returns true if any fields have norms </summary>
        public virtual bool HasNorms
        {
            get { return hasNorms; }
        }

        /// <summary>
        /// Returns true if any fields have DocValues </summary>
        public virtual bool HasDocValues
        {
            get { return hasDocValues; }
        }

        /// <summary>
        /// Returns the number of fields </summary>
        public virtual int Size // LUCENENET TODO: rename Count
        {
            get
            {
                Debug.Assert(byNumber.Count == byName.Count);
                return byNumber.Count;
            }
        }

        /// <summary>
        /// Returns an iterator over all the fieldinfo objects present,
        /// ordered by ascending field number
        /// </summary>
        // TODO: what happens if in fact a different order is used?
        public virtual IEnumerator<FieldInfo> GetEnumerator()
        {
            return values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Return the fieldinfo object referenced by the field name </summary>
        /// <returns> the FieldInfo object or null when the given fieldName
        /// doesn't exist. </returns>
        public virtual FieldInfo FieldInfo(string fieldName)
        {
            FieldInfo ret;
            byName.TryGetValue(fieldName, out ret);
            return ret;
        }

        /// <summary>
        /// Return the fieldinfo object referenced by the fieldNumber. </summary>
        /// <param name="fieldNumber"> field's number. </param>
        /// <returns> the FieldInfo object or null when the given fieldNumber
        /// doesn't exist. </returns>
        /// <exception cref="IllegalArgumentException"> if fieldNumber is negative </exception>
        public virtual FieldInfo FieldInfo(int fieldNumber)
        {
            if (fieldNumber < 0)
            {
                throw new System.ArgumentException("Illegal field number: " + fieldNumber);
            }
            Index.FieldInfo ret;
            byNumber.TryGetValue(fieldNumber, out ret);
            return ret;
        }

        public sealed class FieldNumbers
        {
            private readonly IDictionary<int?, string> numberToName;
            private readonly IDictionary<string, int?> nameToNumber;

            // We use this to enforce that a given field never
            // changes DV type, even across segments / IndexWriter
            // sessions:
            private readonly IDictionary<string, DocValuesType?> docValuesType;

            // TODO: we should similarly catch an attempt to turn
            // norms back on after they were already ommitted; today
            // we silently discard the norm but this is badly trappy
            private int lowestUnassignedFieldNumber = -1;

            internal FieldNumbers()
            {
                this.nameToNumber = new Dictionary<string, int?>();
                this.numberToName = new Dictionary<int?, string>();
                this.docValuesType = new Dictionary<string, DocValuesType?>();
            }

            /// <summary>
            /// Returns the global field number for the given field name. If the name
            /// does not exist yet it tries to add it with the given preferred field
            /// number assigned if possible otherwise the first unassigned field number
            /// is used as the field number.
            /// </summary>
            internal int AddOrGet(string fieldName, int preferredFieldNumber, DocValuesType? dvType)
            {
                lock (this)
                {
                    if (dvType != null)
                    {
                        DocValuesType? currentDVType;
                        docValuesType.TryGetValue(fieldName, out currentDVType);
                        if (currentDVType == null)
                        {
                            docValuesType[fieldName] = dvType;
                        }
                        else if (currentDVType != null && currentDVType != dvType)
                        {
                            throw new System.ArgumentException("cannot change DocValues type from " + currentDVType + " to " + dvType + " for field \"" + fieldName + "\"");
                        }
                    }
                    int? fieldNumber;
                    nameToNumber.TryGetValue(fieldName, out fieldNumber);
                    if (fieldNumber == null)
                    {
                        int? preferredBoxed = Convert.ToInt32(preferredFieldNumber);

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

                    return (int)fieldNumber;
                }
            }

            // used by assert
            internal bool ContainsConsistent(int? number, string name, DocValuesType? dvType)
            {
                lock (this)
                {
                    string NumberToNameStr;
                    int? NameToNumberVal;
                    DocValuesType? DocValuesType_E;

                    numberToName.TryGetValue(number, out NumberToNameStr);
                    nameToNumber.TryGetValue(name, out NameToNumberVal);
                    docValuesType.TryGetValue(name, out DocValuesType_E);

                    return name.Equals(NumberToNameStr) && number.Equals(nameToNumber[name]) && (dvType == null || DocValuesType_E == null || dvType == DocValuesType_E);
                }
            }

            /// <summary>
            /// Returns true if the {@code fieldName} exists in the map and is of the
            /// same {@code dvType}.
            /// </summary>
            internal bool Contains(string fieldName, DocValuesType? dvType)
            {
                lock (this)
                {
                    // used by IndexWriter.updateNumericDocValue
                    if (!nameToNumber.ContainsKey(fieldName))
                    {
                        return false;
                    }
                    else
                    {
                        // only return true if the field has the same dvType as the requested one
                        DocValuesType? dvCand;
                        docValuesType.TryGetValue(fieldName, out dvCand);
                        return dvType == dvCand;
                    }
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

            internal void SetDocValuesType(int number, string name, DocValuesType? dvType)
            {
                lock (this)
                {
                    Debug.Assert(ContainsConsistent(number, name, dvType));
                    docValuesType[name] = dvType;
                }
            }
        }

        internal sealed class Builder
        {
            private readonly Dictionary<string, FieldInfo> byName = new Dictionary<string, FieldInfo>();
            private readonly FieldNumbers globalFieldNumbers;

            internal Builder()
                : this(new FieldNumbers())
            {
            }

            /// <summary>
            /// Creates a new instance with the given <seealso cref="FieldNumbers"/>.
            /// </summary>
            internal Builder(FieldNumbers globalFieldNumbers)
            {
                Debug.Assert(globalFieldNumbers != null);
                this.globalFieldNumbers = globalFieldNumbers;
            }

            public void Add(FieldInfos other)
            {
                foreach (FieldInfo fieldInfo in other)
                {
                    Add(fieldInfo);
                }
            }

            /// <summary>
            /// NOTE: this method does not carry over termVector
            ///  booleans nor docValuesType; the indexer chain
            ///  (TermVectorsConsumerPerField, DocFieldProcessor) must
            ///  set these fields when they succeed in consuming
            ///  the document
            /// </summary>
            public FieldInfo AddOrUpdate(string name, IndexableFieldType fieldType)
            {
                // TODO: really, indexer shouldn't even call this
                // method (it's only called from DocFieldProcessor);
                // rather, each component in the chain should update
                // what it "owns".  EG fieldType.indexOptions() should
                // be updated by maybe FreqProxTermsWriterPerField:
                return AddOrUpdateInternal(name, -1, fieldType.IsIndexed, false, fieldType.OmitNorms, false, fieldType.IndexOptions, fieldType.DocValueType, null);
            }

            private FieldInfo AddOrUpdateInternal(string name, int preferredFieldNumber, bool isIndexed, bool storeTermVector, bool omitNorms, bool storePayloads, IndexOptions? indexOptions, DocValuesType? docValues, DocValuesType? normType)
            {
                FieldInfo fi = FieldInfo(name);
                if (fi == null)
                {
                    // this field wasn't yet added to this in-RAM
                    // segment's FieldInfo, so now we get a global
                    // number for this field.  If the field was seen
                    // before then we'll get the same name and number,
                    // else we'll allocate a new one:
                    int fieldNumber = globalFieldNumbers.AddOrGet(name, preferredFieldNumber, docValues);
                    fi = new FieldInfo(name, isIndexed, fieldNumber, storeTermVector, omitNorms, storePayloads, indexOptions, docValues, normType, null);
                    Debug.Assert(!byName.ContainsKey(fi.Name));
                    Debug.Assert(globalFieldNumbers.ContainsConsistent(Convert.ToInt32(fi.Number), fi.Name, fi.DocValuesType));
                    byName[fi.Name] = fi;
                }
                else
                {
                    fi.Update(isIndexed, storeTermVector, omitNorms, storePayloads, indexOptions);

                    if (docValues != null)
                    {
                        // only pay the synchronization cost if fi does not already have a DVType
                        bool updateGlobal = !fi.HasDocValues;
                        fi.DocValuesType = docValues; // this will also perform the consistency check.
                        if (updateGlobal)
                        {
                            // must also update docValuesType map so it's
                            // aware of this field's DocValueType
                            globalFieldNumbers.SetDocValuesType(fi.Number, name, docValues);
                        }
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
                return AddOrUpdateInternal(fi.Name, fi.Number, fi.IsIndexed, fi.HasVectors, fi.OmitsNorms, fi.HasPayloads, fi.IndexOptions, fi.DocValuesType, fi.NormType);
            }

            public FieldInfo FieldInfo(string fieldName)
            {
                FieldInfo ret;
                byName.TryGetValue(fieldName, out ret);
                return ret;
            }

            public FieldInfos Finish()
            {
                return new FieldInfos(byName.Values.ToArray());
            }
        }
    }
}