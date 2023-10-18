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
using System.Reflection;

namespace Lucene.Net.Util.Events
{
    /// <summary>
    /// Represents a reference to a <see cref="Delegate"/> that may contain a
    /// <see cref="WeakReference"/> to the target. This class is used
    /// internally.
    /// </summary>
    internal class DelegateReference : IDelegateReference
    {
        private readonly Delegate _delegate;
        private readonly WeakReference _weakReference;
        private readonly MethodInfo _method;
        private readonly Type _delegateType;

        /// <summary>
        /// Initializes a new instance of <see cref="DelegateReference"/>.
        /// </summary>
        /// <param name="delegate">The original <see cref="Delegate"/> to create a reference for.</param>
        /// <param name="keepReferenceAlive">If <see langword="false" /> the class will create a weak reference to the delegate, allowing it to be garbage collected. Otherwise it will keep a strong reference to the target.</param>
        /// <exception cref="ArgumentNullException">If the passed <paramref name="delegate"/> is not assignable to <see cref="Delegate"/>.</exception>
        public DelegateReference(Delegate @delegate, bool keepReferenceAlive)
        {
            if (@delegate == null)
                throw new ArgumentNullException("delegate");

            if (keepReferenceAlive)
            {
                this._delegate = @delegate;
            }
            else
            {
                _weakReference = new WeakReference(@delegate.Target);
                _method = @delegate.GetMethodInfo();
                _delegateType = @delegate.GetType();
            }
        }

        /// <summary>
        /// Gets the <see cref="Delegate" /> (the target) referenced by the current <see cref="DelegateReference"/> object.
        /// </summary>
        /// <value><see langword="null"/> if the object referenced by the current <see cref="DelegateReference"/> object has been garbage collected; otherwise, a reference to the <see cref="Delegate"/> referenced by the current <see cref="DelegateReference"/> object.</value>
        public Delegate Target
        {
            get
            {
                if (_delegate != null)
                {
                    return _delegate;
                }
                else
                {
                    return TryGetDelegate();
                }
            }
        }

        /// <summary>
        /// Checks if the <see cref="Delegate" /> (the target) referenced by the current <see cref="DelegateReference"/> object are equal to another <see cref="Delegate" />.
        /// This is equivalent with comparing <see cref="Target"/> with <paramref name="delegate"/>, only more efficient.
        /// </summary>
        /// <param name="delegate">The other delegate to compare with.</param>
        /// <returns>True if the target referenced by the current object are equal to <paramref name="delegate"/>.</returns>
        public bool TargetEquals(Delegate @delegate)
        {
            if (_delegate != null)
            {
                return _delegate == @delegate;
            }
            if (@delegate == null)
            {
                return !_method.IsStatic && !_weakReference.IsAlive;
            }
            return _weakReference.Target == @delegate.Target && Equals(_method, @delegate.GetMethodInfo());
        }

        private Delegate TryGetDelegate()
        {
            if (_method.IsStatic)
            {
                return _method.CreateDelegate(_delegateType, null);
            }
            object target = _weakReference.Target;
            if (target != null)
            {
                return _method.CreateDelegate(_delegateType, target);
            }
            return null;
        }
    }
}

#endif