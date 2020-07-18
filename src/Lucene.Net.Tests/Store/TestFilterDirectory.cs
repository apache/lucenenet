using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Reflection;
using Assert = Lucene.Net.TestFramework.Assert;
using JCG = J2N.Collections.Generic;

namespace Lucene.Net.Store
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

    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;

    [TestFixture]
    public class TestFilterDirectory : LuceneTestCase
    {
        [Test]
        public virtual void TestOverrides()
        {
            // verify that all methods of Directory are overridden by FilterDirectory,
            // except those under the 'exclude' list

            // LUCENENET specific - using string here because MethodInfo.GetHashCode() returns a different
            // value even if the signature is the same. The string seems to be a reasonable way to check 
            // equality between method signatures.
            ISet<string> exclude = new JCG.HashSet<string>();
            exclude.Add(typeof(Directory).GetMethod("Copy", new Type[] { typeof(Directory), typeof(string), typeof(string), typeof(IOContext) }).ToString());
            exclude.Add(typeof(Directory).GetMethod("CreateSlicer", new Type[] { typeof(string), typeof(IOContext) }).ToString());
            exclude.Add(typeof(Directory).GetMethod("OpenChecksumInput", new Type[] { typeof(string), typeof(IOContext) }).ToString());
            foreach (MethodInfo m in typeof(FilterDirectory).GetMethods())
            {
                // LUCNENET specific - Dispose() is final (we have to override the protected method).
                // Since it is abstract, no need for a check here - the compiler takes care of that.
                if (!m.IsFinal && m.DeclaringType == typeof(Directory))
                {
                    Assert.IsTrue(exclude.Contains(m.ToString()), "method " + m.Name + " not overridden!");
                }
            }
        }
    }
}