using Lucene.Net.Diagnostics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

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
    /// Holds updates of a single <see cref="DocValues"/> field, for a set of documents.
    /// <para/>
    /// @lucene.experimental
    /// </summary>
    internal abstract class DocValuesFieldUpdates
    {
        // LUCENENET specific: de-nested Type enum and renamed DocValuesFieldUpdatesType

        /// <summary>
        /// An iterator over documents and their updated values. Only documents with
        /// updates are returned by this iterator, and the documents are returned in
        /// increasing order.
        /// </summary>
        public abstract class Iterator
        {
            /// <summary>
            /// Returns the next document which has an update, or
            /// <see cref="Search.DocIdSetIterator.NO_MORE_DOCS"/> if there are no more documents to
            /// return.
            /// </summary>
            public abstract int NextDoc();

            /// <summary>
            /// Returns the current document this iterator is on. </summary>
            public abstract int Doc { get; }

            /// <summary>
            /// Returns the value of the document returned from <see cref="NextDoc()"/>. A
            /// <c>null</c> value means that it was unset for this document.
            /// </summary>
            public abstract object Value { get; }

            /// <summary>
            /// Reset the iterator's state. Should be called before <see cref="NextDoc()"/>
            /// and <see cref="Value"/>.
            /// </summary>
            public abstract void Reset();
        }

        public class Container
        {
            internal readonly IDictionary<string, NumericDocValuesFieldUpdates> numericDVUpdates = new Dictionary<string, NumericDocValuesFieldUpdates>();
            internal readonly IDictionary<string, BinaryDocValuesFieldUpdates> binaryDVUpdates = new Dictionary<string, BinaryDocValuesFieldUpdates>();

            internal virtual bool Any()
            {
                foreach (NumericDocValuesFieldUpdates updates in numericDVUpdates.Values)
                {
                    if (updates.Any())
                    {
                        return true;
                    }
                }
                foreach (BinaryDocValuesFieldUpdates updates in binaryDVUpdates.Values)
                {
                    if (updates.Any())
                    {
                        return true;
                    }
                }
                return false;
            }

            internal virtual int Count => numericDVUpdates.Count + binaryDVUpdates.Count; // LUCENENET NOTE: This was size() in Lucene.

            internal virtual DocValuesFieldUpdates GetUpdates(string field, DocValuesFieldUpdatesType type)
            {
                switch (type)
                {
                    case DocValuesFieldUpdatesType.NUMERIC:
                        NumericDocValuesFieldUpdates num;
                        numericDVUpdates.TryGetValue(field, out num);
                        return num;

                    case DocValuesFieldUpdatesType.BINARY:
                        BinaryDocValuesFieldUpdates bin;
                        binaryDVUpdates.TryGetValue(field, out bin);
                        return bin;

                    default:
                        throw new ArgumentException("unsupported type: " + type);
                }
            }

            internal virtual DocValuesFieldUpdates NewUpdates(string field, DocValuesFieldUpdatesType type, int maxDoc)
            {
                switch (type)
                {
                    case DocValuesFieldUpdatesType.NUMERIC:
                        NumericDocValuesFieldUpdates numericUpdates;
                        if (Debugging.AssertsEnabled && Debugging.ShouldAssert(!numericDVUpdates.ContainsKey(field))) Debugging.ThrowAssert();
                        numericUpdates = new NumericDocValuesFieldUpdates(field, maxDoc);
                        numericDVUpdates[field] = numericUpdates;
                        return numericUpdates;

                    case DocValuesFieldUpdatesType.BINARY:
                        BinaryDocValuesFieldUpdates binaryUpdates;
                        if (Debugging.AssertsEnabled && Debugging.ShouldAssert(!binaryDVUpdates.ContainsKey(field))) Debugging.ThrowAssert();
                        binaryUpdates = new BinaryDocValuesFieldUpdates(field, maxDoc);
                        binaryDVUpdates[field] = binaryUpdates;
                        return binaryUpdates;

                    default:
                        throw new ArgumentException("unsupported type: " + type);
                }
            }

            public override string ToString()
            {
                return "numericDVUpdates=" + string.Format(J2N.Text.StringFormatter.InvariantCulture, "{0}", numericDVUpdates) + 
                    " binaryDVUpdates=" + string.Format(J2N.Text.StringFormatter.InvariantCulture, "{0}", binaryDVUpdates);
            }
        }

        internal readonly string field;
        internal readonly DocValuesFieldUpdatesType type;

        protected internal DocValuesFieldUpdates(string field, DocValuesFieldUpdatesType type)
        {
            this.field = field;
            this.type = type;
        }

        /// <summary>
        /// Add an update to a document. For unsetting a value you should pass
        /// <c>null</c>.
        /// </summary>
        public abstract void Add(int doc, object value);

        /// <summary>
        /// Returns an <see cref="Iterator"/> over the updated documents and their
        /// values.
        /// </summary>
        public abstract Iterator GetIterator();

        /// <summary>
        /// Merge with another <see cref="DocValuesFieldUpdates"/>. this is called for a
        /// segment which received updates while it was being merged. The given updates
        /// should override whatever updates are in that instance.
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public abstract void Merge(DocValuesFieldUpdates other);

        /// <summary>
        /// Returns true if this instance contains any updates. </summary>
        /// <returns> TODO </returns>
        public abstract bool Any();
    }

    // LUCENENET specific - de-nested Type enumeration and renamed DocValuesFieldUpdatesType
    // primarily so it doesn't conflict with System.Type.
    public enum DocValuesFieldUpdatesType
    {
        NUMERIC,
        BINARY
    }
}