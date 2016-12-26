using Lucene.Net.Search.Spell;
using Lucene.Net.Store;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.IO;

namespace Lucene.Net.Search.Suggest
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
    /// Simple Lookup interface for <see cref="string"/> suggestions.
    /// @lucene.experimental
    /// </summary>
    public abstract class Lookup
    {

        /// <summary>
        /// Result of a lookup.
        /// @lucene.experimental
        /// </summary>
        public sealed class LookupResult : IComparable<LookupResult>
        {
            /// <summary>
            /// the key's text </summary>
            public readonly string key;

            /// <summary>
            /// Expert: custom Object to hold the result of a
            /// highlighted suggestion. 
            /// </summary>
            public readonly object highlightKey;

            /// <summary>
            /// the key's weight </summary>
            public readonly long value;

            /// <summary>
            /// the key's payload (null if not present) </summary>
            public readonly BytesRef payload;

            /// <summary>
            /// the key's contexts (null if not present) </summary>
            public readonly IEnumerable<BytesRef> contexts;

            /// <summary>
            /// Create a new result from a key+weight pair.
            /// </summary>
            public LookupResult(string key, long value)
                : this(key, null, value, null, null)
            {
            }

            /// <summary>
            /// Create a new result from a key+weight+payload triple.
            /// </summary>
            public LookupResult(string key, long value, BytesRef payload)
                : this(key, null, value, payload, null)
            {
            }

            /// <summary>
            /// Create a new result from a key+highlightKey+weight+payload triple.
            /// </summary>
            public LookupResult(string key, object highlightKey, long value, BytesRef payload)
                : this(key, highlightKey, value, payload, null)
            {
            }

            /// <summary>
            /// Create a new result from a key+weight+payload+contexts triple.
            /// </summary>
            public LookupResult(string key, long value, BytesRef payload, IEnumerable<BytesRef> contexts)
                : this(key, null, value, payload, contexts)
            {
            }

            /// <summary>
            /// Create a new result from a key+weight+contexts triple.
            /// </summary>
            public LookupResult(string key, long value, HashSet<BytesRef> contexts)
                : this(key, null, value, null, contexts)
            {
            }

            /// <summary>
            /// Create a new result from a key+highlightKey+weight+payload+contexts triple.
            /// </summary>
            public LookupResult(string key, object highlightKey, long value, BytesRef payload, IEnumerable<BytesRef> contexts)
            {
                this.key = key;
                this.highlightKey = highlightKey;
                this.value = value;
                this.payload = payload;
                this.contexts = contexts;
            }

            public override string ToString()
            {
                return key + "/" + value;
            }

            /// <summary>
            /// Compare alphabetically. </summary>
            public int CompareTo(LookupResult o)
            {
                return CHARSEQUENCE_COMPARATOR.Compare(key, o.key);
            }
        }

        /// <summary>
        /// A simple char-by-char comparator for <see cref="string"/>
        /// </summary>
        public static readonly IComparer<string> CHARSEQUENCE_COMPARATOR = new CharSequenceComparator();

        private class CharSequenceComparator : IComparer<string>
        {

            public virtual int Compare(string o1, string o2)
            {
                int l1 = o1.Length;
                int l2 = o2.Length;

                int aStop = Math.Min(l1, l2);
                for (int i = 0; i < aStop; i++)
                {
                    int diff = o1[i] - o2[i];
                    if (diff != 0)
                    {
                        return diff;
                    }
                }
                // One is a prefix of the other, or, they are equal:
                return l1 - l2;
            }

        }

        /// <summary>
        /// A <see cref="PriorityQueue{LookupResult}"/> collecting a fixed size of high priority <see cref="LookupResult"/>s.
        /// </summary>
        public sealed class LookupPriorityQueue : PriorityQueue<LookupResult>
        {
            // TODO: should we move this out of the interface into a utility class?
            /// <summary>
            /// Creates a new priority queue of the specified size.
            /// </summary>
            public LookupPriorityQueue(int size)
                : base(size)
            {
            }

            protected internal override bool LessThan(LookupResult a, LookupResult b)
            {
                return a.value < b.value;
            }

            /// <summary>
            /// Returns the top N results in descending order. </summary>
            /// <returns> the top N results in descending order. </returns>
            public LookupResult[] Results // LUCENENET TODO: Change to GetResults() (array)
            {
                get
                {
                    int size = Size();
                    var res = new LookupResult[size];
                    for (int i = size - 1; i >= 0; i--)
                    {
                        res[i] = Pop();
                    }
                    return res;
                }
            }
        }

        /// <summary>
        /// Sole constructor. (For invocation by subclass 
        /// constructors, typically implicit.)
        /// </summary>
        public Lookup()
        {
        }

        /// <summary>
        /// Build lookup from a dictionary. Some implementations may require sorted
        /// or unsorted keys from the dictionary's iterator - use
        /// <see cref="SortedInputIterator"/> or
        /// <see cref="UnsortedInputIterator"/> in such case.
        /// </summary>
        public virtual void Build(IDictionary dict)
        {
            Build(dict.EntryIterator);
        }

        /// <summary>
        /// Calls <see cref="Load(DataInput)"/> after converting
        /// <see cref="Stream"/> to <see cref="DataInput"/>
        /// </summary>
        public virtual bool Load(Stream input)
        {
            DataInput dataIn = new InputStreamDataInput(input);
            try
            {
                return Load(dataIn);
            }
            finally
            {
                IOUtils.Close(input);
            }
        }

        /// <summary>
        /// Calls <see cref="Store(DataOutput)"/> after converting
        /// <see cref="Stream"/> to <see cref="DataOutput"/>
        /// </summary>
        public virtual bool Store(Stream output)
        {
            DataOutput dataOut = new OutputStreamDataOutput(output);
            try
            {
                return Store(dataOut);
            }
            finally
            {
                IOUtils.Close(output);
            }
        }

        /// <summary>
        /// Get the number of entries the lookup was built with </summary>
        /// <returns> total number of suggester entries </returns>
        public abstract long Count { get; }

        /// <summary>
        /// Builds up a new internal <see cref="Lookup"/> representation based on the given <see cref="IInputIterator"/>.
        /// The implementation might re-sort the data internally.
        /// </summary>
        public abstract void Build(IInputIterator inputIterator);

        /// <summary>
        /// Look up a key and return possible completion for this key. </summary>
        /// <param name="key"> lookup key. Depending on the implementation this may be
        /// a prefix, misspelling, or even infix. </param>
        /// <param name="onlyMorePopular"> return only more popular results </param>
        /// <param name="num"> maximum number of results to return </param>
        /// <returns> a list of possible completions, with their relative weight (e.g. popularity) </returns>
        public virtual List<LookupResult> DoLookup(string key, bool onlyMorePopular, int num)
        {
            return DoLookup(key, null, onlyMorePopular, num);
        }

        /// <summary>
        /// Look up a key and return possible completion for this key. </summary>
        /// <param name="key"> lookup key. Depending on the implementation this may be
        /// a prefix, misspelling, or even infix. </param>
        /// <param name="contexts"> contexts to filter the lookup by, or null if all contexts are allowed; if the suggestion contains any of the contexts, it's a match </param>
        /// <param name="onlyMorePopular"> return only more popular results </param>
        /// <param name="num"> maximum number of results to return </param>
        /// <returns> a list of possible completions, with their relative weight (e.g. popularity) </returns>
        public abstract List<LookupResult> DoLookup(string key, IEnumerable<BytesRef> contexts, bool onlyMorePopular, int num);

        /// <summary>
        /// Persist the constructed lookup data to a directory. Optional operation. </summary>
        /// <param name="output"> <see cref="DataOutput"/> to write the data to. </param>
        /// <returns> true if successful, false if unsuccessful or not supported. </returns>
        /// <exception cref="System.IO.IOException"> when fatal IO error occurs. </exception>
        public abstract bool Store(DataOutput output);

        /// <summary>
        /// Discard current lookup data and load it from a previously saved copy.
        /// Optional operation. </summary>
        /// <param name="input"> the <see cref="DataInput"/> to load the lookup data. </param>
        /// <returns> true if completed successfully, false if unsuccessful or not supported. </returns>
        /// <exception cref="System.IO.IOException"> when fatal IO error occurs. </exception>
        public abstract bool Load(DataInput input);

        /// <summary>
        /// Get the size of the underlying lookup implementation in memory </summary>
        /// <returns> ram size of the lookup implementation in bytes </returns>
        public abstract long GetSizeInBytes();
    }
}