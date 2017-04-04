/*
 *
 * Licensed to the Apache Software Foundation (ASF) under one
 * or more contributor license agreements.  See the NOTICE file
 * distributed with this work for additional information
 * regarding copyright ownership.  The ASF licenses this file
 * to you under the Apache License, Version 2.0 (the
 * "License"); you may not use this file except in compliance
 * with the License.  You may obtain a copy of the License at
 *
 *   http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing,
 * software distributed under the License is distributed on an
 * "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
 * KIND, either express or implied.  See the License for the
 * specific language governing permissions and limitations
 * under the License.
 *
*/

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;

namespace Lucene.Net.Support
{
    /// <summary>A collection of <typeparamref name="TItem"/> which can be
    /// looked up by instances of <typeparamref name="TKey"/>.</summary>
    /// <typeparam name="TItem">The type of the items contains in this
    /// collection.</typeparam>
    /// <typeparam name="TKey">The type of the keys that can be used to look
    /// up the items.</typeparam>
    internal class GeneralKeyedCollection<TKey, TItem> : System.Collections.ObjectModel.KeyedCollection<TKey, TItem>
    {
        /// <summary>Creates a new instance of the
        /// <see cref="GeneralKeyedCollection{TKey, TItem}"/> class.</summary>
        /// <param name="converter">The <see cref="Converter{TInput, TOutput}"/> which will convert
        /// instances of <typeparamref name="TItem"/> to <typeparamref name="TKey"/>
        /// when the override of <see cref="GetKeyForItem(TItem)"/> is called.</param>
        internal GeneralKeyedCollection(Func<TItem, TKey> converter)
            : base()
        {
            // If the converter is null, throw an exception.
            if (converter == null) throw new ArgumentNullException("converter");

            // Store the converter.
            this.converter = converter;

            // That's all folks.
            return;
        }

        /// <summary>The <see cref="Converter{TInput, TOutput}"/> which will convert
        /// instances of <typeparamref name="TItem"/> to <typeparamref name="TKey"/>
        /// when the override of <see cref="GetKeyForItem(TItem)"/> is called.</summary>
        private readonly Func<TItem, TKey> converter;

        /// <summary>Converts an item that is added to the collection to
        /// a key.</summary>
        /// <param name="item">The instance of <typeparamref name="TItem"/>
        /// to convert into an instance of <typeparamref name="TKey"/>.</param>
        /// <returns>The instance of <typeparamref name="TKey"/> which is the
        /// key for this item.</returns>
        protected override TKey GetKeyForItem(TItem item)
        {
            // The converter is not null.
            Debug.Assert(converter != null);

            // Call the converter.
            return converter(item);
        }

        /// <summary>Determines if a key for an item exists in this
        /// collection.</summary>
        /// <param name="key">The instance of <typeparamref name="TKey"/>
        /// to see if it exists in this collection.</param>
        /// <returns>True if the key exists in the collection, false otherwise.</returns>
        public bool ContainsKey(TKey key)
        {
            // Call the dictionary - it is lazily created when the first item is added
            if (Dictionary != null)
            {
                return Dictionary.ContainsKey(key);
            }
            else
            {
                return false;
            }
        }

        public ICollection<TKey> Keys
        {
            get
            {
                if (this.Dictionary != null)
                {
                    return this.Dictionary.Keys;
                }
                else
                {
                    return new Collection<TKey>(this.Select(this.GetKeyForItem).ToArray());
                }
            }
        }

        public System.Collections.Generic.IList<TItem> Values()
        {
            return base.Items;
        }
    }
}