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
using System.Threading.Tasks;

namespace Lucene.Net.Util.Events
{
    /// <summary>
    /// Extends <see cref="EventSubscription"/> to invoke the <see cref="EventSubscription.Action"/> delegate in a background thread.
    /// </summary>
    internal class BackgroundEventSubscription : EventSubscription
    {
        /// <summary>
        /// Creates a new instance of <see cref="BackgroundEventSubscription"/>.
        /// </summary>
        /// <param name="actionReference">A reference to a delegate of type <see cref="System.Action"/>.</param>
        /// <exception cref="ArgumentNullException">When <paramref name="actionReference"/> or <see paramref="filterReference"/> are <see langword="null" />.</exception>
        /// <exception cref="ArgumentException">When the target of <paramref name="actionReference"/> is not of type <see cref="System.Action"/>.</exception>
        public BackgroundEventSubscription(IDelegateReference actionReference)
            : base(actionReference)
        {
        }

        /// <summary>
        /// Invokes the specified <see cref="System.Action"/> in an asynchronous thread by using a <see cref="Task"/>.
        /// </summary>
        /// <param name="action">The action to execute.</param>
        public override void InvokeAction(Action action)
        {
            Task.Run(action);
        }
    }
    /// <summary>
    /// Extends <see cref="EventSubscription{TPayload}"/> to invoke the <see cref="EventSubscription{TPayload}.Action"/> delegate in a background thread.
    /// </summary>
    /// <typeparam name="TPayload">The type to use for the generic <see cref="System.Action{TPayload}"/> and <see cref="Predicate{TPayload}"/> types.</typeparam>
    internal class BackgroundEventSubscription<TPayload> : EventSubscription<TPayload>
    {
        /// <summary>
        /// Creates a new instance of <see cref="BackgroundEventSubscription{TPayload}"/>.
        /// </summary>
        /// <param name="actionReference">A reference to a delegate of type <see cref="System.Action{TPayload}"/>.</param>
        /// <param name="filterReference">A reference to a delegate of type <see cref="Predicate{TPayload}"/>.</param>
        /// <exception cref="ArgumentNullException">When <paramref name="actionReference"/> or <see paramref="filterReference"/> are <see langword="null" />.</exception>
        /// <exception cref="ArgumentException">When the target of <paramref name="actionReference"/> is not of type <see cref="System.Action{TPayload}"/>,
        /// or the target of <paramref name="filterReference"/> is not of type <see cref="Predicate{TPayload}"/>.</exception>
        public BackgroundEventSubscription(IDelegateReference actionReference, IDelegateReference filterReference)
            : base(actionReference, filterReference)
        {
        }

        /// <summary>
        /// Invokes the specified <see cref="System.Action{TPayload}"/> in an asynchronous thread by using a <see cref="System.Threading.ThreadPool"/>.
        /// </summary>
        /// <param name="action">The action to execute.</param>
        /// <param name="argument">The payload to pass <paramref name="action"/> while invoking it.</param>
        public override void InvokeAction(Action<TPayload> action, TPayload argument)
        {
            //ThreadPool.QueueUserWorkItem( (o) => action(argument) );
            Task.Run(() => action(argument));
        }
    }
}
#endif
