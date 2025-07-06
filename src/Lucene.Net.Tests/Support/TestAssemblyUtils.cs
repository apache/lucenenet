using Lucene.Net.Attributes;
using Lucene.Net.Util;
using NUnit.Framework;
using System.Linq;

namespace Lucene.Net.Support
{
    /*
     * Licensed to the Apache Software Foundation (ASF) under one or more
     * contributor license agreements.  See the NOTICE file distributed with
     * this work for additional information regarding copyright ownership.
     * The ASF licenses this file to You under the Apache License, Version 2.0
     * (the "License"); you may not use this file except in compliance with
     * the License.  You may obtain a copy of the License at
     *
     *     http://www.apache.org/licenses/LICENSE-2.0
     *
     * Unless required by applicable law or agreed to in writing, software
     * distributed under the License is distributed on an "AS IS" BASIS,
     * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
     * See the License for the specific language governing permissions and
     * limitations under the License.
     */

    [TestFixture]
    public class TestAssemblyUtils : LuceneTestCase
    {
        [Test, LuceneNetSpecific]
        public void TestGetReferencedAssemblies()
        {
            var assemblies = AssemblyUtils.GetReferencedAssemblies().ToList();
            Assert.Greater(assemblies.Count, 0);
            Assert.IsTrue(assemblies.Contains(typeof(LuceneVersion).Assembly)); // Lucene.Net should definitely be in the list
        }
    }
}
