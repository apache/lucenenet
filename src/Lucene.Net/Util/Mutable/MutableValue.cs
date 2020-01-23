using J2N.Text;
using System;

namespace Lucene.Net.Util.Mutable
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
    /// Base class for all mutable values.
    /// <para/>
    /// @lucene.internal
    /// </summary>
    public abstract class MutableValue : IComparable<MutableValue>, IComparable
    {
        protected MutableValue()
        {
            Exists = true;
        }

        public bool Exists { get; set; }

        public abstract void Copy(MutableValue source);

        public abstract MutableValue Duplicate();

        public abstract bool EqualsSameType(object other);

        public abstract int CompareSameType(object other);

        public abstract object ToObject();

        public virtual int CompareTo(MutableValue other)
        {
            Type c1 = this.GetType();
            Type c2 = other.GetType();
            if (c1 != c2)
            {
                int c = c1.GetHashCode() - c2.GetHashCode();
                if (c == 0)
                {
                    c = c1.FullName.CompareToOrdinal(c2.FullName);
                }
                return c;
            }
            return CompareSameType(other);
        }


        // LUCENENET specific implementation, for use with FunctionFirstPassGroupingCollector 
        // (note that IComparable<T> does not inherit IComparable, so we need to explicitly 
        // implement here in order to support IComparable)
        public virtual int CompareTo(object other)
        {
            Type c1 = this.GetType();
            Type c2 = other.GetType();
            if (c1 != c2)
            {
                int c = c1.GetHashCode() - c2.GetHashCode();
                if (c == 0)
                {
                    c = c1.FullName.CompareToOrdinal(c2.FullName);
                }
                return c;
            }
            return CompareSameType(other);
        }

        public override bool Equals(object other)
        {
            return (this.GetType() == other.GetType()) && this.EqualsSameType(other);
        }

        public override abstract int GetHashCode();

        public override string ToString()
        {
            return Exists ? ToObject().ToString() : "(null)";
        }
    }
}