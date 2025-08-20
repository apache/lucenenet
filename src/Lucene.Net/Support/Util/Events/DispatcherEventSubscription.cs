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
using System.Threading;

namespace Lucene.Net.Util.Events
{
    ///<summary>
    /// Extends <see cref="EventSubscription"/> to invoke the <see cref="EventSubscription.Action"/> delegate
    /// in a specific <see cref="SynchronizationContext"/>.
    ///</summary>
    internal class DispatcherEventSubscription : EventSubscription
    {
        private readonly SynchronizationContext syncContext;

        ///<summary>
        /// Creates a new instance of <see cref="BackgroundEventSubscription"/>.
        ///</summary>
        ///<param name="actionReference">A reference to a delegate of type <see cref="System.Action{TPayload}"/>.</param>
        ///<param name="context">The synchronization context to use for UI thread dispatching.</param>
        ///<exception cref="ArgumentNullException">When <paramref name="actionReference"/> or <see paramref="filterReference"/> are <see langword="null" />.</exception>
        ///<exception cref="ArgumentException">When the target of <paramref name="actionReference"/> is not of type <see cref="System.Action{TPayload}"/>.</exception>
        public DispatcherEventSubscription(IDelegateReference actionReference, SynchronizationContext context)
            : base(actionReference)
        {
            syncContext = context;
        }

        /// <summary>
        /// Invokes the specified <see cref="System.Action{TPayload}"/> asynchronously in the specified <see cref="SynchronizationContext"/>.
        /// </summary>
        /// <param name="action">The action to execute.</param>
        public override void InvokeAction(Action action)
        {
            syncContext.Post((o) => action(), null);
        }
    }

    ///<summary>
    /// Extends <see cref="EventSubscription{TPayload}"/> to invoke the <see cref="EventSubscription{TPayload}.Action"/> delegate
    /// in a specific <see cref="SynchronizationContext"/>.
    ///</summary>
    /// <typeparam name="TPayload">The type to use for the generic <see cref="System.Action{TPayload}"/> and <see cref="Predicate{TPayload}"/> types.</typeparam>
    internal class DispatcherEventSubscription<TPayload> : EventSubscription<TPayload>
    {
        private readonly SynchronizationContext syncContext;

        ///<summary>
        /// Creates a new instance of <see cref="BackgroundEventSubscription{TPayload}"/>.
        ///</summary>
        ///<param name="actionReference">A reference to a delegate of type <see cref="System.Action{TPayload}"/>.</param>
        ///<param name="filterReference">A reference to a delegate of type <see cref="Predicate{TPayload}"/>.</param>
        ///<param name="context">The synchronization context to use for UI thread dispatching.</param>
        ///<exception cref="ArgumentNullException">When <paramref name="actionReference"/> or <see paramref="filterReference"/> are <see langword="null" />.</exception>
        ///<exception cref="ArgumentException">When the target of <paramref name="actionReference"/> is not of type <see cref="System.Action{TPayload}"/>,
        ///or the target of <paramref name="filterReference"/> is not of type <see cref="Predicate{TPayload}"/>.</exception>
        public DispatcherEventSubscription(IDelegateReference actionReference, IDelegateReference filterReference, SynchronizationContext context)
            : base(actionReference, filterReference)
        {
            syncContext = context;
        }

        /// <summary>
        /// Invokes the specified <see cref="System.Action{TPayload}"/> asynchronously in the specified <see cref="SynchronizationContext"/>.
        /// </summary>
        /// <param name="action">The action to execute.</param>
        /// <param name="argument">The payload to pass <paramref name="action"/> while invoking it.</param>
        public override void InvokeAction(Action<TPayload> action, TPayload argument)
        {
            syncContext.Post((o) => action((TPayload)o), argument);
        }
    }
}

#endif
