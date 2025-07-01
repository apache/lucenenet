// Source: https://github.com/PrismLibrary/Prism/blob/7f0b1680bbe754da790274f80851265f808d9bbf

#region Copyright .NET Foundation, Licensed under the MIT License (MIT)
// The MIT License (MIT)
//
// Copyright(c).NET Foundation
//
// All rights reserved. Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated
// documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use,
// copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software
// is furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
// OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR
// IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
#endregion

#if !FEATURE_CONDITIONALWEAKTABLE_ENUMERATOR

using System;
using System.Collections.Generic;
using System.Linq;

namespace Lucene.Net.Util.Events
{
    /// <summary>
    /// Manage delegates using weak references to prevent keeping target instances longer than expected.
    /// </summary>
    internal class WeakDelegatesManager
    {
        private readonly List<DelegateReference> _listeners = new List<DelegateReference>();

        /// <summary>
        /// Adds a weak reference to the specified <see cref="Delegate"/> listener.
        /// </summary>
        /// <param name="listener">The original <see cref="Delegate"/> to add.</param>
        public void AddListener(Delegate listener)
        {
            _listeners.Add(new DelegateReference(listener, false));
        }

        /// <summary>
        /// Removes the weak reference to the specified <see cref="Delegate"/> listener.
        /// </summary>
        /// <param name="listener">The original <see cref="Delegate"/> to remove.</param>
        public void RemoveListener(Delegate listener)
        {
            //Remove the listener, and prune collected listeners
            _listeners.RemoveAll(reference => reference.TargetEquals(null) || reference.TargetEquals(listener));
        }

        /// <summary>
        /// Invoke the delegates for all targets still being alive.
        /// </summary>
        /// <param name="args">An array of objects that are the arguments to pass to the delegates. -or- null, if the method represented by the delegate does not require arguments. </param>
        public void Raise(params object[] args)
        {
            _listeners.RemoveAll(listener => listener.TargetEquals(null));

            foreach (Delegate handler in _listeners.Select(listener => listener.Target).Where(listener => listener != null).ToList())
            {
                handler.DynamicInvoke(args);
            }
        }
    }
}

#endif
