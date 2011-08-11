// -----------------------------------------------------------------------
// <copyright company="Apache" file="ThreadLocalTest.cs" >
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

using Lucene.Net.Support.Threading;

namespace Lucene.Net.Support
{
#if NUNIT
    using NUnit.Framework;
    using Extensions.NUnit;
#else
    using Gallio.Framework;
    using MbUnit.Framework;
#endif
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading;


    [TestFixture]
    [Category(TestCategories.Unit)]
    [Parallelizable]
    public class ThreadLocalTest
    {
        private Threading.ThreadLocal<string> local;
            
        [Test]
        public void ProofOfConcept()
        {
           int i = 0;
           this.local = new Threading.ThreadLocal<string>(() => {
               i++;
               return i.ToString();
           });

           Console.WriteLine(this.local.Value);
           Thread t = new Thread(() => {
               Console.WriteLine(local.Value);
           });
           t.Start();
           t.Join(2000);
        }
    }
}
