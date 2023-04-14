// Lucene version compatibility level 4.8.1
using Lucene.Net.Util;
using System;
using System.Runtime.InteropServices;
#nullable enable

namespace Lucene.Net.Facet
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
    /// Keeps highest results, first by largest <see cref="float"/> value,
    /// then tie break by smallest ord.
    /// <para/>
    /// NOTE: This was TopOrdAndFloatQueue in Lucene
    /// </summary>

    // LUCENENET NOTE: Keeping this around because it is public. Although,
    // we don't use it internally anymore, we use TopOrdAndSingleComparer
    // with ValuePriorityQueue instead.
    public class TopOrdAndSingleQueue : PriorityQueue<OrdAndValue<float>>
    {
        // LUCENENET specific - de-nested OrdAndValue and made it into a generic struct
        // so it can be used with this class and TopOrdAndInt32Queue

        /// <summary>
        /// Initializes a new instance of <see cref="TopOrdAndSingleQueue"/> with the
        /// specified <paramref name="topN"/> size.
        /// </summary>
        public TopOrdAndSingleQueue(int topN) : base(topN) // LUCENENET NOTE: Doesn't pre-populate because sentinelFactory is null
        {
        }

        protected internal override bool LessThan(OrdAndValue<float> a, OrdAndValue<float> b)
            => TopOrdAndSingleComparer.Default.LessThan(a, b);
    }

    /// <summary>
    /// Keeps highest results, first by largest <see cref="float"/> value,
    /// then tie break by smallest ord.
    /// <para/>
    /// NOTE: This is a refactoring of TopOrdAndFloatQueue in Lucene
    /// </summary>
    // LUCENENET: Refactored PriorityQueue<T> subclass into PriorityComparer<T>
    // implementation, which can be passed into ValuePriorityQueue.
    public sealed class TopOrdAndSingleComparer : PriorityComparer<OrdAndValue<float>>
    {
        /// <summary>
        /// Returns a default sort order comparer for <see cref="OrdAndValue{Single}"/>.
        /// Keeps highest results, first by largest <see cref="float"/> value,
        /// then tie break by smallest ord.
        /// </summary>
        public static TopOrdAndSingleComparer Default { get; } = new TopOrdAndSingleComparer();

        protected internal override bool LessThan(OrdAndValue<float> a, OrdAndValue<float> b)
        {
            if (a.Value < b.Value)
            {
                return true;
            }
            else if (a.Value > b.Value)
            {
                return false;
            }
            else
            {
                return a.Ord > b.Ord;
            }
        }
    }

    /// <summary>
    /// Holds a single entry.
    /// </summary>
    // LUCENENET specific - de-nested this and made it into a struct so it can be shared
    // and stack allocated.
    [StructLayout(LayoutKind.Sequential)]
    public struct OrdAndValue<T> : IEquatable<OrdAndValue<T>>
        where T : struct
    {
        private int ord;
        private T value;

        /// <summary>
        /// Ordinal of the entry. </summary>
        public int Ord => ord;

        /// <summary>
        /// Value associated with the ordinal. </summary>
        public T Value => value;

        /// <summary>
        /// Default constructor.
        /// </summary>
        public OrdAndValue(int ord, T value)
        {
            this.ord = ord;
            this.value = value;
        }

        #region Added for better .NET support
        public override bool Equals(object? obj)
        {
            return obj is OrdAndValue<T> other && Equals(other);
        }

        public bool Equals(OrdAndValue<T> other)
        {
            return this.ord == other.ord && this.value.Equals(other.value);
        }

        public override int GetHashCode()
        {
            return this.ord.GetHashCode() ^ this.value.GetHashCode();
        }

        public static bool operator ==(OrdAndValue<T> left, OrdAndValue<T> right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(OrdAndValue<T> left, OrdAndValue<T> right)
        {
            return !(left == right);
        }
        #endregion
    }
}