// -----------------------------------------------------------------------
// <copyright file="WeakReferenceOfTTest.cs" company="Apache">
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

namespace Lucene.Net.Support
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

#if NUNIT
    using NUnit.Framework;
    using Extensions.NUnit;
#else 
    using Gallio.Framework;
    using MbUnit.Framework;
#endif

    using Lucene.Net.Internal;
    using System.Threading;
    
    [TestFixture]
    [Category(TestCategories.Unit)]
    [Parallelizable]
    public class WeakReferenceOfTTest
    {

       

        [Test]
        public void Constructor()
        {
            var instance = Create();
            WeakReference<ReferenceType> weakReference = null;

            Assert.DoesNotThrow(() =>
            {
                weakReference = new WeakReference<ReferenceType>(instance);
            });

            Assert.AreEqual(instance, weakReference.Target);
            Assert.IsTrue(weakReference.IsAlive);
            Assert.IsFalse(weakReference.TrackResurrection);
        }

        [Test]
        public void ConstructorWithResurrection()
        {
            var instance = Create();
            WeakReference<ReferenceType> weakReference = null;

            Assert.DoesNotThrow(() =>
            {
                weakReference = new WeakReference<ReferenceType>(instance, true);
            });

            Assert.AreEqual(instance, weakReference.Target);
            Assert.IsTrue(weakReference.IsAlive);
            Assert.IsTrue(weakReference.TrackResurrection);
        }

        [Test]
        public void GarbageCollection()
        {
            // creating the instance inside the method would keep the instance alive,
            // thus we create this instance in a static method.
            var instance = Create();
            WeakReference<ReferenceType> weakReference = null;    

            weakReference = new WeakReference<ReferenceType>(instance);

            Assert.AreEqual(instance, weakReference.Target);
            Assert.IsTrue(weakReference.IsAlive);
            

            instance = null;

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.WaitForFullGCComplete();

            Assert.IsFalse(weakReference.IsAlive);
            Assert.IsNull(weakReference.Target);            
        }

        private static ReferenceType Create()
        {
            var instance = new ReferenceType() { Name = "test" };
            return instance;
        }
    }
}
