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
    /// Holds updates of a single DocValues field, for a set of documents.
    ///
    /// @lucene.experimental
    /// </summary>
    internal abstract class AbstractDocValuesFieldUpdates // LUCENENET specific - Added Abstract prefix for the internal type
    {
        // LUCENENET specific: moved Type enum to new class named DocValuesFieldUpdates (the original name of this class)

        /// <summary>
        /// An iterator over documents and their updated values. Only documents with
        /// updates are returned by this iterator, and the documents are returned in
        /// increasing order.
        /// </summary>
        public interface IIterator // LUCENENET TODO: should this be renamed to IEnumerator?
        {
            // LUCENENET TODO: This was an abstract class in the original source (and internal)
            // Should we make this an abstract classs here? And should it be public (and nested in DocValuesFieldUpdates)?

            /// <summary>
            /// Returns the next document which has an update, or
            /// <seealso cref="DocIdSetIterator#NO_MORE_DOCS"/> if there are no more documents to
            /// return.
            /// </summary>
            int NextDoc();

            /// <summary>
            /// Returns the current document this iterator is on. </summary>
            int Doc { get; }

            /// <summary>
            /// Returns the value of the document returned from <seealso cref="#nextDoc()"/>. A
            /// {@code null} value means that it was unset for this document.
            /// </summary>
            object Value { get; } // LUCENENET TODO: Should this interface be made generic?

            /// <summary>
            /// Reset the iterator's state. Should be called before <seealso cref="#nextDoc()"/>
            /// and <seealso cref="#value()"/>.
            /// </summary>
            void Reset();
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

            internal virtual int Count // LUCENENET NOTE: This was size() in Lucene.
            {
                get { return numericDVUpdates.Count + binaryDVUpdates.Count; }
            }

            internal virtual AbstractDocValuesFieldUpdates GetUpdates(string field, DocValuesFieldUpdates.Type type)
            {
                switch (type)
                {
                    case DocValuesFieldUpdates.Type.NUMERIC:
                        NumericDocValuesFieldUpdates num;
                        numericDVUpdates.TryGetValue(field, out num);
                        return num;

                    case DocValuesFieldUpdates.Type.BINARY:
                        BinaryDocValuesFieldUpdates bin;
                        binaryDVUpdates.TryGetValue(field, out bin);
                        return bin;

                    default:
                        throw new System.ArgumentException("unsupported type: " + type);
                }
            }

            internal virtual AbstractDocValuesFieldUpdates NewUpdates(string field, DocValuesFieldUpdates.Type type, int maxDoc)
            {
                switch (type)
                {
                    case DocValuesFieldUpdates.Type.NUMERIC:
                        NumericDocValuesFieldUpdates numericUpdates;
                        Debug.Assert(!numericDVUpdates.TryGetValue(field, out numericUpdates));
                        numericUpdates = new NumericDocValuesFieldUpdates(field, maxDoc);
                        numericDVUpdates[field] = numericUpdates;
                        return numericUpdates;

                    case DocValuesFieldUpdates.Type.BINARY:
                        BinaryDocValuesFieldUpdates binaryUpdates;
                        Debug.Assert(!binaryDVUpdates.TryGetValue(field, out binaryUpdates));
                        binaryUpdates = new BinaryDocValuesFieldUpdates(field, maxDoc);
                        binaryDVUpdates[field] = binaryUpdates;
                        return binaryUpdates;

                    default:
                        throw new System.ArgumentException("unsupported type: " + type);
                }
            }

            public override string ToString()
            {
                return "numericDVUpdates=" + numericDVUpdates + " binaryDVUpdates=" + binaryDVUpdates;
            }
        }

        internal readonly string field;
        internal readonly DocValuesFieldUpdates.Type type;

        protected internal AbstractDocValuesFieldUpdates(string field, DocValuesFieldUpdates.Type type)
        {
            this.field = field;
            this.type = type;
        }

        /// <summary>
        /// Add an update to a document. For unsetting a value you should pass
        /// {@code null}.
        /// </summary>
        public abstract void Add(int doc, object value);

        /// <summary>
        /// Returns an <seealso cref="IIterator"/> over the updated documents and their
        /// values.
        /// </summary>
        public abstract IIterator GetIterator();

        /// <summary>
        /// Merge with another <seealso cref="AbstractDocValuesFieldUpdates"/>. this is called for a
        /// segment which received updates while it was being merged. The given updates
        /// should override whatever updates are in that instance.
        /// </summary>
        public abstract void Merge(AbstractDocValuesFieldUpdates other);

        /// <summary>
        /// Returns true if this instance contains any updates. </summary>
        /// <returns> TODO </returns>
        public abstract bool Any();
    }

    // LUCENENET specific class used to nest Type enumeration into the correct place
    // primarily so it doesn't conflict with System.Type.
    public class DocValuesFieldUpdates
    {
        private DocValuesFieldUpdates() { } // Disallow creation

        public enum Type
        {
            NUMERIC,
            BINARY
        }
    }
}