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
using System.Threading.Tasks;
using Assert = Lucene.Net.TestFramework.Assert;

namespace Lucene.Net.Util.Events
{
    [TestFixture]
    public class TestDelegateReference
    {
        [Test]
        public void KeepAlivePreventsDelegateFromBeingCollected()
        {
            var delegates = new SomeClassHandler();
            var delegateReference = new DelegateReference((Action<string>)delegates.DoEvent, true);

            delegates = null;
            GC.Collect();

            Assert.NotNull(delegateReference.Target);
        }

        [Test]
        public async Task NotKeepAliveAllowsDelegateToBeCollected()
        {
            var delegates = new SomeClassHandler();
            var delegateReference = new DelegateReference((Action<string>)delegates.DoEvent, false);

            delegates = null;
            await Task.Delay(100);
            GC.Collect();

            Assert.Null(delegateReference.Target);
        }

        [Test]
        public async Task NotKeepAliveKeepsDelegateIfStillAlive()
        {
            var delegates = new SomeClassHandler();
            var delegateReference = new DelegateReference((Action<string>)delegates.DoEvent, false);

            GC.Collect();

            Assert.NotNull(delegateReference.Target);

            GC.KeepAlive(delegates);  //Makes delegates ineligible for garbage collection until this point (to prevent oompiler optimizations that may release the referenced object prematurely).
            delegates = null;
            await Task.Delay(100);
            GC.Collect();

            Assert.Null(delegateReference.Target);
        }

        [Test]
        public void TargetShouldReturnAction()
        {
            var classHandler = new SomeClassHandler();
            Action<string> myAction = new Action<string>(classHandler.MyAction);

            var weakAction = new DelegateReference(myAction, false);

            ((Action<string>)weakAction.Target)("payload");
            Assert.AreEqual("payload", classHandler.MyActionArg);
        }

        [Test]
        public async Task ShouldAllowCollectionOfOriginalDelegate()
        {
            var classHandler = new SomeClassHandler();
            Action<string> myAction = new Action<string>(classHandler.MyAction);

            var weakAction = new DelegateReference(myAction, false);

            var originalAction = new WeakReference(myAction);
            myAction = null;
            await Task.Delay(100);
            GC.Collect();
            Assert.False(originalAction.IsAlive);

            ((Action<string>)weakAction.Target)("payload");
            Assert.AreEqual("payload", classHandler.MyActionArg);
        }

        [Test]
        public async Task ShouldReturnNullIfTargetNotAlive()
        {
            SomeClassHandler handler = new SomeClassHandler();
            var weakHandlerRef = new WeakReference(handler);

            var action = new DelegateReference((Action<string>)handler.DoEvent, false);

            handler = null;
            await Task.Delay(100);
            GC.Collect();
            Assert.False(weakHandlerRef.IsAlive);

            Assert.Null(action.Target);
        }

        [Test]
        public void WeakDelegateWorksWithStaticMethodDelegates()
        {
            var action = new DelegateReference((Action)SomeClassHandler.StaticMethod, false);

            Assert.NotNull(action.Target);
        }

        [Test]
        public void TargetEqualsActionShouldReturnTrue()
        {
            var classHandler = new SomeClassHandler();
            Action<string> myAction = new Action<string>(classHandler.MyAction);

            var weakAction = new DelegateReference(myAction, false);

            Assert.True(weakAction.TargetEquals(new Action<string>(classHandler.MyAction)));
        }

        [Test]
        public async Task TargetEqualsNullShouldReturnTrueIfTargetNotAlive()
        {
            SomeClassHandler handler = new SomeClassHandler();
            var weakHandlerRef = new WeakReference(handler);

            var action = new DelegateReference((Action<string>)handler.DoEvent, false);

            handler = null;

            // Intentional delay to encourage Garbage Collection to actually occur
            await Task.Delay(100);
            GC.Collect();
            Assert.False(weakHandlerRef.IsAlive);

            Assert.True(action.TargetEquals(null));
        }

        [Test]
        public void TargetEqualsNullShouldReturnFalseIfTargetAlive()
        {
            SomeClassHandler handler = new SomeClassHandler();
            var weakHandlerRef = new WeakReference(handler);

            var action = new DelegateReference((Action<string>)handler.DoEvent, false);

            Assert.False(action.TargetEquals(null));
            Assert.True(weakHandlerRef.IsAlive);
            GC.KeepAlive(handler);
        }

        [Test]
        public void TargetEqualsWorksWithStaticMethodDelegates()
        {
            var action = new DelegateReference((Action)SomeClassHandler.StaticMethod, false);

            Assert.True(action.TargetEquals((Action)SomeClassHandler.StaticMethod));
        }

        //todo: fix
        //[Test]
        //public void NullDelegateThrows()
        //{
        //    Assert.ThrowsException<ArgumentNullException>(() =>
        //    {
        //        var action = new DelegateReference(null, true);
        //    });
        //}

        public class SomeClassHandler
        {
            public string MyActionArg;

            public void DoEvent(string value)
            {
                string myValue = value;
            }

            public static void StaticMethod()
            {
#pragma warning disable 0219
                int i = 0;
#pragma warning restore 0219
            }

            public void MyAction(string arg)
            {
                MyActionArg = arg;
            }
        }
    }
}

#endif
