using Lucene.Net.Util;
using System;
using System.Runtime.InteropServices;

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
    public class TopOrdAndSingleQueue : PriorityQueue<OrdAndValue<float>>
    {
        // LUCENENET specific - de-nested OrdAndValue and made it into a generic struct
        // so it can be used with this class and TopOrdAndInt32Queue

        /// <summary>
        /// Sole constructor.
        /// </summary>
        public TopOrdAndSingleQueue(int topN) : base(topN, false)
        {
        }

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
    [StructLayout(LayoutKind.Sequential)]
    public sealed class OrdAndValue<T> : IEquatable<OrdAndValue<T>>
    {
        /// <summary>
        /// Ordinal of the entry. </summary>
        public int Ord;

        /// <summary>
        /// Value associated with the ordinal. </summary>
        public T Value;

        /// <summary>
        /// Default constructor.
        /// </summary>
        public OrdAndValue(int ord, T value)
        {
            Ord = ord;
            Value = value;
        }

        #region Added for better .NET support
        public override bool Equals(object obj)
        {
            return obj is OrdAndValue<T> other && Equals(other);
        }

        public bool Equals(OrdAndValue<T> other)
        {
            return this.Ord == other.Ord && this.Value.Equals(other.Value);
        }

        public override int GetHashCode()
        {
            return this.Ord.GetHashCode() ^ this.Value.GetHashCode();
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