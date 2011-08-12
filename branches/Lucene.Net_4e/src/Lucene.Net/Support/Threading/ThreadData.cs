// -----------------------------------------------------------------------
// <copyright company="Apache" file="ThreadData.cs">
//
//      Licensed to the Apache Software Foundation (ASF) under one or more
//      contributor license agreements.  See the NOTICE file distributed with
//      this work for additional information regarding copyright ownership.
//      The ASF licenses this file to You under the Apache License, Version 2.0
//      (the "License"); you may not use this file except in compliance with
//      the License.  You may obtain a copy of the License at
// 
//      http://www.apache.org/licenses/LICENSE-2.0
// 
//      Unless required by applicable law or agreed to in writing, software
//      distributed under the License is distributed on an "AS IS" BASIS,
//      WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//      See the License for the specific language governing permissions and
//      limitations under the License.
//
// </copyright>
// -----------------------------------------------------------------------

namespace Lucene.Net.Support.Threading
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading;

    /// <summary>
    /// Provides Simulated GetData &amp; SetData functionality for Threads. 
    /// </summary>
    /// <remarks>
    ///    <para>
    ///     <see cref="ThreadData"/> is meant to provide functionality for storing data
    ///     in a thread similar to the way the full .NET framework does. 
    ///    </para>
    ///    <note>
    ///     Silverlight does not support the <c>GetData(LocalDataStoreSlot)</c> / 
    ///     <c>SetData(LocalDataStoreSlot, object)</c> for threads.  The Windows 
    ///     Mobile 7 version of Silverlight 4 does not even support the 
    ///     <c>ThreadStatic</c> attribute much less the generic <c>ConcurrentDictionary</c>.  
    ///    </note>
    /// </remarks>
    public static class ThreadData
    {
        private static readonly WeakDictionary<Thread, object[]> threadSlots = new WeakDictionary<Thread, object[]>();
        private static readonly object slotsSyncRoot = new object();
        private static readonly object hashSyncRoot = new object();
        private static Dictionary<string, object> hash;



        /// <summary>
        /// Gets the hash.
        /// </summary>
        /// <value>The hash.</value>
        private static Dictionary<string, object> Hash
        {
            get
            {
                lock (hashSyncRoot)
                {
                    if (hash == null) 
                        hash = new Dictionary<string, object>();
                }

                return hash;
            }
        }

        /// <summary>
        /// Allocates the data slot.
        /// </summary>
        /// <returns>
        /// An instance of <see cref="LocalDataStoreSlot"/>.
        /// </returns>
        public static LocalDataStoreSlot AllocateDataSlot()
        {
            return new LocalDataStoreSlot(true);
        }

        /// <summary>
        /// Allocates the named data slot.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>
        /// An instance of <see cref="LocalDataStoreSlot"/>.
        /// </returns>
        public static LocalDataStoreSlot AllocateNamedDataSlot(string name)
        {
            LocalDataStoreSlot slot;
            lock (hashSyncRoot)
            {
                hash = Hash;
                object value;
                if (hash.TryGetValue(name, out value))
                {
                    throw new ArgumentException("Named data slot already exists", "name");
                }

                slot = AllocateDataSlot();
                hash.Add(name, slot);
            }

            return slot;
        }

        /// <summary>
        /// Frees the named data slot.
        /// </summary>
        /// <param name="name">The name.</param>
        public static void FreeNamedDataSlot(string name)
        {
            lock (hashSyncRoot)
            {
                var store = Hash;
                object value = null;
                if (store.TryGetValue(name, out value))
                {
                    store.Remove(name);
                }
            }
        }

        /// <summary>
        /// Gets the data.
        /// </summary>
        /// <param name="slot">The slot.</param>
        /// <returns>An instance of <see cref="Object"/>.</returns>
        public static object GetData(LocalDataStoreSlot slot)
        {
            if (slot == null)
                throw new ArgumentNullException("slot");

            object data = null;
            lock (slotsSyncRoot)
            {
                object[] slots = null;
                threadSlots.TryGetValue(Thread.CurrentThread, out slots);

                if (slots != null && slot.SlotId < slots.Length)
                    data = slots[slot.SlotId];
            }

            return data;
        }

        /// <summary>
        /// Frees the local slot data.
        /// </summary>
        /// <param name="slot">The slot.</param>
        /// <param name="isThread">if set to <c>true</c> [is thread].</param>
        /// <exception cref="NotImplementedException">
        ///     Thrown when <paramref name="isThread"/> is true since the 
        ///     storing data outside a thread context is currently not supported.
        /// </exception>
        public static void FreeLocalSlotData(int slot, bool isThread)
        {
            if (!isThread)
                throw new NotImplementedException("FreeLocalSlotData currently only supports thread contexts");

            lock (slotsSyncRoot)
            {
                object[] slots = null;
                var current = Thread.CurrentThread;
                threadSlots.TryGetValue(Thread.CurrentThread, out slots);

                if (slots != null && slot < slots.Length)
                {
                    // TODO: write an extension method for arrays to RemoveAt();
                    object[] copy = new object[slots.Length - 1];
                    if (slot > 0)
                        Array.Copy(slots, 0, copy, 0, slot);

                    if (slot < (slots.Length - 1))
                        Array.Copy(slots, slot + 1, copy, slot, (slots.Length - slot - 1));

                    slots = copy;

                    threadSlots[current] = slots;
                }
            }
        }

        /// <summary>
        /// Sets the data.
        /// </summary>
        /// <param name="slot">The slot.</param>
        /// <param name="data">The data.</param>
        public static void SetData(LocalDataStoreSlot slot, object data)
        {
            lock (slotsSyncRoot)
            {
                object[] slots = null;
                if (!threadSlots.TryGetValue(Thread.CurrentThread, out slots))
                    threadSlots[Thread.CurrentThread] = slots = new object[slot.SlotId + 2];
                else if (slot.SlotId >= slots.Length)
                {
                    object[] copy = new object[slot.SlotId + 2];
                    int cap = slots.Length;
                    if (copy.Length < cap)
                        cap = copy.Length;

                    // TODO: create a static method for CopyTo
                    Array.Copy(slots, slots.GetLowerBound(0), copy, copy.GetLowerBound(0), cap);

                    threadSlots[Thread.CurrentThread] = slots = copy;
                }

                slots[slot.SlotId] = data;
            }
        }
    }
}