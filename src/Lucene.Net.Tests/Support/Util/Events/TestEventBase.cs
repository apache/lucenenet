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
using Assert = Lucene.Net.TestFramework.Assert;

namespace Lucene.Net.Util.Events
{
    [TestFixture]
    public class TestEventBase
    {
        [Test]
        public void CanPublishSimpleEvents()
        {
            var eventBase = new TestableEventBase();
            var eventSubscription = new MockEventSubscription();
            bool eventPublished = false;
            eventSubscription.GetPublishActionReturnValue = delegate
            {
                eventPublished = true;
            };
            eventBase.Subscribe(eventSubscription);

            eventBase.Publish();

            Assert.True(eventSubscription.GetPublishActionCalled);
            Assert.True(eventPublished);
        }

        [Test]
        public void CanHaveMultipleSubscribersAndRaiseCustomEvent()
        {
            var customEvent = new TestableEventBase();
            Payload payload = new Payload();
            object[] received1 = null;
            object[] received2 = null;
            var eventSubscription1 = new MockEventSubscription();
            eventSubscription1.GetPublishActionReturnValue = delegate (object[] args) { received1 = args; };
            var eventSubscription2 = new MockEventSubscription();
            eventSubscription2.GetPublishActionReturnValue = delegate (object[] args) { received2 = args; };

            customEvent.Subscribe(eventSubscription1);
            customEvent.Subscribe(eventSubscription2);

            customEvent.Publish(payload);

            Assert.AreEqual(1, received1.Length);
            Assert.AreSame(received1[0], payload);

            Assert.AreEqual(1, received2.Length);
            Assert.AreSame(received2[0], payload);
        }

        [Test]
        public void ShouldSubscribeAndUnsubscribe()
        {
            var eventBase = new TestableEventBase();

            var eventSubscription = new MockEventSubscription();
            eventBase.Subscribe(eventSubscription);

            Assert.NotNull(eventSubscription.SubscriptionToken);
            Assert.True(eventBase.Contains(eventSubscription.SubscriptionToken));

            eventBase.Unsubscribe(eventSubscription.SubscriptionToken);

            Assert.False(eventBase.Contains(eventSubscription.SubscriptionToken));
        }

        [Test]
        public void WhenEventSubscriptionActionIsNullPruneItFromList()
        {
            var eventBase = new TestableEventBase();

            var eventSubscription = new MockEventSubscription();
            eventSubscription.GetPublishActionReturnValue = null;

            var token = eventBase.Subscribe(eventSubscription);

            eventBase.Publish();

            Assert.False(eventBase.Contains(token));
        }


        class TestableEventBase : EventBase
        {
            public SubscriptionToken Subscribe(IEventSubscription subscription)
            {
                return base.InternalSubscribe(subscription);
            }

            public void Publish(params object[] arguments)
            {
                base.InternalPublish(arguments);
            }
        }

        class MockEventSubscription : IEventSubscription
        {
            public Action<object[]> GetPublishActionReturnValue;
            public bool GetPublishActionCalled;

            public Action<object[]> GetExecutionStrategy()
            {
                GetPublishActionCalled = true;
                return GetPublishActionReturnValue;
            }

            public SubscriptionToken SubscriptionToken { get; set; }
        }

        class Payload { }

    }
}

#endif