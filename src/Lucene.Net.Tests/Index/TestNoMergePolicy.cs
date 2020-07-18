using Lucene.Net.Support;
using NUnit.Framework;
using System;
using System.Reflection;
using Assert = Lucene.Net.TestFramework.Assert;

namespace Lucene.Net.Index
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
    public class TestNoMergePolicy : LuceneTestCase
    {
        [Test]
        public virtual void TestNoMergePolicy_Mem()
        {
            MergePolicy mp = NoMergePolicy.NO_COMPOUND_FILES;
            Assert.IsNull(mp.FindMerges(/*null*/ (MergeTrigger)int.MinValue, (SegmentInfos)null));
            Assert.IsNull(mp.FindForcedMerges(null, 0, null));
            Assert.IsNull(mp.FindForcedDeletesMerges(null));
            Assert.IsFalse(mp.UseCompoundFile(null, null));
            mp.Dispose();
        }

        [Test]
        public virtual void TestCompoundFiles()
        {
            Assert.IsFalse(NoMergePolicy.NO_COMPOUND_FILES.UseCompoundFile(null, null));
            Assert.IsTrue(NoMergePolicy.COMPOUND_FILES.UseCompoundFile(null, null));
        }

        [Test]
        public virtual void TestFinalSingleton()
        {
            assertTrue(typeof(NoMergePolicy).IsSealed);
            ConstructorInfo[] ctors = typeof(NoMergePolicy).GetConstructors(BindingFlags.Instance |
                    BindingFlags.NonPublic |
                    BindingFlags.Public |
                    BindingFlags.DeclaredOnly); // LUCENENET NOTE: It seems .NET automatically adds a private static constructor, so leaving off the static BindingFlag
            assertEquals("expected 1 private ctor only: " + Arrays.ToString(ctors), 1, ctors.Length);
            assertTrue("that 1 should be private: " + ctors[0], ctors[0].IsPrivate);
        }

        [Test]
        public virtual void TestMethodsOverridden()
        {
            // Ensures that all methods of MergePolicy are overridden. That's important
            // to ensure that NoMergePolicy overrides everything, so that no unexpected
            // behavior/error occurs
            foreach (MethodInfo m in typeof(NoMergePolicy).GetMethods())
            {
                // getDeclaredMethods() returns just those methods that are declared on
                // NoMergePolicy. getMethods() returns those that are visible in that
                // context, including ones from Object. So just filter out Object. If in
                // the future MergePolicy will extend a different class than Object, this
                // will need to change.
                if (m.Name.Equals("Clone", StringComparison.Ordinal))
                {
                    continue;
                }
                if (m.DeclaringType != typeof(object) && !m.IsFinal && m.IsVirtual)
                {
                    Assert.IsTrue(m.DeclaringType == typeof(NoMergePolicy), m + " is not overridden ! Declaring Type: " + m.DeclaringType);
                }
            }
        }
    }
}