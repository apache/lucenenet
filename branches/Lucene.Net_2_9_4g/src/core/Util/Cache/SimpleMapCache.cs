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
using System.Collections.Generic;

namespace Lucene.Net.Util.Cache
{
	
	/// <summary> Simple cache implementation that uses a HashMap to store (key, value) pairs.
	/// This cache is not synchronized, use {@link Cache#SynchronizedCache(Cache)}
	/// if needed.
	/// </summary>
    public class SimpleMapCache<K, V> : Cache<K, V>
	{
        internal Support.Dictionary<K, V> map;
		
		public SimpleMapCache():this(new Support.Dictionary<K,V>())
		{
		}

        public SimpleMapCache(Support.Dictionary<K, V> map)
		{
			this.map = map;
		}
		
		public override V Get(K key)
		{
			return map[key];
		}
		
		public override void  Put(K key, V value_Renamed)
		{
			map[key] = value_Renamed;
		}
		
		public override void  Close()
		{
			// NOOP
		}
		
		public override bool ContainsKey(K key)
		{
			return map.ContainsKey(key);
		}
		
		/// <summary> Returns a Set containing all keys in this cache.</summary>
		public virtual ICollection<K> KeySet()
		{
			return map.Keys;
		}
		
		internal override Cache<K,V> GetSynchronizedCache()
		{
			return new SynchronizedSimpleMapCache<K,V>(this);
		}
		
		private class SynchronizedSimpleMapCache<K,V>:SimpleMapCache<K,V>
		{
			internal System.Object mutex;
			internal SimpleMapCache<K,V> cache;
			
			internal SynchronizedSimpleMapCache(SimpleMapCache<K,V> cache)
			{
				this.cache = cache;
				this.mutex = this;
			}
			
			public override void  Put(K key, V value_Renamed)
			{
				lock (mutex)
				{
					cache.Put(key, value_Renamed);
				}
			}
			
			public override V Get(K key)
			{
				lock (mutex)
				{
					return cache.Get(key);
				}
			}
			
			public override bool ContainsKey(K key)
			{
				lock (mutex)
				{
					return cache.ContainsKey(key);
				}
			}
			
			public override void  Close()
			{
				lock (mutex)
				{
					cache.Close();
				}
			}
			
			public override ICollection<K> KeySet()
			{
				lock (mutex)
				{
					return cache.KeySet();
				}
			}
			
			internal override Cache<K,V> GetSynchronizedCache()
			{
				return this;
			}
		}
	}
}