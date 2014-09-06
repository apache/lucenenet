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

namespace Lucene.Net.Util.Mutable
{
    /// <summary>
    /// <seealso cref="MutableValue"/> implementation of type
    /// <code>int</code>.
    /// </summary>
    public class MutableValueInt : MutableValue
    {
        public int Value;

        public override object ToObject()
        {
            return Exists ? (object)Value : null;
        }

        public override void Copy(MutableValue source)
        {
            MutableValueInt s = (MutableValueInt)source;
            Value = s.Value;
            Exists = s.Exists;
        }

        public override MutableValue Duplicate()
        {
            MutableValueInt v = new MutableValueInt();
            v.Value = this.Value;
            v.Exists = this.Exists;
            return v;
        }

        public override bool EqualsSameType(object other)
        {
            MutableValueInt b = (MutableValueInt)other;
            return Value == b.Value && Exists == b.Exists;
        }

        public override int CompareSameType(object other)
        {
            MutableValueInt b = (MutableValueInt)other;
            int ai = Value;
            int bi = b.Value;
            if (ai < bi)
            {
                return -1;
            }
            else if (ai > bi)
            {
                return 1;
            }

            if (Exists == b.Exists)
            {
                return 0;
            }
            return Exists ? 1 : -1;
        }

        public override int GetHashCode()
        {
            // TODO: if used in HashMap, it already mixes the value... maybe use a straight value?
            return (Value >> 8) + (Value >> 16);
        }
    }
}