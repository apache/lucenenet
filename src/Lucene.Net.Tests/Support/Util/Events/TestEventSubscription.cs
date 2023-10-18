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
using System.Collections.Generic;
using Assert = Lucene.Net.TestFramework.Assert;

namespace Lucene.Net.Util.Events
{
    [TestFixture]
    public class TestEventSubscription
    {
        [Test]
        public void NullTargetInActionThrows()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                var actionDelegateReference = new MockDelegateReference()
                {
                    Target = null
                };
                var filterDelegateReference = new MockDelegateReference()
                {
                    Target = (Predicate<object>)(arg =>
                    {
                        return true;
                    })
                };
                var eventSubscription = new EventSubscription<object>(actionDelegateReference,
                                                                                filterDelegateReference);
            });

        }

        [Test]
        public void NullTargetInActionThrowsNonGeneric()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                var actionDelegateReference = new MockDelegateReference()
                {
                    Target = null
                };
                var eventSubscription = new EventSubscription(actionDelegateReference);
            });
        }

        [Test]
        public void DifferentTargetTypeInActionThrows()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                var actionDelegateReference = new MockDelegateReference()
                {
                    Target = (Action<int>)delegate { }
                };
                var filterDelegateReference = new MockDelegateReference()
                {
                    Target = (Predicate<string>)(arg =>
                    {
                        return true;
                    })
                };
                var eventSubscription = new EventSubscription<string>(actionDelegateReference,
                                                                                filterDelegateReference);
            });
        }

        [Test]
        public void DifferentTargetTypeInActionThrowsNonGeneric()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                var actionDelegateReference = new MockDelegateReference()
                {
                    Target = (Action<int>)delegate { }
                };

                var eventSubscription = new EventSubscription(actionDelegateReference);
            });
        }

        [Test]
        public void NullActionThrows()
        {
            Assert.Throws<ArgumentNullException>(() =>
            {
                var filterDelegateReference = new MockDelegateReference()
                {
                    Target = (Predicate<object>)(arg =>
                    {
                        return true;
                    })
                };
                var eventSubscription = new EventSubscription<object>(null, filterDelegateReference);
            });
        }

        [Test]
        public void NullActionThrowsNonGeneric()
        {
            Assert.Throws<ArgumentNullException>(() =>
            {
                var eventSubscription = new EventSubscription(null);
            });
        }

        [Test]
        public void NullTargetInFilterThrows()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                var actionDelegateReference = new MockDelegateReference()
                {
                    Target = (Action<object>)delegate { }
                };

                var filterDelegateReference = new MockDelegateReference()
                {
                    Target = null
                };
                var eventSubscription = new EventSubscription<object>(actionDelegateReference,
                                                                                filterDelegateReference);
            });
        }


        [Test]
        public void DifferentTargetTypeInFilterThrows()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                var actionDelegateReference = new MockDelegateReference()
                {
                    Target = (Action<string>)delegate { }
                };

                var filterDelegateReference = new MockDelegateReference()
                {
                    Target = (Predicate<int>)(arg =>
                    {
                        return true;
                    })
                };

                var eventSubscription = new EventSubscription<string>(actionDelegateReference,
                                                                                filterDelegateReference);
            });
        }

        [Test]
        public void NullFilterThrows()
        {
            Assert.Throws<ArgumentNullException>(() =>
            {
                var actionDelegateReference = new MockDelegateReference()
                {
                    Target = (Action<object>)delegate { }
                };

                var eventSubscription = new EventSubscription<object>(actionDelegateReference,
                                                                                null);
            });
        }

        [Test]
        public void CanInitEventSubscription()
        {
            var actionDelegateReference = new MockDelegateReference((Action<object>)delegate { });
            var filterDelegateReference = new MockDelegateReference((Predicate<object>)delegate { return true; });
            var eventSubscription = new EventSubscription<object>(actionDelegateReference, filterDelegateReference);

            var subscriptionToken = new SubscriptionToken(t => { });

            eventSubscription.SubscriptionToken = subscriptionToken;

            Assert.AreSame(actionDelegateReference.Target, eventSubscription.Action);
            Assert.AreSame(filterDelegateReference.Target, eventSubscription.Filter);
            Assert.AreSame(subscriptionToken, eventSubscription.SubscriptionToken);
        }

        [Test]
        public void CanInitEventSubscriptionNonGeneric()
        {
            var actionDelegateReference = new MockDelegateReference((Action)delegate { });
            var eventSubscription = new EventSubscription(actionDelegateReference);

            var subscriptionToken = new SubscriptionToken(t => { });

            eventSubscription.SubscriptionToken = subscriptionToken;

            Assert.AreSame(actionDelegateReference.Target, eventSubscription.Action);
            Assert.AreSame(subscriptionToken, eventSubscription.SubscriptionToken);
        }

        [Test]
        public void GetPublishActionReturnsDelegateThatExecutesTheFilterAndThenTheAction()
        {
            var executedDelegates = new List<string>();
            var actionDelegateReference =
                new MockDelegateReference((Action<object>)delegate { executedDelegates.Add("Action"); });

            var filterDelegateReference = new MockDelegateReference((Predicate<object>)delegate
            {
                executedDelegates.Add(
                    "Filter");
                return true;
            });

            var eventSubscription = new EventSubscription<object>(actionDelegateReference, filterDelegateReference);


            var publishAction = eventSubscription.GetExecutionStrategy();

            Assert.NotNull(publishAction);

            publishAction.Invoke(null);

            Assert.AreEqual(2, executedDelegates.Count);
            Assert.AreEqual("Filter", executedDelegates[0]);
            Assert.AreEqual("Action", executedDelegates[1]);
        }

        [Test]
        public void GetPublishActionReturnsNullIfActionIsNull()
        {
            var actionDelegateReference = new MockDelegateReference((Action<object>)delegate { });
            var filterDelegateReference = new MockDelegateReference((Predicate<object>)delegate { return true; });

            var eventSubscription = new EventSubscription<object>(actionDelegateReference, filterDelegateReference);

            var publishAction = eventSubscription.GetExecutionStrategy();

            Assert.NotNull(publishAction);

            actionDelegateReference.Target = null;

            publishAction = eventSubscription.GetExecutionStrategy();

            Assert.Null(publishAction);
        }

        [Test]
        public void GetPublishActionReturnsNullIfActionIsNullNonGeneric()
        {
            var actionDelegateReference = new MockDelegateReference((Action)delegate { });

            var eventSubscription = new EventSubscription(actionDelegateReference);

            var publishAction = eventSubscription.GetExecutionStrategy();

            Assert.NotNull(publishAction);

            actionDelegateReference.Target = null;

            publishAction = eventSubscription.GetExecutionStrategy();

            Assert.Null(publishAction);
        }

        [Test]
        public void GetPublishActionReturnsNullIfFilterIsNull()
        {
            var actionDelegateReference = new MockDelegateReference((Action<object>)delegate { });
            var filterDelegateReference = new MockDelegateReference((Predicate<object>)delegate { return true; });

            var eventSubscription = new EventSubscription<object>(actionDelegateReference, filterDelegateReference);

            var publishAction = eventSubscription.GetExecutionStrategy();

            Assert.NotNull(publishAction);

            filterDelegateReference.Target = null;

            publishAction = eventSubscription.GetExecutionStrategy();

            Assert.Null(publishAction);
        }

        [Test]
        public void GetPublishActionDoesNotExecuteActionIfFilterReturnsFalse()
        {
            bool actionExecuted = false;
            var actionDelegateReference = new MockDelegateReference()
            {
                Target = (Action<int>)delegate { actionExecuted = true; }
            };
            var filterDelegateReference = new MockDelegateReference((Predicate<int>)delegate
            {
                return false;
            });

            var eventSubscription = new EventSubscription<int>(actionDelegateReference, filterDelegateReference);


            var publishAction = eventSubscription.GetExecutionStrategy();

            publishAction.Invoke(new object[] { null });

            Assert.False(actionExecuted);
        }

        [Test]
        public void StrategyPassesArgumentToDelegates()
        {
            string passedArgumentToAction = null;
            string passedArgumentToFilter = null;

            var actionDelegateReference = new MockDelegateReference((Action<string>)(obj => passedArgumentToAction = obj));
            var filterDelegateReference = new MockDelegateReference((Predicate<string>)(obj =>
            {
                passedArgumentToFilter = obj;
                return true;
            }));

            var eventSubscription = new EventSubscription<string>(actionDelegateReference, filterDelegateReference);
            var publishAction = eventSubscription.GetExecutionStrategy();

            publishAction.Invoke(new[] { "TestString" });

            Assert.AreEqual("TestString", passedArgumentToAction);
            Assert.AreEqual("TestString", passedArgumentToFilter);
        }
    }
}

#endif