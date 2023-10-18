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

using NUnit.Framework;
using System;
using System.Threading;
using Assert = Lucene.Net.TestFramework.Assert;

namespace Lucene.Net.Util.Events
{
    [TestFixture]
    public class TestDispatcherEventSubscription
    {
        [Test]
        public void ShouldCallInvokeOnDispatcher()
        {
            DispatcherEventSubscription<object> eventSubscription = null;

            IDelegateReference actionDelegateReference = new MockDelegateReference()
            {
                Target = (Action<object>)(arg =>
                {
                    return;
                })
            };

            IDelegateReference filterDelegateReference = new MockDelegateReference
            {
                Target = (Predicate<object>)(arg => true)
            };
            var mockSyncContext = new MockSynchronizationContext();

            eventSubscription = new DispatcherEventSubscription<object>(actionDelegateReference, filterDelegateReference, mockSyncContext);

            eventSubscription.GetExecutionStrategy().Invoke(new object[0]);

            Assert.True(mockSyncContext.InvokeCalled);
        }

        [Test]
        public void ShouldCallInvokeOnDispatcherNonGeneric()
        {
            DispatcherEventSubscription eventSubscription = null;

            IDelegateReference actionDelegateReference = new MockDelegateReference()
            {
                Target = (Action)(() =>
                { })
            };

            var mockSyncContext = new MockSynchronizationContext();

            eventSubscription = new DispatcherEventSubscription(actionDelegateReference, mockSyncContext);

            eventSubscription.GetExecutionStrategy().Invoke(new object[0]);

            Assert.True(mockSyncContext.InvokeCalled);
        }

        [Test]
        public void ShouldPassParametersCorrectly()
        {
            IDelegateReference actionDelegateReference = new MockDelegateReference()
            {
                Target =
                    (Action<object>)(arg1 =>
                    {
                        return;
                    })
            };
            IDelegateReference filterDelegateReference = new MockDelegateReference
            {
                Target = (Predicate<object>)(arg => true)
            };

            var mockSyncContext = new MockSynchronizationContext();

            DispatcherEventSubscription<object> eventSubscription = new DispatcherEventSubscription<object>(actionDelegateReference, filterDelegateReference, mockSyncContext);

            var executionStrategy = eventSubscription.GetExecutionStrategy();
            Assert.NotNull(executionStrategy);

            object argument1 = new object();

            executionStrategy.Invoke(new[] { argument1 });

            Assert.AreSame(argument1, mockSyncContext.InvokeArg);
        }
    }

    internal class MockSynchronizationContext : SynchronizationContext
    {
        public bool InvokeCalled;
        public object InvokeArg;

        public override void Post(SendOrPostCallback d, object state)
        {
            InvokeCalled = true;
            InvokeArg = state;
        }
    }
}

#endif