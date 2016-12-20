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
        public interface Iterator // LUCENENET TODO: Rename with "I" (or should this be renamed to IEnumerator?)
        {
            /// <summary>
            /// Returns the next document which has an update, or
            /// <seealso cref="DocIdSetIterator#NO_MORE_DOCS"/> if there are no more documents to
            /// return.
            /// </summary>
            int NextDoc();

            /// <summary>
            /// Returns the current document this iterator is on. </summary>
            int Doc(); // LUCENENET TODO: Make property

            /// <summary>
            /// Returns the value of the document returned from <seealso cref="#nextDoc()"/>. A
            /// {@code null} value means that it was unset for this document.
            /// </summary>
            object Value(); // LUCENENET TODO: Make property

            /// <summary>
            /// Reset the iterator's state. Should be called before <seealso cref="#nextDoc()"/>
            /// and <seealso cref="#value()"/>.
            /// </summary>
            void Reset();
        }

        public class Container
        {
            internal readonly IDictionary<string, NumericDocValuesFieldUpdates> NumericDVUpdates = new Dictionary<string, NumericDocValuesFieldUpdates>();
            internal readonly IDictionary<string, BinaryDocValuesFieldUpdates> BinaryDVUpdates = new Dictionary<string, BinaryDocValuesFieldUpdates>();

            internal virtual bool Any()
            {
                foreach (NumericDocValuesFieldUpdates updates in NumericDVUpdates.Values)
                {
                    if (updates.Any())
                    {
                        return true;
                    }
                }
                foreach (BinaryDocValuesFieldUpdates updates in BinaryDVUpdates.Values)
                {
                    if (updates.Any())
                    {
                        return true;
                    }
                }
                return false;
            }

            internal virtual int Size() // LUCENENET TODO: Rename Count property
            {
                return NumericDVUpdates.Count + BinaryDVUpdates.Count;
            }

            internal virtual AbstractDocValuesFieldUpdates GetUpdates(string field, DocValuesFieldUpdates.Type_e type)
            {
                switch (type)
                {
                    case DocValuesFieldUpdates.Type_e.NUMERIC:
                        NumericDocValuesFieldUpdates num;
                        NumericDVUpdates.TryGetValue(field, out num);
                        return num;

                    case DocValuesFieldUpdates.Type_e.BINARY:
                        BinaryDocValuesFieldUpdates bin;
                        BinaryDVUpdates.TryGetValue(field, out bin);
                        return bin;

                    default:
                        throw new System.ArgumentException("unsupported type: " + type);
                }
            }

            internal virtual AbstractDocValuesFieldUpdates NewUpdates(string field, DocValuesFieldUpdates.Type_e type, int maxDoc)
            {
                switch (type)
                {
                    case DocValuesFieldUpdates.Type_e.NUMERIC:
                        NumericDocValuesFieldUpdates numericUpdates;
                        Debug.Assert(!NumericDVUpdates.TryGetValue(field, out numericUpdates));
                        numericUpdates = new NumericDocValuesFieldUpdates(field, maxDoc);
                        NumericDVUpdates[field] = numericUpdates;
                        return numericUpdates;

                    case DocValuesFieldUpdates.Type_e.BINARY:
                        BinaryDocValuesFieldUpdates binaryUpdates;
                        Debug.Assert(!BinaryDVUpdates.TryGetValue(field, out binaryUpdates));
                        binaryUpdates = new BinaryDocValuesFieldUpdates(field, maxDoc);
                        BinaryDVUpdates[field] = binaryUpdates;
                        return binaryUpdates;

                    default:
                        throw new System.ArgumentException("unsupported type: " + type);
                }
            }

            public override string ToString()
            {
                return "numericDVUpdates=" + NumericDVUpdates + " binaryDVUpdates=" + BinaryDVUpdates;
            }
        }

        internal readonly string Field;
        internal readonly DocValuesFieldUpdates.Type_e Type;

        protected internal AbstractDocValuesFieldUpdates(string field, DocValuesFieldUpdates.Type_e type)
        {
            this.Field = field;
            this.Type = type;
        }

        /// <summary>
        /// Add an update to a document. For unsetting a value you should pass
        /// {@code null}.
        /// </summary>
        public abstract void Add(int doc, object value);

        /// <summary>
        /// Returns an <seealso cref="Iterator"/> over the updated documents and their
        /// values.
        /// </summary>
        public abstract Iterator GetIterator(); // LUCENENET TODO: Rename Iterator()? Check consistency across API

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

    public class DocValuesFieldUpdates
    {
        private DocValuesFieldUpdates() { } // Disallow creation

        public enum Type_e // LUCENENET TODO: Rename Type
        {
            NUMERIC,
            BINARY
        }
    }
}