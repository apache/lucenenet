/*
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 * 
 * http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Collections;

namespace Lucene.Net.Support
{
    /// <summary>
    /// A Hashtable which holds weak references to its keys so they
    /// can be collected during GC. 
    /// </summary>
    [System.Diagnostics.DebuggerDisplay("Count = {Values.Count}")]
    public class WeakHashTable : Hashtable, IEnumerable
    {
        /// <summary>
        /// A weak referene wrapper for the hashtable keys. Whenever a key\value pair 
        /// is added to the hashtable, the key is wrapped using a WeakKey. WeakKey saves the
        /// value of the original object hashcode for fast comparison.
        /// </summary>
        class WeakKey 
        {
            WeakReference reference;
            int hashCode;

            public WeakKey(object key)
            {
                if (key == null)
                    throw new ArgumentNullException("key");

                hashCode = key.GetHashCode();
                reference = new WeakReference(key);
            }

            public override int GetHashCode()
            {
                return hashCode;
            }

            public object Target
            {
                get { return reference.Target; }
            }

            public bool IsAlive
            {
                get { return reference.IsAlive; }
            }
        }

        /// <summary>
        /// A Dictionary enumerator which wraps the original hashtable enumerator 
        /// and performs 2 tasks: Extract the real key from a WeakKey and skip keys
        /// that were already collected.
        /// </summary>
        class WeakDictionaryEnumerator : IDictionaryEnumerator
        {
            IDictionaryEnumerator baseEnumerator;
            object currentKey;
            object currentValue;

            public WeakDictionaryEnumerator(IDictionaryEnumerator baseEnumerator)
            {
                this.baseEnumerator = baseEnumerator;
            }

            public DictionaryEntry Entry
            {
                get
                {
                    return new DictionaryEntry(this.currentKey, this.currentValue);
                }
            }

            public object Key
            {
                get
                {
                    return this.currentKey;
                }
            }

            public object Value
            {
                get
                {
                    return this.currentValue;
                }
            }

            public object Current
            {
                get
                {
                    return Entry;
                }
            }

            public bool MoveNext()
            {
                while (baseEnumerator.MoveNext())
                {
                    object key = ((WeakKey)baseEnumerator.Key).Target;
                    if (key != null)
                    {
                        this.currentKey = key;
                        this.currentValue = baseEnumerator.Value;
                        return true;
                    }
                }
                return false;
            }

            public void Reset()
            {
                baseEnumerator.Reset();
                this.currentKey = null;
                this.currentValue = null;
            }
        }


        /// <summary>
        /// Serves as a simple "GC Monitor" that indicates whether cleanup is needed. 
        /// If collectableObject.IsAlive is false, GC has occurred and we should perform cleanup
        /// </summary>
        WeakReference collectableObject = new WeakReference(new Object());

        /// <summary>
        /// Customize the hashtable lookup process by overriding KeyEquals. KeyEquals
        /// will compare both WeakKey to WeakKey and WeakKey to real keys
        /// </summary>
        protected override bool KeyEquals(object x, object y)
        {
            if (x == y)
                return true;

            if (x is WeakKey)
            {
                x = ((WeakKey)x).Target;
                if (x == null)
                    return false;
            }

            if (y is WeakKey)
            {
                y = ((WeakKey)y).Target;
                if (y == null)
                    return false;
            }

            return x.Equals(y);
        }

        protected override int GetHash(object key)
        {
            return key.GetHashCode();
        }

        /// <summary>
        /// Perform cleanup if GC occurred
        /// </summary>
        private void CleanIfNeeded()
        {
            if (collectableObject.Target == null)
            {
                Clean();
                collectableObject = new WeakReference(new Object());
            }
        }

        /// <summary>
        /// Iterate over all keys and remove keys that were collected
        /// </summary>
        private void Clean()
        {
            foreach (WeakKey wtk in ((Hashtable)base.Clone()).Keys)
            {
                if (!wtk.IsAlive)
                {
                    Remove(wtk);
                }
            }
        }


        /// <summary>
        /// Wrap each key with a WeakKey and add it to the hashtable
        /// </summary>
        public override void Add(object key, object value)
        {
            CleanIfNeeded();
            base.Add(new WeakKey(key), value);
        }

        public override IDictionaryEnumerator GetEnumerator()
        {
            Hashtable tmp = null;
            tmp = (Hashtable)base.Clone();
            return new WeakDictionaryEnumerator(tmp.GetEnumerator());
        }

        /// <summary>
        /// Create a temporary copy of the real keys and return that
        /// </summary>
        public override ICollection Keys
        {
            get
            {
                ArrayList keys = new ArrayList(Count);
                Hashtable tmpTable = (Hashtable)base.Clone();
                
                foreach (WeakKey key in tmpTable.Keys)
                {
                    object realKey = key.Target;
                    if (realKey != null)
                        keys.Add(realKey);
                }
                
                return keys;
            }
        }

        public override object this[object key]
        {
            get
            {
                return base[key];
            }
            set
            {
                CleanIfNeeded();
                base[new WeakKey(key)] = value;
            }
        }

        public override void CopyTo(Array array, int index)
        {
            int arrayIndex = index;
            foreach (DictionaryEntry de in this)
            {
                array.SetValue(de, arrayIndex++);
            }
        }

        public override int Count
        {
            get
            {
                CleanIfNeeded();
                return base.Count;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}