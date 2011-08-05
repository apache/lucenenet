// -----------------------------------------------------------------------
// <copyright company="Apache" file="ArrayUtilTest.cs" >
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
    using Categories = Lucene.Net.TestCategories;


    [TestFixture]
    [Category(Categories.Unit)]
    [Parallelizable]
    public class ArrayUtilTest
    {

        [Test]
        public void OversizeGrowthAlgorythm()
        {
            int currentSize = 0;
            long copyCost = 0;

            Assert.IsTrue(IntPtr.Size == 4 || IntPtr.Size == 8, "The int ptr size should be either 8 (64bit) or 4 (32bit)");

            while (currentSize != int.MaxValue)
            {
                int nextSize = ArrayUtil.Oversize(1 + currentSize, RamUsageEstimator.NumBytesObjectRef);
                
                Assert.IsTrue(nextSize > currentSize);
                
                if (currentSize > 0)
                {
                    copyCost += currentSize;
                    double copyCostPerElement = ((double)copyCost / currentSize);
                    Assert.IsTrue(copyCostPerElement < 10.0, "cost : " + copyCostPerElement);
                }

                currentSize = nextSize;
            }
        }
    }
}
