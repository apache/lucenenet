// -----------------------------------------------------------------------
// <copyright file="AttributeSourceTest.cs" company="Apache">
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

namespace Lucene.Net.Util
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
    using Lucene.Net.Analysis.TokenAttributes;
  
    [TestFixture]
    [Category(TestCategories.Unit)]
    [Parallelizable]
    public class AttributeSourceTest
    {

        [Test]
        public void GetInterfaces()
        {
            var foundInterfaces = AttributeSource.GetAttributeInterfaces(typeof(AttributeBaseTest.TwoAttributes));

            var first = foundInterfaces.First.Value.Target;
            var second = foundInterfaces.Last.Value.Target;

            Assert.AreEqual(2, foundInterfaces.Count);

            Assert.IsTrue((first.Name == "ISecondAttribute" || first.Name == "IFlagsAttribute"));
            Assert.IsTrue((second.Name == "ISecondAttribute" || second.Name == "IFlagsAttribute"));

            foundInterfaces = AttributeSource.GetAttributeInterfaces(this.GetType());

            Assert.AreEqual(0, foundInterfaces.Count);

            foundInterfaces = null;
            foundInterfaces = AttributeSource.GetAttributeInterfaces(typeof(AttributeFacade));

            Assert.AreEqual(0, foundInterfaces.Count);
        }


        #region Helpers

        private class AttributeFacade : IAttribute
        {

        }

        #endregion
    }
}
