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

namespace Lucene.Net.Support
{
    [Serializable]
    public class Set<T> : System.Collections.Generic.List<T>
    {
        System.Collections.Generic.HashSet<T> _Set = new System.Collections.Generic.HashSet<T>();

        public Set()
        {
        }
        
        public new virtual void Add(T item)
        {
            if (_Set.Contains(item)) return;
            _Set.Add(item);
            base.Add(item);
        }

        public void Add(Support.Set<T> items)
        {
            foreach(T item in items)
            {
                if(_Set.Contains(item)) continue;
                _Set.Add(item);
                base.Add(item);
            }
        }

        public void Add(System.Collections.Generic.IList<T> items)
        {
            foreach (T item in items)
            {
                if (_Set.Contains(item)) continue;
                _Set.Add(item);
                base.Add(item);
            }
        }

        public new bool Contains(T item)
        {
            return _Set.Contains(item);
        }

        public new void Clear()
        {
            _Set.Clear();
            base.Clear();
        }

        public new void Remove(T item)
        {
            _Set.Remove(item);
            base.Remove(item);
        }
    }
}