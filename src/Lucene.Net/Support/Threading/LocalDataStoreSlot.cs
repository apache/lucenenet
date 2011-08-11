// -----------------------------------------------------------------------
// <copyright company="Apache" file="LocalDataStoreSlot.cs">
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

    /// <summary>
    /// Encapsulates a memory slot to store local data. This class cannot be inherited.
    /// This currently only supports Threading Slots.
    /// </summary>
    public sealed class LocalDataStoreSlot
    {
        private static readonly object syncRoot = new object();
        private static bool[] bitmap;


        /// <summary>
        /// Initializes a new instance of the <see cref="LocalDataStoreSlot"/> class.
        /// </summary>
        /// <param name="isThreadLocal">if set to <c>true</c> [in thread].</param>
        internal LocalDataStoreSlot(bool isThreadLocal)
        {
            if (!isThreadLocal)
                throw new NotImplementedException("Only saving to a thread is currently supported");

            this.IsThreadLocal = true;

            lock (syncRoot)
            {
                int i;
                bool[] bitmapCopy = bitmap;

                if (bitmapCopy != null)
                {
                    // find a slot that has been closed, assign the index
                    for (i = 0; i < bitmapCopy.Length; ++i)
                    {
                        if (!bitmapCopy[i])
                        {
                            this.SlotId = i;
                            bitmapCopy[i] = true;
                            return;
                        }
                    }

                    // if a slot was not open, expand bitmap 2 places
                    bool[] newBitmap = new bool[i + 2];
                    Array.Copy(bitmapCopy, newBitmap, newBitmap.Length);
                    bitmapCopy = newBitmap;
                }
                else
                {
                   // create a new bitmap
                   bitmapCopy = new bool[2];
                   i = 0;
                }

                // assign slot
                bitmapCopy[i] = true;
                this.SlotId = i;
                bitmap = bitmapCopy;
            }
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="LocalDataStoreSlot"/> class. 
        /// Releases unmanaged resources and performs other cleanup operations before the
        /// <see cref="LocalDataStoreSlot"/> is reclaimed by garbage collection.
        /// </summary>
        ~LocalDataStoreSlot()
        {
            lock (syncRoot)
            {
                ThreadData.FreeLocalSlotData(this.SlotId, this.IsThreadLocal);
                bitmap[this.SlotId] = false;
            }
        }

        /// <summary>
        /// Gets or sets the slot index.
        /// </summary>
        /// <value>The index of the slot.</value>
        internal int SlotId { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is thread local.
        /// </summary>
        /// <value>
        ///     <c>true</c> if this instance is thread local; otherwise, <c>false</c>.
        /// </value>
        internal bool IsThreadLocal { get; set; }
    }
}