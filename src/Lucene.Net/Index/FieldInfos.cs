using J2N.Collections.Generic.Extensions;
using Lucene.Net.Diagnostics;
using Lucene.Net.Support.Threading;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using JCG = J2N.Collections.Generic;

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
    /// Collection of <see cref="Index.FieldInfo"/>s (accessible by number or by name).
    /// <para/>
    /// @lucene.experimental
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

        private readonly IDictionary<int, FieldInfo> byNumber = new JCG.SortedDictionary<int, FieldInfo>();
        private readonly IDictionary<string, FieldInfo> byName = new JCG.Dictionary<string, FieldInfo>();
        private readonly ICollection<FieldInfo> values; // for an unmodifiable iterator

        /// <summary>
        /// Constructs a new <see cref="FieldInfos"/> from an array of <see cref="Index.FieldInfo"/> objects
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
                    throw new ArgumentException("illegal field number: " + info.Number + " for field " + info.Name);
                }

                if (byNumber.TryGetValue(info.Number, out FieldInfo previous))
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
                // LUCENENET specific - to avoid boxing, changed from CompareTo() to IndexOptionsComparer.Compare()
                hasProx |= info.IsIndexed && IndexOptionsComparer.Default.Compare(info.IndexOptions, IndexOptions.DOCS_AND_FREQS_AND_POSITIONS) >= 0;
                hasFreq |= info.IsIndexed && info.IndexOptions != IndexOptions.DOCS_ONLY;
                hasOffsets |= info.IsIndexed && IndexOptionsComparer.Default.Compare(info.IndexOptions, IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS) >= 0;
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
        /// Returns <c>true</c> if any fields have freqs </summary>
        public virtual bool HasFreq => hasFreq;

        /// <summary>
        /// Returns <c>true</c> if any fields have positions </summary>
        public virtual bool HasProx => hasProx;

        /// <summary>
        /// Returns <c>true</c> if any fields have payloads </summary>
        public virtual bool HasPayloads => hasPayloads;

        /// <summary>
        /// Returns <c>true</c> if any fields have offsets </summary>
        public virtual bool HasOffsets => hasOffsets;

        /// <summary>
        /// Returns <c>true</c> if any fields have vectors </summary>
        public virtual bool HasVectors => hasVectors;

        /// <summary>
        /// Returns <c>true</c> if any fields have norms </summary>
        public virtual bool HasNorms => hasNorms;

        /// <summary>
        /// Returns <c>true</c> if any fields have <see cref="DocValues"/> </summary>
        public virtual bool HasDocValues => hasDocValues;

        /// <summary>
        /// Returns the number of fields.
        /// <para/>
        /// NOTE: This was size() in Lucene.
        /// </summary>
        public virtual int Count
        {
            get
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(byNumber.Count == byName.Count);
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
        /// Return the <see cref="Index.FieldInfo"/> object referenced by the <paramref name="fieldName"/> </summary>
        /// <returns> the <see cref="Index.FieldInfo"/> object or <c>null</c> when the given <paramref name="fieldName"/>
        /// doesn't exist. </returns>
        public virtual FieldInfo FieldInfo(string fieldName)
        {
            byName.TryGetValue(fieldName, out FieldInfo ret);
            return ret;
        }

        /// <summary>
        /// Return the <see cref="Index.FieldInfo"/> object referenced by the <paramref name="fieldNumber"/>. </summary>
        /// <param name="fieldNumber"> field's number. </param>
        /// <returns> the <see cref="Index.FieldInfo"/> object or null when the given <paramref name="fieldNumber"/>
        /// doesn't exist. </returns>
        /// <exception cref="ArgumentException"> if <paramref name="fieldNumber"/> is negative </exception>
        public virtual FieldInfo FieldInfo(int fieldNumber)
        {
            if (fieldNumber < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(fieldNumber), "Illegal field number: " + fieldNumber); // LUCENENET specific - changed from IllegalArgumentException to ArgumentOutOfRangeException (.NET convention)
            }
            byNumber.TryGetValue(fieldNumber, out FieldInfo ret);
            return ret;
        }

        internal sealed class FieldNumbers
        {
            private readonly IDictionary<int, string> numberToName;
            private readonly IDictionary<string, int> nameToNumber;

            // We use this to enforce that a given field never
            // changes DV type, even across segments / IndexWriter
            // sessions:
            private readonly IDictionary<string, DocValuesType> docValuesType;

            // TODO: we should similarly catch an attempt to turn
            // norms back on after they were already ommitted; today
            // we silently discard the norm but this is badly trappy
            private int lowestUnassignedFieldNumber = -1;

            internal FieldNumbers()
            {
                this.nameToNumber = new Dictionary<string, int>();
                this.numberToName = new Dictionary<int, string>();
                this.docValuesType = new Dictionary<string, DocValuesType>();
            }

            /// <summary>
            /// Returns the global field number for the given field name. If the name
            /// does not exist yet it tries to add it with the given preferred field
            /// number assigned if possible otherwise the first unassigned field number
            /// is used as the field number.
            /// </summary>
            internal int AddOrGet(string fieldName, int preferredFieldNumber, DocValuesType dvType)
            {
                UninterruptableMonitor.Enter(this);
                try
                {
                    if (dvType != DocValuesType.NONE)
                    {
                        if (!docValuesType.TryGetValue(fieldName, out DocValuesType currentDVType) || currentDVType == DocValuesType.NONE) // default value in .NET (value type 0)
                        {
                            docValuesType[fieldName] = dvType;
                        }
                        else if (currentDVType != DocValuesType.NONE && currentDVType != dvType)
                        {
                            throw new ArgumentException("cannot change DocValues type from " + currentDVType + " to " + dvType + " for field \"" + fieldName + "\"");
                        }
                    }
                    if (!nameToNumber.TryGetValue(fieldName, out int fieldNumber))
                    {
                        int preferredBoxed = preferredFieldNumber;

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
                finally
                {
                    UninterruptableMonitor.Exit(this);
                }
            }

            // used by assert
            internal bool ContainsConsistent(int number, string name, DocValuesType dvType)
            {
                UninterruptableMonitor.Enter(this);
                try
                {
                    numberToName.TryGetValue(number, out string numberToNameStr);
                    nameToNumber.TryGetValue(name, out int nameToNumberVal);
                    this.docValuesType.TryGetValue(name, out DocValuesType docValuesType);

                    return name.Equals(numberToNameStr, StringComparison.Ordinal) 
                        && number.Equals(nameToNumber[name]) && 
                        (dvType == DocValuesType.NONE || docValuesType == DocValuesType.NONE || dvType == docValuesType);
                }
                finally
                {
                    UninterruptableMonitor.Exit(this);
                }
            }

            /// <summary>
            /// Returns <c>true</c> if the <paramref name="fieldName"/> exists in the map and is of the
            /// same <paramref name="dvType"/>.
            /// </summary>
            internal bool Contains(string fieldName, DocValuesType dvType)
            {
                UninterruptableMonitor.Enter(this);
                try
                {
                    // used by IndexWriter.updateNumericDocValue
                    if (!nameToNumber.ContainsKey(fieldName))
                    {
                        return false;
                    }
                    else
                    {
                        // only return true if the field has the same dvType as the requested one
                        docValuesType.TryGetValue(fieldName, out DocValuesType dvCand); // LUCENENET NOTE: This could be NONE even if TryGetValue returns false
                        return dvType == dvCand;
                    }
                }
                finally
                {
                    UninterruptableMonitor.Exit(this);
                }
            }

            internal void Clear()
            {
                UninterruptableMonitor.Enter(this);
                try
                {
                    numberToName.Clear();
                    nameToNumber.Clear();
                    docValuesType.Clear();
                }
                finally
                {
                    UninterruptableMonitor.Exit(this);
                }
            }

            internal void SetDocValuesType(int number, string name, DocValuesType dvType)
            {
                UninterruptableMonitor.Enter(this);
                try
                {
                    if (Debugging.AssertsEnabled) Debugging.Assert(ContainsConsistent(number, name, dvType));
                    docValuesType[name] = dvType;
                }
                finally
                {
                    UninterruptableMonitor.Exit(this);
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
            /// Creates a new instance with the given <see cref="FieldNumbers"/>.
            /// </summary>
            internal Builder(FieldNumbers globalFieldNumbers)
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(globalFieldNumbers != null);
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
            /// booleans nor docValuesType; the indexer chain
            /// (TermVectorsConsumerPerField, DocFieldProcessor) must
            /// set these fields when they succeed in consuming
            /// the document
            /// </summary>
            public FieldInfo AddOrUpdate(string name, IIndexableFieldType fieldType)
            {
                // TODO: really, indexer shouldn't even call this
                // method (it's only called from DocFieldProcessor);
                // rather, each component in the chain should update
                // what it "owns".  EG fieldType.indexOptions() should
                // be updated by maybe FreqProxTermsWriterPerField:
                return AddOrUpdateInternal(name, -1, fieldType.IsIndexed, false, fieldType.OmitNorms, false, fieldType.IndexOptions, fieldType.DocValueType, DocValuesType.NONE);
            }

            private FieldInfo AddOrUpdateInternal(string name, int preferredFieldNumber, bool isIndexed, bool storeTermVector, bool omitNorms, bool storePayloads, IndexOptions indexOptions, DocValuesType docValues, DocValuesType normType)
            {
                // LUCENENET: Bypass FieldInfo method so we can access the quick boolean check
                if (!TryGetFieldInfo(name, out FieldInfo fi) || fi is null)
                {
                    // this field wasn't yet added to this in-RAM
                    // segment's FieldInfo, so now we get a global
                    // number for this field.  If the field was seen
                    // before then we'll get the same name and number,
                    // else we'll allocate a new one:
                    int fieldNumber = globalFieldNumbers.AddOrGet(name, preferredFieldNumber, docValues);
                    fi = new FieldInfo(name, isIndexed, fieldNumber, storeTermVector, omitNorms, storePayloads, indexOptions, docValues, normType, null);
                    if (Debugging.AssertsEnabled)
                    {
                        Debugging.Assert(!byName.ContainsKey(fi.Name));
                        Debugging.Assert(globalFieldNumbers.ContainsConsistent(fi.Number, fi.Name, fi.DocValuesType));
                    }
                    byName[fi.Name] = fi;
                }
                else
                {
                    fi.Update(isIndexed, storeTermVector, omitNorms, storePayloads, indexOptions);

                    if (docValues != DocValuesType.NONE)
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

                    if (!fi.OmitsNorms && normType != DocValuesType.NONE)
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

            public bool TryGetFieldInfo(string fieldName, out FieldInfo ret) // LUCENENET specific - changed from FieldInfo to TryGetFieldInfo
            {
                return byName.TryGetValue(fieldName, out ret);
            }

            public FieldInfos Finish()
            {
                return new FieldInfos(byName.Values.ToArray());
            }
        }
    }
}