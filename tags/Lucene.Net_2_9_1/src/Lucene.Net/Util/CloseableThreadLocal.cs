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

namespace Lucene.Net.Util
{
	
	/// <summary>Java's builtin ThreadLocal has a serious flaw:
	/// it can take an arbitrarily long amount of time to
	/// dereference the things you had stored in it, even once the
	/// ThreadLocal instance itself is no longer referenced.
	/// This is because there is single, master map stored for
	/// each thread, which all ThreadLocals share, and that
	/// master map only periodically purges "stale" entries.
	/// 
	/// While not technically a memory leak, because eventually
	/// the memory will be reclaimed, it can take a long time
	/// and you can easily hit OutOfMemoryError because from the
	/// GC's standpoint the stale entries are not reclaimaible.
	/// 
	/// This class works around that, by only enrolling
	/// WeakReference values into the ThreadLocal, and
	/// separately holding a hard reference to each stored
	/// value.  When you call {@link #close}, these hard
	/// references are cleared and then GC is freely able to
	/// reclaim space by objects stored in it. 
	/// </summary>
	
	public class CloseableThreadLocal
	{
		
		private System.LocalDataStoreSlot t = System.Threading.Thread.AllocateDataSlot();
		
		private System.Collections.IDictionary hardRefs = new System.Collections.Hashtable();
		
		public /*protected internal*/ virtual System.Object InitialValue()
		{
			return null;
		}
		
		public virtual System.Object Get()
		{
			System.WeakReference weakRef = (System.WeakReference) System.Threading.Thread.GetData(t);
			if (weakRef == null || weakRef.Target==null)
			{
				System.Object iv = InitialValue();
				if (iv != null)
				{
					Set(iv);
					return iv;
				}
				else
					return null;
			}
			else
			{
				return weakRef.Target;
			}
		}
		
		public virtual void  Set(System.Object object_Renamed)
		{
			
			System.Threading.Thread.SetData(this.t, new System.WeakReference(object_Renamed));
			
			lock (hardRefs.SyncRoot)
			{
				hardRefs[SupportClass.ThreadClass.Current()] = object_Renamed;
				
				// Purge dead threads
                System.Collections.ArrayList tmp = new System.Collections.ArrayList();
                System.Collections.IEnumerator it = hardRefs.GetEnumerator();
				while (it.MoveNext())
				{
					SupportClass.ThreadClass t = (SupportClass.ThreadClass) ((System.Collections.DictionaryEntry) it.Current).Key;
					if (!t.IsAlive)
					{
                        tmp.Add(t);
					}
				}
                foreach (SupportClass.ThreadClass th in tmp)
                {
                    hardRefs.Remove(th);
                }
            }
		}
		
		public virtual void  Close()
		{
			// Clear the hard refs; then, the only remaining refs to
			// all values we were storing are weak (unless somewhere
			// else is still using them) and so GC may reclaim them:
			hardRefs = null;
			t = null;
		}
	}
}